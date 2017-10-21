using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UncertainEventStreams.Entities
{
    public class World
    {
        public enum State { Overlap, NoOverlap }

        public double Probability { get; set; }

        public State WorldState { get; set; }

        public World(State state, double probability)
        {
            Probability = probability;
            WorldState = state;
        }
    }
}
