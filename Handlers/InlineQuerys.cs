using KoriMiyohashi.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot;
using KoriMiyohashi.Modules.Types;
using MamoLib.StringHelper;
using Telegram.Bot.Types.ReplyMarkups;

namespace KoriMiyohashi.Handlers
{
    public class InlineQuerys : BaseHandler
    {

        public InlineQuerys()
        {
            listener.RegisterInlineQuery("edit", EditQuery);
            listener.RegisterInlineQuery("add", AddQuery);
            listener.RegisterInlineQuery("page", PageQuery);
            listener.RegisterInlineQuery("switch", SwitchQuery);
            listener.RegisterInlineQuery("", DefaultQuery);
        }

        private async Task DefaultQuery(CallbackQuery query, DbUser user, string arg3)
        {
            switch (arg3)
            {
                case "submit":
                case "preview":
                default:
                    break;
            }
        }

        private async Task SwitchQuery(CallbackQuery query, DbUser user, string data)
        {
            var sub = repos.Submissions.Queryable()
                .Includes(x => x.User)
                .Includes(x => x.Songs)
                .Where(x => x.Status != "Done")
                .Where(x => x.Status != "CANCEL")
                .Where(x => x.UserId == user.Id).First();
            if (sub is null)
            {
                var stw = await query.Message!.FastReply("没有正在进行的投稿");
                query.Message!.DeleteLater();
                stw.DeleteLater();
                return;
            }
            switch (data)
            {
                case "switch/anonymous":
                    if (sub.Anonymous)
                        sub.Anonymous = false;
                    else
                        sub.Anonymous = true;
                    break;
            }
            repos.Submissions.Storageable(sub).ExecuteCommand();
            await RefreshMainPage(query.Message!.Chat.Id, sub);
        }

        private async Task PageQuery(CallbackQuery query, DbUser user, string data)
        {
            var sub = repos.Submissions.Queryable()
                .Includes(x => x.User)
                .Includes(x => x.Songs)
                .Where(x => x.Status != "Done")
                .Where(x => x.Status != "CANCEL")
                .Where(x => x.UserId == user.Id).First();
            if (sub is null)
            {
                var stw = await query.Message!.FastReply("没有正在进行的投稿");
                query.Message!.DeleteLater();
                stw.DeleteLater();
                return;
            }
            switch (data)
            {
                case "page/tags":
                    var markup = FastGenerator.GeneratorInlineButton([
                        new (){
                            { "🥇推荐","edit/tags/recommand"},
                            { "🎁赠予","edit/tags/gift"},
                            { "💬留言","edit/tags/message"},
                        }
                        ]);
                    _ = bot.EditMessageReplyMarkupAsync(query.Message!.Chat.Id, query.Message.MessageId,
                        markup);
                    break;
                case "page/main":
                    await RefreshMainPage(query.Message!.Chat.Id,sub);
                    break;
                case "page/song":
                    string text = $"当前投稿包含 <code>{sub.Songs.Count}</code> 个曲目\n\n" +
                        $"如需添加曲目，请发送<b>音频文件</b>或者以 <b>http</b> 开头的链接\n";
                    InlineKeyboardMarkup inline;
                    if (sub.Songs.Count == 0)
                    {
                        inline = FastGenerator.GeneratorInlineButton([
                            new(){
                                { "◀️ 回到投稿预览页","page/main"}
                            },
                        ]);
                    }
                    else
                    {
                        var songs = new Dictionary<string, string>();
                        for (int i = 0; i < sub.Songs.Count; i++)
                        {
                            songs[$"{i+1}"] = $"page/songdetail/{i}";
                            text += $"\n{i+1} - {sub.Songs[i].Title}";
                        }
                        inline = FastGenerator.GeneratorInlineButton([
                            new(){
                                { "◀️ 回到投稿预览页","page/main"}
                            },
                            songs
                        ]);
                    }

                    _ = bot.DeleteMessageAsync(query.Message!.Chat.Id, sub.SubmissionMessageId);
                    var st = await bot.SendTextMessageAsync(
                         query.Message!.Chat.Id,
                         text + "\n\n曲目数量请控制在8以内，超过8个曲目将无法识别",
                         parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                         replyMarkup: inline);
                    sub.SubmissionMessageId = st.MessageId;
                    repos.Submissions.Storageable(sub).ExecuteCommand();
                    break;
                default:
                    // page/songdetail
                    if (data.StartsWith("page/songdetail/"))
                    {
                        var songIdInSub = int.Parse(data.Split('/')[2]);
                        Song song = sub.Songs[songIdInSub];
                        await NavigateToSongPage(query.Message!.Chat.Id, sub, song.Id);
                        return;
                    }
                    break;
            }
        }

        private async Task AddQuery(CallbackQuery query, DbUser user, string data)
        {
            var sub = repos.Submissions.Queryable()
                .Includes(x => x.User)
                .Includes(x => x.Songs)
                .Where(x => x.Status != "Done")
                .Where(x => x.Status != "CANCEL")
                .Where(x => x.UserId == user.Id).First();
            if (sub is null)
            {
                var stw = await query.Message!.FastReply("没有正在进行的投稿");
                query.Message!.DeleteLater();
                stw.DeleteLater();
                return;
            }


            var paths = data.Split('/');
            switch (paths[1])
            {
                case "page":

                default:
                    Log.Warning("Unknown query {@0}", query);
                    break;
            }
        }

        private async Task EditQuery(CallbackQuery query, DbUser user, string data)
        {
            var sub = repos.Submissions.Queryable()
                .Includes(x => x.User)
                .Includes(x => x.Songs)
                .Where(x => x.Status != "Done")
                .Where(x => x.Status != "CANCEL")
                .Where(x => x.UserId == user.Id).First();
            if (sub is null)
            {
                var stw = await query.Message!.FastReply("没有正在进行的投稿");
                query.Message!.DeleteLater();
                stw.DeleteLater();
                return;
            }
            switch (data)
            {
                case "edit/tags/recommand":
                    sub.Tags = "推荐";
                    break;
                case "edit/tags/gift":
                    sub.Tags = "赠予";
                    break;
                case "edit/tags/message":
                    sub.Tags = "留言";
                    break;
                case "edit/tags/daily":
                    sub.Tags = "每日推荐";
                    break;
                case "edit/description":
                    sub.Status = "Edit/Description";
                    var tx = await bot.SendTextMessageAsync(query.Message!.Chat.Id, "发送一段话补充推荐理由吧");
                    repos.Submissions.Storageable(sub).ExecuteCommand();
                    tx.DeleteLater(10);
                    return;
                default:
                    break;
            }
            repos.Submissions.Storageable(sub).ExecuteCommand();
            await RefreshMainPage(query.Message!.Chat.Id, sub);
        }
    }
}
