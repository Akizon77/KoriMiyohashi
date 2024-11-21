using Telegram.Bot.Types;
using Telegram.Bot;

namespace MamoLib.TgExtensions
{
    public static class TgExtensions
    {
        public async static Task<Message> FastReply(this TelegramBotClient b, Message message, string text)
        {
            try
            {
                return await b.SendTextMessageAsync(message.Chat.Id, text, replyToMessageId: message.MessageId, parseMode: Telegram.Bot.Types.Enums.ParseMode.Html);
            }
            catch (Exception)
            {
                return await b.SendTextMessageAsync(message.Chat.Id, text, parseMode: Telegram.Bot.Types.Enums.ParseMode.Html);
            }
        }
        
        public static void DeleteLater(this TelegramBotClient b, Message message, int second = 15)
        {
            _ = Task.Run(async () =>
            {
                Thread.Sleep(TimeSpan.FromSeconds(second));
                await b.DeleteMessageAsync(message.Chat.Id, message.MessageId);
            });

        }
        public static string GetFullName(string? firstname, string? lastname, string fallback = "Blank")
        {
            if (string.IsNullOrWhiteSpace(firstname) && string.IsNullOrWhiteSpace(lastname))
            {
                return fallback;
            }
            firstname = firstname ?? "";
            lastname = lastname ?? "";
            return (firstname + " " + lastname).Trim();
        }
        public static string GetFullName(this Telegram.Bot.Types.User user)
        {
            return GetFullName(user.FirstName, user.LastName);
        }
    }
}
