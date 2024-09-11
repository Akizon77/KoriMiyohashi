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
using System.Web;

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

        internal async Task<Message> SendGroupMedia(long chatId, IEnumerable<InputMediaAudio> media,
            string caption = "",
            ParseMode parseMode = ParseMode.Html,
            InlineKeyboardMarkup? inlineKeyboardMarkup = null)
        {
            if (media.Count() == 1)
            {
                InputMediaAudio? audio = media.First();
                return await bot.SendAudioAsync(chatId, audio.Media, caption: caption, parseMode: parseMode, replyMarkup: inlineKeyboardMarkup);
            }

            media.Last().Caption = caption;
            media.Last().ParseMode = parseMode;
            var messages = await bot.SendMediaGroupAsync(chatId, media);
            _ =  bot.EditMessageReplyMarkupAsync(chatId, messages.Last().MessageId, inlineKeyboardMarkup);
            return messages.First();
        }

        internal async Task<Message> RefreshMainPage(long chatId, Submission sub)
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

        internal async Task<Message> SendOneAudioOrText(long chatId,List<Song> songs, string textOrCaption = "", ParseMode parseMode = ParseMode.Html,IReplyMarkup? replyMarkup = null)
        {
            string? fileIds = null;

            foreach (var item in songs)
            {
                if (item.FileId != null)
                {
                    fileIds = item.FileId;
                    break;
                }
            }
            Message st;
            if (String.IsNullOrEmpty(fileIds))
            {
                st = await bot.SendTextMessageAsync(chatId,
                    textOrCaption,
                    replyMarkup: replyMarkup,
                    parseMode: Telegram.Bot.Types.Enums.ParseMode.Html);
            }
            else
            {
                st = await bot.SendAudioAsync(chatId,
                    InputFile.FromFileId(fileIds),
                    caption: textOrCaption,
                    parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                    replyMarkup: replyMarkup);
            }
            return st;
        }

        internal async Task<Message> NavigateToSongPage(long chatId, Submission sub, int songID)
        {
            Song song = repos.Songs.Queryable().Where(x => x.Id == songID).First();

            var text = $"标题: <code>{song.Title}</code>\n" +
                $"艺术家: <code>{song.Artist}</code>\n";

            Dictionary<string, string> dic;
            if (string.IsNullOrEmpty(song.FileId))
            {
                dic = new()
                {
                    { "➕ 添加文件",$"edit/song/addFile/{songID}" },
                    { "🗑 移除此曲目",$"edit/song/delete/{songID}" },
                };
                text = "<b>链接投稿</b>\n\n" + text + $"链接: {HttpUtility.HtmlAttributeEncode(song.Link)}";
            }
            else
            {
                dic = new(){
                    { "📤 发送文件",$"send/song/{song.Id}" },
                    { "🗑 移除此曲目",$"edit/song/delete/{songID}" },
                };
                text = "<b>文件投稿</b>\n\n" + text + $"文件ID:\n<code>{song.FileId}</code>";
                if (!string.IsNullOrEmpty(song.Link))
                    text +=  $"\n链接: {HttpUtility.HtmlAttributeEncode(song.Link)}";
                
            }

            var inline = FastGenerator.GeneratorInlineButton([
                new(){
                    { "◀️ 返回","page/song" }
                },
                new (){
                    { "修改标题",$"edit/song/title/{songID}" },
                    { "修改艺术家",$"edit/song/artist/{songID}" },
                    //{ "修改专辑",$"edit/song/album/{songID}" },
                },
                dic
            ]);

            var st = await bot.SendTextMessageAsync(chatId, text,
                replyMarkup: inline,
                parseMode: ParseMode.Html,
                disableWebPagePreview: true);

            _ = bot.DeleteMessageAsync(chatId, sub.SubmissionMessageId);

            sub.SubmissionMessageId = st.MessageId;

            repos.Submissions.Storageable(sub).ExecuteCommand();

            return st;
        }

        internal async Task<Message> Publish(long chatId, Submission sub)
        {
            if (sub.Songs.Count == 0)
            {
                var t = "请至少添加一首单曲哦~\n\n直接发送音频文件 或者 发送音乐平台分享链接即可哦";
                var st2 = await bot.SendTextMessageAsync(chatId, t);
                st2.DeleteLater();
                return st2;
            }
            List<String> fileIds = new();
            for (var i = 0; i < sub.Songs.Count; i++)
            {
                if (!string.IsNullOrEmpty(sub.Songs[i].FileId))
                    fileIds.Add(sub.Songs[i].FileId!);
            }

            if (fileIds.Count == 0)
            {
                return await bot.SendTextMessageAsync(chatId, sub.ToPubHtmlString(),
                    parseMode: ParseMode.Html);
            }
            else if (fileIds.Count == 1)
            {
                return await bot.SendAudioAsync(chatId, InputFile.FromFileId(fileIds[0]),
                    parseMode:ParseMode.Html, 
                    caption: sub.ToPubHtmlString());
            }
            else //if (sub.Songs.Count > 1)
            {
                List<InputMediaAudio> inputMediaAudio = new();
                foreach (var s in fileIds)
                {
                    inputMediaAudio.Add(new InputMediaAudio(InputFile.FromFileId(s)));
                }
                return await SendGroupMedia(chatId, inputMediaAudio,sub.ToPubHtmlString());
            }
        }

        internal async Task<Message> Aduit(long chatId, Submission sub)
        {
            throw new NotImplementedException();
        }
    }
}