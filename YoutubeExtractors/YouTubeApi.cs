using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using Windows.Storage;
using Windows.Storage.FileProperties;
using System.Text.RegularExpressions;
using System.Threading;
using System.Net.Http;
using Windows.Web;
using Windows.Web.UI;
using Windows.UI.Popups;
using System.Windows;
using System.IO;
using YoutubeDL;
using Windows.Foundation;
using Windows.Storage.Streams;
using Windows.Media.MediaProperties;
using Windows.Media.Transcoding;
using Windows.Media.Playback;
using Windows.Media.Core;
using YoutubeDL.Models;
using YoutubeDL.Downloaders;
//using YoutubeDL.Uwp.Postprocessors;
using YoutubeDL.Python;

namespace NCSMusic
{
    public class YouTubeApi
    {
        private static YouTubeDL _yt;
        public static YouTubeDL yt 
        { 
            get 
            {
                if (_yt == null)
                    _yt = CreateYTDL();
                return _yt;
            }
        }

        string ncsplaylist = "PLRBp0Fe2GpgnIh0AiYKh7o7HnYAej-5ph";
        //string ncsChannelId = "UC_aEa8K-EOJ3D6gOs7HcyNg";
        //string snChannelId = "UC4wY5Tv2alA6dMCL2sJX7_g";
        string apiKey = "AIzaSyAi7JojH375uL19NgLU3OszDxIUbPwHU4Y";

        public YouTubeApi()
        {

        }

        public static YouTubeDL CreateYTDL()
        {
            var ytdl = new YouTubeDL();
            ytdl.Options.SkipDownload = true;
            //ytdl.Options.Merger = (f) => new MediaFoundationMergerPP();
            //ytdl.Options.Converter = (f) => new MediaFoundationConvertPP();
            ytdl.Options.ExtractFlat = "in_playlist";
            ytdl.AddExtractor<YoutubeExplodeVideoIE>();
            ytdl.AddExtractor<YoutubeExplodePlaylistIE>();
            //ytdl.AddPythonExtractors();

            return ytdl;
        }

        public Task<Uri> GetVideoLink(Video vid, bool onlyAudio)
        {
            string uri;
            if (onlyAudio)
            {
                uri = vid.Formats.WithBestAudioBitrate().Url;
            }
            else
            {
                uri = vid.Formats.WithBestVideoResolution().Url;
            }
            return Task.FromResult(new Uri(uri));
        }

        public async Task<T> Fetch<T>(string id) where T : InfoDict
        {
            var iv = await yt.ExtractInfoAsync(id, false);
            return iv as T;
        }

        public Task<InfoDict> SearchVideosAsync(string searchstr, int num = 1)
        {
            return yt.GetSearchResults(searchstr, num, download: false);
        }

        public static async Task DownloadAudio(Video video, string filepath, Action<double> progress)
        {
            await DownloadAudio(video, await StorageFile.GetFileFromPathAsync(filepath), progress);
        }

        public static async Task DownloadAudio(Video video, StorageFile filepath, Action<double> progressAction)
        {
            foreach (var i in video.Formats.WithAudio())
            {
                Debug.WriteLine(i.AudioCodec + ", " + i.AudioBitrate + ", " + i.Extension);
            }


            IAudioFormat info = video.Formats.WithBestAudioBitrate();
            Debug.WriteLine("selected: " + info.AudioCodec + ", " + info.AudioBitrate + ", " + info.Extension);
            //string filename = filepath.Name.Substring(0, filepath.Name.Length - filepath.FileType.Length) + "." + info.Extension;
            //Debug.WriteLine(filename);
            //try
            //{
            //    await filepath.RenameAsync(filename);
            //}
            //catch { }
            //Debug.WriteLine("renamed");

            //Stream stream = await filepath.OpenStreamForWriteAsync();
            Debug.WriteLine("stream opened");


            Progress<double> progress = new Progress<double>(progressAction);
            Debug.WriteLine("progressBarCr");

            var fd = FileDownloader.GetSuitableDownloader(info.Protocol);
            if (fd is HttpFD httpFd)
            {
                httpFd.MultiThreadDownload = false;
            }
            fd.OnProgress += (sender, e) => progressAction(e.Percent);
            await fd.DownloadAsync(info, filepath.Path);
            Debug.WriteLine("downloaded");
            //stream.Dispose();
        }

        public async Task GetStreamByMediaFormat(Video video, MediaFormat format, VideoProcessMode mode, StorageFile outputFile, Action<double> progressAction)
        {
            Progress<double> progress = new Progress<double>(progressAction);
            switch (mode)
            {
                case VideoProcessMode.None:
                    IFormat info = null;
                    switch (format)
                    {
                        case MediaFormat.m4a:
                            info = video.Formats.GetAudioOnlyFormats().First(x => x.AudioCodec == "aac" || x.AudioCodec == "m4a");
                            break;
                        case MediaFormat.aac:
                            info = video.Formats.GetAudioOnlyFormats().First(x => x.AudioCodec == "aac" || x.AudioCodec == "m4a");
                            break;
                        case MediaFormat.webm:
                            info = video.Formats.GetAudioOnlyFormats().First(x => x.AudioCodec == "opus" || x.AudioCodec == "webm");
                            break;
                        case MediaFormat.ogg:
                            info = video.Formats.GetAudioOnlyFormats().First(x => x.AudioCodec == "vorbis" || x.AudioCodec == "ogg");
                            break;
                        case MediaFormat.mp4:
                            info = video.Formats.MuxedWithBestResolution();
                            break;
                    }

                    var fd = FileDownloader.GetSuitableDownloader(info.Protocol);
                    fd.OnProgress += (sender, e) => progressAction(e.Percent);
                    await fd.DownloadAsync(info, outputFile.Path);
                    break;
                case VideoProcessMode.Convert:

                    IAudioFormat i = GetConversionReadyStream(video.Formats, format);

                    Debug.WriteLine(
                        "Converting " +
                        i.Extension + " File with " +
                        i.AudioCodec + " Audio to " +
                        " " + format + " File.");

                    //InMemoryRandomAccessStream output = new InMemoryRandomAccessStream();

                    var fd2 = FileDownloader.GetSuitableDownloader(i.Protocol);
                    fd2.OnProgress += (sender, e) => progressAction(e.Percent);
                    await fd2.DownloadAsync(i, outputFile.Path + ".temp");

                    var ifile = await StorageFile.GetFileFromPathAsync(outputFile.Path + ".temp");

                    await MediaConverter.Convert(ifile, outputFile, format, progressAction);


                    break;
                case VideoProcessMode.Merge:

                    IVideoFormat vinfo = video.Formats.WithBestVideoResolution();
                    IAudioFormat ainfo = video.Formats.WithBestAudioBitrate();

                    Debug.WriteLine(
                        "Merging " +
                        vinfo.Quality +
                        " " + vinfo.Extension +
                        " Video with " + ainfo.Extension +
                        " " + ainfo.AudioBitrate +
                        " Audio to " +
                        " " + format + " Video.");

                    //InMemoryRandomAccessStream videoStream = new InMemoryRandomAccessStream();
                    //InMemoryRandomAccessStream audioStream = new InMemoryRandomAccessStream();

                    //await yt.DownloadMediaStreamAsync(vinfo, videoStream.AsStream(), progress);
                    //await yt.DownloadMediaStreamAsync(ainfo, audioStream.AsStream(), progress);

                    var fd3 = FileDownloader.GetSuitableDownloader(vinfo.Protocol);
                    fd3.OnProgress += (sender, e) => progressAction(e.Percent);
                    await fd3.DownloadAsync(vinfo, outputFile.Path + ".temp1");
                    fd3 = FileDownloader.GetSuitableDownloader(ainfo.Protocol);
                    fd3.OnProgress += (sender, e) => progressAction(e.Percent);
                    await fd3.DownloadAsync(ainfo, outputFile.Path + ".temp2");

                    var ivfile = await StorageFile.GetFileFromPathAsync(outputFile.Path + ".temp1");
                    var iafile = await StorageFile.GetFileFromPathAsync(outputFile.Path + ".temp2");

                    MediaConverter.MergeAudioAndVideo(ivfile, iafile, outputFile);
                    break;
                case VideoProcessMode.MergeAndConvert:

                    IVideoFormat vinfo2 = video.Formats.WithBestVideoResolution();
                    IAudioFormat ainfo2 = video.Formats.WithBestAudioBitrate();

                    Debug.WriteLine(
                        "Merging and converting " +
                        vinfo2.Quality +
                        " " + vinfo2.Extension +
                        " Video with " + ainfo2.Extension +
                        " " + ainfo2.AudioBitrate +
                        " Audio to " +
                        " " + format + " Video.");

                    //InMemoryRandomAccessStream videoStream2 = new InMemoryRandomAccessStream();
                    //InMemoryRandomAccessStream audioStream2 = new InMemoryRandomAccessStream();
                    //InMemoryRandomAccessStream output2 = new InMemoryRandomAccessStream();

                    //await yt.DownloadMediaStreamAsync(vinfo2, videoStream2.AsStream(), progress);
                    //await yt.DownloadMediaStreamAsync(ainfo2, audioStream2.AsStream(), progress);

                    var fd4 = FileDownloader.GetSuitableDownloader(vinfo2.Protocol);
                    fd4.OnProgress += (sender, e) => progressAction(e.Percent);
                    await fd4.DownloadAsync(vinfo2, outputFile.Path + ".temp1");
                    fd4 = FileDownloader.GetSuitableDownloader(ainfo2.Protocol);
                    fd4.OnProgress += (sender, e) => progressAction(e.Percent);
                    await fd4.DownloadAsync(ainfo2, outputFile.Path + ".temp2");

                    var ivfile2 = await StorageFile.GetFileFromPathAsync(outputFile.Path + ".temp1");
                    var iafile2 = await StorageFile.GetFileFromPathAsync(outputFile.Path + ".temp2");
                    var iofile2 = await StorageFile.GetFileFromPathAsync(outputFile.Path + ".temp3");

                    MediaConverter.MergeAudioAndVideo(ivfile2, iafile2, iofile2);
                    //output2.Seek(0);
                    Debug.WriteLine("Merging Complete");

                    await MediaConverter.Convert(iofile2, outputFile, format, progressAction);
                    Debug.WriteLine("Conversion Complete");
                    break;
            }
        }

        // Called
        /*
        public async Task DownloadInFormat(Video video, StorageFile output, MediaFormat format, Action<double> progressAction = null)
        {
            Progress<double> progress = new Progress<double>();
            if (progressAction != null)
            {
                progress = new Progress<double>(progressAction);
            }
            else
            {
                progress = new Progress<double>();
            }

            //Check if video or audio is requested
            if (MediaConverter.isVideoType(format))
            {
                IVideoFormat vinfo = video.Formats.WithBestVideoResolution();
                IAudioFormat ainfo = video.Formats.WithBestAudioBitrate();

                InMemoryRandomAccessStream videoStream = new InMemoryRandomAccessStream();
                InMemoryRandomAccessStream audioStream = new InMemoryRandomAccessStream();

                await yt.DownloadMediaStreamAsync(vinfo, videoStream.AsStream(), progress);
                await yt.DownloadMediaStreamAsync(ainfo, audioStream.AsStream(), progress);


                if (format == MediaFormat.mp4)
                {
                    //Merge
                    MediaConverter.MergeAudioAndVideo(videoStream, audioStream, output);
                }
                else
                {
                    //MergeAndConvert
                    InMemoryRandomAccessStream tmpOutput = new InMemoryRandomAccessStream();
                    MediaConverter.MergeAudioAndVideo(videoStream, audioStream, tmpOutput);
                    await MediaConverter.Convert(tmpOutput, output, format, progressAction);
                }

            }
            else
            {
                AudioStreamInfo info;
                if (format == MediaFormat.aac && set.Audio.Any(x => x.AudioEncoding == AudioEncoding.Aac))
                {
                    info = set.Audio.OrderByDescending(s => s.Bitrate).First(x => x.AudioEncoding == AudioEncoding.Aac);
                }
                else if (format == MediaFormat.m4a && set.Audio.Any(x => x.AudioEncoding == AudioEncoding.Aac))
                {
                    info = set.Audio.OrderByDescending(s => s.Bitrate).First(x => x.AudioEncoding == AudioEncoding.Aac);
                }
                else if (format == MediaFormat.webm && set.Audio.Any(x => x.AudioEncoding == AudioEncoding.Opus))
                {
                    info = set.Audio.OrderByDescending(s => s.Bitrate).First(x => x.AudioEncoding == AudioEncoding.Aac);
                }
                else if (format == MediaFormat.ogg && set.Audio.Any(x => x.AudioEncoding == AudioEncoding.Vorbis))
                {
                    info = set.Audio.OrderByDescending(s => s.Bitrate).First(x => x.AudioEncoding == AudioEncoding.Aac);
                }
                else
                {
                    //Merge
                    info = set.Audio.OrderByDescending(s => s.Bitrate).First(x => x.AudioEncoding == AudioEncoding.Aac);
                    InMemoryRandomAccessStream tmpOutput = new InMemoryRandomAccessStream();

                    await yt.DownloadMediaStreamAsync(info, tmpOutput.AsStream(), progress);
                    await MediaConverter.Convert(tmpOutput, output, format, progressAction);
                    return;
                }
                await yt.DownloadMediaStreamAsync(info, await output.OpenStreamForWriteAsync(), progress);
            }
        }

        // Called
        
        public async Task DownloadInFormatAndQuality(string videoId, StorageFile output, MediaFormat format, VideoQuality vq, Action<double> progressAction = null)
        {
            MediaStreamInfoSet set = await yt.GetVideoMediaStreamInfosAsync(videoId);
            Progress<double> progress = new Progress<double>();
            if (progressAction != null)
            {
                progress = new Progress<double>(progressAction);
            }
            else
            {
                progress = new Progress<double>();
            }

            //Check if video or audio is requested
            if (MediaConverter.isVideoType(format))
            {
                if (set.Muxed.Any(x => x.VideoQuality == vq))
                {
                    MuxedStreamInfo info = set.Muxed.Where(x => x.VideoQuality == vq).FirstOrDefault(x => x.VideoEncoding == VideoEncoding.H264);
                    if (format == MediaFormat.mp4)
                    {
                        //None
                        await yt.DownloadMediaStreamAsync(info, await output.OpenStreamForWriteAsync(), progress);
                    }
                    else
                    {
                        //Convert
                        InMemoryRandomAccessStream tmpOutput = new InMemoryRandomAccessStream();

                        await yt.DownloadMediaStreamAsync(info, tmpOutput.AsStream(), progress);
                        await MediaConverter.Convert(tmpOutput, output, format, progressAction);
                    }
                }
                else
                {
                    VideoStreamInfo vinfo = set.Video.OrderByDescending(s => s.Bitrate).First(x => x.VideoQuality == vq && x.VideoEncoding == VideoEncoding.H264);
                    AudioStreamInfo ainfo = set.Audio.OrderByDescending(s => s.Bitrate).First(x => x.AudioEncoding == AudioEncoding.Aac);

                    InMemoryRandomAccessStream videoStream = new InMemoryRandomAccessStream();
                    InMemoryRandomAccessStream audioStream = new InMemoryRandomAccessStream();

                    await yt.DownloadMediaStreamAsync(vinfo, videoStream.AsStream(), progress);
                    await yt.DownloadMediaStreamAsync(ainfo, audioStream.AsStream(), progress);


                    if (format == MediaFormat.mp4)
                    {
                        //Merge
                        MediaConverter.MergeAudioAndVideo(videoStream, audioStream, output);
                    }
                    else
                    {
                        //MergeAndConvert
                        InMemoryRandomAccessStream tmpOutput = new InMemoryRandomAccessStream();
                        MediaConverter.MergeAudioAndVideo(videoStream, audioStream, tmpOutput);
                        await MediaConverter.Convert(tmpOutput, output, format, progressAction);
                    }
                }
            }
            else
            {
                AudioStreamInfo info;
                if (format == MediaFormat.aac && set.Audio.Any(x => x.AudioEncoding == AudioEncoding.Aac))
                {
                    info = set.Audio.OrderByDescending(s => s.Bitrate).First(x => x.AudioEncoding == AudioEncoding.Aac);
                }
                else if (format == MediaFormat.m4a && set.Audio.Any(x => x.AudioEncoding == AudioEncoding.Aac))
                {
                    info = set.Audio.OrderByDescending(s => s.Bitrate).First(x => x.AudioEncoding == AudioEncoding.Aac);
                }
                else if (format == MediaFormat.webm && set.Audio.Any(x => x.AudioEncoding == AudioEncoding.Opus))
                {
                    info = set.Audio.OrderByDescending(s => s.Bitrate).First(x => x.AudioEncoding == AudioEncoding.Aac);
                }
                else if (format == MediaFormat.ogg && set.Audio.Any(x => x.AudioEncoding == AudioEncoding.Vorbis))
                {
                    info = set.Audio.OrderByDescending(s => s.Bitrate).First(x => x.AudioEncoding == AudioEncoding.Aac);
                }
                else
                {
                    //Merge
                    info = set.Audio.OrderByDescending(s => s.Bitrate).First(x => x.AudioEncoding == AudioEncoding.Aac);
                    InMemoryRandomAccessStream tmpOutput = new InMemoryRandomAccessStream();

                    await yt.DownloadMediaStreamAsync(info, tmpOutput.AsStream(), progress);
                    await MediaConverter.Convert(tmpOutput, output, format, progressAction);
                    return;
                }
                await yt.DownloadMediaStreamAsync(info, await output.OpenStreamForWriteAsync(), progress);
            }
        }*/

        /*
        public static async Task<bool> TryGetStreamInMediaFormat(YouTubeDL yt, YoutubeDL.Models.Video video, MediaFormat format, IRandomAccessStream stream, Action<double> progressAction)
        {
            Progress<double> progress = new Progress<double>(progressAction);

            Debug.WriteLine("Available:");
            foreach (var msi in video.Formats.GetAudioOnlyFormats())
            {
                Debug.WriteLine("Audio: " + msi.Extension + " Aenc: " + msi.AudioCodec);
            }

            foreach (var msi in video.Formats.GetMuxedFormats())
            {
                Debug.WriteLine("Muxed: " + msi.Extension + " Venc: " + msi.VideoCodec + " Aenc: " + msi.AudioCodec + " Res: " + msi.Height + "x" + msi.Width);
            }

            foreach (var msi in video.Formats.GetVideoOnlyFormats())
            {
                Debug.WriteLine("Video: " + msi.Extension + " Venc: " + msi.VideoCodec + " Res: " + msi.Height + "x" + msi.Width);
            }

            try
            {
                switch (format)
                {
                    case MediaFormat.m4a:
                        info = set.Audio.First(x => x.AudioEncoding == AudioEncoding.Aac);
                        break;
                    case MediaFormat.aac:
                        info = set.Audio.First(x => x.AudioEncoding == AudioEncoding.Aac);
                        break;
                    case MediaFormat.webm:
                        info = set.Audio.First(x => x.AudioEncoding == AudioEncoding.Opus);
                        break;
                    case MediaFormat.ogg:
                        info = set.Audio.First(x => x.AudioEncoding == AudioEncoding.Vorbis);
                        break;
                    case MediaFormat.mp4:
                        info = set.Muxed.OrderByDescending(x => x.VideoQuality).First(x => x.VideoEncoding == VideoEncoding.H264);
                        break;
                }
            }
            catch
            {
                MediaStreamInfo i = GetConversionReadyStream(set,format);
                await yt.DownloadMediaStreamAsync(i, stream.AsStream(), progress);
                return true;
            }

            if (info == null)
            {
                MediaStreamInfo i = GetConversionReadyStream(set, format);
                await yt.DownloadMediaStreamAsync(i, stream.AsStream(), progress);
                return true;
            }

            Debug.WriteLine("Selected: " + info.Container + " Url: " + info.Url);
            Debug.WriteLine(info.Container.GetFileExtension());

            await yt.DownloadMediaStreamAsync(info, stream.AsStream(), progress);
            return false;
        }*/

        public static IAudioFormat GetConversionReadyStream(ICollection<IFormat> set, MediaFormat format)
        {
            Debug.WriteLine("Getting Conversion-Ready Stream");
            if (!MediaConverter.isVideoType(format))
            {
                IAudioFormat audio = set.WithBestAudioBitrate();
                if (audio == default)
                {
                    Debug.WriteLine("No aac stream, returning H264 stream");
                    IMuxedFormat muxed = set.MuxedWithBestResolution();
                    Debug.WriteLine("Selected: " + muxed.Extension + " Venc: " + muxed.VideoCodec + " Aenc: " + muxed.AudioCodec + " Res: " + muxed.Height + "p");
                    return muxed;
                }
                else
                {
                    Debug.WriteLine("Returning audio stream");
                    Debug.WriteLine("Selected: " + audio.Extension + " Aenc: " + audio.AudioCodec + " Bitrate: " + audio.AudioBitrate);
                    return audio;
                }
            }
            else
            {
                Debug.WriteLine("Video stream needed, returning H264 stream");
                IMuxedFormat muxed = set.MuxedWithBestResolution();
                Debug.WriteLine("Selected: " + muxed.Extension + " Venc: " + muxed.VideoCodec + " Aenc: " + muxed.AudioCodec + " Res: " + muxed.Height + "p");
                return muxed;
            }
        }

        public static Task<Uri> TryGetUriInMediaFormat(YouTubeDL yt, Video video, MediaFormat format)
        {
            Debug.WriteLine("Available:");
            IFormat info;

            var audiof = video.Formats.GetAudioOnlyFormats();
            var videof = video.Formats.GetVideoOnlyFormats();
            var muxedf = video.Formats.GetMuxedFormats();

            foreach (var msi in audiof)
            {
                Debug.WriteLine("Audio: " + msi.Extension + " Aenc: " + msi.AudioCodec);
            }

            foreach (var msi in muxedf)
            {
                Debug.WriteLine("Muxed: " + msi.Extension + " Venc: " + msi.VideoCodec + " Aenc: " + msi.AudioCodec + " Res: " + msi.Height + "x" + msi.Width);
            }

            foreach (var msi in videof)
            {
                Debug.WriteLine("Video: " + msi.Extension + " Venc: " + msi.VideoCodec + " Res: " + msi.Height + "x" + msi.Width);
            }

            try
            {
                switch (format)
                {
                    case MediaFormat.aac:
                        info = audiof.First(x => x.AudioCodec == "aac");
                        break;
                    case MediaFormat.webm:
                        info = audiof.First(x => x.AudioCodec == "opus");
                        break;
                    case MediaFormat.ogg:
                        info = audiof.First(x => x.AudioCodec == "vorbis");
                        break;
                    case MediaFormat.mp4:
                        info = muxedf.First(x => x.AudioCodec == "h264");
                        break;
                    default:
                        info = audiof.First(x => x.AudioCodec == "aac");
                        break;
                }
            }
            catch
            {

                if (!MediaConverter.isVideoType(format))
                {
                    info = video.Formats.WithBestAudioBitrate();
                }
                else
                {
                    info = video.Formats.WithBestVideoResolution();
                }

                Debug.WriteLine("selected: " + info.Extension);
                return Task.FromResult(new Uri(info.Url));
            }

            Debug.WriteLine("selected: " + info.Extension);
            return Task.FromResult(new Uri(info.Url));
        }

        /*
        public async Task DownloadAndTranscodeAsync(YoutubeDL.Models.Video video, StorageFile outputFile, MediaFormat targetFormat, Action<double> progressAction)
        {
            Debug.WriteLine("Creating RAM Stream");
            InMemoryRandomAccessStream untranscodedVideo = new InMemoryRandomAccessStream();

            Debug.WriteLine("Downloading");
            bool transcodingNeeded = await TryGetStreamInMediaFormat(yt, video, targetFormat, untranscodedVideo, progressAction);

            if (transcodingNeeded)
            {
                Debug.WriteLine("Transcoding and Writing to file");

                //StorageFile tempFile = await ApplicationData.Current.TemporaryFolder.CreateFileAsync("temptranscoding.tmp", CreationCollisionOption.GenerateUniqueName);

                //using (var fileStream1 = await tempFile.OpenAsync(FileAccessMode.ReadWrite))
                //{
                //    await RandomAccessStream.CopyAndCloseAsync(untranscodedVideo, fileStream1);
                //}

                untranscodedVideo.Seek(0);
                IRandomAccessStream source = untranscodedVideo.CloneStream();// await tempFile.OpenAsync(FileAccessMode.ReadWrite);
                await MediaConverter.Convert(source, outputFile, targetFormat, progressAction);
            }
            else
            {
                Debug.WriteLine("Writing to file: " + untranscodedVideo.Size);

                //Stream fileStream = await outputFile.OpenStreamForWriteAsync();
                //untranscodedVideo.Seek(0);
                //Stream mediaStream = untranscodedVideo.AsStream();

                //Debug.WriteLine("Bytes to Write: " + mediaStream.Length);

                //byte[] buffer = new byte[mediaStream.Length];
                //await mediaStream.ReadAsync(buffer, 0, buffer.Length);
                //await fileStream.WriteAsync(buffer, 0, buffer.Length);

                untranscodedVideo.Seek(0);

                using (var fileStream1 = await outputFile.OpenAsync(FileAccessMode.ReadWrite))
                {
                    await RandomAccessStream.CopyAndCloseAsync(untranscodedVideo, fileStream1);
                }

            }
            Debug.WriteLine("Finished.");
        }*/


        public async Task DownloadFormat(Video video, string format, StorageFile file)
        {

        }

        public async Task DownloadFormat(Video video, string format, string filepath)
        {
            
        }

        /*
        public PlaylistItem[] GetPlaylistItems(string playlistId)
        {
            string nextPageToken = "";
            List<PlaylistItem> playlistItems = new List<PlaylistItem>();
            while (true)
            {

                PlaylistItemsResource.ListRequest preq = new PlaylistItemsResource.ListRequest(null,null);// yts.PlaylistItems.List("snippet,contentDetails");

                preq.PlaylistId = playlistId;
                preq.MaxResults = 50;


                if (nextPageToken != "")
                {
                    preq.PageToken = nextPageToken;
                }

                PlaylistItemListResponse resp = preq.Execute();

                foreach (PlaylistItem item in resp.Items)
                {
                    if (item.Snippet.Title == "Private video" || item.Snippet.Title == "Deleted video")
                    {
                        continue;
                    }
                    playlistItems.Add(item);
                    Debug.WriteLine("got items: " + playlistItems.Count);
                }

                if (resp.NextPageToken == "" || resp.NextPageToken == null)
                {
                    return playlistItems.ToArray();
                }

                Debug.WriteLine("nextpagetoken: " + resp.NextPageToken);

                nextPageToken = resp.NextPageToken;
            }
        }

        public async Task<PlaylistItem[]> GetPlaylistItemsAsync(string playlistId)
        {
            string nextPageToken = "";
            List<PlaylistItem> playlistItems = new List<PlaylistItem>();
            while (true)
            {

                PlaylistItemsResource.ListRequest preq = new PlaylistItemsResource.ListRequest(null, null); //yts.PlaylistItems.List("snippet,contentDetails");

                preq.PlaylistId = playlistId;
                preq.MaxResults = 50;


                if (nextPageToken != "")
                {
                    preq.PageToken = nextPageToken;
                }

                PlaylistItemListResponse resp = await preq.ExecuteAsync();

                foreach (PlaylistItem item in resp.Items)
                {
                    if (item.Snippet.Title == "Private video" || item.Snippet.Title == "Deleted video")
                    {
                        continue;
                    }
                    playlistItems.Add(item);
                    Debug.WriteLine("got items: " + playlistItems.Count);
                }

                if (resp.NextPageToken == "" || resp.NextPageToken == null)
                {
                    return playlistItems.ToArray();
                }

                Debug.WriteLine("nextpagetoken: " + resp.NextPageToken);

                nextPageToken = resp.NextPageToken;
            }
        }

        public string[] GetPlaylistItemIds(string playlistId)
        {
            string nextPageToken = "";
            List<string> playlistItems = new List<string>();
            while (true)
            {

                PlaylistItemsResource.ListRequest preq = new PlaylistItemsResource.ListRequest(null, null); //yts.PlaylistItems.List("snippet,contentDetails");

                preq.PlaylistId = playlistId;
                preq.MaxResults = 50;


                if (nextPageToken != "")
                {
                    preq.PageToken = nextPageToken;
                }

                PlaylistItemListResponse resp = preq.Execute();

                foreach (PlaylistItem item in resp.Items)
                {
                    if (item.Snippet.Title == "Private video" || item.Snippet.Title == "Deleted video")
                    {
                        continue;
                    }
                    playlistItems.Add(item.ContentDetails.VideoId);
                    Debug.WriteLine("got items: " + playlistItems.Count);
                }

                if (resp.NextPageToken == "" || resp.NextPageToken == null)
                {
                    return playlistItems.ToArray();
                }

                Debug.WriteLine("nextpagetoken: " + resp.NextPageToken);

                nextPageToken = resp.NextPageToken;
            }
        }
        */
    }
}
