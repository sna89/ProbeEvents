using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UncertainEventStreams.Events.IntervalBased
{
    public class SegmentedIntervalEvent
    {
        public string Key { get; private set; }

        public SortedSet<BasicIntervalEvent> Segments { get; set; }

        public SegmentedIntervalEvent(IEnumerable<BasicIntervalEvent> segments, string key)
        {
            Segments = new SortedSet<BasicIntervalEvent>(segments.OrderBy(x => x.IntervalIndex));
            Key = key;
        }

        /// <summary>
        /// Checks that alll conditions for segmented interval events are satisfied
        /// </summary>
        /// <returns></returns>
        public bool Validate()
        {
            //1. All segments are BasicIntervalEvent by definition.

            //2. first segment, first point event is start
            if (!(Segments.Min.Events.Min.Type == EventType.Start))
            {
                Console.WriteLine("Key: {0}, Segment: {1} is not valid. first event isn't start event (event type: {2})", 
                    Key, Segments.Min.IntervalIndex, Segments.Min.Events.Min.Type);
                return false;
            }

            //3. first segment, first point event is start
            if (!(Segments.Max.Events.Max.Type == EventType.End))
            {
                Console.WriteLine("Key: {0}, Segment: {1} is not valid. last event isn't end event (event type: {2})",
                    Key, Segments.Max.IntervalIndex, Segments.Max.Events.Max.Type);
                return false;
            }

            foreach (var seg in Segments)
            {
                //4. All last point events (except last interval) are suspend events
                if (seg != Segments.Max && seg.Events.Max.Type != EventType.Suspend)
                {
                    Console.WriteLine("Key: {0}, Segment: {1} is not valid. last event isn't suspend event (event type: {2})",
                        Key, seg.IntervalIndex, seg.Events.Max.Type);
                    return false;
                }

                //5. All first point events (except first interval) are resume events
                if (false) //seg != Segments.Min && (seg.Events.Min.Type != EventType.Resume && seg.Events.Min.Type != EventType.NotActive))
                {
                    Console.WriteLine("Key: {0}, Segment: {1} is not valid. first event isn't resume event (event type: {2})",
                        Key, seg.IntervalIndex, seg.Events.Min.Type);
                    return false;
                }

                //6. All intermidiate events are active events
                //var pe = seg.Events.Except(new PointEvent[] { seg.Events.Min, seg.Events.Max }).FirstOrDefault(x => x.Type != EventType.Active && x.Type != EventType.NotActive);
                //if (pe != null)
                //{
                //    Console.WriteLine("Key: {0}, Segment: {1} is not valid. intermidiate events isn't active event (event type: {2})",
                //        Key, seg.IntervalIndex, pe.Type);
                //    return false;

                //}
            }

            //7. Overlap between intervals 
            var overlap = Segments
                .Join(Segments, x => x.IntervalIndex + 1, y => y.IntervalIndex, (x, y) => new { x.Events.Max, y.Events.Min })
                .Where(x=> x.Max.Timestamp.HasValue && x.Min.Timestamp.HasValue)
                .Where(x => x.Max.Timestamp > x.Min.Timestamp)
                .FirstOrDefault();
            if(overlap != null)
            {
                Console.WriteLine("Key: {0}, Segment: {1} is overlapping with segment: {2}",
                    Key, overlap.Max.EventIndex, overlap.Min.EventIndex);
                return false;
            }

            var events = Segments.SelectMany(x => x.Events).Where(x=>x.Timestamp.HasValue);            
            if(!CheckOrdering(events.OrderBy(x => x.Timestamp), events.OrderBy(x=>x.EventIndex)))
            {
                //Write to log
                return false;
            }

            return true;
        }

        private bool CheckOrdering(IOrderedEnumerable<PointEvent> orderedEnumerable1, IOrderedEnumerable<PointEvent> orderedEnumerable2)
        {
            var it = orderedEnumerable2.GetEnumerator();
            it.MoveNext();

            foreach (var first in orderedEnumerable1)
            {
                if (first != it.Current)
                {
                    return false;
                }

                it.MoveNext();
            }

            return true;
        }

        public IEnumerable<PointEvent> GetEventStream(int stop)
        {        
            var resume = Segments.First(x => x.IntervalId == stop).Events.Single(x=>x.Type == EventType.Resume).EventIndex;

            var upperBound = Segments.SelectMany(x => x.Events).Where(x => x.EventIndex > resume && x.Timestamp.HasValue)
                .OrderBy(x => x.Timestamp).Select(x=>x.EventIndex).First();

            var lowerBound = Segments.SelectMany(x => x.Events).Where(x => x.EventIndex < resume && x.Timestamp.HasValue && x.Type != EventType.NotActive)
                .OrderByDescending(x => x.Timestamp).Select(x => x.EventIndex).First();


            return Segments
            .OrderBy(x => x.IntervalIndex)
            .SelectMany(x => x.Events)
            .Where(x => x.EventIndex >= lowerBound && x.EventIndex <= upperBound)
            .OrderBy(x => x.EventIndex)
            .Select((x, i) =>
            {
                x.EventIndex = i;
                return x;
            });
        }
    }
}
