using KoriMiyohashi.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot;
using KoriMiyohashi.Modules.Types;

namespace KoriMiyohashi.Handlers
{
    public class Audios : BaseHandler
    {
        public Audios()
        {
            listener.RegisterAudioMessage(OnAudio);
        }

        private async Task OnAudio(Audio audio, DbUser user, string arg3, Message message)
        {
            message.DeleteLater(5);
            Song song = new Song()
            {
                Title = audio.Title ?? "无标题",
                Artist = audio.Performer ?? "无艺术家",
                FileId = audio.FileId,
            };
            var unfinish = GetUnfinish(user);
            //没有正在进行的投稿
            if (unfinish.Count() == 0)
            {
                Submission sub = new()
                {
                    UserId = user.Id,
                };
                var subid = repos.Submissions.Insertable(sub).ExecuteReturnIdentity();
                song.SubmissionId = subid;
                repos.Songs.Insertable(song).ExecuteCommand();
                sub = GetSubmission(subid);

                var st = await bot.SendAudioAsync(
                    message.Chat.Id,InputFile.FromFileId(song.FileId),
                    caption:"欢迎使用音频文件投稿\n" + sub.ToHtmlString(),
                    parseMode:Telegram.Bot.Types.Enums.ParseMode.Html,
                    replyMarkup:FastGenerator.DefaultSubmissionMarkup());

                sub.SubmissionMessageId = st.MessageId;
                repos.Submissions.Storageable(sub).ExecuteCommand();

            }
            //存在进行中的投稿
            else
            {
                Submission sub = unfinish.FirstOrDefault()!;
                song.SubmissionId = sub.Id;
                repos.Songs.Insertable(song).ExecuteCommand();
                sub = GetSubmission(sub.Id);
                string? fileIds = null;

                foreach (var item in sub.Songs)
                {
                    if (item.FileId != null)
                    {
                        fileIds = item.FileId;
                        break;
                    }
                }

                _ = bot.DeleteMessageAsync(message.Chat.Id, sub.SubmissionMessageId);
                Message st;
                if (String.IsNullOrEmpty(fileIds))
                {
                    st = await bot.SendTextMessageAsync(message.Chat.Id,
                        sub.ToHtmlString(),
                        replyMarkup: FastGenerator.DefaultSubmissionMarkup(),
                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html);
                }
                else
                {
                    st = await bot.SendAudioAsync(message.Chat.Id,
                        InputFile.FromFileId(fileIds),
                        caption:sub.ToHtmlString(),
                        parseMode:Telegram.Bot.Types.Enums.ParseMode.Html,
                        replyMarkup:FastGenerator.DefaultSubmissionMarkup());
                }

                sub.SubmissionMessageId = st.MessageId;
                repos.Submissions.Storageable(sub).ExecuteCommand();
            }
        }
    }
}
