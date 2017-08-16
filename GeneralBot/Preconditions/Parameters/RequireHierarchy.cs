﻿using System;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;

namespace GeneralBot.Preconditions.Parameters
{
    /// <summary>
    /// Requires the user's, bot's hierarchy to be higher than the targeted user to execute.
    /// </summary>
    public class RequireHierarchy : ParameterPreconditionAttribute
    {
        public override async Task<PreconditionResult> CheckPermissions(ICommandContext context,
            ParameterInfo parameter, object value, IServiceProvider services)
        {
            var user = context.User as SocketGuildUser;
            if (context.Guild == null || user == null)
                return PreconditionResult.FromError("This command cannot be used outside of a guild.");

            SocketGuildUser targetUser = null;
            if (value is SocketGuildUser targetGuildUser) targetUser = targetGuildUser;
            if (value is ulong userId) targetUser = await context.Guild.GetUserAsync(userId).ConfigureAwait(false) as SocketGuildUser;
            if (targetUser == null)
                return PreconditionResult.FromSuccess();

            if (user.Hierarchy < targetUser.Hierarchy)
                return PreconditionResult.FromError("You cannot target anyone else whose roles are higher than yours.");

            var currentUser = await context.Guild.GetCurrentUserAsync().ConfigureAwait(false) as SocketGuildUser;
            if (currentUser?.Hierarchy < targetUser.Hierarchy)
                return PreconditionResult.FromError("The bot's role is lower than the targeted user.");

            return PreconditionResult.FromSuccess();
        }
    }
}