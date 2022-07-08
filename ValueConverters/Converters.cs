using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

namespace NCSMusic
{
    public class TrackToFavIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType,
        object parameter, string culture)
        {
            TrackX track = (TrackX)value;

            if (MainPage.IsFavourite(track))
            {
                return new SymbolIcon(Symbol.SolidStar);
            }
            else
            {
                return new SymbolIcon(Symbol.OutlineStar);
            }
        }

        public object ConvertBack(object value, Type targetType,
         object parameter, string culture)
        {
            return null;
        }
    }

    public class TrackToDownloadIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType,
        object parameter, string culture)
        {
            TrackX track = (TrackX)value;

            if (track.IsSaveFileLinked)
            {
                return new SymbolIcon(Symbol.Accept);
            }
            else
            {
                return new SymbolIcon(Symbol.Download);
            }
        }

        public object ConvertBack(object value, Type targetType,
         object parameter, string culture)
        {
            return null;
        }
    }
}
