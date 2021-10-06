using BusinessLogic.Algorithms;
using Common;
using DataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogic
{
    public class ModelSolver
    {
        public static void Solve(Model model, Algorithm algorithm)
        {
            // Put the model in canonical form if it is not already
            algorithm.PutModelInCanonicalForm(model);

            // Solve the model using the chosen algorithm
            algorithm.Solve(model);

            // Write the results to a text file
            //ModelWriter.WriteResultsToFile(model);

            Console.Clear();
            Console.WriteLine("Here is the solution:");
            Console.WriteLine("=========================================================================================================");
        }
    }
}
