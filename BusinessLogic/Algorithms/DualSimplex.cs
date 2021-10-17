using Common;
using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogic.Algorithms
{
    public class DualSimplex : Algorithm
    {
        public override void PutModelInCanonicalForm(Model model)
        {
            // Our initial table
            List<List<double>> tableZero = new List<List<double>>();

            // Add the objective function coefficients to the initial table first (and take the decision variables across the = sign: make them negative)
            tableZero.Add(new List<double>());

            foreach (var decVar in model.ObjectiveFunction.DecisionVariables)
            {
                tableZero[0].Add(decVar.Coefficient * -1);
            }

            // We need to add a slack/excess variable for each constraint so we add extra columns with 0s
            // If we have any = constraints then we need to split those into 2 <= and >= constraints
            for (int i = 0; i < model.Constraints.Count; i++)
            {
                tableZero[0].Add(0);
                if (model.Constraints[i].InequalitySign == InequalitySign.EqualTo)
                    tableZero[0].Add(0);
            }

            // Finally, add a 0 for the RHS of the objective function
            tableZero[0].Add(0);

            // First we'll split any = constraints
            var equalsConstraints = model.Constraints.Where(c => c.InequalitySign == InequalitySign.EqualTo).ToList();
            if (equalsConstraints?.Count() > 0)
            {
                for (int i = 0; i < equalsConstraints.Count(); i++)
                {
                    model.Constraints[model.Constraints.FindIndex(c => c == equalsConstraints[i])].InequalitySign = InequalitySign.LessThanOrEqualTo;
                    var newConstraint = new Constraint();
                    newConstraint.InequalitySign = InequalitySign.GreaterThanOrEqualTo;
                    newConstraint.RightHandSide = equalsConstraints[i].RightHandSide;

                    foreach (var decVar in equalsConstraints[i].DecisionVariables)
                    {
                        newConstraint.DecisionVariables.Add(new DecisionVariable() { Coefficient = decVar.Coefficient });
                    }

                    model.Constraints.Add(newConstraint);
                }
            }

            for (int i = 0; i < model.Constraints.Count; i++)
            {
                List<double> constraintValues = new List<double>();

                // Add the technical coefficients of the decision variables to the constraints
                foreach (var decVar in model.Constraints[i].DecisionVariables)
                {
                    if (model.Constraints[i].InequalitySign == InequalitySign.LessThanOrEqualTo)
                    {
                        constraintValues.Add(decVar.Coefficient);
                    }
                    else
                    {
                        constraintValues.Add(decVar.Coefficient * -1);
                    }
                }

                // Add relevant 0s and 1s for our slack and excess variables
                for (int j = 0; j < model.Constraints.Count; j++)
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

                // Add the RHS of our constraint
                if (model.Constraints[i].InequalitySign == InequalitySign.LessThanOrEqualTo)
                {
                    constraintValues.Add(model.Constraints[i].RightHandSide);
                }
                else
                {
                    constraintValues.Add(model.Constraints[i].RightHandSide * -1);
                }
                
                tableZero.Add(constraintValues);
            }

            model.Result.Add(tableZero);
        }

        public override void Solve(Model model)
        {
            Iterate(model);
            var primalSimplex = new PrimalSimplex();
            primalSimplex.Solve(model);
        }

        private void Iterate(Model model)
        {
            // Check if we can pivot
            if (!CanPivot(model))
                return;

            // Get the pivot row
            int pivotRow = GetPivotRow(model);
            // Then get the pivot column first
            int pivotColumn = GetPivotColumn(model, pivotRow);

            if (pivotColumn == -1)
                throw new InfeasibleException("There is no suitable column to pivot on - the problem is infeasible");

            Pivot(model, pivotRow, pivotColumn);

            // Recursively iterate until the model is optimal (or until we cannot iterate anymore - infeasible)
            Iterate(model);
        }

        private bool CanPivot(Model model)
        {
            // If we have any negatives in the RHS then we can pivot (obviously if we also have a column to pivot on)
            bool canPivot = false;
            var table = model.Result[model.Result.Count - 1];

            for (int i = 1; i < table.Count; i++)
            {
                if (table[i][table[i].Count - 1] < -0.000000000001)
                {
                    canPivot = true;
                    break;
                }
            }

            return canPivot;
        }

        private int GetPivotRow(Model model)
        {
            int pivotRow = -1;
            var table = model.Result[model.Result.Count - 1];
            double mostNegative = 0;

            for (int i = 1; i < table.Count; i++)
            {
                if (table[i][table[i].Count - 1] < 0 && table[i][table[i].Count - 1] < mostNegative)
                {
                    mostNegative = table[i][table[i].Count - 1];
                    pivotRow = i;
                }
            }

            return pivotRow;
        }

        private int GetPivotColumn(Model model, int pivotRow)
        {
            int pivotColumn = -1;
            var table = model.Result[model.Result.Count - 1];

            double lowestRatio = double.MaxValue;
            for (int i = 0; i < table[0].Count - 1; i++)
            {
                if (table[pivotRow][i] < 0)
                {
                    double ratio = Math.Abs(table[0][i] / table[pivotRow][i]);
                    if (ratio < lowestRatio)
                    {
                        lowestRatio = ratio;
                        pivotColumn = i;
                    }
                }
            }

            return pivotColumn;
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
    }
}
