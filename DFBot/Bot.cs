using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using Services;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DFBot
{
    class Bot
    {
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commands;
        public bool IsRunning { get; private set; }
        public ulong MyId { get { return _client.CurrentUser.Id; } }
        //ServerID -> Channel
        internal Dictionary<string, IMessageChannel> NotificationChannels { get; set; }
        //ServerID -> [outpostName / role]
        internal Dictionary<string, HashSet<string>> NotificationsSent { get; private set; }

        internal Bot()
        {
            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Info,
            });

            _commands = new CommandService(new CommandServiceConfig
            {
                LogLevel = LogSeverity.Info,
                CaseSensitiveCommands = false,
            });

            _client.Log += Log;
            _commands.Log += Log;

            _client.JoinedGuild += JoinedNewGuild;
            _client.SetGameAsync("Use .help");
            _client.Ready += BotReadyEvent;

            NotificationChannels = new Dictionary<string, IMessageChannel>();
            NotificationsSent = new Dictionary<string, HashSet<string>>();
        }
        private static Task Log(LogMessage message)
        {
            switch (message.Severity)
            {
                case LogSeverity.Critical:
                case LogSeverity.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case LogSeverity.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case LogSeverity.Info:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                case LogSeverity.Verbose:
                case LogSeverity.Debug:
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    break;
            }
            Console.WriteLine($"{DateTime.Now,-19} [{message.Severity,8}] {message.Source}: {message.Message} {message.Exception}");
            Console.ResetColor();

            return Task.CompletedTask;
        }

        private async Task JoinedNewGuild(SocketGuild arg)
        {
            await SendJoinedMessage(arg);
            await CreateSubscriptionRoles(arg);
        }

        private async Task CreateSubscriptionRoles(SocketGuild arg)
        {
            var outposts = OutpostService.GetAll();
            try
            {
                foreach (string outpost in outposts)
                    if (!RoleExists(outpost, arg))
                    {
                        await arg.CreateRoleAsync(outpost);
                    }
            }
            catch (HttpException)
            {
                await SendMessageAtDefaultChannel("I don't have permissions to create the roles for the pings", arg);
            }
        }

        private bool RoleExists(string outpost, SocketGuild arg)
        {
            return arg.Roles.Any(r => r.Name == outpost);
        }
        private async Task SendMessageAtDefaultChannel(string message, SocketGuild arg)
        {
            SocketTextChannel myDefaultChannel = GetDefaultChannel(arg);
            EmbedBuilder builder = new EmbedBuilder();
            builder.Title = "Nail Yetis";
            builder.Description = message.ToString();
            if (myDefaultChannel != null)
                await myDefaultChannel.SendMessageAsync("", embed: builder.Build());
            else
                Console.WriteLine("You don't have any write permissions at " + arg.Name);
        }
        private async Task SendJoinedMessage(SocketGuild arg)
        {
            StringBuilder message = new StringBuilder("Thank you for adding <@").Append(_client.CurrentUser.Id).AppendLine("> to your server!");
            message.AppendLine("To set in which channel you wish to receive the notifications, enter '.setchannel' at that channel");
            message.AppendLine("For a detailed information concerning the commands, enter '.help'");
            message.AppendLine("Enjoy! :D");
            await SendMessageAtDefaultChannel(message.ToString(), arg);
        }

        private SocketTextChannel GetDefaultChannel(SocketGuild arg)
        {
            var myChannels = arg.TextChannels.Where(c =>
            c.Users.Where(u => u.Id == _client.CurrentUser.Id).Count() > 0
            );
            return myChannels.Where(c =>
                 c.Users.Where(u => u.Id == _client.CurrentUser.Id)
                        .FirstOrDefault()
                        .GetPermissions(c).SendMessages == true
                 )
                 .OrderBy(c => c.Position)
                 .FirstOrDefault();
        }

        internal async Task ConnectAsync()
        {
            await InitCommands();

            await _client.LoginAsync(TokenType.Bot, ConfigurationManager.AppSettings["BotToken"]);
            await _client.StartAsync();
        }

        private Task BotReadyEvent()
        {
            var contexts = ContextService.GetContexts();
            foreach(var context in contexts)
            {
                IMessageChannel channel = _client.GetChannel(ulong.Parse(context.DefaultChannelId)) as IMessageChannel;
                if (channel == null)
                    throw new InvalidCastException("The default channel " + context.DefaultChannelId + " of " 
                        + _client.GetGuild(ulong.Parse(context.ServerId)).Name + " was not a text channel");
                //var toAdd = new ContextView(context.ServerId, channel);
                NotificationChannels.Add(context.ServerId, channel);
            }
            IsRunning = true;
            return Task.CompletedTask;
        }

        private async Task InitCommands()
        {
            await _commands.AddModuleAsync<BotCommandModule>();
            await _commands.AddModuleAsync<HelpCommandModule>();
            HelpCommandModule._commands = _commands;

            _client.MessageReceived += HandleCommandAsync;
        }

        private async Task HandleCommandAsync(SocketMessage arg)
        {
            var msg = arg as SocketUserMessage;
            if (msg == null) return;

            if (msg.Author.Id == _client.CurrentUser.Id || msg.Author.IsBot) return;

            int pos = 0;

            if (msg.HasCharPrefix('.', ref pos))
            {
                var context = new SocketCommandContext(_client, msg);

                var result = await _commands.ExecuteAsync(context, pos);

                if (!result.IsSuccess && result.Error != CommandError.UnknownCommand)
                    await msg.Channel.SendMessageAsync(result.ErrorReason);
            }
        }

        internal SocketGuild GetGuild(string serverId)
        {
            return _client.GetGuild(ulong.Parse(serverId));
        }
    }
}
