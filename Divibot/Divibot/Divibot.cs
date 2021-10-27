using Divibot.Commands;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using DSharpPlus.SlashCommands.EventArgs;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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

            // Register modules
            ulong debugGuild = ulong.Parse(Environment.GetEnvironmentVariable("DebugGuild"));
#if DEBUG
            Commands.RegisterCommands<GeneralModule>(debugGuild);
            Commands.RegisterCommands<InfoModule>(debugGuild);
            Commands.RegisterCommands<EncodeModule>(debugGuild);
            Commands.RegisterCommands<DecodeModule>(debugGuild);
#else
            Commands.RegisterCommands<GeneralModule>();
            Commands.RegisterCommands<InfoModule>();
            Commands.RegisterCommands<EncodeModule>();
            Commands.RegisterCommands<DecodeModule>();
#endif
            Commands.RegisterCommands<OwnerModule>(debugGuild);

            // Handle errored slash commands
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
                    } else if (check is SlashRequireUserPermissionsAttribute) {
                        SlashRequireUserPermissionsAttribute userPermAttribute = check as SlashRequireUserPermissionsAttribute;
                        if (evt.Context.Member != null && evt.Context.Member.Permissions.HasPermission(Permissions.ManageRoles)) {
                            content = $"Sorry, but you do not have enough permissions to run this command. You're missing one of the following: {userPermAttribute.Permissions.ToPermissionString()}";
                        } else {
                            content = $"Sorry, but you do not have enough permissions to run this command.";
                        }
                        found = true;
                    } else if (check is SlashRequireBotPermissionsAttribute) {
                        SlashRequireBotPermissionsAttribute botPermAttribute = check as SlashRequireBotPermissionsAttribute;
                        if (evt.Context.Member != null && evt.Context.Member.Permissions.HasPermission(Permissions.ManageRoles)) {
                            content = $"It seems as though I don't have enough permissions to run this command. I'm missing one of the following: {botPermAttribute.Permissions.ToPermissionString()}";
                        } else {
                            content = $"It seems as though I don't have enough permissions to run this command.";
                        }
                        found = true;
                    } else if (check is SlashRequireOwnerAttribute) {
                        content = $"Sorry, only the owner of the bot can run this command.";
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

        // Divibot's definitely exclusive and totally amazing pagination feature
        public static string Pagination(string[] lines, int page = 0) {
            int pages = (int) Math.Ceiling((double) lines.Length / 10);
            string output = "";
            if (pages == 1) {
                foreach (string line in lines) {
                    output += line + '\n';
                }
            } else if (pages > 1) {
                if (page == 0) {
                    for (int i = 0; i < 10; i++) {
                        output += lines[i] + '\n';
                    }
                } else if (page == pages - 1) {
                    for (int i = page * 10; i < lines.Length; i++) {
                        output += lines[i] + '\n';
                    }
                } else {
                    for (int i = page * 10; i < (page * 10) + 10; i++) {
                        output += lines[i] + '\n';
                    }
                }
            }
            return output;
        }

        // Divibot's not-as-amazing-as-pagination-but-still-totally amazing comma-separated list creation feature
        public static string CreateCommaList(List<string> items, string combiningWord) {
            if (items.Count == 0) {
                return "N/A";
            } else if (items.Count == 1) {
                return items.First();
            } else if (items.Count == 2) {
                return $"{items.First()} {combiningWord} {items.Last()}";
            } else {
                return string.Join(", ", items.ToArray(), 0, items.Count - 1) + " " + combiningWord + " " + items.Last();
            }
        }

    }

}
