using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;

namespace Divibot.Commands {

    [SlashCommandGroup("decode", "Various commands to decode messages.")]
    public class DecodeModule : ApplicationCommandModule {

        [SlashCommand("binary", "Decodes groups of UTF-8 encoded binary digits into a message.")]
        public async Task DecodeBinaryAsync(InteractionContext context, [Option("text", "The text you wish to decode.")] string text) {
            // Remove Spaces
            text = text.Replace(" ", "");

            // Convert Binary to String
            List<byte> bytes = new List<byte>();
            for (int i = 0; i < text.Length; i += 8) {
                bytes.Add(Convert.ToByte(text.Substring(i, 8), 2));
            }
            string output = Encoding.UTF8.GetString(bytes.ToArray());
            if (string.IsNullOrWhiteSpace(output)) {
                output = "You're trying to decode... a space? How's that supposed to work?";
            } else {
                output = Encoding.UTF8.GetString(bytes.ToArray());
            }

            // Respond
            await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder() {
                Content = output
            });
        }

        [SlashCommand("hexadecimal", "Decodes groups of UTF-8 encoded hexadecimal digits into a message.")]
        public async Task DecodeHexadecimalAsync(InteractionContext context, [Option("text", "The text you wish to decode.")] string text) {
            // Remove Spaces & Newlines
            text = text.Replace(" ", "").Replace("\n", "");

            string output = "";
            for (int i = 0; i < text.Length; i += 2) {
                try {
                    int hexint = int.Parse($"{text[i]}{text[i + 1]}", NumberStyles.AllowHexSpecifier);
                    if (hexint == 255) {
                        continue;
                    }
                    output += (char) hexint;
                } catch (Exception e) {
                    output = "Sorry, it seems as though the inputted hexadeicmal is invalid.";
                    return;
                }
            }
            if (string.IsNullOrWhiteSpace(output)) {
                output = "You're trying to decode... a space? How's that supposed to work?";
            }

            // Respond
            await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder() {
                Content = output
            });
        }

        [SlashCommand("fube", "Decodes a message that was encoded using the Fire's Ultimate Botting Experience (FUBE) language.")]
        public async Task DecodeFubeAsync(InteractionContext context, [Option("text", "The text you wish to decode.")] string text) {
            string output = text;

            // 6 Long
            output = output.Replace("iiiiii~", "F");
            output = output.Replace("IIIIII~", "L");
            output = output.Replace("llllll~", "R");
            output = output.Replace("LLLLLL~", "X");

            // 5 Long
            output = output.Replace("iiiii~", "E");
            output = output.Replace("IIIII~", "K");
            output = output.Replace("lllll~", "Q");
            output = output.Replace("LLLLL~", "W");
            output = output.Replace(";;;;;~", "*");

            // 4 Long
            output = output.Replace("iiii~", "D");
            output = output.Replace("IIII~", "J");
            output = output.Replace("llll~", "P");
            output = output.Replace("LLLL~", "V");
            output = output.Replace(";;;;~", "!");

            // 3 Long
            output = output.Replace("iii~", "C");
            output = output.Replace("III~", "I");
            output = output.Replace("lll~", "O");
            output = output.Replace("LLL~", "U");
            output = output.Replace(";;;~", "?");

            // 2 Long
            output = output.Replace("ii~", "B");
            output = output.Replace("II~", "H");
            output = output.Replace("ll~", "N");
            output = output.Replace("LL~", "T");
            output = output.Replace("jj~", "Z");
            output = output.Replace(";;~", ",");

            // 1 Long
            output = output.Replace("i~", "A");
            output = output.Replace("I~", "G");
            output = output.Replace("l~", "M");
            output = output.Replace("L~", "S");
            output = output.Replace("j~", "Y");
            output = output.Replace(";~", ".");

            // Respond
            await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder() {
                Content = output
            });
        }

    }

}
