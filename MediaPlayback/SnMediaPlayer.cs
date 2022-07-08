using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using System.Diagnostics;
using Windows.Storage;
using Windows.Media.Playback;
using Windows.Media.Core;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using YoutubeDL;
using YoutubeDL.Models;

namespace NCSMusic
{
    public class YoutubeMediaPlayer
    {
        MediaPlayer player = new MediaPlayer();

        //YoutubeClient youtube = new YoutubeClient();
        CoreDispatcher uiThread;

        Slider slider;
        Button playButton;
        Button nextButton;
        Button previousButton;
        ToggleButton shuffleButton;
        ToggleButton repeatButton;
        ToggleButton repeatOneButton;
        ListView tracksListView;
        SymbolIcon playSymbol;

        public event EventHandler CurrentSongChanged;

        public List<YoutubeVideo> lstTracks = new List<YoutubeVideo>();
        public YoutubeVideo currentTrack;

        bool isPlaying = false;

        public YoutubeMediaPlayer(CoreDispatcher uiThread)
        {
            this.uiThread = uiThread;
            player.MediaEnded += Player_MediaEnded;
            player.PlaybackSession.PositionChanged += PlaybackSession_PositionChanged;
        }

        private async void PlaybackSession_PositionChanged(MediaPlaybackSession sender, object args)
        {
            await uiThread.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                if (slider.Value + 1 < sender.Position.TotalMilliseconds)
                {
                    slider.Maximum = sender.NaturalDuration.TotalMilliseconds;
                    slider.Value = Math.Round(sender.Position.TotalMilliseconds, 0);

                    Debug.WriteLine("Pos: " + slider.Value + " Max: " + sender.NaturalDuration.TotalMilliseconds);
                }
            });
        }

        private async void Player_MediaEnded(MediaPlayer sender, object args)
        {
            await uiThread.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                isPlaying = false;
                switch (repeatMode)
                {
                    case RepeatMode.None:
                        break;
                    case RepeatMode.RepeatOne:
                        Play(currentTrack);
                        break;
                    case RepeatMode.Repeat:
                        NextButton_Click(null, null);
                        break;
                    case RepeatMode.Shuffle:
                        NextButton_Click(null, null);
                        break;
                }
            });
        }

        public Slider ProgressSlider
        {
            get
            {
                return slider;
            }
            set
            {
                slider = value;
                slider.ValueChanged += Slider_ValueChanged;
            }
        }

        public Button PlayButton
        {
            get
            {
                return nextButton;
            }
            set
            {
                nextButton = value;
                nextButton.Click += NextButton_Click;
            }
        }

        public ListView TracksListView
        {
            get
            {
                return tracksListView;
            }
            set
            {
                tracksListView = value;
                tracksListView.SelectionChanged += TracksListView_SelectionChanged;
            }
        }

        private void TracksListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            throw new NotImplementedException();
        }

        public Button PreviousButton
        {
            get
            {
                return previousButton;
            }
            set
            {
                previousButton = value;
                previousButton.Click += PreviousButton_Click;
            }
        }

        public Button NextButton
        {
            get
            {
                return playButton;
            }
            set
            {
                playButton = value;
                playButton.Click += PlayButton_Click;
            }
        }

        public ToggleButton ShuffleButton
        {
            get
            {
                return shuffleButton;
            }
            set
            {
                shuffleButton = value;
                shuffleButton.Click += ShuffleButton_Click;
            }
        }

        public ToggleButton RepeatButton
        {
            get
            {
                return repeatButton;
            }
            set
            {
                repeatButton = value;
                repeatButton.Click += RepeatButton_Click;
            }
        }

        public ToggleButton RepeatOneButton
        {
            get
            {
                return repeatOneButton;
            }
            set
            {
                repeatOneButton = value;
                repeatOneButton.Click += RepeatOneButton_Click;
            }
        }

        public SymbolIcon PlaySymbol
        {
            get
            {
                return playSymbol;
            }
            set
            {
                playSymbol = value;
            }
        }

        public RepeatMode repeatMode { get; set; }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (isPlaying)
            {
                player.Pause();
                playSymbol.Symbol = Symbol.Play;
                playSymbol.UpdateLayout();
                isPlaying = false;
            }
            else
            {
                player.Play();
                playSymbol.Symbol = Symbol.Pause;
                playSymbol.UpdateLayout();
                isPlaying = true;
            }
        }

        public async Task LoadPlaylist(string playlistId)
        {
           // Playlist s = await youtube.GetPlaylistAsync(playlistId);
            //lstTracks = YoutubeVideo.GetYoutubeVideos(this,s.Videos.ToArray());
        }

        private void Slider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (slider.Value + 1000 < player.PlaybackSession.Position.TotalMilliseconds || slider.Value - 1000 > player.PlaybackSession.Position.TotalMilliseconds)
            {
                Debug.WriteLine("Set: " + player.PlaybackSession.Position.TotalMilliseconds + " to " + slider.Value);
                player.PlaybackSession.Position = new TimeSpan(0, 0, 0, 0, (int)slider.Value);
            }
        }

        public YoutubeVideo GetVideoByTitle(string title)
        {
            return lstTracks.First(x => x.Title == title);
        }

        public YoutubeVideo GetVideoById(string videoId)
        {
            return lstTracks.First(x => x.VideoId == videoId);
        }

        public async void Play(YoutubeVideo video)
        {
            //MediaStreamInfoSet set = await youtube.GetVideoMediaStreamInfosAsync(video.VideoId);
            //MuxedStreamInfo info = set.Muxed.WithHighestVideoQuality();
            //Uri streamUri = new Uri(info.Url);
            //MediaSource source = MediaSource.CreateFromUri(streamUri);

            //player.Source = source;
            player.Play();
            currentTrack = video;
            isPlaying = true;
            CurrentSongChanged?.Invoke(null,null);
        }

        private void RepeatOneButton_Click(object sender, RoutedEventArgs e)
        {
            if (repeatOneButton.IsChecked == true)
            {
                repeatMode = RepeatMode.RepeatOne;
                shuffleButton.IsChecked = false;
                repeatButton.IsChecked = false;
            }
            else
            {
                repeatMode = RepeatMode.None;
            }
        }

        private void RepeatButton_Click(object sender, RoutedEventArgs e)
        {
            if (repeatButton.IsChecked == true)
            {
                repeatMode = RepeatMode.Repeat;
                repeatOneButton.IsChecked = false;
                shuffleButton.IsChecked = false;
            }
            else
            {
                repeatMode = RepeatMode.None;
            }
        }

        private void ShuffleButton_Click(object sender, RoutedEventArgs e)
        {
            if (shuffleButton.IsChecked == true)
            {
                repeatMode = RepeatMode.Shuffle;
                repeatOneButton.IsChecked = false;
                repeatButton.IsChecked = false;
            }
            else
            {
                repeatMode = RepeatMode.None;
            }
        }

        private void PreviousButton_Click(object sender, RoutedEventArgs e)
        {
            if (lstTracks.Count <= 1) { return; }
            int cindex = lstTracks.IndexOf(currentTrack);
            if (cindex - 1 == -1)
            {
                Play(lstTracks[lstTracks.Count - 1]);
            }
            else
            {
                Play(lstTracks[cindex - 1]);
            }
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            if (lstTracks.Count <= 1) { return; }
            int cindex = lstTracks.IndexOf(currentTrack);
            if (cindex + 1 == lstTracks.Count)
            {
                if (repeatMode == RepeatMode.Shuffle)
                {
                    Play(lstTracks[new Random().Next(0,lstTracks.Count - 1)]);
                }
                else
                {
                    Play(lstTracks[0]);
                }
            }
            else
            {
                if (repeatMode == RepeatMode.Shuffle)
                {
                    Play(lstTracks[new Random().Next(0, lstTracks.Count - 1)]);
                }
                else
                {
                    Play(lstTracks[cindex + 1]);
                }
            }
        }

    }

    public class YoutubeVideo
    {
        YoutubeMediaPlayer player;
        Video video;

        public string Title
        {
            get
            {
                return video.Title;
            }
        }

        public string VideoId
        {
            get
            {
                return video.Id;
            }
        }

        public YoutubeVideo(YoutubeMediaPlayer player, Video video)
        {
            this.player = player;
            this.video = video;
        }

        public static List<YoutubeVideo> GetYoutubeVideos(YoutubeMediaPlayer player, Video[] videos)
        {
            List<YoutubeVideo> lstv = new List<YoutubeVideo>();
            foreach (Video video in videos)
            {
                YoutubeVideo v = new YoutubeVideo(player,video);
                lstv.Add(v);
            }
            return lstv;
        }

        public void Play()
        {
            player.Play(this);
        }
    }

    public enum RepeatMode
    {
        None,
        Repeat,
        RepeatOne,
        Shuffle
    }
}
