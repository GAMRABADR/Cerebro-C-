using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Collections.Concurrent;

namespace IA_CEREBRO.Modules;

[Summary("Gestione dei ruoli tramite reazioni")]
public class RoleManager : ModuleBase<SocketCommandContext>
{
    private static readonly ConcurrentDictionary<ulong, Dictionary<IEmote, ulong>> _roleMessages = new();

    [Command("createroles")]
    [Summary("Crea un messaggio per l'assegnazione automatica dei ruoli")]
    [Remarks("Esempio: !createroles Scegli i tuoi ruoli")]
    [RequireUserPermission(GuildPermission.Administrator)]
    public async Task CreateRoleMessage(
        [Summary("Titolo del messaggio dei ruoli")]
        [Remainder] string title = "Seleziona i tuoi ruoli")
    {
        var embed = new EmbedBuilder()
            .WithTitle(title)
            .WithDescription("Reagisci per ottenere un ruolo!")
            .WithColor(Color.Blue)
            .Build();

        var message = await ReplyAsync(embed: embed);
        _roleMessages[message.Id] = new Dictionary<IEmote, ulong>();
    }

    [Command("addrole")]
    [Summary("Aggiunge un ruolo al messaggio di assegnazione ruoli")]
    [Remarks("Esempio: !addrole 123456789 @Giocatore 🎮")]
    [RequireUserPermission(GuildPermission.Administrator)]
    public async Task AddRoleToMessage(
        [Summary("ID del messaggio dei ruoli")]
        ulong messageId,
        [Summary("Il ruolo da aggiungere")]
        IRole role,
        [Summary("L'emoji da usare per questo ruolo")]
        string emote)
    {
        if (!_roleMessages.ContainsKey(messageId))
        {
            await ReplyAsync("Messaggio non trovato!");
            return;
        }

        var emoteObj = Emote.TryParse(emote, out var parsedEmote) ? 
            (IEmote)parsedEmote : new Emoji(emote);

        var message = await Context.Channel.GetMessageAsync(messageId) as IUserMessage;
        if (message == null)
        {
            await ReplyAsync("Messaggio non trovato nel canale!");
            return;
        }

        _roleMessages[messageId][emoteObj] = role.Id;
        await message.AddReactionAsync(emoteObj);
        await ReplyAsync($"Ruolo {role.Name} aggiunto con l'emote {emote}");
    }

    // Eventi di gestione reazioni
    public static async Task HandleReactionAdded(
        Cacheable<IUserMessage, ulong> cachedMessage,
        Cacheable<IMessageChannel, ulong> channel,
        SocketReaction reaction)
    {
        if (!_roleMessages.ContainsKey(cachedMessage.Id)) return;

        var guild = (channel.Value as SocketGuildChannel)?.Guild;
        var user = reaction.User.Value as SocketGuildUser;
        if (guild == null || user == null || user.IsBot) return;

        if (_roleMessages[cachedMessage.Id].TryGetValue(reaction.Emote, out ulong roleId))
        {
            var role = guild.GetRole(roleId);
            if (role != null)
            {
                await user.AddRoleAsync(role);
            }
        }
    }

    public static async Task HandleReactionRemoved(
        Cacheable<IUserMessage, ulong> cachedMessage,
        Cacheable<IMessageChannel, ulong> channel,
        SocketReaction reaction)
    {
        if (!_roleMessages.ContainsKey(cachedMessage.Id)) return;

        var guild = (channel.Value as SocketGuildChannel)?.Guild;
        var user = reaction.User.Value as SocketGuildUser;
        if (guild == null || user == null || user.IsBot) return;

        if (_roleMessages[cachedMessage.Id].TryGetValue(reaction.Emote, out ulong roleId))
        {
            var role = guild.GetRole(roleId);
            if (role != null)
            {
                await user.RemoveRoleAsync(role);
            }
        }
    }
}
