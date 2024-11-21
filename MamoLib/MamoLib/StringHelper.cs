using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MamoLib.StringHelper
{
    public static class StringHelper
    {
        /// <summary>
        /// 将&lt;&gt;&amp;替换为转义字符。
        /// 全部转义请使用 <see cref="System.Security.SecurityElement.Escape"/>
        /// </summary>
        public static string HtmlEscape(this string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            char[] chars = text.ToCharArray();
            int len = text.Length;
            StringBuilder sb = new StringBuilder(len + (len / 10));

            for (int i = 0; i < len; i++)
            {
                switch (chars[i])
                {
                    case '<': sb.Append("&lt;"); break;
                    case '>': sb.Append("&gt;"); break;
                    case '&': sb.Append("&amp;"); break;
                    default: sb.Append(chars[i]); break;
                }
            }

            return sb.ToString();
        }
    }
}
