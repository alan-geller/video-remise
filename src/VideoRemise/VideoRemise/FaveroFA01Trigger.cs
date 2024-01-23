using LightDisplayVisualEffect;
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
    // The first two data bytes are the left and right scores, in each in two nibbles of BCD.
    // The next two data bytes are the clock time in minutes and seconds, each in two nibbles of BCD.
    // The lights are bit-coded into the fifth data byte, as follows:
    //    bit 0: left-side white(off target)
    //    bit 1: right-side white
    //    bit 2: left-side red
    //    bit 3: right-side green
    //    bit 4: right-side yellow(grounding)
    //    bit 5: left-side yellow
    // The sixth, seventh, and eighth data bytes are unknown.
    // The checksum byte is a simple unsigned sum of all of the initial data bytes. This means
    // That it is possible for the checksum byte to be 0xff and look like a start of frame, so 
    // the code needs to check for that. None of the other data bytes can be 0xff, so if teo
    // consecutive flag values are seen, the first must be the checksum from the previous frame
    // and the second the start of the new frame.
    internal class FaveroFA01Trigger : SerialTrigger
    {
        const byte StartOfFrame = 0xff;
        const int LightStatusPosition = 5;
        const byte LightStatusMask = 0x0f;
        const int FrameSize = 10;
        byte[] CurrentFrame;

        int position;
        Lights lightStatus;

        public FaveroFA01Trigger()
        {
            readFrameLength = FrameSize;
            position = 0;
            FiresClockEvents = false;
            CurrentFrame = new byte[FrameSize];
        }

        public override async Task Initialize(VideoRemiseConfig config)
        {
            await base.Initialize(config);
            //device.BaudRate = 1200;
        }

        protected override async Task ProcessBufferAsync()
        {
            async Task<bool> ScanForStartOfFrame()
            {
                while (reader.UnconsumedBufferLength > 0)
                {
                    var readByte = reader.ReadByte();
                    await Log($"Read {readByte:x} while looking for start of frame");
                    if (readByte == StartOfFrame)
                    {
                        CurrentFrame[position] = readByte;
                        position++;
                        return reader.UnconsumedBufferLength > 0;
                    }
                }
                return false;
            }

            bool ValidateChecksum()
            {
                byte sum = 0;
                for (int i = 0; i < FrameSize - 1; i++)
                {
                    sum += CurrentFrame[i];
                }
                return sum == CurrentFrame[CurrentFrame.Length - 1];
            }

            // If we think we're at the beginning, scan for a start of frame byte
            if (position == 0)
            {
                if (!await ScanForStartOfFrame())
                {
                    // If we didn't get past the start-of-frame byte yet, keep reading
                    return;
                }
            }

            var currentByte = reader.ReadByte();
            await Log($"Position {position}: read {currentByte:x}");
            // We ignore all the other bytes; this could mean that we accept a frame
            // with a bad checksum, but we don't know the checksum algorithm (yet).
            if ((currentByte == StartOfFrame) && (position == 1))
            {
                // Two consecutive start-of-frame bytes means the first was actually a checksum byte
                // from the last frame, and this one is the real start-of-frame, so we should move on
                // to the next byte (if there is one).
                if (reader.UnconsumedBufferLength == 0)
                {
                    return;
                }
                else
                {
                    currentByte = reader.ReadByte();
                }
            }
            CurrentFrame[position] = currentByte;
            position++;

            if (position == LightStatusPosition)
            {
                lightStatus = (Lights)(LightStatusMask & currentByte);
                await Log($"Light status is {lightStatus}");
            }

            if (position >= readFrameLength)
            {
                if (ValidateChecksum())
                {
                    FireLightEvent(lightStatus);
                }
                position = 0;
            }
        }
    }
}
