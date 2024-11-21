using KoriMiyohashi.Modules.Types;
using MamoLib.StringHelper;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace KoriMiyohashi.Handlers
{
    public class Commands : BaseHandler
    {
        public Commands()
        {
            listener.RegisterCommand("about", AboutCommand, "关于");
            listener.RegisterCommand("start", AboutCommand);
            listener.RegisterCommand("newpost", NewPostCommand, "新投稿");
            listener.RegisterCommand("cancel", CancelPostCommand, "取消投稿");
            listener.RegisterCommand("echo", EchoCommand, "传话", false);
            listener.RegisterCommand("admins", AdminsCommand, "编辑管理员信息", false);
            listener.RegisterCommand("list", ListCommand, "列出未审核稿件", false);
        }

        private async Task ListCommand(Message message, DbUser user, string arg3, string[] arg4)
        {
            if (!user.Aduit)
            {
                message.FastReply("权限不足").Result.DeleteLater();
                message.DeleteLater();
                return;
            }

            string text = GetPage(0);
            List<InlineKeyboardButton> buttons = new();
            buttons.Add(InlineKeyboardButton.WithCallbackData("🔄 Refresh", "list/page/0"));
            if (GetUnfinish().Count() > 10)
                buttons.Add(InlineKeyboardButton.WithCallbackData("▶️ Next Page", "list/page/1"));
            if (text == "") text = "当前暂无未审核稿件";
            await bot.SendTextMessageAsync(message.Chat.Id, text, replyToMessageId: message.MessageId, parseMode: ParseMode.Html, replyMarkup: new InlineKeyboardMarkup(buttons.ToArray()));
            return;
        }

        private async Task AdminsCommand(Message message, DbUser sender, string command, string[] args)
        {
            if (!sender.Owner)
            {
                message.FastReply("权限不足").Result.DeleteLater();
                message.DeleteLater();
                return;
            }
            if (args.Count() < 2 && !args.Contains("list"))
            {
                Usage();
                return;
            }

            var action = args[0];

            if (action == "list")
            {
                List<DbUser> list = repos.DbUsers.Queryable().Where(x => x.Aduit == true).ToList();
                string text = "当前管理员列表:\n";
                foreach (var item in list)
                {
                    text += $"{item.FullName.HtmlEscape()}(<code>{item.Id}</code>)\n";
                }
                await message.FastReply(text);
                return;
            }

            long userid;
            DbUser? user;

            if (!long.TryParse(args[1], out userid))
            {
                Usage();
                return;
            }

            var l = repos.DbUsers.Queryable().Where(u => u.Id == userid).ToList();

            if (l.Count == 1)
            {
                user = l.First();
                switch (action)
                {
                    case "add":
                        user.Aduit = true;
                        repos.DbUsers.Storageable(user).ExecuteCommand();
                        await message.FastReply($"<a href=\"tg://user?id={user.Id}\">{user.FullName.HtmlEscape()}</a> 已拥有管理员权限");
                        return;

                    case "remove":
                        user.Aduit = false;
                        repos.DbUsers.Storageable(user).ExecuteCommand();
                        await message.FastReply($"<a href=\"tg://user?id={user.Id}\">{user.FullName.HtmlEscape()}</a> 不再拥有管理员权限");
                        return;

                    default:
                        Usage();
                        return;
                }
            }
            else
            {
                user = new DbUser()
                {
                    Id = userid,
                    FullName = $"{userid}",
                };
                switch (action)
                {
                    case "add":
                        user.Aduit = true;
                        repos.DbUsers.Storageable(user).ExecuteCommand();
                        await message.FastReply($"<a href=\"tg://user?id={user.Id}\">{user.FullName.HtmlEscape()}</a> 已拥有管理员权限");
                        return;

                    case "remove":
                        user.Aduit = false;
                        repos.DbUsers.Storageable(user).ExecuteCommand();
                        await message.FastReply($"<a href=\"tg://user?id={user.Id}\">{user.FullName.HtmlEscape()}</a> 不再拥有管理员权限");
                        return;

                    default:
                        Usage();
                        return;
                }
            }

            void Usage()
            {
                var me = bot.GetMeAsync().Result;
                message.FastReply($"<b>指令 {command}@{me.Username} 用法:</b>\n" +
                                    $"\n" +
                                    $"<code>/{command}@{me.Username} add {"<用户ID>".HtmlEscape()}</code> - 添加管理员\n" +
                                    $"<code>/{command}@{me.Username} remove {"<用户ID>".HtmlEscape()}</code> - 添加管理员\n" +
                                    $"<code>/{command}@{me.Username} list</code> - 添加管理员\n" +
                                    $"\n" +
                                    $"<b>示例</b>\n" +
                                    $"<code>/{command}@{me.Username} add {sender.Id}</code> - 添加{sender.FullName.HtmlEscape()}为管理员")
                                    .Result.DeleteLater();
                message.DeleteLater();
            }
        }

        private async Task EchoCommand(Message message, DbUser user, string arg3, string[] arg4)
        {
            if (!user.Aduit)
            {
                message.FastReply("权限不足").Result.DeleteLater();
                message.DeleteLater();
                return;
            }
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
            if (!user.Submit)
            {
                message.FastReply("权限不足").Result.DeleteLater();
                message.DeleteLater();
                return;
            }
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
            if (!user.Submit)
            {
                message.FastReply("权限不足").Result.DeleteLater();
                message.DeleteLater();
                return;
            }
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
            //message.DeleteLater(1);
            //sent.DeleteLater(60);

            if (command == "start")
            {
                _ = bot.SendTextMessageAsync(message.Chat.Id, "发送音频文件 (Telegram内可直接播放) 或者使用 /newpost 开始投稿。",
                disableWebPagePreview: true,
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Html, replyToMessageId: message.MessageId);
            }
        }
    }
}