using KoriMiyohashi.Modules;
using KoriMiyohashi.Modules.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace KoriMiyohashi.Handlers
{
    public class Commands :  BaseHandler
    {
        public Commands()
        {
            listener.RegisterCommand("about", AboutCommand, "关于");
            listener.RegisterCommand("start", AboutCommand);
            listener.RegisterCommand("newpost", NewPostCommand, "新投稿");
            listener.RegisterCommand("cancel", CancelPostCommand, "取消投稿");
            listener.RegisterCommand("echo", EchoCommand, "传话",false);
        }

        private async Task EchoCommand(Message message, DbUser user, string arg3, string[] arg4)
        {
            var prevmsg = message.ReplyToMessage!;
            var sub = repos.Submissions.Queryable()
                .Includes(x => x.User)
                .Includes(x => x.Songs)
                .Where(x => x.GroupMessageId == prevmsg.MessageId)
                .First();
            try
            {
                if (!message.Text!.Contains(' ')) return;
                try
                {
                    await bot.SendTextMessageAsync(sub.UserId
                    , $"来自管理员的消息: {message.Text?.Split(' ', 2)[1]}", replyToMessageId: sub.SubmissionMessageId);
                }
                catch 
                {
                    await bot.SendTextMessageAsync(sub.UserId
                    , $"来自管理员的消息: {message.Text?.Split(' ', 2)[1]}");
                }
                
                _ = message.FastReply("消息已转发");
            }
            catch (NullReferenceException)
            {
            }
            
        }

        private async Task CancelPostCommand(Message message, DbUser user, string arg3, string[] arg4)
        {
            message.DeleteLater();
            var unfinished = GetUnfinish(user).ToList();
            if (unfinished.Count == 0)
            {
                var stw = await message.FastReply("没有正在进行的投稿");
                stw.DeleteLater();
                return;
            }

            foreach (var item in unfinished)
            {
                item.Status = "CANCEL";
                _ = bot.DeleteMessageAsync(message.Chat.Id, item.SubmissionMessageId);
            }
            repos.Submissions.Storageable(unfinished).ExecuteCommand();
            var st = await message.FastReply("已取消投稿任务");
            st.DeleteLater();

        }

        private async Task NewPostCommand(Message message, DbUser user, string arg3, string[] arg4)
        {
            message.DeleteLater(1);
            var unfinished = GetUnfinish(user);
            if (unfinished.Count() != 0)
            {
                var text = $"你还有未完成的投稿！";
                var sent = await bot.SendTextMessageAsync(
                    message.Chat.Id, text,
                    parseMode: Telegram.Bot.Types.Enums.ParseMode.Html);
                //删除上次的消息
                sent.DeleteLater(5);
                await RefreshMainPage(message.Chat.Id, unfinished.First());
                return;
            }

            Submission submission = new() { UserId = user.Id, Status = "WAITING" };
            var subid = repos.Submissions.Insertable(submission).ExecuteReturnIdentity();
            submission = repos.Submissions.Queryable().Where(x => x.Id == subid).Includes(x => x.User).Includes(x => x.Songs).First();
            var st = await bot.SendTextMessageAsync(
                message.Chat.Id, submission.ToHtmlString(),
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                replyMarkup: FastGenerator.DefaultSubmissionMarkup());
            submission.SubmissionMessageId = st.MessageId;
            repos.Submissions.Storageable(submission).ExecuteCommand();

        }

        private async Task AboutCommand(Message message, DbUser user, string command, string[] args)
        {
            var text = $"<code>Kori Miyohashi</code> 是由 <a href=\"https://t.me/AkizonChan\">Mamo</a> 开发的 Telegram 频道投稿机器人。\n\n<code>Kori Miyohashi(聖代橋氷織)</code> 是 <code>Recette</code> 的游戏 <a href=\"https://store.steampowered.com/app/2374590/Sugar_Sweet_Temptation/?l=schinese\">しゅがてん！-sugarfull tempering- </a> 及其衍生作品的登场角色。\n\n当前版本 <code>{AppInfo.Version}</code>";
            var sent = await bot.SendTextMessageAsync(message.Chat.Id, text,
                disableWebPagePreview: true,
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Html, replyToMessageId: message.MessageId);
            message.DeleteLater(1);
            sent.DeleteLater(60);

            if (command == "start")
            {
                _ = bot.SendTextMessageAsync(message.Chat.Id, "发送音频文件 (Telegram内可直接播放) 或者使用 /newpost 开始投稿。",
                disableWebPagePreview: true,
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Html, replyToMessageId: message.MessageId);
            }
        }
    }
}