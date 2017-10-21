using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UncertainEventStreams.Events
{
    public class BasicIntervalEvent : IComparable, IComparable<BasicIntervalEvent>
    {
        public int IntervalIndex { get; private set; }
        public int IntervalId { get; private set; }
        public SortedSet<PointEvent> Events { get; set; }

        public BasicIntervalEvent(IEnumerable<PointEvent> events, int intervalId, int intervalIndex)
        {
            Events = new SortedSet<PointEvent>(events.OrderBy(x=>x.EventIndex));
            IntervalId = intervalId;
            IntervalIndex = intervalIndex;
        }

        public int CompareTo(object obj)
        {
            var other = (obj as BasicIntervalEvent);
            if (other == null)
            {
                return 0;
            }


            return this.IntervalIndex - other.IntervalIndex;
        }

        public int CompareTo(BasicIntervalEvent other)
        {
            return this.IntervalIndex - other.IntervalIndex;
        }
    }
}
