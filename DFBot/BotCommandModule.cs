using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using Discord;
using Services;
using System;

namespace DFBot
{
    public class BotCommandModule : ModuleBase<SocketCommandContext>
    {
        [Command("setchannel")]
        [Summary("Sets this channel as the one to print notifications.")]
        public async Task SetChannel()
        {
            BotService.AddContext(Context);
            await ReplyAsync("The channel <#" + Context.Channel.Id + "> was set for notifications");
        }
        [Command("sub")]
        [Summary("Starts pinging you when an OA starts at that outpost. Use: '.sub all' or '.sub <outpost>'")]
        public async Task Subscribe(string arg = null)
        {
            if (arg == null)
            {
                await ReplyAsync("The correct use of this command is '.sub all' or '.sub <outpost>'");
            }
            else if (arg == "all")
            {
                await ReplyAsync("Subscribing to all outposts...");
                BotService.SubscribeToAllOutposts(Context);
            }
            else {
                string outpost = OutpostService.FindName(arg);
                if (outpost != null)
                {
                    await ReplyAsync($"Subscribing to {outpost}");
                    BotService.SubscribeToOutpost(outpost, Context);
                }
                else
                {
                    await ReplyAsync("You must choose one of the following outposts");
                    await PrintOutposts();
                }
            }
        }
        [Command("unsub")]
        [Summary("Removes the selected subscription. Use: '.unsub all' or '.unsub <outpost>'")]
        public async Task Unsubscribe(string arg = null)
        {
            if (arg == null)
            {
                await ReplyAsync("The correct use of this command is '.unsub all' or '.unsub <outpost>'");
            }
            else if (arg == "all")
            {
                await ReplyAsync("Unsubscribing from all outposts...");
                BotService.UnsubscribeFromAllOutposts(Context);
            }
            else {
                string outpost = OutpostService.FindName(arg);
                if (outpost != null)
                {
                    await ReplyAsync($"Unsubscribing from {outpost}");
                    BotService.UnsubscribeFromOutpost(outpost, Context);
                }
                else
                {
                    await ReplyAsync("You must choose one of the following outposts");
                    await PrintOutposts();
                }
            }
        }

        [Command("subs")]
        [Summary("Prints all the outposts you are subscribed to. Use: '.subs'")]
        public async Task PrintSubscriptions()
        {
            List<string> outposts = BotService.GetAllOutpostsSubscribedTo(Context);
            List<Tuple<string, string>> fields = new List<Tuple<string, string>>();
            StringBuilder message = new StringBuilder();
            message.Append("You are subscribed to ").Append(outposts.Count).AppendLine(" outposts");
            foreach (string outpost in outposts)
                message.Append("- ").AppendLine(outpost);
            Tuple<string, string> tuple = new Tuple<string, string>("Outposts", message.ToString());
            fields.Add(tuple);
            await ReplyAsync(fields);
            await ReplyAsync(message.ToString());
        }


        //Categories should be a thing later on
        [Command("outposts")]
        [Summary("Prints all the outposts. Use: '.outposts''")]
        public async Task PrintOutposts()
        {
            var outpostNames = OutpostService.GetAll();
            StringBuilder message = new StringBuilder();
            foreach (string outpost in outpostNames)
                message.Append("- ").AppendLine(outpost);
            await ReplyAsync(message.ToString());
        }

        protected override Task<IUserMessage> ReplyAsync(string message, bool isTTS = false, Embed embed = null, RequestOptions options = null)
        {
            if (embed != null)
                return base.ReplyAsync(message, isTTS, embed, options);
            EmbedBuilder builder = new EmbedBuilder();
            builder.Title = "Nail Yetis";
            builder.Description = message;
            return base.ReplyAsync("", isTTS, builder.Build(), options);
        }

        private Task<IUserMessage> ReplyAsync(List<Tuple<string, string>> fields)
        {
            EmbedBuilder builder = new EmbedBuilder();
            builder.Title = "Nail Yetis";
            foreach (Tuple<string, string> field in fields)
            {
                builder.AddField(f => { f.Name = field.Item1; f.Value = field.Item2; });
            }
            return ReplyAsync("", embed: builder.Build());
        }
    }
}
