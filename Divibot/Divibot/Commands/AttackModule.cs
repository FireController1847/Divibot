using Divibot.Attack;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.Interactivity.Extensions;
using Divibot.Database.Entities;
using Divibot.Database;
using Microsoft.EntityFrameworkCore;

namespace Divibot.Commands {

    public class AttackModule : ApplicationCommandModule {

        // Dependency Injection
        private Random _random;
        private DivibotDbContext _dbContext;
        private AttackService _attackService;

        // Constructor
        public AttackModule(Random random, DivibotDbContext dbContext, AttackService attackService) {
            _random = random;
            _dbContext = dbContext;
            _attackService = attackService;
        }

        // TODO: I should add a command for custom classes that generates a string of text based on
        //       approximate chances they have for certain categories. For example, if somebody's
        //       custom says they're > 80% good in nice attacks and < 20% in gross attacks, it could
        //       say: "You seem to be an expert at being nice to others. Your gross attacks don't appear to have the same luck."
        //       Just an idea, think it over, because it could be too complicated and/or too cheaty.
        //       This could also just be a response for the final generation prompt.

        [SlashCommand("attack", "Attacks another user!")]
        public async Task AttackAsync(InteractionContext context, [Option("user", "The user you want to attack.")] DiscordUser user, [Option("attack", "The attack you want to use.")] string attack) {
            // Acknowledge
            await context.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            // Fetch appropriate attack
            AttackCategory attackCategory = AttackCategory.NICE; // This will never be used, but it's needed to get rid of compiler errors.
            AttackType attackType = null;
            foreach (KeyValuePair<AttackCategory, AttackType[]> kvp in AttackConstants.AttackTypes) {
                attackType = kvp.Value.FirstOrDefault(a => a.Id.Contains(Divibot.ToSnakeCase(attack)));
                if (attackType != null) {
                    attackCategory = kvp.Key;
                    break;
                }
            }
            if (attackType == null) {
                await context.EditResponseAsync(new DiscordWebhookBuilder() {
                    Content = "Sorry, I wasn't able to find an attack by that name :confused:"
                });
                return;
            }

            // Get the user's class
            AttackClass attackClass = await _attackService.GetClassForUserAsync(context.User.Id);
            if (attackClass == null) {
                await SetupCustomClass(context);
                return;
            }

            // Get chacnes
            EntityAttackTypeChance chances = _dbContext.AttackTypeChances.SingleOrDefault(c => c.UserId == context.User.Id && c.AttackCategory == attackCategory && c.AttackTypeId == attackType.Id);
            if (chances == null) {
                // Assume we're missing a specific attack
                chances = _attackService.GenerateChancesFor(context.User.Id, attackCategory, attackType, attackClass);
                _dbContext.AttackTypeChances.Add(chances);
            }

            // Attack!
            string message = $"{context.User.Mention} used {attackType.Name} on {user.Mention}.";
            int gain = 0;
            if (_random.NextDouble() < (chances.CritChance / 100.0)) {
                message += " It was a critical hit!";
                gain = 2;
            } else if (_random.NextDouble() < (chances.Chance / 100.0)) {
                message += " The attack was effective!";
                gain = 1;
            } else if (_random.NextDouble() < (chances.IneffChance / 100.0)) {
                message += " The attack was ineffective!";
                gain = 0;
            } else {
                message += " The attack missed!";
                gain = -1;
            }

            // Update score
            EntityAttackUser currentUser = _dbContext.AttackUsers.SingleOrDefault(c => c.UserId == context.User.Id);
            if (currentUser == null) {
                // How?
                return;
            }
            currentUser.Score += gain;
            _dbContext.AttackUsers.Attach(currentUser);
            _dbContext.Entry(currentUser).Property(c => c.Score).IsModified = true;

            // Add gain to message
            message += $" You {(gain < 0 ? "lost" : "gained")} {Math.Abs(gain)} {(Math.Abs(gain) == 1 ? "point" : "points")}.";

            // Save database
            await _dbContext.SaveChangesAsync();

            // Respond
            // Note: At least this doesn't error compared to an edit, but technically this is 'editing' the message so it
            //       does not send the notification for the mention. Either Discord will fix this one day, or it'll never
            //       mention. As far as I'm concerned, at this point, it's their problem, not mine. I've done what they
            //       tell me to do, and their system doesn't handle it correctly. Not my fault!
            await context.FollowUpAsync(new DiscordFollowupMessageBuilder() {
                Content = message
            }.AddMention(new UserMention(user)));
        }

        [SlashCommand("attacks", "Provides the comprehensive list of all attacks.")]
        public async Task AttacksAsync(InteractionContext context) {
            // Prepare message
            string content = "Here's a list of all attacks:\n\n";

            // Add attacks
            foreach (AttackCategory cat in Enum.GetValues(typeof(AttackCategory))) {
                content += $"// **{Divibot.ToProperCase(cat.ToString())}**\n";
                AttackConstants.AttackTypes.TryGetValue(cat, out AttackType[] types);
                foreach (AttackType type in types) {
                    content += $">> {type.Name}\n";
                }

                // Add extra newline for new category
                content += "\n";
            }

            // Respond
            await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder() {
                Content = content,
                IsEphemeral = true
            });
        }

        [SlashCommandGroup("class", "Commands relating to attacks and your current attack class.")]
        public class AttackClassModule : ApplicationCommandModule {

            // Dependency Injection
            private DivibotDbContext _dbContext;

            // Constructor
            public AttackClassModule(DivibotDbContext dbContext) {
                _dbContext = dbContext;
            }

            [SlashCommand("score", "Tells you your current attack score.")]
            public async Task ClassScoreAsync(InteractionContext context) {
                // Check for existing user
                EntityAttackUser attackUser = await _dbContext.AttackUsers.SingleOrDefaultAsync(u => u.UserId == context.User.Id);
                if (attackUser == null) {
                    await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder() {
                        Content = "It appears as though you don't currently have a class yet. Try running the attack command once first, then check back here!",
                        IsEphemeral = true
                    });
                } else {
                    await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder() {
                        Content = $"You currently have {attackUser.Score} point{(attackUser.Score != 1 ? "s" : "")}."
                    });
                }
            }

            [SlashCommand("remove", "Removes your current class.")]
            public async Task ClassRemoveAsync(InteractionContext context) {
                // Check for an existing user
                EntityAttackUser attackUser = await _dbContext.AttackUsers.SingleOrDefaultAsync(u => u.UserId == context.User.Id);
                if (attackUser == null) {
                    await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder() {
                        Content = "It appears as though you don't currently have a class to remove!",
                        IsEphemeral = true
                    });
                    return;
                }

                // Check if the user is sure
                DiscordComponent[] components = new DiscordComponent[] {
                    new DiscordButtonComponent(ButtonStyle.Primary, "class_remove_nevermind", "Nevermind"),
                    new DiscordButtonComponent(ButtonStyle.Danger, "class_remove_confirm", "I'm Sure")
                };
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder() {
                    Content = "Are you sure you want to remove your current class? **Your score will be reset to zero**, and you'll have to take the survey again!",
                    IsEphemeral = true
                }.AddComponents(components));
                DiscordMessage responseMessage = await context.GetOriginalResponseAsync();

                // Wait for response
                var response = await responseMessage.WaitForButtonAsync(context.User);

                // Handle timeout
                if (response.TimedOut) {
                    await InteractionTimedOut(context.Interaction, responseMessage.Id, components);
                    return;
                }

                // Handle nevermind
                if (response.Result.Id == "class_remove_nevermind") {
                    await context.EditResponseAsync(new DiscordWebhookBuilder() {
                        Content = "Whew, alright! That would've been close."
                    });
                    return;
                }

                // Handle confirm
                await _dbContext.Database.ExecuteSqlRawAsync($"DELETE FROM attackusers WHERE UserId = '{context.User.Id}'");
                await _dbContext.Database.ExecuteSqlRawAsync($"DELETE FROM attacktypechances WHERE UserId = '{context.User.Id}'");
                await _dbContext.Database.ExecuteSqlRawAsync($"DELETE FROM customattackcategorychances WHERE UserId = '{context.User.Id}'");
                await _dbContext.Database.ExecuteSqlRawAsync($"DELETE FROM customattackmodifierchances WHERE UserId = '{context.User.Id}'");
                _dbContext.ChangeTracker.Clear();

                // Respond
                await context.EditResponseAsync(new DiscordWebhookBuilder() {
                    Content = "Alright, I've removed your class. Run the attack command to gain a new one."
                });
            }

        }

        /// <summary>
        /// An interactive session to create a custom class for a user.
        /// </summary>
        /// <param name="context">The context from the original command call.</param>
        private async Task SetupCustomClass(InteractionContext context) {
            // Initial message edit
            await context.EditResponseAsync(new DiscordWebhookBuilder() {
                Content = "Sorry, it seems as though you don't have a class yet."
            });

            // Create a followup with two confirmation buttons
            DiscordComponent[] followupComponents = new DiscordComponent[] {
                new DiscordButtonComponent(ButtonStyle.Primary, "att_class_confirm", "Yes"),
                new DiscordButtonComponent(ButtonStyle.Danger, "att_class_deny", "No")
            };
            DiscordMessage followup = await context.FollowUpAsync(new DiscordFollowupMessageBuilder() {
                Content = "Do you want to take a quick survey to get one set up?",
                IsEphemeral = true
            }.AddComponents(followupComponents));

            // Wait for response
            var followupResponse = await followup.WaitForButtonAsync();

            // Handle timeout
            if (followupResponse.TimedOut) {
                await InteractionTimedOut(context.Interaction, followup.Id, followupComponents);
                return;
            }

            // Handle if the user denied, otherwise anything else must be a confirm
            if (followupResponse.Result.Id == "att_class_deny") {
                await followupResponse.Result.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder() {
                    Content = "No worries! To get this popup again, just run the attack command!"
                }.AddComponents(await DisableComponents(followupComponents)));
                return;
            }

            // Since we've confirmed, show an informational message.
            DiscordComponent[] infoComponents = new DiscordComponent[] {
                new DiscordButtonComponent(ButtonStyle.Primary, "att_class_question1", "Okay")
            };
            await followupResponse.Result.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder() {
                Content = "Sweet! I'm going to ask you six questions to determine what your attack chances will be. " +
                          "They will show up as a list of options, and once you've selected one we'll move on to the next question!\n\n" +
                          "If at any point in time you'd like to stop, feel free to dismiss this message. Press okay to continue."
            }.AddComponents(infoComponents));
            DiscordMessage infoFollowup = (await followupResponse.Result.Interaction.GetFollowupMessageAsync(followup.Id));

            // Wait for response
            var infoFollowupResponse = await infoFollowup.WaitForButtonAsync();

            // Handle timeout
            if (infoFollowupResponse.TimedOut) {
                await InteractionTimedOut(followupResponse.Result.Interaction, infoFollowup.Id, infoComponents);
                return;
            }

            // Prepare stats
            Dictionary<string, int> categoryScores = new Dictionary<string, int>();
            foreach (AttackCategory attackCategory in Enum.GetValues(typeof(AttackCategory))) {
                categoryScores.Add(attackCategory.ToString(), 1);
            }

            // Show first question
            DiscordComponent[] question1Components = new DiscordComponent[] {
                new DiscordSelectComponent("att_class_q1_select", null, new List<DiscordSelectComponentOption>() {
                    new DiscordSelectComponentOption("$80/blade or get out", "RUDE", "+RUDE"),
                    new DiscordSelectComponentOption("Must own dogs", "GROSS", "+GROSS"),
                    new DiscordSelectComponentOption("The cutting edge", "JOKING", "+JOKING"),
                    new DiscordSelectComponentOption("Just a trim?", "NICE", "+NICE"),
                    new DiscordSelectComponentOption($"{context.User.Username}'s lawn care", "COWARD", "+COWARD"),
                    new DiscordSelectComponentOption("My lawn hasn't been mowed for weeks", "SAD", "+SAD"),
                }.OrderBy(a => _random.Next()).ToList())
            };
            await infoFollowupResponse.Result.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder() {
                Content = "If you ran a lawn mowing business, what would it be called?"
            }.AddComponents(question1Components));
            DiscordMessage question1Followup = (await infoFollowupResponse.Result.Interaction.GetFollowupMessageAsync(infoFollowup.Id));

            // Wait for response
            var question1FollowupResponse = await question1Followup.WaitForSelectAsync("att_class_q1_select");

            // Handle timeout
            if (question1FollowupResponse.TimedOut) {
                await InteractionTimedOut(infoFollowupResponse.Result.Interaction, question1Followup.Id, question1Components);
                return;
            }

            // Add point to appropriate category
            string question1Value = question1FollowupResponse.Result.Values[0];
            CustomClassAddPoints(ref categoryScores, question1Value);

            // Show second question
            DiscordComponent[] question2Components = new DiscordComponent[] {
                new DiscordSelectComponent("att_class_q2_select", null, new List<DiscordSelectComponentOption>() {
                    new DiscordSelectComponentOption("It's flipping you off", "RUDE", "+RUDE"),
                    new DiscordSelectComponentOption("I... don't want to tell you", "GROSS", "+GROSS"),
                    new DiscordSelectComponentOption("A penis", "JOKING", "+JOKING"),
                    new DiscordSelectComponentOption("A flower", "NICE", "+NICE"),
                    new DiscordSelectComponentOption("A black blob", "COWARD", "+COWARD"),
                    new DiscordSelectComponentOption("My depression", "SAD", "+SAD"),
                }.OrderBy(a => _random.Next()).ToList())
            };
            await question1FollowupResponse.Result.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder() {
                Content = "I just showed you a black blob. What does it look like?"
            }.AddComponents(question2Components));
            DiscordMessage question2Followup = (await question1FollowupResponse.Result.Interaction.GetFollowupMessageAsync(question1Followup.Id));

            // Wait for response
            var question2FollowupResponse = await question2Followup.WaitForSelectAsync("att_class_q2_select");

            // Handle timeout
            if (question2FollowupResponse.TimedOut) {
                await InteractionTimedOut(question1FollowupResponse.Result.Interaction, question2Followup.Id, question2Components);
                return;
            }

            // Add point to appropriate category
            string question2Value = question2FollowupResponse.Result.Values[0];
            CustomClassAddPoints(ref categoryScores, question2Value);

            // Show third question
            DiscordComponent[] question3Components = new DiscordComponent[] {
                new DiscordSelectComponent("att_class_q3_select", null, new List<DiscordSelectComponentOption>() {
                    new DiscordSelectComponentOption("Shut the hell up!", "RUDE", "+RUDE"),
                    new DiscordSelectComponentOption("Do it again...", "GROSS", "+GROSS"),
                    new DiscordSelectComponentOption("MAY THE HOLY LORD GIVE YA THE POWUH AND THE BLESSING TO EVACUTE THE DEMONSSSS IN JESUS NAME, AMEEEN!", "JOKING", "+JOKING"),
                    new DiscordSelectComponentOption("Bless you", "NICE", "+NICE"),
                    new DiscordSelectComponentOption("Excuse you", "COWARD", "+COWARD"),
                    new DiscordSelectComponentOption("I remember when I could sneeze...", "SAD", "+SAD"),
                }.OrderBy(a => _random.Next()).ToList())
            };
            await question2FollowupResponse.Result.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder() {
                Content = "I sneezed. What do you tell me?"
            }.AddComponents(question3Components));
            DiscordMessage question3Followup = (await question2FollowupResponse.Result.Interaction.GetFollowupMessageAsync(question2Followup.Id));

            // Wait for response
            var question3FollowupResponse = await question3Followup.WaitForSelectAsync("att_class_q3_select");

            // Handle timeout
            if (question3FollowupResponse.TimedOut) {
                await InteractionTimedOut(question2FollowupResponse.Result.Interaction, question3Followup.Id, question3Components);
                return;
            }

            // Add point to appropriate category
            string question3Value = question3FollowupResponse.Result.Values[0];
            CustomClassAddPoints(ref categoryScores, question3Value);

            // Show fourth question
            DiscordComponent[] question4Components = new DiscordComponent[] {
                new DiscordSelectComponent("att_class_q4_select", null, new List<DiscordSelectComponentOption>() {
                    new DiscordSelectComponentOption("Your mom", "RUDE", "+RUDE"),
                    new DiscordSelectComponentOption("Every single piece of toilet paper", "GROSS", "+GROSS"),
                    new DiscordSelectComponentOption("Rocks.", "JOKING", "+JOKING"),
                    new DiscordSelectComponentOption("The next big discovery", "NICE", "+NICE"),
                    new DiscordSelectComponentOption("A fish", "COWARD", "+COWARD"),
                    new DiscordSelectComponentOption("The very embodiment of my anxiety", "SAD", "+SAD"),
                }.OrderBy(a => _random.Next()).ToList())
            };
            await question3FollowupResponse.Result.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder() {
                Content = "If I travelled to the deepest part of the ocean, what will I find?"
            }.AddComponents(question4Components));
            DiscordMessage question4Followup = (await question3FollowupResponse.Result.Interaction.GetFollowupMessageAsync(question3Followup.Id));

            // Wait for response
            var question4FollowupResponse = await question4Followup.WaitForSelectAsync("att_class_q4_select");

            // Handle timeout
            if (question4FollowupResponse.TimedOut) {
                await InteractionTimedOut(question3FollowupResponse.Result.Interaction, question4Followup.Id, question4Components);
                return;
            }

            // Add point to appropriate category
            string question4Value = question4FollowupResponse.Result.Values[0];
            CustomClassAddPoints(ref categoryScores, question4Value);

            // Show fifth question
            DiscordComponent[] question5Components = new DiscordComponent[] {
                new DiscordSelectComponent("att_class_q5_select", null, new List<DiscordSelectComponentOption>() {
                    new DiscordSelectComponentOption("Place a tack on his seat", "RUDE", "+RUDE"),
                    new DiscordSelectComponentOption("Funnel all fumes from the student's bathroom to his office", "GROSS", "+GROSS"),
                    new DiscordSelectComponentOption("Shoot his ass!", "JOKING", "+JOKING"),
                    new DiscordSelectComponentOption("Wait paitently for him to come back", "NICE", "+NICE"),
                    new DiscordSelectComponentOption("Scream and run", "COWARD", "+COWARD"),
                    new DiscordSelectComponentOption("Cry", "SAD", "+SAD"),
                }.OrderBy(a => _random.Next()).ToList())
            };
            await question4FollowupResponse.Result.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder() {
                Content = "You've been sent to the principal's office for a school you hate but he's not there, what do you do?"
            }.AddComponents(question5Components));
            DiscordMessage question5Followup = (await question4FollowupResponse.Result.Interaction.GetFollowupMessageAsync(question4Followup.Id));

            // Wait for response
            var question5FollowupResponse = await question5Followup.WaitForSelectAsync("att_class_q5_select");

            // Handle timeout
            if (question5FollowupResponse.TimedOut) {
                await InteractionTimedOut(question4FollowupResponse.Result.Interaction, question5Followup.Id, question5Components);
                return;
            }

            // Add point to appropriate category
            string question5Value = question5FollowupResponse.Result.Values[0];
            CustomClassAddPoints(ref categoryScores, question5Value);

            // Show sixth question
            DiscordComponent[] question6Components = new DiscordComponent[] {
                new DiscordSelectComponent("att_class_q6_select", null, new List<DiscordSelectComponentOption>() {
                    new DiscordSelectComponentOption("I'LL SEE YOU ALL IN HELL", "RUDE", "+RUDE"),
                    new DiscordSelectComponentOption("(sneezes violently)", "GROSS", "+GROSS"),
                    new DiscordSelectComponentOption("Make sure not to hit the door on the way out :)", "JOKING", "+JOKING"),
                    new DiscordSelectComponentOption("Don't panic, we'll make it out alive", "NICE", "+NICE"),
                    new DiscordSelectComponentOption("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA", "COWARD", "+COWARD"),
                    new DiscordSelectComponentOption("We're all gonna dieeee!!!!!!!!", "SAD", "+SAD"),
                }.OrderBy(a => _random.Next()).ToList())
            };
            await question5FollowupResponse.Result.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder() {
                Content = "You're in an airplane and you're going down, what are your final words?"
            }.AddComponents(question6Components));
            DiscordMessage question6Followup = (await question5FollowupResponse.Result.Interaction.GetFollowupMessageAsync(question5Followup.Id));

            // Wait for response
            var question6FollowupResponse = await question6Followup.WaitForSelectAsync("att_class_q6_select");

            // Handle timeout
            if (question6FollowupResponse.TimedOut) {
                await InteractionTimedOut(question5FollowupResponse.Result.Interaction, question6Followup.Id, question6Components);
                return;
            }

            // Add point to appropriate category
            string question6Value = question6FollowupResponse.Result.Values[0];
            CustomClassAddPoints(ref categoryScores, question6Value);

            // Create loading response for anticipation
            await question6FollowupResponse.Result.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder() {
                Content = "Nice responses! Please wait while I figure out how this all adds up..."
            });
            DiscordMessage loadingFinalFollowup = (await question6FollowupResponse.Result.Interaction.GetFollowupMessageAsync(question6Followup.Id));

            // Remove any possible previous chances
            await _dbContext.Database.ExecuteSqlRawAsync($"DELETE FROM customattackcategorychances WHERE UserId = '{context.User.Id}'");
            await _dbContext.Database.ExecuteSqlRawAsync($"DELETE FROM customattackmodifierchances WHERE UserId = '{context.User.Id}'");
            _dbContext.ChangeTracker.Clear();

            // // Final calculations \\ \\
            // Get the user's minimum and maximum scores
            int minScore = int.MaxValue;
            int maxScore = int.MinValue;
            foreach (KeyValuePair<string, int> categoryScore in categoryScores) {
                if (categoryScore.Value > maxScore) {
                    maxScore = categoryScore.Value;
                }
                if (categoryScore.Value < minScore) {
                    minScore = categoryScore.Value;
                }
            }
            //context.Client.Logger.LogInformation($"MINSCORE: {minScore}, MAXSCORE: {maxScore}");

            // Create the custom chances for each category
            foreach (KeyValuePair<string, int> categoryScore in categoryScores) {
                EntityCustomAttackCategoryChance chance = new EntityCustomAttackCategoryChance() {
                    UserId = context.User.Id,
                    Category = Enum.Parse<AttackCategory>(categoryScore.Key),
                    ChanceMax = Convert.ToUInt32((categoryScore.Value / (double) (maxScore + 1)) * 100)
                };
                chance.ChanceMin = Convert.ToUInt32(chance.ChanceMax * (_random.NextDouble() * (0.9 - 0.6) + 0.6));
                //context.Client.Logger.LogInformation($"CAT: {categoryScore.Key}, CHANCEMIN: {chance.ChanceMin}, CHANCEMAX: {chance.ChanceMax}");
                _dbContext.CustomAttackCategoryChances.Add(chance);
            }

            // Create the crit modifier chances
            EntityCustomAttackModifierChance critChance = new EntityCustomAttackModifierChance() {
                UserId = context.User.Id,
                Modifier = AttackModifier.CRIT,
                ChanceMin = Convert.ToUInt32((_random.NextDouble() * (0.5 - 0.1) + 0.1) * 100)
            };
            critChance.ChanceMax = Convert.ToUInt32((_random.NextDouble() * (0.7 - (critChance.ChanceMin / 100.0)) + (critChance.ChanceMin / 100.0)) * 100);
            _dbContext.CustomAttackModifierChances.Add(critChance);

            EntityCustomAttackModifierChance ineffChance = new EntityCustomAttackModifierChance() {
                UserId = context.User.Id,
                Modifier = AttackModifier.INEFF,
                ChanceMin = Convert.ToUInt32((_random.NextDouble() * (0.85 - 0.7) + 0.7) * 100)
            };
            ineffChance.ChanceMax = Convert.ToUInt32((_random.NextDouble() * (1.0 - (ineffChance.ChanceMin / 100.0)) + (ineffChance.ChanceMin / 100.0)) * 100);
            _dbContext.CustomAttackModifierChances.Add(ineffChance);

            // Save database
            await _dbContext.SaveChangesAsync();
            // // Final calculations \\ \\

            // Assign user to class
            AttackClass attackClass = AttackConstants.Classes.Single(c => c.Id == "CUSTOM");
            attackClass = await _attackService.ApplyCustomClassChances(context.User.Id, attackClass);
            await _attackService.UpdateUserClassAsync(context.User.Id, attackClass);

            // Delay for aniticipation
            await Task.Delay(TimeSpan.FromSeconds(3));

            // Find best attack
            AttackCategory bestCategory = AttackCategory.NICE; // This will never be used.
            uint bestCategoryChance = uint.MinValue;
            AttackCategory worstCategory = AttackCategory.NICE; // This will never be used.
            uint worstCategoryChance = uint.MaxValue;
            foreach (KeyValuePair<AttackCategory, uint> kvp in attackClass.Chances.ChanceMaxs) {
                if (kvp.Value > bestCategoryChance) {
                    bestCategory = kvp.Key;
                    bestCategoryChance = kvp.Value;
                }
            }
            foreach (KeyValuePair<AttackCategory, uint> kvp in attackClass.Chances.ChanceMins) {
                if (kvp.Value < worstCategoryChance) {
                    worstCategory = kvp.Key;
                    worstCategoryChance = kvp.Value;
                }
            }

            // Final response
            await question6FollowupResponse.Result.Interaction.EditFollowupMessageAsync(loadingFinalFollowup.Id, new DiscordWebhookBuilder() {
                Content = $"All done! :blush:\n\nI've created a completely custom class with chances geared towards your answers. " +
                          $"Now, I don't want to give away too much, but I can tell you that **your best category is {Divibot.ToProperCase(bestCategory.ToString())} attacks** and **your worst category is {Divibot.ToProperCase(worstCategory.ToString())} attacks**.\n\n" +
                          $"Good luck out there, attack responsibly!"
            });
        }

        /// <summary>
        /// Calculates the amount of gained points per category depending on the response.
        /// </summary>
        /// <param name="dictionary">The dictionary storing points.</param>
        /// <param name="response">The responding category.</param>
        private void CustomClassAddPoints(ref Dictionary<string, int> dictionary, string response) {
            if (response == "RUDE") {
                dictionary["RUDE"] = Math.Max(dictionary["RUDE"] + 2, 1);
                //dictionary["GROSS"] += 0;
                //dictionary["JOKING"] += 0;
                dictionary["NICE"] = Math.Max(dictionary["NICE"] - 1, 1);
                //dictionary["COWARD"] += 0;
                dictionary["SAD"] = Math.Max(dictionary["SAD"] + 1, 1);
            } else if (response == "GROSS") {
                dictionary["RUDE"] = Math.Max(dictionary["RUDE"] + 1, 1);
                dictionary["GROSS"] = Math.Max(dictionary["GROSS"] + 2, 1);
                dictionary["JOKING"] = Math.Max(dictionary["JOKING"] + 1, 1);
                dictionary["NICE"] = Math.Max(dictionary["NICE"] - 1 , 1);
                dictionary["COWARD"] = Math.Max(dictionary["COWARD"] - 1, 1);
                //dictionary["SAD"] += 0;
            } else if (response == "JOKING") {
                //dictionary["RUDE"] += 0;
                //dictionary["GROSS"] += 0;
                dictionary["JOKING"] = Math.Max(dictionary["JOKING"] + 2, 1);
                dictionary["NICE"] = Math.Max(dictionary["NICE"] + 1, 1);
                dictionary["COWARD"] = Math.Max(dictionary["COWARD"] - 1, 1);
                //dictionary["SAD"] += 0;
            } else if (response == "NICE") {
                dictionary["RUDE"] = Math.Max(dictionary["RUDE"] - 1, 1);
                //dictionary["GROSS"] += 0;
                dictionary["JOKING"] = Math.Max(dictionary["JOKING"] + 1, 1);
                dictionary["NICE"] = Math.Max(dictionary["NICE"] + 2, 1);
                dictionary["COWARD"] = Math.Max(dictionary["COWARD"] - 1, 1);
                //dictionary["SAD"] += 0;
            } else if (response == "COWARD") {
                dictionary["RUDE"] = Math.Max(dictionary["RUDE"] + 1, 1);
                //dictionary["GROSS"] += 0;
                //dictionary["JOKING"] += 0;
                //dictionary["NICE"] += 0;
                dictionary["COWARD"] = Math.Max(dictionary["COWARD"] + 2, 1);
                dictionary["SAD"] = Math.Max(dictionary["SAD"] + 1, 1);
            } else if (response == "SAD") {
                //dictionary["RUDE"] += 0;
                //dictionary["GROSS"] += 0;
                dictionary["JOKING"] = Math.Max(dictionary["JOKING"] + 1, 1);
                //dictionary["NICE"] += 0;
                dictionary["COWARD"] = Math.Max(dictionary["COWARD"] + 1, 1);
                dictionary["SAD"] = Math.Max(dictionary["SAD"] + 2, 1);
            }
        }

        // TODO: The bottom two following utility methods should be either an extension method or a utility function in Divibot, not here.

        /// <summary>
        /// Disables all of the provided components.
        /// </summary>
        /// <param name="components">The components to disable.</param>
        /// <returns>The components but disabled.</returns>
        public static async Task<DiscordComponent[]> DisableComponents(DiscordComponent[] components) {
            foreach (DiscordComponent component in components) {
                if (component is DiscordButtonComponent) {
                    (component as DiscordButtonComponent).Disable();
                } else if (component is DiscordLinkButtonComponent) {
                    (component as DiscordLinkButtonComponent).Disabled = true;
                } else if (component is DiscordSelectComponent) {
                    (component as DiscordSelectComponent).Disable();
                }
            }
            return components;
        }

        /// <summary>
        /// Edits a followup message with a timeout message.
        /// </summary>
        /// <param name="interaction">The interaction to use.</param>
        /// <param name="messageId">The id of the followup message to edit.</param>
        /// <param name="components">The components on the message, if any.</param>
        public static async Task InteractionTimedOut(DiscordInteraction interaction, ulong messageId, DiscordComponent[] components = null) {
            try {
                // Begin building
                DiscordWebhookBuilder builder = new DiscordWebhookBuilder() {
                    Content = "Looks like it took you a little too long to respond. No worries, maybe we'll chat again later?"
                };

                // Check for components
                if (components != null) {
                    builder.AddComponents(await DisableComponents(components));
                }

                // Edit
                await interaction.EditFollowupMessageAsync(messageId, builder);
            } catch (Exception) {
                // This means we were possibly dismissed or we took too long. That's okay.
            }
        }

    }

}
