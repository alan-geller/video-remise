using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using YamlDotNet.Serialization.NamingConventions;

namespace VideoRemise
{
    internal class Match
    {
        public string LeftFencer { get; set; }
        public string RightFencer { get; set; }
        public int Weapon { get; set; }
        public List<TriggerEvent> History { get; private set; }
        public int CameraCount { get; set; }

        public bool IsStarted => History.Count > 0;
        public bool IsMatchSetUp => !(string.IsNullOrWhiteSpace(LeftFencer) ||
                                        string.IsNullOrWhiteSpace(RightFencer) ||
                                        Weapon == 0);

        public Match()
        {
            History = new List<TriggerEvent>();
        }

        public TriggerEvent GetNextEvent(TimeSpan now)
        {
            foreach (var trigger in History)
            {
                if (trigger.BaseTime > now)
                {
                    return trigger;
                }
            }
            return null;
        }

        public bool AddTrigger(TimeSpan when, TriggerType type)
        {
            void AddNewEvent()
            {
                var e = new TriggerEvent() { BaseTime = when, EventType = type };
                History.Add(e);
            }

            void UpdateCurrentEvent(TriggerEvent e)
            {
                if (((type & TriggerType.LeftOnTarget) != 0) ||
                    ((type & TriggerType.LeftOffTarget) != 0))
                {
                    e.LeftLightDelta = when - e.BaseTime;
                }
                if (((type & TriggerType.RightOnTarget) != 0) ||
                    ((type & TriggerType.RightOffTarget) != 0))
                {
                    e.RightLightDelta = when - e.BaseTime;
                }
                e.EventType |= type;
            }

            if (History.Count > 0)
            {
                var config = (Application.Current as App).Config;

                var last = History[History.Count - 1];
                if (last.BaseTime + TimeSpan.FromMilliseconds(config.ActionContinuationMillis)
                    > when)
                {
                    UpdateCurrentEvent(last);
                    return false;
                }
                else
                {
                    AddNewEvent();
                    return true;
                }
            }
            else
            {
                AddNewEvent();
                return true;
            }
        }

        public async Task Save()
        {
            var fileName = IsMatchSetUp ? $"{LeftFencer}-{RightFencer}" : "No match";

            var myVideos = await Windows.Storage.StorageLibrary.GetLibraryAsync(Windows.Storage.KnownLibraryId.Videos);
            var fencingVideos = await myVideos.SaveFolder.CreateFolderAsync(VideoChannel.SaveSubfolderName,
                CreationCollisionOption.OpenIfExists);
            var file = await fencingVideos.CreateFileAsync($"{fileName}.yaml",
                CreationCollisionOption.GenerateUniqueName);
            var serializer = new YamlDotNet.Serialization.SerializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build();
            var yaml = serializer.Serialize(this);
            await Windows.Storage.FileIO.WriteTextAsync(file, yaml);
        }
    }
}
