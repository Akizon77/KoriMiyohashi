using KoriMiyohashi.Modules.Types;
using MamoLib.TgExtensions;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace KoriMiyohashi.Handlers
{
    public partial class InlineQuerys
    {
        private async Task AduitQuery(CallbackQuery query, DbUser user, string data)
        {
            //STATUS: ADUIT/WAITING
            var args = data.Split('/');
            var action = args[1];
            var subId = int.Parse(args[2]);
            var sub = GetSubmission(subId);
            var message = query.Message!;
            var chatid = message.Chat.Id;
            async Task FastAduit(string text,string status)
            {
                await bot.EditMessageReplyMarkupAsync(chatid, message.MessageId
                        , FastGenerator.GeneratorInlineButton([
                            new(){ { $"{message.From!.GetFullName()}: {text}",TimeStamp.GetNow().ToString()} }]));
                sub.Status = status;
                repos.Submissions.Storageable(sub).ExecuteCommand();
                sub = GetSubmission(sub.Id);
            }
            switch (action)
            {
                case "approve":
                    await FastAduit("通过","APPROVED");
                    //TODO: 发布
                    try
                    {
                        await bot.SendTextMessageAsync(sub.UserId, "感谢你的投稿，稿件已通过", replyToMessageId: sub.SubmissionMessageId);
                    }
                    catch
                    {
                        await bot.SendTextMessageAsync(sub.UserId, "感谢你的投稿，稿件已通过");
                    }
                    return;
                case "reject":
                    await FastAduit("拒绝", "REJECTED");
                    //TODO: 通知
                    return;
                case "slient":
                    await FastAduit("静默拒绝", "REJECTED");
                    return;
                case "details":
                default:
                    throw new InvalidOperationException(data);
            }
            throw new NotImplementedException();
        }
    }
}
