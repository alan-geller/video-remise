using LightDisplayVisualEffect;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
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

        private int position;
        private Lights lightStatus;
        private StorageFile logFile;

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
            var myFiles = await StorageLibrary.GetLibraryAsync(KnownLibraryId.Documents);
            var myFolder = await myFiles.SaveFolder.CreateFolderAsync("VideoRemise",
                CreationCollisionOption.OpenIfExists);
            logFile = await myFolder.CreateFileAsync("serial_log.txt",
                CreationCollisionOption.GenerateUniqueName);
        }

        protected override async Task ProcessBuffer()
        {
            async Task<bool> ScanForFrameAsync()
            {
                while (reader.UnconsumedBufferLength > 0)
                {
                    var readByte = reader.ReadByte();
                    if (readByte == StartOfFrame)
                    {
                        await FileIO.AppendTextAsync(logFile, $"Found start of frame, 0x{StartOfFrame:x2}\n");
                        position++;
                        return reader.UnconsumedBufferLength > 0;
                    }
                }
                return false;
            }

            // If we think we're at the beginning, scan for a start of frame byte
            if (position == 0)
            {
                var frameFound = await ScanForFrameAsync();
                if (!frameFound)
                {
                    // If we didn't get passed the start-of-frame byte yet, keep reading
                    return;
                }
            }

            var currentByte = reader.ReadByte();
            position++;
            await FileIO.AppendTextAsync(logFile, $"Read 0x{currentByte:x2} at offset {position}\n");

            // We ignore all the other bytes; this could mean that we accept a frame
            // with a bad checksum, but we don't know the checksum algorithm (yet).
            if (position == LightStatusPosition)
            {
                lightStatus = (Lights)(LightStatusMask & currentByte);
            }

            if (position >= readFrameLength)
            {
                FireLightEvent(lightStatus);
                await FileIO.AppendTextAsync(logFile, $"Completed frame with light data 0x{lightStatus:b8}\n");
                position = 0;
            }
        }
    }
}
