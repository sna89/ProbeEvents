using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UncertainEventStreams.Preprocessing
{
    public class JourneyPatternsStore
    {
        private bool _open;
        private Dictionary<string, List<JourneyPatternItem>> _journeyPatterns;

        private StoreHelper _helper;

        public JourneyPatternsStore()
        {
            _helper = new StoreHelper();
            _journeyPatterns = new Dictionary<string, List<JourneyPatternItem>>();
            _open = false;
        }

        private void Open()
        {
            var journeyPatterns = _helper.GetReader<Tuple<string, JourneyPatternItem>>("SELECT [Journey Pattern ID], [Stop Index], [Stop ID] FROM JourneyPatterns",
                r =>
                {
                    var key = (string)r["Journey Pattern ID"];
                    var stop = new JourneyPatternItem() { StopId = (int)r["Stop ID"], StopIndex = (int)r["Stop Index"] };
                    var tuple = new Tuple<string, JourneyPatternItem>(key, stop);

                    return tuple;
                });

            foreach (var stop in journeyPatterns)
            {
                if (!_journeyPatterns.ContainsKey(stop.Item1))
                {
                    _journeyPatterns[stop.Item1] = new List<JourneyPatternItem>();
                }
                _journeyPatterns[stop.Item1].Add(stop.Item2);
            }

            _open = true;
            //return journey;
        }

        public List<JourneyPatternItem> GetPatterns(string journey)
        {
            if(!_open)
            {
                Open();
            }

            if(!_journeyPatterns.ContainsKey(journey))
            {
                throw new Exception(string.Format("journey {0} does not exists", journey));
            }

            return _journeyPatterns[journey];
        }        
    }
}
