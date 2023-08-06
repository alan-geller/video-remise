using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace VideoRemise
{
    // The Favero FA01 scoring machine sends 10-byte frames:
    // Each frame has 0xff at position 0, then 8 bytes of data, and a final checksum byte.
    // The lights are bit-coded into the byte at position 5, as follows:
    //bit 0: left-side white(off target)
    //bit 1: right-side white
    //bit 2: left-side red
    //bit 3: right-side green
    //bit 4: right-side yellow(grounding)
    //bit 5: left-side yellow
    internal class FaveroFA01Trigger : SerialTrigger
    {
        const byte StartOfFrame = 0xff;
        const int LightStatusPosition = 5;
        const byte LightStatusMask = 0x0f;

        int position;
        Lights lightStatus;

        public FaveroFA01Trigger()
        {
            readFrameLength = 10;
            position = 0;
            FiresClockEvents = false;
        }

        public override async Task Initialize(VideoRemiseConfig config)
        {
            await base.Initialize(config);
            device.BaudRate = 1200;
        }

        protected override void ProcessBuffer()
        {
            bool ScanForFrame()
            {
                while (reader.UnconsumedBufferLength > 0)
                {
                    var readByte = reader.ReadByte();
                    if (readByte == StartOfFrame)
                    {
                        position++;
                        return reader.UnconsumedBufferLength > 0;
                    }
                }
                return false;
            }

            // If we think we're at the beginning, scan for a start of frame byte
            if (position == 0)
            {
                if (!ScanForFrame())
                {
                    // If we didn't get passed the start-of-frame byte yet, keep reading
                    return;
                }
            }

            var currentByte = reader.ReadByte();
            position++;
            // We ignore all the other bytes; this could mean that we accept a frame
            // with a bad checksum, but we don't know the checksum algorithm (yet).
            if (position == LightStatusPosition)
            {
                lightStatus = (Lights)(LightStatusMask & currentByte);
            }

            if (position >= readFrameLength)
            {
                FireLightEvent(lightStatus);
                position = 0;
            }
        }
    }
}
