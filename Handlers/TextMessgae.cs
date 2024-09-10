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
            var sub = repos.Submissions.Queryable()
                .Includes(x => x.User)
                .Includes(x => x.Songs)
                .Where(x => x.Status != "Done")
                .Where(x => x.Status != "CANCEL")
                .Where(x => x.UserId == user.Id).First();
            if (arg3.StartsWith("http"))
            {
                
                if (sub != null)
                {
                    //发送链接，且存在投稿
                    Song song = new Song()
                    {
                        SubmissionId = sub.Id,
                        Link = arg3
                    };
                    repos.Songs.Insertable(song).ExecuteCommand();
                    sub = GetSubmission(sub.Id);
                    message.DeleteLater(1);
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


        }
    }
}
