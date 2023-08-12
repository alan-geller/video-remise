using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Windows.Foundation.Metadata;
using Windows.Media.Core;
using Windows.Storage;
using Windows.UI.Core;
using Windows.ApplicationModel;

namespace VideoRemise
{
    internal class PhraseRecording
    {
        StorageFile file;
        TimeSpan replayLength;
        TriggerType triggerEvent;

        public PhraseRecording(StorageFile _file, double _seconds, TriggerType _triggerEvent)
        {
            file = _file;
            replayLength = TimeSpan.FromSeconds(_seconds);
            triggerEvent = _triggerEvent;
        }

        public (MediaSource, TimeSpan) GetSourceAndLength()
        {
            return (MediaSource.CreateFromStorageFile(file), replayLength);
        }

        public async Task<(PhraseRecording, PhraseRecording)> SplitMP4File(TimeSpan splitTime)
        {
            var myVideos = await StorageLibrary.GetLibraryAsync(KnownLibraryId.Videos);
            var fencingVideos = await myVideos.SaveFolder.CreateFolderAsync(VideoChannel.SaveSubfolderName,
                CreationCollisionOption.OpenIfExists);
            var name = Path.GetFileNameWithoutExtension(file.Name);
            var localFolder = ApplicationData.Current.LocalFolder;
            await file.CopyAsync(localFolder, "toSplit.mp4", NameCollisionOption.ReplaceExisting);

            var outputName1 = $"{fencingVideos.Path}\\{name}-split1.mp4";
            var outputName2 = $"{fencingVideos.Path}\\{name}-split2.mp4";

            string[] data = { splitTime.TotalSeconds.ToString(), file.Path, outputName1, outputName2 };

            if (File.Exists($"{localFolder.Path}\\Done"))
            {
                File.Delete($"{localFolder.Path}\\Done");
            }

            File.WriteAllLines($"{localFolder.Path}\\ffmpeg-command", data);

            if (ApiInformation.IsApiContractPresent("Windows.ApplicationModel.FullTrustAppContract", 1, 0))
            {
                await FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync();
            }

            while (!File.Exists($"{localFolder.Path}\\Done"))
            {
            }

            File.Delete($"{localFolder.Path}\\Done");

            var output1 = await fencingVideos.GetFileAsync($"{name}-split1.mp4");
            var output2 = await fencingVideos.GetFileAsync($"{name}-split2.mp4");

            var phrase1 = new PhraseRecording(output1, replayLength.TotalSeconds, triggerEvent);
            var phrase2 = new PhraseRecording(output2, replayLength.TotalSeconds, triggerEvent);
            return (phrase1, phrase2);
        }
    }
}
