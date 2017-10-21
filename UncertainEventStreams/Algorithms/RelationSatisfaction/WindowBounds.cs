using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UncertainEventStreams.Algorithms.RelationSatisfaction
{
    public class WindowBounds
    {
        public DateTime Lower { get; set; }

        public DateTime Upper { get; set; }

        public long TicksDifference { get { return (Upper - Lower).Ticks; } }
    }
}
