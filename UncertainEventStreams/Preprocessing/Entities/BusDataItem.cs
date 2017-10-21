using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UncertainEventStreams.Preprocessing
{
    public class BusDataItem
    {
        public DateTime? Timestamp { get; set; }

        public int LineId { get; set; }

        public string JourneyPatternId { get; set; }

        public int VehicleJourneyId { get; set; }

        public int StopId { get; set; }

        public bool AtStop { get; set; }

        public string Id { get { return JourneyPatternId.ToString(); } }

        public override string ToString()
        {
            return string.Format("Timestamp: {0}, JourneyPatternId: {1}, VehicleJourneyId: {2}", 
                Timestamp, JourneyPatternId, VehicleJourneyId);
        }

        public BusDataItem Clone()
        {
            return (BusDataItem)MemberwiseClone();
        }
    }
}
