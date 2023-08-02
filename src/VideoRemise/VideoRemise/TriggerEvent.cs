using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoRemise
{
    internal enum TriggerType
    {
        LeftOnTarget = 1,
        LeftOffTarget = 2,
        RightOnTarget = 4,
        RightOffTarget = 8,
        Halt = 16
    }

    internal class TriggerEvent
    {
        public TimeSpan BaseTime { get; set; }
        public TimeSpan LeftLightDelta { get; set; }
        public TimeSpan RightLightDelta { get; set; }
        public TriggerType EventType { get; set; }

        public TriggerEvent()
        {
            BaseTime = TimeSpan.Zero;
            LeftLightDelta = TimeSpan.Zero;
            RightLightDelta = TimeSpan.Zero;
            EventType = TriggerType.Halt;
        }
    }
}
