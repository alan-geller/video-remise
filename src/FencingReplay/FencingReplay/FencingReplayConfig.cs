using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Documents;
using YamlDotNet.Serialization.NamingConventions;

namespace FencingReplay
{
    internal class MediaSourceInfo
    {
        string groupId;
        string groupDisplayName;
        string sourceId;

    }

    internal class FencingReplayConfig
    {
        public List<MediaSourceInfo> VideoSources { get; set; } = new List<MediaSourceInfo>();
        public MediaSourceInfo AudioSources { get; set; } = null;

        public string TriggerProtocol { get; set; } = "";
        public bool ManualTriggerEnabled { get; set; } = true;

        public int ReplaySecondsBeforeTrigger { get; set; } = 6;
        public int ReplaySecondsAfterTrigger { get; set; } = 2;

        public void ToFile(string filePath)
        {
            var serializer = new YamlDotNet.Serialization.SerializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build();

            File.WriteAllText(filePath, serializer.Serialize(this));
        }

        public static FencingReplayConfig FromFile(string filePath)
        {
            var deserializer = new YamlDotNet.Serialization.DeserializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build();

            try
            {
                return deserializer.Deserialize<FencingReplayConfig>(File.ReadAllText(filePath));
            }
            catch (Exception)
            {
                var config =  new FencingReplayConfig();
                config.ToFile(filePath);
                return config;
            }
        }

        public static FencingReplayConfig FromFile()
        {
            var filePath = Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, "config.yaml");

            return FromFile(filePath);
        }

    }
}
