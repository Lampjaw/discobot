using Discord;
using Discord.Commands;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace discobot
{
    public class PublicModule : ModuleBase<SocketCommandContext>
    {
        private readonly IMusicManager _manager;

        public PublicModule(IMusicManager manager)
        {
            _manager = manager;
        }

        [Command("#play", RunMode = RunMode.Async)]
        public async Task PlayMusic([Remainder] string requestUrl)
        {
            try
            {
                var requestUri = new Uri(requestUrl);
                var channel = (Context.User as IVoiceState).VoiceChannel;
                var guildId = channel.GuildId.ToString();

                var username = $"{Context.User.Username}#{Context.User.Discriminator}";
                await _manager.AddMusicToQueue(requestUri, channel.GuildId.ToString(), username);
                await _manager.PlayMusicQueueAsync(channel);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        [Command("#shuffle", RunMode = RunMode.Async)]
        public async Task ShuffleMusic()
        {
            try
            {
                var guildId = Context.Guild.Id.ToString();

                _manager.ShuffleQueue(guildId);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        [Command("#clear", RunMode = RunMode.Async)]
        public async Task ClearMusic()
        {
            try
            {
                var guildId = Context.Guild.Id.ToString();

                _manager.ClearQueue(guildId);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        [Command("#np", RunMode = RunMode.Async)]
        public async Task NowPlaying()
        {
            try
            {
                var guildId = Context.Guild.Id.ToString();

                var np = _manager.GetNowPlaying(guildId);
                
                if (np == null)
                {
                    return;
                }

                var embed = new EmbedBuilder()
                    .WithTitle(np.Video.Title)
                    .WithUrl(np.Video.Url)
                    .WithAuthor("Now Playing♪", Context.Client.CurrentUser.GetAvatarUrl())
                    .WithDescription($"`Requested by:` {np.Requester}")
                    .Build();

                await ReplyAsync(embed: embed);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        [Command("#q", RunMode = RunMode.Async)]
        public async Task GetQueue()
        {
            try
            {
                var guildId = Context.Guild.Id.ToString();

                var queue = _manager.GetQueue(guildId);

                if (queue == null || !queue.Any())
                {
                    return;
                }

                var sb = new StringBuilder();

                var truncQueue = queue.Take(10).ToList();
                for(var i = 0; i < truncQueue.Count(); i++)
                {
                    sb.AppendLine($"`{i + 1}.` {truncQueue[i].Video.Title} | `{GetDurationString(truncQueue[i].Video.Duration)} Requested by: {truncQueue[i].Requester}`\n");
                }

                var totalDuration = new TimeSpan(queue.Sum(a => a.Video.Duration.Ticks));

                sb.AppendLine($"**{queue.Count()} songs in queue | {totalDuration}**");

                var embed = new EmbedBuilder()
                    .WithTitle($"Queue for {Context.Guild.Name}")
                    .WithDescription(sb.ToString())
                    .Build();

                await ReplyAsync(embed: embed);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private string GetDurationString(TimeSpan ts)
        {
            var duration = "";

            if (ts.Hours > 1)
            {
                duration += $"{ts.Hours}:";
            }

            duration += $"{PadInt(ts.Minutes)}:{PadInt(ts.Seconds)}";

            return duration;
        }

        private string PadInt(int value)
        {
            return value > 9 ? value.ToString() : $"0{value}";
        }

        [Command("#skip", RunMode = RunMode.Async)]
        public async Task SkipMusic()
        {
            try
            {
                var guildId = Context.Guild.Id.ToString();

                _manager.SkipCurrentPlaying(guildId);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}
