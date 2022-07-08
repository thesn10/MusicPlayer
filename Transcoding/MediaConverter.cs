using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.Media.Editing;
using Windows.Media.MediaProperties;
using Windows.Storage.Pickers;
using Windows.Media.Transcoding;
using Windows.Media.Playback;
using Windows.Media.Core;

namespace NCSMusic
{
    class MediaConverter
    {

        public static async Task Convert(IRandomAccessStream source, StorageFile destination, MediaFormat format, Action<double> onProgress)
        {
            IRandomAccessStream dstStream = await destination.OpenAsync(FileAccessMode.ReadWrite);
            await Convert(source, dstStream, format, onProgress);
        }

        public static async Task Convert(StorageFile source, IRandomAccessStream destination, MediaFormat format, Action<double> onProgress)
        {
            IRandomAccessStream srcStream = await source.OpenAsync(FileAccessMode.ReadWrite);
            await Convert(srcStream, destination, format, onProgress);
        }

        public static async Task Convert(StorageFile source, StorageFile destination, MediaFormat format, Action<double> onProgress)
        {
            IRandomAccessStream srcStream = await source.OpenAsync(FileAccessMode.ReadWrite);
            IRandomAccessStream dstStream = await destination.OpenAsync(FileAccessMode.ReadWrite);
            await Convert(srcStream, dstStream, format, onProgress);
        }

        public static async Task Convert(IRandomAccessStream source, IRandomAccessStream destination, MediaFormat format, Action<double> onProgress)
        {
            MediaTranscoder transcoder = new MediaTranscoder();
            MediaEncodingProfile profile = GetProfile(format);
            try
            {
                Debug.WriteLine("MEP Container Properties: ContainerType: " + profile.Container.Type + " ContainerSubtype: " + profile.Container.Subtype);
                Debug.WriteLine("MEP Audio Properties: AudioType: " + profile.Audio.Type + " AudioSubtype: " + profile.Audio.Subtype + " Bitrate: " + profile.Audio.Bitrate);
               
            }
            catch
            {

            }
            try
            {
                Debug.WriteLine("MEP Video Properties: VideoType: " + profile.Video.Type + " VideoSubtype: " + profile.Audio.Subtype + " Framerate " + profile.Video.FrameRate + " Bounds: " + profile.Video.Width + "x" + profile.Video.Width);
            }
            catch
            {

            }

            PrepareTranscodeResult prepareOp = await transcoder.PrepareStreamTranscodeAsync(source, destination, profile);

            if (prepareOp.CanTranscode)
            {
                var transcodeOp = prepareOp.TranscodeAsync();

                transcodeOp.Progress +=
                    new AsyncActionProgressHandler<double>(delegate(IAsyncActionWithProgress<double> asyncInfo, double percent) 
                    {
                        onProgress(percent / 100);
                    });

                await transcodeOp;
            }
            else
            {
                switch (prepareOp.FailureReason)
                {
                    case TranscodeFailureReason.CodecNotFound:
                        Debug.WriteLine("Codec not found.");
                        break;
                    case TranscodeFailureReason.InvalidProfile:
                        Debug.WriteLine("Invalid profile.");
                        break;
                    case TranscodeFailureReason.Unknown:
                        Debug.WriteLine("Unknown failure.");
                        break;
                    default:
                        Debug.WriteLine("Unknown failure.");
                        break;
                }
            }
        }

        public static async void MergeAudioAndVideo(StorageFile video, StorageFile audio, StorageFile output)
        {
            MediaComposition composition = new MediaComposition();

            MediaClip clip = await MediaClip.CreateFromFileAsync(video);
            composition.Clips.Add(clip);

            BackgroundAudioTrack backgroundTrack = await BackgroundAudioTrack.CreateFromFileAsync(audio);
            composition.BackgroundAudioTracks.Add(backgroundTrack);

            await composition.RenderToFileAsync(output, MediaTrimmingPreference.Precise, GetProfile(MediaFormat.mp4));
        }

        public static async void MergeAudioAndVideo(IRandomAccessStream video, IRandomAccessStream audio, IRandomAccessStream output)
        {
            MediaComposition composition = new MediaComposition();

            StorageFile vfile = await ApplicationData.Current.TemporaryFolder.CreateFileAsync("video.mp4", CreationCollisionOption.GenerateUniqueName);
            IRandomAccessStream vfileStream = await vfile.OpenAsync(FileAccessMode.ReadWrite);
            video.Seek(0);
            await RandomAccessStream.CopyAndCloseAsync(video,vfileStream);


            MediaClip clip = await MediaClip.CreateFromFileAsync(vfile);
            composition.Clips.Add(clip);

            StorageFile afile = await ApplicationData.Current.TemporaryFolder.CreateFileAsync("audio.m4a", CreationCollisionOption.GenerateUniqueName);
            IRandomAccessStream afileStream = await afile.OpenAsync(FileAccessMode.ReadWrite);
            audio.Seek(0);
            await RandomAccessStream.CopyAndCloseAsync(audio, afileStream);

            //string ffmpegcmd = "ffmpeg -i " + vfile.Path + " -i " + afile.Path + " -c copy " + ApplicationData.Current.TemporaryFolder.Path + "/output.mp4";

            BackgroundAudioTrack backgroundTrack = await BackgroundAudioTrack.CreateFromFileAsync(afile);
            composition.BackgroundAudioTracks.Add(backgroundTrack);

            StorageFile ofile = await ApplicationData.Current.TemporaryFolder.CreateFileAsync("merged.temp", CreationCollisionOption.GenerateUniqueName);

            await composition.RenderToFileAsync(ofile,MediaTrimmingPreference.Precise,GetProfile(MediaFormat.mp4));

            IRandomAccessStream ofileStream = await ofile.OpenAsync(FileAccessMode.ReadWrite);
            output.Seek(0);
            await RandomAccessStream.CopyAsync(ofileStream, output);
            output.Seek(0);

            await vfile.DeleteAsync();
            await afile.DeleteAsync();
            await ofile.DeleteAsync();
            return;
        }

        public static async void MergeAudioAndVideo(IRandomAccessStream video, IRandomAccessStream audio, StorageFile output)
        {
            MediaComposition composition = new MediaComposition();

            StorageFile vfile = await ApplicationData.Current.TemporaryFolder.CreateFileAsync("video.mp4", CreationCollisionOption.GenerateUniqueName);
            IRandomAccessStream vfileStream = await vfile.OpenAsync(FileAccessMode.ReadWrite);
            video.Seek(0);
            await RandomAccessStream.CopyAndCloseAsync(video, vfileStream);

            MediaClip clip = await MediaClip.CreateFromFileAsync(vfile);
            composition.Clips.Add(clip);

            StorageFile afile = await ApplicationData.Current.TemporaryFolder.CreateFileAsync("audio.m4a", CreationCollisionOption.GenerateUniqueName);
            IRandomAccessStream afileStream = await afile.OpenAsync(FileAccessMode.ReadWrite);
            audio.Seek(0);
            await RandomAccessStream.CopyAndCloseAsync(audio, afileStream);

            BackgroundAudioTrack backgroundTrack = await BackgroundAudioTrack.CreateFromFileAsync(afile);
            composition.BackgroundAudioTracks.Add(backgroundTrack);

            await composition.RenderToFileAsync(output, MediaTrimmingPreference.Precise, GetProfile(MediaFormat.mp4));

            await vfile.DeleteAsync();
            await afile.DeleteAsync();
            return;
        }

        public static MediaEncodingProfile GetProfileEx(MediaFormat format)
        {
            switch (format)
            {
                case MediaFormat.alac:
                    return MediaEncodingProfile.CreateAlac(AudioEncodingQuality.High);

                case MediaFormat.avi:
                    return MediaEncodingProfile.CreateAvi(VideoEncodingQuality.Auto);

                case MediaFormat.flac:
                    return MediaEncodingProfile.CreateFlac(AudioEncodingQuality.High);

                case MediaFormat.hvec:
                    return MediaEncodingProfile.CreateHevc(VideoEncodingQuality.Auto);

                case MediaFormat.m4a:
                    return MediaEncodingProfile.CreateM4a(AudioEncodingQuality.High);

                case MediaFormat.mp3:
                    return MediaEncodingProfile.CreateMp3(AudioEncodingQuality.High);

                case MediaFormat.mp4:
                    return MediaEncodingProfile.CreateMp4(VideoEncodingQuality.Auto);

                case MediaFormat.wav:
                    return MediaEncodingProfile.CreateWav(AudioEncodingQuality.High);

                case MediaFormat.wma:
                    return MediaEncodingProfile.CreateWma(AudioEncodingQuality.High);

                case MediaFormat.wmv:
                    return MediaEncodingProfile.CreateWmv(VideoEncodingQuality.Auto);
                default:
                    return null;
            }
        }


        public static MediaEncodingProfile GetProfile(MediaFormat format)
        {
            switch (format)
            {
                case MediaFormat.alac:
                    return MediaEncodingProfile.CreateAlac(AudioEncodingQuality.High);

                case MediaFormat.avi:
                    return MediaEncodingProfile.CreateAvi(VideoEncodingQuality.HD1080p);

                case MediaFormat.flac:
                    return MediaEncodingProfile.CreateFlac(AudioEncodingQuality.High);

                case MediaFormat.hvec:
                    return MediaEncodingProfile.CreateHevc(VideoEncodingQuality.HD1080p);

                case MediaFormat.m4a:
                    return MediaEncodingProfile.CreateM4a(AudioEncodingQuality.High);

                case MediaFormat.mp3:
                    return MediaEncodingProfile.CreateMp3(AudioEncodingQuality.High);

                case MediaFormat.mp4:
                    return MediaEncodingProfile.CreateMp4(VideoEncodingQuality.HD1080p);

                case MediaFormat.wav:
                    return MediaEncodingProfile.CreateWav(AudioEncodingQuality.High);

                case MediaFormat.wma:
                    return MediaEncodingProfile.CreateWma(AudioEncodingQuality.High);

                case MediaFormat.wmv:
                    return MediaEncodingProfile.CreateWmv(VideoEncodingQuality.HD1080p);
                default:
                    return null;
            }
        }

        public static bool isVideoType(MediaFormat f)
        {
            if (f == MediaFormat.mp3 || 
                f == MediaFormat.m4a || 
                f == MediaFormat.alac || 
                f == MediaFormat.flac || 
                f == MediaFormat.wav || 
                f == MediaFormat.wma || 
                f == MediaFormat.aac || 
                f == MediaFormat.webm || 
                f == MediaFormat.ogg)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

    }

    public enum MediaFormat
    {
        mp3,
        m4a,
        alac,
        flac,
        wav,
        wma,

        hvec,
        mp4,
        wmv,
        avi,

        aac,
        webm,
        ogg
    }


}
