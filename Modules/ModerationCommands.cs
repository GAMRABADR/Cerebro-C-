using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace IA_CEREBRO.Modules;

[Summary("Comandi per la moderazione del server")]
public class ModerationCommands : ModuleBase<SocketCommandContext>
{
    [Command("clear")]
    [Summary("Elimina un numero specifico di messaggi dal canale")]
    [Remarks("Esempio: !clear 10")]
    [RequireUserPermission(GuildPermission.ManageMessages)]
    public async Task ClearMessages(
        [Summary("Numero di messaggi da eliminare (max 100)")]
        int amount = 100)
    {
        var messages = await Context.Channel.GetMessagesAsync(amount + 1).FlattenAsync();
        await ((ITextChannel)Context.Channel).DeleteMessagesAsync(messages);
        var message = await ReplyAsync($"Eliminati {amount} messaggi!");
        await Task.Delay(3000);
        await message.DeleteAsync();
    }

    [Command("kick")]
    [Summary("Espelle un utente dal server")]
    [Remarks("Esempio: !kick @utente spam")]
    [RequireUserPermission(GuildPermission.KickMembers)]
    public async Task KickUser(
        [Summary("L'utente da espellere")]
        SocketGuildUser user,
        [Summary("Motivo dell'espulsione")]
        [Remainder] string reason = null)
    {
        if (user.Hierarchy >= (Context.User as SocketGuildUser).Hierarchy)
        {
            await ReplyAsync("Non puoi kickare questo utente!");
            return;
        }

        await user.KickAsync(reason);
        await ReplyAsync($"{user.Username} è stato kickato dal server. Motivo: {reason ?? "Nessun motivo specificato"}");
    }

    [Command("ban")]
    [Summary("Bandisce un utente dal server")]
    [Remarks("Esempio: !ban @utente comportamento inappropriato")]
    [RequireUserPermission(GuildPermission.BanMembers)]
    public async Task BanUser(
        [Summary("L'utente da bannare")]
        SocketGuildUser user,
        [Summary("Motivo del ban")]
        [Remainder] string reason = null)
    {
        if (user.Hierarchy >= (Context.User as SocketGuildUser).Hierarchy)
        {
            await ReplyAsync("Non puoi bannare questo utente!");
            return;
        }

        await user.Guild.AddBanAsync(user, 0, reason);
        await ReplyAsync($"{user.Username} è stato bannato dal server. Motivo: {reason ?? "Nessun motivo specificato"}");
    }

    [Command("mute")]
    [Summary("Silenzia temporaneamente un utente")]
    [Remarks("Esempio: !mute @utente 10 spam in chat")]
    [RequireUserPermission(GuildPermission.MuteMembers)]
    public async Task MuteUser(
        [Summary("L'utente da mutare")]
        SocketGuildUser user,
        [Summary("Durata del mute in minuti")]
        int minutes,
        [Summary("Motivo del mute")]
        [Remainder] string reason = null)
    {
        if (user.Hierarchy >= (Context.User as SocketGuildUser).Hierarchy)
        {
            await ReplyAsync("Non puoi mutare questo utente!");
            return;
        }

        await user.SetTimeOutAsync(TimeSpan.FromMinutes(minutes));
        await ReplyAsync($"{user.Username} è stato mutato per {minutes} minuti. Motivo: {reason ?? "Nessun motivo specificato"}");
    }

    [Command("warn")]
    [Summary("Invia un avvertimento a un utente")]
    [Remarks("Esempio: !warn @utente linguaggio inappropriato")]
    [RequireUserPermission(GuildPermission.KickMembers)]
    public async Task WarnUser(
        [Summary("L'utente da avvertire")]
        SocketGuildUser user,
        [Summary("Motivo dell'avvertimento")]
        [Remainder] string reason)
    {
        var embed = new EmbedBuilder()
            .WithTitle("⚠️ Avvertimento")
            .WithDescription($"{user.Mention} è stato avvertito")
            .AddField("Motivo", reason)
            .AddField("Moderatore", Context.User.Username)
            .WithColor(Color.Orange)
            .WithCurrentTimestamp()
            .Build();

        await ReplyAsync(embed: embed);
        
        try
        {
            var dmChannel = await user.CreateDMChannelAsync();
            await dmChannel.SendMessageAsync($"Sei stato avvertito in {Context.Guild.Name} per: {reason}");
        }
        catch
        {
            await ReplyAsync("Non è stato possibile inviare un DM all'utente.");
        }
    }
}
