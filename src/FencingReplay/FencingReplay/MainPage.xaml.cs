﻿using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System.Display;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace FencingReplay
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        class VideoChannel
        {
            MainPage mainPage;
            int gridColumn;
            ListBox sourceSelector;
            CaptureElement captureElement;
            MediaFrameSourceGroup currentSource;
            MediaCapture currentCapture;
            DisplayRequest currentRequest;

            static IReadOnlyList<MediaFrameSourceGroup> CurrentSources;

            internal VideoChannel(MainPage page, int column)
            {
                mainPage = page;
                gridColumn = column;
                sourceSelector = new ListBox();
                PopulateSourceList(sourceSelector);
                mainPage.LayoutGrid.Children.Add(sourceSelector);
                Grid.SetColumn(sourceSelector, gridColumn);
                Grid.SetRow(sourceSelector, 0);
                captureElement = new CaptureElement();
                mainPage.LayoutGrid.Children.Add(captureElement);
                Grid.SetColumn(captureElement, gridColumn);
                Grid.SetRow(captureElement, 1);
            }

            async void SetSource(string displayName)
            {
                var sourceGroup = FindMediaSource(displayName);
            }

            static MediaFrameSourceGroup FindMediaSource(string displayName)
            {
                // This method will never get called when CurrentSources is uninitialized
                foreach (var frameSource in CurrentSources)
                {
                    if (displayName == frameSource.DisplayName)
                    {
                        return frameSource;
                    }
                }
                return null;
            }

            static async void PopulateSourceList(ListBox listBox)
            {
                if (CurrentSources == null)
                {
                    CurrentSources = await MediaFrameSourceGroup.FindAllAsync();

                    //FileSavePicker savePicker = new FileSavePicker();
                    //savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                    //savePicker.FileTypeChoices.Add("Plain Text", new List<string>() { ".txt" });
                    //savePicker.SuggestedFileName = "sources";

                    //StorageFile file = await savePicker.PickSaveFileAsync();
                    //if (file != null)
                    //{
                    //    using (var buffer = new StringWriter())
                    //    {
                    //        foreach (var frameSource in frameSources)
                    //        {
                    //            buffer.WriteLine($"Source group {frameSource.DisplayName}:");
                    //            buffer.WriteLine($"  ID: {frameSource.Id}");
                    //            foreach (var src in frameSource.SourceInfos)
                    //            {
                    //                buffer.WriteLine($"    Source ID: {src.Id}");
                    //                buffer.WriteLine($"      Source Kind: {src.SourceKind}");
                    //                buffer.WriteLine($"      Stream type: {src.MediaStreamType}");
                    //            }
                    //        }
                    //        await FileIO.WriteTextAsync(file, buffer.ToString());
                    //    }
                    //}    
                }

                listBox.Items.Clear();
                foreach (var frameSource in CurrentSources)
                {
                    listBox.Items.Add(frameSource.DisplayName);
                } 
            }
        }

        bool playing = false;
        List<VideoChannel> channels;
        

        public MainPage()
        {
            this.InitializeComponent();

            channels = new List<VideoChannel>();
            channels.Add(new VideoChannel(this, 0));
            channels.Add(new VideoChannel(this, 1));
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
        }

        private void ListBox1_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selection = ListBox1.SelectedItem?.ToString();
            SetVideoSource(Capture1, selection);
        }

        private void ListBox2_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private MediaFrameSourceGroup FindMediaSource(string displayName)
        {
            // We have to use the blocking GetResults to transition from asynch to synch
            var frameSources = MediaFrameSourceGroup.FindAllAsync().GetResults();
            foreach (var frameSource in frameSources)
            {
                if (displayName == frameSource.DisplayName)
                {
                    return frameSource;
                }
            }
            return null;
        }

        private void SetVideoSource(CaptureElement captureElement, string displayName)
        {

        }
    }
}