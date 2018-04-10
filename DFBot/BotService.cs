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
            if (bot.Contexts.ContainsKey(id))
                bot.Contexts[id] = context;
            else
            {
                bot.Contexts.Add(id, context);
                bot.NotificationsSent.Add(id, new HashSet<string>());
            }
        }

        private static void NotifyServersEvent(object sender, ElapsedEventArgs e)
        {
            NotifyServers();
        }
        private static async void NotifyServers()
        {
            foreach (string serverId in bot.Contexts.Keys)
            {
                if (bot.Contexts.ContainsKey(serverId))
                    await NotifyServer(bot.Contexts[serverId], INTERVAL);
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
        /*internal static void SubscribeToOutpost(string outpost, SocketCommandContext context)
        {
            string guildId = context.Guild.Id + "";
            string userId = context.User.Id + "";

            if (!bot.Subscriptions.ContainsKey(guildId))
            {
                Dictionary<string, List<string>> subs = new Dictionary<string, List<string>>();
                bot.Subscriptions.Add(guildId, subs);
                bot.NotificationsSent.Add(guildId, BuildSubsSent());
            }
            if (!bot.Subscriptions[guildId].ContainsKey(userId))
            {
                bot.Subscriptions[guildId][userId] = new List<string>();
            }
            bot.Subscriptions[guildId][userId].Add(outpost);
        }*/
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

        internal static async Task<IRole> CreateRole(string name, SocketGuild server, ISocketMessageChannel channel = null)
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

        /*internal static List<string> GetAllOutpostsSubscribedTo(SocketCommandContext context)
        {
            string guildId = context.Guild.Id + "";
            string userId = context.User.Id + "";
            if (!bot.Subscriptions.ContainsKey(guildId))
            {
                Dictionary<string, List<string>> subs = new Dictionary<string, List<string>>();
                bot.Subscriptions.Add(guildId, subs);
                bot.NotificationsSent.Add(guildId, BuildSubsSent());
            }
            if (!bot.Subscriptions[guildId].ContainsKey(userId))
            {
                bot.Subscriptions[guildId][userId] = new List<string>();
            }
            return bot.Subscriptions[guildId][userId];
        }*/
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
        /*private static Dictionary<string, HashSet<string>> BuildSubsSent()
        {
            Dictionary<string, HashSet<string>> subsSent = new Dictionary<string, HashSet<string>>();
            subsSent.Add("nastyasHoldout", new HashSet<string>());
            subsSent.Add("doggsStockade", new HashSet<string>());
            subsSent.Add("secronomBunker", new HashSet<string>());
            subsSent.Add("fortPastor", new HashSet<string>());
            subsSent.Add("precinct13", new HashSet<string>());
            return subsSent;
        }*/
        /*internal static void UnsubscribeFromOutpost(string outpost, SocketCommandContext context)
        {
            string guildId = context.Guild.Id + "";
            string userId = context.User.Id + "";
            if (!bot.Subscriptions.ContainsKey(guildId))
            {
                Dictionary<string, List<string>> subs = new Dictionary<string, List<string>>();
                bot.Subscriptions.Add(guildId, subs);
                bot.NotificationsSent.Add(guildId, BuildSubsSent());
            }
            if (!bot.Subscriptions[guildId].ContainsKey(userId))
            {
                bot.Subscriptions[guildId][userId] = new List<string>();
            }
            bot.Subscriptions[guildId][userId].Remove(outpost);
        }*/
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

        internal static async Task NotifyServer(SocketCommandContext context, double interval = INTERVAL)
        {
            CheckBotExists();
            string serverId = "" + context.Guild.Id;
            var outpostsWithOA = OutpostService.GetOutpostsWithOA();
            var outpostsWithoutOA = OutpostService.GetAll().Except(outpostsWithOA);
            RemoveFromNotificationsSent(outpostsWithoutOA);
            await PrintOAs(outpostsWithOA, context);
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

        /*private static async Task PrintOAs(List<string> OAs, SocketCommandContext context)
        {
            if (OAs.Count == 0) return;
            string serverId = "" + context.Guild.Id;

            if (bot.Subscriptions.ContainsKey(serverId))
            {
                var subscriptions = bot.Subscriptions[serverId];
                foreach (var subsOfUser in subscriptions)
                {
                    string message = "<@" + subsOfUser.Key + ">";
                    EmbedBuilder builder = new EmbedBuilder();
                    builder.Title = "OA Report";
                    StringBuilder value = new StringBuilder();
                    foreach (var outpostSubscribedTo in subsOfUser.Value)
                    {
                        if (OAs.Contains(outpostSubscribedTo))
                        {
                            if (!HasSentNotification(subsOfUser.Key, serverId, outpostSubscribedTo))
                            {
                                value.Append("- ").AppendLine(outpostSubscribedTo);
                                bot.NotificationsSent[serverId][outpostSubscribedTo].Add(subsOfUser.Key);
                            }
                        }
                    }
                    if (value.Length > 0)
                    {
                        builder.Description = value.ToString();
                        await context.Channel.SendMessageAsync(message, embed: builder.Build());
                    }
                }
            }
            else
            {
                return; //Nobody is subscribed to anything
            }
        }*/
        private static async Task PrintOAs(List<string> OAs, SocketCommandContext context)
        {
            if (OAs.Count == 0) return;
            string serverId = "" + context.Guild.Id;
            foreach (string outpost in OAs)
            {
                if (HasSentNotification(outpost, serverId)) continue;
                IRole role = context.Guild.Roles.Where(r => r.Name == outpost).FirstOrDefault();
                if (role == null)
                {
                    role = await CreateRole(outpost, context.Guild, context.Channel);
                }
                StringBuilder message = new StringBuilder();
                message.Append(role.Mention).Append(" OA has just started!");
                await context.Channel.SendMessageAsync(message.ToString());
                bot.NotificationsSent[serverId].Add(outpost);
            }
        }
        /*private static bool HasSentNotification(string id, string serverId, string outpostSubscribedTo)
        {
            return bot.NotificationsSent[serverId][outpostSubscribedTo].Contains(id);
        }*/
        private static bool HasSentNotification(string outpost, string serverId)
        {
            return bot.NotificationsSent[serverId].Contains(outpost);
        }
    }
}
