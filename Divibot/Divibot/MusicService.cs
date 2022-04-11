using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using DSharpPlus.Lavalink.EventArgs;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Divibot.Music {

    // The primary music service
    public class MusicService {

        // Properties
        public static EventId MusicServiceLogEventId { get; } = new EventId(1752, "MusicService");
        public IDictionary<ulong, MusicPlayer> Players { get; } = new Dictionary<ulong, MusicPlayer>();

        // Dependency Injection
        private DiscordClient _client;

        // Constructor
        public MusicService(DiscordClient client) {
            this._client = client;

            // Get ideal node and attach events
            LavalinkExtension lavalink = _client.GetLavalink();
            LavalinkNodeConnection node = lavalink.GetIdealNodeConnection();
            node.GuildConnectionCreated += async (conn, args) => Players.Add(conn.Guild.Id, new MusicPlayer(conn.Guild.Id));
            node.GuildConnectionRemoved += async (conn, args) => Players.Remove(conn.Guild.Id);
            node.PlaybackStarted += OnPlaybackStarted;
            node.PlaybackFinished += OnPlaybackFinished;
            node.TrackException += OnTrackException;
            node.TrackStuck += OnTrackStuck;
        }

        // Skips the current song, ending the connection if no more items in the queue
        public async Task Skip(LavalinkGuildConnection conn, MusicPlayer player) {
            if (!player.Queue.TryDequeue(out LavalinkTrack nextTrack)) {
                await player.TextChannel.SendMessageAsync(new DiscordMessageBuilder() {
                    Content = "There are no more items in the queue."
                });
                await conn.DisconnectAsync();
                return;
            }
            await conn.PlayAsync(nextTrack);
        }

        // Handles playback starting
        private async Task OnPlaybackStarted(LavalinkGuildConnection conn, TrackStartEventArgs args) {
            _client.Logger.LogInformation(MusicServiceLogEventId, $"Playback started for {conn.Guild.Name} ({conn.Guild.Id})");

            // Fetch player
            if (!Players.TryGetValue(conn.Guild.Id, out MusicPlayer player)) {
                await player.TextChannel.SendMessageAsync(new DiscordMessageBuilder() {
                    Content = "There was an internal error trying to play music. Try stopping then trying again."
                });
                return;
            }

            // Send message
            LavalinkTrack track = conn.CurrentState.CurrentTrack;
            await player.TextChannel.SendMessageAsync(new DiscordMessageBuilder() {
                Content = $"Now Playing: **{track.Title}** by *{track.Author}*"
            });
        }

        // Handles playback finishing
        private async Task OnPlaybackFinished(LavalinkGuildConnection conn, TrackFinishEventArgs args) {
            // If the reason is "replaced" that's okay, just ignore it
            if (args.Reason == TrackEndReason.Replaced) {
                return;
            }

            // Log it
            _client.Logger.LogInformation(MusicServiceLogEventId, $"Playback finished for {conn.Guild.Name} ({conn.Guild.Id}). Reason: {args.Reason}");

            // Fetch player
            if (!Players.TryGetValue(conn.Guild.Id, out MusicPlayer player)) {
                // INTERNAL ERROR; CAN'T FIND PLAYER?
                return;
            }

            // Fetch next track
            await this.Skip(conn, player);
        }

        // Handles track exceptions
        private async Task OnTrackException(LavalinkGuildConnection conn, TrackExceptionEventArgs args) {
            _client.Logger.LogInformation(MusicServiceLogEventId, $"Track exception in {conn.Guild.Name} ({conn.Guild.Id})");

            // Fetch player
            if (!Players.TryGetValue(conn.Guild.Id, out MusicPlayer player)) {
                // INTERNAL ERROR; CAN'T FIND PLAYER?
                return;
            }

            // Alert of track exception
            await player.TextChannel.SendMessageAsync(new DiscordMessageBuilder() {
                Content = $"Track {conn.CurrentState.CurrentTrack.Title} threw an exception! Skipping..."
            });
        }

        // Handles track stuck
        private async Task OnTrackStuck(LavalinkGuildConnection conn, TrackStuckEventArgs args) {
            _client.Logger.LogInformation(MusicServiceLogEventId, $"Track stuck in {conn.Guild.Name} ({conn.Guild.Id})");

            // Fetch player
            if (!Players.TryGetValue(conn.Guild.Id, out MusicPlayer player)) {
                // INTERNAL ERROR; CAN'T FIND PLAYER?
                return;
            }

            // Alert of track exception
            await player.TextChannel.SendMessageAsync(new DiscordMessageBuilder() {
                Content = $"Track {conn.CurrentState.CurrentTrack.Title} got stuck! Skipping..."
            });
        }

    }

    // The queue class
    public class MusicPlayer {

        // Properties
        public ulong GuildId { get; }
        public DiscordChannel TextChannel { get; set; } = null;
        public Queue<LavalinkTrack> Queue { get; } = new Queue<LavalinkTrack>();

        // Constructor
        public MusicPlayer(ulong guildId) {
            GuildId = guildId;
        }

    }

}
