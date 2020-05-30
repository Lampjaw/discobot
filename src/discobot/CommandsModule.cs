using Discord;
using Discord.Commands;
using System;
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
                    .WithDescription($"Requested by: {np.Requester}")
                    .Build();

                await ReplyAsync(embed: embed);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
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
