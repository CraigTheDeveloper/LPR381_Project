using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogic
{
    public class ListCloner
    {
        public static List<List<List<double>>> CloneList(List<List<List<double>>> oldList)
        {
            List<List<List<double>>> newList = new List<List<List<double>>>();

            int iterationCount = oldList.Count;
            int rowCount = oldList[0].Count;
            int colCount = oldList[0][0].Count;

            for (int i = 0; i < iterationCount; i++)
            {
                var table = new List<List<double>>();
                for (int j = 0; j < rowCount; j++)
                {
                    var row = new List<double>();
                    for (int k = 0; k < colCount; k++)
                    {
                        row.Add(oldList[i][j][k]);
                    }
                    table.Add(row);
                }
                newList.Add(table);
            }

            return newList;
        }

        public static List<List<double>> CloneList(List<List<double>> oldList)
        {
            List<List<double>> newList = new List<List<double>>();

            int rowCount = oldList.Count;
            int colCount = oldList[0].Count;

            for (int i = 0; i < rowCount; i++)
            {
                var newRow = new List<double>();
                for (int j = 0; j < colCount; j++)
                {
                    newRow.Add(oldList[i][j]);
                }
                newList.Add(newRow);
            }

            return newList;
        }
    }
}
