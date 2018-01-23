using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UncertainEventStreams.Entities;
using UncertainEventStreams.Inference;
using UncertainEventStreams.Preprocessing;
using UncertainEventStreams.Preprocessing.Tasks;

namespace UncertainEventStreams
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                PreprocessTask preTask = new PreprocessTask();
                preTask.Run();

                #region run inference

                var inference = new InferenceHelper();
                var helper = new EventLogStore();
                var journeys = helper.JourneysToCompare();
                foreach (var journey in journeys)
                {
                    try
                    {

                        inference.GetOverlapProbability(journey.First, journey.Second, journey.StopId);
                    }
                    catch (Exception ex)
                    {
                    }
                }

                #endregion
                ////var FileImport = new FileImport();
                ////FileImport.Load();
                return;




                //new PreprocessTask().Run();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed during run, exception: {0}", ex);               
            }
            finally
            {
                Console.WriteLine("Press any key to terminate...");
                Console.ReadKey();
            }
        }
    }
}
