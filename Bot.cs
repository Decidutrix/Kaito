using System;
using System.Net.NetworkInformation;
using System.Reflection.Metadata;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Collections.Generic;
using System.Net.Sockets;

namespace Kaito
{
    class Bot
    {
        private readonly DiscordSocketClient _client;
        private readonly Dictionary<string, Func<SocketMessage, Task>> _commands;
        private readonly Timer _timer;
        private readonly IConfiguration _configuration;

        
        public Bot()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddUserSecrets<Bot>();

            _configuration = builder.Build();
   
            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMembers | GatewayIntents.GuildMessages
            });

            _client.Log += LogAsync;
            _client.Ready += ReadyAsync;
            _client.MessageReceived += MessageRecievedAsync;
            _client.UserJoined += UserJoinedAsync;

            _commands = new Dictionary<string, Func<SocketMessage, Task>>
            {
                { "!ping", PingAsync },
                { "!specs", SpecsAsync },
            };

            // Set up a timer to check every 5 minutes to see if I'm live (300,000 ms)
            _timer = new Timer(async _ => await CheckYoutubeLiveAsync(), null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
        }
        

        public async Task RunAsynce()
        {
            var token = _configuration["ApiKeys:DiscordBotToken"];

            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            await Task.Delay(-1);
        }

        private Task LogAsync(LogMessage log)
        {
            Console.WriteLine(log);
            return Task.CompletedTask;
        }

        private Task ReadyAsync()
        {
            Console.WriteLine($"Connected as {_client.CurrentUser}");
            return Task.CompletedTask;
        }

        private async Task MessageRecievedAsync(SocketMessage message)
        {
            if (message.Author.IsBot) return;

            if (_commands.TryGetValue(message.Content, out var command))
            {
                await command(message);
            }
        }


        // Command Responses

        private async Task PingAsync(SocketMessage message)
        {
            await message.Channel.SendMessageAsync("Pong!");
        }

        private async Task SpecsAsync(SocketMessage message)
        {
            await message.Channel.SendMessageAsync("Here's the current specs that Pochama's running https://pcpartpicker.com/list/CbqrTY");
        }


        // Assigning Roles

        private async Task UserJoinedAsync(SocketGuildUser user)
        {
            Console.WriteLine($"User joined: {user.Username}");
            var role = user.Guild.Roles.FirstOrDefault(r => r.Name == "tester");

            if (role == null)
            {
                Console.WriteLine("Role not found or typo in role name.");
            }
            try
            {
                await user.AddRoleAsync(role);
                Console.WriteLine($"Successfully Assigned {role.Name} role to {user.Username}");
            }
            catch
            {
                Console.WriteLine($"Error assigning role.");
            }
        }

        private string _lastNotifiedStreamId;

        // Live check
        private async Task CheckYoutubeLiveAsync()
        {
            var apiKey = _configuration["ApiKeys:YoutubeApiKey"];

            if (string.IsNullOrEmpty(apiKey))
            {
                Console.WriteLine("API Key is not set.");
                return;
            }

            var channelId = "UCXziJO5wZ-tgSHG7XcxbJJQ";

            var youtubeService = new YouTubeService(new BaseClientService.Initializer
            {
                ApiKey = apiKey,
                ApplicationName = this.GetType().ToString()
            });

            var searchRequest = youtubeService.Search.List("snippet");
            searchRequest.ChannelId = channelId;
            searchRequest.Type = "video";
            searchRequest.EventType = SearchResource.ListRequest.EventTypeEnum.Live;

            var searchResponse = await searchRequest.ExecuteAsync();

            if (searchResponse.Items.Count > 0)
            {
                var liveStream = searchResponse.Items.FirstOrDefault();

                if (liveStream != null)
                {
                    var currentStreamId = liveStream.Id.VideoId;

                    if (_lastNotifiedStreamId != currentStreamId)
                    {
                        var liveUrl = $"https://www.youtube.com/watch?v={liveStream.Id.VideoId}";

                        var channel = _client.GetGuild(1277825987278540965).GetTextChannel(1277825987278540968);
                        await channel.SendMessageAsync($"🔴 @here ** Parzinox started doing shenanigans again! Come check out what he's doing live on YouTube!**\nWatch here: {liveUrl}");

                        // Checks for a repeat notif
                        _lastNotifiedStreamId = currentStreamId;
                    }
                }
            }
            else
            {
                Console.WriteLine("No live stream found.");
            }
        }
    }


}
