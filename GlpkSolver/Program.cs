using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GlpkSharp;
using System.IO;
using System.Diagnostics;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            int numberOfRuns = 500;
            for (int i = 1; i < numberOfRuns + 1; i++)
            {
                int[,] costs = CreateRandomArray(i + 4, i + 4, 1, 1000000, i);
                Solving(costs);
            }


        }
        private static void Log(string logMessage, TextWriter w)
        {
            w.WriteLine($"{DateTime.Now.ToLongTimeString()};{DateTime.Now.ToShortDateString()};{logMessage}");
        }

        private static void Solving(int[,] costs)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            int numWorkers = costs.GetLength(0); 
            int numTasks = costs.GetLength(1); 

            LPProblem l = new LPProblem();

            int numSpace = numWorkers * numTasks * 2 + 1; 

            int[] ia = new int[numSpace];
            int[] ja = new int[numSpace];
            double[] ar = new double[numSpace];

            int numRows = numTasks + numWorkers; //
            l.AddRows(numRows);

            for (int i = 1; i < numRows + 1; i++)
            {
                l.SetRowName(i, $"row{i}");
                l.SetRowBounds(i, BOUNDSTYPE.Fixed, 1, 1);
            }

            int numCols = numWorkers * numTasks;
            l.AddCols(numCols);

            for (int i = 0; i < numWorkers; i++)
            {
                for (int j = 0; j < numTasks; j++)
                {
                    int colIndex = i * numTasks + j + 1;
                    l.SetColName(colIndex, $"x{i+1}{j+1}");
                    l.SetColKind(colIndex, COLKIND.Binary);
                }
            }

            

            //Each worker is assigned to one task
            for (int i = 0; i < numWorkers; i++)
            {
                for (int j = 0; j < numTasks; j++)
                {
                    int index = i * numTasks + j + 1;
                    int row = i + 1;
                    ia[index] = row;
                    ja[index] = index;
                }
            }

            

            //each task is assigned to one worker
            for (int i = 0; i < numWorkers; i++)
            {
                for (int j = 0; j < numTasks; j++)
                {
                    int index = (i * numTasks + j + 1) + (numTasks * numWorkers);
                    int row = (i + 1) + numWorkers;
                    ia[index] = row;

                    int colIndex = j * numWorkers + i + 1;
                    ja[index] = colIndex;
                }
            }

            

            for (int i = 1; i < numSpace; i++)
            {
                ar[i] = 1;
            }

           

            for (int i = 0; i < numWorkers; i++)
            {
                for (int j = 0; j < numTasks; j++)
                {
                    int colIndex = i * numTasks + j + 1;
                    l.SetObjCoef(colIndex, costs[i, j]);
                }
            }

            

            l.LoadMatrix(ia, ja, ar);

            l.ObjectiveDirection = OptimisationDirection.MINIMISE;

            l.SolveSimplex();

            l.WriteCPLEX("proba.lp");
            l.WriteSol("res.txt");

            for (int i = 0; i < numCols; i++)
            {
                Console.WriteLine($"{l.GetColName(i+1)}:{l.GetColPrimal(i+1).ToString()}");
            }

            stopwatch.Stop();

            using (StreamWriter w = File.AppendText("log.txt"))
            {
                Log($"{stopwatch.ElapsedMilliseconds};{numTasks * numWorkers}", w);

            }

            Console.WriteLine($"Elapsed time is {stopwatch.ElapsedMilliseconds} ms");

            l.Destroy();

        }

        private static int[,] CreateRandomArray(int rows, int columns, int min, int max, int seed)
        {
            int[,] array = new int[rows, columns];

            Random rnd = new Random(seed);
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < columns; j++)
                {
                    array[i, j] = rnd.Next(min, max);
                }
            }
            return array;
        }
    }
}
