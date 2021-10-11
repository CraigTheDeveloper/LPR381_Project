using Common;
using Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess
{
    public class ModelReader
    {
        public static Model ReadModelFromFile(string path)
        {
            List<string> lines = new List<string>();

            try
            {
                using (FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    using (StreamReader reader = new StreamReader(fileStream))
                    {
                        while (!reader.EndOfStream)
                        {
                            lines.Add(reader.ReadLine());
                        }
                    }
                }

                return ConvertToModel(lines);
            }
            catch (FileNotFoundException)
            {
                throw new CustomException("Could not find the specified file");
            }
            catch (DirectoryNotFoundException)
            {
                throw new CustomException("Could not find the specified directory");
            }
            catch (ArgumentException)
            {
                throw new CustomException("The specified path was invalid");
            }
            catch (IOException)
            {
                throw new CustomException("There was an error reading the specified file");
            }
        }

        private static Model ConvertToModel(List<string> lines)
        {
            Model model = new Model();

            try
            {
                if (lines.Count < 1)
                {
                    throw new CustomException("The specified file is empty");
                }

                string[] objectiveFunction = lines[0].Split(' ');

                if (!(objectiveFunction[0].ToLower().Equals("max") || objectiveFunction[0].ToLower().Equals("min")))
                {
                    throw new CustomException("The given problem does not specify whether to maximize or minimize the objective function");
                }

                model.ProblemType = objectiveFunction[0].ToLower().Equals("max") ? ProblemType.Maximization : ProblemType.Minimization;

                for (int i = 1; i < objectiveFunction.Length; i++)
                {
                    model.ObjectiveFunction.DecisionVariables.Add(new DecisionVariable() { Coefficient = double.Parse(objectiveFunction[i]) });
                }

                if (lines.Count < 2)
                {
                    throw new CustomException("The given problem does not contain any constraints");
                }

                for (int i = 1; i < lines.Count - 1; i++)
                {
                    string[] constraintArr = lines[i].Split(' ');
                    Constraint constraint = new Constraint();

                    for (int j = 0; j < constraintArr.Length - 2; j++)
                    {
                        constraint.DecisionVariables.Add(new DecisionVariable() { Coefficient = double.Parse(constraintArr[j]) });
                    }

                    switch (constraintArr[constraintArr.Length - 2])
                    {
                        case "=":
                            {
                                constraint.InequalitySign = InequalitySign.EqualTo;
                            }
                            break;
                        case "<=":
                            {
                                constraint.InequalitySign = InequalitySign.LessThanOrEqualTo;
                            }
                            break;
                        case ">=":
                            {
                                constraint.InequalitySign = InequalitySign.GreaterThanOrEqualTo;
                            }
                            break;
                        default:
                            {
                                throw new CustomException($"Constraint {model.Constraints.Count + 1} does not have a valid inequality symbol");
                            }
                    }

                    constraint.RightHandSide = double.Parse(constraintArr[constraintArr.Length - 1]);
                    model.Constraints.Add(constraint);
                }

                string[] signRestrictions = lines[lines.Count - 1].Split(' ');

                foreach (var restriction in signRestrictions)
                {
                    switch (restriction.ToLower())
                    {
                        case "+":
                            {
                                model.SignRestrictions.Add(SignRestriction.Positive);
                            }
                            break;
                        case "-":
                            {
                                model.SignRestrictions.Add(SignRestriction.Negative);
                            }
                            break;
                        case "urs":
                            {
                                model.SignRestrictions.Add(SignRestriction.Unrestricted);
                            }
                            break;
                        case "int":
                            {
                                model.SignRestrictions.Add(SignRestriction.Integer);
                            }
                            break;
                        case "bin":
                            {
                                model.SignRestrictions.Add(SignRestriction.Binary);
                            }
                            break;
                        default:
                            {
                                throw new CustomException("Invalid sign restriction found");
                            }
                    }
                }
            }
            catch (FormatException)
            {
                throw new CustomException("One of the decision variables has an invalid coefficient");
            }

            return model;
        }
    }
}
