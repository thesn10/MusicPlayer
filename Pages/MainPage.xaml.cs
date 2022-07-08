using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.UI.Popups;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Media.Playback;
using Windows.Media.Core;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using System.Collections.ObjectModel;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using YoutubeDL;
using YoutubeDL.Models;
using System.Reflection;
using Windows.Media.Audio;
using Windows.UI.Xaml.Shapes;
using System.Runtime.InteropServices;
//using AudioEffectsRT;
using System.Net.Http;

// Die Elementvorlage "Leere Seite" wird unter https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x407 dokumentiert.

namespace NCSMusic
{
    /// <summary>
    /// Eine leere Seite, die eigenständig verwendet oder zu der innerhalb eines Rahmens navigiert werden kann.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        int height = (int)Window.Current.Bounds.Height;
        ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
        MediaPlayer player;
        bool isPlaying = false;
        bool enableMusicEffects = false;
        CoreDispatcher d;
        string ncsplaylist = "PLRBp0Fe2GpgnIh0AiYKh7o7HnYAej-5ph";
        TrackX currentTrack;

        static ObservableCollection<TrackX> favs = new ObservableCollection<TrackX>();

        YouTubeApi ytapi = new YouTubeApi();
        //0 = None
        //1 = RepeatOne
        //2 = RepeatAll
        //3 = Shuffle
        int repeatIndex
        {
            get
            {
                if (localSettings.Values.ContainsKey("rindex"))
                {
                    return (int)localSettings.Values["rindex"];
                }
                return 0;
            }
            set
            {
                localSettings.Values["rindex"] = value;
            }
        }

        bool showplayer = true;

        public MainPage()
        {
            this.InitializeComponent();


            //UUtlpLVXVNFws_B9_fdGSW9Q
            Debug.WriteLine(ApplicationData.Current.LocalFolder.Path);
            Task t = Task.CompletedTask;

            d = Dispatcher;

            if (localSettings.Containers.ContainsKey("playlistSettings"))
            {
                LoadPlaylistsFromSettings();

            }


            if (localSettings.Values.ContainsKey("rindex"))
            {
                UpdateRButtons();
            }

            if (localSettings.Values.ContainsKey("dlogo"))
            {
                if ((bool)localSettings.Values["dlogo"])
                {
                    ibSn.Opacity = 0.1;
                }
                else
                {
                    ibSn.Opacity = 0;
                }
            }

            if (localSettings.Values.ContainsKey("enableMusicEffects"))
            {
                enableMusicEffects = (bool)localSettings.Values["enableMusicEffects"];
            }

            player = new MediaPlayer();

            if (enableMusicEffects)
            {
                //FFTAudioEffect.player = player;
                //FFTAudioEffect.SpectrumDataReady += FFTAudioEffect_SpectrumDataReady;
                //GenerateBands(50);
            }

            /*
            if (localSettings.Values.ContainsKey("enableMusicEffects"))
            {
                if ((bool)localSettings.Values["enableMusicEffects"])
                {
                    enableMusicEffects = true;
                    FFTAudioEffect.player = player;
                    FFTAudioEffect.SpectrumDataReady += FFTAudioEffect_SpectrumDataReady;
                    GenerateBands(50);
                }
                else
                {
                    enableMusicEffects = false;
                }
            }*/


            //PrintTypes();


            if (localSettings.Values.ContainsKey("volume"))
            {
                double volume = (double)localSettings.Values["volume"];
                sldrVol.Value = volume;
            }

            btnList.Visibility = Visibility.Collapsed;
            btnPlay.Visibility = Visibility.Collapsed;
            lblSongName.Visibility = Visibility.Collapsed;
            mediaPlayerScreen.Visibility = Visibility.Visible;

            btnPlay.Click += BtnPlay_Click;
            btnNext.Click += BtnNext_Click;
            btnSettings.Click += BtnSettings_Click;
            btnList.Click += BtnList_Click;
            player.MediaEnded += Player_MediaEnded;
            btnPrevious.Click += BtnPrevious_Click;
            btnRepeatOne.Click += BtnRepeatOne_Click;
            sldrTime.ValueChanged += SldrTime_ValueChanged;
            lstVwSearch.SelectionChanged += LstVwTracks_SelectionChanged;
            sldrVol.ValueChanged += SldrVol_ValueChanged;
            tswStreamType.Toggled += TswStreamType_Toggled;
            txtBxSearch.KeyDown += TxtBxSearch_KeyDown;

            player.PlaybackSession.PositionChanged += PlaybackSession_PositionChanged;

            mediaPlayerScreen.SetMediaPlayer(player);
            sldrTime.Value = 5000;
            getMusic();
            lstVwSearch.SelectionMode = ListViewSelectionMode.Single;
        }

        List<Rectangle> lstBands = new List<Rectangle>();

        public void GenerateBands(int amount)
        {
            if (lstBands != null)
            {
                foreach (Rectangle rect in lstBands)
                {
                    rpAudioSpectrum.Children.Remove(rect);
                }
                lstBands.Clear();
                GC.Collect();
            }

            lstBands = new List<Rectangle>();

            double width = 10;
            double gapwidth = 5;

            for (int i = 1; i < amount+1; i++)
            {
                Rectangle rect = new Rectangle();
                rect.Height = 0;
                rect.Width = width;
                //rect.Opacity = 0.5;
                rect.Opacity = 1;
                rect.IsHitTestVisible = false;


                rect.HorizontalAlignment = HorizontalAlignment.Left;
                rect.VerticalAlignment = VerticalAlignment.Bottom;
                rect.SetValue(RelativePanel.AlignRightWithPanelProperty, true);

                double twidth = width + gapwidth;

                rect.Margin = new Thickness(0, 0, (twidth * (amount - i)), -300);

                AcrylicBrush awb = (AcrylicBrush)Application.Current.Resources["SystemControlAccentAcrylicWindowAccentMediumHighBrush"];
                SolidColorBrush scb = new SolidColorBrush(Windows.UI.Colors.Blue);

                rect.Fill = awb;
                rpAudioSpectrum.Children.Add(rect);

                lstBands.Add(rect);
            }
        }

        
        private async void FFTAudioEffect_SpectrumDataReady(object sender, object e)
        {
            //Debug.WriteLine("YAH");
            Dispatcher.RunAsync(CoreDispatcherPriority.High, delegate ()
            {
                double[] audioData = new double[1];
                try
                {
                    audioData = (double[])e;
                }
                catch
                {
                    return;
                }

                //SolidColorBrush awb = (SolidColorBrush)Application.Current.Resources["ApplicationPageBackgroundThemeBrush"];
                //Windows.UI.Color c = awb.Color;
                //c.R = (byte)(audioData[10] * 100);
                //c.G = (byte)(audioData[12] * 100);
                //c.B = (byte)(audioData[8] * 100);
                //this.Background = new SolidColorBrush(c);

                for (int i = 0; i < audioData.Length; i += 1)
                {
                    //double logamp = 100 + (100 * Math.Log10(e.AudioData[i] / 100));
                    double height = (audioData[i] * 300);// *10000;//(e.AudioData[i] * polyheight) / (capture.WaveFormat.SampleRate *100);
                                                       //Debug.WriteLine(height);
                                                       //Debug.WriteLine(height);
                                                       //height = 30* Math.Log(height);

                    if (height < 0)
                    {
                        height = 0;
                        //height = Math.Abs(height);
                    }
                    else if (height > 300)
                    {
                        height = 300;
                        //height = Math.Abs(height);
                    }


                    lstBands[i].Height = height;

                }
            });
        }

        private async void BtnLoadMore_Click(object sender, RoutedEventArgs e)
        {

            Button button = (Button)sender;

            int page;
            if (button.Tag == null)
            {
                page = 2;
                button.Tag = page;
            }
            else
            {
                page = (int)button.Tag + 1;
                button.Tag = page;
            }

            InfoDict searchResult = await ytapi.SearchVideosAsync(txtBxSearch.Text, 20 * page); // page
            List<InfoDict> sResult = (searchResult as Playlist).Entries;

            if (!sResult.Any()) return;

            
            
            for (int i = 0; i < lstVwSearch.Items.Count; i++)
            {
                //Remove items that are already in the list
                Debug.WriteLine("Removing");
                try
                {
                    Debug.WriteLine("Removing " + i);
                    sResult.RemoveAt(i);
                }
                catch
                {

                }
            }


            foreach (ContentUrl v in sResult)
            {
                //add items
                TrackX result = downloadedTracks.Find(x => x.VideoID == (string)v.AdditionalProperties["id"]);
                if (result == default(TrackX))
                {
                    lstVwSearch.Items.Add(new TrackX(v));
                }
                else 
                {
                    lstVwSearch.Items.Add(result);
                }
            }
        }

        List<TrackX> downloadedTracks = new List<TrackX>();

        public async Task LoadDownloadedTracks(string location)
        {
            try
            {
                StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(location);
                downloadedTracks.Clear();
                downloadedTracks.AddRange(await TrackX.FromSNMetaFolder(folder));
                Debug.WriteLine("Downloaded Tracks: " + downloadedTracks.Count);
            } catch { }
        }

        public async Task InitAsync()
        {
            //UUtlpLVXVNFws_B9_fdGSW9Q
            //this.InitializeComponent();
            Debug.WriteLine(ApplicationData.Current.LocalFolder.Path);

            if (localSettings.Values.ContainsKey("txtBxDownloadLocation"))
            {
                string dlloc = (string)ApplicationData.Current.LocalSettings.Values["txtBxDownloadLocation"];
                await LoadDownloadedTracks(dlloc);
            }

            if (localSettings.Containers.ContainsKey("fav"))
            {
                lstVwFav.ItemsSource = favs;
                await LoadFavsSync();
            }

            d = Dispatcher;

            if (localSettings.Containers.ContainsKey("playlistSettings"))
            {
                await LoadPlaylistsFromSettings();

            }


            if (localSettings.Values.ContainsKey("rindex"))
            {
                UpdateRButtons();
            }

            if (localSettings.Values.ContainsKey("dlogo"))
            {
                if ((bool)localSettings.Values["dlogo"])
                {
                    ibSn.Opacity = 0;
                }
                else
                {
                    ibSn.Opacity = 0.1;
                }
            }

            player = new MediaPlayer();

            if (localSettings.Values.ContainsKey("enableMusicEffects"))
            {
                if ((bool)localSettings.Values["enableMusicEffects"])
                {
                    enableMusicEffects = true;
                    //FFTAudioEffect.player = player;
                    //FFTAudioEffect.SpectrumDataReady += FFTAudioEffect_SpectrumDataReady;
                    //GenerateBands(50);
                }
                else
                {
                    enableMusicEffects = false;
                }
            }


            //PrintTypes();


            if (localSettings.Values.ContainsKey("volume"))
            {
                double volume = (double)localSettings.Values["volume"];
                sldrVol.Value = volume;
            }

            btnList.Visibility = Visibility.Collapsed;
            btnPlay.Visibility = Visibility.Collapsed;
            lblSongName.Visibility = Visibility.Collapsed;
            mediaPlayerScreen.Visibility = Visibility.Visible;

            btnPlay.Click += BtnPlay_Click;
            btnNext.Click += BtnNext_Click;
            btnSettings.Click += BtnSettings_Click;
            btnList.Click += BtnList_Click;
            player.MediaEnded += Player_MediaEnded;
            btnPrevious.Click += BtnPrevious_Click;
            btnRepeatOne.Click += BtnRepeatOne_Click;
            sldrTime.ValueChanged += SldrTime_ValueChanged;
            lstVwSearch.SelectionChanged += LstVwTracks_SelectionChanged;
            sldrVol.ValueChanged += SldrVol_ValueChanged;
            tswStreamType.Toggled += TswStreamType_Toggled;
            txtBxSearch.KeyDown += TxtBxSearch_KeyDown;

            player.PlaybackSession.PositionChanged += PlaybackSession_PositionChanged;

            mediaPlayerScreen.SetMediaPlayer(player);
            sldrTime.Value = 5000;
            getMusic();
            lstVwSearch.SelectionMode = ListViewSelectionMode.Single;
        }

        private async void TxtBxSearch_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                TextBox b = (TextBox)sender;
                string query = b.Text;

                if (query.Length == 11)
                {
                    TrackX tx = null;
                    try
                    {
                        TrackX result = downloadedTracks.Find(x => x.VideoID == query);
                        if (result == default(TrackX))
                        {
                            tx = new TrackX((await YouTubeApi.yt.ExtractInfoAsync(query) as Video));
                            lstVwSearch.Items.Add(tx);
                        }
                        else
                        {
                            lstVwSearch.Items.Add(result);
                        }
                        return;
                    }
                    catch { }
                }

                InfoDict searchResult = await ytapi.SearchVideosAsync(txtBxSearch.Text, 20);
                List<InfoDict> sResult = (searchResult as Playlist).Entries;

                if (!sResult.Any()) return;

                lstVwSearch.Items.Clear();

                foreach (ContentUrl v in sResult)
                {
                    //add items
                    TrackX result = downloadedTracks.Find(x => x.VideoID == (string)v.AdditionalProperties["id"]);
                    if (result == default(TrackX))
                    {
                        lstVwSearch.Items.Add(new TrackX(v));
                    }
                    else
                    {
                        lstVwSearch.Items.Add(result);
                    }
                }

            }
        }

        private void TswStreamType_Toggled(object sender, RoutedEventArgs e)
        {
            if (tswStreamType.IsOn)
            {
                btnList.Visibility = Visibility.Visible;
            }
            else
            {
                rpScreen.Visibility = Visibility.Collapsed;
                symScreen.Symbol = Symbol.SlideShow;
                btnList.Visibility = Visibility.Collapsed;
            }
        }

        public void PrintTypes()
        {
            // We get the current assembly through the current class
            var currentAssembly = this.GetType().GetTypeInfo().Assembly;

            // we filter the defined classes according to the interfaces they implement
            var iDisposableAssemblies = currentAssembly.DefinedTypes.Where(type => type.ImplementedInterfaces.Any(inter => inter == typeof(IStorageFile))).ToList();
            Debug.WriteLine("sddddddddddddddddddddddddddddddd");
            foreach (TypeInfo t in iDisposableAssemblies)
            {
                Debug.WriteLine(t.FullName);
            }

        }

        private async Task<IEnumerable<Assembly>> GetAssemblyListAsync()
        {
            var folder = Windows.ApplicationModel.Package.Current.InstalledLocation;

            List<Assembly> assemblies = new List<Assembly>();
            foreach (Windows.Storage.StorageFile file in await folder.GetFilesAsync())
            {
                try
                {
                    if (file.FileType == ".dll" || file.FileType == ".exe")
                    {
                        AssemblyName name = new AssemblyName()
                        {
                            Name = System.IO.Path.GetFileNameWithoutExtension(file.Name)
                        };
                        Assembly asm = Assembly.Load(name);
                        assemblies.Add(asm);
                    }
                }
                catch { }
            }

            return assemblies;
        }

        private void SldrVol_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            player.Volume = sldrVol.Value / 100;
            localSettings.Values["volume"] = sldrVol.Value;
        }

        private void BtnList_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("CLICKED");

            if (rpScreen.Visibility == Visibility.Collapsed)
            {
                rpScreen.Visibility = Visibility.Visible;
                symScreen.Symbol = Symbol.MusicInfo;
            }
            else
            {
                rpScreen.Visibility = Visibility.Collapsed;
                symScreen.Symbol = Symbol.SlideShow;
            }

            //sbDetails.Duration = new Duration(TimeSpan.FromSeconds(10));
            //sbDetails.Begin();
            //Debug.WriteLine("Ani");


            /*
            TimeSpan t = player.PlaybackSession.Position;
            IMediaPlaybackSource s = player.Source;
            player.Pause();
            player.Source = null;
            mediaPlayerScreen.Visibility = Visibility.Collapsed;
            mediaPlayerScreen.SetMediaPlayer(new MediaPlayer());
            mediaPlayerScreen.InvalidateArrange();
            ListViewItem item = (ListViewItem)lstVwTracks.SelectedItem;
            PlaylistItem d = (PlaylistItem)item.Tag;
            Uri vuri = await YouTubeApi.GetVideoLink(d.ContentDetails.VideoId);
            player.Source = MediaSource.CreateFromUri(vuri);
            player.Play();
            player.Play();*/
            //player.PlaybackSession.Position = t;

            //mediaPlayerScreen.SetMediaPlayer(null);
        }

        private void SldrTime_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (sldrTime.Value + 1000 < player.PlaybackSession.Position.TotalMilliseconds || sldrTime.Value - 1000 > player.PlaybackSession.Position.TotalMilliseconds)
            {
                Debug.WriteLine("Set: " + player.PlaybackSession.Position.TotalMilliseconds + " to " + sldrTime.Value);
                player.PlaybackSession.Position = new TimeSpan(0, 0, 0, 0, (int)sldrTime.Value);
            }
        }

        public void UpdateRButtons()
        {
            switch (repeatIndex)
            {
                case 0:
                    btnRepeatOne.IsChecked = false;
                    symRepeatOne.Symbol = Symbol.RepeatAll;
                    break;
                case 1:
                    btnRepeatOne.IsChecked = true;
                    symRepeatOne.Symbol = Symbol.RepeatAll;
                    break;
                case 2:
                    btnRepeatOne.IsChecked = true;
                    symRepeatOne.Symbol = Symbol.RepeatOne;
                    break;
                case 3:
                    btnRepeatOne.IsChecked = true;
                    symRepeatOne.Symbol = Symbol.Shuffle;
                    break;
            }
        }

        private async void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            SettingsDialog d = new SettingsDialog();
            ContentDialogResult r = await d.ShowAsync();
            if (r == ContentDialogResult.Primary)
            {
                if (localSettings.Containers.ContainsKey("playlistSettings"))
                {
                    foreach (PivotItem p in playlistPivot.Items)
                    {
                        if ((string)p.Header != "Search" && (string)p.Header != "Favorites")
                        {
                            playlistPivot.Items.Remove(p);
                        }
                    }


                    await LoadPlaylistsFromSettings();

                }
            }
        }

        private void BtnRepeatOne_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Checked: " + btnRepeatOne.IsChecked + ", Symbol: " + symRepeatOne.Symbol);
            if (btnRepeatOne.IsChecked == false)
            {
                switch (symRepeatOne.Symbol)
                {
                    case Symbol.RepeatAll:
                        repeatIndex = 2;
                        btnRepeatOne.IsChecked = true;
                        symRepeatOne.Symbol = Symbol.RepeatOne;
                        btnRepeatOne.Content = symRepeatOne;
                        Debug.WriteLine("Changed to: " + symRepeatOne.Symbol);
                        break;
                    case Symbol.RepeatOne:
                        repeatIndex = 3;
                        btnRepeatOne.IsChecked = true;
                        symRepeatOne.Symbol = Symbol.Shuffle;
                        btnRepeatOne.Content = symRepeatOne;
                        Debug.WriteLine("Changed to: " + symRepeatOne.Symbol);
                        break;
                    case Symbol.Shuffle:
                        repeatIndex = 0;
                        btnRepeatOne.IsChecked = false;
                        symRepeatOne.Symbol = Symbol.RepeatAll;
                        btnRepeatOne.Content = symRepeatOne;
                        Debug.WriteLine("Changed to: " + symRepeatOne.Symbol);
                        break;
                }
            }
            else
            {
                repeatIndex = 1;
                btnRepeatOne.IsChecked = true;
                symRepeatOne.Symbol = Symbol.RepeatAll;
                btnRepeatOne.Content = symRepeatOne;
                Debug.WriteLine("Changed to: " + symRepeatOne.Symbol);
            }
        }

        private void BtnPrevious_Click(object sender, RoutedEventArgs e)
        {
            PivotItem item = (PivotItem)playlistPivot.SelectedItem;

            ListView lstView;
            try
            {
                lstView = (ListView)item.Content;
            }
            catch
            {
                RelativePanel p = (RelativePanel)item.Content;
                lstView = (ListView)p.Tag;
            }


            if (lstView.SelectedIndex == 0)
            {
                lstView.SelectedIndex = lstView.Items.Count - 1;
            }
            else
            {
                lstView.SelectedIndex = lstView.SelectedIndex - 1;
            }
        }

        private void BtnNext_Click(object sender, RoutedEventArgs e)
        {
            switch (repeatIndex)
            {
                default:
                    PlayNextTrack(false);
                    break;
                case 3:
                    PlayNextTrack(true);
                    break;
            }

            //TransitionCollection col = new TransitionCollection();
            //EntranceThemeTransition trans = new EntranceThemeTransition();
            //trans.FromHorizontalOffset = 1000;
            //col.Add(new EntranceThemeTransition());
            //this.Transitions = col;
            //this.Frame.Navigate(typeof(SongDetails), null, new DrillInNavigationTransitionInfo());
        }

        public void PlayNextTrack(bool random)
        {
            PivotItem item = (PivotItem)playlistPivot.SelectedItem;

            ListView lstView;
            try
            {
                lstView = (ListView)item.Content;
            }
            catch
            {
                RelativePanel p = (RelativePanel)item.Content;
                lstView = (ListView)p.Tag;
            }

            if (!random)
            {
                if ((lstView.SelectedIndex + 1) > (lstView.Items.Count - 1))
                {
                    lstView.SelectedIndex = 0;
                    lstView.ScrollIntoView(lstView.SelectedItem);
                }
                else
                {
                    lstView.SelectedIndex = lstView.SelectedIndex + 1;
                    lstView.ScrollIntoView(lstView.SelectedItem);
                }
            }
            else
            {
                lstView.SelectedIndex = new Random().Next(lstView.Items.Count);
                lstView.ScrollIntoView(lstView.SelectedItem);
            }
        }

        private async void PlaybackSession_PositionChanged(MediaPlaybackSession sender, object args)
        {
            await d.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                if (sldrTime.Value + 1 < sender.Position.TotalMilliseconds)
                {
                    sldrTime.Maximum = sender.NaturalDuration.TotalMilliseconds;
                    sldrTime.Value = Math.Round(sender.Position.TotalMilliseconds, 0);

                    //Debug.WriteLine("Pos: " + sldrTime.Value + " Max: " + sender.NaturalDuration.TotalMilliseconds);
                }
            });
        }

        private async void Player_MediaEnded(MediaPlayer sender, object args)
        {
            await d.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                isPlaying = false;
                switch (repeatIndex)
                {
                    case 0:
                        break;
                    case 1:
                        PlayNextTrack(false);
                        break;
                    case 2:
                        PlayObj(currentTrack);
                        break;
                    case 3:
                        PlayNextTrack(true);
                        break;
                }
            });
        }

        private void BtnPlay_Click(object sender, RoutedEventArgs e)
        {
            if (isPlaying)
            {
                player.Pause();
                symBtnPlay.Symbol = Symbol.Play;
                symBtnPlay.UpdateLayout();
                isPlaying = false;
            }
            else
            {
                player.Play();
                symBtnPlay.Symbol = Symbol.Pause;
                symBtnPlay.UpdateLayout();
                isPlaying = true;
            }
        }

        private void LstVwTracks_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListView x = (ListView)sender;
            if (x.SelectedItem == null)
            {
                return;
            }

            PlayObj(x.SelectedItem);
        }

        public async void PlayObj(object track)
        {
            if (track.GetType() == typeof(TrackX))
            {
                await Play((TrackX)track);
            }
            else if (track.GetType() == typeof(VideoX))
            {
                VideoX vx = (VideoX)track;
                Video v = (Video)vx.video;
                await Play(new TrackX(v));
            }
            else if (track.GetType() == typeof(Video))
            {
                Video v = (Video)track;
                await Play(new TrackX(v));
            }
            else
            {
                Debug.WriteLine(track.GetType().FullName + " is not " + typeof(VideoX).FullName + " or " + typeof(Video).FullName);
            }
        }

        public async void PlayItem(ListViewItem item)
        {
            TrackX track = (TrackX)item.Tag;
            await Play(track);
        }

        public async Task Play(TrackX track)
        {
            btnPlay.Visibility = Visibility.Visible;
            lblSongName.Visibility = Visibility.Visible;

            mediaPlayerScreen = new MediaPlayerElement();
            mediaPlayerScreen.SetMediaPlayer(player);

            //PlaylistItem video = (PlaylistItem)item.Tag;

            //Uri vuri = await YouTubeApi.GetVideoLink(video.ContentDetails.VideoId);
            try
            {
                MediaPlaybackItem itm;

                if (track.IsSaveFileLinked)
                {
                    Debug.WriteLine("Got storagefile");
                    itm = new MediaPlaybackItem(MediaSource.CreateFromStorageFile(track.SavedTrack));
                }
                else if (track.IsYTVideoLinked)
                {

                    Uri vuri = await track.GetDownloadUri(!tswStreamType.IsOn);
                    Debug.WriteLine("Got uri: " + vuri.AbsoluteUri);
                    itm = new MediaPlaybackItem(MediaSource.CreateFromUri(vuri));
                }
                else
                {
                    await track.GetYoutubeVideo();
                    Uri vuri = await track.GetDownloadUri(!tswStreamType.IsOn);
                    Debug.WriteLine("Got uri: " + vuri.AbsoluteUri);
                    itm = new MediaPlaybackItem(MediaSource.CreateFromUri(vuri));
                }

                player.RemoveAllEffects();

                if (enableMusicEffects)
                {
                    PropertySet set = new PropertySet();
                    set.Add("Attack", 0d);
                    set.Add("Decay", 65d);
                    set.Add("FreqMin", 20d);
                    set.Add("FreqMax", 200d);
                    set.Add("Sensitivity", 30d);
                    set.Add("Bands", 50);

                    await itm.Source.OpenAsync();

                    set.Add("AudioType", itm.AudioTracks.First().GetEncodingProperties().Subtype);
                    Debug.WriteLine("AT: " + itm.AudioTracks.First().GetEncodingProperties().Subtype);

                    //player.AddAudioEffect(typeof(FFTAudioEffect).FullName, false, set);
                }

                player.Source = itm;

                xlblTrackName.Text = track.Title;
                lblSongName.Text = track.Title;
                if (track.Duration.Seconds < 10)
                {
                    txtDuration.Text = track.Duration.Minutes + ":0" + track.Duration.Seconds;
                }
                else
                {
                    txtDuration.Text = track.Duration.Minutes + ":" + track.Duration.Seconds;
                }
                currentTrack = track;
                player.PlaybackSession.Position = new TimeSpan(0, 0, 0);
                player.Volume = sldrVol.Value / 100;
                sldrTime.Value = 0;
                player.Play();
                isPlaying = true;
                symBtnPlay.Symbol = Symbol.Pause;
                sldrTime.Maximum = player.PlaybackSession.NaturalDuration.TotalMilliseconds;
            }
            catch (Exception e)
            {
                var dialog = new MessageDialog("Error playing track: " + e.Message + " Source: " +  e.Source);
                await dialog.ShowAsync();
            }
        }

        public void AddToFavList(TrackX track)
        {
            Debug.WriteLine("FavVideoComplete: " + track.Title);
            TrackX result = downloadedTracks.Find(x => x.VideoID == track.VideoID);
            if (result == default(TrackX))
            {
                favs.Add(track);
            }
            else
            {
                favs.Add(result);
            }
            Debug.WriteLine("FavVideoAdded");
        }

        private async Task LoadFavs()
        {
            ApplicationDataContainer favSettings = localSettings.Containers["fav"];
            List<Task> lstTasks = new List<Task>();

            foreach (KeyValuePair<string, object> p in favSettings.Values)
            {
                if ((string)p.Value != null && (string)p.Value != "")
                {
                    Debug.WriteLine("SFAVVideo: " + (string)p.Value);

                    TrackX result = downloadedTracks.Find(x => x.VideoID == (string)p.Value);
                    if (result == default(TrackX))
                    {
                        Task<Task> t = ytapi.Fetch<Video>((string)p.Value).ContinueWith(
                        async delegate (Task<Video> v)
                        {
                            Debug.WriteLine("WWAASS: " + (string)p.Value);
                            Video vid = await v;
                            await Dispatcher.RunAsync(CoreDispatcherPriority.High, delegate
                            {
                                favs.Add(new TrackX(vid));
                                Debug.WriteLine("FavVideo: " + vid.Title);
                            });
                        });

                        lstTasks.Add(t);
                    }
                    else
                    {
                        favs.Add(result);
                    }
                }
            }

            await Task.WhenAll(lstTasks);
        }

        private async Task LoadFavsSync()
        {
            ApplicationDataContainer favSettings = localSettings.Containers["fav"];
            List<Task> lstTasks = new List<Task>();

            foreach (KeyValuePair<string, object> p in favSettings.Values)
            {
                if ((string)p.Value != null && (string)p.Value != "")
                {

                    TrackX result = downloadedTracks.Find(x => x.VideoID == (string)p.Value);
                    if (result == default(TrackX))
                    {
                        try
                        {
                            Debug.WriteLine("SFAVVideo: " + (string)p.Value);
                            Task<Video> tvid = ytapi.Fetch<Video>((string)p.Value);
                            Task whenadded = tvid.ContinueWith((t) =>
                            {
                                if (t.Exception != null) 
                                {
                                    Debug.WriteLine("ExceptionInFavVideo: " + t.Exception.GetType().Name);
                                }
                                try
                                {
                                    Debug.WriteLine("MEM: " + (string)p.Value);
                                    AddToFavList(new TrackX(t.Result));
                                }
                                catch
                                {

                                }
                            }, TaskScheduler.FromCurrentSynchronizationContext());
                            lstTasks.Add(tvid);
                        }
                        catch
                        {

                        }
                    }
                    else
                    {
                        AddToFavList(result);
                    }
                }
            }
            try
            {
                await Task.WhenAll(lstTasks).ConfigureAwait(false);
            }
            catch
            {

            }
        }

        public async Task LoadFavsSlow()
        {
            ApplicationDataContainer favSettings = localSettings.Containers["fav"];
            List<Task> lstTasks = new List<Task>();

            foreach (KeyValuePair<string, object> p in favSettings.Values)
            {
                if ((string)p.Value != null && (string)p.Value != "")
                {
                    Debug.WriteLine("SFAVVideo: " + (string)p.Value);
                    Video vid = await ytapi.Fetch<Video>((string)p.Value);
                    AddToFavList(new TrackX(vid));
                }
            }
        }

        public async Task LoadPlaylistsFromSettingsSync()
        {
            ApplicationDataContainer playlistSettings = localSettings.CreateContainer("playlistSettings", ApplicationDataCreateDisposition.Always);
            ApplicationDataContainer customPlaylist = localSettings.CreateContainer("customPlaylists", ApplicationDataCreateDisposition.Always);

            foreach (KeyValuePair<string, object> p in playlistSettings.Values)
            {
                if ((string)p.Value != null && (string)p.Value != "")
                {
                    Debug.WriteLine("Loading " + (string)p.Value);
                    await LoadPlaylistbyId((string)p.Value);
                }
            }

            foreach (KeyValuePair<string, ApplicationDataContainer> playlist in customPlaylist.Containers)
            {
                await LoadPlaylistbyADC(playlist.Value, playlist.Key);
            }
        }

        public async Task LoadPlaylistsFromSettings()
        {
            Debug.WriteLine("Loading Settings:");
            ApplicationDataContainer playlistSettings = localSettings.CreateContainer("playlistSettings", ApplicationDataCreateDisposition.Always);
            ApplicationDataContainer customPlaylist = localSettings.CreateContainer("customPlaylists", ApplicationDataCreateDisposition.Always);
            List<Task> lstTasks = new List<Task>();

            foreach (KeyValuePair<string, object> p in playlistSettings.Values)
            {
                if ((string)p.Value != null && (string)p.Value != "")
                {
                    Debug.WriteLine("Loading " + (string)p.Value);
                    lstTasks.Add(LoadPlaylistbyId((string)p.Value));
                }
            }

            foreach (KeyValuePair<string, ApplicationDataContainer> playlist in customPlaylist.Containers)
            {
                lstTasks.Add(LoadPlaylistbyADC(playlist.Value, playlist.Key));
            }

            await Task.WhenAll(lstTasks);
            pring.IsActive = false;
            pring.Visibility = Visibility.Collapsed;
        }

        public async Task LoadPlaylistbyId(string playlistId)
        {
            try
            {
                Debug.WriteLine("pid " + playlistId);
                Task<Playlist> t = ytapi.Fetch<Playlist>(playlistId);
                Debug.WriteLine("2");
                ListView view = new ListView();
                Debug.WriteLine("3");
                view.Name = "lstVwPlaylist";
                view.SelectionChanged += LstVwTracks_SelectionChanged;
                view.ItemTemplate = dtListView;
                view.ItemContainerStyle = styleListView;
                view.Background = ibSn;
                Debug.WriteLine("4");
                Playlist p;
                try
                {
                    p = await t;
                }
                catch (HttpRequestException)
                {
                    return;
                }

                Debug.WriteLine("4.5");

                foreach (Video v in p.Entries)
                {
                    Debug.WriteLine("4.6: " + v.Title);
                    TrackX result = downloadedTracks.Find(x => x.VideoID == v.Id);
                    if (result == default(TrackX))
                    {
                        view.Items.Add(new TrackX(v));
                    }
                    else
                    {
                        result.AddYTVideoDefinition(v);
                        view.Items.Add(result);
                    }
                }

                foreach (object v in p.Entries)
                {
                    if (v is ContentUrl curl)
                    {
                        string title = (string)curl.AdditionalProperties["title"];
                        string id = (string)curl.AdditionalProperties["id"];
                        Debug.WriteLine("4.6: " + title);
                        TrackX result = downloadedTracks.Find(x => x.VideoID == id);
                        if (result == default(TrackX))
                        {
                            view.Items.Add(new TrackX(curl));
                        }
                        else
                        {
                            view.Items.Add(result);
                        }
                    }
                }

                Debug.WriteLine("5");

                PivotItem pi = new PivotItem();
                pi.Content = view;
                pi.Header = p.Title;
                Debug.WriteLine("6");
                playlistPivot.Items.Add(pi);
                Debug.WriteLine("Loaded Playlist: " + p.Title);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                //ApplicationDataContainer playlistSettings = localSettings.Containers["playlistSettings"];
                //playlistSettings.Values.Remove(playlistSettings.Values.First(x => (string)x.Value == playlistId));
            }
        }

        public async Task LoadPlaylistbyADC(ApplicationDataContainer playlist, string name)
        {
            try
            {
                Debug.WriteLine("pName " + name);
                Debug.WriteLine("2");
                ListView view = new ListView();
                Debug.WriteLine("3");
                view.Name = "lstVwPlaylist";
                view.SelectionChanged += LstVwTracks_SelectionChanged;
                view.ItemTemplate = dtListView;
                view.ItemContainerStyle = styleListView;
                view.Background = ibSn;
                Debug.WriteLine("4");

                List<Task> tasks = new List<Task>();

                foreach (KeyValuePair<string,object> vid in playlist.Values)
                {
                    Debug.WriteLine("Loading " + (string)vid.Value);
                    TrackX result = downloadedTracks.Find(x => x.VideoID == (string)vid.Value);
                    if (result == default(TrackX))
                    {
                        Debug.WriteLine("TRY LOAD VID " + vid.Value);
                        Task task = ytapi.Fetch<Video>((string)vid.Value).ContinueWith((t) =>
                        {
                            if (t.Exception != null) return;
                            Debug.WriteLine("LOADED VID " + vid.Value);
                            view.Items.Add(new TrackX(t.Result));
                        }, TaskScheduler.FromCurrentSynchronizationContext());
                        tasks.Add(task);
                    }
                    else
                    {
                        view.Items.Add(result);
                    }
                }
                Debug.WriteLine("5");

                PivotItem pi = new PivotItem();
                pi.Content = view;
                pi.Header = name;
                Debug.WriteLine("6");

                if (playlistPivot.Items.Any(x => ((x as PivotItem).Header as string) == name))
                {
                    var pobj = playlistPivot.Items.First(x => ((x as PivotItem).Header as string) == name);
                    playlistPivot.Items.Remove(pobj);
                }

                playlistPivot.Items.Add(pi);
                await Task.WhenAll(tasks);
                Debug.WriteLine("Loaded Playlist: " + name);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                //ApplicationDataContainer cp = localSettings.CreateContainer("customPlaylists", ApplicationDataCreateDisposition.Always);
                //cp.DeleteContainer(name);
            }
        }

        async void getMusic()
        {
            /*
            StorageLibrary d = await StorageLibrary.GetLibraryAsync(KnownLibraryId.Music);
            StorageFile f = await d.SaveFolder.CreateFileAsync("urls.txt");
            Stream s = await f.OpenStreamForWriteAsync();
            StreamWriter sw = new StreamWriter(s);
            YouTubeUri[] uris = await YouTubeUri.GetUrisAsync("https://www.youtube.com/watch?v=KhSyOp2YrWI");

            foreach (YouTubeUri uri in uris)
            {


                await sw.WriteLineAsync(uri.Uri.ToString());
                await sw.WriteLineAsync();
            }
            sw.Dispose();
            s.Dispose();*/

            //string playlis2 = "PLxtNsXIpXlFOpyQP_pBU2bNQUiGtpdIWU";
            //string playlist = "PLR7JWZAjVdyt484P_iMNVfjWuKceX5BFp";
            //IReadOnlyList<YoutubeExplode.Models.Video> d = await client.GetChannelUploadsAsync("UC4wY5Tv2alA6dMCL2sJX7_g");

            /*
            foreach (PlaylistItem video in await api.GetPlaylistItemsAsync(playlist))
            {
                ListViewItem item = new ListViewItem();
                item.Content = video.Snippet.Title;//f.DisplayName;//arr[0].Uri.AbsoluteUri;
                item.Tag = video;
                lstVwTracks.Items.Add(item);
            }*/
        }

        private async void btnDownload_Click(object sender, RoutedEventArgs e)
        {
            AppBarButton button = (AppBarButton)sender;
            TrackX track = (TrackX)button.Tag;
            RelativePanel relativePanel = (RelativePanel)button.Parent;
            RelativePanel relativePanel2 = (RelativePanel)relativePanel.Parent;
            ProgressBar pbDownload = (ProgressBar)relativePanel2.FindName("pbDownload");

            StorageFolder appfolder;
            string dlloc = "";

            try
            {
                dlloc = (string)ApplicationData.Current.LocalSettings.Values["txtBxDownloadLocation"];
                appfolder = await StorageFolder.GetFolderFromPathAsync(dlloc);
            }
            catch
            {
                try
                {
                    appfolder = await ApplicationData.Current.RoamingFolder.GetFolderAsync("ytmusic");
                }
                catch
                {
                    appfolder = await ApplicationData.Current.RoamingFolder.CreateFolderAsync("ytmusic");
                }
                localSettings.Values["txtBxDownloadLocation"] = appfolder.Path;
            }

            if (track.IsSaveFileLinked)
            {
                await track.DeleteSaveFile();
                downloadedTracks.Remove(track);
                button.Icon = new SymbolIcon(Symbol.Download);
                return;
            }

            pbDownload.Visibility = Visibility.Visible;
            await track.DownloadAndSerializeToDisk(appfolder, new Action<double>(async delegate (double progr)
            {
                await d.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    pbDownload.Value = progr * 100;
                });
            }));
            pbDownload.Visibility = Visibility.Collapsed;
            button.Icon = new SymbolIcon(Symbol.Accept);
            downloadedTracks.Add(track);

            /*
            try
            {
                Debug.WriteLine("Searching for file");
                f = (await appfolder.GetFilesAsync()).First(x => x.DisplayName == video.Id);
                Debug.WriteLine("File found, deleting");
                await f.DeleteAsync();
                await LoadDownloadedTracks(dlloc);
                button.Icon = new SymbolIcon(Symbol.Download);
                return;
            }
            catch (Exception)
            {
                try
                {
                    Debug.WriteLine("File not found, creating file");
                    f = await appfolder.CreateFileAsync(video.Id + ".sntemp");
                    Debug.WriteLine("File created");
                }
                catch (Exception xe)
                {
                    await new MessageDialog(xe.Message).ShowAsync();
                    return;
                }
            }

            Debug.WriteLine(ApplicationData.Current.RoamingFolder.Path);
            Debug.WriteLine(f.Path);

            pbDownload.Visibility = Visibility.Visible;
            await ytapi.DownloadAsync(video, f, new Action<double>(async delegate (double progr)
            {
                await d.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    pbDownload.Value = progr * 100;
                });
            }));

            pbDownload.Visibility = Visibility.Collapsed;

            try
            {
                //var mp = await f.Properties.GetMusicPropertiesAsync();
                //mp.Title = video.Title;
                //mp.Artist = video.Author;
                //await mp.SavePropertiesAsync();
            }
            catch (Exception xe)
            {
                Debug.WriteLine(xe.Message);
            }

            downloadedTracks.Add(f);
            button.Icon = new SymbolIcon(Symbol.Accept);*/

        }

        private async void btnSaveLocal_Click(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            TrackX track = (TrackX)button.Tag;
            RelativePanel relativePanel = (RelativePanel)button.Parent;
            RelativePanel relativePanel2 = (RelativePanel)relativePanel.Parent;
            ProgressBar pbDownload = (ProgressBar)relativePanel2.FindName("pbDownload");

            SaveTrack dia = new SaveTrack(track);
            await dia.ShowAsync();
            return;
            /*

            StorageFolder appfolder;

            try
            {
                appfolder = await ApplicationData.Current.RoamingFolder.GetFolderAsync("ytmusic");
            }
            catch
            {
                appfolder = await ApplicationData.Current.RoamingFolder.CreateFolderAsync("ytmusic");
            }
            
            FileSavePicker savePicker = new Windows.Storage.Pickers.FileSavePicker();
            savePicker.SuggestedStartLocation = PickerLocationId.MusicLibrary;
            // Dropdown of file types the user can save the file as
            savePicker.FileTypeChoices.Add("Audio", new List<string>() { ".mp3", ".m4a", ".alac", ".flac", ".wav", ".wma",".webm" });
            savePicker.FileTypeChoices.Add("Video", new List<string>() { ".mp4", ".wmv", ".avi" });
            // Default file name if the user does not type one in or select a file to replace
            savePicker.SuggestedFileName = video.Title.Trim(System.IO.Path.GetInvalidFileNameChars());
            


            StorageFile f = await savePicker.PickSaveFileAsync();

            if (f == null) return;

            Debug.WriteLine(f.ContentType);

            string fileType = f.FileType.Substring(1);
            Debug.WriteLine(fileType);

            MediaFormat format = (MediaFormat)Enum.Parse(typeof(MediaFormat),fileType);

            Debug.WriteLine(ApplicationData.Current.RoamingFolder.Path);
            Debug.WriteLine(f.Path);

            pbDownload.Visibility = Visibility.Visible;
            await ytapi.DownloadAndTranscodeAsync(video, f, format, new Action<double>(async delegate (double progr)
            {
                await d.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    pbDownload.Value = progr * 100;
                });
            }));

            pbDownload.Visibility = Visibility.Collapsed;*/
        }

        private void btnFavorite_Click(object sender, RoutedEventArgs e)
        {
            AppBarButton button = (AppBarButton)sender;
            TrackX track = (TrackX)button.Tag;

            ApplicationDataContainer favourites;

            if (!localSettings.Containers.ContainsKey("fav"))
            {
                favourites = localSettings.CreateContainer("fav", ApplicationDataCreateDisposition.Always);

            }
            else
            {
                favourites = localSettings.Containers["fav"];
            }

            if (favs.Any(x => x == track))
            {
                favourites.Values.Remove(track.Title);
                favs.Remove(track);
                button.Icon = new SymbolIcon(Symbol.OutlineStar);
            }
            else
            {
                TrackX result = downloadedTracks.Find(x => x.VideoID == track.VideoID);
                if (result == default(TrackX))
                {
                    favourites.Values.Add(track.Title, track.VideoID);
                    favs.Add(track);
                }
                else
                {
                    favourites.Values.Add(result.Title, result.VideoID);
                    favs.Add(result);
                }
                button.Icon = new SymbolIcon(Symbol.SolidStar);
            }
        }

        public static bool IsFavourite(TrackX track)
        {
            return favs.Any(x => x.VideoID == track.VideoID);
        }

        private void btnDownload_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            AppBarButton button = (AppBarButton)sender;
            TrackX t = (TrackX)button.Tag;

            try
            {
                if (downloadedTracks.Any(x => x.VideoID == t.VideoID))
                {
                    button.Icon = new SymbolIcon(Symbol.Cancel);
                }
            }
            catch
            {

            }
        }

        private void btnDownload_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            AppBarButton button = (AppBarButton)sender;
            TrackX t = (TrackX)button.Tag;

            try
            {
                if (downloadedTracks.Any(x => x.VideoID == t.VideoID))
                {
                    button.Icon = new SymbolIcon(Symbol.Accept);
                }
            }
            catch
            {

            }
        }

        private void btnMore_Click(object sender, RoutedEventArgs e)
        {
            
            AppBarButton button = (AppBarButton)sender;
            MenuFlyout flyout = (MenuFlyout)button.Flyout;

            MenuFlyoutSubItem itmAddToPlaylist = (MenuFlyoutSubItem)flyout.Items.Where(x => x.Name == "itmAddToPlaylist").First();
            MenuFlyoutSubItem itmRemoveFromPlaylist = (MenuFlyoutSubItem)flyout.Items.Where(x => x.Name == "itmRemoveFromPlaylist").First();

            ApplicationDataContainer customPlaylist = localSettings.CreateContainer("customPlaylists", ApplicationDataCreateDisposition.Always);

            foreach (KeyValuePair<string, ApplicationDataContainer> playlist in customPlaylist.Containers)
            {
                //await LoadPlaylistbyADC(playlist.Value, pName);
                MenuFlyoutItem item = new MenuFlyoutItem();
                item.Tag = new object[] { playlist.Value, button.Tag };
                item.Text = playlist.Key;
                item.Icon = new SymbolIcon(Symbol.MusicInfo);
                item.Click += ATPlaylist_Click;

                itmAddToPlaylist.Items.Add(item);

                MenuFlyoutItem item2 = new MenuFlyoutItem();
                item2.Tag = new object[] { playlist.Value, button.Tag };
                item2.Text = playlist.Key;
                item2.Icon = new SymbolIcon(Symbol.MusicInfo);
                item2.Click += RFPlaylist_Click;

                itmRemoveFromPlaylist.Items.Add(item2);
            }
            
        }

        private async void RFPlaylist_Click(object sender, RoutedEventArgs e)
        {
            MenuFlyoutItem item = (MenuFlyoutItem)sender;
            object[] con = (object[])item.Tag;
            ApplicationDataContainer playlist = (ApplicationDataContainer)con[0];
            TrackX vid = (TrackX)con[1];

            try
            {
                playlist.Values.Remove(vid.Title);
                await LoadPlaylistbyADC(playlist, item.Text);
            }
            catch { }
        }

        private async void ATPlaylist_Click(object sender, RoutedEventArgs e)
        {
            MenuFlyoutItem item = (MenuFlyoutItem)sender;
            object[] con = (object[])item.Tag;
            ApplicationDataContainer playlist = (ApplicationDataContainer)con[0];
            TrackX vid = (TrackX)con[1];

            playlist.Values.Add(vid.Title, vid.VideoID);
            await LoadPlaylistbyADC(playlist, item.Text);
        }

        private async void btnNewPlaylist_Click(object sender, RoutedEventArgs e)
        {
            ApplicationDataContainer customPlaylist = localSettings.CreateContainer("customPlaylists", ApplicationDataCreateDisposition.Always);
            await new NewPlaylistDialog(this).ShowAsync();
        }

        private async void ItmViewOnYT_Click(object sender, RoutedEventArgs e)
        {
            MenuFlyoutItem item = (MenuFlyoutItem)sender;
            TrackX vid = (TrackX)item.Tag;
            await Windows.System.Launcher.LaunchUriAsync(vid.GetUrl());
        }

        private void BtnExpand_Click(object sender, RoutedEventArgs e)
        {
            //doubleani.To = this.ActualHeight;
            rpTrackDetails.Visibility = Visibility.Visible;
            xpand.Begin();

            //rpPlayer.
            //this.Frame.Navigate(typeof(TrackDetails), null, new EntranceNavigationTransitionInfo());
        }

        private void BtnCollapse_Click(object sender, RoutedEventArgs e)
        {
            clapse.Begin();
            clapse.Completed += Clapse_Completed;
        }

        private void Clapse_Completed(object sender, object e)
        {
            rpTrackDetails.Visibility = Visibility.Collapsed;
        }

        bool downloadedtracksloaded = false;

        private async void PlaylistPivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PivotItem item = e.AddedItems[0] as PivotItem;

            if (!downloadedtracksloaded)
            {
                if (localSettings.Values.ContainsKey("txtBxDownloadLocation"))
                {
                    string dlloc = (string)ApplicationData.Current.LocalSettings.Values["txtBxDownloadLocation"];
                    await LoadDownloadedTracks(dlloc).ConfigureAwait(false);
                    downloadedtracksloaded = true;
                }
            }

            await Dispatcher.RunAsync(CoreDispatcherPriority.High, async delegate () {
                switch (item.Header.ToString())
                {
                    case "Favorites":
                        if (lstVwFav.Items.Count <= 0 && localSettings.Containers.ContainsKey("fav"))
                        {
                            lstVwFav.ItemsSource = favs;
                            await LoadFavsSync().ConfigureAwait(false);
                        }
                        break;
                    case "Search":
                        break;
                }
            });
            
        }
    }
}
