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
        private StorageFile file;
        private TimeSpan replayLength;
        private TriggerType triggerEvent;
        internal bool Tag { get; set; }

        public PhraseRecording(StorageFile _file, TimeSpan _seconds, TriggerType _triggerEvent)
        {
            file = _file;
            replayLength = _seconds;
            triggerEvent = _triggerEvent;
        }

        public (MediaSource, TimeSpan) GetSourceAndLength()
        {
            return (MediaSource.CreateFromStorageFile(file), replayLength);
        }
    }
}
