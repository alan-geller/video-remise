using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace FencingReplay
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ConfigPage : Page
    {
        public ConfigPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            var config = (Application.Current as App).Config;

            // Populate from the current confiuration
            if (config.TriggerProtocol != "")
            {
                triggerCombo.SelectedItem = config.TriggerProtocol;
            }

            manualTrigger.IsChecked = config.ManualTriggerEnabled;

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
                    break;
                case 2:
                    videoCount2Btn.IsChecked = true;
                    videoFeedLeft.IsEnabled = true;
                    videoFeedCenter.IsEnabled = false;
                    videoFeedRight.IsEnabled = true;
                    break;
                case 3:
                    videoCount3Btn.IsChecked = true;
                    videoFeedLeft.IsEnabled = true;
                    videoFeedCenter.IsEnabled = true;
                    videoFeedRight.IsEnabled = true;
                    break;
                default:
                    videoFeedLeft.IsEnabled = false;
                    videoFeedCenter.IsEnabled = false;
                    videoFeedRight.IsEnabled = false;
                    break;
            }
        }

        private void OnSave(object sender, RoutedEventArgs e)
        {
            // Populate the config object
            var config = (Application.Current as App).Config;

            config.Save();

            Frame.Navigate(typeof(MainPage), config);
        }

        private void OnCancel(object sender, RoutedEventArgs e)
        {
            var config = (Application.Current as App).Config;
            Frame.Navigate(typeof(MainPage), config);
        }
    }
}
