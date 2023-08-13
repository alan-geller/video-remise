﻿using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Media.Capture.Frames;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Windows.Devices.Enumeration;
using Windows.Devices.Usb;
using System.Threading.Tasks;
using Windows.Devices.SerialCommunication;
using System.Text.RegularExpressions;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace VideoRemise
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ConfigPage : Page
    {
        private VideoRemiseConfig config;
        private Dictionary<string, DeviceInformation> devices;

        public ConfigPage()
        {
            devices = new Dictionary<string, DeviceInformation>();
            this.InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            config = (Application.Current as App).Config;

            await PopulateAdapterList();

            if (!string.IsNullOrWhiteSpace(config.TriggerProtocol))
            {
                triggerProtocol.SelectedItem = config.TriggerProtocol;
            }

            manualTrigger.IsChecked = config.ManualTriggerEnabled;

            videoFeedLeft.Items.Clear();
            videoFeedCenter.Items.Clear();
            videoFeedRight.Items.Clear();
            var sources = await MediaFrameSourceGroup.FindAllAsync();
            foreach (var source in sources)
            {
                videoFeedLeft.Items.Add(source.DisplayName);
                videoFeedCenter.Items.Add(source.DisplayName);
                videoFeedRight.Items.Add(source.DisplayName);
            }

            videoCount1Btn.IsChecked = false;
            videoCount2Btn.IsChecked = false;
            videoCount3Btn.IsChecked = false;
            switch (config.VideoSources.Count)
            {
                case 1:
                    videoCount1Btn.IsChecked = true;
                    videoFeedLeft.IsEnabled = false;
                    videoFeedCenter.IsEnabled = true;
                    videoFeedRight.IsEnabled = false;
                    videoFeedCenter.SelectedItem = config.VideoSources[0];
                    break;
                case 2:
                    videoCount2Btn.IsChecked = true;
                    videoFeedLeft.IsEnabled = true;
                    videoFeedCenter.IsEnabled = false;
                    videoFeedRight.IsEnabled = true;
                    videoFeedLeft.SelectedItem = config.VideoSources[0];
                    videoFeedRight.SelectedItem = config.VideoSources[1];
                    break;
                case 3:
                    videoCount3Btn.IsChecked = true;
                    videoFeedLeft.IsEnabled = true;
                    videoFeedCenter.IsEnabled = true;
                    videoFeedRight.IsEnabled = true;
                    videoFeedLeft.SelectedItem = config.VideoSources[0];
                    videoFeedCenter.SelectedItem = config.VideoSources[1];
                    videoFeedRight.SelectedItem = config.VideoSources[2];
                    break;
                default:
                    videoFeedLeft.IsEnabled = false;
                    videoFeedCenter.IsEnabled = false;
                    videoFeedRight.IsEnabled = false;
                    break;
            }

            epeePre.Text = config.ReplayMillisBeforeTrigger[VideoRemiseConfig.Epee].ToString();
            epeePost.Text = config.ReplayMillisAfterTrigger[VideoRemiseConfig.Epee].ToString();
            foilPre.Text = config.ReplayMillisBeforeTrigger[VideoRemiseConfig.Foil].ToString();
            foilPost.Text = config.ReplayMillisAfterTrigger[VideoRemiseConfig.Foil].ToString();
            saberPre.Text = config.ReplayMillisBeforeTrigger[VideoRemiseConfig.Saber].ToString();
            saberPost.Text = config.ReplayMillisAfterTrigger[VideoRemiseConfig.Saber].ToString();

            redColor.Color = config.RedLightColor;
            greenColor.Color = config.GreenLightColor;
        }

        private async Task PopulateAdapterList()
        {
            var myDevices = 
                await DeviceInformation.FindAllAsync(SerialDevice.GetDeviceSelector());

            triggerAdapter.Items.Clear();
            foreach (var device in myDevices)
            {
                triggerAdapter.Items.Add(device.Name);
                devices[device.Name] = device;
                if (device.Id == config.AdapterDeviceId)
                {
                    triggerAdapter.SelectedItem = device.Name;
                }
            }
        }

        private void OnCameraCount1(object sender, RoutedEventArgs e)
        {
            videoFeedLeft.IsEnabled = false;
            videoFeedCenter.IsEnabled = true;
            videoFeedRight.IsEnabled = false;

            switch (config.VideoSources.Count)
            {
                case 2:
                    config.VideoSources.RemoveAt(1);
                    break;
                case 3:
                    config.VideoSources.RemoveAt(2);
                    config.VideoSources.RemoveAt(0);
                    break;
            }

            videoFeedLeft.SelectedIndex = -1;
            videoFeedCenter.SelectedIndex = -1;
            videoFeedRight.SelectedIndex = -1;

            if (config.VideoSources.Count == 1)
            {
                videoFeedCenter.SelectedItem = config.VideoSources[0];
            }
        }

        private void OnCameraCount2(object sender, RoutedEventArgs e)
        {
            videoFeedLeft.IsEnabled = true;
            videoFeedCenter.IsEnabled = false;
            videoFeedRight.IsEnabled = true;

            switch (config.VideoSources.Count)
            {
                case 3:
                    config.VideoSources.RemoveAt(2);
                    break;
            }

            videoFeedLeft.SelectedIndex = -1;
            videoFeedCenter.SelectedIndex = -1;
            videoFeedRight.SelectedIndex = -1;

            if (config.VideoSources.Count >= 1)
            {
                videoFeedLeft.SelectedItem = config.VideoSources[0];
            }
            if (config.VideoSources.Count == 2)
            {
                videoFeedRight.SelectedItem = config.VideoSources[1];
            }
        }

        private void OnCameraCount3(object sender, RoutedEventArgs e)
        {
            videoFeedLeft.IsEnabled = true;
            videoFeedCenter.IsEnabled = true;
            videoFeedRight.IsEnabled = true;

            switch (config.VideoSources.Count)
            {
                case 1:
                    videoFeedLeft.SelectedIndex = -1;
                    videoFeedCenter.SelectedItem = config.VideoSources[0];
                    videoFeedRight.SelectedIndex = -1;
                    break;
                case 2:
                    videoFeedLeft.SelectedItem = config.VideoSources[0];
                    videoFeedCenter.SelectedIndex = -1;
                    videoFeedRight.SelectedItem = config.VideoSources[1];
                    break;
                case 3:
                    videoFeedLeft.SelectedItem = config.VideoSources[0];
                    videoFeedCenter.SelectedItem = config.VideoSources[1];
                    videoFeedRight.SelectedItem = config.VideoSources[2];
                    break;
            }
        }

        private void OnSave(object sender, RoutedEventArgs e)
        {
            string SameOrDefault(string s, string def)
            {
                return string.IsNullOrWhiteSpace(s) ? def : s;
            }

            // Populate the config object
            var config = (Application.Current as App).Config;

            if (triggerAdapter.SelectedItem != null)
            {
                config.AdapterDeviceId = devices[triggerAdapter.SelectedItem.ToString()].Id;
            }
            else
            {
                config.AdapterDeviceId = "";
            }
            config.ManualTriggerEnabled = manualTrigger.IsChecked ?? true;
            config.TriggerProtocol = triggerProtocol.SelectedItem?.ToString() ?? "";
            config.AudioSource = null;

            var newSources = new List<string>();
            if (videoFeedLeft.SelectedIndex >= 0)
            {
                newSources.Add(videoFeedLeft.SelectedItem.ToString());
            }
            if (videoFeedCenter.SelectedIndex >= 0)
            {
                newSources.Add(videoFeedCenter.SelectedItem.ToString());
            }
            if (videoFeedRight.SelectedIndex >= 0)
            {
                newSources.Add(videoFeedRight.SelectedItem.ToString());
            }

            var camerasChanged = false;
            if (newSources.Count != config.VideoSources.Count)
            {
                camerasChanged = true;
            }
            else
            {
                for (int i = 0; i < newSources.Count; i++)
                {
                    if (newSources[i] != config.VideoSources[i])
                    {
                        camerasChanged = true;
                    }
                }
            }
            config.VideoSources = newSources;

            config.ReplayMillisBeforeTrigger[VideoRemiseConfig.Epee] = 
                int.Parse(SameOrDefault(epeePre.Text, "0"));
            config.ReplayMillisAfterTrigger[VideoRemiseConfig.Epee] = 
                int.Parse(SameOrDefault(epeePost.Text, "0"));
            config.ReplayMillisBeforeTrigger[VideoRemiseConfig.Foil] = 
                int.Parse(SameOrDefault(foilPre.Text, "0"));
            config.ReplayMillisAfterTrigger[VideoRemiseConfig.Foil] = 
                int.Parse(SameOrDefault(foilPost.Text, "0"));
            config.ReplayMillisBeforeTrigger[VideoRemiseConfig.Saber] = 
                int.Parse(SameOrDefault(saberPre.Text, "0"));
            config.ReplayMillisAfterTrigger[VideoRemiseConfig.Saber] = 
                int.Parse(SameOrDefault(saberPost.Text, "0"));

            config.RedLightColor = redColor.Color;
            config.GreenLightColor = greenColor.Color;

            config.Save();
            (Application.Current as App).Config = config;

            Frame.Navigate(typeof(MainPage), camerasChanged);
        }

        private void OnCancel(object sender, RoutedEventArgs e)
        {
            //var config = (Application.Current as App).Config;
            Frame.Navigate(typeof(MainPage), false);
        }

        // Make sure that the current text is a valid decimal number
        // It should be a (possibly empty) string of digits, then an optional period,
        // then another possibly empty series of digits.
        private void VerifyDigitEntry(object sender, TextBoxBeforeTextChangingEventArgs e)
        {
            if (e.NewText.Length > 0)
            {
                int n = e.NewText.IndexOf('.');
                if (n == -1)
                {
                    e.Cancel = !e.NewText.All(char.IsDigit);
                }
                else
                {
                    var pre = e.NewText.Substring(0, n);
                    var post = e.NewText.Substring(n + 1);
                    e.Cancel = !pre.All(char.IsDigit) || !post.All(char.IsDigit);
                }
            }
        }
    }
}
