using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Text;

namespace IA_CEREBRO.Modules;

public class ServerManagementCommands : ModuleBase<SocketCommandContext>
{
    [Command("clear")]
    [Summary("Elimina un numero specificato di messaggi dal canale (Solo Moderatori)")]
    [RequireUserPermission(GuildPermission.ManageMessages)]
    public async Task ClearMessages(int count = 100)
    {
        if (count <= 0 || count > 100)
        {
            await ReplyAsync("❌ Il numero di messaggi da eliminare deve essere tra 1 e 100!");
            return;
        }

        var messages = await Context.Channel.GetMessagesAsync(count).FlattenAsync();
        await ((ITextChannel)Context.Channel).DeleteMessagesAsync(messages);
        
        var msg = await ReplyAsync($"✅ Eliminati {messages.Count()} messaggi!");
        await Task.Delay(3000);
        await msg.DeleteAsync();
    }

    [Command("slowmode")]
    [Summary("Imposta la modalità lenta nel canale (Solo Moderatori)")]
    [RequireUserPermission(GuildPermission.ManageChannels)]
    public async Task SetSlowMode(int seconds = 0)
    {
        if (seconds < 0 || seconds > 21600)
        {
            await ReplyAsync("❌ Il tempo deve essere tra 0 e 21600 secondi (6 ore)!");
            return;
        }

        await (Context.Channel as ITextChannel).ModifyAsync(x => x.SlowModeInterval = seconds);
        
        if (seconds == 0)
            await ReplyAsync("✅ Modalità lenta disattivata!");
        else
            await ReplyAsync($"✅ Modalità lenta impostata a {seconds} secondi!");
    }

    [Command("serverinfo")]
    [Summary("Mostra informazioni sul server (Solo Moderatori)")]
    [RequireUserPermission(GuildPermission.MuteMembers)]
    public async Task ServerInfo()
    {
        var guild = Context.Guild;
        var owner = guild.Owner;
        
        var embed = new EmbedBuilder()
            .WithTitle($"📊 Informazioni su {guild.Name}")
            .WithThumbnailUrl(guild.IconUrl)
            .WithColor(Color.Blue)
            .AddField("👑 Proprietario", owner.Username, true)
            .AddField("📅 Creato il", $"{guild.CreatedAt:dd/MM/yyyy}", true)
            .AddField("🌍 Regione", guild.PreferredLocale, true)
            .AddField("👥 Membri", guild.MemberCount, true)
            .AddField("💬 Canali", guild.Channels.Count, true)
            .AddField("😀 Emoji", guild.Emotes.Count, true)
            .WithFooter($"ID Server: {guild.Id}")
            .WithCurrentTimestamp();

        await ReplyAsync(embed: embed.Build());
    }

    [Command("userinfo")]
    [Summary("Mostra informazioni su un utente (Solo Moderatori)")]
    [RequireUserPermission(GuildPermission.MuteMembers)]
    public async Task UserInfo(SocketGuildUser user = null)
    {
        user ??= Context.User as SocketGuildUser;
        
        var roles = user.Roles.Where(r => !r.IsEveryone).OrderByDescending(r => r.Position);
        var joinPosition = Context.Guild.Users.OrderBy(u => u.JoinedAt).ToList().IndexOf(user) + 1;
        
        var embed = new EmbedBuilder()
            .WithTitle($"👤 Informazioni su {user.Username}")
            .WithThumbnailUrl(user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl())
            .WithColor(user.Roles.Max(r => r.Position) > 0 ? user.Roles.OrderByDescending(r => r.Position).First().Color : Color.Default)
            .AddField("🏷️ Tag", user.ToString(), true)
            .AddField("🆔 ID", user.Id, true)
            .AddField("📅 Account creato", $"{user.CreatedAt:dd/MM/yyyy}", true)
            .AddField("📆 Entrato nel server", $"{user.JoinedAt:dd/MM/yyyy}", true)
            .AddField("📊 Posizione entrata", $"#{joinPosition}", true)
            .AddField($"👑 Ruoli [{roles.Count()}]", string.Join(", ", roles.Select(r => r.Mention)) ?? "Nessun ruolo")
            .WithFooter($"Richiesto da {Context.User.Username}")
            .WithCurrentTimestamp();

        await ReplyAsync(embed: embed.Build());
    }

    [Command("announce")]
    [Summary("Invia un annuncio formattato (Solo Admin)")]
    [RequireUserPermission(GuildPermission.Administrator)]
    public async Task Announce([Remainder] string announcement)
    {
        var embed = new EmbedBuilder()
            .WithTitle("📢 Annuncio")
            .WithDescription(announcement)
            .WithColor(Color.Gold)
            .WithFooter($"da {Context.User.Username}")
            .WithCurrentTimestamp();

        await ReplyAsync(embed: embed.Build());
    }
}
