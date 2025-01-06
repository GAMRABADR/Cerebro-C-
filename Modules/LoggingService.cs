using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace IA_CEREBRO.Modules;

[Summary("Sistema di logging per il server")]
public class LoggingService : ModuleBase<SocketCommandContext>
{
    private static ulong? _logChannelId;

    [Command("setlog")]
    [Summary("Imposta il canale per i log del server")]
    [Remarks("Esempio: !setlog #log-server")]
    [RequireUserPermission(GuildPermission.Administrator)]
    public async Task SetLogChannel(
        [Summary("Il canale dove verranno inviati i log")]
        ITextChannel channel)
    {
        _logChannelId = channel.Id;
        await ReplyAsync($"Canale log impostato su {channel.Mention}");
    }

    // Metodi di logging
    public static async Task LogModeration(SocketGuild guild, string action, IUser target, IUser moderator, string reason)
    {
        if (!_logChannelId.HasValue) return;

        var channel = guild.GetTextChannel(_logChannelId.Value);
        if (channel == null) return;

        var embed = new EmbedBuilder()
            .WithTitle($"🛡️ Azione di Moderazione: {action}")
            .WithDescription($"**Target:** {target.Mention}\n**Moderatore:** {moderator.Mention}\n**Motivo:** {reason}")
            .WithColor(Color.Red)
            .WithCurrentTimestamp()
            .Build();

        await channel.SendMessageAsync(embed: embed);
    }

    public static async Task LogUserJoined(SocketGuildUser user)
    {
        if (!_logChannelId.HasValue) return;

        var channel = user.Guild.GetTextChannel(_logChannelId.Value);
        if (channel == null) return;

        var embed = new EmbedBuilder()
            .WithTitle("👋 Nuovo Utente")
            .WithDescription($"{user.Mention} si è unito al server")
            .AddField("Account Creato", user.CreatedAt.ToString("dd/MM/yyyy HH:mm:ss"))
            .WithColor(Color.Green)
            .WithCurrentTimestamp()
            .Build();

        await channel.SendMessageAsync(embed: embed);
    }

    public static async Task LogUserLeft(SocketGuildUser user)
    {
        if (!_logChannelId.HasValue) return;

        var channel = user.Guild.GetTextChannel(_logChannelId.Value);
        if (channel == null) return;

        var embed = new EmbedBuilder()
            .WithTitle("👋 Utente Uscito")
            .WithDescription($"{user.Username}#{user.Discriminator} ha lasciato il server")
            .WithColor(Color.Orange)
            .WithCurrentTimestamp()
            .Build();

        await channel.SendMessageAsync(embed: embed);
    }

    public static async Task LogMessageDeleted(Cacheable<IMessage, ulong> message, Cacheable<IMessageChannel, ulong> channel)
    {
        if (!_logChannelId.HasValue) return;
        var msg = await message.GetOrDownloadAsync();
        if (msg == null || msg.Author.IsBot) return;

        var logChannel = (channel.Value as SocketGuildChannel)?.Guild.GetTextChannel(_logChannelId.Value);
        if (logChannel == null) return;

        var embed = new EmbedBuilder()
            .WithTitle("🗑️ Messaggio Eliminato")
            .WithDescription($"**Autore:** {msg.Author.Mention}\n**Canale:** {channel.Value}\n**Contenuto:** {msg.Content}")
            .WithColor(Color.Red)
            .WithCurrentTimestamp()
            .Build();

        await logChannel.SendMessageAsync(embed: embed);
    }

    public static async Task LogMessageEdited(Cacheable<IMessage, ulong> before, SocketMessage after, ISocketMessageChannel channel)
    {
        if (!_logChannelId.HasValue) return;
        var beforeMsg = await before.GetOrDownloadAsync();
        if (beforeMsg == null || beforeMsg.Author.IsBot || beforeMsg.Content == after.Content) return;

        var guild = (channel as SocketGuildChannel)?.Guild;
        if (guild == null) return;

        var logChannel = guild.GetTextChannel(_logChannelId.Value);
        if (logChannel == null) return;

        var embed = new EmbedBuilder()
            .WithTitle("✏️ Messaggio Modificato")
            .WithDescription($"**Autore:** {after.Author.Mention}\n**Canale:** {channel.Name}")
            .AddField("Prima", beforeMsg.Content)
            .AddField("Dopo", after.Content)
            .WithColor(Color.Blue)
            .WithCurrentTimestamp()
            .Build();

        await logChannel.SendMessageAsync(embed: embed);
    }
}
