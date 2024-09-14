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
            Song song = new Song()
            {
                Title = audio.Title ?? "无标题",
                Artist = audio.Performer ?? "无艺术家",
                FileId = audio.FileId,
            };
            //群组内审核消息
            if (message.Chat.Type == Telegram.Bot.Types.Enums.ChatType.Group || message.Chat.Type == Telegram.Bot.Types.Enums.ChatType.Supergroup)
            {
                if (!user.Aduit)
                {
                    message.FastReply("权限不足").Result.DeleteLater();
                    return;
                }
                //群组内回复消息
                if (message.ReplyToMessage == null)
                    return;
                var sub = repos.Submissions.Queryable()
                    .Where(x => x.GroupMessageId == message.ReplyToMessage.MessageId)
                    .Includes(x => x.Songs)
                    .Includes(x => x.User)
                    .First();
                // ADUIT/WAITING
                // ADUIT/ADDFILE/{songid}
                if (sub.Songs.Count == 1)
                {
                    if (string.IsNullOrEmpty(sub.Songs[0].FileId))
                        sub.Songs[0].FileId = audio.FileId;
                    else
                        return;
                    repos.Songs.Storageable(sub.Songs[0]).ExecuteCommand();
                    sub = GetSubmission(sub.Id);
                    await Approve(sub, user);
                    return;
                }
                if (sub.Status == "ADUIT/WAITING")
                {
                    
                    for (int i = 0; i < sub.Songs.Count; i++)
                    {
                        if (string.IsNullOrEmpty(sub.Songs[i].FileId))
                        {
                            sub.Songs[i].FileId = audio.FileId;
                            repos.Songs.Storageable(sub.Songs[i]).ExecuteCommand();
                            _ = message.FastReply($"{sub.Songs[i].Artist} - {sub.Songs[i].Title}\n已添加音频", replyMarkup: FastGenerator.GeneratorInlineButton([
                                new(){
                                    { "发错了?点击删除音频",$"aduit/delfile/{sub.Songs[i].Id}" }
                                }]));
                            var notFilled = sub.Songs.Where(x => string.IsNullOrEmpty(x.FileId)).ToList();
                            if (notFilled.Count == 0)
                                await Approve(sub, user);
                            return;
                        }
                    }
                }
                else if (sub.Status.StartsWith("ADUIT/ADDFILE/"))
                {
                    var songid = int.Parse(sub.Status.Split('/')[2]);
                    song = repos.Songs.Queryable().Where(x => x.Id == songid).First();
                    song.FileId = audio.FileId;
                    repos.Songs.Storageable(song).ExecuteCommand();
                    _ = message.FastReply($"{song.Artist} - {song.Title}\n已添加音频", replyMarkup: FastGenerator.GeneratorInlineButton([
                        new(){
                             { "发错了?点击删除音频",$"aduit/delfile/{sub.Id}/{song.Id}" }
                        }]));   
                    var notFilled = sub.Songs.Where(x => string.IsNullOrEmpty(x.FileId)).ToList();
                    if (notFilled.Count == 0)
                        await Approve(sub, user);
                    sub.Status = "ADUIT/WAITING";
                    repos.Submissions.Storageable(sub).ExecuteCommand();
                    return;
                }

            }
            message.DeleteLater(5);
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
                //为曲目补充文件
                if (sub.Status.StartsWith("Edit/Song/AddFile/"))
                {
                    int songId = int.Parse(sub.Status.Split('/')[3]);
                    song = repos.Songs.Queryable().Where(x => x.Id == songId).First();
                    song.FileId = audio.FileId;
                    repos.Songs.Storageable(song).ExecuteCommand();

                    sub.Status = "WAITING";
                    repos.Submissions.Storageable(sub).ExecuteCommand();
                    sub = GetSubmission(sub.Id);
                    await RefreshMainPage(message.Chat.Id,sub);
                    return;
                }
                //添加曲目
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
