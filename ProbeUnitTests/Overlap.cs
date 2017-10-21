using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UncertainEventStreams.Inference;
using UncertainEventStreams.Preprocessing;
using UncertainEventStreams.Entities;
using UncertainEventStreams.Algorithms.RelationSatisfaction;
using UncertainEventStreams.Events.IntervalBased;
using UncertainEventStreams.Events;

namespace ProbeUnitTests
{
    [TestClass]
    public class Overlap
    {
        [TestMethod]
        public void TestMethod1()
        {
            var stopId = 1111;
            var inference = new InferenceHelper();
            var c = new ComparisonInfo(new JourneyKey("A", 123), new JourneyKey("B", 456), stopId);
            var algorithm = new RelationSatisfaction();

            #region First event

            var e11 = new BasicIntervalEvent(new PointEvent[]
            {
                new PointEvent(EventType.Start, DateTime.Parse("2016-01-01 8:00"), 1, PointEvent.PointType.A),
                new PointEvent(EventType.Active, DateTime.Parse("2016-01-01 8:20"), 2, PointEvent.PointType.A),
                new PointEvent(EventType.Suspend, null, 3, PointEvent.PointType.A)
            }, 1, 1);

            var e12 = new BasicIntervalEvent(new PointEvent[]
            {
                new PointEvent(EventType.Resume, null, 4, PointEvent.PointType.A),
                new PointEvent(EventType.Active, DateTime.Parse("2016-01-01 8:40"), 5, PointEvent.PointType.A),
                new PointEvent(EventType.End, DateTime.Parse("2016-01-01 9:00"), 6, PointEvent.PointType.A)
            }, stopId, 2);

            var interval1 = new SegmentedIntervalEvent(new BasicIntervalEvent[] { e11, e12 }, c.First.ToString());

            #endregion

            #region Second event

            var e21 = new BasicIntervalEvent(new PointEvent[]
            {
                new PointEvent(EventType.Start, DateTime.Parse("2016-01-01 8:00").AddMilliseconds(10), 1, PointEvent.PointType.B),
                new PointEvent(EventType.Active, DateTime.Parse("2016-01-01 8:20").AddMilliseconds(10), 2, PointEvent.PointType.B),
                new PointEvent(EventType.Suspend, null, 3, PointEvent.PointType.B)
            }, 1, 1);

            var e22 = new BasicIntervalEvent(new PointEvent[]
            {
                new PointEvent(EventType.Resume, null, 4, PointEvent.PointType.B),
                new PointEvent(EventType.Active, DateTime.Parse("2016-01-01 8:40").AddMilliseconds(10), 5, PointEvent.PointType.B),
                new PointEvent(EventType.End, DateTime.Parse("2016-01-01 9:00").AddMilliseconds(10), 6, PointEvent.PointType.B)
            }, stopId, 2);

            var interval2 = new SegmentedIntervalEvent(new BasicIntervalEvent[] { e21, e22 }, c.Second.ToString());

            #endregion

            algorithm.Run(interval1, interval2, stopId, x => true, debug: true);
        }

        [TestMethod]
        public void TestMethod2()
        {
            var stopId = 1111;
            var inference = new InferenceHelper();
            var c = new ComparisonInfo(new JourneyKey("A", 123), new JourneyKey("B", 456), stopId);
            var algorithm = new RelationSatisfaction();

            #region First event

            var e11 = new BasicIntervalEvent(new PointEvent[]
            {
                new PointEvent(EventType.Start, DateTime.Parse("2016-01-01 7:00"), 1, PointEvent.PointType.A),
                new PointEvent(EventType.Suspend, null, 3, PointEvent.PointType.A)
            }, 1, 1);

            var e12 = new BasicIntervalEvent(new PointEvent[]
            {
                new PointEvent(EventType.Resume, null, 4, PointEvent.PointType.A),
                new PointEvent(EventType.End, DateTime.Parse("2016-01-01 10:00"), 6, PointEvent.PointType.A)
            }, stopId, 2);

            var interval1 = new SegmentedIntervalEvent(new BasicIntervalEvent[] { e11, e12 }, c.First.ToString());

            #endregion

            #region Second event

            var e21 = new BasicIntervalEvent(new PointEvent[]
            {
                new PointEvent(EventType.Start, DateTime.Parse("2016-01-01 7:00").AddMilliseconds(10), 1, PointEvent.PointType.B),
                new PointEvent(EventType.Suspend, null, 3, PointEvent.PointType.B)
            }, 1, 1);

            var e22 = new BasicIntervalEvent(new PointEvent[]
            {
                new PointEvent(EventType.Resume, null, 4, PointEvent.PointType.B),
                new PointEvent(EventType.End, DateTime.Parse("2016-01-01 10:00").AddMilliseconds(10), 6, PointEvent.PointType.B)
            }, stopId, 2);

            var interval2 = new SegmentedIntervalEvent(new BasicIntervalEvent[] { e21, e22 }, c.Second.ToString());

            #endregion

            algorithm.Run(interval1, interval2, stopId, x => true, debug: true);
        }
    }
}
