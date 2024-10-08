﻿using KoriMiyohashi.Modules.Types;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;

namespace KoriMiyohashi.Modules
{
    public static class Parser
    {
        public static void ParseQQMusic(string url, ref Song song)
        {
            if (!url.Contains("qq.com")) return;

            // 解析URL获取查询参数
            Uri uri = new Uri(url);
            string query = uri.Query;
            System.Collections.Specialized.NameValueCollection queryParameters =
                System.Web.HttpUtility.ParseQueryString(query);
            string? id = queryParameters["songmid"];
            // 如果没有提取到songmid，返回原文章对象
            if (id is null) return;

            // 设置文章链接
            song.Link = $"https://i.y.qq.com/v8/playsong.html?songmid={id}";

            // 定义正则表达式，用于从页面源码中提取数据
            string pattern = @"<\/div><script crossorigin=""anonymous"">window\.__ssrFirstPageData__ =([\s\S]*?)<\/script>";

            // 发起HTTP请求获取页面源码
            HttpClient httpClient = new();
            var body = httpClient.GetStringAsync(url).Result;

            // 使用正则表达式匹配所需数据
            MatchCollection matches = Regex.Matches(body, pattern);
            JObject jobj = new();

            try
            {
                // 提取并解析匹配到的数据
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

            // 尝试从解析的数据中提取文章标题

            try
            {
                song.Title = jobj["songList"]![0]!["title"]!.ToString();
            }
            catch { }
            // 尝试从解析的数据中提取歌手信息
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
                    // 去除最后一个字符（多余的逗号）
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

        public static void ParseNeteaseMusic(string url, ref Song song)
        {
            if (!url.Contains("163")) return;
            // 从URL中解析出查询参数
            Uri uri = new Uri(url);
            string query = uri.Query;
            System.Collections.Specialized.NameValueCollection queryParameters =
                System.Web.HttpUtility.ParseQueryString(query);
            string? id = queryParameters["id"];

            // 如果没有解析出id，则直接返回原始的Post对象
            if (id is null) return;

            // 设置帖子的链接
            song.Link = $"https://music.163.com/song?id={id}";

            // 使用HttpClient获取歌曲详情
            HttpClient client = new HttpClient();
            var s = client.GetAsync($"http://music.163.com/api/song/detail/?id={id}&ids=%5B{id}%5D").Result;
            var jo = JObject.Parse(s.Content.ReadAsStringAsync().Result);

            // 尝试解析歌曲标题
            try
            {
                song.Title = jo["songs"]![0]!["name"]!.ToString();

                // 如果有翻译的歌曲名，则添加到标题中
                try
                {
                    if (jo["songs"]![0]!["transName"] != null)
                        if (jo["songs"]![0]!["transName"]!.ToString() != "")
                            song.Title += $"({jo["songs"]![0]!["transName"]})";
                }
                catch { }
            }
            catch { }

            // 尝试解析歌手信息
            try
            {
                string singer = "";
                var list = jo["songs"]![0]!["artists"]!.ToList();
                foreach (var item in list)
                {
                    singer += item["name"]!.ToString() + "、";
                }
                // 如果没有歌手信息，则设置作者为null
                if (singer == "")
                    song.Artist = "未知艺术家";
                else if (singer.Length > 1)
                {
                    // 去除最后一个字符（多余的逗号）并设置作者
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