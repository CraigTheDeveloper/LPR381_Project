using Common;
using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogic.Algorithms
{
    public class BranchAndBoundSimplex : Algorithm
    {
        private Model model;
        public BinaryTree Results { get; set; } = new BinaryTree();
        private DualSimplex dualSimplex = new DualSimplex();
        private List<List<List<double>>> candidateSolutions = new List<List<List<double>>>();

        public override void PutModelInCanonicalForm(Model model)
        {
            dualSimplex.PutModelInCanonicalForm(model);
            Results.Add(model.Result);
        }

        public override void Solve(Model model)
        {
            this.model = model;

            int level = 1;
            while (level <= Results.GetHeight(Results.Root))
            {
                SolveCurrentLevel(Results.Root, level);
                level++;
            }
        }

        private void SolveCurrentLevel(BinaryTreeNode root, int level)
        {
            if (root == null)
                return;

            if (level == 1)
            {
                try
                {
                    Solve(root);
                    Branch(root);
                }
                catch (InfeasibleException)
                {
                    return;
                }
            }
            else if (level > 1)
            {
                SolveCurrentLevel(root.LeftNode, level - 1);
                SolveCurrentLevel(root.RightNode, level - 1);
            }
        }

        public List<List<double>> GetBestCandidate()
        {
            double bestRHS = candidateSolutions[0][0][candidateSolutions[0][0].Count - 1];
            List<List<double>> bestSolution = candidateSolutions[0];

            for (int i = 1; i < candidateSolutions.Count; i++)
            {
                if (model.ProblemType == ProblemType.Maximization)
                {
                    if (candidateSolutions[i][0][candidateSolutions[i][0].Count - 1] > bestRHS)
                    {
                        bestRHS = candidateSolutions[i][0][candidateSolutions[i][0].Count - 1];
                        bestSolution = candidateSolutions[i];
                    }
                }
                else
                {
                    if (candidateSolutions[i][0][candidateSolutions[i][0].Count - 1] < bestRHS)
                    {
                        bestRHS = candidateSolutions[i][0][candidateSolutions[i][0].Count - 1];
                        bestSolution = candidateSolutions[i];
                    }
                }
            }

            return bestSolution;
        }

        private void Solve(BinaryTreeNode root)
        {
            var model = new Model() { ProblemType = this.model.ProblemType, Result = root.Data };
            dualSimplex.Solve(model);
        }

        private void Branch(BinaryTreeNode root)
        {
            if (CanBranch(root).Count == 0)
            {
                candidateSolutions.Add(root.Data[root.Data.Count - 1]);
            }     
            else
            {
                // Get the variable we need to branch on
                int branchVariableIndex = GetBranchVariable(root.Data[root.Data.Count - 1], CanBranch(root));
                // Add the new constraints to the old table and add that resulting table to the binary tree
                AddSubProblems(root, branchVariableIndex);
            }
        }

        private void AddSubProblems(BinaryTreeNode root, int branchVariableIndex)
        {
            var table = root.Data[root.Data.Count - 1];
            double rhs = GetRhsOfVariable(branchVariableIndex, table);

            int constraintOneRhs;
            int constraintTwoRhs;

            constraintOneRhs = (int)Math.Truncate(rhs);
            constraintTwoRhs = constraintOneRhs + 1;

            var subProblemOneTable = ListCloner.CloneList(table);
            var subProblemTwoTable = ListCloner.CloneList(table);

            subProblemOneTable.Add(new List<double>());
            subProblemTwoTable.Add(new List<double>());

            for (int i = 0; i < subProblemOneTable[0].Count - 1; i++)
            {
                if (i == branchVariableIndex)
                {
                    subProblemOneTable[subProblemOneTable.Count - 1].Add(1);
                    subProblemTwoTable[subProblemTwoTable.Count - 1].Add(-1);
                }
                else
                {
                    subProblemOneTable[subProblemOneTable.Count - 1].Add(0);
                    subProblemTwoTable[subProblemTwoTable.Count - 1].Add(0);
                }
            }

            subProblemOneTable[subProblemOneTable.Count - 1].Add(constraintOneRhs);
            subProblemTwoTable[subProblemTwoTable.Count - 1].Add(constraintTwoRhs * -1);

            // Add a column with 1s and 0s for our slack/excess
            for (int i = 0; i < subProblemOneTable.Count; i++)
            {
                // We need to add the column before the RHS (last) column
                var tempOne = subProblemOneTable[i][subProblemOneTable[i].Count - 1];
                var tempTwo = subProblemTwoTable[i][subProblemTwoTable[i].Count - 1];

                if (i == subProblemOneTable.Count - 1)
                {
                    subProblemOneTable[i][subProblemOneTable[i].Count - 1] = 1;
                    subProblemTwoTable[i][subProblemTwoTable[i].Count - 1] = 1;
                }
                else
                {
                    subProblemOneTable[i][subProblemOneTable[i].Count - 1] = 0;
                    subProblemTwoTable[i][subProblemTwoTable[i].Count - 1] = 0;
                }

                subProblemOneTable[i].Add(tempOne);
                subProblemTwoTable[i].Add(tempTwo);
            }

            int subProblemBasicRow = GetBasicRow(table, branchVariableIndex);

            for (int i = 0; i < subProblemOneTable[subProblemBasicRow].Count; i++)
            {
                subProblemOneTable[subProblemOneTable.Count - 1][i] -= subProblemOneTable[subProblemBasicRow][i];
                subProblemTwoTable[subProblemTwoTable.Count - 1][i] += subProblemTwoTable[subProblemBasicRow][i];
            }

            Results.Add(new List<List<List<double>>>() { subProblemOneTable }, root.Data);
            Results.Add(new List<List<List<double>>>() { subProblemTwoTable }, root.Data);
        }

        private int GetBasicRow(List<List<double>> table, int branchVariableIndex)
        {
            int basicRow = -1;

            for (int i = 1; i < table.Count; i++)
            {
                if (table[i][branchVariableIndex] == 1)
                {
                    basicRow = i;
                    break;
                }
            }

            return basicRow;
        }

        private int GetBranchVariable(List<List<double>> table, List<int> intBinVarIndexes)
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

        private List<int> CanBranch(BinaryTreeNode root)
        {
            var intBinVarIndexes = new List<int>();
            var indexesToDiscard = new List<int>();

            for (int i = 0; i < model.SignRestrictions.Count; i++)
            {
                if (model.SignRestrictions[i] == SignRestriction.Integer || model.SignRestrictions[i] == SignRestriction.Binary)
                {
                    intBinVarIndexes.Add(i);
                }
            }

            // Check each int/bin variable to see if the sign restriction is violated

            var table = root.Data[root.Data.Count - 1];

            foreach (var intBinVar in intBinVarIndexes)
            {
                // First check if the variable is non-basic (which means it = 0 and int/bin restriction is satisfied)
                if (!IsVariableBasic(intBinVar, table))
                {
                    indexesToDiscard.Add(intBinVar);
                }
                // If it is basic, make sure the RHS is not already an integer
                else
                {
                    double rhs = GetRhsOfVariable(intBinVar, table);

                    if (rhs - Math.Truncate(rhs) < 0.00001)
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
    }
}
