using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Net.Http;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace KoriMiyohashi.Modules
{
    public partial class BiliDown
    {
        private string ImgKey { get; set; } = "";
        private string SubKey { get; set; } = "";

        private DateTime lastUpdateTime = DateTime.MinValue;

        public  string UserAgent { get; set; } = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36 Edg/131.0.0.0";

        public BiliDown(string imgk,string subk)
        {
            ImgKey= imgk;
            SubKey= subk;
            lastUpdateTime = DateTime.Now;
        }
        public async Task RefreshWbiTokens()
        {
            var (imgk, subk) = await GetWbiKeys();
            ImgKey = imgk;
            SubKey = subk;
            lastUpdateTime = DateTime.Now;
        }
        public (string, string) GetInstanceWbiKey()
        {
            return (ImgKey, SubKey);
        }

        private static readonly Lazy<Task<BiliDown>> _sharedInstance = new Lazy<Task<BiliDown>>(async () =>
        {
            var (imgk, subk) = await GetWbiKeys();
            return new BiliDown(imgk, subk);
        });

        /// <summary>
        /// Shared instance
        /// </summary>
        public static Task<BiliDown> Shared => _sharedInstance.Value;
    }
    public partial class BiliDown
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="bvid"></param>
        /// <param name="videoQuality"></param>
        /// <returns>Stream,stream length,videoDetails</returns>
        public async Task<(Stream, long, JsonNode)> GetDownloadVideoStream(string bvid, BiliVideoQuality videoQuality)
        {
            var videoDetails = await GetVideoDetailsAsync(bvid);
            var videostream = await GetVideoDownloadInfoAsync(bvid, (long)videoDetails!["data"]!["cid"]!, BiliVideoQuality._720P);
            var stream_url = (string)videostream!["data"]!["durl"]![0]!["url"]!;
            stream_url = System.Text.RegularExpressions.Regex.Unescape(stream_url);

            var stream_request = new HttpRequestMessage(HttpMethod.Get, stream_url);

            stream_request.Headers.Add("User-Agent", UserAgent);
            stream_request.Headers.Add("Referer", "https://www.bilibili.com");

            using var client = new HttpClient();
            var res = await client.SendAsync(stream_request, HttpCompletionOption.ResponseHeadersRead);
            res.EnsureSuccessStatusCode();
            return (await res.Content.ReadAsStreamAsync(), res.Content.Headers.ContentLength ?? -1, videoDetails);
        }

        public async Task<JsonNode> GetVideoDownloadInfoAsync(string bvid, long cid, BiliVideoQuality videoQuality = BiliVideoQuality._720P, BiliVideoFormatFNVAL biliVideoFormat = BiliVideoFormatFNVAL.MP4)
        {
            var sign = await GetWbiSignQueryAsync(new Dictionary<string, string>{
                { "cid", cid.ToString()},
                { "bvid", bvid},
                { "fnval",$"{(int)biliVideoFormat}"},
                { "qn",$"{(int)videoQuality}"}
            });
            var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.bilibili.com/x/player/wbi/playurl?{sign}");

            request.Headers.Add("Accept", "application/json");
            request.Headers.Add("User-Agent", UserAgent);

            using var client = new HttpClient();
            var response = await client.SendAsync(request);
            var result = await response.Content.ReadAsStringAsync();
            return JsonNode.Parse(result)!;
        }
    }

    public partial class BiliDown
    {
        [Obsolete("Use GetVideoDetailsAsync instead")]
        public async Task<Tuple<long, string, string>> GetCid(string videoId)
        {
            var resp_json = await GetVideoDetailsAsync(videoId);
            return new((long)resp_json!["data"]!["cid"]!, (string)resp_json!["data"]!["title"]!, (string)resp_json!["data"]!["owner"]!["name"]!);
        }

        public async Task<JsonNode> GetVideoDetailsAsync(string videoId)
        {
            var base_url = "https://api.bilibili.com/x/web-interface/wbi/view";

            switch (videoId[..2].ToUpper())
            {
                case "AV":
                    base_url += $"?aid={videoId[2..]}";
                    break;

                case "BV":
                    base_url += $"?bvid={videoId}";
                    break;

                default:
                    throw new Exception();
            }
            using (var client = new HttpClient())
            {
                var request = new HttpRequestMessage();
                request.RequestUri = new Uri(base_url);
                request.Method = HttpMethod.Get;
                request.Headers.Add("Accept", "application/json");
                request.Headers.Add("User-Agent", UserAgent);
                var resp = await client.SendAsync(request);
                var resp_str = await resp.Content.ReadAsStringAsync();
                var resp_json = JsonNode.Parse(resp_str);
                return resp_json!;
            }
        }
    }

    public partial class BiliDown
    {
        private static readonly int[] MixinKeyEncTab =
        {
            46, 47, 18, 2, 53, 8, 23, 32, 15, 50, 10, 31, 58, 3, 45, 35, 27, 43, 5, 49, 33, 9, 42, 19, 29, 28, 14, 39,
            12, 38, 41, 13, 37, 48, 7, 16, 24, 55, 40, 61, 26, 17, 0, 1, 60, 51, 30, 4, 22, 25, 54, 21, 56, 59, 6, 63,
            57, 62, 11, 36, 20, 34, 44, 52
        };

        /// <summary>
        /// 对 imgKey 和 subKey 进行字符顺序打乱编码
        /// </summary>
        /// <param name="orig">原始字符串</param>
        /// <returns></returns>
        private static string GetMixinKey(string orig)
        {
            return MixinKeyEncTab.Aggregate("", (s, i) => s + orig[i])[..32];
        }

        /// <summary>
        /// Encode Wbi Auth
        /// </summary>
        /// <param name="parameters">http query dict</param>
        /// <param name="imgKey">Wbi imgKey</param>
        /// <param name="subKey">Wbi subKey</param>
        /// <returns></returns>
        private static Dictionary<string, string> EncWbi(Dictionary<string, string> parameters, string imgKey, string subKey)
        {
            string mixinKey = GetMixinKey(imgKey + subKey);
            string currTime = DateTimeOffset.Now.ToUnixTimeSeconds().ToString();
            //添加 wts 字段
            parameters["wts"] = currTime;
            // 按照 key 重排参数
            parameters = parameters.OrderBy(p => p.Key).ToDictionary(p => p.Key, p => p.Value);
            //过滤 value 中的 "!'()*" 字符
            parameters = parameters.ToDictionary(
                kvp => kvp.Key,
                kvp => new string(kvp.Value.Where(chr => !"!'()*".Contains(chr)).ToArray())
            );
            // 序列化参数
            string query = new FormUrlEncodedContent(parameters).ReadAsStringAsync().Result;
            //计算 w_rid
            using MD5 md5 = MD5.Create();
            byte[] hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(query + mixinKey));
            string wbiSign = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            parameters["w_rid"] = wbiSign;

            return parameters;
        }

        /// <summary>
        /// Get Wbi Auth key with guest.
        /// </summary>
        /// <returns>img_key and sub_key</returns>
        public static async Task<(string, string)> GetWbiKeys()
        {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
            httpClient.DefaultRequestHeaders.Referrer = new Uri("https://www.bilibili.com/");

            HttpResponseMessage responseMessage = await httpClient.SendAsync(new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri("https://api.bilibili.com/x/web-interface/nav"),
            });

            JsonNode response = JsonNode.Parse(await responseMessage.Content.ReadAsStringAsync())!;

            string imgUrl = (string)response["data"]!["wbi_img"]!["img_url"]!;
            imgUrl = imgUrl.Split("/")[^1].Split(".")[0];

            string subUrl = (string)response["data"]!["wbi_img"]!["sub_url"]!;
            subUrl = subUrl.Split("/")[^1].Split(".")[0];
            return (imgUrl, subUrl);
        }

        public async Task<string> GetWbiSignQueryAsync(Dictionary<string, string> query)
        {
            if (string.IsNullOrEmpty(ImgKey))
                throw new ArgumentNullException(nameof(ImgKey));
            if (string.IsNullOrEmpty(SubKey))
                throw new ArgumentNullException(nameof(SubKey));

            Dictionary<string, string> signedParams = EncWbi(
                parameters: query,
                imgKey: ImgKey,
                subKey: SubKey
            );

            return await new FormUrlEncodedContent(signedParams).ReadAsStringAsync();
        }
    }

    public enum BiliVideoQuality
    {
        _1080P = 128,
        _720P = 64,
        _480P = 32,
        _360P = 16,
    }
    public enum BiliVideoFormatFNVAL
    {
        MP4 = 1,
        DASH = 16,
        HDR = 64,
        _4K = 128,
    }
}
