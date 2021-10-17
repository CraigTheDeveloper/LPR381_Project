using BusinessLogic;
using BusinessLogic.Algorithms;
using Common;
using DataAccess;
using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Presentation
{
    public class MainMenu
    {
        public static void Run()
        {
            Model model = new Model();

            try
            {
                Console.Clear();
                Console.WriteLine("Welcome to the solver");
                Console.WriteLine("=========================================================================================================");
                Console.WriteLine("To get started, choose a premade text file that contains the model you would like to solve:");
                Console.WriteLine("\nlimpopo.txt\nsanta.txt\nkorean.txt\nfarmer.txt\nbranch.txt\ncut.txt\n");
                string modelPath = Console.ReadLine();
                Console.WriteLine("=========================================================================================================");
                model = ModelReader.ReadModelFromFile(modelPath);
                SolveModelUsingAlgorithm(model);
            }
            catch (CustomException ex)
            {
                Console.WriteLine($"There was an error. Details: {ex.Message}.");
                Console.WriteLine("\nHere are the tables that were calculated before we ran into that error:");
                SolvedModelPrinter.Print(model);
                Console.WriteLine("\n\nPress any key to continue. . .");
                Console.ReadKey();
                Run();
            }
            catch (InfeasibleException ex)
            {
                Console.WriteLine($"There was an error. Details: {ex.Message}.");
                Console.WriteLine("\nHere are the tables that were calculated before we ran into that error:");
                SolvedModelPrinter.Print(model);
                Console.WriteLine("\n\nPress any key to continue. . .");
                Console.ReadKey();
                Run();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"There was an error. Details: {ex.Message}.");
                Console.WriteLine("\nHere are the tables that were calculated before we ran into that error:");
                SolvedModelPrinter.Print(model);
                Console.WriteLine("\n\nPress any key to continue. . .");
                Console.ReadKey();
                Run();
            }
        }

        private static void SolveModelUsingAlgorithm(Model model)
        {
            Console.Clear();
            Console.WriteLine("Great! Now that we have your model, the next step is to choose an algorithm to solve it with.");
            Console.WriteLine("Based on factors in your model, these are the available algorithms you can use:");
            Console.WriteLine("=========================================================================================================");

            string userInput;
            Algorithm algorithm;

            // First check if we have any integer/binary sign restrictions - which means we would have to use either branch and bound or cutting plane
            if (model.SignRestrictions.Contains(SignRestriction.Binary) || model.SignRestrictions.Contains(SignRestriction.Integer))
            {
                Console.WriteLine("BNB - Branch and Bound Simplex Algorithm");
                Console.WriteLine("CP - Cutting Plane Algorithm");
                userInput = Console.ReadLine();

                switch (userInput.ToUpper())
                {
                    case "BNB":
                        {
                            algorithm = new BranchAndBoundSimplex();
                        }
                        break;
                    case "CP":
                        {
                            algorithm = new CuttingPlane();
                        }
                        break;
                    default:
                        {
                            throw new CustomException("Invalid selection made");
                        }
                }
            }
            else if (model.ProblemType == ProblemType.Minimization)
            {
                Console.WriteLine("DS - Dual Simplex Algorithm");
                Console.WriteLine("TPS - Two Phase Simplex Algorithm");
                userInput = Console.ReadLine();

                switch (userInput.ToUpper())
                {
                    case "DS":
                        {
                            algorithm = new DualSimplex();
                        }
                        break;
                    case "TPS":
                        {
                            algorithm = new TwoPhaseSimplex();
                        }
                        break;
                    default:
                        {
                            throw new CustomException("Invalid selection made");
                        }
                }
            }
            // If we reach this point, our problem is a linear max problem
            else
            {
                // If we have a normal max problem, we must use Primal Simplex to solve
                if (!(model.Constraints.Any(c => c.InequalitySign == InequalitySign.EqualTo) ||
                    model.Constraints.Any(c => c.InequalitySign == InequalitySign.GreaterThanOrEqualTo)))
                {
                    Console.WriteLine("PS - Primal Simplex");
                    userInput = Console.ReadLine();

                    switch (userInput.ToUpper())
                    {
                        case "PS":
                            {
                                algorithm = new PrimalSimplex();
                            }
                            break;
                        default:
                            {
                                throw new CustomException("Invalid selection made");
                            }
                    }
                }
                // Otherwise we have a non-normal max problem, and we must use either Dual Simplex or Two Phase Simplex to solve
                else
                {
                    Console.WriteLine("DS - Dual Simplex Algorithm");
                    Console.WriteLine("TPS - Two Phase Simplex Algorithm");
                    userInput = Console.ReadLine();

                    switch (userInput.ToUpper())
                    {
                        case "DS":
                            {
                                algorithm = new DualSimplex();
                            }
                            break;
                        case "TPS":
                            {
                                algorithm = new TwoPhaseSimplex();
                            }
                            break;
                        default:
                            {
                                throw new CustomException("Invalid selection made");
                            }
                    }
                }
            }

            ModelSolver.Solve(model, algorithm);
            SolvedModelPrinter.Print(model);
        }
    }
}
