using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UncertainEventStreams.Events
{
    public enum EventType
    {
        Start,
        Suspend,
        Resume,
        Active,
        NotActive,
        End
    }
}
