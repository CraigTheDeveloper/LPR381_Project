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
        public override void PutModelInCanonicalForm(Model model)
        {
            // Our initial table
            List<List<double>> initialTable = new List<List<double>>();

            // Add the objective function coefficients to the initial table first (and take the decision variables across the = sign: make them negative)
            initialTable.Add(new List<double>());

            foreach (var decVar in model.ObjectiveFunction.DecisionVariables)
            {
                initialTable[0].Add(decVar.Coefficient * -1);
            }

            // We need to add a slack/excess & artificial variables so we add extra columns with 0s
            // If we have any = constraints then we need to split those into 2 <= and >= constraints
            for (int i = 0; i < model.Constraints.Count; i++)
            {
                initialTable[0].Add(0);
                if (model.Constraints[i].InequalitySign == InequalitySign.EqualTo)
                    initialTable[0].Add(0);
            }

            // Finally, add a 0 for the RHS of the objective function
            initialTable[0].Add(0);

            // Firstly, make rhs non negative
            var nonNegativeConstraints = model.Constraints.Where(c => c.InequalitySign == InequalitySign.EqualTo).ToList();
            if (nonNegativeConstraints?.Count() > 0)
            {
                for (int i = 0; i < nonNegativeConstraints.Count(); i++)
                {
                    model.Constraints[model.Constraints.FindIndex(c => c == nonNegativeConstraints[i])].InequalitySign = InequalitySign.LessThanOrEqualTo;
                    var newConstraint = new Constraint();
                    newConstraint.InequalitySign = InequalitySign.GreaterThanOrEqualTo;
                    newConstraint.RightHandSide = nonNegativeConstraints[i].RightHandSide;

                    foreach (var decVar in nonNegativeConstraints[i].DecisionVariables)
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

                // Add relevant 0s and 1s for our slack, excess and artificial variables
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

                initialTable.Add(constraintValues);
            }

            model.Result.Add(initialTable);

        }

        private bool CanPivot(Model model)
        {
            // If we have any negatives in the RHS then we can pivot (obviously if we also have a row to pivot on)
            bool canPivot = false;

            var table = model.Result[model.Result.Count - 1];

            for (int i = 1; i < table.Count; i++)
            {
                if (table[i][table[i].Count - 1] < 0)
                {
                    canPivot = true;
                    break;
                }
            }

            return canPivot;
        }

        private void wElim(Model model, int wRow, int excessCols)
        {
            var previousTable = model.Result[model.Result.Count - 1];
            var newTable = new List<List<double>>();

            if (model.Constraints[wRow].RightHandSide > 0)
            {
                throw new InfeasibleException("There is no feasible solution.");
            }
            else
            {
                for (int i = wRow; i < previousTable.Count; wRow++)
                {
                    newTable.Add(new List<double>());

                    for (int j = 0; j < previousTable[i].Count; j++)
                    {
                        newTable[i].Add(previousTable[i][j]);
                    }
                }
            }
            model.Result.Add(newTable);
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

            if (model.ProblemType == ProblemType.Maximization)
            {
                double mostNegative = 0;

                for (int i = 0; i < table[0].Count - 1; i++)
                {
                    if (table[0][i] < 0 && table[0][i] < mostNegative)
                    {
                        mostNegative = table[0][i];
                        colIndex = i;
                    }
                }
            }
            else
            {
                double mostPositive = 0;

                for (int i = 0; i < table[0].Count - 1; i++)
                {
                    if (table[0][i] > 0 && table[0][i] > mostPositive)
                    {
                        mostPositive = table[0][i];
                        colIndex = i;
                    }
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

        private bool IsOptimal(Model model)
        {
            bool isOptimal = true;
            var table = model.Result[model.Result.Count - 1];

            if (model.ProblemType == ProblemType.Maximization)
            {
                for (int i = 0; i < table[0].Count - 1; i++)
                {
                    if (table[0][i] < 0)
                    {
                        isOptimal = false;
                        break;
                    }
                }
            }
            else
            {
                for (int i = 0; i < table[0].Count - 1; i++)
                {
                    if (table[0][i] > 0)
                    {
                        isOptimal = false;
                        break;
                    }
                }
            }

            return isOptimal;
        }

        private void Iterate(Model model)
        {

            // Get the pivot column first
            int pivotColumn = GetPivotColumn(model);
            // Then get the pivot row
            int pivotRow = GetPivotRow(model, pivotColumn);
            // Check if optimal - if not, iterate
            if (IsOptimal(model))
                return;

            if (pivotRow == -1)
                throw new InfeasibleException("There is no suitable row to pivot on - the problem is infeasible");

            Pivot(model, pivotRow, pivotColumn);

            // Recursively iterate until the model is optimal (or until we cannot iterate anymore - infeasible)
            Iterate(model);
        }

        public override void Solve(Model model)
        {
            Iterate(model);
            var primalSimplex = new PrimalSimplex();
            primalSimplex.Solve(model);
        }
    }
}
