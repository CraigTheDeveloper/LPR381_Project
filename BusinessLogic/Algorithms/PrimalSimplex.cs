using Common;
using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogic.Algorithms
{
    public class PrimalSimplex : Algorithm
    {
        public override void PutModelInCanonicalForm(Model model)
        {
            // We should have a normal max problem if the execution reaches this point. This is because the "higher up" algorithms like dual
            // simplex and two phase simplex will have already put the model in their respective canonical forms and by the time we reach
            // primal simplex we just need to solve

            // Our initial table
            List<List<double>> tableZero = new List<List<double>>();

            // Add the objective function coefficients to the initial table first (and take the decision variables across the = sign: make them negative)
            tableZero.Add(new List<double>());

            foreach (var decVar in model.ObjectiveFunction.DecisionVariables)
            {
                tableZero[0].Add(decVar.Coefficient * -1);
            }

            // Add columns with 0s for the slack variables and RHS
            for (int i = 0; i <= model.Constraints.Count; i++)
            {
                tableZero[0].Add(0);
            }

            for (int i = 0; i < model.Constraints.Count; i++)
            {
                List<double> constraintValues = new List<double>();

                // Add the technical coefficients of the decision variables to the constraints
                foreach (var decVar in model.Constraints[i].DecisionVariables)
                {
                    constraintValues.Add(decVar.Coefficient);
                }

                // Add relevant 0s and 1s for our slack variables
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
                constraintValues.Add(model.Constraints[i].RightHandSide);

                tableZero.Add(constraintValues);
            }

            // Add the initial table to the model's result, which will store the canonical form of the model for the algorithm
            model.Result.Add(tableZero);
        }

        public override void Solve(Model model)
        {
            Iterate(model);
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
    }
}
