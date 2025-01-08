using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Cerebro.Modules
{
    public sealed class VoiceService : IDisposable
    {
        private readonly DiscordSocketClient _client;
        private readonly ILogger<VoiceService> _logger;
        private readonly ConcurrentDictionary<ulong, VoiceState> _voiceStates;
        private readonly SemaphoreSlim _reconnectLock;
        private readonly CancellationTokenSource _disposalToken;
        
        private const int MaxRetries = 5;
        private const int BaseDelayMs = 2000;
        private const int MaxDelayMs = 30000;

        public VoiceService(DiscordSocketClient client, ILogger<VoiceService> logger)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _voiceStates = new ConcurrentDictionary<ulong, VoiceState>();
            _reconnectLock = new SemaphoreSlim(1, 1);
            _disposalToken = new CancellationTokenSource();

            InitializeEvents();
        }

        private void InitializeEvents()
        {
            _client.VoiceServerUpdated += HandleVoiceServerUpdate;
            _client.Disconnected += HandleDisconnect;
            _client.UserVoiceStateUpdated += HandleVoiceStateUpdate;
        }

        private async Task HandleVoiceServerUpdate(SocketVoiceServer server)
        {
            try
            {
                if (_voiceStates.TryGetValue(server.Guild.Id, out var state) && !state.IsReconnecting)
                {
                    _logger.LogInformation($"Voice server updated: Guild={server.Guild.Id}");
                    await ReconnectAsync(state);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error handling voice update: Guild={server.Guild.Id}");
            }
        }

        private async Task HandleDisconnect(Exception ex)
        {
            _logger.LogWarning(ex, "Discord connection lost");
            
            foreach (var state in _voiceStates.Values)
            {
                if (!state.IsReconnecting && state.Channel != null)
                {
                    _ = Task.Run(async () => await ReconnectAsync(state));
                }
            }
        }

        private Task HandleVoiceStateUpdate(SocketUser user, SocketVoiceState before, SocketVoiceState after)
        {
            if (user.Id == _client.CurrentUser.Id)
            {
                var guildId = (before.VoiceChannel ?? after.VoiceChannel)?.Guild.Id;
                if (guildId.HasValue)
                {
                    UpdateVoiceState(guildId.Value, after.VoiceChannel);
                }
            }
            return Task.CompletedTask;
        }

        private void UpdateVoiceState(ulong guildId, SocketVoiceChannel? channel)
        {
            if (_voiceStates.TryGetValue(guildId, out var state))
            {
                state.CurrentChannel = channel;
            }
        }

        private async Task ReconnectAsync(VoiceState state)
        {
            if (state.IsReconnecting || state.Channel == null) return;

            await _reconnectLock.WaitAsync(_disposalToken.Token);
            try
            {
                state.IsReconnecting = true;
                state.RetryCount++;

                if (state.RetryCount > MaxRetries)
                {
                    _logger.LogError($"Max reconnection attempts reached: Guild={state.GuildId}");
                    ResetState(state);
                    return;
                }

                var delay = Math.Min(BaseDelayMs * Math.Pow(2, state.RetryCount - 1), MaxDelayMs);
                await Task.Delay((int)delay, _disposalToken.Token);

                try
                {
                    _logger.LogInformation($"Reconnecting to voice: Guild={state.GuildId}, Attempt={state.RetryCount}/{MaxRetries}");
                    
                    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                    using var linkedToken = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token, _disposalToken.Token);
                    
                    await state.Channel.ConnectAsync(selfDeaf: false, selfMute: false, external: false)
                        .WaitAsync(linkedToken.Token);
                    
                    ResetState(state);
                    _logger.LogInformation($"Voice reconnection successful: Guild={state.GuildId}");
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, $"Voice reconnection failed: Guild={state.GuildId}");
                    if (state.RetryCount < MaxRetries)
                    {
                        _ = Task.Run(async () => await ReconnectAsync(state));
                    }
                }
            }
            finally
            {
                _reconnectLock.Release();
            }
        }

        public async Task JoinChannelAsync(IVoiceChannel channel)
        {
            var state = _voiceStates.GetOrAdd(channel.GuildId, gid => new VoiceState(gid));
            state.Channel = channel;

            try
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                await channel.ConnectAsync(selfDeaf: false, selfMute: false, external: false)
                    .WaitAsync(timeout.Token);
                    
                ResetState(state);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to join voice channel: Guild={channel.GuildId}, Channel={channel.Name}");
                await ReconnectAsync(state);
            }
        }

        public async Task LeaveChannelAsync(ulong guildId)
        {
            if (_voiceStates.TryRemove(guildId, out var state) && state.CurrentChannel != null)
            {
                try
                {
                    await state.CurrentChannel.DisconnectAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error leaving voice channel: Guild={guildId}");
                }
            }
        }

        private void ResetState(VoiceState state)
        {
            state.IsReconnecting = false;
            state.RetryCount = 0;
        }

        public void Dispose()
        {
            try
            {
                _disposalToken.Cancel();
                
                foreach (var state in _voiceStates.Values)
                {
                    if (state.CurrentChannel != null)
                    {
                        state.CurrentChannel.DisconnectAsync().GetAwaiter().GetResult();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during VoiceService disposal");
            }
            finally
            {
                _disposalToken.Dispose();
                _reconnectLock.Dispose();
                
                _client.VoiceServerUpdated -= HandleVoiceServerUpdate;
                _client.Disconnected -= HandleDisconnect;
                _client.UserVoiceStateUpdated -= HandleVoiceStateUpdate;
            }
        }

        private class VoiceState
        {
            public ulong GuildId { get; }
            public IVoiceChannel? Channel { get; set; }
            public SocketVoiceChannel? CurrentChannel { get; set; }
            public bool IsReconnecting { get; set; }
            public int RetryCount { get; set; }

            public VoiceState(ulong guildId)
            {
                GuildId = guildId;
            }
        }
    }
}
