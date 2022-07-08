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
using Windows.Storage;

// Die Elementvorlage "Inhaltsdialogfeld" wird unter https://go.microsoft.com/fwlink/?LinkId=234238 dokumentiert.

namespace NCSMusic
{
    public sealed partial class NewPlaylistDialog : ContentDialog
    {
        MainPage mainPage;

        public NewPlaylistDialog(MainPage mainpage)
        {
            this.InitializeComponent();
            mainPage = mainpage;
        }

        private async void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            if (txtbxname.Text != "")
            {
                var container = ApplicationData.Current.LocalSettings.CreateContainer("customPlaylists", ApplicationDataCreateDisposition.Always).CreateContainer(txtbxname.Text, ApplicationDataCreateDisposition.Always);
                await mainPage.LoadPlaylistbyADC(container, txtbxname.Text);
            }
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {

        }
    }
}
