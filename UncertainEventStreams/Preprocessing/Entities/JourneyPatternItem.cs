using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UncertainEventStreams.Preprocessing
{
    public class JourneyPatternItem
    {
        public int StopId { get; set; }
        public int StopIndex { get; set; }

        public override string ToString()
        {
            return string.Format("StopId: {0}, StopIdex: {1}", StopId, StopIndex);
        }
    }
}
