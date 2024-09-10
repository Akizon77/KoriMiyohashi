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

        private async Task DefaultQuery(CallbackQuery query, DbUser user, string data)
        {
            switch (data)
            {
                case "submit":
                    throw new NotImplementedException();
                case "preview":
                    var sub = GetUnfinish(user).First();

                    await Publish(query.Message!.Chat.Id, sub);
                    
                    break;
                default:
                    break;
            }
            if (data.StartsWith("send/song/"))
            {
                int songId = int.Parse(data.Split('/')[2]);
                Song song = repos.Songs.Queryable().Where(x => x.Id == songId).First();
                await bot.SendAudioAsync(query.Message!.Chat.Id,InputFile.FromFileId(song.FileId!));
                return;
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
                        $"如需添加曲目，请发送<b>音频文件</b>或者<b>音乐平台链接</b>。\n" +
                        $"目前可以自动识别并填充<b>QQ音乐</b>、<b>网易云音乐</b>的歌曲信息。\n";
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
            if (data.StartsWith("edit/song/"))
            {
                var arg = data.Split('/');
                var action = arg[2];
                var songId = int.Parse(arg[3]);

                var text = "";
                switch (action)
                {
                    case "title":
                        text = $"正在修改标题";
                        sub.Status = $"Edit/Song/Title/{songId}";break;
                    case "artist":
                        text = $"正在修改艺术家";
                        sub.Status = $"Edit/Song/Artist/{songId}";break;
                    case "album":
                        text = $"正在修改专辑";
                        sub.Status = $"Edit/Song/Album/{songId}"; break;
                    case "addFile":
                        text = $"正在补充文件";
                        sub.Status = $"Edit/Song/AddFile/{songId}"; break;
                    case "delete":
                        Song song = repos.Songs.Queryable().Where(x => x.Id == songId).First();
                        song.SubmissionId = 0;
                        await repos.Songs.Storageable(song).ExecuteCommandAsync();
                        text = $"已移除曲目 <code>{song.Title} - {song.Artist}</code>";

                        var st2 = await bot.SendTextMessageAsync(query.Message!.Chat.Id, text, parseMode: Telegram.Bot.Types.Enums.ParseMode.Html);
                        st2.DeleteLater();
                        await repos.Submissions.Storageable(sub).ExecuteCommandAsync();

                        sub = GetSubmission(sub.Id);

                        await RefreshMainPage(query.Message.Chat.Id, sub);
                        return;
                    default:
                        throw new InvalidOperationException($"无效的操作: {data}");
                }
                var st =await bot.SendTextMessageAsync(query.Message!.Chat.Id,text);
                st.DeleteLater();
                repos.Submissions.Storageable(sub).ExecuteCommand();
                return;
            }

            repos.Submissions.Storageable(sub).ExecuteCommand();
            await RefreshMainPage(query.Message!.Chat.Id, sub);
        }
    }
}
