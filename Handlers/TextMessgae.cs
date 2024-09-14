using KoriMiyohashi.Modules;
using KoriMiyohashi.Modules.Types;
using MamoLib.StringHelper;
using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace KoriMiyohashi.Handlers
{
    public class TextMessgae : BaseHandler
    {
        public TextMessgae()
        {
            listener.RegisterTextMessage(OnTextMessage);
        }

        private async Task OnTextMessage(Message message, DbUser user, string arg3)
        {
            if (message.Chat.Type != Telegram.Bot.Types.Enums.ChatType.Private)
            {
                return;
            }
            var subs = GetUnfinish(user);
            Submission? sub = null;
            if (subs.Count() > 0)
                sub = subs.First();
            if (arg3.StartsWith("http"))
            {
                var parsed = new Song();
                Parser.ParseQQMusic(arg3, ref parsed);
                Parser.ParseNeteaseMusic(arg3, ref parsed);
                Parser.ParseSpotify(arg3,ref parsed);
                //存在投稿的情况
                if (sub != null)
                {
                    message.DeleteLater(1);
                    //发送链接，且存在投稿
                    Song song = new Song()
                    {
                        Title = parsed.Title,
                        Artist = parsed.Artist,
                        SubmissionId = sub.Id,
                        Link = string.IsNullOrEmpty(parsed.Link)?arg3: parsed.Link,
                    };
                    repos.Songs.Insertable(song).ExecuteCommand();
                    sub = GetSubmission(sub.Id);
                    await RefreshMainPage(message.Chat.Id, sub);
                    return;
                }
                //没有进行投稿
                else
                {
                    message.DeleteLater(1);
                    sub = new()
                    {
                        UserId = user.Id,
                    };
                    sub = repos.Submissions.Insertable(sub).ExecuteReturnEntity();
                    Song song = parsed;
                    song.SubmissionId = sub.Id;
                    song.Link = string.IsNullOrEmpty(parsed.Link) ? arg3 : parsed.Link;
                    repos.Songs.Insertable(song).ExecuteCommand();
                    sub = GetSubmission(sub.Id);
                    await RefreshMainPage(message.Chat.Id, sub);
                    return;
                }
            }
            // 正在进行投稿
            if (sub != null)
            {
                if (sub.Status == "Edit/Description")
                {
                    sub.Description = arg3;
                    sub.Status = "WAITING";
                    repos.Submissions.Storageable(sub).ExecuteCommand();
                    message.DeleteLater(1);
                    await RefreshMainPage(message.Chat.Id, sub);
                }
                else if(sub.Status.StartsWith("Edit/Song/"))
                {
                    var arg = sub.Status.Split('/');
                    var action = arg[2];
                    int songId = int.Parse(arg[3]);
                    Song song = repos.Songs.Queryable().Where(x => x.Id == songId).First();
                    switch (action)
                    {
                        case "Title":
                            song.Title = arg3;break;
                        case "Artist":
                            song.Artist = arg3; break;
                        case "Album":
                            //song. = arg3; break;
                        default:
                            break;
                    }
                    sub.Status = "WAITING";
                    repos.Submissions.Storageable(sub).ExecuteCommand();
                    repos.Songs.Storageable(song).ExecuteCommand();
                    message.DeleteLater(1);
                    await NavigateToSongPage(message.Chat.Id,sub,songId);
                }
                else if (sub.Status == "WAITING")
                {
                    message.DeleteLater(10);
                    var st = await bot.SendTextMessageAsync(message.Chat.Id, "没有需要文字交互的任务进行");
                    st.DeleteLater(10);
                }
                return;
            }
            message.DeleteLater(10);
            var st1 = await bot.SendTextMessageAsync(message.Chat.Id,$"可以使用以下方式投稿哦~\n\n" +
                $"- 发送音频文件(可在Telegram内直接播放)\n" +
                $"- 发送音乐平台分享链接\n" +
                $"- 使用 /newpost 创建空白投稿");
            st1.DeleteLater(30);
        }
    }
}
