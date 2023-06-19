using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        List<MediaSourceInfo> videoSources;
        List<MediaSourceInfo> audioSources;

        void ToFile(string filePath)
        {
            var serializer = new YamlDotNet.Serialization.SerializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build();

            File.WriteAllText(filePath, serializer.Serialize(this));
        }

        static FencingReplayConfig FromFile(string filePath)
        {
            var deserializer = new YamlDotNet.Serialization.DeserializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build();

            var myConfig = deserializer.Deserialize<FencingReplayConfig>(File.ReadAllText(filePath));

            return myConfig;
        }
    }
}
