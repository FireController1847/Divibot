using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Divibot.Commands {

    public class ContextualEvalGlobals {

        public InteractionContext context;

    }

    public class OwnerModule : ApplicationCommandModule {

        [SlashCommand("serverlist", "Creates a list of servers using Divibot's pagination feature.")]
        [SlashRequireOwner]
        public async Task ServerlistAsync(InteractionContext context, [Option("page", "The page to list."), Minimum(1), Maximum(int.MaxValue)] long page = 1) {
            IReadOnlyDictionary<ulong, DiscordGuild> guilds = Divibot.Client.Guilds;
            string[] lines = guilds.Values.OrderBy(g => g.Name).Select((g, i) => {
                return $"Server: {g.Name} ({g.Id})\nOwner: {g.Owner?.Username} ({g.Owner?.Id})\n";
            }).ToArray();
            string output = Divibot.Pagination(lines, Convert.ToInt32(page) - 1);
            await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder() {
                Content = $"{output}Page {page}/{(int) Math.Ceiling((double) guilds.Count / 10)}"
            });
        }

        [SlashCommand("eval", "Evaluates the given C# code using Roslyn.")]
        [SlashRequireOwner]
        public async Task EvalAsync(InteractionContext context, [Option("code", "The code to evaluate.")] string code) {
            // Acknowledge
            await context.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            // Create globals
            ContextualEvalGlobals globals = new ContextualEvalGlobals() {
                context = context
            };

            // Evaluate
            string content;
            try {
                string result = (await CSharpScript
                    .EvaluateAsync(code, ScriptOptions.Default
                        .WithReferences(typeof(Divibot).Assembly),
                    globals)).ToString();
                content = $"```\n{result}\n```";
            } catch (Exception e) {
                content = $"```\n{e.Message}\n\nSee console for more information.\n```";
                Divibot.Client.Logger.LogError(e, e.Message);
            }

            // Respond
            await context.EditResponseAsync(new DiscordWebhookBuilder() {
                Content = content
            });
        }

    }

}
