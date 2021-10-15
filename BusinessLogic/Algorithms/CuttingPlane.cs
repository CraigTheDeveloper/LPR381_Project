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

        // As other algorithms don't take integer requirements into account, said requirements
        // must be changed temporarily to a >= 0 rqeuirement, to ensure accurate results.


        private List<int> intConstraints = new List<int>(); // Used to remember the 'int' constraints
        private int totalDicisionVariables = 0;
        public override void PutModelInCanonicalForm(Model model)
        {
            Algorithm chosenAlgo;

            // Changing integer restrictions into positive restrictions
            for (int i = 0; i < model.SignRestrictions.Count(); i++)
			{
                if (model.SignRestrictions[i] == SignRestriction.Integer)
                {
                    intConstraints.Add(i);
                    model.SignRestrictions[i] = SignRestriction.Positive;
                }
            }

            // Choosing which algorithm will solve partially
            if (model.ProblemType == ProblemType.Minimization)
            {
                chosenAlgo = new DualSimplex();
            }
            else
            {
                if (!(model.Constraints.Any(c => c.InequalitySign == InequalitySign.EqualTo) ||
                    model.Constraints.Any(c => c.InequalitySign == InequalitySign.GreaterThanOrEqualTo)))
                {
                    chosenAlgo = new PrimalSimplex();
                }
                else
                {
                    chosenAlgo = new DualSimplex();
                }
            }

            chosenAlgo.PutModelInCanonicalForm(model);
            chosenAlgo.Solve(model);
        }

        public override void Solve(Model model)
        {
            totalDicisionVariables = model.ObjectiveFunction.DecisionVariables.Count();
            List<List<double>> previousTable = new List<List<double>>(model.Result[model.Result.Count - 1]);



            int targetRow = GetTargetRow(model, previousTable);// 0 if no row
            List<List<double>> newTable = AddNewRow(previousTable, previousTable[targetRow]);
            model.Result.Add(newTable);

            DualSimplex test = new DualSimplex();
            test.Iterate(model);
        }

        // Finding which constraint must be targetted first/next
        private int GetTargetRow(Model model, List<List<double>> previousTable)
        {
            int targetRow = 0;
            List<double> remainders = new List<double>();
            List<double> remainderRatios = new List<double>();

			// Getting RHS remainders
			for (int i = 1; i < previousTable.Count; i++)
			{
                // Only targetting rows with Int Constraints, or newly created rows
				if (intConstraints.Contains(i-1) || i > model.Constraints.Count)
				{
                    double rowRHS = previousTable[i][previousTable[0].Count() - 1];
                    remainders.Add(Math.Abs(rowRHS) - Math.Abs(Math.Truncate(rowRHS)));
                }
			}

            // Finding how far the RHS remainders are from 0,5
			foreach (var remainder in remainders)
			{
                remainderRatios.Add(Math.Abs(0.5-remainder));
			}

            // Choosing a target row based on the remainder closest to 0.5 ,or left most x if
            // there are similar ratios
            List<double> sortedRatios = remainderRatios;
            sortedRatios.Sort();

            // If the first two ratios(when sorted) are the same, it means there are multiple RHS values
            // equally close to 0,5
            if (sortedRatios[0] == sortedRatios[1])
			{
                targetRow = FindLeftMostX(previousTable, remainderRatios, sortedRatios);
			}
			else
			{
                for (int i = 0; i < remainderRatios.Count; i++)
                {
                    if (remainderRatios[i] == sortedRatios[0])
                    {
                        targetRow = intConstraints[i+1];
                    }
                }
            }

            return targetRow; // 0 is returned if no row is found
        }

        private int FindLeftMostX(List<List<double>> previousTable, List<double> remainderRatios, List<double> sortedRatios)
		{
            
			for (int i = 0; i < totalDicisionVariables; i++)//columns
			{
				for (int j = 1; j < previousTable.Count; j++)//rows
				{
                    if (previousTable[j][i] == 1 && remainderRatios[j-1] == sortedRatios[0])
					{
                        return j;
					}
				}
            }
            return 0;
		}
        
        // Creating a new row based on the Target Row
        private List<List<double>> AddNewRow(List<List<double>> previousTable, List<double> targetRow)
		{
            List<List<double>> newTable = new List<List<double>>();
            for (int i = 0; i < previousTable.Count; i++)
            {
                newTable.Add(new List<double>());

                for (int j = 0; j < previousTable[i].Count; j++)
                {
                    newTable[i].Add(previousTable[i][j]);
                }
            }

            List<double> newRow = new List<double>();
            int newColumnLocation = targetRow.Count - 1;

            // Getting remainders of the target constraints
            for (int i = 0; i < targetRow.Count; i++)
			{
				if (i == newColumnLocation)
				{
                    // Adding new column for new row
                    newRow.Add(1);
                }
                double constraintVar = targetRow[i];
                double roundedValue = Math.Floor(constraintVar);

                // Moving lef-hand side values to the right
                newRow.Add(Math.Abs(constraintVar - roundedValue) * -1);
            }

            // Adding new column values to previous constraints
            foreach (var row in newTable)
			{
                row.Insert(newColumnLocation, 0);
			}
            newTable.Add(newRow);

            return newTable;
		}

        /*
         *  TO DO:
         *  Choose a row -COMPLETE!
         *  Get rounding new row, add to new table -99%
         *  Looping Algorithm - INCOMPLETE
         * 
         * Change Methods to only require Model instead of both model and table
         */
    }
}
