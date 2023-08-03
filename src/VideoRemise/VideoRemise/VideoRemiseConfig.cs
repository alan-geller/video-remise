using System;
using System.Collections.Generic;
using System.IO;
using Windows.Storage;
using YamlDotNet.Serialization.NamingConventions;

namespace VideoRemise
{
    internal class VideoRemiseConfig
    {
        public const int Epee = 0;
        public const int Foil = 1;
        public const int Saber = 2;

        public List<string> VideoSources { get; set; } = new List<string>();
        public string AudioSource { get; set; } = null;

        public byte UsbAdapterHostClass { get; set; } = 2;
        public string UsbAdapterName { get; set; }
        public string TriggerProtocol { get; set; } = "";
        public bool ManualTriggerEnabled { get; set; } = true;

        public int[] ReplayMillisBeforeTrigger { get; } = { 6000, 6000, 6000 };
        public int[] ReplayMillisAfterTrigger { get; } = { 2000, 2000, 2000 };
        public int ActionContinuationMillis { get; set; } = 1500;

        public bool IsReadyToGo
        {
            get
            {
                return (VideoSources.Count > 0) &&
                    (TriggerProtocol != "" || ManualTriggerEnabled);
            }
        }

        public VideoRemiseConfig()
        {

        }

        public void Save()
        {
            var appSettings = ApplicationData.Current.LocalSettings;

            // Device settings
            var deviceSettings = appSettings.CreateContainer("Device", 
                ApplicationDataCreateDisposition.Always);
            deviceSettings.Values["VideoCount"] = VideoSources.Count;
            int i = 0;
            foreach (var src in VideoSources)
            {
                deviceSettings.Values[$"VideoSource{i++}"] = src;
            }
            deviceSettings.Values["TriggerProtocol"] = TriggerProtocol;
            deviceSettings.Values["ManualTriggerEnabled"] = ManualTriggerEnabled;
            deviceSettings.Values["AudioEnabled"] = AudioSource != null;

            // Timing settings
            var timingSettings = appSettings.CreateContainer("Timing", 
                ApplicationDataCreateDisposition.Always);
            for (i = 0; i < 3; i++)
            {
                timingSettings.Values[$"PreTrigger{i}"] = ReplayMillisBeforeTrigger[i];
                timingSettings.Values[$"PostTrigger{i}"] = ReplayMillisAfterTrigger[i];
            }
        }

        public void ToFile(string filePath)
        {
            var serializer = new YamlDotNet.Serialization.SerializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build();

            File.WriteAllText(filePath, serializer.Serialize(this));
        }

        public static VideoRemiseConfig Load()
        {
            var config = new VideoRemiseConfig();
            var appSettings = ApplicationData.Current.LocalSettings;

            try
            {
                // Throws if the container doesn't exist
                var deviceSettings = appSettings.CreateContainer("Device",
                    ApplicationDataCreateDisposition.Existing);
                var videoCount = (int)deviceSettings.Values["VideoCount"];
                for (int i = 0; i < videoCount; i++)
                {
                    config.VideoSources.Add(deviceSettings.Values[$"VideoSource{i}"]?.ToString());
                }
                config.TriggerProtocol = deviceSettings.Values["TriggerProtocol"].ToString();
                config.ManualTriggerEnabled = (bool)deviceSettings.Values["ManualTriggerEnabled"];
                if ((bool)(deviceSettings.Values["AudioEnabled"] ?? false))
                {
                    config.AudioSource = "test";
                }
            }
            catch (Exception)
            {
                // Ignore this
            }

            try
            {
                // Throws if the container doesn't exist
                var timingSettings = appSettings.CreateContainer("Timing",
                    ApplicationDataCreateDisposition.Existing);
                for (int i = 0; i < 3; i++)
                {
                    config.ReplayMillisBeforeTrigger[i] = (int)timingSettings.Values[$"PreTrigger{i}"];
                    config.ReplayMillisAfterTrigger[i] = (int)timingSettings.Values[$"PostTrigger{i}"];
                }
            }
            catch (Exception)
            {
                // Ignore this
            }

            return config;
        }

        public static VideoRemiseConfig FromFile(string filePath)
        {
            var deserializer = new YamlDotNet.Serialization.DeserializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build();

            try
            {
                return deserializer.Deserialize<VideoRemiseConfig>(File.ReadAllText(filePath));
            }
            catch (Exception)
            {
                var config =  new VideoRemiseConfig();
                config.ToFile(filePath);
                return config;
            }
        }

        public static VideoRemiseConfig FromFile()
        {
            var filePath = Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, "config.yaml");

            return FromFile(filePath);
        }

        public static string WeaponName(int i)
        {
            switch (i)
            {
                case Epee:
                    return "epee";
                case Foil:
                    return "foil";
                case Saber:
                    return "saber";
                default:
                    return "undefined";
            }
        }
    }
}
