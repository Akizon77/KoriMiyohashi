using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace KoriMiyohashi.Modules
{
    public class Uploader
    {
        TelegramBotClient BotClient { get; set; }
        public Uploader(TelegramBotClient botClient) 
        {
            BotClient = botClient;
        }
        private static readonly Lazy<Uploader> _sharedInstance = new Lazy<Uploader>(() =>
        {
            return new (Hosting.GetRequiredService<Listener>().BotClient) ;
        });
        public static Uploader Shared => _sharedInstance.Value;

        public async Task<Message> UploadFile(long chatid,string path,string? fileName = null)
        {
            throw new NotImplementedException();
        }
        public async Task<Message> UploadBilibiliVideo(string av_or_bv_id, long chatid)
        {
            var biliDownloader = await BiliDown.Shared;
            await biliDownloader.RefreshWbiTokens();
            var video_detail = await biliDownloader.GetVideoDetailsAsync(av_or_bv_id);
            var cid = (long)video_detail!["data"]!["cid"]!;
            var bvid = (string)video_detail!["data"]!["bvid"]!;
            var videostream = await biliDownloader.GetVideoDownloadInfoAsync(bvid,cid,BiliVideoQuality._360P,BiliVideoFormatFNVAL.DASH);

            var stream_url = (string)videostream!["data"]!["dash"]!["audio"]![0]!["baseUrl"]!;
            stream_url = System.Text.RegularExpressions.Regex.Unescape(stream_url);
            var stream_request = new HttpRequestMessage(HttpMethod.Get, stream_url);
            stream_request.Headers.Add("User-Agent", biliDownloader.UserAgent);
            stream_request.Headers.Add("Referer", "https://www.bilibili.com");
            using var client = new HttpClient();
            var res = await client.SendAsync(stream_request, HttpCompletionOption.ResponseHeadersRead);
            res.EnsureSuccessStatusCode();
            var stream = await res.Content.ReadAsStreamAsync();

            if (!Directory.Exists("./temp"))
                Directory.CreateDirectory("./temp");
            var buffer = new byte[64 * 1024];
            int read;
            var bytesRead = 0L;

            var tips = await BotClient.SendTextMessageAsync(chatid,"正在尝试从哔哩哔哩下载...");
            using (var fileStream = new FileStream($"./temp/{bvid}.mp3", FileMode.Create, FileAccess.Write, FileShare.None))
            {
                while ((read = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, read);
                    bytesRead += read;
                }
            }
            Message message;
            using (var fs_stream = new FileStream($"./temp/{bvid}.mp3", FileMode.Open, FileAccess.Read))
            {
                message = await BotClient.SendAudioAsync(chatid, InputFile.FromStream(fs_stream), caption: $"{(string)video_detail!["data"]!["title"]!}\n\n请勿删除此消息");
            }
            try
            {
                System.IO.File.Delete($"./temp/{bvid}.mp3");
            }
            catch (Exception)
            {
                Log.Warning("无法删除缓存文件" + $"./temp/{bvid}.mp3");
            }
            tips.DeleteLater(0);
            return message;
        }

    }
}
