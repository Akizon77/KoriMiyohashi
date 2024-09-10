using KoriMiyohashi.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot;
using KoriMiyohashi.Modules.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace KoriMiyohashi.Handlers
{
    public class BaseHandler
    {
        internal Listener listener => Hosting.GetRequiredService<Listener>();
        internal TelegramBotClient bot => listener.BotClient;
        internal Repos repos => Hosting.GetRequiredService<Repos>();

        internal IEnumerable<Submission> GetUnfinish(DbUser user)
        {
            var unfinished = repos.Submissions.Queryable()
                .Includes(x => x.User)
                .Includes(x => x.Songs)
                .Where(x => x.Status != "DONE")
                .Where(x => x.Status != "CANCEL")
                .Where(x => x.UserId == user.Id).ToList();
            return unfinished;
        }
        internal Submission GetSubmission(int id) 
        {
            var unfinished = repos.Submissions.Queryable()
                .Includes(x => x.User)
                .Includes(x => x.Songs)
                .Where(x => x.Id == id).First();
            return unfinished;
        }
        internal async Task<Message> SendGroupMedia(long chatId,IEnumerable<InputMediaAudio> media,
            string caption = "", 
            ParseMode parseMode = ParseMode.Html,
            InlineKeyboardMarkup? inlineKeyboardMarkup = null)
        {
            if (media.Count() == 1)
            {
                InputMediaAudio? audio = media.First();
                return await bot.SendAudioAsync(chatId, audio.Media,caption:caption,parseMode:parseMode,replyMarkup:inlineKeyboardMarkup);
            }

            media.Last().Caption = caption;
            media.Last().ParseMode = parseMode;
            var messages =  await bot.SendMediaGroupAsync(chatId, media);
            await bot.EditMessageReplyMarkupAsync(chatId, messages.Last().MessageId, inlineKeyboardMarkup);
            return messages.First();
        }

        internal async Task<Message> RefreshMainPage(long chatId,Submission sub)
        {
            string? fileIds = null;

            foreach (var item in sub.Songs)
            {
                if (item.FileId != null)
                {
                    fileIds = item.FileId;
                    break;
                }
            }

            _ = bot.DeleteMessageAsync(chatId, sub.SubmissionMessageId);
            Message st;
            if (String.IsNullOrEmpty(fileIds))
            {
                st = await bot.SendTextMessageAsync(chatId,
                    sub.ToHtmlString(),
                    replyMarkup: FastGenerator.DefaultSubmissionMarkup(),
                    parseMode: Telegram.Bot.Types.Enums.ParseMode.Html);
            }
            else
            {
                st = await bot.SendAudioAsync(chatId,
                    InputFile.FromFileId(fileIds),
                    caption: sub.ToHtmlString(),
                    parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                    replyMarkup: FastGenerator.DefaultSubmissionMarkup());
            }

            sub.SubmissionMessageId = st.MessageId;
            repos.Submissions.Storageable(sub).ExecuteCommand();
            return st;
        }


    }
}
