using Discord;
using YoutubeExplode.Videos;

namespace discobot
{
    public class QueueVideo
    {
        public QueueVideo(Video video, string requester)
        {
            Video = video;
            Requester = requester;
        }

        public Video Video { get; }
        public string Requester { get; }
    }
}
