using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Collections.Concurrent;
using Victoria;
using Victoria.Node;

namespace IA_CEREBRO.Modules;

[Group("audio")]
[Summary("Comandi per la gestione audio")]
public class AudioCommands : ModuleBase<SocketCommandContext>
{
    private readonly LavaNode _lavaNode;
    private static readonly ConcurrentDictionary<ulong, bool> AutoJoinEnabled = new();
    private static readonly ConcurrentDictionary<ulong, IVoiceChannel> ConnectedChannels = new();

    public AudioCommands(LavaNode lavaNode)
    {
        _lavaNode = lavaNode;
    }

    [Command("join")]
    [Summary("Fa entrare il bot nel tuo canale vocale")]
    [RequireUserPermission(GuildPermission.MuteMembers)]
    public async Task JoinChannel()
    {
        var user = Context.User as SocketGuildUser;
        if (user?.VoiceChannel == null)
        {
            await ReplyAsync("❌ Devi essere in un canale vocale!");
            return;
        }

        await JoinSpecificChannel(user.VoiceChannel);
    }

    [Command("join")]
    [Summary("Fa entrare il bot in un canale vocale specifico")]
    [RequireUserPermission(GuildPermission.Administrator)]
    public async Task JoinChannel([Remainder] string channelName)
    {
        if (string.IsNullOrWhiteSpace(channelName))
        {
            await ReplyAsync("❌ Specifica il nome del canale vocale!");
            return;
        }

        var voiceChannels = Context.Guild.VoiceChannels;
        var targetChannel = voiceChannels
            .FirstOrDefault(x => x.Name.ToLower().Contains(channelName.ToLower()));

        if (targetChannel == null)
        {
            var availableChannels = string.Join("\n", voiceChannels.Select(x => $"- {x.Name}"));
            await ReplyAsync($"❌ Non ho trovato nessun canale vocale chiamato '{channelName}'\n\nCanali vocali disponibili:\n{availableChannels}");
            return;
        }

        await JoinSpecificChannel(targetChannel);
    }

    private async Task JoinSpecificChannel(IVoiceChannel targetChannel)
    {
        try
        {
            // Verifica se il bot è già nel canale
            if (Context.Guild.CurrentUser.VoiceChannel == targetChannel)
            {
                await ReplyAsync("❌ Sono già in questo canale vocale!");
                return;
            }

            // Se il bot è in un altro canale, prima disconnettilo
            if (Context.Guild.CurrentUser.VoiceChannel != null)
            {
                await Context.Guild.CurrentUser.VoiceChannel.DisconnectAsync();
                ConnectedChannels.TryRemove(Context.Guild.Id, out _);
            }

            await _lavaNode.JoinAsync(targetChannel, Context.Channel as ITextChannel);
            ConnectedChannels.TryAdd(Context.Guild.Id, targetChannel);
            await ReplyAsync($"✅ Mi sono unito al canale vocale {targetChannel.Name}!");
        }
        catch (Exception ex)
        {
            await ReplyAsync($"❌ Non sono riuscito a unirmi al canale: {ex.Message}");
        }
    }

    [Command("leave")]
    [Summary("Fa uscire il bot dal canale vocale")]
    [RequireUserPermission(GuildPermission.MuteMembers)]
    public async Task LeaveChannel()
    {
        try
        {
            var voiceChannel = Context.Guild.CurrentUser.VoiceChannel;
            if (voiceChannel == null)
            {
                await ReplyAsync("❌ Non sono in nessun canale vocale!");
                return;
            }

            var player = _lavaNode.GetPlayer(Context.Guild);
            
            if (player.PlayerState == Victoria.Enums.PlayerState.Playing)
            {
                await player.StopAsync();
            }

            await _lavaNode.LeaveAsync(voiceChannel);
            ConnectedChannels.TryRemove(Context.Guild.Id, out _);
            await ReplyAsync("👋 Ho lasciato il canale vocale!");
        }
        catch (Exception ex)
        {
            await ReplyAsync($"❌ Si è verificato un errore: {ex.Message}");
        }
    }

    [Command("autojoin")]
    [Summary("Attiva/disattiva l'auto-join nei canali vocali (Solo Admin)")]
    [RequireUserPermission(GuildPermission.Administrator)]
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
            catch
            {
                // Ignora eventuali errori nell'auto-join
            }
        }
    }
}
