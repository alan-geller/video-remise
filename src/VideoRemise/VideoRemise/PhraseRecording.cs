using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace VideoRemise
{
    internal class PhraseRecording
    {
        StorageFile file;
        TimeSpan replayLength;
        TriggerType triggerEvent;

        public PhraseRecording(StorageFile _file, int _seconds, TriggerType _triggerEvent)
        {
            file = _file;
            replayLength = TimeSpan.FromSeconds(_seconds);
            triggerEvent = _triggerEvent;
        }

        public (MediaSource, TimeSpan) GetSourceAndLength()
        {
            return (MediaSource.CreateFromStorageFile(file), replayLength);
        }
    }
}
