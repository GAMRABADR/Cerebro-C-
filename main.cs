using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace IA_CEREBRO;

public class Program
{
    private DiscordSocketClient? _client;
    private CommandService? _commands;
    private IServiceProvider? _services;
    private readonly KeepAlive _keepAlive;

    public Program()
    {
        _client = new DiscordSocketClient(new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent | GatewayIntents.GuildVoiceStates,
            LogLevel = LogSeverity.Info
        });

        _commands = new CommandService(new CommandServiceConfig
        {
            LogLevel = LogSeverity.Info,
            CaseSensitiveCommands = false,
        });

        _services = ConfigureServices();

        _keepAlive = new KeepAlive();
    }

    public static Task Main(string[] args) => new Program().MainAsync();

    public async Task MainAsync()
    {
        // Avvia il server keep-alive
        _keepAlive.Start();

        _client.Log += LogAsync;
        _client.Ready += ReadyAsync;
        _client.MessageReceived += HandleCommandAsync;

        string token = GetToken();

        await _client.LoginAsync(TokenType.Bot, token);
        await _client.StartAsync();

        await Task.Delay(Timeout.Infinite);
    }

    private string GetToken()
    {
        // Prova prima a leggere dalla variabile d'ambiente
        var token = Environment.GetEnvironmentVariable("DISCORD_TOKEN");
        
        // Se non trovato, prova a leggere dal file .env
        if (string.IsNullOrEmpty(token))
        {
            try
            {
                if (File.Exists(".env"))
                {
                    token = File.ReadAllText(".env").Trim();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore nella lettura del file .env: {ex.Message}");
            }
        }

        // Se ancora non trovato, prova a leggere da chiave.txt per retrocompatibilità
        if (string.IsNullOrEmpty(token))
        {
            try
            {
                if (File.Exists("chiave.txt"))
                {
                    token = File.ReadAllText("chiave.txt").Trim();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore nella lettura del file chiave.txt: {ex.Message}");
            }
        }

        if (string.IsNullOrEmpty(token))
        {
            throw new Exception("Token non trovato! Assicurati di averlo impostato nel file .env o nella variabile d'ambiente DISCORD_TOKEN");
        }

        return token;
    }

    private Task LogAsync(LogMessage log)
    {
        Console.WriteLine(log.ToString());
        return Task.CompletedTask;
    }

    private Task ReadyAsync()
    {
        Console.WriteLine($"{_client?.CurrentUser} è online!");
        return Task.CompletedTask;
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
