using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot;
using MamoLib.TgExtensions;
using KoriMiyohashi.Modules;

namespace KoriMiyohashi
{
    public static class Extensions
    {
        private static TelegramBotClient Bot { get; set; }

        static Extensions()
        {
            Bot = Hosting.GetRequiredService<Listener>().BotClient;
        }

        public static void DeleteLater(this Message message, int second = 15) => TgExtensions.DeleteLater(Bot,message,second);
        public async static Task<Message> FastReply(this Message message, string text) => await TgExtensions.FastReply(Bot, message, text);
    }
}