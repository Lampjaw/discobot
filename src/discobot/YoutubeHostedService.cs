using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;
using Discord.WebSocket;
using Discord;
using Discord.Commands;
using Microsoft.Extensions.Options;

namespace discobot
{
    public class YoutubeHostedService : IHostedService
    {
        private readonly IServiceProvider _services;
        private readonly CommandService _commands;
        private readonly DiscordSocketClient _discordClient;
        private readonly BotOptions _options;

        public YoutubeHostedService(IServiceProvider services, CommandService commands, DiscordSocketClient discordClient, IOptions<BotOptions> options)
        {
            _services = services;
            _commands = commands;
            _discordClient = discordClient;
            _options = options.Value;

            _discordClient.MessageReceived += MessageReceivedAsync;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _commands.AddModuleAsync<PublicModule>(_services);

            await _discordClient.LoginAsync(TokenType.Bot, _options.DiscordToken);
            await _discordClient.StartAsync();
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _discordClient.StopAsync();
        }

        public async Task MessageReceivedAsync(SocketMessage rawMessage)
        {
            if (!(rawMessage is SocketUserMessage message)) return;
            if (message.Source != MessageSource.User) return;

            var context = new SocketCommandContext(_discordClient, message);

            await _commands.ExecuteAsync(context, 0, _services);
        }
    }
}
