﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using GeneralBot.Commands.Results;
using GeneralBot.Extensions;
using GeneralBot.Models.Config;
using GeneralBot.Models.Database.CoreSettings;
using Humanizer;

namespace GeneralBot.Commands.User
{
    public class InfoModule : ModuleBase<SocketCommandContext>
    {
        public ConfigModel Config { get; set; }
        public CoreContext CoreSettings { get; set; }

        [Command("invite")]
        [RequireContext(ContextType.Guild)]
        public async Task<RuntimeResult> GetOrCreateInviteAsync()
        {
            if (Context.Channel is SocketGuildChannel channel)
            {
                var dbEntry = CoreSettings.GuildsSettings.SingleOrDefault(x => x.GuildId == Context.Guild.Id) ??
                              CoreSettings.GuildsSettings.Add(new GuildSettings {GuildId = Context.Guild.Id}).Entity;
                if (!dbEntry.IsInviteAllowed) return CommandRuntimeResult.FromError("The admin has disabled this command.");
                var invite = await channel.GetLastInviteAsync(true);
                await ReplyAsync(invite.Url);
            }
            return CommandRuntimeResult.FromSuccess();
        }

        [Command("info")]
        public async Task<RuntimeResult> GetInfoAsync()
        {
            var embedBuilder = new EmbedBuilder
            {
                Author = new EmbedAuthorBuilder
                {
                    Name = "Bot Info:",
                    IconUrl = Context.Client.CurrentUser.GetAvatarUrlOrDefault()
                },
                ThumbnailUrl =
                    "https://emojipedia-us.s3.amazonaws.com/thumbs/120/twitter/103/information-source_2139.png",
                Color = new Color(61, 138, 192)
            };
            // Owners
            embedBuilder.AddField("Owners",
                string.Join(", ", Config.Owners.Select(x => Context.Client.GetUser(x).ToString())));

            // Application uptime
            var currentProcess = Process.GetCurrentProcess();
            embedBuilder.AddField("Uptime", (DateTime.Now - currentProcess.StartTime).Humanize());

            // Memory report
            var memInfoTitleBuilder = new StringBuilder();
            var memInfoDescriptionBuilder = new StringBuilder();
            var heapBytes = GC.GetTotalMemory(false).Bytes();
            memInfoTitleBuilder.Append("Heap Size");
            memInfoDescriptionBuilder.Append(
                $"{Math.Round(heapBytes.LargestWholeNumberValue, 2)} {heapBytes.LargestWholeNumberSymbol}");
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var workingSetBytes = currentProcess.WorkingSet64.Bytes();
                memInfoTitleBuilder.Append(" / Working Set");
                memInfoDescriptionBuilder.Append(
                    $" / {Math.Round(workingSetBytes.LargestWholeNumberValue, 2)} {workingSetBytes.LargestWholeNumberSymbol}");
            }
            embedBuilder.AddInlineField(memInfoTitleBuilder.ToString(), memInfoDescriptionBuilder);

            // Application latency
            embedBuilder.AddInlineField("Latency", Context.Client.Latency + "ms");

            // Discord application creation date
            var appInfo = await Context.Client.GetApplicationInfoAsync();
            embedBuilder.AddInlineField("Created On", appInfo.CreatedAt.UtcDateTime);

            // Last updated on based on file modification date
            embedBuilder.AddInlineField("Last Update",
                File.GetLastWriteTimeUtc(Assembly.GetEntryAssembly().Location));

            // Lib version
            embedBuilder.AddInlineField("Discord.NET Version", DiscordConfig.Version);
            await ReplyAsync("", embed: embedBuilder);
            return CommandRuntimeResult.FromSuccess();
        }

        [Group("help")]
        public class HelpModule : ModuleBase<SocketCommandContext>
        {
            public CoreContext CoreSettings { get; set; }
            public CommandService CommandService { get; set; }
            public IServiceProvider ServiceProvider { get; set; }

            [Command]
            [Summary("Need help for a specific command? Use this!")]
            public async Task<RuntimeResult> HelpSpecificCommandAsync(string input)
            {
                var commandInfos = await GetCommandInfosAsync(input);
                if (commandInfos.Count == 0)
                    return CommandRuntimeResult.FromError("I could not find any related commands!");

                var embed = new EmbedBuilder
                {
                    Author = new EmbedAuthorBuilder
                    {
                        Name = $"Here are some commands related to \"{input}\"...",
                        IconUrl = Context.Client.CurrentUser.GetAvatarUrlOrDefault()
                    }
                };
                foreach (var commandInfo in commandInfos)
                {
                    if (embed.Fields.Count > 5)
                    {
                        embed.AddInlineField($"And {commandInfos.Count - embed.Fields.Count} more...",
                            "Refine your search term to see more!");
                        break;
                    }
                    embed.AddField(x =>
                    {
                        x.Name = $"{GetCommandPrefix(Context.Guild)}{BuildCommandInfo(commandInfo)}";
                        x.Value = commandInfo.Summary ?? "No summary.";
                    });
                }
                await ReplyAsync("", embed: embed);
                return CommandRuntimeResult.FromSuccess();
            }

            private string GetCommandPrefix(SocketGuild guild) => guild == null
                ? "!"
                : CoreSettings.GuildsSettings.SingleOrDefault(x => x.GuildId == Context.Guild.Id).CommandPrefix;

            private static string BuildCommandInfo(CommandInfo cmdInfo) =>
                $"{cmdInfo.Aliases.First()} {cmdInfo.Parameters.GetParamsUsage()}";

            private async Task<IReadOnlyCollection<CommandInfo>> GetCommandInfosAsync(string input)
            {
                var commandInfos = new List<CommandInfo>();
                foreach (var module in CommandService.Modules)
                foreach (var command in module.Commands)
                {
                    var check = await command.CheckPreconditionsAsync(Context, ServiceProvider);
                    if (!check.IsSuccess) continue;
                    if (command.Aliases.Any(x => x.ContainsCaseInsensitive(input)) ||
                        module.IsSubmodule &&
                        module.Aliases.Any(x => x.ContainsCaseInsensitive(input)))
                        commandInfos.Add(command);
                }
                return commandInfos;
            }
        }
    }
}