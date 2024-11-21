using MamoLib.StringHelper;
using SqlSugar;
using System.Web;

namespace KoriMiyohashi.Modules.Types
{
    public class DbUser : MamoLib.Sql.SqlReopBase
    {
        [SugarColumn(IsPrimaryKey = true)]
        public long Id { get; set; }

        public string FullName { get; set; } = "";
        public bool Submit { get; set; } = true;
        public bool Aduit { get; set; } = false;
        public bool Owner { get; set; } = false;
    }

    public class Submission : MamoLib.Sql.SqlReopBase
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int Id { get; set; }

        public bool Anonymous { get; set; } = false;
        public int SubmissionMessageId { get; set; }

        [SugarColumn(IsNullable = true)]
        public int? GroupMessageId { get; set; }

        [SugarColumn(IsNullable = true)]
        public int? ChannelMessageId { get; set; }

        public string Status { get; set; } = "WAITING";
        public long UserId { get; set; }

        [Navigate(NavigateType.OneToOne, nameof(UserId))]
        public DbUser User { get; set; }

        public string Tags { get; set; } = "推荐";

        [SugarColumn(ColumnDataType = "LONGTEXT")]
        public string Description { get; set; } = "";

        [Navigate(NavigateType.OneToMany, nameof(Song.SubmissionId))]
        public List<Song> Songs { get; set; }

        public override string ToString()
        {
            return $"状态: {Status}\n" +
                $"用户ID: {UserId}\n" +
                $"标签: #{Tags}\n" +
                $"附言: {Description}\n" +
                $"曲目数量: {Songs.Count}";
        }

        public string ToHtmlString()
        {
            string text = $"标签: #{Tags}\n" +
                    $"附言: {Description.HtmlEscape()}\n" +
                    $"曲目数量: {Songs.Count}\n" +
                    "\n" +
                    $"当前曲目：";
            int i = 1;
            foreach (Song song in Songs)
            {
                if (!String.IsNullOrEmpty(song.Link))
                {
                    text += $"\n{i}: <a href=\"{HttpUtility.HtmlAttributeEncode(song.Link)}\">{song.Artist.HtmlEscape()} - {song.Title.HtmlEscape()}</a>";
                }
                else
                {
                    text += $"\n{i}: <code>{song.Artist.HtmlEscape()} - {song.Title.HtmlEscape()}</code>";
                }
                i++;
            }
            if (Anonymous)
            {
                return $"状态: <code>{Status}</code>\n" +
                    $"投稿人: 匿名\n" +
                    text;
            }
            else
            {
                return $"状态: <code>{Status}</code>\n" +
                    $"投稿人: <a href=\"tg://user?id={User.Id}\">{User.FullName.HtmlEscape()}</a>\n" +
                    text;
            }
        }

        public string ToPubHtmlString()
        {
            string text =
                $"Tag: #{Tags}\n" +
                $"附言: {Description}\n\n" +
                $"曲目数量: {Songs.Count}";
            int i = 1;
            foreach (Song song in Songs)
            {
                if (!String.IsNullOrEmpty(song.Link))
                {
                    text += $"\n{i}: <a href=\"{HttpUtility.HtmlAttributeEncode(song.Link)}\">{song.Artist.HtmlEscape()} - {song.Title.HtmlEscape()}</a>";
                }
                else
                {
                    text += $"\n{i}: <code>{song.Artist.HtmlEscape()} - {song.Title.HtmlEscape()}</code>";
                }
                i++;
            }
            if (Anonymous)
            {
                return
                    $"投稿人: 匿名\n" +
                    text;
            }
            else
            {
                return
                    $"投稿人: <a href=\"tg://user?id={User.Id}\">{User.FullName.HtmlEscape()}</a>\n" +
                    text;
            }
        }
    }

    public class Song : MamoLib.Sql.SqlReopBase
    {
        [SugarColumn(IsIdentity = true, IsPrimaryKey = true)]
        public int Id { get; set; }

        public int SubmissionId { get; set; }

        [SqlSugar.SugarColumn(ColumnDataType = "LONGTEXT")]
        public string Title { get; set; } = "未知标题";

        [SqlSugar.SugarColumn(ColumnDataType = "LONGTEXT")]
        public string Artist { get; set; } = "未知作曲家";

        [SqlSugar.SugarColumn(ColumnDataType = "LONGTEXT", IsNullable = true)]
        public string? Link { get; set; }

        [SqlSugar.SugarColumn(ColumnDataType = "LONGTEXT", IsNullable = true)]
        public string? FileId { get; set; }
    }
}