using KoriMiyohashi.Modules.Types;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Internal;
using NetTaste;
using Newtonsoft.Json.Linq;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using Telegram.Bot.Types;

namespace KoriMiyohashi.Modules
{
    public static class Parser
    {
        public static async Task<Message?> Parse(string url, Song song, long? chatid = null)
        {
            try
            {
                var rawResp = String.Empty;
                Uri uri = new(url);
                var handler = new HttpClientHandler
                {
                    AllowAutoRedirect = true, 
                    MaxAutomaticRedirections = 5 
                };
                using (HttpClient client = new HttpClient(handler))
                {
                    int maxRedirects = 5; //在3xx有正文时不会自动重定向
                    int redirectCount = 0;
                    HttpResponseMessage? response = null;
                    do
                    {
                        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);

                        request.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
                        request.Headers.Add("Accept-Encoding", "identity, gzip, deflate");
                        request.Headers.Add("Accept-Language", "zh-CN,zh;q=0.9");
                        request.Headers.Add("Priority", "u=0, i");
                        request.Headers.Add("Sec-CH-UA", "\"Chromium\";v=\"128\", \"Not;A=Brand\";v=\"24\", \"Microsoft Edge\";v=\"128\"");
                        request.Headers.Add("Sec-CH-UA-Mobile", "?0");
                        request.Headers.Add("Sec-CH-UA-Platform", "\"Windows\"");
                        request.Headers.Add("Upgrade-Insecure-Requests", "1");
                        request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/128.0.0.0 Safari/537.36 Edg/128.0.0.0");

                        response = client.SendAsync(request).Result;
                        uri = request.RequestUri!;
                        if ((int)response.StatusCode >= 300 && (int)response.StatusCode < 400)
                        {
                            if (redirectCount++ >= maxRedirects)
                            {
                                throw new Exception("Too many redirects");
                            }

                            uri = response.Headers.Location!; 
                        }
                        else
                        {
                            break;
                        }
                    }
                    while (true);
                    
                    
                    var contentStream = response.Content.ReadAsStreamAsync().Result;

                    if (response.Content.Headers.ContentEncoding.Contains("gzip"))
                    {
                        using (var decompressionStream = new GZipStream(contentStream, CompressionMode.Decompress))
                        using (var reader = new StreamReader(decompressionStream))
                        {
                            rawResp = reader.ReadToEndAsync().Result;
                        }
                    }
                    else if (response.Content.Headers.ContentEncoding.Contains("deflate"))
                    {
                        using (var decompressionStream = new DeflateStream(contentStream, CompressionMode.Decompress))
                        using (var reader = new StreamReader(decompressionStream))
                        {
                            rawResp = reader.ReadToEndAsync().Result;
                        }
                    }
                    else
                    {
                        using (var reader = new StreamReader(contentStream))
                        {
                            rawResp = reader.ReadToEndAsync().Result;
                        }
                    }

                }
                System.Collections.Specialized.NameValueCollection queryParameters = System.Web.HttpUtility.ParseQueryString(uri.Query);
                //网易云
                if (uri.Host.Contains("163"))
                {
                    string? netease_id = queryParameters["id"];
                    if (netease_id is null) throw new Exception("无法找到网易云ID");
                    song.Link = $"https://music.163.com/song?id={netease_id}";

                    HttpClient client = new HttpClient();
                    var s = await client.GetAsync($"http://music.163.com/api/song/detail/?id={netease_id}&ids=%5B{netease_id}%5D");
                    var jo = JObject.Parse(await s.Content.ReadAsStringAsync());
                    try
                    {
                        song.Title = jo["songs"]![0]!["name"]!.ToString();
                        if (jo["songs"]![0]!["transName"] != null)
                            if (jo["songs"]![0]!["transName"]!.ToString() != "")
                                song.Title += $"({jo["songs"]![0]!["transName"]})";
                        //歌手
                        string singer = "";
                        var list = jo["songs"]![0]!["artists"]!.ToList();
                        foreach (var item in list)
                        {
                            singer += item["name"]!.ToString() + "、";
                        }
                        if (singer == "")
                            song.Artist = "未知艺术家";
                        else if (singer.Length > 1)
                        {
                            // 多余的逗号
                            singer = singer[..(singer.Length - 1)];
                            song.Artist = singer;
                        }
                        else
                            song.Artist = "未知艺术家";
                    }
                    catch { }
                }
                //QQ
                if (uri.Host.Contains("qq"))
                {
                    try
                    {
                        string? qq_id = queryParameters["songmid"];
                        if (qq_id is null)
                        {
                            string _pattern = @".*songDetail/(.*)/?";
                            Match _match = Regex.Match(uri.AbsolutePath, _pattern);
                            qq_id = _match.Success ? _match.Groups[1].Value : string.Empty;
                        }
                        song.Link = $"https://i.y.qq.com/v8/playsong.html?songmid={qq_id}";
                        if (uri.Host != ("i.y.qq.com"))
                            rawResp = await new HttpClient().GetStringAsync(new Uri(song.Link));
                        //Title
                        string pattern = @"<span\s+class=""song_name__text"">(.*?)</span>";
                        Match match = Regex.Match(rawResp, pattern);
                        song.Title = match.Success ? match.Groups[1].Value : string.Empty;

                        pattern = @"<h2\s+class=""singer_name"">(.*?)</h2>";
                        match = Regex.Match(rawResp, pattern);
                        song.Artist = match.Success ? match.Groups[1].Value : string.Empty;
                    }
                    catch
                    {
                        ParseQQMusic(url, ref song);
                    }

                }
                //哔哩哔哩
                if (uri.Host.Contains("bilibili") || uri.Host.Contains("b23"))
                {
                    string pattern = @"video/(.*)/";
                    Match match = Regex.Match(uri.AbsolutePath, pattern);
                    var video_id = match.Success ? match.Groups[1].Value : string.Empty;
                    song.Link = "https://bilibili.com/" + video_id;

                    pattern = @"<meta .* name=""title"" content=""(.*?)"">";
                    match = Regex.Match(rawResp, pattern);
                    var t = match.Success ? match.Groups[1].Value : string.Empty;
                    song.Title = t.Replace("_哔哩哔哩_bilibili", "");

                    pattern = @"<meta .* name=""author"" content=""(.*?)"">";
                    match = Regex.Match(rawResp, pattern);
                    song.Artist = match.Success ? match.Groups[1].Value : string.Empty;
                    if (chatid != null)
                        return await Uploader.Shared.UploadBilibiliVideo(video_id, chatid ?? throw new NullReferenceException());
                    
                }
                //酷狗

                if (uri.Host.Contains("kugou"))
                {
                    string? id = queryParameters["id"];
                    if (string.IsNullOrEmpty(id))
                    {
                        string _pattern = @"share/(.*)\.html";
                        Match _match = Regex.Match(uri.AbsolutePath, _pattern);
                        id = _match.Success ? _match.Groups[1].Value : string.Empty;
                    }
                    song.Link = $"https://www.kugou.com/share/{id}.html";

                    var pattern = @"""author_name"":""(.*?)""";
                    var match = Regex.Match(rawResp, pattern);
                    song.Artist = match.Success ? match.Groups[1].Value : string.Empty;

                    pattern = @"""song_name"":""(.*?)""";
                    match = Regex.Match(rawResp, pattern);
                    var t = match.Success ? match.Groups[1].Value : string.Empty;
                    string decodedString = WebUtility.HtmlDecode(t);
                    song.Title = decodedString;
                }

                if (uri.Host.Contains("youtu")){
                    
                    string? id = queryParameters["v"];
                    if (string.IsNullOrEmpty(id))
                    {
                        // https://youtu.be/vboHrJJ83VY?si=xxxxx
                        string _pattern = @"youtu\.be\/(.*)\?";
                        Match _match = Regex.Match(uri.AbsolutePath, _pattern);
                        id = _match.Success ? _match.Groups[1].Value : string.Empty;
                    }
                    if (string.IsNullOrEmpty(id))
                        throw new Exception("无法找到youtube视频ID");
                    using HttpClient client = new HttpClient();
                    HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get,$"https://youtube.googleapis.com/youtube/v3/videos?part=snippet&id={id}&key={Env.Youtube_API}");
                    var resp = await client.SendAsync(requestMessage);
                    var resp_str = await resp.Content.ReadAsStringAsync();
                    var jobj = JObject.Parse(resp_str);
                    song.Title = (string)jobj["items"]![0]!["snippet"]!["title"]!;
                    song.Artist = (string)jobj["items"]![0]!["snippet"]!["channelTitle"]!;
                    song.Link = $"https://www.youtube.com/watch?v={id}";
                }

            }
            catch (Exception e)
            {
                Log.Warning("无法解析平台信息: {0},{1}", e.Message,url);
            }
            return null;
        }
        [Obsolete("已合并，请使用Parse")]
        public static void ParseQQMusic(string url, ref Song song)
        {
            if (!url.Contains("qq.com")) return;

            Uri uri = new Uri(url);
            string query = uri.Query;
            System.Collections.Specialized.NameValueCollection queryParameters =
                System.Web.HttpUtility.ParseQueryString(query);
            string? id = queryParameters["songmid"];
            // 如果没有提取到songmid，返回原文章对象
            if (id is null) return;

            song.Link = $"https://i.y.qq.com/v8/playsong.html?songmid={id}";
            // 正则
            string pattern = @"<\/div><script crossorigin=""anonymous"">window\.__ssrFirstPageData__ =([\s\S]*?)<\/script>";

            HttpClient httpClient = new();
            var body = httpClient.GetStringAsync(url).Result;

            MatchCollection matches = Regex.Matches(body, pattern);
            JObject jobj = new();

            try
            {
                var l1 = "</div><script crossorigin=\"anonymous\">window.__ssrFirstPageData__ =".Length;
                var l2 = "</script>".Length;
                foreach (Match match in matches)
                {
                    if (match.Value.Contains("songList"))
                    {
                        var content = match.Value[l1..(match.Value.Length - l2)];
                        jobj = JObject.Parse(content);
                        break;
                    }
                }
            }
            catch { }

            // 标题
            try
            {
                song.Title = jobj["songList"]![0]!["title"]!.ToString();
            }
            catch { }
            // 歌手
            try
            {
                string singer = "";
                var list = jobj["songList"]![0]!["singer"]!.ToList();
                foreach (var item in list)
                {
                    singer += item["title"] + "、";
                }
                // 如果没有歌手信息，设置作者为null
                if (singer == "")
                    song.Artist = "未知艺术家";
                else if (singer.Length > 1)
                {
                    // 多余的逗号
                    singer = singer[..(singer.Length - 1)];
                    song.Artist = singer;
                }
                else
                    song.Artist = "未知艺术家";
            }
            catch { }
            // 尝试从解析的数据中提取专辑信息
            //try
            //{
            //    if (jobj["songList"][0]["album"]["title"].ToString() == "默认专辑")
            //        song.Album = "单曲";
            //    else if (string.IsNullOrEmpty(jobj["songList"][0]["album"]["title"].ToString()))
            //        song.Album = "单曲";
            //    else
            //        song.Album = jobj["songList"][0]["album"]["title"].ToString();
            //}
            //catch { }
        }
        [Obsolete("已合并，请使用Parse")]
        public static void ParseNeteaseMusic(string url, ref Song song)
        {
            if (!url.Contains("163")) return;
            Uri uri = new Uri(url);
            string query = uri.Query;
            System.Collections.Specialized.NameValueCollection queryParameters =
                System.Web.HttpUtility.ParseQueryString(query);
            string? id = queryParameters["id"];

            if (id is null) return;
            song.Link = $"https://music.163.com/song?id={id}";

            HttpClient client = new HttpClient();
            var s = client.GetAsync($"http://music.163.com/api/song/detail/?id={id}&ids=%5B{id}%5D").Result;
            var jo = JObject.Parse(s.Content.ReadAsStringAsync().Result);

            try
            {
                song.Title = jo["songs"]![0]!["name"]!.ToString();

                // 翻译
                try
                {
                    if (jo["songs"]![0]!["transName"] != null)
                        if (jo["songs"]![0]!["transName"]!.ToString() != "")
                            song.Title += $"({jo["songs"]![0]!["transName"]})";
                }
                catch { }
            }
            catch { }

            // 歌手
            try
            {
                string singer = "";
                var list = jo["songs"]![0]!["artists"]!.ToList();
                foreach (var item in list)
                {
                    singer += item["name"]!.ToString() + "、";
                }
                if (singer == "")
                    song.Artist = "未知艺术家";
                else if (singer.Length > 1)
                {
                    // 多余的逗号
                    singer = singer[..(singer.Length - 1)];
                    song.Artist = singer;
                }
                else
                    song.Artist = "未知艺术家";
            }
            catch { }

            // 尝试解析专辑信息
            //try
            //{
            //    post.Album = jo["songs"][0]["album"]["name"]?.ToString();
            //}
            //catch { }
            //
            //return post;
        }

        public static void ParseSpotify(string url, ref Song song)
        {
            //try
            //{
            //    using (HttpClient client = new HttpClient())
            //    {
            //        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
            //
            //        // 设置请求头
            //        request.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
            //        request.Headers.Add("Accept-Encoding", "gzip, deflate, br, zstd");
            //        request.Headers.Add("Accept-Language", "zh-CN,zh;q=0.9");
            //        request.Headers.Add("Priority", "u=0, i");
            //        request.Headers.Add("Sec-CH-UA", "\"Chromium\";v=\"128\", \"Not;A=Brand\";v=\"24\", \"Microsoft Edge\";v=\"128\"");
            //        request.Headers.Add("Sec-CH-UA-Mobile", "?0");
            //        request.Headers.Add("Sec-CH-UA-Platform", "\"Windows\"");
            //        request.Headers.Add("Sec-Fetch-Dest", "document");
            //        request.Headers.Add("Sec-Fetch-Mode", "navigate");
            //        request.Headers.Add("Sec-Fetch-Site", "none");
            //        request.Headers.Add("Sec-Fetch-User", "?1");
            //        request.Headers.Add("Upgrade-Insecure-Requests", "1");
            //        request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/128.0.0.0 Safari/537.36 Edg/128.0.0.0");
            //
            //        HttpResponseMessage response = client.SendAsync(request).Result;
            //        string pageContent = response.Content.ReadAsStringAsync().Result;
            //
            //
            //        string titlePattern = @"<meta property=""og:title"" content=""(.*?)"" />";
            //        string artistPattern = @"<meta property=""og:description"" content=""Song · (.*?) ·";
            //        string linkPattern = @"<meta property=""og:url"" content=""(.*?)"" />";
            //
            //        song.Title = Regex.Match(pageContent, titlePattern).Groups[1].Value;
            //        song.Artist = Regex.Match(pageContent, artistPattern).Groups[1].Value;
            //        song.Link = Regex.Match(pageContent, linkPattern).Groups[1].Value;
            //    }
            //}
            //catch { }
        }
    }
}