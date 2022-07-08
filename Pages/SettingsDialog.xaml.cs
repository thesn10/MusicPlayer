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
using Windows.UI.ViewManagement;
using Windows.UI.Xaml.Markup;
using Windows.UI.Popups;
using Windows.Storage;
using YoutubeDL;
using YoutubeDL.Models;

// Die Elementvorlage "Inhaltsdialogfeld" wird unter https://go.microsoft.com/fwlink/?LinkId=234238 dokumentiert.

namespace NCSMusic
{
    public sealed partial class SettingsDialog : ContentDialog
    {
        ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
        ApplicationDataContainer playlistSettings;

        YouTubeApi ytapi = new YouTubeApi();
        YouTubeDL yt = YouTubeApi.yt;

        public SettingsDialog()
        {
            this.InitializeComponent();
            LoadSettings();
        }

        public async void LoadSettings()
        {
            playlistSettings = localSettings.CreateContainer("playlistSettings", ApplicationDataCreateDisposition.Always);
            ApplicationDataContainer cpSettings = localSettings.CreateContainer("customPlaylists", ApplicationDataCreateDisposition.Always);
            try
            {
                swDownloadOrStream.IsOn = (bool)localSettings.Values["swDownloadOrStream"];
            }
            catch { }
            try
            {
                swSnLogo.IsOn = (bool)localSettings.Values["dlogo"];
            }
            catch { }
            try
            {
                swVisualization.IsOn = (bool)localSettings.Values["enableMusicEffects"];
            }
            catch { }
            try
            {
                txtBxDownloadLocation.Text = (string)localSettings.Values["txtBxDownloadLocation"];
            }
            catch { }
            try
            {
                txtBxYoutubePlaylist.Text = (string)localSettings.Values["txtBxYoutubePlaylist"];
            }
            catch { }

            foreach (KeyValuePair<string, object> p in playlistSettings.Values)
            {
                try
                {
                    lstPlaylists.Items.Add(new PlaylistX(await ytapi.Fetch<Playlist>((string)p.Value)));
                }catch 
                {

                }
            }
            foreach (KeyValuePair<string, ApplicationDataContainer> p in cpSettings.Containers)
            {
                lstCPlaylists.Items.Add(new PlaylistX(p.Key,"Custom Playlist"));
            }
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            localSettings.Values["swDownloadOrStream"] = swDownloadOrStream.IsOn;
            localSettings.Values["dlogo"] = swSnLogo.IsOn;
            localSettings.Values["enableMusicEffects"] = swVisualization.IsOn;
            localSettings.Values["txtBxDownloadLocation"] = txtBxDownloadLocation.Text;
            localSettings.Values["txtBxYoutubePlaylist"] = txtBxYoutubePlaylist.Text;
            playlistSettings.Values.Clear();
            foreach (object o in lstPlaylists.Items)
            {
                PlaylistX i = (PlaylistX)o;
                playlistSettings.Values[i.PlaylistTitle] = i.playlist.Id;
            }
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
        }

        private void ButtonRemove_Click(object sender, RoutedEventArgs e)
        {
            Button b = (Button)sender;
            try
            {
                PlaylistX h = (PlaylistX)lstPlaylists.Items.First(x => (x as PlaylistX).PlaylistTitle == (string)b.Tag);
                lstPlaylists.Items.Remove(h);
            }
            catch
            {
                PlaylistX h = (PlaylistX)lstCPlaylists.Items.First(x => (x as PlaylistX).PlaylistTitle == (string)b.Tag);
                lstCPlaylists.Items.Remove(h);
                localSettings.CreateContainer("customPlaylists", ApplicationDataCreateDisposition.Always).DeleteContainer((string)b.Tag);
            }
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            string playlistLink = txtBxYoutubePlaylist.Text;
            string playlistId = GetPlaylistIdByLink(playlistLink);

            if (playlistId == null)
            {
                await new MessageDialog("This is not a valid youtube playlist link/id").ShowAsync();
                return;
            }
            else if (playlistId.StartsWith("UC"))
            {
                playlistId = "UU" + playlistId.Substring(2);
            }

            Playlist playlist;

            try
            {
                playlist = await ytapi.Fetch<Playlist>(playlistId);
            }
            catch
            {
                await new MessageDialog("An error occurred while acessing the playlist").ShowAsync();
                return;
            }

            localSettings.Values["txtBxYoutubePlaylist"] = txtBxYoutubePlaylist.Text;

            lstPlaylists.Items.Add(new PlaylistX(playlist));
        }

        public string GetPlaylistIdByLink(string link)
        {
            if (link.ToLower().StartsWith("https://www.youtube.com/playlist?list="))
            {
                return link.Substring(38, 34);
            }
            else if (link.ToLower().StartsWith("https://youtube.com/playlist?list="))
            {
                return link.Substring(34, 34);
            }
            else if (link.ToLower().StartsWith("www.youtube.com/playlist?list="))
            {
                return link.Substring(30, 34);
            }
            else if (link.ToLower().StartsWith("youtube.com/playlist?list="))
            {
                return link.Substring(26, 34);
            }
            else if (link.Length == 34)
            {
                return link;
            }
            else if (link.Length == 24)
            {
                return link;
            }
            else
            {
                return null;
            }
        }
    }

    public class PlaylistX
    {

        public PlaylistX(Playlist playlist)
        {
            this.playlist = playlist;
            PlaylistTitle = playlist.Title;
            PlaylistLink = playlist.WebpageUrl;
        }

        public PlaylistX(string title, string url)
        {
            this.playlist = null;
            PlaylistTitle = title;
            PlaylistLink = url;
        }

        public string PlaylistTitle { get; set; }
        public string PlaylistLink { get; set; }
        public Playlist playlist;
    }

    public class VideoX
    {
        public static ApplicationDataContainer favs = ApplicationData.Current.LocalSettings.Containers["fav"];
        public Video video { get; set; }
        public SymbolIcon favIcon { get; set; }
        public SymbolIcon dlIcon { get; set; }

        public VideoX(Video video, List<StorageFile> downloadedVideos)
        {
            this.video = video;


            Symbol downl;
            try
            {
                if (downloadedVideos.Any(x => x.DisplayName == video.Id))
                {
                    downl = Symbol.Accept;
                }
                else
                {
                    downl = Symbol.Download;
                }
            }
            catch
            {
                downl = Symbol.Download;
            }

            Symbol fav;
            try
            {
                if (favs.Values.Any(x => (string)x.Value == video.Id))
                {
                    fav = Symbol.SolidStar;
                }
                else
                {
                    fav = Symbol.OutlineStar;
                }
            }
            catch
            {
                fav = Symbol.OutlineStar;
            }

            this.favIcon = new SymbolIcon(fav);
            this.dlIcon = new SymbolIcon(downl);
        }

        public VideoX(Video video, Symbol dl)
        {
            this.video = video;

            Symbol fav;
            try
            {
                if (favs.Values.Any(x => (string)x.Value == video.Id))
                {
                    fav = Symbol.SolidStar;
                }
                else
                {
                    fav = Symbol.OutlineStar;
                }
            }
            catch
            {
                fav = Symbol.OutlineStar;
            }

            this.favIcon = new SymbolIcon(fav);
            this.dlIcon = new SymbolIcon(dl);
        }

        public VideoX(Video video,Symbol fav,Symbol dl)
        {
            this.video = video;
            this.favIcon = new SymbolIcon(fav);
            this.dlIcon = new SymbolIcon(dl);
        }

    }
}
