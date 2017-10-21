using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UncertainEventStreams.Events;
using UncertainEventStreams.Events.IntervalBased;

namespace UncertainEventStreams.Entities
{
    public class Journey
    {
        public JourneyKey Key { get; set; }

        public SegmentedIntervalEvent Intervals { get; set; }

        public Journey(JourneyKey key, SegmentedIntervalEvent interval)
        {
            Key = key;
            Intervals = interval;
        }
    }
}
