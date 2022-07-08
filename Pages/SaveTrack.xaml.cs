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
using System.Threading;
using System.Threading.Tasks;
using YoutubeDL.Models;
using YoutubeDL;
using System.Diagnostics;
using Windows.UI.Popups;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Core;
using System.Windows.Input;
using System.Reflection;

// Die Elementvorlage "Inhaltsdialogfeld" wird unter https://go.microsoft.com/fwlink/?LinkId=234238 dokumentiert.

namespace NCSMusic
{
    public sealed partial class SaveTrack : ContentDialog
    {
        public TrackX track { get; set; }

        public YouTubeDL client { get; set; } = YouTubeApi.yt;

        public YouTubeApi ytapi { get; set; }

        public MediaFormat format { get; set; }
        public int videoQuality { get; set; }

        public VideoProcessMode mode { get; set; }

        public SaveTrack(TrackX track)
        {
            this.track = track;

            this.InitializeComponent();

            InitAsync();
        }

        private async void Update(object sender, SelectionChangedEventArgs e)
        {
            if (cbFormat.SelectedItem == null)
            {
                return;
            }

            format = (MediaFormat)cbFormat.SelectedItem;

            if (MediaConverter.isVideoType(format))
            {
                cbQuality.IsEnabled = true;

                
            }
            else
            {
                cbQuality.IsEnabled = false;
            }

            if (cbQuality.SelectedItem != null)
            {
                videoQuality = (int)cbQuality.SelectedItem;
            }


            mode = await GetVideoProcessMode(videoQuality, format);

            Debug.WriteLine(mode);

            switch (mode)
            {
                case VideoProcessMode.None:
                    lblWarning.Visibility = Visibility.Collapsed;
                    break;
                case VideoProcessMode.Convert:
                    lblWarning.Text = "Requires Conversion";
                    lblWarning.Visibility = Visibility.Visible;
                    break;
                case VideoProcessMode.Merge:
                    lblWarning.Text = "Requires Merge";
                    lblWarning.Visibility = Visibility.Visible;
                    break;
                case VideoProcessMode.MergeAndConvert:
                    lblWarning.Text = "Requires Conversion and Merge. This can take up to a minute!";
                    lblWarning.Visibility = Visibility.Visible;
                    break;
            }
        }

        public async void InitAsync()
        {
            var qualities = (await track.GetOrFetchVideo(true)).Formats.WithVideo().Select(x => x.Height);
            cbQuality.ItemsSource = qualities;

            IEnumerable<MediaFormat> mediaFormats = Enum.GetValues(typeof(MediaFormat)).Cast<MediaFormat>();
            cbFormat.ItemsSource = mediaFormats.ToList();

            cbQuality.SelectionChanged += Update;
            cbFormat.SelectionChanged += Update;
        }

        public async Task<VideoProcessMode> GetVideoProcessMode(int vq, MediaFormat f)
        {
            if (MediaConverter.isVideoType(f))
            {
                if ((await track.GetOrFetchVideo(true)).Formats.GetMuxedFormats().Any(x => x.Height == vq))
                {
                    if (f == MediaFormat.mp4)
                    {
                        return VideoProcessMode.None;
                    }
                    else
                    {
                        return VideoProcessMode.Convert;
                    }
                }
                else
                {
                    if (f == MediaFormat.mp4)
                    {
                        return VideoProcessMode.Merge;
                    }
                    else
                    {
                        return VideoProcessMode.MergeAndConvert;
                    }
                }
            }
            else
            {
                if (f == MediaFormat.aac && (await track.GetOrFetchVideo(true)).Formats.WithAudio().Any(x => x.AudioCodec == "m4a" || x.AudioCodec == "aac"))
                {
                    return VideoProcessMode.None;
                }
                else if (f == MediaFormat.m4a && (await track.GetOrFetchVideo(true)).Formats.WithAudio().Any(x => x.AudioCodec == "m4a" || x.AudioCodec == "aac"))
                {
                    return VideoProcessMode.None;
                }
                else if (f == MediaFormat.webm && (await track.GetOrFetchVideo(true)).Formats.WithAudio().Any(x => x.AudioCodec == "opus"))
                {
                    return VideoProcessMode.None;
                }
                else if (f == MediaFormat.ogg && (await track.GetOrFetchVideo(true)).Formats.WithAudio().Any(x => x.AudioCodec == "vorbis"))
                {
                    return VideoProcessMode.None;
                }
                else
                {
                    Debug.WriteLine("mp3 stream bo f");
                    return VideoProcessMode.Convert;
                }
            }
        }

        private async void btnStart_Click(object sender, RoutedEventArgs args)
        {
            StorageFolder appfolder;

            try
            {
                appfolder = await ApplicationData.Current.RoamingFolder.GetFolderAsync("ytmusic");
            }
            catch
            {
                appfolder = await ApplicationData.Current.RoamingFolder.CreateFolderAsync("ytmusic");
            }

            FileSavePicker savePicker = new FileSavePicker();
            savePicker.SuggestedStartLocation = PickerLocationId.MusicLibrary;
            // Dropdown of file types the user can save the file as
            savePicker.FileTypeChoices.Add(format.ToString(), new List<string>() { "." + format.ToString() });
            // Default file name if the user does not type one in or select a file to replace
            savePicker.SuggestedFileName = track.Title.Trim(Path.GetInvalidFileNameChars());



            StorageFile f = await savePicker.PickSaveFileAsync();

            if (f == null) return;

            Debug.WriteLine(f.ContentType);

            string fileType = f.FileType.Substring(1);
            Debug.WriteLine(fileType);

            Debug.WriteLine(ApplicationData.Current.RoamingFolder.Path);
            Debug.WriteLine(f.Path);

            ytapi = new YouTubeApi();


            string formatSpec = $"best[height={videoQuality}]/bestvideo[height={videoQuality}]+bestaudio";

            await track.DownloadYTVideo(f, formatSpec, new Action<double>(async delegate (double progr)
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    pbProgress.Value = progr * 100;
                });
            }));

            this.Hide();
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {

        }
    }

    public enum VideoProcessMode
    {
        None,
        Convert,
        Merge,
        MergeAndConvert
    }
}
