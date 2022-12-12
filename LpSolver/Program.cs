using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using LpSolveDotNet;

namespace parosito_lpsolve_dotnetcore
{
    class Program
    {
        /// <summary>
        /// This class demonstrates how to reproduce the example model from http://lpsolve.sourceforge.net/5.5/formulate.htm#CS.NET
        /// using LpSolveDotNet.
        /// </summary>
        /// 
      
        
        internal class FormulateSample
        {
            public static void Main()
            {
                LpSolve.Init();
                int numberOfRuns = 700;
                int minCost = 1;
                int maxCost = 1000000;

                for (int i = 1; i < numberOfRuns + 1; i++)
                {
                    int[,] costs = CreateRandomArray(i+4, i+4, minCost, maxCost,i);

                    Solving(costs);
                }

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


            private static int Solving(int[,] costs)
            {
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                int numWorkers = costs.GetLength(0);
                int numTasks = costs.GetLength(1);
                int numCols = numWorkers*numTasks; // there are [number of workers]*[number of tasks] variables in the model 

                using (LpSolve lp = LpSolve.make_lp(0, numCols))
                {
                    if (lp == null)
                    {
                        return 1; // couldn't construct a new model...
                    }


                    //create space large enough for one row
                    int[] colno = new int[numCols];
                    double[] row = new double[numCols+1];

                    //let us name our variables
                    for (int i = 0; i < numWorkers; i++)
                    {
                        for (int j = 0; j < numTasks; j++)
                        {
                            int colIndex = i * numTasks + j + 1;
                            lp.set_col_name(colIndex, $"worker_{i+1}_task_{j+1}");
                            
                        }
                    }
                    // makes building the model faster if it is done rows by row
                    lp.set_add_rowmode(true);
                    for(int i = 0;i< numWorkers; i++)
                    {
                        for (int j = 0; j < numTasks; j++)
                        {
                            int colIndex = i * numTasks + j + 1;
                            colno[j] = colIndex; // column index
                            row[j] = 1;          //column value

                        
                        }
                        if (lp.add_constraintex(numTasks, row, colno, lpsolve_constr_types.EQ, 1) == false)
                        {
                            return 3;
                        }

                    }
                    for (int i = 0; i < numWorkers; i++)
                    {
                        for (int k = 0; k < numTasks; k++)
                        {

                            colno[k] = i + k * numTasks+1; // columnIndex
                            row[k] = 1; //column value

                        }
                        if (lp.add_constraintex(numWorkers, row, colno, lpsolve_constr_types.EQ, 1) == false)
                        {
                            return 3;
                        }
                    }

          

                    //rowmode should be turned off again when done building the model
                    lp.set_add_rowmode(false);

                    //set the objective function 

                    for (int i = 0; i < numWorkers; i++)
                    {
                        for (int j = 0; j < numTasks; j++)
                        {
                            int colIndex = i * numTasks + j;
                            colno[colIndex] = colIndex+1;
                            row[colIndex] = costs[i, j];
                        }
                    }
                    

                    

                    if (lp.set_obj_fnex(numCols, row, colno) == false)
                    {
                        return 4;
                    }

                    // set the object direction to maximize
                    lp.set_minim();

                    //write the model in a file: lp.write_lp("model.lp");

                    // I only want to see important messages on screen while solving
                    lp.set_verbose(lpsolve_verbosity.IMPORTANT);
                    lp.write_lp("model.lp");
                    // Now let lpsolve calculate a solution
                    lpsolve_return s = lp.solve();
                    if (s != lpsolve_return.OPTIMAL)
                    {
                        return 5;
                    }
                    // a solution is calculated, now lets get some results

                    // objective value
                    Console.WriteLine("Objective value: " + lp.get_objective());

                    // variable values
                    lp.get_variables(row); 
                    for (int j = 0; j < numCols; j++)
                    {
                        if (row[j] > 0.5)
                        {
                            Console.WriteLine(lp.get_col_name(j + 1) + ": " + row[j]);

                        }
                    }
                    lp.delete_lp();
                }
                
                stopwatch.Stop();

                using (StreamWriter w = File.AppendText("log.txt"))
                {
                    Log($"{stopwatch.ElapsedMilliseconds};{numTasks * numWorkers}", w);

                }

                Console.WriteLine($"Elapsed time is {stopwatch.ElapsedMilliseconds} ms");
                
                return 0;
            } 
            private static void Log(string logMessage, TextWriter w)
            {
                w.WriteLine($"{DateTime.Now.ToLongTimeString()};{DateTime.Now.ToShortDateString()};{logMessage}");
            }

        }
    }
}
