using Discord;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;

namespace Cerebro.Modules
{
    public class VoiceConnectionHandler
    {
        private readonly DiscordSocketClient _client;
        private IVoiceChannel _lastVoiceChannel;
        private bool _isReconnecting;
        private const int MaxReconnectAttempts = 5;
        private int _currentReconnectAttempts;

        public VoiceConnectionHandler(DiscordSocketClient client)
        {
            _client = client;
            _client.VoiceServerUpdated += HandleVoiceServerUpdate;
            _client.Disconnected += HandleDisconnect;
        }

        private async Task HandleVoiceServerUpdate(SocketVoiceServer server)
        {
            Console.WriteLine($"Voice server updated: {server.Guild.Name}");
            if (_lastVoiceChannel != null && !_isReconnecting)
            {
                await AttemptReconnect();
            }
        }

        private async Task HandleDisconnect(Exception ex)
        {
            if (_lastVoiceChannel != null && !_isReconnecting)
            {
                Console.WriteLine($"Bot disconnected from voice. Reason: {ex?.Message}");
                await AttemptReconnect();
            }
        }

        private async Task AttemptReconnect()
        {
            if (_currentReconnectAttempts >= MaxReconnectAttempts)
            {
                Console.WriteLine("Max reconnection attempts reached. Giving up.");
                _isReconnecting = false;
                _currentReconnectAttempts = 0;
                return;
            }

            _isReconnecting = true;
            _currentReconnectAttempts++;

            try
            {
                Console.WriteLine($"Attempting to reconnect to voice (Attempt {_currentReconnectAttempts}/{MaxReconnectAttempts})");
                await Task.Delay(5000 * _currentReconnectAttempts); // Exponential backoff
                await _lastVoiceChannel.ConnectAsync();
                
                _isReconnecting = false;
                _currentReconnectAttempts = 0;
                Console.WriteLine("Successfully reconnected to voice channel");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to reconnect: {ex.Message}");
                if (_currentReconnectAttempts < MaxReconnectAttempts)
                {
                    await AttemptReconnect();
                }
            }
        }

        public async Task SetVoiceChannel(IVoiceChannel channel)
        {
            _lastVoiceChannel = channel;
            await channel.ConnectAsync();
        }

        public void Reset()
        {
            _lastVoiceChannel = null;
            _isReconnecting = false;
            _currentReconnectAttempts = 0;
        }
    }
}
