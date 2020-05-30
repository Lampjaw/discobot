using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace discobot
{
    static class Program
    {
        public static async Task Main(string[] args)
        {
            var hostBuilder = new HostBuilder()
                .ConfigureAppConfiguration(builder =>
                {
                    builder.SetBasePath(Directory.GetCurrentDirectory());
                    builder.AddJsonFile("appsettings.json", optional: true);
                    builder.AddEnvironmentVariables();
                })
                .ConfigureServices((context, services) =>
                {
                    services.AddOptions();
                    services.Configure<BotOptions>(context.Configuration);

                    services.AddSingleton<IMusicManager, MusicManager>();
                    services.AddSingleton<DiscordSocketClient>();
                    services.AddSingleton<CommandService>();
                    services.AddHostedService<YoutubeHostedService>();
                }).ConfigureLogging((context, builder) =>
                {
                    builder.AddConsole();
                });

            await hostBuilder.RunConsoleAsync();
        }
    }
}
