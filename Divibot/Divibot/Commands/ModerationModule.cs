using Divibot.Database;
using Divibot.Database.Entities;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TimeSpanParserUtil;

namespace Divibot.Commands {

    public class ModerationModule : ApplicationCommandModule {

        // Dependency Injection
        private DivibotDbContext _dbContext;

        // Constructor
        public ModerationModule(DivibotDbContext dbContext) {
            _dbContext = dbContext;
        }

        [SlashCommand("purge", "Purges the given amount of messages up to 2 weeks old.")]
        [SlashRequireGuild]
        [SlashRequireUserPermissions(Permissions.ManageMessages)]
        [SlashRequireBotPermissions(Permissions.ManageMessages)]
        public async Task PurgeAsync(InteractionContext context, [Option("amount", "The amount of messages to attempt to purge.")] [Minimum(1)] [Maximum(int.MaxValue)] long amountLong) {
            // Acknowledge
            await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder() {
                Content = "Please wait while I fetch the messages..."
            });

            // Calculate amount of 'pages'
            int amount = Convert.ToInt32(amountLong) + 1;
            int pages = (int) Math.Floor((double) amount / 100);

            // Fetch all messages
            List<DiscordMessage> messages = new List<DiscordMessage>();

            // If there's more, add more
            bool tooOld = false;
            for (int i = 0; i < pages; i++) {
                IReadOnlyList<DiscordMessage> loopMessages;
                if (messages.Count == 0) {
                    loopMessages = await context.Channel.GetMessagesAsync(100);
                } else {
                    loopMessages = await context.Channel.GetMessagesBeforeAsync(messages.Last().Id, 100);
                }
                if (loopMessages.Count == 0) {
                    break;
                } else if (loopMessages.Last().Timestamp < DateTime.Now.Subtract(TimeSpan.FromDays(14))) {
                    loopMessages = loopMessages.Where(m => m.Timestamp >= DateTime.Now.Subtract(TimeSpan.FromDays(14))).ToList();
                    messages.AddRange(loopMessages);
                    tooOld = true;
                    break;
                }
                messages.AddRange(loopMessages);

                // Give a status update, because why not
                await context.EditResponseAsync(new DiscordWebhookBuilder() {
                    Content = $"Please wait while I fetch the messages... (Found {messages.Count} messages)"
                });

                // Delay
                await Task.Delay(1000);
            }

            // Get the remaining amount, if there is any
            if (amount % 100 != 0 && !tooOld) {
                IReadOnlyList<DiscordMessage> remainingMessages;
                if (messages.Count == 0) {
                    remainingMessages = await context.Channel.GetMessagesAsync(amount % 100);
                } else {
                    remainingMessages = await context.Channel.GetMessagesBeforeAsync(messages.Last().Id, (amount % 100));
                }
                remainingMessages = remainingMessages.Where(m => m.Timestamp >= DateTime.Now.Subtract(TimeSpan.FromDays(14))).ToList();
                messages.AddRange(remainingMessages);
            }

            // Handle no messages found (how?)
            if (messages.Count == 0) {
                await context.EditResponseAsync(new DiscordWebhookBuilder() {
                    Content = "Sorry, I wasn't able to find any messages I can delete. As a reminder, I cannot delete messages more than 2 weeks old."
                });

                // Wait
                await Task.Delay(5000);

                try {
                    // Delete response
                    await context.DeleteResponseAsync();
                } catch (Exception) {
                    // Ok.
                }

                // Return
                return;
            }

            // Remove first message, as that's the reply
            messages.RemoveAt(0);

            // Get the total count of messages
            context.Client.Logger.LogInformation($"Total message count: {messages.Count}");

            // Confirm
            DiscordMessage lastMessage = messages.Last();
            DiscordComponent[] components = new DiscordComponent[] {
                new DiscordButtonComponent(ButtonStyle.Danger, "moderation_purge_confirm", "Confirm"),
                new DiscordButtonComponent(ButtonStyle.Primary, "moderation_purge_cancel", "Cancel"),
            };
            await context.EditResponseAsync(new DiscordWebhookBuilder() {
                Content = $"A link to the last message I found that I can delete is below. All of the messages after this message will be deleted. Confirm?\n\n" +
                          $"{lastMessage.JumpLink}"
            }.AddComponents(components));
            DiscordMessage responseMessage = await context.GetOriginalResponseAsync();

            // Wait for response
            var response = await responseMessage.WaitForButtonAsync(context.User);

            // Handle timeout
            if (response.TimedOut) {
                await AttackModule.InteractionTimedOut(context.Interaction, responseMessage.Id, components);
                return;
            }

            // Handle cancel
            if (response.Result.Id == "moderation_purge_cancel") {
                await context.EditResponseAsync(new DiscordWebhookBuilder() {
                    Content = "Whew, alright! No worries. That would've been close."
                });

                // Wait
                await Task.Delay(5000);

                try {
                    // Delete response
                    await context.DeleteResponseAsync();
                } catch (Exception) {
                    // Ok.
                }

                // Return
                return;
            }

            // In-progress response
            await context.EditResponseAsync(new DiscordWebhookBuilder() {
                Content = "Alright, I'll start removing these messages! Please wait, as this can take a long time if there are a lot of messages."
            });

            // Delete all messages
            int messageCount = messages.Count;
            int messagesPages = (int) Math.Floor((double) messages.Count / 100);
            int messagesModulus = messages.Count % 100;
            for (int i = 0; i < messagesPages; i++) {
                await context.Channel.DeleteMessagesAsync(messages.Take(100), $"Purge requested by {context.User.Mention}");
                await Task.Delay(5000);
            }

            // Delete remaining amount
            if (messagesModulus != 0) {
                await context.Channel.DeleteMessagesAsync(messages, $"Purge requested by {context.User.Mention}");
            }

            // Final response
            await context.EditResponseAsync(new DiscordWebhookBuilder() {
                Content = $"All done! I've deleted {messageCount} message{(messageCount != 0 ? "s" : "")}."
            });

            // Wait
            await Task.Delay(5000);

            try {
                // Delete response
                await context.DeleteResponseAsync();
            } catch (Exception) {
                // Ok.
            }
        }

        [SlashCommand("yeet", "Removes the given user's permission to chat in the channel the command was run in.")]
        [SlashRequireGuild]
        [SlashRequireUserPermissions(Permissions.ManageChannels)]
        [SlashRequireBotPermissions(Permissions.ManageChannels)]
        public async Task YeetAsync(InteractionContext context, [Option("user", "The user you want to yeet out of the channel.")] DiscordUser user, [Option("time", "The amount of time they should be yeeted for, if temporary. Defaults to infinity.")] string time = "forever") {
            // Get member
            DiscordMember member = user as DiscordMember;

            // Check for self
            if (member.Id == context.User.Id) {
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder() {
                    Content = "Sorry, you can't yeet yourself. Why would you want to, anyways? :confused:",
                    IsEphemeral = true
                });
                return;
            }

            // Check for the requested user's permission
            if (member.Hierarchy > context.Member.Hierarchy) {
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder() {
                    Content = "Sorry, you can't yeet a user whose role is higher than yours.",
                    IsEphemeral = true
                });
            }

            // Check for an existing overwrite
            DiscordOverwrite overwrite = await GetExistingMemberOverwrite(context.Channel, user.Id);
            if (overwrite != null && overwrite.Denied.HasFlag(Permissions.SendMessages)) {
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder() {
                    Content = "Sorry, looks like this user is already yeeted from this channel. If you want to change their time, try un-yeeting them first!",
                    IsEphemeral = true
                });
                return;
            }

            // if the time is not "forever", then we need to add the user to the database
            TimeSpan timeSpan = TimeSpan.Zero;
            if (time != "forever") {
                // Parse duration
                bool timeParseSuccess = TimeSpanParser.TryParse(time, out timeSpan);
                if (!timeParseSuccess) {
                    await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder() {
                        Content = "Sorry, it seems I wasn't quite able to understand the provided timestamp. Try formatting it differently.",
                        IsEphemeral = true
                    });
                    return;
                }

                // Validate time
                if (timeSpan > TimeSpan.FromDays(60)) {
                    await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder() {
                        Content = "Sorry, I only allow temporarily yeeting people for up to 60 days.",
                        IsEphemeral = true
                    });
                    return;
                } else if (timeSpan < TimeSpan.FromMinutes(1)) {
                    await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder() {
                        Content = "Sorry, I only allow temporarily yeeting people for no less than 1 minute.",
                        IsEphemeral = true
                    });
                    return;
                }

                // Remove any existing values
                await _dbContext.Database.ExecuteSqlRawAsync($"DELETE FROM yeetedusers WHERE GuildId = '{context.Guild.Id}' AND ChannelId = '{context.Channel.Id}' AND UserId = '{user.Id}'");
                _dbContext.ChangeTracker.Clear();

                // Add new value
                await _dbContext.YeetedUsers.AddAsync(new EntityYeetedUser() {
                    GuildId = context.Guild.Id,
                    ChannelId = context.Channel.Id,
                    UserId = user.Id,
                    ExpirationDate = DateTime.Now.Add(timeSpan)
                });

                // Save database
                await _dbContext.SaveChangesAsync();
            }

            // If an overwrite already exists, just modify it
            // Otherwise, add a new one
            if (overwrite != null) {
                await overwrite.UpdateAsync(
                    deny: overwrite.Denied | Permissions.SendMessages | Permissions.AddReactions | Permissions.CreatePublicThreads | Permissions.CreatePrivateThreads,
                    reason: $"User {context.User.Username}#{context.User.Discriminator} ({context.User.Id}) requested a yeetage of {user.Username}#{user.Discriminator} ({user.Id})"
                );
            } else {
                await context.Channel.AddOverwriteAsync(
                    member,
                    deny: Permissions.SendMessages | Permissions.AddReactions | Permissions.CreatePublicThreads | Permissions.CreatePrivateThreads,
                    reason: $"User {context.User.Username}#{context.User.Discriminator} ({context.User.Id}) requested a yeetage of {user.Username}#{user.Discriminator} ({user.Id})"
                );
            }

            // Send message
            if (time != "forever") {
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder() {
                    Content = $"Alright, I've temporarily yeeted {user.Mention} out of this channel for {timeSpan}" // TODO: humanize
                });
            } else {
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder() {
                    Content = $"Alright, I've yeeted {user.Mention} out of this channel."
                });
            }
        }

        [SlashCommand("unyeet", "Re-allows a user to have permission to chat in the channel the command was run in.")]
        [SlashRequireGuild]
        [SlashRequireUserPermissions(Permissions.ManageChannels)]
        [SlashRequireBotPermissions(Permissions.ManageChannels)]
        public async Task UnyeetUserAsync(InteractionContext context, [Option("user", "The user you wish to unyeet from the channel.")] DiscordUser user) {
            // Get member
            DiscordMember member = user as DiscordMember;

            // Pass to method
            bool success = await UnyeetUser(context.Channel, member.Id, $"User {context.User.Username}#{context.User.Discriminator} ({context.User.Id}) requested an un-yeetage of {user.Username}#{user.Discriminator} ({user.Id})");
            if (!success) {
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder() {
                    Content = $"Sorry, looks like I wasn't able to find any permission overwrites for {user.Mention} that denies the 'Send Messages' permission.",
                    IsEphemeral = true
                });
                return;
            }

            // Remove existing database values, if any
            await _dbContext.Database.ExecuteSqlRawAsync($"DELETE FROM yeetedusers WHERE GuildId = '{context.Guild.Id}' AND ChannelId = '{context.Channel.Id}' AND UserId = '{user.Id}'");

            // Respond
            await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder() {
                Content = $"Alright, {user.Mention} now has permission to speak in this channel once more!"
            });
        }

        /// <summary>
        /// Fetches an existing Discord member's overwrite, if it exists, from the given channel, otherwise returns null.
        /// </summary>
        public static async Task<DiscordOverwrite> GetExistingMemberOverwrite(DiscordChannel channel, ulong userId) {
            // Check channel overrides
            DiscordOverwrite overwrite = null;
            foreach (DiscordOverwrite ow in channel.PermissionOverwrites) {
                if (ow.Type == OverwriteType.Member && (await ow.GetMemberAsync()).Id == userId) {
                    overwrite = ow;
                    break;
                }
            }
            return overwrite;
        }

        /// <summary>
        /// Fetches an existing Discord member's overwrite, if it exists, from the given channel, otherwise returns null.
        /// </summary>
        public static async Task<DiscordOverwrite> GetExistingRoleOverwrite(DiscordChannel channel, ulong roleId) {
            // Check channel overrides
            DiscordOverwrite overwrite = null;
            foreach (DiscordOverwrite ow in channel.PermissionOverwrites) {
                if (ow.Type == OverwriteType.Role && (await ow.GetRoleAsync()).Id == roleId) {
                    overwrite = ow;
                    break;
                }
            }
            return overwrite;
        }

        /// <summary>
        /// Unyeet's a user from the given channel.
        /// </summary>
        public static async Task<bool> UnyeetUser(DiscordChannel channel, ulong userId, string reason) {
            // Check channel overrides
            DiscordOverwrite overwrite = await GetExistingMemberOverwrite(channel, userId);
            if (overwrite == null || !overwrite.Denied.HasFlag(Permissions.SendMessages)) {
                return false;
            }

            // Make permission modifications
            Permissions newDeniedPermissions = overwrite.Denied & ~(Permissions.SendMessages | Permissions.AddReactions | Permissions.CreatePublicThreads | Permissions.CreatePrivateThreads);

            // If there's no more permission modifications, remove the override
            // Otherwise, only update it
            if (newDeniedPermissions == 0 && overwrite.Allowed == 0) {
                // Remove overwrite
                await overwrite.DeleteAsync(reason);
            } else {
                // Update overwrite
                await overwrite.UpdateAsync(
                    deny: newDeniedPermissions,
                    reason: reason
                );
            }

            // Everything's a success
            return true;
        }
        public static async Task UnyeetUser(DiscordClient client, ulong guildId, ulong channelId, ulong userId) {
            // Fetch guild
            DiscordGuild guild = await client.GetGuildAsync(guildId);

            // Fetch channel
            DiscordChannel channel = guild.GetChannel(channelId);

            // Fetch member
            DiscordMember member = await guild.GetMemberAsync(userId);

            // Pass to method
            await UnyeetUser(channel, member.Id, $"User {member.Username}#{member.Discriminator} ({member.Id}) was automatically un-yeeted since their time has ended.");
        }

        [SlashCommand("lockdown", "Removes everyone's ability to chat in the channel the command was run in.")]
        [SlashRequireGuild]
        [SlashRequireUserPermissions(Permissions.ManageChannels)]
        [SlashRequireBotPermissions(Permissions.ManageChannels)]
        public async Task LockdownAsync(InteractionContext context, [Option("time", "The amount of time this lockdown should last, if temporary.")] string time = "forever") {
            // TODO: Complete.
        }

        [SlashCommand("cleardms", "Deletes all of the bot's messages in your DMs with it.")]
        [SlashRequireDirectMessage]
        public async Task ClearDMsAsync(InteractionContext context) {
            await context.Interaction.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            // Let's do this
            bool keepGoing = true;
            do {
                // Fetch messages
                IReadOnlyList<DiscordMessage> messages = await context.Channel.GetMessagesAsync();
                IReadOnlyList<DiscordMessage> botMessages = messages.Where(m => m.Author.Id == context.Client.CurrentUser.Id).ToList();
                if (botMessages.Count() == 0) {
                    keepGoing = false;
                }

                // Delete messages
                foreach (DiscordMessage message in botMessages) {
                    await message.DeleteAsync();
                    await Task.Delay(1000);
                }
            } while (keepGoing);
        }

    }

}
