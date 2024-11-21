using KoriMiyohashi.Modules;
using KoriMiyohashi.Modules.Types;
using MamoLib.TgExtensions;
using Microsoft.Extensions.Hosting;
using System.Web;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace KoriMiyohashi.Handlers
{
    public partial class InlineQuerys
    {
        private async Task AduitQuery(CallbackQuery query, DbUser user, string data)
        {
            if (!user.Aduit)
            {
                await bot.AnswerCallbackQueryAsync(query.Id, "权限不足",true);
                return;
            }
            //STATUS: ADUIT/WAITING
            var args = data.Split('/');
            var action = args[1];
            var subId = int.Parse(args[2]);
            var sub = GetSubmission(subId);
            var message = query.Message!;
            var chatid = message.Chat.Id;
            async Task FastAduit(string text, string status)
            {
                await bot.EditMessageReplyMarkupAsync(chatid, message.MessageId
                        , FastGenerator.GeneratorInlineButton([
                            new(){ { $"{query.From!.GetFullName()}({query.From.Id}): {text}",TimeStamp.GetNow().ToString()} }]));
                sub.Status = status;
                repos.Submissions.Storageable(sub).ExecuteCommand();
                sub = GetSubmission(sub.Id);
            }
            switch (action)
            {
                case "approve":
                    await Approve(sub,user);
                    return;
                case "reject":
                    await FastAduit("拒绝", "REJECTED");
                    try
                    {
                        await bot.SendTextMessageAsync(sub.UserId, $"感谢您的投稿！遗憾地通知您，您的稿件未能通过审核。", replyToMessageId: sub.SubmissionMessageId);
                    }
                    catch
                    {
                        await bot.SendTextMessageAsync(sub.UserId, $"感谢您的投稿！遗憾地通知您，您的稿件未能通过审核。");
                    }
                    return;
                case "slient":
                    await FastAduit("静默拒绝", "REJECTED");
                    return;
                case "details":
                    await Publish(query.Message!.Chat.Id, sub,replyTo: query.Message!.MessageId);
                    return ;
                case "addfile":
                    var dic = new Dictionary<string, string>();
                    for (int i = 0; i < sub.Songs.Count; i++)
                    {
                        if (string.IsNullOrEmpty(sub.Songs[i].FileId))
                            dic.Add((i+1).ToString(), $"aduit/song/{sub.Id}/{sub.Songs[i].Id}");
                    }
                    var inline = FastGenerator.GeneratorInlineButton([
                        new(){
                            {"⬅️ 返回","aduit/mainpage/" + sub.Id }
                        },
                        new(){
                            {"添加文件回复此消息即可",$"{TimeStamp.GetNow()}" }
                        },
                        new(){
                            {"未指定情况下将顺序补充",$"{TimeStamp.GetNow()}" }
                        },
                        dic
                        ]);
                    await bot.EditMessageReplyMarkupAsync(query.Message!.Chat.Id,query.Message.MessageId,inline);
                    return;
                case "delfile":
                    var songid0 = int.Parse(args[3]);
                    var song0 = repos.Songs.Queryable().Where(x => x.Id == songid0).First();
                    song0.FileId = null;
                    repos.Songs.Storageable(song0).ExecuteCommand();
                    query.Message!.DeleteLater(1);
                    return;
                case "mainpage":
                    await bot.EditMessageReplyMarkupAsync(query.Message!.Chat.Id, query.Message.MessageId, FastGenerator.DefaultAduitMarkup(sub));
                    return;
                case "song":
                    var songid = int.Parse(args[3]);
                    var song = repos.Songs.Queryable().Where(x => x.Id == songid).First();
                    var titleTrimmed = "";
                    var artistTrimmed = "";
                    if (song.Title.Length > 15) titleTrimmed = song.Title[..14];
                    else titleTrimmed = song.Title;
                    if (song.Artist.Length > 15) artistTrimmed = song.Artist[..14];
                    else artistTrimmed = song.Artist;

                    sub.Status = $"ADUIT/ADDFILE/{songid}";
                    repos.Submissions.Storageable(sub).ExecuteCommand();
                    // Update Sub (Optional)

                    List<List<InlineKeyboardButton>> keyboardInline = new();
                    keyboardInline.Add(new List<InlineKeyboardButton>() { InlineKeyboardButton.WithCallbackData("⬅️ 返回", "aduit/addfile/" + sub.Id) });
                    keyboardInline.Add(new List<InlineKeyboardButton>() { InlineKeyboardButton.WithCallbackData($"标题:{titleTrimmed}", $"{TimeStamp.GetNow()}") });
                    keyboardInline.Add(new List<InlineKeyboardButton>() { InlineKeyboardButton.WithCallbackData($"艺术家:{artistTrimmed}", $"{TimeStamp.GetNow()}") });
                    keyboardInline.Add(new List<InlineKeyboardButton>() { InlineKeyboardButton.WithUrl($"使用音频回复此消息(点击搜索)", $"https://google.com/search?q={HttpUtility.UrlEncode($"{song.Artist} {song.Title}")}") });
                    
                    await bot.EditMessageReplyMarkupAsync(query.Message!.Chat.Id, query.Message.MessageId, new InlineKeyboardMarkup(keyboardInline));
                    return;
                default:
                    throw new InvalidOperationException("无效的操作: "+data);
            }

            
        }

        private async Task ListQuery(CallbackQuery query, DbUser user, string data)
        {
            if (!user.Aduit)
            {
                await bot.AnswerCallbackQueryAsync(query.Id, "权限不足", true);
                return;
            }
            await bot.AnswerCallbackQueryAsync(query.Id);
            Message message = query.Message!;
            int.TryParse(data.Split('/').Last(), out int page);
            if (page < 0) return;
            var body = GetPage(page);
            if (body == "")
            {
                await message.FastEdit("暂无未审核消息", InlineKeyboardButton.WithCallbackData("🔄 Refresh", "list/page/0"));
                return;
            }
            InlineKeyboardMarkup replyMarkup;
            List<InlineKeyboardButton> buttons = new();
            if (page > 0)
                buttons.Add(InlineKeyboardButton.WithCallbackData("◀️ Prev Page", $"list/page/{page - 1}"));
            buttons.Add(InlineKeyboardButton.WithCallbackData("🔄 Refresh", "list/page/0"));
            if ((page + 1) * 10 < GetUnfinish().Count())
                buttons.Add(InlineKeyboardButton.WithCallbackData("▶️ Next Page", $"list/page/{page + 1}"));
            replyMarkup = new InlineKeyboardMarkup(buttons.ToArray());
            await message.FastEdit("当前未审核的稿件有\n" + body, replyMarkup);
        }
    }
}
