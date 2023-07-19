﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.UI.Xaml.Documents;
using Windows.Storage;
using YamlDotNet.Serialization.NamingConventions;

namespace FencingReplay
{
    internal class FencingReplayConfig
    {
        public const int Epee = 0;
        public const int Foil = 1;
        public const int Saber = 2;

        public List<string> VideoSources { get; set; } = new List<string>();
        public string AudioSource { get; set; } = null;

        public string TriggerProtocol { get; set; } = "";
        public bool ManualTriggerEnabled { get; set; } = true;

        public int[] ReplaySecondsBeforeTrigger { get; } = { 6, 6, 6 };
        public int[] ReplaySecondsAfterTrigger { get; } = { 2, 2, 2 };

        public bool IsReadyToGo
        {
            get
            {
                return (VideoSources.Count > 0) &&
                    (TriggerProtocol != "" || ManualTriggerEnabled);
            }
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
                timingSettings.Values[$"PreTrigger{i}"] = ReplaySecondsBeforeTrigger[i];
                timingSettings.Values[$"PostTrigger{i}"] = ReplaySecondsAfterTrigger[i];
            }
        }

        public void ToFile(string filePath)
        {
            var serializer = new YamlDotNet.Serialization.SerializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build();

            File.WriteAllText(filePath, serializer.Serialize(this));
        }

        public static FencingReplayConfig Load()
        {
            var config = new FencingReplayConfig();
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
                    config.ReplaySecondsBeforeTrigger[i] = (int)timingSettings.Values[$"PreTrigger{i}"];
                    config.ReplaySecondsAfterTrigger[i] = (int)timingSettings.Values[$"PostTrigger{i}"];
                }
            }
            catch (Exception)
            {
                // Ignore this
            }

            return config;
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
