using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Divibot.Commands {

    public class GeneralModule : ApplicationCommandModule {

        [SlashCommand("test", "Checks to make sure the bot is up and running. If you don't get a response, it's probably not!")]
        public async Task TestAsync(InteractionContext context) {
            await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder() {
                Content = $"Looks like everything's up and running over here! Thanks for checking in on me, {(context.Guild != null && !string.IsNullOrEmpty(context.Member.Nickname) ? context.Member.Nickname : context.User.Username)} :blush:"
            });
        }

        [SlashCommand("ping", "Determines the amount of time it takes the bot to respond to commands.")]
        public async Task PingAsync(InteractionContext context, [Option("message", "An optional message. Include {time} to make it fully custom.")] string message = default) {
            // Build response
            string response;
            if (message != null) {
                if (message.IndexOf("{time}") == -1) {
                    response = message + " Pong! Took {time}!";
                } else {
                    response = message;
                }
            } else {
                response = "Pong! Took {time}!";
            }

            // Start stopwatch
            Stopwatch sw = new Stopwatch();
            sw.Start();

            // Acknowledge interaction
            await context.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            // Stop stopwatch
            sw.Stop();

            // Respond with a message
            await context.EditResponseAsync(new DiscordWebhookBuilder() {
                Content = response.Replace("{time}", $"`{sw.ElapsedMilliseconds}ms`")
            });
        }

        [SlashCommand("servers", "Tells you the number of servers that the bot is in.")]
        public async Task ServersAsync(InteractionContext context) {
            await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder() {
                Content = $"Last I checked, I was in {context.Client.Guilds.Count} server{(context.Client.Guilds.Count != 1 ? "s" : "")}."
            });
        }

        [SlashCommand("members", "Tells you the number of members in the server the command was run in.")]
        public async Task MembersAsync(InteractionContext context) {
            // Check for DMs
            if (context.Guild == null) {
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder() {
                    Content = "Looks like it's just you and me in here! :slight_smile:"
                });
            } else {
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder() {
                    Content = $"I think there's about {context.Guild.MemberCount} members in {context.Guild.Name}"
                });
            }
        }

        [SlashCommand("support", "Provides an invite to Divibot's home server to ask for support.")]
        public async Task SupportAsync(InteractionContext context) {
            await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder() {
                Content = "For support with Divibot, please join this server and ask for help in the #support channel :slight_smile: https://discord.gg/0xxkiR1rO4zRsYLp",
                IsEphemeral = true
            });
        }

        [SlashCommand("uptime", "Tells you how long Divibot has been up and running since its last restart.")]
        public async Task UptimeAsync(InteractionContext context) {
            // Generate time string
            TimeSpan uptime = Divibot.Uptime.Elapsed;
            string formatString = "";
            if (uptime.Days > 0) {
                formatString += $"d' day{(uptime.Days != 1 ? "s" : "")}'";
            }
            if (uptime.Days > 0 || uptime.Hours > 0) {
                formatString += $"h' hour{(uptime.Hours != 1 ? "s" : "")}'";
            }
            if (uptime.Days > 0 || uptime.Hours > 0 || uptime.Minutes > 0) {
                formatString += $"m' minute{(uptime.Minutes != 1 ? "s" : "")} and '";
            }
            formatString = (formatString + $"s' second{(uptime.Seconds != 1 ? "s" : "")}'").Trim();
            string time = uptime.ToString(formatString);

            // Respond
            await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder() {
                Content = $"I've been online for {time}!"
            });
        }
    }

}
