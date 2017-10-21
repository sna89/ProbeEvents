using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UncertainEventStreams.Entities;

namespace UncertainEventStreams.Inference
{
    public class ComparisonInfo
    {
        public JourneyKey First { get; set; }
        public JourneyKey Second { get; set; }
        public int StopId { get; set; }

        public ComparisonInfo(JourneyKey first, JourneyKey second, int stop)
        {
            First = first;
            Second = second;
            StopId = stop;
        }
    }
}
