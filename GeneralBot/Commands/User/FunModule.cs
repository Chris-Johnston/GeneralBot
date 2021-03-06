﻿using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using GeneralBot.Commands.Results;
using GeneralBot.Extensions;
using GeneralBot.Extensions.Helpers;
using GeneralBot.Models.Config;
using GeneralBot.Models.Urban;
using Newtonsoft.Json;

namespace GeneralBot.Commands.User
{
    [Summary("Fun Commands")]
    [Remarks("Want to goof around? Try these!")]
    public class FunModule : ModuleBase<SocketCommandContext>
    {
        public ConfigModel Config { get; set; }
        public HttpClient HttpClient { get; set; }
        public Random Random { get; set; }

        [Command("8ball")]
        [Summary("Ask it any questions!")]
        public async Task<RuntimeResult> EightBallAsync([Remainder] string input)
        {
            int responseCount = Config.Commands.EightBallResponses.Count;
            if (responseCount == 0)
                // This should hopefully never happen.
            {
                return CommandRuntimeResult.FromError(
                    "The 8ball responses are not yet set, contact the bot developers.");
            }
            string response = Config.Commands.EightBallResponses[Random.Next(0, responseCount)];
            var embed = new EmbedBuilder
                {
                    Author = new EmbedAuthorBuilder
                    {
                        Name = "The Magic 8Ball",
                        IconUrl = Context.Client.CurrentUser.GetAvatarUrlOrDefault()
                    },
                    Color = ColorHelper.GetRandomColor(),
                    ThumbnailUrl = Config.Icons.EightBall
                }
                .AddField("You asked...", input)
                .AddField("The 8 ball says...", response);
            await ReplyAsync("", embed: embed.Build()).ConfigureAwait(false);
            return CommandRuntimeResult.FromSuccess();
        }

        [Command("choose")]
        [Alias("decide")]
        [Summary("Can't decide? Ask the bot to choose it for you!")]
        public async Task<RuntimeResult> ChooseAsync([Remainder] string input)
        {
            var regexParsed = Regex.Split(input, "or|;|,|and", RegexOptions.IgnoreCase);
            if (regexParsed.Length == 0)
                return CommandRuntimeResult.FromError("You need to supply more than one option!");
            var embed = new EmbedBuilder
            {
                Author = new EmbedAuthorBuilder
                {
                    Name = "I think you should choose...",
                    IconUrl = Context.Client.CurrentUser.GetAvatarUrlOrDefault()
                },
                Color = ColorHelper.GetRandomColor(),
                Description = regexParsed[Random.Next(0, regexParsed.Length)]
            };
            await ReplyAsync("", embed: embed.Build()).ConfigureAwait(false);
            return CommandRuntimeResult.FromSuccess();
        }

        [Command("urban")]
        [Alias("urban-dictionary", "ud")]
        [Summary("Looks up a term on the Urban Dictionary.")]
        public async Task<RuntimeResult> UrbanAsync([Remainder] string term)
        {
            string parsedTerm = WebUtility.HtmlEncode(term);
            using (var response =
                await HttpClient.GetAsync($"http://api.urbandictionary.com/v0/define?term={parsedTerm}").ConfigureAwait(false))
            {
                if (!response.IsSuccessStatusCode)
                {
                    return CommandRuntimeResult.FromError(
                        "Urban cannot be reached at the moment, please try again later!");
                }
                string responseParsed = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var search = JsonConvert.DeserializeObject<UrbanResponse>(responseParsed);
                var result = search.Results?.FirstOrDefault();
                if (result == null)
                    return CommandRuntimeResult.FromError($"No definition for {Format.Bold(term)} found!");
                var builder = new EmbedBuilder
                    {
                        Title = $"Urban dictionary search for {term}:",
                        Description = result.Definition,
                        Color = Color.Green,
                        Footer = new EmbedFooterBuilder {Text = $"Defined by {result.Author}."}
                    }
                    .AddField("Example:", result.Example)
                    .AddField("Likes:", $":thumbsup: {result.Likes}", true)
                    .AddField("Dislikes:", $":thumbsdown: {result.Dislikes}", true);
                await ReplyAsync("", embed: builder.Build()).ConfigureAwait(false);
                return CommandRuntimeResult.FromSuccess();
            }
        }
    }
}