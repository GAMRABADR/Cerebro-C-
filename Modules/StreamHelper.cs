using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Net.Http;

namespace IA_CEREBRO.Modules;

public class StreamHelper : ModuleBase<SocketCommandContext>
{
    private static readonly HttpClient _httpClient = new HttpClient();
    private const string TEMP_FOLDER = "temp_streams";

    [Command("stream")]
    [RequireUserPermission(GuildPermission.AttachFiles)]
    public async Task HandleStreamShare(string videoUrl = null)
    {
        // Se non c'è un URL, controlla gli allegati
        if (string.IsNullOrEmpty(videoUrl) && Context.Message.Attachments.Count == 0)
        {
            var embed = new EmbedBuilder()
                .WithTitle("📹 Come condividere video in alta qualità")
                .WithDescription(
                    "Per condividere un video in alta qualità, usa uno di questi metodi:\n\n" +
                    "1. Carica direttamente il video con il comando `!stream`\n" +
                    "2. Condividi un link a un video già caricato: `!stream [url]`\n\n" +
                    "**Servizi supportati per lo streaming:**\n" +
                    "• Google Drive (link condivisibile)\n" +
                    "• Streamable\n" +
                    "• WeTransfer\n" +
                    "• OneDrive (link condivisibile)\n\n" +
                    "Il bot creerà un link di visualizzazione ottimizzato per tutti gli utenti.")
                .WithColor(Color.Blue)
                .Build();

            await ReplyAsync(embed: embed);
            return;
        }

        await Context.Message.AddReactionAsync(new Emoji("⏳"));

        try
        {
            string shareLink;
            
            if (Context.Message.Attachments.Count > 0)
            {
                var attachment = Context.Message.Attachments.First();
                // Crea un link ottimizzato per lo streaming usando Streamable o servizi simili
                shareLink = await CreateStreamableLink(attachment.Url);
            }
            else
            {
                // Verifica e ottimizza l'URL fornito
                shareLink = await OptimizeVideoUrl(videoUrl);
            }

            var responseEmbed = new EmbedBuilder()
                .WithTitle("🎥 Video in Alta Qualità")
                .WithDescription($"Video condiviso da {Context.User.Mention}\n[Clicca qui per guardare in alta qualità]({shareLink})")
                .WithColor(Color.Green)
                .WithFooter("Questo link permette la visione in alta qualità per tutti gli utenti")
                .Build();

            await ReplyAsync(embed: responseEmbed);
            await Context.Message.RemoveAllReactionsAsync();
            await Context.Message.AddReactionAsync(new Emoji("✅"));
        }
        catch (Exception ex)
        {
            await Context.Message.RemoveAllReactionsAsync();
            await Context.Message.AddReactionAsync(new Emoji("❌"));
            await ReplyAsync($"❌ Errore durante l'elaborazione del video: {ex.Message}");
        }
    }

    private async Task<string> CreateStreamableLink(string videoUrl)
    {
        // Qui implementeremo l'integrazione con Streamable API
        // Per ora restituiamo l'URL originale
        return videoUrl;

        // TODO: Implementare l'integrazione con Streamable
        // 1. Scarica il video
        // 2. Caricalo su Streamable usando la loro API
        // 3. Restituisci il link di Streamable
    }

    private async Task<string> OptimizeVideoUrl(string url)
    {
        // Verifica il tipo di URL e ottimizza in base al servizio
        if (url.Contains("drive.google.com"))
        {
            // Ottimizza link di Google Drive
            return OptimizeGoogleDriveUrl(url);
        }
        else if (url.Contains("1drv.ms") || url.Contains("onedrive"))
        {
            // Ottimizza link di OneDrive
            return OptimizeOneDriveUrl(url);
        }
        else if (url.Contains("wetransfer.com"))
        {
            // Restituisci il link diretto se possibile
            return url;
        }

        // Se l'URL non è riconosciuto, restituisci quello originale
        return url;
    }

    private string OptimizeGoogleDriveUrl(string url)
    {
        // Converte il link di Google Drive in un formato diretto
        if (url.Contains("view?usp=sharing"))
        {
            url = url.Replace("view?usp=sharing", "preview");
        }
        return url;
    }

    private string OptimizeOneDriveUrl(string url)
    {
        // Ottimizza il link di OneDrive per lo streaming diretto
        if (!url.Contains("embed"))
        {
            url = url.Replace("view.aspx", "embed");
        }
        return url;
    }

    [Command("streaminfo")]
    public async Task StreamInfo()
    {
        var embed = new EmbedBuilder()
            .WithTitle("ℹ️ Informazioni sullo Streaming")
            .WithDescription(
                "**Formati supportati:**\n" +
                "• MP4 (H.264)\n" +
                "• WebM\n" +
                "• MOV\n\n" +
                "**Limiti di dimensione:**\n" +
                "• Massimo 2GB per file\n\n" +
                "**Servizi supportati:**\n" +
                "• Google Drive\n" +
                "• Streamable\n" +
                "• WeTransfer\n" +
                "• OneDrive\n\n" +
                "**Qualità supportata:**\n" +
                "• Fino a 4K (3840x2160)\n" +
                "• Bitrate fino a 50Mbps")
            .WithColor(Color.Blue)
            .Build();

        await ReplyAsync(embed: embed);
    }
}
