using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UncertainEventStreams.Algorithms;
using UncertainEventStreams.Algorithms.RelationSatisfaction;
using UncertainEventStreams.Entities;
using UncertainEventStreams.Events;
using UncertainEventStreams.Events.IntervalBased;
using UncertainEventStreams.Preprocessing;

namespace UncertainEventStreams.Inference
{
    public class InferenceHelper
    {
        #region Private members

        private EventLogStore _logStore;

        #endregion

        #region Private methods

        private Journey Load(JourneyKey key, PointEvent.PointType type)
        {
            var journeyEvents = _logStore.GetProcessedLog(key, type);
            var journey = new Journey(key, journeyEvents);
            if (!journey.Intervals.Validate())
            {
                throw new Exception(string.Format("Intervals are not valid for journey: {0}", key));
            }

            return journey;
        }

        #endregion

        #region Constructors

        public InferenceHelper()
        {
            _logStore = new EventLogStore();
        }

        #endregion

        #region Public methods

        public void GetOverlapProbability(JourneyKey firstJourney, JourneyKey secondJourney, int stop)
        {
            var j1 = Load(firstJourney, PointEvent.PointType.A);
            var j2 = Load(secondJourney, PointEvent.PointType.B);
            var algorithm = new RelationSatisfaction();
            

            var res = algorithm.Run(j1.Intervals, j2.Intervals, stop, (l) => CheckOverlapPath(l, stop));
            if (res != null)
            {
                _logStore.WriteResult(firstJourney, secondJourney, stop, res);
            }            
        }

        public void GetCompleteProbability(JourneyKey firstJourney, JourneyKey secondJourney, int stop)
        {
            var j1 = Load(firstJourney, PointEvent.PointType.A);
            var j2 = Load(secondJourney, PointEvent.PointType.B);
            var algorithm = new RelationSatisfaction();

            var res = algorithm.Run(j1.Intervals, j2.Intervals, stop, MockCheckPath);
            if (res != null)
            {
                _logStore.WriteResult(firstJourney, secondJourney, stop, res);
            }
        }

        public bool MockCheckPath(TreeNode l)
        {
            return true;
        }

        public  bool CheckOverlapPath(TreeNode l, int stop)
        {
            var resumeA = l.PathToRoot.First(x => x.Data.IntervalId == stop && x.Data.DataPointType == PointEvent.PointType.A && x.Data.Type == EventType.Resume);
            var suspendPreA = l.PathToRoot.First(x => x.Rank < resumeA.Rank && x.Data.DataPointType == PointEvent.PointType.A && x.Data.Type == EventType.Suspend);

            var resumeB = l.PathToRoot.First(x => x.Data.IntervalId == stop && x.Data.DataPointType == PointEvent.PointType.B && x.Data.Type == EventType.Resume);
            var suspendPreB = l.PathToRoot.First(x => x.Rank < resumeB.Rank && x.Data.DataPointType == PointEvent.PointType.B && x.Data.Type == EventType.Suspend);

            return (suspendPreA.Rank < suspendPreB.Rank && resumeA.Rank > suspendPreB.Rank)
                || (suspendPreB.Rank < suspendPreA.Rank && resumeB.Rank > suspendPreA.Rank);
        }

        #endregion
    }
}
