using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Google.OrTools;
using Google.OrTools.LinearSolver;

//stopwatch: https://www.techiedelight.com/measure-execution-time-csharp/

namespace OrToolsSolver
{
    class Program
    {
        static void Main(string[] args)
        {
            int numberOfRuns = 500;
            int minCost = 1;
            int maxCost = 1000000;
            for (int i = 1; i < numberOfRuns + 1; i++)
            {
                int[,] costs = CreateRandomArray(i+4,i+4,minCost,maxCost,i);
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

            // Solver.
            Solver solver = Solver.CreateSolver("SCIP");

            // Variables.
            // x[i, j] is an array of 0-1 variables, which will be 1
            // if worker i is assigned to task j.
            Variable[,] x = new Variable[numWorkers, numTasks];
            for (int i = 0; i < numWorkers; ++i)
            {
                for (int j = 0; j < numTasks; ++j)
                {
                    x[i, j] = solver.MakeIntVar(0, 1, $"worker_{i}_task_{j}");
                }
            }

            // Constraints
            // Each worker is assigned to at most one task.
            for (int i = 0; i < numWorkers; ++i)
            {
                Constraint constraint = solver.MakeConstraint(0, 1, "");
                for (int j = 0; j < numTasks; ++j)
                {
                    constraint.SetCoefficient(x[i, j], 1);
                }
            }
            // Each task is assigned to exactly one worker.
            for (int j = 0; j < numTasks; ++j)
            {
                Constraint constraint = solver.MakeConstraint(1, 1, "");
                for (int i = 0; i < numWorkers; ++i)
                {
                    constraint.SetCoefficient(x[i, j], 1);
                }
            }

            // Objective
            Objective objective = solver.Objective();
            for (int i = 0; i < numWorkers; ++i)
            {
                for (int j = 0; j < numTasks; ++j)
                {
                    objective.SetCoefficient(x[i, j], costs[i, j]);
                }
            }
            objective.SetMinimization();

            // Solve
            Solver.ResultStatus resultStatus = solver.Solve();

            // Print solution.
            // Check that the problem has a feasible solution.

            if (resultStatus == Solver.ResultStatus.OPTIMAL || resultStatus == Solver.ResultStatus.FEASIBLE)
            {

                Console.WriteLine($"Total cost: {solver.Objective().Value()}\n");
                for (int i = 0; i < numWorkers; ++i)
                {
                    for (int j = 0; j < numTasks; ++j)
                    {
                        // Test if x[i, j] is 0 or 1 (with tolerance for floating point
                        // arithmetic).
                        if (x[i, j].SolutionValue() > 0.5)
                        {
                            Console.WriteLine($"Worker {i + 1} assigned to task {j + 1}. Cost: {costs[i, j]}");
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine("No solution found.");
            }

            stopwatch.Stop();

            //Console.WriteLine(solver.ExportModelAsLpFormat(false).Replace(@"\\", "").Replace(",_", ","));
            using (StreamWriter w = File.AppendText("log.txt"))
            {
                Log($"{stopwatch.ElapsedMilliseconds};{numTasks * numWorkers}", w);

            }
            Console.WriteLine($"Elapsed time is {stopwatch.ElapsedMilliseconds} ms");
            solver.Dispose();

        }

        private static int[,] CreateRandomArray(int rows, int columns, int min, int max, int seed)
        {
            int[,] array = new int[rows,columns];

            Random rnd = new Random(seed);
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < columns; j++)
                {
                    array[i, j] = rnd.Next(min,max);
                }
            }
            return array;
        }
    }
}
