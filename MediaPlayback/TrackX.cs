using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YoutubeDL;
using YoutubeDL.Models;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.Data.Json;
using System.IO;
//using YoutubeDL.Uwp.Postprocessors;


namespace NCSMusic
{
    public class TrackX
    {
        string title;
        string videoId;
        string author;
        string description;
        TimeSpan duration;

        Video ytvideo;
        StorageFile savedtrack;

        static YouTubeApi ytApi = new YouTubeApi();

        // neccesary for xaml property access
        public TrackX Self => this;
        public string Title => title;
        public string VideoID => videoId;
        public string Author => author;
        public string Description => description;
        public TimeSpan Duration => duration;
        public bool IsYTVideoLinked => ytvideo != null;
        public bool IsSaveFileLinked => savedtrack != null;
        public string ViewsString => ytvideo != null ? (ytvideo.Views != 0 ? ytvideo.Views.ToString("n") + " Views" : "") : "";
        public StorageFile SavedTrack => savedtrack;

        public long ViewCount { get; set; }
        public long LikeCount { get; set; }
        public long DislikeCount { get; set; }

        public TrackX(Video trackYTVideo)
        {
            title = trackYTVideo.Title;
            videoId = trackYTVideo.Id;
            author = trackYTVideo.Uploader;
            description = trackYTVideo.Description;
            duration = trackYTVideo.Duration;
            ytvideo = trackYTVideo;
        }

        public TrackX(ContentUrl trackYTVideo)
        {
            title = (string)trackYTVideo.AdditionalProperties["title"];
            videoId = (string)trackYTVideo.AdditionalProperties["id"];
            author = "Not loaded";
            duration = TimeSpan.Zero;
        }

        public TrackX(StorageFile trackYTVideo, JsonObject snMeta)
        {
            savedtrack = trackYTVideo;

            JsonObject trackx = snMeta.GetNamedObject("trackX");
            JsonObject statisticsJson = trackx.GetNamedObject("statistics");

            title = trackx.GetNamedString("title");
            videoId = trackx.GetNamedString("videoId");
            author = trackx.GetNamedString("author");
            description = trackx.GetNamedValue("description").ValueType == JsonValueType.Null ? null : trackx.GetNamedString("description");
            duration = TimeSpan.Parse(trackx.GetNamedString("duration"));

            ViewCount = (long)statisticsJson.GetNamedNumber("viewCount");
            LikeCount = (long)statisticsJson.GetNamedNumber("likeCount");
            DislikeCount = (long)statisticsJson.GetNamedNumber("dislikeCount");
        }

        public Uri GetUrl()
        {
            return new Uri("https://www.youtube.com/watch?v=" + videoId);
        }

        public async Task<Uri> GetDownloadUri(bool onlyAudio)
        {
            var vid = await GetOrFetchVideo(true);
            return await ytApi.GetVideoLink(vid, onlyAudio);
        }

        public async Task<Video> GetOrFetchVideo(bool includeFormats = false)
        {
            if (!IsYTVideoLinked)
            {
                ytvideo = await ytApi.Fetch<Video>(VideoID);
                title = ytvideo.Title;
                videoId = ytvideo.Id;
                author = ytvideo.Uploader;
                description = ytvideo.Description;
                duration = ytvideo.Duration;
            }

            if (includeFormats) ytvideo.Formats = await YoutubeExplodeVideoIE.GetStreamData(ytvideo);
            return ytvideo;
        }

        public async Task DeleteSaveFile()
        {
            StorageFolder folder = await savedtrack.GetParentAsync();
            await savedtrack.DeleteAsync();
            foreach (StorageFile file in (await folder.GetFilesAsync()).Where(x => x.DisplayName == videoId))
            {
                await file.DeleteAsync();
            }
            savedtrack = null;
        }

        public static async Task<TrackX> FromVideoId(string videoId)
        {
            var ix = await YouTubeApi.yt.ExtractInfoAsync(videoId);
            if (ix is Video v)
            {
                return new TrackX(v);
            }
            throw new NotSupportedException();
        }

        public static async Task<TrackX> FromSNMeta(StorageFile trackYTVideo, StorageFile snMeta)
        {
            Stream snmetaStream = await snMeta.OpenStreamForReadAsync();
            StreamReader snmetaReader = new StreamReader(snmetaStream);
            string snmetaString = await snmetaReader.ReadToEndAsync();
            JsonObject snmetaJson = JsonObject.Parse(snmetaString);
            return new TrackX(trackYTVideo, snmetaJson);
        }

        public static async Task<TrackX[]> FromSNMetaFolder(StorageFolder snMetaFolder)
        {
            List<Task<TrackX>> lstTasks = new List<Task<TrackX>>();
            List<StorageFile> lstMatch = new List<StorageFile>();
            foreach (StorageFile file in await snMetaFolder.GetFilesAsync())
            {
                if (file.FileType == ".snmeta")
                {
                    try
                    {
                        StorageFile snmusic = lstMatch.FirstOrDefault(x => x.FileType == ".snmusic" && x.DisplayName == file.DisplayName);
                        if (snmusic == default(StorageFile))
                        {
                            lstMatch.Add(file);
                        }
                        else
                        {
                            lstTasks.Add(TrackX.FromSNMeta(snmusic, file));
                            lstMatch.Remove(snmusic);
                        }
                    }
                    catch
                    {

                    }
                }
                else if (file.FileType == ".snmusic")
                {
                    try
                    {
                        StorageFile snmeta = lstMatch.FirstOrDefault(x => x.FileType == ".snmeta" && x.DisplayName == file.DisplayName);
                        if (snmeta == default(StorageFile))
                        {
                            lstMatch.Add(file);
                        }
                        else
                        {
                            lstTasks.Add(TrackX.FromSNMeta(file, snmeta));
                            lstMatch.Remove(snmeta);
                        }
                    }
                    catch
                    {

                    }
                }
            }

            return await Task.WhenAll(lstTasks);
        }

        public void AddYTVideoDefinition(Video video)
        {
            ytvideo = video;
        }

        public async Task GetYoutubeVideo()
        {
            if (!IsYTVideoLinked)
            {
                var xytvideo = await YouTubeApi.yt.ExtractInfoAsync(videoId);
                if (xytvideo is Video v)
                {
                    ytvideo = v;
                }
                else throw new NotSupportedException();
            }
        }

        public Task DownloadYTVideo(StorageFile outputFile, string formatSpec, Action<double> progressAction = null)
        {
            return DownloadYTVideo(outputFile.Path, formatSpec, progressAction);
        }

        public async Task DownloadYTVideo(string outputLocation, string formatSpec, Action<double> progressAction = null)
        {
            var vid = await GetOrFetchVideo(true);

            var fmt = vid.Formats.SelectFormats(formatSpec).FirstOrDefault();

            if (fmt == default)
            {
                return;
            }

            await YouTubeApi.yt.DownloadFormat(outputLocation, fmt);
        }

        public async Task DownloadAudio(StorageFile output, Action<double> progressAction = null)
        {
            var vid = await GetOrFetchVideo();
            if (vid.Formats == null)
                vid.Formats = await YoutubeExplodeVideoIE.GetStreamData(vid);
            await YouTubeApi.DownloadAudio(vid, output, progressAction);
        }

        public async Task DownloadAndSerializeToDisk(StorageFolder folder, Action<double> progressAction = null)
        {
            JsonObject json = new JsonObject();
            JsonObject trackx = new JsonObject();
            JsonObject statistics = new JsonObject();

            statistics.SetNamedValue("viewCount", JsonValue.CreateNumberValue(ViewCount));
            statistics.SetNamedValue("likeCount", JsonValue.CreateNumberValue(LikeCount));
            statistics.SetNamedValue("dislikeCount", JsonValue.CreateNumberValue(DislikeCount));

            trackx.SetNamedValue("title", JsonValue.CreateStringValue(title));
            trackx.SetNamedValue("videoId", JsonValue.CreateStringValue(videoId));
            trackx.SetNamedValue("author", JsonValue.CreateStringValue(author));
            trackx.SetNamedValue("description", description == null ? JsonValue.CreateNullValue() : JsonValue.CreateStringValue(description));
            trackx.SetNamedValue("duration", JsonValue.CreateStringValue(duration.ToString()));
            trackx.SetNamedValue("statistics", statistics);

            json.SetNamedValue("trackX", trackx);

            string strJson = json.Stringify();

            StorageFile snmeta = await folder.CreateFileAsync(videoId + ".snmeta" , CreationCollisionOption.ReplaceExisting);
            StorageFile snmusic = await folder.CreateFileAsync(videoId + ".snmusic", CreationCollisionOption.ReplaceExisting);

            Stream snmetaStream = await snmeta.OpenStreamForWriteAsync();
            StreamWriter snmetaWriter = new StreamWriter(snmetaStream);

            await snmetaWriter.WriteAsync(strJson);
            await DownloadAudio(snmusic, progressAction);

            savedtrack = snmusic;

            await snmetaWriter.FlushAsync();
            snmetaWriter.Dispose();
        }

        public async Task DownloadAndSerializeToDisk(string folderPath, Action<double> progressAction = null)
        {
            StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(folderPath);
            await DownloadAndSerializeToDisk(folder, progressAction);
        }
    }
}
