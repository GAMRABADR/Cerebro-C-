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
    [Summary("Fa entrare il bot nel tuo canale vocale (Solo Moderatori)")]
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
    [Summary("Fa entrare il bot in un canale vocale specifico (Solo Admin)")]
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
        const int maxRetries = 3;
        const int retryDelayMs = 2000;
        Exception lastException = null;
        bool showRetryMessages = false;  // Per default, non mostriamo i messaggi di retry

        try
        {
            if (Context.Guild.CurrentUser.VoiceChannel == targetChannel)
            {
                await ReplyAsync("❌ Sono già in questo canale vocale!");
                return;
            }

            var botPerms = Context.Guild.CurrentUser.GetPermissions(targetChannel);
            if (!botPerms.Connect || !botPerms.Speak)
            {
                await ReplyAsync("❌ Non ho i permessi necessari per unirmi al canale vocale! Ho bisogno dei permessi di Connessione e Parla.");
                return;
            }

            // Se il bot è in un altro canale, prima disconnettilo
            if (Context.Guild.CurrentUser.VoiceChannel != null)
            {
                await Context.Guild.CurrentUser.VoiceChannel.DisconnectAsync();
                ConnectedChannels.TryRemove(Context.Guild.Id, out _);
                await Task.Delay(500); // Ridotto il delay per la disconnessione
            }

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10)); // Ridotto il timeout
                    await targetChannel.ConnectAsync(selfDeaf: false, selfMute: false, external: false).WaitAsync(cts.Token);
                    ConnectedChannels.TryAdd(Context.Guild.Id, targetChannel);
                    await ReplyAsync($"✅ Mi sono unito al canale vocale {targetChannel.Name}!");
                    return;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    
                    // Se è l'ultimo tentativo, non mostrare il messaggio di retry
                    if (attempt < maxRetries)
                    {
                        await Task.Delay(retryDelayMs);
                    }
                    
                    // Log dell'errore in console per debugging
                    Console.WriteLine($"Tentativo {attempt} fallito per il canale {targetChannel.Name}: {ex.Message}");
                }
            }

            // Se arriviamo qui, tutti i tentativi sono falliti
            string errorMessage = "Non riesco a connettermi al canale vocale. ";
            if (lastException is OperationCanceledException)
            {
                errorMessage += "La connessione è troppo lenta o instabile.";
            }
            else if (lastException is Discord.Net.HttpException httpEx)
            {
                errorMessage += httpEx.Reason ?? "Si è verificato un errore di comunicazione con Discord.";
            }
            else
            {
                errorMessage += "Si è verificato un problema di connessione.";
            }

            await ReplyAsync($"❌ {errorMessage} Riprova più tardi.");
            Console.WriteLine($"Errore finale dopo {maxRetries} tentativi per il canale {targetChannel.Name}: {lastException}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Errore critico durante il join al canale {targetChannel.Name}: {ex}");
            await ReplyAsync("❌ Si è verificato un errore inaspettato durante la connessione al canale.");
        }
    }

    [Command("leave")]
    [Summary("Fa uscire il bot dal canale vocale (Solo Moderatori)")]
    [RequireUserPermission(GuildPermission.MuteMembers)]
    public async Task LeaveChannel([Remainder] string channelName = null)
    {
        try
        {
            var voiceChannel = Context.Guild.CurrentUser.VoiceChannel;
            if (voiceChannel == null)
            {
                await ReplyAsync("❌ Non sono in nessun canale vocale!");
                return;
            }

            // Se viene specificato un canale, verifica che sia quello giusto
            if (!string.IsNullOrEmpty(channelName) && 
                !voiceChannel.Name.Equals(channelName, StringComparison.OrdinalIgnoreCase))
            {
                await ReplyAsync($"❌ Non sono nel canale {channelName}!");
                return;
            }

            await voiceChannel.DisconnectAsync();
            ConnectedChannels.TryRemove(Context.Guild.Id, out _);
            await ReplyAsync($"👋 Ho lasciato il canale {voiceChannel.Name}!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Errore durante l'uscita dal canale: {ex}");
            await ReplyAsync("❌ Si è verificato un errore durante l'uscita dal canale.");
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
