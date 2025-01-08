using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Cerebro.Modules;  // Aggiungere questa riga

namespace Cerebro.Modules

{
    public class AudioCommands : ModuleBase<SocketCommandContext>
    {
        private readonly VoiceService _voiceService;
        private readonly ILogger<AudioCommands> _logger;
        private static readonly ConcurrentDictionary<ulong, bool> AutoJoinEnabled = new();

        public AudioCommands(VoiceService voiceService, ILogger<AudioCommands> logger)
        {
            _voiceService = voiceService;
            _logger = logger;
        }

        [Command("join")]
        [Summary("Fa entrare il bot nel tuo canale vocale (Solo Moderatori)")]
        [RequireUserPermission(GuildPermission.MuteMembers)]
        public async Task JoinChannel()
        {
            try
            {
                var user = Context.User as SocketGuildUser;
                if (user?.VoiceChannel == null)
                {
                    await ReplyAsync("❌ Devi essere in un canale vocale!");
                    return;
                }

                await _voiceService.JoinChannelAsync(user.VoiceChannel);
                await ReplyAsync($"✅ Mi sono connesso al canale `{user.VoiceChannel.Name}`!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante la connessione al canale vocale");
                await ReplyAsync("❌ Si è verificato un errore durante la connessione. Riprova tra qualche secondo.");
            }
        }

        [Command("join")]
        [Summary("Fa entrare il bot in un canale vocale specifico (Solo Admin)")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task JoinChannel([Remainder] string? channelName)
        {
            try
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

                await _voiceService.JoinChannelAsync(targetChannel);
                await ReplyAsync($"✅ Mi sono connesso al canale `{targetChannel.Name}`!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante la connessione al canale vocale specifico");
                await ReplyAsync("❌ Si è verificato un errore durante la connessione. Riprova tra qualche secondo.");
            }
        }

        [Command("leave")]
        [Summary("Fa uscire il bot dal canale vocale")]
        [RequireUserPermission(GuildPermission.MuteMembers)]
        public async Task LeaveChannel()
        {
            try
            {
                await _voiceService.LeaveChannelAsync(Context.Guild.Id);
                await ReplyAsync("✅ Ho lasciato il canale vocale!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante l'uscita dal canale vocale");
                await ReplyAsync("❌ Si è verificato un errore durante l'uscita dal canale.");
            }
        }

        [Command("voicestatus")]
        [Summary("Mostra lo stato della connessione vocale")]
        public async Task VoiceStatus()
        {
            var user = Context.Guild.CurrentUser;
            var voiceChannel = user.VoiceChannel;

            var embed = new EmbedBuilder()
                .WithTitle("📊 Stato Connessione Vocale")
                .WithColor(voiceChannel != null ? Color.Green : Color.Red)
                .WithCurrentTimestamp();

            if (voiceChannel != null)
            {
                embed.AddField("Stato", "🟢 Connesso", true)
                     .AddField("Canale", voiceChannel.Name, true)
                     .AddField("Latenza", $"{Context.Client.Latency}ms", true);
            }
            else
            {
                embed.AddField("Stato", "🔴 Disconnesso", true)
                     .AddField("Latenza", $"{Context.Client.Latency}ms", true);
            }

            await ReplyAsync(embed: embed.Build());
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
                    if (!guild.CurrentUser.VoiceChannel?.Id.Equals(channel.Id) ?? true)
                    {
                        await guild.CurrentUser.VoiceChannel.DisconnectAsync();
                        await channel.ConnectAsync();
                    }
                }
                catch
                {
                    // Ignora eventuali errori nell'auto-join
                }
            }
        }
    }
}
