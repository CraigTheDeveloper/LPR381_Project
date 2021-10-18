using BusinessLogic.Algorithms;
using Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Presentation
{
    public class ModelWriter
    {
        public static void WriteResultsToFile(Model model)
        {
            var fileName = Guid.NewGuid().ToString() + ".txt";

            using (FileStream stream = new FileStream(fileName, FileMode.Create, FileAccess.Write))
            {
                using (StreamWriter writer = new StreamWriter(stream))
                {
                    int iteration = 0;

                    foreach (var table in model.Result)
                    {
                        writer.WriteLine($"\n\nTable {iteration}:");

                        for (int i = 0; i < table.Count; i++)
                        {
                            for (int j = 0; j < table[0].Count; j++)
                            {
                                writer.Write($"\t{table[i][j]:0.###}");
                            }
                            writer.WriteLine();
                        }

                        iteration++;
                    }
                }
            }

            Console.WriteLine($"\n\nThe results have been written to the file: {fileName}");
        }

        public static void WriteResultsToFile(BranchAndBoundSimplex branchAndBoundSimplex)
        {
            var fileName = Guid.NewGuid().ToString() + ".txt";

            using (FileStream stream = new FileStream(fileName, FileMode.Create, FileAccess.Write))
            {
                using (StreamWriter writer = new StreamWriter(stream))
                {
                    var tree = branchAndBoundSimplex.Results;
                    WriteBranchResults(tree.Root, writer);
                    List<List<double>> bestCandidate = branchAndBoundSimplex.GetBestCandidate();

                    writer.WriteLine("\n\nThis is the best solution of all the candidates:\n");
                    for (int i = 0; i < bestCandidate.Count; i++)
                    {
                        for (int j = 0; j < bestCandidate[i].Count; j++)
                        {
                            writer.Write($"\t{bestCandidate[i][j]:0.###}");
                        }
                        writer.WriteLine();
                    }
                }
            }

            Console.WriteLine($"\n\nThe results have been written to the file: {fileName}");
        }

        private static void WriteBranchResults(BusinessLogic.BinaryTreeNode root, StreamWriter writer, string previousProblem = "0")
        {
            if (root == null)
                return;

            if (previousProblem.Equals("0"))
            {
                writer.WriteLine("\n\n");
                writer.WriteLine("Sub-Problem 0");

                for (int i = 0; i < root.Data.Count; i++)
                {
                    for (int j = 0; j < root.Data[i].Count; j++)
                    {
                        for (int k = 0; k < root.Data[i][j].Count; k++)
                        {
                            writer.Write($"\t{root.Data[i][j][k]:0.###}");
                        }
                        writer.WriteLine();
                    }
                    writer.WriteLine("\n\n");
                }

                WriteBranchResults(root.LeftNode, writer, "1");
                WriteBranchResults(root.RightNode, writer, "2");
            }
            else
            {
                writer.WriteLine("\n\n");
                writer.WriteLine($"Sub-Problem {previousProblem}");

                for (int i = 0; i < root.Data.Count; i++)
                {
                    for (int j = 0; j < root.Data[i].Count; j++)
                    {
                        for (int k = 0; k < root.Data[i][j].Count; k++)
                        {
                            writer.Write($"\t{root.Data[i][j][k]:0.###}");
                        }
                        writer.WriteLine();
                    }
                    writer.WriteLine("\n\n");
                }

                WriteBranchResults(root.LeftNode, writer, previousProblem + ".1");
                WriteBranchResults(root.RightNode, writer, previousProblem + ".2");
            }
        }
    }
}
