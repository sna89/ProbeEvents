using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UncertainEventStreams.Events
{
    public class PointEvent : IComparable, IComparable<PointEvent>
    {
        public enum PointType { A , B }

        public int EventIndex { get; set; }

        public int IntervalId { get; set; }

        public EventType Type { get; set; }

        public DateTime? Timestamp { get; set; }

        public PointType DataPointType { get; set; }

        public bool DeterministicKnown { get { return Timestamp.HasValue; } }

        public PointEvent(EventType type, DateTime? timestamp, int eventIndex, PointType pointType)
        {
            Type = type;
            Timestamp = timestamp;
            EventIndex = eventIndex;
            DataPointType = pointType;
        }


        public int CompareTo(object obj)
        {
            var other = (obj as PointEvent);
            if (other == null)
            {
                return 0;
            }

            return this.EventIndex - other.EventIndex;
        }

        public int CompareTo(PointEvent other)
        {
            return this.EventIndex - other.EventIndex;
        }
    }
}
