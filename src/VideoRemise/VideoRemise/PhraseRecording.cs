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
        internal readonly StorageFile file;
        internal TimeSpan ReplayLength { get; set; }
        internal TimeSpan? ReplayStart { get; set; }
        internal TriggerType triggerEvent;

        public PhraseRecording(StorageFile _file, TimeSpan _seconds, TimeSpan? _start, TriggerType _triggerEvent)
        {
            file = _file;
            ReplayLength = _seconds;
            ReplayStart = _start;
            triggerEvent = _triggerEvent;
        }

        public MediaSource GetSource()
        {
            return MediaSource.CreateFromStorageFile(file);
        }

        public PhraseRecording SplitAt(TimeSpan splitTime)
        {
            var newLength = VideoChannel.TSMin(splitTime, ReplayLength);
            // Guaranteed to be at least 0
            var newStart = splitTime - newLength;
            return new PhraseRecording(file, newLength, newStart, triggerEvent);
        }
    }
}
