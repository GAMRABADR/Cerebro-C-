using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System.IO;
using IA_CEREBRO.Modules;
using Victoria;

namespace IA_CEREBRO;

public class Program
{
    private DiscordSocketClient? _client;
    private CommandService? _commands;
    private IServiceProvider? _services;
    private readonly KeepAlive _keepAlive;
    private LavaNode _lavaNode;

    public Program()
    {
        var config = new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent | GatewayIntents.GuildMembers
        };

        _client = new DiscordSocketClient(config);
        _commands = new CommandService();

        _services = new ServiceCollection()
            .AddSingleton(_client)
            .AddSingleton(_commands)
            .AddLavaNode(x => {
                x.SelfDeaf = false;
            })
            .BuildServiceProvider();

        _lavaNode = _services.GetRequiredService<LavaNode>();

        _keepAlive = new KeepAlive();
    }

    public static Task Main(string[] args) => new Program().MainAsync();

    public async Task MainAsync()
    {
        try 
        {
            Console.WriteLine("Starting bot initialization...");
            // Avvia il server keep-alive
            _keepAlive.Start();

            _client!.Log += LogAsync;
            _client!.Ready += async () =>
            {
                try
                {
                    await _lavaNode.ConnectAsync();
                    Console.WriteLine("LavaNode connesso con successo!");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Errore durante la connessione di LavaNode: {ex.Message}");
                }
                await ReadyAsync();
            };
            _client!.MessageReceived += HandleCommandAsync;
            _client!.UserVoiceStateUpdated += AudioCommands.HandleVoiceStateUpdated;

            // Registra i moduli dei comandi
            await _commands!.AddModulesAsync(Assembly.GetEntryAssembly(), _services);

            string token = GetToken();
            Console.WriteLine("Attempting to connect to Discord...");
            
            await _client!.LoginAsync(TokenType.Bot, token);
            Console.WriteLine("Login successful, starting client...");
            await _client!.StartAsync();
            Console.WriteLine("Client started successfully!");

            await Task.Delay(Timeout.Infinite);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Critical error in MainAsync: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            throw;
        }
    }

    private string GetToken()
    {
        try
        {
            // Prova a leggere dal file .env
            if (File.Exists(".env"))
            {
                Console.WriteLine("Trovato file .env, lettura del token...");
                string[] lines = File.ReadAllLines(".env");
                foreach (string line in lines)
                {
                    if (line.StartsWith("DISCORD_TOKEN="))
                    {
                        string token = line.Substring("DISCORD_TOKEN=".Length).Trim();
                        if (!string.IsNullOrEmpty(token))
                        {
                            Console.WriteLine("Token letto con successo dal file .env");
                            return token;
                        }
                    }
                }
            }

            // Se non trova nel file .env, prova le variabili d'ambiente
            var envToken = Environment.GetEnvironmentVariable("DISCORD_TOKEN");
            if (!string.IsNullOrEmpty(envToken))
            {
                Console.WriteLine("Token trovato nelle variabili d'ambiente");
                return envToken;
            }

            throw new Exception("Token non trovato! Assicurati che il file .env contenga DISCORD_TOKEN=il_tuo_token");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Errore nella lettura del token: {ex.Message}");
            throw;
        }
    }

    private Task LogAsync(LogMessage log)
    {
        Console.WriteLine(log.ToString());
        return Task.CompletedTask;
    }

    private async Task ReadyAsync()
    {
        Console.WriteLine($"⁦{_client.CurrentUser.Username}⁩#{_client.CurrentUser.Discriminator} è online!");
        await _client.SetGameAsync("!help per i comandi", type: ActivityType.Playing);

        // Se il bot è stato riavviato, invia un messaggio di conferma
        var startupTime = DateTime.Now;
        if (File.Exists(Path.Combine(Path.GetTempPath(), "restart_bot.bat")))
        {
            await Task.Delay(2000); // Aspetta che il bot sia completamente inizializzato
            try
            {
                var guilds = _client.Guilds;
                foreach (var guild in guilds)
                {
                    var defaultChannel = guild.DefaultChannel;
                    var currentUser = guild.CurrentUser;
                    if (defaultChannel != null && 
                        defaultChannel is ITextChannel textChannel && 
                        currentUser.GetPermissions(textChannel).SendMessages)
                    {
                        await defaultChannel.SendMessageAsync($"✅ Bot riavviato con successo! Sono tornato online alle {startupTime:HH:mm:ss}");
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore nell'invio del messaggio di riavvio: {ex.Message}");
            }
        }
    }

    private async Task HandleCommandAsync(SocketMessage messageParam)
    {
        if (messageParam is not SocketUserMessage message) return;
        if (message.Author.Id == _client?.CurrentUser.Id) return;

        int argPos = 0;
        if (!message.HasCharPrefix('!', ref argPos)) return;

        var context = new SocketCommandContext(_client, message);
        await _commands!.ExecuteAsync(context, argPos, _services);
    }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection()
            .AddSingleton<CommandService>()
            .AddSingleton<DiscordSocketClient>();

        return services.BuildServiceProvider();
    }
}
