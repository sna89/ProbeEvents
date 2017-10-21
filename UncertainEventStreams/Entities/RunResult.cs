using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UncertainEventStreams.Entities
{
    public class RunResult
    {
        public double Probability { get; set; }

        public TimeSpan Runtime { get; set; }

        public int MissingEvents { get; set; }
        public int Pruned { get; set; }

        public RunResult(double probability, TimeSpan runtime, int missingEvents)
        {
            Probability = probability;
            Runtime = runtime;
            MissingEvents = missingEvents;
        }
    }
}
