using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using DSharpPlus.SlashCommands.EventArgs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Divibot {

    public static class Divibot {

        // Settings
        public static string Prefix { get; } = Environment.GetEnvironmentVariable("Prefix");
        public static Stopwatch Uptime { get; } = new Stopwatch();

        // Properties
        public static DiscordClient Client { get; private set; }
        public static SlashCommandsExtension Commands { get; private set; }
        public static ServiceProvider Services { get; private set; }

        // Main
        public static async Task Main(string[] args) {
            // Dependency injection
            Services = new ServiceCollection()
                .AddSingleton(impl => {
                    return new DiscordClient(new DiscordConfiguration() {
                        Token = Environment.GetEnvironmentVariable("BotToken"),
                        Intents = DiscordIntents.Guilds |
                                  DiscordIntents.GuildMembers |
                                  DiscordIntents.GuildPresences |
                                  DiscordIntents.GuildMessages
                    });
                })
                .AddSingleton<Random>()
                .BuildServiceProvider();

            // Create client
            Client = Services.GetRequiredService<DiscordClient>();

            // Add slash commands
            Commands = Client.UseSlashCommands(new SlashCommandsConfiguration() {
                Services = Services
            });
#if DEBUG
            Commands.RegisterCommands(Assembly.GetExecutingAssembly(), ulong.Parse(Environment.GetEnvironmentVariable("DebugGuild")));
#else
            Commands.RegisterCommands(Assembly.GetExecutingAssembly());
#endif
            Commands.SlashCommandErrored += OnSlashCommandErrored;

            // Start uptime
            Uptime.Start();

            // Connect
            await Client.ConnectAsync();

            // Don't close immediately
            await Task.Delay(-1);
        }

        // Handle slash command errors
        private static async Task OnSlashCommandErrored(SlashCommandsExtension ext, SlashCommandErrorEventArgs evt) {
            string content = "";

            if (evt.Exception is SlashExecutionChecksFailedException) {
                IReadOnlyList<SlashCheckBaseAttribute> failedChecks = (evt.Exception as SlashExecutionChecksFailedException).FailedChecks;
                bool found = false;
                foreach (SlashCheckBaseAttribute check in failedChecks) {
                    if (check is SlashRequireGuildAttribute) {
                        content = "Sorry, but this command only works inside of servers.";
                        found = true;
                    } else if (check is SlashRequireDirectMessageAttribute) {
                        content = "Sorry, but this command only works inside of DMs.";
                        found = true;
                    }
                    if (found) {
                        break;
                    }
                }
                if (!found) {
                    // Handle unknown checks
                    content = $"There was an internal error trying to run this command. Please try again, or if the error continues occuring, contact the bot developer with the following information:\n```\n" +
                              $"A check failed, but I'm not quite sure which one. See failed checks {string.Join(",", failedChecks.Select(c => c.GetType().Name).ToArray())}\n```";
                }
            } else {
                content = $"There was an internal error trying to run this command. Please try again, or if the error continues occuring, contact the bot developer with the following information:\n```\n{evt.Exception.Message}\n```";
            }

            // Respond
            await evt.Context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder() {
                Content = content,
                IsEphemeral = true
            });
        }

    }

}
