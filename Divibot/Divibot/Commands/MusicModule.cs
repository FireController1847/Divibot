using Divibot.Music;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using DSharpPlus.Lavalink.EventArgs;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Web;

namespace Divibot.Commands {

    [SlashCommandGroup("music", "Various commands related to musical functionalities.")]
    public class MusicModule : ApplicationCommandModule {

        // Dependency Injection
        public MusicService _musicService;
        public HttpClient HttpClient;

        // Constructor
        public MusicModule(MusicService musicService) {
            this._musicService = musicService;
            this.HttpClient = new HttpClient();
            this.HttpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Divibot", "100.0.1185.36"));
        }

        [SlashCommand("play", "Plays music!")]
        [SlashRequireGuild]
        public async Task PlayAsync(InteractionContext context, [Option("Song", "The song you want to search for or play. Can be a URL or search query.")] string song) {
            LavalinkNodeConnection node = await this.GetNodeConnectionAsync(context);
            if (node == null) return;

            // Check current user's channel
            DiscordChannel voiceChannel = context.Member.VoiceState?.Channel;
            if (voiceChannel == null) {
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder() {
                    Content = "It looks like you're not in a voice channel. Join one for me to join you, then try again!",
                    IsEphemeral = true
                });
            }

            // Get the guild connection / connect
            // WARN: https://github.com/DSharpPlus/DSharpPlus/issues/1269
            LavalinkGuildConnection conn = await node.ConnectAsync(voiceChannel);

            // Fetch the player
            _musicService.Players.TryGetValue(context.Guild.Id, out MusicPlayer player);
            if (player.TextChannel == null) {
                // Essentially "MusicPlayer" initialization
                player.TextChannel = context.Channel;
                await conn.SetVolumeAsync(10);
            }

            // Searching!
            await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder() {
                Content = "Searching..."
            });

            // Search for song
            LavalinkLoadResult result;
            if (song.IndexOf("https://") == -1 && song.IndexOf("http://") == -1) {
                result = await node.Rest.GetTracksAsync(song, LavalinkSearchType.Youtube);
            } else {
                result = await node.Rest.GetTracksAsync(song, LavalinkSearchType.Plain);
            }

            // Handle load responses
            if (result.LoadResultType == LavalinkLoadResultType.LoadFailed || result.LoadResultType == LavalinkLoadResultType.NoMatches) {
                await context.EditResponseAsync(new DiscordWebhookBuilder() {
                    Content = "I was unable to find the song you're searching for, or there was an error."
                });
            } else if (result.LoadResultType == LavalinkLoadResultType.PlaylistLoaded) {
                // Check for manage guild permission
                // TODO: Make this an editable permission node within the bot
                if (!context.Member.Permissions.HasPermission(Permissions.ManageGuild)) {
                    await context.EditResponseAsync(new DiscordWebhookBuilder() {
                        Content = "Sorry, you must have the Manage Server permission to be able to load playlists!"
                    });
                } else {
                    foreach (LavalinkTrack track in result.Tracks) {
                        if (conn.CurrentState.CurrentTrack == null) {
                            await conn.PlayAsync(track);
                        } else {
                            player.Queue.Enqueue(track);
                        }
                    }
                    await context.EditResponseAsync(new DiscordWebhookBuilder() {
                        Content = "Alright, I've queued up " + result.Tracks.Count() + " songs!"
                    });
                    return;
                }
            } else if (conn.CurrentState.CurrentTrack != null) {
                LavalinkTrack track = result.Tracks.First();
                player.Queue.Enqueue(track);
                await context.EditResponseAsync(new DiscordWebhookBuilder() {
                    Content = $"Alright, I have added **{track.Title}** by *{track.Author}* to the queue. It is position {player.Queue.Count}"
                });
                return;
            } else {
                await conn.PlayAsync(result.Tracks.First());
                await context.EditResponseAsync(new DiscordWebhookBuilder() {
                    Content = $"Let's go! It's time to start this jammin' session!"
                });
                return;
            }
        }

        [SlashCommand("stop", "Stops the music")]
        [SlashRequireGuild]
        public async Task StopAsync(InteractionContext context) {
            LavalinkNodeConnection node = await this.GetNodeConnectionAsync(context);
            if (node == null) return;
            LavalinkGuildConnection conn = await this.GetGuildConnectionAsync(context, node);
            if (conn == null) return;

            // Forcefully remove player just in case.
            _musicService.Players.Remove(context.Guild.Id);
            await conn.DisconnectAsync();

            // Respond
            await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder() {
                Content = "Alrighty, I've stopped playing music in here!"
            });
        }

        [SlashCommand("skip", "Skips to the next song in the queue")]
        [SlashRequireGuild]
        public async Task SkipAsync(InteractionContext context) {
            LavalinkNodeConnection node = await this.GetNodeConnectionAsync(context);
            if (node == null) return;
            LavalinkGuildConnection conn = await this.GetGuildConnectionAsync(context, node);
            if (conn == null) return;
            MusicPlayer player = await this.GetMusicPlayerAsync(context, conn);
            if (player == null) return;

            // Skip :)
            await _musicService.Skip(conn, player);

            // Respond
            await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder() {
                Content = "Alright, I've skipped this song."
            });
        }

        [SlashCommand("current", "Tells you the name of the current song")]
        [SlashRequireGuild]
        public async Task CurrentAsync(InteractionContext context) {
            LavalinkNodeConnection node = await this.GetNodeConnectionAsync(context);
            if (node == null) return;
            LavalinkGuildConnection conn = await this.GetGuildConnectionAsync(context, node);
            if (conn == null) return;

            // Get current song
            LavalinkTrack track = conn.CurrentState.CurrentTrack;

            // Respond
            await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder() {
                Content = $"The current song is **{track.Title}** by *{track.Author}* @ {(conn.CurrentState.PlaybackPosition.Hours != 0 ? conn.CurrentState.PlaybackPosition.Hours.ToString() + ":" : "")}{conn.CurrentState.PlaybackPosition:m\\:ss} / {(track.Length.Hours != 0 ? track.Length.Hours.ToString() + ":" : "")}{track.Length:m\\:ss}"
            });
        }

        [SlashCommand("pause", "Pauses the current song")]
        [SlashRequireGuild]
        public async Task PauseAsync(InteractionContext context) {
            LavalinkNodeConnection node = await this.GetNodeConnectionAsync(context);
            if (node == null) return;
            LavalinkGuildConnection conn = await this.GetGuildConnectionAsync(context, node);
            if (conn == null) return;

            // Pause
            await conn.PauseAsync();

            // Respond
            await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder() {
                Content = "Alright, I've paused the current song. Come back soon, yeah?"
            });
        }

        [SlashCommand("resume", "Resumes the current song")]
        [SlashRequireGuild]
        public async Task ResumeAsync(InteractionContext context) {
            LavalinkNodeConnection node = await this.GetNodeConnectionAsync(context);
            if (node == null) return;
            LavalinkGuildConnection conn = await this.GetGuildConnectionAsync(context, node);
            if (conn == null) return;

            // Pause
            await conn.ResumeAsync();

            // Respond
            await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder() {
                Content = "Welcome back! Your music is now playing again."
            });
        }

        [SlashCommand("queue", "Displays a list of songs using Divibot's pagination feature")]
        [SlashRequireGuild]
        public async Task QueueAsync(InteractionContext context, [Minimum(1)] [Maximum(int.MaxValue)] [Option("Page", "The page to view.")] long page = 1) {
            LavalinkNodeConnection node = await this.GetNodeConnectionAsync(context);
            if (node == null) return;
            LavalinkGuildConnection conn = await this.GetGuildConnectionAsync(context, node);
            if (conn == null) return;
            MusicPlayer player = await this.GetMusicPlayerAsync(context, conn);
            if (player == null) return;

            // Check queue length
            if (player.Queue.Count == 0) {
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder() {
                    Content = "Looks like there aren't any songs in the queue.",
                    IsEphemeral = true
                });
                return;
            }

            // Check for an invalid page
            if (page > (int)Math.Ceiling((double)player.Queue.Count / 10)) {
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder() {
                    Content = "I'm not sure how to navigate to that page? :thinking:",
                    IsEphemeral = true
                });
                return;
            }

            // Respond
            await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder() {
                Content = this.PrepareQueuePagination(player, (int) page)
            });
        }

        [SlashCommand("clear", "Clears the queue")]
        [SlashRequireGuild]
        public async Task ClearAsync(InteractionContext context) {
            LavalinkNodeConnection node = await this.GetNodeConnectionAsync(context);
            if (node == null) return;
            LavalinkGuildConnection conn = await this.GetGuildConnectionAsync(context, node);
            if (conn == null) return;
            MusicPlayer player = await this.GetMusicPlayerAsync(context, conn);
            if (player == null) return;

            // Clear queue
            player.Queue.Clear();

            // Respond
            await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder() {
                Content = "The queue has been cleared!"
            });
        }

        [SlashCommand("shuffle", "Shuffles the queue")]
        [SlashRequireGuild]
        public async Task ShuffleAsync(InteractionContext context) {
            LavalinkNodeConnection node = await this.GetNodeConnectionAsync(context);
            if (node == null) return;
            LavalinkGuildConnection conn = await this.GetGuildConnectionAsync(context, node);
            if (conn == null) return;
            MusicPlayer player = await this.GetMusicPlayerAsync(context, conn);
            if (player == null) return;

            // Check queue length
            if (player.Queue.Count == 0) {
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder() {
                    Content = "Looks like there aren't any songs in the queue to shuffle.",
                    IsEphemeral = true
                });
                return;
            }

            // Move to list
            List<LavalinkTrack> tracks = new List<LavalinkTrack>();
            tracks.AddRange(player.Queue);
            player.Queue.Clear();

            // Shuffle it
            tracks.Shuffle();

            // Re-add to queue
            foreach (LavalinkTrack track in tracks) {
                player.Queue.Enqueue(track);
            }

            // Respond with queue
            await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder() {
                Content = "Alright, the queue has been shuffled.\n\n" + this.PrepareQueuePagination(player, 1)
            });
        }

        // Fetches the node connection
        private async Task<LavalinkNodeConnection> GetNodeConnectionAsync(InteractionContext context) {
            LavalinkExtension lavalink = context.Client.GetLavalink();
            LavalinkNodeConnection node = lavalink.GetIdealNodeConnection();
            if (node == null) {
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder() {
                    Content = "Uh-oh, looks like something went wrong internally. If this happens again, make sure to let us know!",
                    IsEphemeral = true
                });
            }
            return node;
        }

        [SlashCommand("dequeue", "Removes a song from the queue")]
        [SlashRequireGuild]
        public async Task DequeueAsync(InteractionContext context, [Minimum(1)] [Maximum(int.MaxValue)] [Option("Number", "The position of the song to remove from the queue")] long position) {
            LavalinkNodeConnection node = await this.GetNodeConnectionAsync(context);
            if (node == null) return;
            LavalinkGuildConnection conn = await this.GetGuildConnectionAsync(context, node);
            if (conn == null) return;
            MusicPlayer player = await this.GetMusicPlayerAsync(context, conn);
            if (player == null) return;

            // Attempt to find track
            LavalinkTrack track = player.Queue.Where((t, i) => i + 1 == position).FirstOrDefault();
            if (track == null) {
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder() {
                    Content = "Sorry, I wasn't able to find that track in the queue :confused:",
                    IsEphemeral = true
                });
                return;
            }

            // Move to list
            List<LavalinkTrack> tracks = new List<LavalinkTrack>();
            tracks.AddRange(player.Queue);
            player.Queue.Clear();

            // Remove item
            tracks.RemoveAt((int) position - 1);

            // Re-add to queue
            foreach (LavalinkTrack trk in tracks) {
                player.Queue.Enqueue(trk);
            }

            // Respond
            await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder() {
                Content = $"I've removed **{track.Title}** by *{track.Author}* from the queue"
            });
        }

        [SlashCommand("move", "Moves songs around in the queue")]
        [SlashRequireGuild]
        public async Task MoveAsync(InteractionContext context, [Minimum(1)] [Maximum(int.MaxValue)] [Option("From", "The position of the song to move from")] long from, [Minimum(1)] [Maximum(int.MaxValue)] [Option("To", "The position of the song to move to")] long to) {
            LavalinkNodeConnection node = await this.GetNodeConnectionAsync(context);
            if (node == null) return;
            LavalinkGuildConnection conn = await this.GetGuildConnectionAsync(context, node);
            if (conn == null) return;
            MusicPlayer player = await this.GetMusicPlayerAsync(context, conn);
            if (player == null) return;

            // Attempt to find track
            LavalinkTrack track = player.Queue.Where((t, i) => i + 1 == from).FirstOrDefault();
            if (track == null) {
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder() {
                    Content = "Sorry, I wasn't able to find that track in the queue :confused:",
                    IsEphemeral = true
                });
                return;
            }

            // Move to list
            List<LavalinkTrack> tracks = new List<LavalinkTrack>();
            tracks.AddRange(player.Queue);
            player.Queue.Clear();

            // Move item
            tracks.RemoveAt((int)from - 1);
            tracks.Insert((int)to - 1, track);

            // Re-add to queue
            foreach (LavalinkTrack trk in tracks) {
                player.Queue.Enqueue(trk);
            }

            // Respond
            await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder() {
                Content = $"Alright, I've moved **{track.Title}** by *{track.Author}* from position {from} to position {to}\n\n" + this.PrepareQueuePagination(player, 1)
            });
        }

        [SlashCommand("repeat", "Repeats the current song")]
        [SlashRequireGuild]
        public async Task RepeatAsync(InteractionContext context, [Choice("start", "start")][Choice("end", "end")][Option("Choice", "Whether to enable or disable repeating")] string choice) {
            LavalinkNodeConnection node = await this.GetNodeConnectionAsync(context);
            if (node == null) return;
            LavalinkGuildConnection conn = await this.GetGuildConnectionAsync(context, node);
            if (conn == null) return;
            MusicPlayer player = await this.GetMusicPlayerAsync(context, conn);
            if (player == null) return;

            // Set repeat
            if (choice == "start") {
                player.Repeat = true;

                // Respond
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder() {
                    Content = "Okay, I'll keep repeating this song after it ends until you tell me to stop! :grin:"
                });
            } else if (choice == "end") {
                player.Repeat = false;

                // Respond
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder() {
                    Content = "Gettin' sick of it? Alright, I'll stop repeating this song after it ends."
                });
            }
        }

        [SlashCommand("lyrics", "Sends the lyrics for the current song")]
        public async Task LyricsAsync(InteractionContext context, [Minimum(1)] [Maximum(int.MaxValue)] [Option("Page", "The page to view.")] long page = 1) {
            // Acknowledge
            await context.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            LavalinkNodeConnection node = await this.GetNodeConnectionAsync(context);
            if (node == null) return;
            LavalinkGuildConnection conn = await this.GetGuildConnectionAsync(context, node);
            if (conn == null) return;

            // Get current song
            LavalinkTrack track = conn.CurrentState.CurrentTrack;

            // Find suggestions
            HttpResponseMessage httpSuggestResponse = await HttpClient.GetAsync($"https://api.lyrics.ovh/suggest/{HttpUtility.UrlEncode(track.Title.ToLower().Replace("official", "").Replace("video", "").Replace("-", "").Replace("(", "").Replace(")", "").Replace("lyrics", "").Replace("lyric", "").Replace("kareoke", "") + " " + track.Author.ToLower().Replace("topic", "").Replace("official", "").Replace("-", "").Replace("(", "").Replace(")", ""))}");
            if (!httpSuggestResponse.IsSuccessStatusCode) {
                await context.EditResponseAsync(new DiscordWebhookBuilder() {
                    Content = $"I was unable to find lyrics for **{track.Title}** by *{track.Author}*"
                });
                return;
            }
            string suggestResponse = httpSuggestResponse.Content.ReadAsStringAsync().Result;
            JsonDocument suggestResponseObj = JsonDocument.Parse(suggestResponse);
            if (suggestResponseObj == null) {
                await context.EditResponseAsync(new DiscordWebhookBuilder() {
                    Content = $"I was unable to find lyrics for **{track.Title}** by *{track.Author}*"
                });
                return;
            }
            JsonElement suggestResponseFirstObj = suggestResponseObj.RootElement.GetProperty("data");
            if (suggestResponseFirstObj.GetArrayLength() == 0) {
                await context.EditResponseAsync(new DiscordWebhookBuilder() {
                    Content = $"I was unable to find lyrics for **{track.Title}** by *{track.Author}*"
                });
                return;
            }
            JsonElement? suggestResponseFirstInArr = suggestResponseFirstObj.EnumerateArray().First();
            if (suggestResponseFirstInArr == null) {
                await context.EditResponseAsync(new DiscordWebhookBuilder() {
                    Content = $"I was unable to find lyrics for **{track.Title}** by *{track.Author}*"
                });
                return;
            }
            JsonElement artist = suggestResponseFirstInArr.Value.GetProperty("artist").GetProperty("name");
            JsonElement title = suggestResponseFirstInArr.Value.GetProperty("title");

            // Get API response
            HttpResponseMessage httpResponseMessage = await HttpClient.GetAsync($"https://api.lyrics.ovh/v1/{artist}/{title}");
            if (!httpResponseMessage.IsSuccessStatusCode) {
                await context.EditResponseAsync(new DiscordWebhookBuilder() {
                    Content = $"I was unable to find lyrics for **{track.Title}** by *{track.Author}*"
                });
                return;
            }

            string response = httpResponseMessage.Content.ReadAsStringAsync().Result;
            if (string.IsNullOrEmpty(response)) {
                await context.EditResponseAsync(new DiscordWebhookBuilder() {
                    Content = $"There was an error processing your request."
                });
                return;
            }

            JsonDocument responseObj = JsonDocument.Parse(response);
            if (responseObj.RootElement.TryGetProperty("lyrics", out JsonElement lyricsProp)) {
                string lyrics = lyricsProp.GetString();
                if (lyrics == null) {
                    await context.EditResponseAsync(new DiscordWebhookBuilder() {
                        Content = $"There was an error processing your request."
                    });
                    return;
                }
                lyrics = lyrics.Replace("\\n", "\n").Replace("\\r", "");

                // Check for an invalid page
                if (page > (int)Math.Ceiling((double)(lyrics.Count(c => c == '\n') + 1) / 10)) {
                    await context.EditResponseAsync(new DiscordWebhookBuilder() {
                        Content = "I'm not sure how to navigate to that page? :thinking:"
                    });
                    return;
                }
                
                // Respond
                await context.EditResponseAsync(new DiscordWebhookBuilder() {
                    Content = $"Here's the lyrics for **{track.Title}** by *{track.Author}*:\n\n{this.PrepareLyricsPagination(lyrics, (int) page)}"
                });
            } else {
                await context.EditResponseAsync(new DiscordWebhookBuilder() {
                    Content = $"There was an error processing your request."
                });
            }
        }

        // Fetches a guild connection
        private async Task<LavalinkGuildConnection?> GetGuildConnectionAsync(InteractionContext context, LavalinkNodeConnection node) {
            if (!node.ConnectedGuilds.TryGetValue(context.Guild.Id, out LavalinkGuildConnection conn)) {
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder() {
                    Content = "Umm, looks like there's no music playing in here? :sweat_smile:",
                    IsEphemeral = true
                });
                return null;
            } else {
                return conn;
            }
        }
        
        // Fetches a music player
        private async Task<MusicPlayer?> GetMusicPlayerAsync(InteractionContext context, LavalinkGuildConnection conn) {
            if (!_musicService.Players.TryGetValue(conn.Guild.Id, out MusicPlayer player)) {
                await player.TextChannel.SendMessageAsync(new DiscordMessageBuilder() {
                    Content = "There was an internal error trying to play music. Try stopping then trying again."
                });
                return null;
            } else {
                return player;
            }
        }

        // Prepare queue string
        private string PrepareQueuePagination(MusicPlayer player, int page) {
            string[] lines = player.Queue.Select((x, i) => {
                return $"{i + 1}. **{x.Title}** by *{x.Author}*";
            }).ToArray();
            string output = Divibot.Pagination(lines, (int)page - 1);
            return $"{output}\nPage {page}/{(int)Math.Ceiling((double)player.Queue.Count / 10)}";
        }
        
        // Prepare lyrics string
        private string PrepareLyricsPagination(string lyrics, int page) {
            string[] lines = lyrics.Split("\n").ToArray();
            string output = Divibot.Pagination(lines, (int)page - 1);
            return $"{output}\nPage {page}/{(int)Math.Ceiling((double)(lyrics.Count(c => c == '\n') + 1) / 10)}";
        }

    }

}
