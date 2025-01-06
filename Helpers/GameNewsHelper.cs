using HtmlAgilityPack;
using System.Text.RegularExpressions;

namespace IA_CEREBRO.Helpers;

public class GameNews
{
    public string Title { get; set; }
    public string Url { get; set; }
    public string Source { get; set; }
    public DateTime Date { get; set; }
    public string Category { get; set; }
}

public static class GameNewsHelper
{
    private static readonly HttpClient client = new HttpClient();
    private static readonly Dictionary<string, string[]> CategoryKeywords = new()
    {
        { "pvp", new[] { "pvp", "multiplayer", "competitive", "esports", "battle royale", "versus", "competition", "arena", "team-based" } },
        { "rpg", new[] { "rpg", "role-playing", "jrpg", "action rpg", "mmorpg", "role playing", "dungeon", "fantasy rpg" } },
        { "fps", new[] { "fps", "first-person shooter", "shooter", "battle royale", "first person", "shooting game" } },
        { "strategy", new[] { "strategy", "rts", "4x", "grand strategy", "tactical", "turn-based", "real-time strategy" } },
        { "mmo", new[] { "mmo", "mmorpg", "multiplayer online", "online rpg", "massive multiplayer", "online world" } },
        { "survivor", new[] { "survival", "survivor", "crafting", "sandbox", "open world survival", "survival horror", "survival craft", "survival game", "base building" } }
    };

    private static readonly string[] GameSites = new[]
    {
        "https://www.pcgamer.com/news",
        "https://www.ign.com/pc",
        "https://www.gamespot.com/pc",
        "https://www.rockpapershotgun.com",
        "https://www.eurogamer.net",
        "https://www.pcgamesn.com",
        "https://www.gamespace.com",
        "https://www.spaziogames.it/pc"
    };

    // Dizionario che mappa i siti ai loro colori
    private static readonly Dictionary<string, string> SiteColors = new()
    {
        { "pcgamer.com", "[#FF6B6B]" },      // Rosso
        { "ign.com", "[#4ECDC4]" },          // Turchese
        { "gamespot.com", "[#FFD93D]" },     // Giallo
        { "rockpapershotgun.com", "[#95E1D3]" }, // Verde acqua
        { "eurogamer.net", "[#A8E6CF]" },    // Verde chiaro
        { "pcgamesn.com", "[#FF8B94]" },     // Rosa
        { "gamespace.com", "[#DCD6F7]" },    // Lavanda
        { "spaziogames.it", "[#F7D794]" }    // Arancione chiaro
    };

    public static async Task<List<GameNews>> GetGameNews(string category)
    {
        var tasks = GameSites.Select(site => GetNewsFromSite(site, category));
        var allNewsLists = await Task.WhenAll(tasks);
        
        // Prendi le prime 5 notizie da ogni sito
        var combinedNews = allNewsLists
            .Where(newsList => newsList.Any())
            .SelectMany(newsList => newsList.Take(5))
            .OrderByDescending(news => news.Date)
            .ToList();

        return combinedNews;
    }

    private static async Task<List<GameNews>> GetNewsFromSite(string siteUrl, string category)
    {
        try
        {
            var news = new List<GameNews>();
            var web = new HtmlWeb();
            var doc = await web.LoadFromWebAsync(siteUrl);

            var siteName = new Uri(siteUrl).Host.Replace("www.", "");
            var siteColor = SiteColors.GetValueOrDefault(siteName, "[#FFFFFF]"); // Bianco come colore predefinito
            
            // Selettori personalizzati per ogni sito
            var articleNodes = siteName switch
            {
                "spaziogames.it" => doc.DocumentNode.SelectNodes("//article[contains(@class, 'article-preview')]") ??
                                   doc.DocumentNode.SelectNodes("//div[contains(@class, 'article-preview')]"),
                _ => doc.DocumentNode.SelectNodes("//article") ?? 
                     doc.DocumentNode.SelectNodes("//div[contains(@class, 'article')]") ??
                     doc.DocumentNode.SelectNodes("//div[contains(@class, 'news-item')]") ??
                     doc.DocumentNode.SelectNodes("//div[contains(@class, 'post')]")
            };

            if (articleNodes != null)
            {
                foreach (var node in articleNodes)
                {
                    try
                    {
                        var titleNode = siteName switch
                        {
                            "spaziogames.it" => node.SelectSingleNode(".//h2[contains(@class, 'article-preview__title')]") ??
                                               node.SelectSingleNode(".//h3[contains(@class, 'article-preview__title')]"),
                            _ => node.SelectSingleNode(".//h3") ?? 
                                 node.SelectSingleNode(".//h2") ??
                                 node.SelectSingleNode(".//a[contains(@class, 'title')]")
                        };
                                      
                        var linkNode = node.SelectSingleNode(".//a[contains(@href, '/')]");
                        var dateNode = node.SelectSingleNode(".//time") ?? 
                                     node.SelectSingleNode(".//*[contains(@class, 'date')]") ??
                                     node.SelectSingleNode(".//*[contains(@class, 'article-preview__date')]");

                        if (titleNode != null && linkNode != null)
                        {
                            var title = titleNode.InnerText.Trim();
                            var url = linkNode.GetAttributeValue("href", "");
                            if (!url.StartsWith("http"))
                            {
                                url = $"https://{siteName}{(url.StartsWith("/") ? "" : "/")}{url}";
                            }

                            var date = dateNode != null ? ParseDate(dateNode.GetAttributeValue("datetime", dateNode.InnerText)) : DateTime.Now;

                            // Estrai eventuali tag o categorie dall'articolo
                            var categoryTags = node.SelectNodes(".//*[contains(@class, 'tag') or contains(@class, 'category') or contains(@class, 'article-preview__category')]");
                            var articleCategories = categoryTags != null 
                                ? string.Join(" ", categoryTags.Select(t => t.InnerText.Trim()))
                                : "";

                            news.Add(new GameNews
                            {
                                Title = $"{siteColor}{title}[/]", // Aggiungi il colore al titolo
                                Url = url,
                                Source = siteName,
                                Date = date,
                                Category = articleCategories
                            });
                        }
                    }
                    catch
                    {
                        // Ignora errori nel parsing del singolo articolo
                        continue;
                    }
                }
            }
            return news;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Errore nel recupero delle news da {siteUrl}: {ex.Message}");
            return new List<GameNews>();
        }
    }

    private static bool IsRelevantToCategory(string content, string category)
    {
        if (string.IsNullOrEmpty(category)) return true;
        
        category = category.ToLower();
        if (!CategoryKeywords.ContainsKey(category)) return true;

        var keywords = CategoryKeywords[category];
        return keywords.Any(k => content.Contains(k.ToLower()));
    }

    private static DateTime ParseDate(string dateStr)
    {
        try
        {
            // Rimuovi eventuali testi aggiuntivi come "ago", "published", ecc.
            dateStr = Regex.Replace(dateStr, @"\b(ago|published|posted|on)\b", "", RegexOptions.IgnoreCase).Trim();
            
            // Prova diversi formati di data comuni
            if (DateTime.TryParse(dateStr, out DateTime result))
                return result;

            // Gestisci formati relativi come "2 hours ago", "3 days ago", ecc.
            var relativeMatch = Regex.Match(dateStr, @"(\d+)\s*(hour|day|week|month|year)s?\s*");
            if (relativeMatch.Success)
            {
                var amount = int.Parse(relativeMatch.Groups[1].Value);
                var unit = relativeMatch.Groups[2].Value.ToLower();
                
                return unit switch
                {
                    "hour" => DateTime.Now.AddHours(-amount),
                    "day" => DateTime.Now.AddDays(-amount),
                    "week" => DateTime.Now.AddDays(-amount * 7),
                    "month" => DateTime.Now.AddMonths(-amount),
                    "year" => DateTime.Now.AddYears(-amount),
                    _ => DateTime.Now
                };
            }

            return DateTime.Now;
        }
        catch
        {
            return DateTime.Now;
        }
    }
}
