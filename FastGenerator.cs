using KoriMiyohashi.Modules.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace KoriMiyohashi
{
    public static class FastGenerator
    {
        public static InlineKeyboardMarkup GeneratorInlineButton(List<Dictionary<string, string>> pairs)
        {
            List<List<InlineKeyboardButton>> keyboardInline = new();

            for (int i = 0; i < pairs.Count; i++)
            {
                var dict = pairs[i];
                keyboardInline.Add(new List<InlineKeyboardButton>());
                foreach (var kvp in dict)
                {
                    keyboardInline[i].Add(InlineKeyboardButton.WithCallbackData(kvp.Key, kvp.Value));
                }
            }
            return new InlineKeyboardMarkup(keyboardInline);
        }

        public static InlineKeyboardMarkup DefaultSubmissionMarkup()
        {
            List<Dictionary<string, string>> pair = [
                new() {
                    { "🌈点击下方的按钮操作",TimeStamp.GetNow().ToString()}
                },
                new() {
                    {"🏷修改标签","page/tags" },
                    {"📝修改推荐理由","edit/description" }
                },
                new(){
                    {"🎭切换匿名","switch/anonymous" },
                    {"🎧添加/修改曲目","page/song" }
                },
                new(){
                    //{ "🔍预览稿件","preview" },
                    { "✅提交","submit"}
                }
            ];
            return GeneratorInlineButton(pair);
        }

        public static InlineKeyboardMarkup DefaultAduitMarkup(Submission sub)
        {
            Dictionary<string, string> dic = new();
            if (sub.Songs.Count > 1)
                dic.Add("🔍 详情", $"aduit/details/{sub.Id}");

            foreach (var song in sub.Songs)
                if (string.IsNullOrEmpty(song.FileId))
                {
                    dic.Add("➕ 添加文件", $"aduit/addfile/{sub.Id}");
                    break;
                }
            return FastGenerator.GeneratorInlineButton([
                new(){
                            { "✅ 通过",$"aduit/approve/{sub.Id}" }
                        },
                        new (){
                            { "❌ 拒绝",$"aduit/reject/{sub.Id}" },
                            { "🔕 静默拒绝",$"aduit/slient/{sub.Id}" },
                        },
                        dic
                ]);
        }
    }
}