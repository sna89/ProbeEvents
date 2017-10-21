using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UncertainEventStreams.Entities
{
    public class JourneyKey
    {
        public string JourneyPatternId { get; set; }

        public int VehicleJourneyId { get; set; }

        public JourneyKey(string journeyPatternId, int vehicleJourneyId)
        {
            JourneyPatternId = journeyPatternId;
            VehicleJourneyId = vehicleJourneyId;
        }

        public override string ToString()
        {
            return string.Format("JourneyPatternId:{0}, VehicleJourneyId: {1}", JourneyPatternId, VehicleJourneyId);
        }
    }
}
