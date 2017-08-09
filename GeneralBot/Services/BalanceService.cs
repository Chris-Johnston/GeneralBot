﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using GeneralBot.Models.Database.UserSettings;

namespace GeneralBot.Services
{
    public class BalanceService
    {
        private readonly LoggingService _loggingService;
        private readonly UserContext _userSettings;

        public BalanceService(DiscordSocketClient client, UserContext usersettings, LoggingService loggingService)
        {
            client.MessageReceived += AwardBalanceAsync;
            _userSettings = usersettings;
            _loggingService = loggingService;
        }

        private async Task AwardBalanceAsync(SocketMessage msgArg)
        {
            if (msgArg is SocketUserMessage msg &&
                msg.Channel is SocketGuildChannel &&
                !msg.Author.IsBot)
            {
                var user = msg.Author;
                var dbEntry = _userSettings.Profiles.SingleOrDefault(x => x.UserId == user.Id);
                uint balanceIncrement = Convert.ToUInt32(new Random().Next(1, 10));
                if (msg.Timestamp >= dbEntry.LastMessage.AddMinutes(1))
                {
                    await _loggingService.LogAsync($"Increasing {user}'s balance by {balanceIncrement}...",
                        LogSeverity.Debug).ConfigureAwait(false);
                    dbEntry.LastMessage = msg.Timestamp;
                    dbEntry.Balance = dbEntry.Balance + balanceIncrement;
                }
                _userSettings.Update(dbEntry);
                await _userSettings.SaveChangesAsync();
            }
        }
    }
}