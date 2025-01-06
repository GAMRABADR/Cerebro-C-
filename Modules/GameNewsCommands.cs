using Discord;
using Discord.Commands;
using IA_CEREBRO.Helpers;
using System.Text;

namespace IA_CEREBRO.Modules;

public class GameNewsCommands : ModuleBase<SocketCommandContext>
{
    private static readonly Dictionary<string, string> CategoryEmojis = new()
    {
        { "pvp", "⚔️" },
        { "rpg", "🎭" },
        { "fps", "🎯" },
        { "strategy", "🎲" },
        { "mmo", "🌍" },
        { "survivor", "🏕️" }
    };

    [Command("news")]
    [Summary("Mostra le ultime 10 notizie sui videogiochi per una categoria specifica")]
    public async Task GetGameNews([Remainder] string category = "")
    {
        // Invia un messaggio iniziale di caricamento
        var loadingMsg = await ReplyAsync("🔍 Ricerca delle ultime notizie in corso...");

        try
        {
            var news = await GameNewsHelper.GetGameNews(category);

            if (!news.Any())
            {
                await loadingMsg.ModifyAsync(msg => msg.Content = "❌ Nessuna notizia trovata per questa categoria!");
                return;
            }

            var emoji = CategoryEmojis.GetValueOrDefault(category.ToLower(), "🎮");
            var embed = new EmbedBuilder()
                .WithTitle($"{emoji} Ultime Notizie Gaming {(string.IsNullOrEmpty(category) ? "" : $"- {category.ToUpper()}")}")
                .WithColor(Color.Blue)
                .WithFooter(footer => 
                {
                    footer.Text = $"Richiesto da {Context.User.Username} • Fonti: IGN, PC Gamer, GameSpot";
                })
                .WithCurrentTimestamp();

            var description = new StringBuilder();
            for (int i = 0; i < news.Count; i++)
            {
                var item = news[i];
                description.AppendLine($"**{i + 1}.** [{item.Title}]({item.Url})");
                description.AppendLine($" **Fonte:** {item.Source} • {item.Date:dd/MM/yyyy}");
                description.AppendLine();
            }

            embed.WithDescription(description.ToString());

            await loadingMsg.ModifyAsync(msg => 
            {
                msg.Content = null;
                msg.Embed = embed.Build();
            });
        }
        catch (Exception ex)
        {
            await loadingMsg.ModifyAsync(msg => 
                msg.Content = " Si è verificato un errore durante il recupero delle notizie. Riprova più tardi.");
            Console.WriteLine($"Errore nel comando news: {ex}");
        }
    }

    [Command("newshelp")]
    [Summary("Mostra le categorie disponibili per il comando news")]
    public async Task ShowNewsHelp()
    {
        var embed = new EmbedBuilder()
            .WithTitle("📰 Categorie Disponibili per le News")
            .WithColor(Color.Blue)
            .WithDescription("Usa il comando `!news [categoria]` per vedere le ultime notizie.\n" +
                           "Se non specifichi una categoria, verranno mostrate tutte le notizie.\n\n" +
                           "**Categorie disponibili:**\n" +
                           "⚔️ `pvp` - Giochi PvP e Multiplayer Competitivi\n" +
                           "🎭 `rpg` - Giochi di Ruolo e JRPG\n" +
                           "🎯 `fps` - Sparatutto in Prima Persona\n" +
                           "🎲 `strategy` - Giochi Strategici e Tattici\n" +
                           "🌍 `mmo` - MMO e Giochi Online\n" +
                           "🏕️ `survivor` - Giochi di Sopravvivenza e Crafting\n")
            .WithFooter(footer => 
            {
                footer.Text = $"Richiesto da {Context.User.Username}";
            })
            .WithCurrentTimestamp();

        await ReplyAsync(embed: embed.Build());
    }
}
