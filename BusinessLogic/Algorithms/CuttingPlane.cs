using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogic.Algorithms
{
    public class CuttingPlane : Algorithm
    {
        // Cutting Plane relies on other algorithms to find a partially optimal solution, which
        // is then used to find a solution fitting integer requirements.

        private DualSimplex dualSimplex = new DualSimplex();

        public override void PutModelInCanonicalForm(Model model)
        {
            dualSimplex.PutModelInCanonicalForm(model);
            // Solve the relaxed LP
            dualSimplex.Solve(model);
        }

        public override void Solve(Model model)
        {
            while (CanCut(model).Count > 0)
            {       
                Cut(model);
                dualSimplex.Solve(model);
            }
        }

        private void Cut(Model model)
        {
            var table = model.Result[model.Result.Count - 1];

            // Figure out which variable we need to cut on
            int cutVariableIndex = GetCutVariable(table, CanCut(model));

            // Find the row that the cut variable is basic in
            int basicRow = GetBasicRow(table, cutVariableIndex);

            List<double> cutConstraint = GetCutConstraint(table, basicRow);

            // For user friendliness, we'll show the old table in the results as well as the new table with the newly added constraint as opposed to
            // just adding the new constraint to the old table (which would work, but it would be harder to see where constraints were added)
            var newTable = ListCloner.CloneList(table);

            // Add the new constraint to the new table
            newTable.Add(cutConstraint);

            // Add a column with 0s and 1 for the new slack variable
            for (int i = 0; i < newTable.Count; i++)
            {
                if (i == newTable.Count - 1)
                {
                    newTable[i].Insert(newTable[i].Count - 1, 1);
                }
                else
                {
                    newTable[i].Insert(newTable[i].Count - 1, 0);
                }
            }

            // Add the new table to the model
            model.Result.Add(newTable);
        }

        private List<double> GetCutConstraint(List<List<double>> table, int basicRow)
        {
            List<double> cutConstraint = new List<double>();

            for (int i = 0; i < table[basicRow].Count; i++)
            {
                // We only care about non-integer values when cutting (integer values would stay on the left and be discarded when we create 
                // our new constraint)
                if (table[basicRow][i] == Math.Truncate(table[basicRow][i]))
                {
                    cutConstraint.Add(0);
                }
                else
                {
                    // Split the number into an integer (the nearest integer <= the number) and a fractional part (the difference between the numbers)
                    double fractionalPart = Math.Abs(Math.Floor(table[basicRow][i]) - table[basicRow][i]);
                    cutConstraint.Add(-1 * fractionalPart);
                }
            }

            return cutConstraint;
        }

        private List<int> CanCut(Model model)
        {
            List<int> intBinVarIndexes = new List<int>();
            List<int> indexesToDiscard = new List<int>();
            var lastTable = model.Result[model.Result.Count - 1];

            // Get the indexes of the int/bin variables
            for (int i = 0; i < model.SignRestrictions.Count; i++)
            {
                if (model.SignRestrictions[i] == SignRestriction.Integer || model.SignRestrictions[i] == SignRestriction.Binary)
                {
                    intBinVarIndexes.Add(i);
                }
            }

            // Check each int/bin variable to see if the sign restriction is violated
            foreach (var intBinVar in intBinVarIndexes)
            {
                // First check if the variable is non-basic (which means it = 0 and int/bin restriction is satisfied)
                if (!IsVariableBasic(intBinVar, lastTable))
                {
                    indexesToDiscard.Add(intBinVar);
                }
                // If it is basic, make sure the RHS is not already an integer
                else
                {
                    double rhs = GetRhsOfVariable(intBinVar, lastTable);

                    if (rhs - Math.Truncate(rhs) == 0)
                    {
                        indexesToDiscard.Add(intBinVar);
                    }
                }
            }

            intBinVarIndexes.RemoveAll(v => indexesToDiscard.Contains(v) == true);

            return intBinVarIndexes;
        }

        private double GetRhsOfVariable(int intBinVar, List<List<double>> table)
        {
            if (!IsVariableBasic(intBinVar, table))
                return 0;

            double rhs = 0;

            for (int i = 1; i < table.Count; i++)
            {
                if (table[i][intBinVar] == 1)
                {
                    rhs = table[i][table[i].Count - 1];
                    break;
                }
            }

            return rhs;
        }

        private bool IsVariableBasic(int intBinVar, List<List<double>> table)
        {
            bool isBasic = true;

            for (int i = 0; i < table.Count; i++)
            {
                int numberOfOnes = 0;

                if (table[i][intBinVar] == 1)
                    numberOfOnes++;

                if ((table[i][intBinVar] != 0 && table[i][intBinVar] != 1) || numberOfOnes > 1)
                {
                    isBasic = false;
                    break;
                }
            }

            return isBasic;
        }

        private int GetCutVariable(List<List<double>> table, List<int> intBinVarIndexes)
        {
            if (intBinVarIndexes.Count == 1)
                return intBinVarIndexes[0];

            int branchVariableIndex = -1;
            decimal smallestFractionalPart = 1;

            foreach (var intBinVar in intBinVarIndexes)
            {
                var rhs = (Decimal)GetRhsOfVariable(intBinVar, table);
                decimal fractionalPart = rhs - Math.Truncate(rhs);
                if (Math.Abs(0.5m - fractionalPart) < smallestFractionalPart)
                {
                    smallestFractionalPart = Math.Abs(0.5m - fractionalPart);
                    branchVariableIndex = intBinVar;
                }
            }

            return branchVariableIndex;
        }

        private int GetBasicRow(List<List<double>> table, int variableIndex)
        {
            int basicRow = -1;

            for (int i = 1; i < table.Count; i++)
            {
                if (table[i][variableIndex] == 1)
                {
                    basicRow = i;
                    break;
                }
            }

            return basicRow;
        }
    }
}
