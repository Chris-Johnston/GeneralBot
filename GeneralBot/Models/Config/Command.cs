using System.Collections.Generic;

namespace GeneralBot.Models.Config
{
    public class Command
    {
        public List<string> EightBallResponses { get; set; } = new List<string>
        {
            "It is certain.",
            "It is decidedly so.",
            "Without a doubt!",
            "Yes, definitely!",
            "You may rely on it.",
            "As I see it, yes.",
            "Most likely.",
            "Outlook good.",
            "Yes.",
            "Signs point to yes.",
            "Don't count on it.",
            "My reply is no.",
            "My sources say no.",
            "Outlook not so good.",
            "Very doubtful."
        };
    }
}