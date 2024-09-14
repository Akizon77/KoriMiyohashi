using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot;
using MamoLib.TgExtensions;
using KoriMiyohashi.Modules;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace KoriMiyohashi
{
    public static class Extensions
    {
        private static TelegramBotClient Bot { get; set; }

        static Extensions()
        {
            Bot = Hosting.GetRequiredService<Listener>().BotClient;
        }

        public static void DeleteLater(this Message message, int second = 15) => TgExtensions.DeleteLater(Bot, message, second);
        public async static Task<Message> FastReply(this Message message, string text, ParseMode parseMode = ParseMode.Html, IReplyMarkup? replyMarkup = null)
        {
            try
            {
                return await Bot.SendTextMessageAsync(message.Chat.Id, text, replyToMessageId: message.MessageId, parseMode: parseMode, replyMarkup: replyMarkup);
            }
            catch
            {
                return await Bot.SendTextMessageAsync(message.Chat.Id, text, parseMode: parseMode, replyMarkup: replyMarkup);
            }
        }

        public static async Task FastEdit(this Message message, string text)
        {
            using var _ = new Defer(_ => { });
            await Bot.EditMessageTextAsync(message.Chat.Id, message.MessageId, text, parseMode: ParseMode.Html);
        }


        /// <summary>
        /// 快速编辑消息
        /// </summary>
        /// <param name="message">要快速回复的消息对象。</param>
        /// <param name="reply">内联消息</param>
        /// <returns>一个任务对象，表示异步操作的完成。当操作完成时，任务将完成。</returns>
        public static async Task FastEdit(this Message message, InlineKeyboardMarkup reply)
        {
            using var _ = new Defer(_ => { });
            await Bot.EditMessageReplyMarkupAsync(message.Chat.Id, message.MessageId, replyMarkup: reply);
        }
        /// <summary>
        /// 快速编辑消息
        /// </summary>
        /// <param name="message">要快速回复的消息对象。</param>
        /// <param name="text">编辑内容</param>
        /// <param name="reply">内联消息</param>
        /// <returns>一个任务对象，表示异步操作的完成。当操作完成时，任务将完成。</returns>
        public static async Task FastEdit(this Message message, string text, InlineKeyboardMarkup reply)
        {
            using var _ = new Defer(_ => { });
            await Bot.EditMessageTextAsync(message.Chat.Id, message.MessageId, text, replyMarkup: reply, parseMode: ParseMode.Html);
        }
    }
}