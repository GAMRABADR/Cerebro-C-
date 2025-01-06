using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Collections.Concurrent;

namespace IA_CEREBRO.Modules;

public class AudioCommands : ModuleBase<SocketCommandContext>
{
    private static readonly ConcurrentDictionary<ulong, bool> AutoJoinEnabled = new();
    private static readonly ConcurrentDictionary<ulong, IVoiceChannel> ConnectedChannels = new();

    [Command("join")]
    [Summary("Fa entrare il bot nel tuo canale vocale")]
    public async Task JoinChannel()
    {
        var user = Context.User as SocketGuildUser;
        if (user?.VoiceChannel == null)
        {
            await ReplyAsync("Devi essere in un canale vocale per usare questo comando!");
            return;
        }

        try
        {
            await user.VoiceChannel.ConnectAsync();
            ConnectedChannels.TryAdd(Context.Guild.Id, user.VoiceChannel);
            await ReplyAsync($"✅ Mi sono unito al canale {user.VoiceChannel.Name}!");
        }
        catch (Exception ex)
        {
            await ReplyAsync($"❌ Non sono riuscito a unirmi al canale: {ex.Message}");
        }
    }

    [Command("leave")]
    [Summary("Fa uscire il bot dal canale vocale")]
    public async Task LeaveChannel()
    {
        if (ConnectedChannels.TryRemove(Context.Guild.Id, out _))
        {
            await Context.Guild.CurrentUser.VoiceChannel?.DisconnectAsync();
            await ReplyAsync("👋 Ho lasciato il canale vocale!");
        }
        else
        {
            await ReplyAsync("❌ Non sono in nessun canale vocale!");
        }
    }

    [Command("autojoin")]
    [Summary("Attiva/disattiva l'auto-join nei canali vocali")]
    public async Task ToggleAutoJoin()
    {
        var guildId = Context.Guild.Id;
        bool currentValue = AutoJoinEnabled.GetOrAdd(guildId, false);
        
        if (AutoJoinEnabled.TryUpdate(guildId, !currentValue, currentValue))
        {
            string status = !currentValue ? "attivato" : "disattivato";
            await ReplyAsync($"🔄 Auto-join {status}!");
        }
    }

    // Metodo per gestire l'auto-join
    public static async Task HandleVoiceStateUpdated(SocketUser user, SocketVoiceState oldState, SocketVoiceState newState)
    {
        if (user.IsBot) return;

        var channel = newState.VoiceChannel;
        if (channel == null) return;

        var guild = (user as SocketGuildUser)?.Guild;
        if (guild == null) return;

        // Verifica se l'auto-join è attivo per questo server
        if (!AutoJoinEnabled.GetOrAdd(guild.Id, false)) return;

        // Conta gli utenti nel canale (escludendo i bot)
        var userCount = channel.Users.Count(x => !x.IsBot);

        if (userCount >= 2)
        {
            try
            {
                // Se non siamo già connessi a questo canale
                if (!ConnectedChannels.ContainsKey(guild.Id))
                {
                    await channel.ConnectAsync();
                    ConnectedChannels.TryAdd(guild.Id, channel);
                }
            }
            catch (Exception)
            {
                // Gestione silenziosa degli errori per l'auto-join
            }
        }
    }
}
