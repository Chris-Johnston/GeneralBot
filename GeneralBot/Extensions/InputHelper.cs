﻿using System;
using System.Reflection;
using Discord;
using Discord.WebSocket;

namespace GeneralBot.Extensions
{
    public static class InputHelper
    {
        public static bool ContainsCaseInsensitive(this string originalString, string targetString) => originalString.IndexOf(targetString, StringComparison.CurrentCultureIgnoreCase) != -1;

        public static Color GetRandomColor()
        {
            var random = new Random();
            var fields = typeof(Discord.Color).GetFields();
            var color = (Discord.Color) fields[random.Next(0, fields.Length)].GetValue(null);
            return color;
        }

        public static float GetLuminanceFromColor(byte r, byte g, byte b)
        {
            return (0.2126f * r + 0.7152f * g + 0.0722f * b);
        }
    }
}