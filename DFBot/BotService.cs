using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Services;
using Discord.WebSocket;
using Discord.Net;

namespace DFBot
{
    class BotService
    {
        private static Bot bot;
        private const double INTERVAL = 120000; //Two minutes
        //private const double INTERVAL = 600000; //ten minutes
        internal static async Task RunAsync()
        {
            if (bot != null)
                throw new InvalidOperationException("The bot is already running");

            await BackendRunner.RunBackend(INTERVAL);

            bot = new Bot();
            await bot.ConnectAsync();

            System.Timers.Timer checkForTime = new System.Timers.Timer(INTERVAL);
            checkForTime.Elapsed += new ElapsedEventHandler(NotifyServersEvent);
            checkForTime.Start();

            await Task.Delay(Timeout.Infinite);
        }

        internal static void AddContext(SocketCommandContext context)
        {
            string id = context.Guild.Id + "";
            //ContextView contextView = new ContextView(id);
            //var channel = bot.NotificationChannels[contextView];
            if (bot.NotificationChannels.ContainsKey(id))
            {
                bot.NotificationChannels[id] = context.Channel;
                ContextService.UpdateContext(id, context.Channel.Id);
            }
            else
            {
                bot.NotificationChannels.Add(id, context.Channel);
                ContextService.CreateChannel(id, context.Channel.Id);
                bot.NotificationsSent.Add(id, new HashSet<string>());
            }
        }

        private static void NotifyServersEvent(object sender, ElapsedEventArgs e)
        {
            NotifyServers();
        }
        private static async void NotifyServers()
        {
            foreach (string serverId in bot.NotificationChannels.Keys)
            {
                if (bot.NotificationChannels.ContainsKey(serverId))
                    await NotifyServer(serverId, INTERVAL);
            }
        }

        internal static void SubscribeToAllOutposts(SocketCommandContext context)
        {
            var outposts = OutpostService.GetAll();
            foreach (string outpost in outposts)
            {
                SubscribeToOutpost(outpost, context);
            }
        }

        internal static async void SubscribeToOutpost(string outpost, SocketCommandContext context)
        {
            IRole role = context.Guild.Roles.Where(r => r.Name == outpost).FirstOrDefault();
            if (role == null)
            {
                role = await CreateRole(outpost, context.Guild, context.Channel);
                if (role == null) return;
            }
            var user = context.Guild.Users.Where(u => u.Id == context.User.Id).FirstOrDefault();
            await AssignRole(role, user, context.Guild, context.Channel);
        }

        internal static async Task<bool> AssignRole(IRole role, SocketGuildUser user, SocketGuild server, ISocketMessageChannel channel = null)
        {
            try
            {
                if (!RoleExists(role.Name, server))
                {
                    if (await CreateRole(role.Name, server, channel) == null)
                        throw new HttpException(System.Net.HttpStatusCode.Forbidden);
                }
                await user.AddRoleAsync(role);
                return true;
            }
            catch (HttpException)
            {
                if (channel == null)
                {
                    channel = GetDefaultChannel(server);
                }
                await channel.SendMessageAsync("I don't have permissions to assign the role " + role.Name);
                return false;
            }
        }

        internal static async Task<IRole> CreateRole(string name, SocketGuild server, IMessageChannel channel = null)
        {
            try
            {
                IRole role = null;
                if (!RoleExists(name, server))
                {
                    role = await server.CreateRoleAsync(name);
                }
                return role;
            }
            catch (HttpException)
            {
                if (channel == null)
                {
                    channel = GetDefaultChannel(server);
                }
                await channel.SendMessageAsync("I don't have permissions to create the role " + name);
                return null;
            }
        }

        private static SocketTextChannel GetDefaultChannel(SocketGuild arg)
        {
            var myChannels = arg.TextChannels.Where(c =>
            c.Users.Where(u => u.Id == bot.MyId).Count() > 0
            );
            return myChannels.Where(c =>
                 c.Users.Where(u => u.Id == bot.MyId)
                        .FirstOrDefault()
                        .GetPermissions(c).SendMessages == true
                 )
                 .OrderBy(c => c.Position)
                 .FirstOrDefault();
        }

        private static bool RoleExists(string name, SocketGuild arg)
        {
            return arg.Roles.Any(r => r.Name == name);
        }

        internal static void UnsubscribeFromAllOutposts(SocketCommandContext context)
        {
            var outposts = OutpostService.GetAll();
            foreach (string outpost in outposts)
            {
                UnsubscribeFromOutpost(outpost, context);
            }
        }

        internal static List<string> GetAllOutpostsSubscribedTo(SocketCommandContext context)
        {
            List<string> ret = new List<string>();
            List<string> outposts = OutpostService.GetAll();
            var roles = context.Guild.Users.Where(u => u.Id == context.User.Id).FirstOrDefault().Roles;
            foreach (var role in roles)
            {
                if (outposts.Contains(role.Name))
                {
                    ret.Add(role.Name);
                }
            }
            return ret;
        }

        internal static async void UnsubscribeFromOutpost(string outpost, SocketCommandContext context)
        {
            IRole role = context.Guild.Roles.Where(r => r.Name == outpost).FirstOrDefault();
            if (role != null)
            {
                var user = context.Guild.Users.Where(u => u.Id == context.User.Id).FirstOrDefault();
                await RemoveRole(role, user, context.Guild, context.Channel);
            }
        }

        private static async Task RemoveRole(IRole role, SocketGuildUser user, SocketGuild server, ISocketMessageChannel channel)
        {
            try
            {
                await user.RemoveRoleAsync(role);
            }
            catch (HttpException)
            {
                if (channel == null)
                {
                    channel = GetDefaultChannel(server);
                }
                await channel.SendMessageAsync("I don't have permissions to remove the role " + role.Name);
            }
        }

        internal static async Task NotifyServer(string serverId, double interval = INTERVAL)
        {
            CheckBotExists();
            var outpostsWithOA = OutpostService.GetOutpostsWithOA();
            var outpostsWithoutOA = OutpostService.GetAll().Except(outpostsWithOA);
            RemoveFromNotificationsSent(outpostsWithoutOA);
            await PrintOAs(outpostsWithOA, serverId);
        }

        private static void RemoveFromNotificationsSent(IEnumerable<string> outpostsWithoutOA)
        {
            foreach (var serverId in bot.NotificationsSent.Keys)
            {
                foreach (var outpost in outpostsWithoutOA)
                {
                    bot.NotificationsSent[serverId].Remove(outpost);
                }
            }
        }

        private static void CheckBotExists()
        {
            if (bot == null || !bot.IsRunning)
                throw new InvalidOperationException("The bot is not running yet");
        }


        private static async Task PrintOAs(List<string> OAs, string serverId)
        {
            if (OAs.Count == 0) return;
            foreach (string outpost in OAs)
            {
                if (HasSentNotification(outpost, serverId)) continue;
                var guild = bot.GetGuild(serverId);
                var channel = bot.NotificationChannels[serverId];
                IRole role = guild.Roles.Where(r => r.Name == outpost).FirstOrDefault();
                if (role == null)
                {
                    role = await CreateRole(outpost, guild, channel);
                }
                StringBuilder message = new StringBuilder();
                message.Append(role.Mention).Append(" OA has just started!");
                await channel.SendMessageAsync(message.ToString());
                bot.NotificationsSent[serverId].Add(outpost);
            }
        }

        private static bool HasSentNotification(string outpost, string serverId)
        {
            return bot.NotificationsSent[serverId].Contains(outpost);
        }
    }
}
