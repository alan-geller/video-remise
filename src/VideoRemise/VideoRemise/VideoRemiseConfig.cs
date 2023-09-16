using System;
using System.Collections.Generic;
using System.IO;
using Windows.Storage;
using Windows.UI;
using YamlDotNet.Serialization.NamingConventions;

namespace VideoRemise
{
    internal class VideoRemiseConfig
    {
        public const int Epee = 0;
        public const int Foil = 1;
        public const int Saber = 2;

        // Video sources are stored as the MediaFrameSource.SourceId field, which is unique
        public List<string> VideoSources { get; set; } = new List<string>();
        public string AudioSource { get; set; } = null;

        public string AdapterDeviceId { get; set; }
        public string TriggerProtocol { get; set; }
        public bool ManualTriggerEnabled { get; set; } = true;

        public TimeSpan[] ReplayDurationBeforeTrigger { get; } = 
            { TimeSpan.FromSeconds(6.0), TimeSpan.FromSeconds(6.0), TimeSpan.FromSeconds(6.0) };
        public TimeSpan[] ReplayDurationAfterTrigger { get; } = 
            { TimeSpan.FromSeconds(2.0), TimeSpan.FromSeconds(2.0), TimeSpan.FromSeconds(2.0) };
        public TimeSpan ActionContinuationDuration { get; set; } = 
            TimeSpan.FromSeconds(1.5);

        public Color RedLightColor { get; set; } = Colors.Red;
        public Color GreenLightColor { get; set; }  = Colors.Green;

        public bool IsReadyToGo => (VideoSources.Count > 0) &&
                    ((!string.IsNullOrWhiteSpace(AdapterDeviceId) 
                            && !string.IsNullOrWhiteSpace(TriggerProtocol)) 
                        || ManualTriggerEnabled);

        public VideoRemiseConfig()
        {

        }

        public void Save()
        {
            void SaveColor(ApplicationDataContainer container, Color color, string name)
            {
                container.Values[name + ".A"] = color.A;
                container.Values[name + ".R"] = color.R;
                container.Values[name + ".G"] = color.G;
                container.Values[name + ".B"] = color.B;
            }

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
            deviceSettings.Values["Adapter"] = AdapterDeviceId;
            deviceSettings.Values["TriggerProtocol"] = TriggerProtocol;
            deviceSettings.Values["ManualTriggerEnabled"] = ManualTriggerEnabled;
            deviceSettings.Values["AudioEnabled"] = AudioSource != null;

            // Timing settings
            var timingSettings = appSettings.CreateContainer("Timing", 
                ApplicationDataCreateDisposition.Always);
            for (i = 0; i < 3; i++)
            {
                timingSettings.Values[$"PreTrigger{i}"] = 
                    ReplayDurationBeforeTrigger[i].TotalSeconds;
                timingSettings.Values[$"PostTrigger{i}"] = 
                    ReplayDurationAfterTrigger[i].TotalSeconds;
            }

            // Color settings
            var colorSettings = appSettings.CreateContainer("Colors", 
                ApplicationDataCreateDisposition.Always);
            SaveColor(colorSettings, RedLightColor, "RedLight");
            SaveColor(colorSettings, GreenLightColor, "GreenLight");
        }

        public void ToFile(string filePath)
        {
            var serializer = new YamlDotNet.Serialization.SerializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build();

            File.WriteAllText(filePath, serializer.Serialize(this));
        }

        public static VideoRemiseConfig Load()
        {
            Color LoadColor(ApplicationDataContainer container, string name, Color def)
            {
                Color color = def;
                if (container.Values.ContainsKey(name + ".A") &&
                    container.Values.ContainsKey(name + ".R") &&
                    container.Values.ContainsKey(name + ".G") &&
                    container.Values.ContainsKey(name + ".B"))
                {
                    color.A = (byte)container.Values[name + ".A"];
                    color.R = (byte)container.Values[name + ".R"];
                    color.G = (byte)container.Values[name + ".G"];
                    color.B = (byte)container.Values[name + ".B"];
                }
                return color;
            }

            T ValueOrDefault<T>(ApplicationDataContainer container, string key, 
                T def)
            {
                object o;
                if (container.Values.TryGetValue(key, out o))
                {
                    return (T)o;
                }
                else
                {
                    return def;
                }
            }

            var config = new VideoRemiseConfig();
            var appSettings = ApplicationData.Current.LocalSettings;

            try
            {
                // Throws if the container doesn't exist
                var deviceSettings = appSettings.CreateContainer("Device",
                    ApplicationDataCreateDisposition.Existing);
                var videoCount = ValueOrDefault(deviceSettings, "VideoCount", 0);
                for (int i = 0; i < videoCount; i++)
                {
                    config.VideoSources.Add(ValueOrDefault(deviceSettings, 
                        $"VideoSource{i}", "").ToString());
                }

                config.AdapterDeviceId = ValueOrDefault(deviceSettings, "Adapter", "");
                config.TriggerProtocol = ValueOrDefault(deviceSettings, "TriggerProtocol", 
                    "");
                config.ManualTriggerEnabled = ValueOrDefault(deviceSettings, 
                    "ManualTriggerEnabled", true);
                if (ValueOrDefault(deviceSettings, "AudioEnabled", false))
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
                    config.ReplayDurationBeforeTrigger[i] = 
                        TimeSpan.FromSeconds(ValueOrDefault(timingSettings,
                        $"PreTrigger{i}", 6.0));
                    config.ReplayDurationAfterTrigger[i] =
                        TimeSpan.FromSeconds(ValueOrDefault(timingSettings,
                        $"PostTrigger{i}", 1.5));
                }
            }
            catch (Exception)
            {
                // Ignore this
            }

            try
            {
                // Throws if the container doesn't exist
                var colorSettings = appSettings.CreateContainer("Colors",
                    ApplicationDataCreateDisposition.Existing);
                config.RedLightColor = LoadColor(colorSettings, "RedLight", Colors.Red);
                config.GreenLightColor = LoadColor(colorSettings, "GreenLight", Colors.Green);
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
