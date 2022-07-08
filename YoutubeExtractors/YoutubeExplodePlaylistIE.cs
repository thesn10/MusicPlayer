using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Windows.UI.WebUI;
using YoutubeDL;
using YoutubeDL.Extractors;
using YoutubeDL.Models;
using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Playlists;

namespace NCSMusic
{

    class YoutubeExplodePlaylistIE : SimpleInfoExtractor
    {
        public override bool Working => true;

        public override string Description => "Youtube explode info extrator mapping";

        public override string Name => "Youtube";

        public YoutubeClient client;

        public bool Initialized { get; set; } = false;

        public YoutubeExplodePlaylistIE(IManagingDL dl)
        {

        }

        public override void Initialize()
        {
            if (client != null)
            {
                return;
            }
            client = new YoutubeClient();
        }

        public override bool Suitable(string url)
        {

            return GetId(url) != null;
        }

        public static PlaylistId? GetId(string url)
        {
            try
            {
                //var pl = new PlaylistId(url);
                return url;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public override InfoDict Extract(string url)
        {
            return ExtractAsync(url).GetAwaiter().GetResult();
        }

        public override async Task<InfoDict> ExtractAsync(string url)
        {
            var pId = GetId(url);
            var ePl = await client.Playlists.GetAsync(pId.Value);

            YoutubeDL.Models.Playlist pl = new YoutubeDL.Models.Playlist();
            pl.Title = ePl.Title;
            pl.Id = ePl.Id;
            pl.Uploader = ePl.Author.Title;

            pl.Entries = new List<InfoDict>();
            await foreach (var i in client.Playlists.GetVideosAsync(pId.Value))
            {
                pl.Entries.Add(Parse(i));
            }
            return pl;
        }

        public static YoutubeDL.Models.Video Parse(PlaylistVideo eVid)
        {
            YoutubeDL.Models.Video vid = new YoutubeDL.Models.Video();
            vid.Title = eVid.Title;
            vid.Id = eVid.Id;
            //vid.Description = eVid.Description;
            vid.Uploader = eVid.Author.Title;
            //vid.UploadDate = eVid.UploadDate.UtcDateTime;
            //vid.Views = (int)eVid.Engagement.ViewCount;
            //vid.Likes = (int)eVid.Engagement.LikeCount;
            //vid.Dislikes = (int)eVid.Engagement.DislikeCount;
            vid.UploaderId = eVid.Author.ChannelId;
            vid.Duration = eVid.Duration.Value;
            //vid.Categories = eVid.Keywords.ToList();

            /*List<YoutubeDL.Models.Thumbnail> tlist = new List<YoutubeDL.Models.Thumbnail>();
            tlist.Add(new YoutubeDL.Models.Thumbnail() { Url = eVid.Thumbnails.LowResUrl, Width = 1 });
            tlist.Add(new YoutubeDL.Models.Thumbnail() { Url = eVid.Thumbnails.MediumResUrl, Width = 2 });
            if (eVid.Thumbnails. != null)
                tlist.Add(new YoutubeDL.Models.Thumbnail() { Url = eVid.Thumbnails.StandardResUrl, Width = 3 });
            tlist.Add(new YoutubeDL.Models.Thumbnail() { Url = eVid.Thumbnails.HighResUrl, Width = 4 });
            if (eVid.Thumbnails.HighResUrl != null)
                tlist.Add(new YoutubeDL.Models.Thumbnail() { Url = eVid.Thumbnails.MaxResUrl, Width = 5 });*/

            var tlist = eVid.Thumbnails
                .Select(x => new YoutubeDL.Models.Thumbnail()
                {
                    Url = x.Url,
                    Width = x.Resolution.Width,
                    Height = x.Resolution.Height
                })
                .ToList();

            vid.Thumbnails = new ThumbnailCollection(tlist);
            return vid;
        }
    }
}
