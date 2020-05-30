using Discord;
using Discord.Audio;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Downloader;
using YoutubeExplode;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;

namespace discobot
{
    public class MusicManager : IMusicManager
    {
        private readonly YoutubeClient _ytClient;
        private readonly Dictionary<string, Queue<QueueVideo>> _videoQueue = new Dictionary<string, Queue<QueueVideo>>();
        private readonly Dictionary<string, bool> _guildPlayState = new Dictionary<string, bool>();
        private readonly Dictionary<string, CancellationTokenSource> _guildSongState = new Dictionary<string, CancellationTokenSource>();

        private static readonly string _tmpDir = Directory.CreateDirectory("./tmp").FullName;
        private static Func<string, string> _getGuildPath = (guildId) => Directory.CreateDirectory($"{_tmpDir}/{guildId}").FullName;
        private Func<string, string, string> _getPath = (guildId, videoId) => $"{_getGuildPath(guildId)}/{videoId}.pcm";

        public MusicManager()
        {
            _ytClient = new YoutubeClient();

            FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official).Wait();

            if (Directory.Exists(_tmpDir))
            {
                Directory.Delete(_tmpDir, true);
            }
        }

        public async Task AddMusicToQueue(Uri requestUri, string guildId, string requesterName)
        {
            if (!_videoQueue.ContainsKey(guildId))
            {
                _videoQueue.Add(guildId, new Queue<QueueVideo>());
            }

            if (requestUri.ToString().Contains("playlist"))
            {
                var playlistId = HttpUtility.ParseQueryString(requestUri.Query)["list"];
                var playlistVideos = await GetPlaylistAsync(playlistId);
                playlistVideos.Select(a => new QueueVideo(a, requesterName)).ToList().ForEach(_videoQueue[guildId].Enqueue);
            }
            else
            {
                var videoId = HttpUtility.ParseQueryString(requestUri.Query)["v"];
                var video = await GetVideoAsync(videoId);
                _videoQueue[guildId].Enqueue(new QueueVideo(video, requesterName));
            }
        }

        public async Task PlayMusicQueueAsync(IVoiceChannel channel)
        {
            var guildId = channel.GuildId.ToString();

            if (_guildPlayState.ContainsKey(guildId) && _guildPlayState[guildId] == true)
            {
                return;
            }

            _guildPlayState[guildId] = true;

            using (var vcConn = await channel.ConnectAsync())
            using (var stream = vcConn.CreatePCMStream(AudioApplication.Music))
            {
                while (_videoQueue[guildId].Any())
                {
                    var source = new CancellationTokenSource();
                    _guildSongState[guildId] = source;

                    var item = _videoQueue[guildId].First();
                    var audioFile = await DownloadAudioAsync(guildId, item.Video);

                    await vcConn.SetSpeakingAsync(true);

                    try
                    {
                        using (Stream input = File.OpenRead(audioFile))
                        {
                            await input.CopyToAsync(stream, source.Token);
                        }

                        await stream.FlushAsync(source.Token);
                    }
                    catch (OperationCanceledException) { }
                    finally
                    {
                        _videoQueue[guildId].Dequeue();
                        File.Delete(audioFile);

                        await vcConn.SetSpeakingAsync(false);
                    }
                }
            }

            _guildPlayState.Remove(guildId);
            _videoQueue.Remove(guildId);
            _guildSongState.Remove(guildId);
        }

        public QueueVideo GetNowPlaying(string guildId)
        {
            if (!_videoQueue.ContainsKey(guildId))
            {
                return null;
            }

            return _videoQueue[guildId].First();
        }

        public void ClearQueue(string guildId)
        {
            if (!_videoQueue.ContainsKey(guildId))
            {
                return;
            }

            var npTmp = _videoQueue[guildId].First();
            _videoQueue[guildId].Clear();
            _videoQueue[guildId].Enqueue(npTmp);
        }

        public void ShuffleQueue(string guildId)
        {
            if (!_videoQueue.ContainsKey(guildId))
            {
                return;
            }

            Random rng = new Random();
            var tmp = _videoQueue[guildId].ToList();
            var npTmp = tmp.First();
            tmp.Remove(npTmp);

            int n = tmp.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                var value = tmp[k];
                tmp[k] = tmp[n];
                tmp[n] = value;
            }

            tmp.Insert(0, npTmp);
            _videoQueue[guildId] = new Queue<QueueVideo>(tmp);
        }

        public void SkipCurrentPlaying(string guildId)
        {
            if (!_guildSongState.ContainsKey(guildId))
            {
                return;
            }

            _guildSongState[guildId].Cancel();
        }

        public IEnumerable<QueueVideo> GetQueue(string guildId)
        {
            if (!_videoQueue.ContainsKey(guildId))
            {
                return null;
            }

            return _videoQueue[guildId].ToList();
        }

        private async Task<IEnumerable<Video>> GetPlaylistAsync(string playlistId)
        {
            return await _ytClient.Playlists.GetVideosAsync(playlistId);
        }

        private async Task<Video> GetVideoAsync(string videoId)
        {
            return await _ytClient.Videos.GetAsync(videoId);
        }

        private async Task<string> DownloadAudioAsync(string guildId, Video video)
        {
            var fileName = _getPath(guildId, video.Id);

            if (!File.Exists(fileName))
            {
                var manifest = await _ytClient.Videos.Streams.GetManifestAsync(video.Id);
                var streamInfo = manifest.GetAudioOnly().WithHighestBitrate();

                var mediaInfo = await FFmpeg.GetMediaInfo(streamInfo.Url);

                var conv = FFmpeg.Conversions.New()
                    .AddStream(mediaInfo.AudioStreams)
                    .SetOutputFormat(Xabe.FFmpeg.Format.s16le)
                    .SetAudioBitrate(streamInfo.Bitrate.BitsPerSecond)
                    .SetOutput(fileName)
                    .SetOverwriteOutput(true);

                await conv.Start();
            }

            return fileName;
        }
    }
}
