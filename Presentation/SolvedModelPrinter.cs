using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Presentation
{
    public class SolvedModelPrinter
    {
        public static void Print(Model model)
        {
            int iteration = 0;

            foreach (var table in model.Result)
            {
                Console.WriteLine($"\n\nTable {iteration}:");

                for (int i = 0; i < table.Count; i++)
                {
                    for (int j = 0; j < table[0].Count; j++)
                    {
                        Console.Write($"\t\t{table[i][j]:0.###}");
                    }
                    Console.WriteLine();
                }

                iteration++;
            }
        }
    }
}
