using HtmlAgilityPack;
using System.Text.RegularExpressions;
using System.Web;

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
    private static readonly Dictionary<string, string[]> CategoryKeywords = new()
    {
        { "pvp", new[] { "pvp", "multiplayer", "competitive", "esports", "battle royale", "versus", "competition", "arena", "team-based" } },
        { "rpg", new[] { "rpg", "role-playing", "jrpg", "action rpg", "mmorpg", "role playing", "dungeon", "fantasy rpg" } },
        { "fps", new[] { "fps", "first-person shooter", "shooter", "battle royale", "first person", "shooting game" } },
        { "strategy", new[] { "strategy", "rts", "4x", "grand strategy", "tactical", "turn-based", "real-time strategy" } },
        { "mmo", new[] { "mmo", "mmorpg", "multiplayer online", "online rpg", "massive multiplayer", "online world" } },
        { "survivor", new[] { "survival", "survivor", "crafting", "sandbox", "open world survival", "survival horror", "survival craft", "survival game", "base building" } }
    };

    private static readonly string[] PCKeywords = new[]
    {
        "pc", "steam", "epic games", "gaming pc", "desktop", "windows",
        "rtx", "nvidia", "amd", "intel", "gpu", "cpu", "hardware",
        "directx", "vulkan", "dx12", "pc gaming", "pc release", "pc version",
        "pc port", "pc exclusive", "pc requirements", "system requirements"
    };

    private static readonly string[] GameSites = new[]
    {
        "https://www.pcgamesn.com/",
        "https://www.dsogaming.com/",
        "https://www.rockpapershotgun.com/news/",
        "https://www.eurogamer.net/pc",
        "https://www.gamespot.com/pc/",
        "https://www.vg247.com/pc-gaming",
        "https://www.thegamer.com/pc/",
        "https://www.pcworld.com/category/gaming/"
    };

    private static readonly Dictionary<string, string> SiteColors = new()
    {
        { "pcgamesn.com", "[#9370DB]" },       // Viola medio
        { "dsogaming.com", "[#FF4500]" },      // Arancione
        { "rockpapershotgun.com", "[#32CD32]" }, // Verde lime
        { "eurogamer.net", "[#4169E1]" },      // Blu reale
        { "gamespot.com", "[#FFD700]" },       // Oro
        { "vg247.com", "[#FF69B4]" },          // Rosa caldo
        { "thegamer.com", "[#20B2AA]" },       // Verde acqua
        { "pcworld.com", "[#8B4513]" }         // Marrone
    };

    public static async Task<List<GameNews>> GetGameNews(string category)
    {
        var allNews = new List<GameNews>();
        var client = new HttpClient();
        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
        
        foreach (var site in GameSites)
        {
            try 
            {
                var web = new HtmlWeb();
                web.PreRequest = request => 
                {
                    request.AutomaticDecompression = System.Net.DecompressionMethods.All;
                    request.Headers["User-Agent"] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36";
                    return true;
                };

                var siteName = new Uri(site).Host.Replace("www.", "");
                Console.WriteLine($"\n[DEBUG] Tentativo di caricamento da {siteName}");

                var doc = await web.LoadFromWebAsync(site);
                var siteColor = SiteColors.GetValueOrDefault(siteName, "[#FFFFFF]");

                var articleNodes = siteName switch
                {
                    "pcgamesn.com" => doc.DocumentNode.SelectNodes("//article | //div[contains(@class, 'article-card')]"),
                    "dsogaming.com" => doc.DocumentNode.SelectNodes("//article | //div[contains(@class, 'jeg_posts')]//article"),
                    "rockpapershotgun.com" => doc.DocumentNode.SelectNodes("//article[contains(@class, 'summary')] | //div[contains(@class, 'article')]"),
                    "eurogamer.net" => doc.DocumentNode.SelectNodes("//article | //div[contains(@class, 'article')]"),
                    "gamespot.com" => doc.DocumentNode.SelectNodes("//article | //div[contains(@class, 'card-item')]"),
                    "vg247.com" => doc.DocumentNode.SelectNodes("//article | //div[contains(@class, 'article-card')]"),
                    "thegamer.com" => doc.DocumentNode.SelectNodes("//article | //div[contains(@class, 'display-card')]"),
                    "pcworld.com" => doc.DocumentNode.SelectNodes("//article | //div[contains(@class, 'article-card')]"),
                    _ => doc.DocumentNode.SelectNodes("//article | //div[contains(@class, 'article')]")
                };

                Console.WriteLine($"[DEBUG] {siteName}: Trovati {articleNodes?.Count ?? 0} articoli");

                if (articleNodes != null)
                {
                    var siteNews = new List<GameNews>();
                    foreach (var node in articleNodes)
                    {
                        try
                        {
                            var titleNode = siteName switch
                            {
                                "pcgamesn.com" => node.SelectSingleNode(".//h3 | .//h2"),
                                "dsogaming.com" => node.SelectSingleNode(".//h2 | .//h3[contains(@class, 'jeg_post_title')]"),
                                "rockpapershotgun.com" => node.SelectSingleNode(".//h2 | .//h3"),
                                "eurogamer.net" => node.SelectSingleNode(".//h2 | .//h1"),
                                "gamespot.com" => node.SelectSingleNode(".//h3 | .//h2"),
                                "vg247.com" => node.SelectSingleNode(".//h3 | .//h2"),
                                "thegamer.com" => node.SelectSingleNode(".//h3 | .//h2"),
                                "pcworld.com" => node.SelectSingleNode(".//h3 | .//h2"),
                                _ => node.SelectSingleNode(".//h2 | .//h3 | .//h1")
                            };

                            var linkNode = siteName switch
                            {
                                "pcgamesn.com" => node.SelectSingleNode(".//h3/a | .//h2/a"),
                                "dsogaming.com" => node.SelectSingleNode(".//h2/a | .//h3/a"),
                                "rockpapershotgun.com" => node.SelectSingleNode(".//h2/a | .//h3/a"),
                                "eurogamer.net" => node.SelectSingleNode(".//h2/a | .//h1/a"),
                                "gamespot.com" => node.SelectSingleNode(".//h3/a | .//h2/a"),
                                "vg247.com" => node.SelectSingleNode(".//h3/a | .//h2/a"),
                                "thegamer.com" => node.SelectSingleNode(".//h3/a | .//h2/a"),
                                "pcworld.com" => node.SelectSingleNode(".//h3/a | .//h2/a"),
                                _ => node.SelectSingleNode(".//h2/a | .//h3/a | .//h1/a")
                            } ?? titleNode;

                            if (titleNode != null && linkNode != null)
                            {
                                var title = WebUtility.HtmlDecode(titleNode.InnerText.Trim());
                                var url = linkNode.GetAttributeValue("href", "");
                                
                                if (!url.StartsWith("http"))
                                {
                                    if (url.StartsWith("//"))
                                    {
                                        url = $"https:{url}";
                                    }
                                    else
                                    {
                                        url = $"https://{siteName}{(url.StartsWith("/") ? "" : "/")}{url}";
                                    }
                                }

                                title = $"{siteColor}{title}[/]";

                                var gameNews = new GameNews
                                {
                                    Title = title,
                                    Url = url,
                                    Source = siteName,
                                    Date = DateTime.Now,
                                    Category = ExtractCategory(title)
                                };

                                if (IsPCRelated(title))
                                {
                                    siteNews.Add(gameNews);
                                    Console.WriteLine($"[DEBUG] {siteName}: Aggiunta notizia PC '{title}'");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[ERROR] Errore nel parsing di un articolo da {siteName}: {ex.Message}");
                            continue;
                        }
                    }

                    var relevantNews = siteNews
                        .Where(n => string.IsNullOrEmpty(category) || 
                               IsRelevantToCategory(n.Title.ToLower(), category))
                        .Take(10)
                        .ToList();

                    Console.WriteLine($"[DEBUG] {siteName}: Aggiunte {relevantNews.Count} notizie");
                    allNews.AddRange(relevantNews);
                }
                else
                {
                    Console.WriteLine($"[WARNING] {siteName}: Nessun articolo trovato");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Errore nel caricamento delle notizie da {site}: {ex.Message}");
                continue;
            }
        }

        var orderedNews = allNews
            .OrderByDescending(n => n.Date)
            .ToList();
            
        Console.WriteLine($"\n[DEBUG] Totale notizie PC ordinate: {orderedNews.Count}");
        foreach (var news in orderedNews)
        {
            Console.WriteLine($"[DEBUG] - {news.Source}: {news.Title}");
        }

        return orderedNews;
    }

    private static bool IsPCRelated(string text)
    {
        text = text.ToLower();
        return PCKeywords.Any(keyword => text.Contains(keyword.ToLower())) ||
               !text.Contains("xbox") && !text.Contains("playstation") && 
               !text.Contains("ps5") && !text.Contains("ps4") && 
               !text.Contains("switch") && !text.Contains("nintendo") &&
               !text.Contains("mobile") && !text.Contains("android") && 
               !text.Contains("ios");
    }

    private static string ExtractCategory(string title)
    {
        return "";
    }

    private static bool IsRelevantToCategory(string content, string category)
    {
        if (string.IsNullOrEmpty(category)) return true;
        
        category = category.ToLower();
        if (!CategoryKeywords.ContainsKey(category)) return true;

        var keywords = CategoryKeywords[category];
        return keywords.Any(k => content.Contains(k.ToLower()));
    }
}
