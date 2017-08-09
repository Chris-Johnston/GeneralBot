using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Discord;

namespace GeneralBot.Models.Database.CoreSettings
{
    public class GuildSettings
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        [Required]
        public ulong GuildId { get; set; }

        [Required]
        public string CommandPrefix { get; set; } = "!";

        public ulong ReportChannel { get; set; } = 0;

        public bool IsInviteAllowed { get; set; } = true;

        public bool IsGfyCatEnabled { get; set; } = true;
        
        public GuildPermission ModeratorPermission { get; set; } = GuildPermission.KickMembers;
    }
}