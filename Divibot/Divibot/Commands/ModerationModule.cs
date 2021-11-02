using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Divibot.Commands {

    public class ModerationModule : ApplicationCommandModule {

        [SlashCommand("purge", "Purges the given amount of messages up to 2 weeks old.")]
        [SlashRequireGuild]
        [SlashRequireUserPermissions(Permissions.ManageMessages)]
        [SlashRequireBotPermissions(Permissions.ManageMessages)]
        public async Task PurgeAsync(InteractionContext context, [Option("amount", "The amount of messages to attempt to purge.")] [Maximum(int.MaxValue)] long amount) {
            // Acknowledge
            await context.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            // The acknowledgement will be deleted. Responses must be followups.
            amount += 1;

            // Manage multiple groups
            int totalMessageCount = 0;
            int count = (int) Math.Floor((double) amount / 100);
            if (count == 1) {
                IReadOnlyList<DiscordMessage> messages = await context.Channel.GetMessagesAsync((int) amount);
                totalMessageCount += messages.Count;
                await context.Channel.DeleteMessagesAsync(messages, $"Purge requested by {context.User.Mention}");
            } else {
                for (int i = 0; i < count - 1; i++) {
                    IReadOnlyList<DiscordMessage> loopMessages = await context.Channel.GetMessagesAsync(100);
                    totalMessageCount += loopMessages.Count;
                    if (loopMessages.Count == 0) {
                        break;
                    }
                    await context.Channel.DeleteMessagesAsync(loopMessages, $"Purge requested by {context.User.Mention}");
                    await Task.Delay(5000);
                }
                IReadOnlyList<DiscordMessage> messages = await context.Channel.GetMessagesAsync((int) (amount % 100));
                totalMessageCount += messages.Count;
                if (messages.Count > 0) {
                    await context.Channel.DeleteMessagesAsync(messages, $"Purge requested by {context.User.Mention}");
                }
            }

            // Respond
            DiscordMessage response = await context.Channel.SendMessageAsync($"Successfully deleted {(totalMessageCount - 1)} message{((totalMessageCount - 1) != 1 ? "s" : "")}.");

            // Wait
            await Task.Delay(5000);

            // Delete
            await response.DeleteAsync();
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
