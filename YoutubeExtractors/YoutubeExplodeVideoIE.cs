using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YoutubeDL;
using YoutubeDL.Extractors;
using YoutubeDL.Models;
using YoutubeExplode;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;

namespace NCSMusic
{
    class YoutubeExplodeVideoIE : SimpleInfoExtractor
    {
        public override bool Working => true;

        public override string Description => "Youtube explode info extrator mapping";

        public override string Name => "Youtube";

        public static YoutubeClient client;

        public bool Initialized { get; set; } = false;

        public YoutubeExplodeVideoIE(IManagingDL dl)
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

        public static VideoId? GetId(string url)
        {
            try
            {
                //var vid = new VideoId(url);
                return url;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static async Task<FormatCollection> GetStreamData(YoutubeDL.Models.Video video)
        {
            List<IFormat> flist = new List<IFormat>();
            var streams = await client.Videos.Streams.GetManifestAsync(video.Id);
            foreach (var stream in streams.Streams)
            {
                if (stream is MuxedStreamInfo m)
                {
                    MuxedFormat f = new MuxedFormat();
                    f.AudioCodec = m.AudioCodec;
                    f.TotalBitrate = (float)m.Bitrate.KiloBitsPerSecond;
                    f.FPS = m.VideoQuality.Framerate;
                    f.Width = m.VideoResolution.Width;
                    f.Height = m.VideoResolution.Height;
                    f.Extension = m.Container.Name;
                    //f.Id = m.Tag.ToString();
                    f.Url = m.Url;
                    f.VideoCodec = m.VideoCodec;
                    flist.Add(f);
                }
                else if (stream is VideoOnlyStreamInfo v)
                {
                    VideoFormat f = new VideoFormat();
                    f.VideoBitrate = (int)v.Bitrate.BitsPerSecond;
                    f.FPS = v.VideoQuality.Framerate;
                    f.Width = v.VideoResolution.Width;
                    f.Height = v.VideoResolution.Height;
                    f.Extension = v.Container.Name;
                    //f.Id = v..ToString();
                    f.Url = v.Url;
                    f.VideoCodec = v.VideoCodec;
                    flist.Add(f);
                }
                else if (stream is AudioOnlyStreamInfo a)
                {
                    AudioFormat f = new AudioFormat();
                    f.AudioCodec = a.AudioCodec;
                    f.Extension = a.Container.Name;
                    //f.Id = a.Tag.ToString();
                    f.Url = a.Url;
                    f.AudioBitrate = (int)a.Bitrate.BitsPerSecond;
                    flist.Add(f);
                }
            }
            return new FormatCollection(flist);
        }

        public static YoutubeDL.Models.Video Parse(YoutubeExplode.Videos.Video eVid)
        {
            YoutubeDL.Models.Video vid = new YoutubeDL.Models.Video();
            vid.Title = eVid.Title;
            vid.Id = eVid.Id;
            vid.Description = eVid.Description;
            vid.Uploader = eVid.Author.Title;
            vid.UploadDate = eVid.UploadDate.UtcDateTime;
            vid.Views = (int)eVid.Engagement.ViewCount;
            vid.Likes = (int)eVid.Engagement.LikeCount;
            vid.Dislikes = (int)eVid.Engagement.DislikeCount;
            vid.UploaderId = eVid.Author.ChannelId;
            vid.Duration = eVid.Duration.Value;
            vid.Categories = eVid.Keywords.ToList();

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

        public override InfoDict Extract(string url)
        {
            return ExtractAsync(url).GetAwaiter().GetResult();
        }

        public override async Task<InfoDict> ExtractAsync(string url)
        {
            var vidId = GetId(url);
            var eVid = await client.Videos.GetAsync(vidId.Value);

            return Parse(eVid);
        }
    }
}
