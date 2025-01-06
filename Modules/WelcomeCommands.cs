using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace IA_CEREBRO.Modules;

public class WelcomeCommands : ModuleBase<SocketCommandContext>
{
    private static Dictionary<ulong, ulong> _welcomeChannels = new();

    [Command("setwelcome")]
    [RequireUserPermission(GuildPermission.Administrator)]
    public async Task SetWelcomeChannel(ITextChannel channel)
    {
        _welcomeChannels[Context.Guild.Id] = channel.Id;
        await ReplyAsync($"Il canale di benvenuto è stato impostato su {channel.Mention}");
    }

    public static async Task HandleUserJoined(SocketGuildUser user)
    {
        if (_welcomeChannels.TryGetValue(user.Guild.Id, out ulong channelId))
        {
            if (user.Guild.GetChannel(channelId) is ITextChannel welcomeChannel)
            {
                var embed = new EmbedBuilder()
                    .WithTitle("👋 Nuovo Membro!")
                    .WithDescription($"Benvenuto {user.Mention} in {user.Guild.Name}!")
                    .WithColor(Color.Green)
                    .WithThumbnailUrl(user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl())
                    .AddField("Account Creato", user.CreatedAt.ToString("dd/MM/yyyy"), true)
                    .AddField("Membri Totali", user.Guild.MemberCount, true)
                    .WithCurrentTimestamp()
                    .Build();

                await welcomeChannel.SendMessageAsync(embed: embed);
            }
        }
    }
}
