using Common;
using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogic.Algorithms
{
    public class TwoPhaseSimplex : Algorithm
    {
        private int numberOfArtificialVars = 0;

        public override void PutModelInCanonicalForm(Model model)
        {
            // First make sure that we don't have any negative RHS in the constraints
            for (int i = 0; i < model.Constraints.Count; i++)
            {
                if (model.Constraints[i].RightHandSide < 0)
                {
                    for (int j = 0; j < model.Constraints[i].DecisionVariables.Count; j++)
                    {
                        model.Constraints[i].DecisionVariables[j].Coefficient *= -1;
                    }

                    model.Constraints[i].RightHandSide *= -1;

                    if (model.Constraints[i].InequalitySign == InequalitySign.LessThanOrEqualTo)
                    {
                        model.Constraints[i].InequalitySign = InequalitySign.GreaterThanOrEqualTo;
                    }
                    else if (model.Constraints[i].InequalitySign == InequalitySign.GreaterThanOrEqualTo)
                    {
                        model.Constraints[i].InequalitySign = InequalitySign.LessThanOrEqualTo;
                    }
                }
            }

            // Our initial table
            List<List<double>> tableZero = new List<List<double>>();

            // Add the objective function coefficients to the initial table first (and take the decision variables across the = sign: make them negative)
            tableZero.Add(new List<double>());

            foreach (var decVar in model.ObjectiveFunction.DecisionVariables)
            {
                tableZero[0].Add(decVar.Coefficient * -1);
            }

            foreach (var constraint in model.Constraints)
            {
                if (constraint.InequalitySign == InequalitySign.LessThanOrEqualTo ||
                    constraint.InequalitySign == InequalitySign.GreaterThanOrEqualTo)
                {
                    tableZero[0].Add(0);
                }

                if (constraint.InequalitySign == InequalitySign.EqualTo ||
                    constraint.InequalitySign == InequalitySign.GreaterThanOrEqualTo)
                {
                    tableZero[0].Add(0);
                }
            }

            tableZero[0].Add(0);

            // Add the constraints
            for (int i = 0; i < model.Constraints.Count; i++)
            {
                List<double> constraintValues = new List<double>();

                // Add the technical coefficients of the decision variables to the constraints
                foreach (var decVar in model.Constraints[i].DecisionVariables)
                {
                    constraintValues.Add(decVar.Coefficient);
                }

                // Add the slack/excess variables
                for (int j = 0; j < model.Constraints.Count; j++)
                {
                    if (model.Constraints[j].InequalitySign == InequalitySign.LessThanOrEqualTo)
                    {
                        if (j == i)
                        {
                            constraintValues.Add(1);
                        }
                        else
                        {
                            constraintValues.Add(0);
                        }
                    }
                    else if (model.Constraints[j].InequalitySign == InequalitySign.GreaterThanOrEqualTo)
                    {
                        if (j == i)
                        {
                            constraintValues.Add(-1);
                        }
                        else
                        {
                            constraintValues.Add(0);
                        }
                    }
                }

                // Add the RHS of our constraint
                constraintValues.Add(model.Constraints[i].RightHandSide);

                // Add artificial variables
                for (int j = 0; j < model.Constraints.Count; j++)
                {
                    if (model.Constraints[j].InequalitySign == InequalitySign.EqualTo ||
                        model.Constraints[j].InequalitySign == InequalitySign.GreaterThanOrEqualTo)
                    {
                        if (j == i)
                        {
                            constraintValues.Insert(constraintValues.Count - 1, 1);
                        }
                        else
                        {
                            constraintValues.Insert(constraintValues.Count - 1, 0);
                        }
                    }
                }

                tableZero.Add(constraintValues);
            }

            // Add the w row which is equal to the sum of the artificial variables
            numberOfArtificialVars = model.Constraints.Where(c => c.InequalitySign ==
            InequalitySign.EqualTo || c.InequalitySign == InequalitySign.GreaterThanOrEqualTo).Count();

            List<double> wRow = new List<double>();
            for (int i = 0; i < tableZero[0].Count; i++)
            {
                wRow.Add(0);
            }

            for (int i = 1; i < tableZero.Count; i++)
            {
                if (model.Constraints[i - 1].InequalitySign == InequalitySign.EqualTo ||
                    model.Constraints[i - 1].InequalitySign == InequalitySign.GreaterThanOrEqualTo)
                {
                    for (int j = 0; j < (tableZero[i].Count - numberOfArtificialVars - 1); j++)
                    {
                        wRow[j] += tableZero[i][j];
                    }

                    wRow[wRow.Count - 1] += tableZero[i][tableZero[i].Count - 1];
                }
            }

            tableZero.Insert(0, wRow);
            model.Result.Add(tableZero);
        }

        public override void Solve(Model model)
        {
            Iterate(model);
            var lastTable = model.Result[model.Result.Count - 1];

            // Remove w row and artificial variables
            if (lastTable[0][lastTable[0].Count - 1] > 0)
            {
                throw new InfeasibleException("The problem is infeasible");
            }

            // If all artificial variables are basic and w = 0, drop w row and artificial variables
            bool allArtificialsNonBasic = true;
            for (int i = lastTable[0].Count - (numberOfArtificialVars + 1); i < lastTable[0].Count - 1; i++)
            {
                if (IsVariableBasic(i, lastTable))
                {
                    allArtificialsNonBasic = false;
                    break;
                }
            }

            if (allArtificialsNonBasic)
            {
                // Remove w row
                lastTable.RemoveAt(0);

                // Remove all artificial variables
                for (int i = 0; i < lastTable.Count; i++)
                {
                    for (int j = 0; j < numberOfArtificialVars; j++)
                    {
                        lastTable[i].RemoveAt(lastTable[i].Count - 2);
                    }
                }
            }

            var primalSimplex = new PrimalSimplex();
            primalSimplex.Solve(model);
        }

        private bool IsOptimal(Model model)
        {
            bool isOptimal = true;
            var table = model.Result[model.Result.Count - 1];

            for (int i = 0; i < table[0].Count - 1; i++)
            {
                if (table[0][i] > 0)
                {
                    isOptimal = false;
                    break;
                }
            }

            return isOptimal;
        }

        private void Iterate(Model model)
        {
            // Check if optimal - if not, iterate
            if (IsOptimal(model))
                return;

            // Get the pivot column first
            int pivotColumn = GetPivotColumn(model);
            // Then get the pivot row
            int pivotRow = GetPivotRow(model, pivotColumn);

            if (pivotRow == -1)
                throw new InfeasibleException("There is no suitable row to pivot on - the problem is infeasible");

            Pivot(model, pivotRow, pivotColumn);

            // Recursively iterate until the model is optimal (or until we cannot iterate anymore - infeasible)
            Iterate(model);
        }

        private void Pivot(Model model, int pivotRow, int pivotColumn)
        {
            var previousTable = model.Result[model.Result.Count - 1];
            var newTable = new List<List<double>>();

            for (int i = 0; i < previousTable.Count; i++)
            {
                newTable.Add(new List<double>());

                for (int j = 0; j < previousTable[i].Count; j++)
                {
                    newTable[i].Add(previousTable[i][j]);
                }
            }

            // Let's first make our pivot point = 1
            double factor = 1 / newTable[pivotRow][pivotColumn];
            for (int i = 0; i < newTable[pivotRow].Count; i++)
            {
                newTable[pivotRow][i] *= factor;
            }

            // Now we need to ensure 0s in all other rows of our pivot column
            double pivotColumnValue;
            for (int i = 0; i < newTable.Count; i++)
            {
                pivotColumnValue = newTable[i][pivotColumn];

                if (i != pivotRow && pivotColumnValue != 0)
                {
                    for (int j = 0; j < newTable[i].Count; j++)
                    {
                        newTable[i][j] += (-1 * pivotColumnValue * newTable[pivotRow][j]);
                    }
                }
            }

            model.Result.Add(newTable);
        }

        private int GetPivotColumn(Model model)
        {
            int colIndex = -1;
            var table = model.Result[model.Result.Count - 1];
            double mostPositive = 0;

            for (int i = 0; i < table[0].Count - 1; i++)
            {
                if (table[0][i] > 0 && table[0][i] > mostPositive)
                {
                    mostPositive = table[0][i];
                    colIndex = i;
                }
            }

            return colIndex;
        }

        private int GetPivotRow(Model model, int pivotColumn)
        {
            int rowIndex = -1;
            var table = model.Result[model.Result.Count - 1];

            double lowestRatio = double.MaxValue;
            for (int i = 1; i < table.Count; i++)
            {
                if (table[i][pivotColumn] > 0)
                {
                    double ratio = table[i][table[i].Count - 1] / table[i][pivotColumn];
                    if (ratio < lowestRatio && ratio >= 0)
                    {
                        lowestRatio = ratio;
                        rowIndex = i;
                    }
                }
            }

            return rowIndex;
        }

        private bool IsVariableBasic(int index, List<List<double>> table)
        {
            bool isBasic = true;

            for (int i = 0; i < table.Count; i++)
            {
                int numberOfOnes = 0;

                if (table[i][index] == 1)
                    numberOfOnes++;

                if ((table[i][index] != 0 && table[i][index] != 1) || numberOfOnes > 1)
                {
                    isBasic = false;
                    break;
                }
            }

            return isBasic;
        }
    }
}
