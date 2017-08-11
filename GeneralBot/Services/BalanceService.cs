﻿using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using GeneralBot.Models.Database.UserSettings;

namespace GeneralBot.Services
{
    public class BalanceService
    {
        private readonly LoggingService _loggingService;
        private readonly Random _random;
        private readonly IUserRepository _userSettings;

        public BalanceService(DiscordSocketClient client, IUserRepository usersettings, LoggingService loggingService,
            Random random)
        {
            client.MessageReceived += AwardBalanceAsync;
            _userSettings = usersettings;
            _loggingService = loggingService;
            _random = random;
        }

        private async Task AwardBalanceAsync(SocketMessage msgArg)
        {
            if (msgArg is SocketUserMessage msg &&
                msg.Channel is SocketGuildChannel &&
                !msg.Author.IsBot)
            {
                var user = msg.Author;
                var record = await _userSettings.GetOrCreateProfileAsync(user);
                uint balanceIncrement = Convert.ToUInt32(_random.Next(1, 10));
                if (msg.Timestamp >= record.LastMessage.AddMinutes(1))
                {
                    await _loggingService.LogAsync($"Increasing {user}'s balance by {balanceIncrement}...",
                        LogSeverity.Debug).ConfigureAwait(false);
                    record.LastMessage = msg.Timestamp;
                    record.Balance = record.Balance + balanceIncrement;
                    await _userSettings.SaveRepositoryAsync();
                }
            }
        }
    }
}