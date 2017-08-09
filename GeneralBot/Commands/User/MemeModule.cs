﻿using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using GeneralBot.Commands.Results;
using GeneralBot.Extensions.Helpers;
using GeneralBot.Models.Config;
using GeneralBot.Models.Reddit;
using ImageSharp;
using ImageSharp.Formats;
using Newtonsoft.Json;
using Image = ImageSharp.Image;
using SixLabors.Primitives;
using ImageSharp.PixelFormats;

namespace GeneralBot.Commands.User
{
    [Summary("Meme Commands")]
    [Remarks("For your daily doses of memery, what more could I say?")]
    public class MemeModule : ModuleBase<SocketCommandContext>
    {
        public ConfigModel Config { get; set; }
        public HttpClient HttpClient { get; set; }
        public Random Random { get; set; }
        private IDisposable TypingDisposable { get; set; }

        protected override void BeforeExecute(CommandInfo command)
        {
            base.BeforeExecute(command);
            TypingDisposable = Context.Channel.EnterTypingState();
        }

        protected override void AfterExecute(CommandInfo command)
        {
            base.AfterExecute(command);
            TypingDisposable.Dispose();
        }

        [Command("expand")]
        [Summary("Replies with a s t h e t i c texts.")]
        public Task<RuntimeResult> ExpandMemeAsync([Remainder] string input)
        {
            var regexMatch = Regex.Match(Context.Message.Content, $@"\b({input})\b");
            string parsedInput = Context.Message.Resolve(regexMatch.Success
                ? regexMatch.Index
                : Context.Message.Content.IndexOf(input, StringComparison.OrdinalIgnoreCase));
            var sb = new StringBuilder();
            foreach (char c in parsedInput)
            {
                if (c == ' ') continue;
                sb.Append(c);
                sb.Append(' ');
            }
            return Task.FromResult<RuntimeResult>(CommandRuntimeResult.FromInfo(sb));
        }

        [Command("cowsay")]
        [Summary("Moo!")]
        public async Task<RuntimeResult> CowsayAsync([Remainder] string text)
        {
            string parsedInput = WebUtility.HtmlEncode(text);
            using (var response =
                await HttpClient.GetAsync($"http://cowsay.morecode.org/say?message={parsedInput}&format=text"))
            {
                if (!response.IsSuccessStatusCode)
                {
                    return CommandRuntimeResult.FromError(
                        "I cannot reach cowsay at the moment, please try again later!");
                }
                string output = await response.Content.ReadAsStringAsync();
                // This should likely never happen, but just in case.
                if (string.IsNullOrEmpty(output))
                {
                    return CommandRuntimeResult.FromError(
                        "Cowsay is out of reach, please try again with another text!");
                }
                await ReplyAsync(output);
                return CommandRuntimeResult.FromSuccess();
            }
        }

        [Command("thinking")]
        [Alias("rthinking", "think", "🤔")]
        public async Task<RuntimeResult> ThinkingAsync()
        {
            using (var response = await HttpClient.GetAsync("https://www.reddit.com/r/Thinking/.json"))
            {
                if (!response.IsSuccessStatusCode)
                    return CommandRuntimeResult.FromError("Reddit is out of reach, please try again later!");

                var result =
                    JsonConvert.DeserializeObject<RedditResponseModel>(await response.Content.ReadAsStringAsync());
                var children = Context.Channel.IsNsfw
                    ? result.Data.Children
                    : result.Data.Children.Where(x => !x.Data.IsNsfw).ToList();
                int index = Random.Next(children.Count);
                var post = children[index];
                string title = Config.Commands.ThinkingTitles[Random.Next(Config.Commands.ThinkingTitles.Count)];
                var builder = new EmbedBuilder
                {
                    Title = post.Data.Title,
                    Description = $"{(post.Data.IsNsfw ? $"{title} (NSFW)" : title)}\n\nPosted by u/{post.Data.Author}",
                    Url = "https://www.reddit.com/" + post.Data.Permalink,
                    Color = ColorHelper.GetRandomColor(),
                    ThumbnailUrl = post.Data.Url
                };
                await ReplyAsync("", embed: builder);
                return CommandRuntimeResult.FromSuccess();
            }
        }

        [Command("needsmorejpeg")]
        [Alias("jpg", "jpeg")]
        public async Task<RuntimeResult> NeedsMoreJpegAsync()
        {
            var message =
                (await Context.Channel.GetMessagesAsync().Flatten())?
                .FirstOrDefault(x => x.Attachments.Any(a => a.Width.HasValue));
            if (message == null)
                return CommandRuntimeResult.FromError("No images found!");
            foreach (var attachment in message.Attachments)
            {
                using (var attachmentStream = await WebHelper.GetFileStreamAsync(HttpClient, new Uri(attachment.Url)))
                using (var image = Image.Load(attachmentStream))
                using (var imageStream = new MemoryStream())
                {
                    image.SaveAsJpeg(imageStream, new JpegEncoder {Quality = 2});
                    imageStream.Seek(0, SeekOrigin.Begin);
                    await Context.Channel.SendFileAsync(imageStream, "needsmorejpeg.jpeg");
                }
            }
            return CommandRuntimeResult.FromSuccess();
        }

        [Command("angery")]
        [Summary("Feeling angry?")]
        public async Task<RuntimeResult> Angery()
        {
            var message =
                (await Context.Channel.GetMessagesAsync().Flatten())?
                .FirstOrDefault(x => x.Attachments.Any(a => a.Width.HasValue));
            if (message == null)
                return CommandRuntimeResult.FromError("No images found!");
            foreach (var attachment in message.Attachments)
            {
                using (var attachmentStream = await WebHelper.GetFileStreamAsync(HttpClient, new Uri(attachment.Url)))
                using (var image = Image.Load(attachmentStream))
                using (var imageStream = new MemoryStream())
                {
                    image.DrawPolygon(Rgba32.Red, 1000000,
                        new PointF[] { new Point(0, 0), new Point(image.Width, image.Height)},
                        new GraphicsOptions() { BlenderMode = PixelBlenderMode.Screen, BlendPercentage = 25 })
                        .SaveAsPng(imageStream);
                    imageStream.Seek(0, SeekOrigin.Begin);
                    await Context.Channel.SendFileAsync(imageStream, "angery.png");
                }
            }
            return CommandRuntimeResult.FromSuccess();
        }
    }
}