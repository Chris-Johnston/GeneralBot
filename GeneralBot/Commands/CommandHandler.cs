﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using GeneralBot.Commands.Results;
using GeneralBot.Extensions;
using GeneralBot.Extensions.Helpers;
using GeneralBot.Models.Database.CoreSettings;
using GeneralBot.Services;

namespace GeneralBot.Commands
{
    /// <summary>
    ///     Handler responsible for pre-post command processing, as well as the initialization of <see cref="CommandService" />
    ///     related services.
    /// </summary>
    internal class CommandHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commandService;
        private readonly ICoreRepository _coreSettings;
        private readonly LoggingService _loggingService;
        private readonly IServiceProvider _services;

        public CommandHandler(IServiceProvider services, DiscordSocketClient client, CommandService commandService,
            ICoreRepository settings, LoggingService loggingService)
        {
            _services = services;
            _client = client;
            _commandService = commandService;
            _coreSettings = settings;
            _loggingService = loggingService;
            _client.MessageReceived += CommandHandleAsync;
            _commandService.CommandExecuted += OnCommandExecutedAsync;
        }

        /// <summary>
        ///     <see cref="CommandService" /> initializing. This includes discovery of <see cref="TypeReader" /> and
        ///     <see cref="ModuleBase{T}" />.
        /// </summary>
        public async Task InitAsync()
        {
            // Begin TypeReader discovery & creation.
            // Query for TypeReaders.
            var typeReaders = Assembly.GetEntryAssembly().GetTypes()
                .Where(x => x.GetTypeInfo().BaseType == typeof(TypeReader));
            // Query for method.
            var typeReaderMethod = _commandService.GetType().GetMethods()
                .FirstOrDefault(x => x.ContainsGenericParameters & (x.Name == "AddTypeReader"));
            if (typeReaderMethod == null)
                throw new InvalidOperationException("TypeReader loader method cannot be found.");
            var typeReaderList = new List<string>();
            foreach (var typeReader in typeReaders)
            {
                // Get the static Type property set in the TypeReader.
                var typeReaderType = typeReader.GetProperty("Types")?.GetValue(null);
                if (typeReaderType is Type[] targetTypes)
                {
                    foreach (var targetType in targetTypes)
                    {
                        var typeReaderMethodGeneric = typeReaderMethod.MakeGenericMethod(targetType);
                        typeReaderMethodGeneric.Invoke(_commandService, new[] {Activator.CreateInstance(typeReader)});
                        typeReaderList.Add($"TypeReader: {typeReader.Name} | Type: {targetType}");
                    }
                }
                else
                {
                    await _loggingService.LogAsync(
                        $"{typeReader} does not have a {nameof(Type)} array specified, ignoring...",
                        LogSeverity.Warning);
                }
            }
            await _loggingService.LogAsync(
                $"{typeReaderList.Count} {nameof(TypeReader)}(s) loaded." + Environment.NewLine +
                string.Join(Environment.NewLine, typeReaderList), LogSeverity.Verbose).ConfigureAwait(false);

            // Command module discovery.
            await _commandService.AddModulesAsync(Assembly.GetEntryAssembly());
        }

        /// <summary>
        ///     Pre-command execution handling.
        /// </summary>
        private async Task CommandHandleAsync(SocketMessage msgArg)
        {
            // Bail when it's not an user message.
            var msg = msgArg as SocketUserMessage;
            if (msg == null) return;
            // Also bail if it's a bot message to avoid "certain" situations.
            if (msg.Author.IsBot) return;

            int argPos = 0;
            string prefix = "!";
            // Checks if the channel is a guild channel, if so, attempts to fetch its prefix.
            if (msg.Channel is SocketGuildChannel guildChannel)
            {
                var record = await _coreSettings.GetOrCreateGuildSettingsAsync(guildChannel.Guild);
                prefix = record.CommandPrefix;
            }
            if (!msg.HasStringPrefix(prefix, ref argPos)) return;

            var context = new SocketCommandContext(_client, msg);
            await _commandService.ExecuteAsync(context, argPos, _services);
        }

        /// <summary>
        ///     Post-command execution handling.
        /// </summary>
        private async Task OnCommandExecutedAsync(CommandInfo commandInfo, ICommandContext context, IResult result)
        {
            // Bails if the generic result doesn't have an error reason, or if it's an unknown command error.
            if (string.IsNullOrEmpty(result.ErrorReason) || result.Error == CommandError.UnknownCommand) return;
            var embed = new EmbedBuilder();
            switch (result)
            {
                case CommandRuntimeResult customResult:
                    switch (customResult.Type)
                    {
                        case ResultType.Unknown:
                            break;
                        case ResultType.Info:
                            embed = EmbedHelper.FromInfo(description: customResult.Reason);
                            break;
                        case ResultType.Warning:
                            embed = EmbedHelper.FromWarning(description: customResult.Reason);
                            break;
                        case ResultType.Error:
                            embed = EmbedHelper.FromError(description: customResult.Reason);
                            break;
                        case ResultType.Success:
                            embed = EmbedHelper.FromSuccess(description: customResult.Reason);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    break;
                default:
                    embed = EmbedHelper.FromError(description: result.ErrorReason);
                    break;
            }
            await context.Channel.SendMessageAsync("", embed: embed);
            await _loggingService.LogAsync(
                $"{context.User} executed \"{commandInfo.Aliases.FirstOrDefault()}\" in {context.Message.GetPostedAt()}." +
                Environment.NewLine +
                $"Result: {result.ErrorReason}" +
                Environment.NewLine +
                $"Result Type: {result.GetType().Name}",
                LogSeverity.Verbose).ConfigureAwait(false);
        }
    }
}