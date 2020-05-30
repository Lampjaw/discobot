using Discord;
using System;
using System.Threading.Tasks;

namespace discobot
{
    public interface IMusicManager
    {
        Task AddMusicToQueue(Uri requestUri, string guildId, string requesterName);
        Task PlayMusicQueueAsync(IVoiceChannel channel);
        QueueVideo GetNowPlaying(string guildId);
        void ClearQueue(string guildId);
        void ShuffleQueue(string guildId);
        void SkipCurrentPlaying(string guildId);
    }
}