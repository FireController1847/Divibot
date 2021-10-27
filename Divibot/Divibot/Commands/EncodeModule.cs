using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

/**
 * FUBE
 * LANGUAGE KEY
 * 
 * i = A
 * ii = B
 * iii = C
 * iiii = D
 * iiiii = E
 * iiiiii = F
 * I = G
 * II = H
 * III = I
 * IIII = J
 * IIIII = K
 * IIIIII = L
 * l = M
 * ll = N
 * lll = O
 * llll = P
 * lllll = Q
 * llllll = R
 * L = S
 * LL = T
 * LLL = U
 * LLLL = V
 * LLLLL = W
 * LLLLLL = X
 * j = Y
 * jj = Z
 * 
 * ~ = CHAR-BREAKER
 * ; = .
 * ;; = ,
 * ;;; = ?
 * ;;;; = !
 * ;;;;; = *
 */

namespace Divibot.Commands {

    [SlashCommandGroup("encode", "Various commands to encode messages.")]
    public class EncodeModule : ApplicationCommandModule {

        [SlashCommand("binary", "Encodes a message into groups of UTF-8 encoded binary digits.")]
        public async Task EncodeBinaryAsync(InteractionContext context, [Option("text", "The text you wish to encode.")] string text) {
            string output = "";
            foreach (char chr in text) {
                output += Convert.ToString(chr, 2).PadLeft(8, '0');
            }

            // Split output and respond
            var outputSplit = Enumerable.Range(0, output.Length / 8).Select(i => output.Substring(i * 8, 8));
            await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder() {
                Content = string.Join(' ', outputSplit)
            });
        }

        [SlashCommand("hexadecimal", "Encodes a message into groups of UTF-8 encoded hexadecimal digits.")]
        public async Task EncodeHexadecimalAsync(InteractionContext context, [Option("text", "The text you wish to encode.")] string text) {
            // Convert String to Hexadecimal
            string output = "";
            foreach (char chr in text) {
                output += Convert.ToString(chr, 16);
            }

            // Ensure Multiple of 16
            int modulus = output.Length % 16;
            if (modulus != 0) {
                for (int i = 0; i < (16 - modulus); i++) {
                    output += 'f';
                }
            }

            // Split Lines into Groups of 8 & Add Spaces
            List<string> lines = new List<string>();
            string currentLine = "";
            for (int i = 0; i < output.Length; i += 2) {
                // Append Characters
                currentLine += output[i];
                currentLine += output[i + 1];

                if (i != 0 && (i + 2) % 16 == 0) {
                    lines.Add(currentLine);
                    currentLine = "";
                } else {
                    currentLine += ' ';
                }
            }

            // Add Actual Text
            for (int i = 0; i < lines.Count; i++) {
                string line = lines[i];
                string[] hexlist = line.Split(' ');
                string hexline = "";
                for (int j = 0; j < hexlist.Length; j++) {
                    int hexint = int.Parse(hexlist[j], NumberStyles.AllowHexSpecifier);
                    char hex = (char) hexint;
                    if (hexint == 255 || string.IsNullOrWhiteSpace(hex.ToString())) {
                        hexline += ". ";
                    } else {
                        hexline += $"{hex} ";
                    }
                }
                line += $"               {hexline}";
                lines[i] = line;
            }

            string finalOutput = string.Join('\n', lines);
            if (finalOutput.Length >= 1995) {
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder() {
                    Content = "Sorry, but the encoded hexadecimal was too long to send in one message.",
                    IsEphemeral = true
                });
                return;
            }

            await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder() {
                Content = $"```\n{finalOutput}\n```"
            });
        }

            [SlashCommand("fube", "Encodes a message using the Fire's Ultimate Botting Experience (FUBE) language.")]
        public async Task EncodeFubeAsync(InteractionContext context, [Option("text", "The text you wish to encode.")] string text) {
            string output = "";
            foreach (char chr in text) {
                string resultchr = chr.ToString().ToUpper();

                switch (resultchr) {
                    case "A":
                        resultchr = "i~";
                        break;
                    case "B":
                        resultchr = "ii~";
                        break;
                    case "C":
                        resultchr = "iii~";
                        break;
                    case "D":
                        resultchr = "iiii~";
                        break;
                    case "E":
                        resultchr = "iiiii~";
                        break;
                    case "F":
                        resultchr = "iiiiii~";
                        break;
                    case "G":
                        resultchr = "I~";
                        break;
                    case "H":
                        resultchr = "II~";
                        break;
                    case "I":
                        resultchr = "III~";
                        break;
                    case "J":
                        resultchr = "IIII~";
                        break;
                    case "K":
                        resultchr = "IIIII~";
                        break;
                    case "L":
                        resultchr = "IIIIII~";
                        break;
                    case "M":
                        resultchr = "l~";
                        break;
                    case "N":
                        resultchr = "ll~";
                        break;
                    case "O":
                        resultchr = "lll~";
                        break;
                    case "P":
                        resultchr = "llll~";
                        break;
                    case "Q":
                        resultchr = "lllll~";
                        break;
                    case "R":
                        resultchr = "llllll~";
                        break;
                    case "S":
                        resultchr = "L~";
                        break;
                    case "T":
                        resultchr = "LL~";
                        break;
                    case "U":
                        resultchr = "LLL~";
                        break;
                    case "V":
                        resultchr = "LLLL~";
                        break;
                    case "W":
                        resultchr = "LLLLL~";
                        break;
                    case "X":
                        resultchr = "LLLLLL~";
                        break;
                    case "Y":
                        resultchr = "j~";
                        break;
                    case "Z":
                        resultchr = "jj~";
                        break;
                    case ".":
                        resultchr = ";~";
                        break;
                    case ",":
                        resultchr = ";;~";
                        break;
                    case "?":
                        resultchr = ";;;~";
                        break;
                    case "!":
                        resultchr = ";;;;~";
                        break;
                    case "*":
                        resultchr = ";;;;;~";
                        break;
                }

                output += resultchr;
            }

            await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder() {
                Content = output
            });
        }

    }

}
