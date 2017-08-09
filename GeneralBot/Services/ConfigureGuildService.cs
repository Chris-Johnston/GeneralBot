﻿using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using GeneralBot.Extensions;
using GeneralBot.Models.Database.CoreSettings;

namespace GeneralBot.Services
{
    /// <summary>
    ///     Configures guild registration entries in the database and welcome events.
    /// </summary>
    internal class ConfigureGuildService
    {
        private readonly CoreContext _coreSettings;
        private readonly LoggingService _loggingService;

        public ConfigureGuildService(DiscordSocketClient client, CoreContext coreSettings,
            LoggingService loggingService)
        {
            client.GuildAvailable += RegisterGuildAsync;
            client.JoinedGuild += RegisterGuildAsync;
            client.LeftGuild += UnregisterGuildAsync;
            client.UserJoined += WelcomeAsync;
            _coreSettings = coreSettings;
            _loggingService = loggingService;
        }

        private async Task RegisterGuildAsync(SocketGuild guild)
        {
            var dbEntry = _coreSettings.GuildsSettings.SingleOrDefault(x => x.GuildId == guild.Id);
            if (dbEntry != null) return;

            await _loggingService.LogAsync($"Guild {guild} ({guild.Id}) found, registering...", LogSeverity.Info);
            await _coreSettings.GuildsSettings.AddAsync(new GuildSettings {GuildId = guild.Id});
            await _coreSettings.SaveChangesAsync();
        }

        private async Task UnregisterGuildAsync(SocketGuild guild)
        {
            var guildSettings = _coreSettings.GuildsSettings.Where(x => x.GuildId == guild.Id);
            if (guildSettings == null) return;
            await _loggingService.LogAsync($"Left {guild} ({guild.Id}), unregistering...", LogSeverity.Info);
            _coreSettings.GuildsSettings.RemoveRange(guildSettings);
            await _coreSettings.SaveChangesAsync();
        }

        private async Task WelcomeAsync(SocketGuildUser user)
        {
            var guild = user.Guild;
            await _loggingService.LogAsync($"{user.GetFullnameOrDefault()} ({user.Id}) joined {guild} ({guild.Id}).",
                LogSeverity.Verbose);

            var dbEntry = _coreSettings.GreetingsSettings.SingleOrDefault(x => x.GuildId == guild.Id);
            if (dbEntry == null || !dbEntry.IsJoinEnabled) return;
            var channel = guild.GetTextChannel(dbEntry.ChannelId);
            if (channel == null) return;
            string formattedMessage = dbEntry.WelcomeMessage
                .Replace("{mention}", user.Mention)
                .Replace("{username}", user.Username)
                .Replace("{discrim}", user.Discriminator)
                .Replace("{guild}", guild.Name)
                .Replace("{date}", DateTime.UtcNow.ToString(CultureInfo.InvariantCulture));
            var _ = channel.SendMessageAsync(formattedMessage);
        }
    }
}