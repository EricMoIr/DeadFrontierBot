using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DFBot
{
    //Thank you Foxbot https://gist.github.com/foxbot/7a880c6267cda18dc1ab6587ab7ffbb2
    class HelpCommandModule : ModuleBase<SocketCommandContext>
    {
        internal static CommandService _commands;

        [Command("help")]
        [Summary("Lists this bot's commands.")]
        public async Task Help()
        {
            EmbedBuilder output = new EmbedBuilder();
            output.Title = "DFBot - Help";
            AddHelp(_commands.Modules, ref output);

            await ReplyAsync("", embed: output.Build());
        }

        private void AddHelp(IEnumerable<ModuleInfo> allMods, ref EmbedBuilder builder)
        {
            Stack<ModuleInfo> modules = new Stack<ModuleInfo>();
            foreach (ModuleInfo mod in allMods)
                modules.Push(mod);
            while (modules.Count > 0)
            {
                ModuleInfo module = modules.Pop();
                foreach (var sub in module.Submodules)
                    modules.Push(sub);
                foreach (CommandInfo command in module.Commands)
                    builder.AddField(f =>
                    {
                        f.Name = "." + command.Name;
                        f.Value = command.Summary;
                    });
            }
        }
    }
}
