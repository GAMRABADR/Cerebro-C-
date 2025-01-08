using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Threading.Tasks;

namespace Cerebro.Modules
{
    public class AdminCommands : ModuleBase<SocketCommandContext>
    {
        [Command("shutdown")]
        [Summary("Spegne il bot (Solo Admin)")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task ShutdownCommand()
        {
            await ReplyAsync("⚡ Spegnimento in corso...");
            await Context.Client.StopAsync();
            Environment.Exit(0);
        }
    }
}
