

using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Divibot.Commands {

    [SlashCommandGroup("info", "Various commands to provide information about different things.")]
    public class InfoModule : ApplicationCommandModule {

        [SlashCommand("user", "Provides information about yourself or another user or bot.")]
        public async Task UserInfoAsync(InteractionContext context, [Option("user", "The user or bot that you want information about.")] DiscordUser user = default) {
            // Set user as self if not provided (and as member if they are one)
            if (context.Guild != null) {
                user = user as DiscordMember ?? context.Member;
            } else {
                user = user ?? context.User;
            }

            // Construct message content
            string nickname = (user is DiscordMember && !string.IsNullOrEmpty((user as DiscordMember).Nickname) ? (user as DiscordMember).Nickname : null);
            string content = $"Here's some information about the {(user.IsBot ? "bot" : "user")} {(nickname ?? user.Username)}:\n\n";

            // Add user information
            if (nickname != null) {
                content += $"**>>** Nickname: __{nickname}__\n";
            }
            content += $"**>>** Username: __{user.Username}__\n";
            content += $"**>>** Discriminator: __{user.Discriminator}__\n";
            if (user is DiscordMember) {
                content += $"**>>** Server Join Date: __{(user as DiscordMember).JoinedAt.UtcDateTime.ToString($"dddd, MMMM d, yyyy 'at' h:mm tt, 'UTC'")}__\n";
            }
            content += $"**>>** Creation Date: __{user.CreationTimestamp.UtcDateTime.ToString($"dddd, MMMM d, yyyy 'at' h:mm tt, 'UTC'")}__\n";
            content += $"**>>** Status: __{user.Presence.Status}__\n";
            content += $"**>>** Custom Status: __{(user.Presence.Activities.Count > 0 && user.Presence.Activities[0].CustomStatus != null ? user.Presence.Activities[0].CustomStatus.Name : "None")}__\n";
            content += $"**>>** Avatar: {user.GetAvatarUrl(ImageFormat.Auto, 256)}\n";
            if (user is DiscordMember && (user as DiscordMember).GuildAvatarUrl != null) {
                content += $"**>>** Server Avatar: {(user as DiscordMember).GetGuildAvatarUrl(ImageFormat.Auto, 256)}";
            }

            // Respond
            await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder() {
                Content = content
            });
        }

        [SlashCommand("server", "Provides information about the server in which the command was run.")]
        [SlashRequireGuild]
        public async Task ServerInfoAsync(InteractionContext context) {
            // Acknowledge interaction
            await context.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            // Construct message content
            string content = $"Here's some information about the server {context.Guild.Name}:\n\n";

            // Add guild information
            if (!string.IsNullOrEmpty(context.Guild.Description)) {
                content += $"**>>** Description: __{context.Guild.Description}__\n";
            }
            content += $"**>>** Channels: __{context.Guild.Channels.Count}__\n";
            content += $"**>>** Members: __{context.Guild.MemberCount}__\n";
            content += $"**>>** Roles: __{context.Guild.Roles.Count}__\n";
            content += $"**>>** Emojis: __{context.Guild.Emojis.Count}__\n";
            content += $"**>>** Stickers: __{context.Guild.Stickers.Count}__\n";
            content += $"**>>** Creation Date: __{context.Guild.CreationTimestamp.UtcDateTime.ToString($"dddd, MMMM d, yyyy 'at' h:mm tt, 'UTC'")}__\n";
            content += $"**>>** Owner: __{context.Guild.Owner.Nickname ?? context.Guild.Owner.Username}__\n";
            try {
                if ((await context.Guild.GetMemberAsync(context.Client.CurrentUser.Id)).Permissions.HasPermission(Permissions.ManageGuild)) {
                    IReadOnlyList<DiscordInvite> invites = (await context.Guild.GetInvitesAsync()).Where(i => !i.IsTemporary).ToList();
                    if (invites.Count != 0) {
                        content += $"**>>** Invite: {invites[0]} \n";
                    }
                }
            } catch (Exception) {
                // Fail silently
            }
            content += $"**>>** Server Icon: {context.Guild.GetIconUrl(ImageFormat.Auto, 256)}\n";

            // Respond with a message
            await context.EditResponseAsync(new DiscordWebhookBuilder() {
                Content = content
            });
        }

    }

}
