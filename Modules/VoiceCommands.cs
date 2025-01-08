using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Cerebro.Modules
{
    public class VoiceCommands : ModuleBase<SocketCommandContext>
    {
        private readonly VoiceService _voiceService;
        private readonly ILogger<VoiceCommands> _logger;

        public VoiceCommands(VoiceService voiceService, ILogger<VoiceCommands> logger)
        {
            _voiceService = voiceService;
            _logger = logger;
        }

        [Command("entra")]
        [Summary("Fa entrare il bot nel tuo canale vocale")]
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
                await ReplyAsync("❌ Si è verificato un errore durante la connessione al canale vocale. Riprova tra qualche secondo.");
            }
        }

        [Command("esci")]
        [Summary("Fa uscire il bot dal canale vocale")]
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
                await ReplyAsync("❌ Si è verificato un errore durante l'uscita dal canale vocale.");
            }
        }

        [Command("stato")]
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
    }
}
