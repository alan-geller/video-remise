﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.UI.Xaml.Documents;
using YamlDotNet.Serialization.NamingConventions;

namespace FencingReplay
{
    internal class FencingReplayConfig
    {
        public List<string> VideoSources { get; set; } = new List<string>();
        public string AudioSource { get; set; } = null;

        public string TriggerProtocol { get; set; } = "";
        public bool ManualTriggerEnabled { get; set; } = true;

        public int ReplaySecondsBeforeTrigger { get; set; } = 6;
        public int ReplaySecondsAfterTrigger { get; set; } = 2;

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
            var appSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

            // Device settings
            var deviceSettings = appSettings.CreateContainer("Device", Windows.Storage.ApplicationDataCreateDisposition.Always);
            deviceSettings.Values["VideoCount"] = VideoSources.Count;
            int i = 0;
            foreach (var src in VideoSources)
            {
                deviceSettings.Values[$"VideoSource{++i}"] = src;
            }
            deviceSettings.Values["TriggerProtocol"] = TriggerProtocol;
            deviceSettings.Values["ManualTriggerEnabled"] = ManualTriggerEnabled;
            deviceSettings.Values["AudioEnabled"] = AudioSource != null;
        }


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
