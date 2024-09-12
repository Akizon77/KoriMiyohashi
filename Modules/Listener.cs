using System.Net.Http;
using System.Net;
using System.Threading;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;
using Telegram.Bot;
using MamoLib.TgExtensions;
using KoriMiyohashi.Modules.Types;
using Telegram.Bot.Exceptions;

namespace KoriMiyohashi.Modules
{
    public class Listener
    {
        public TelegramBotClient BotClient { get; set; }
        public User Me { get; set; }
        
        private Repos repos { get; set; }

        public Listener(Repos repo)
        {
            this.repos = repo;
            HttpClient httpClient;
            if (Env.USE_PROXY)
            {
                Log.Information("Use {proxy} proxy server to log in to Telegram...", Env.PROXY);
                WebProxy webProxy = new WebProxy(Env.PROXY, true);
                HttpClientHandler httpClientHandler = new HttpClientHandler();
                httpClientHandler.Proxy = webProxy;
                httpClientHandler.UseProxy = true;
                httpClient = new HttpClient(httpClientHandler);
            }
            else
            {
                Log.Information("Logging in to Telegram...");
                httpClient = new();
            }
            BotClient = new TelegramBotClient(Env.TG_TOKEN, httpClient);
            try
            {
                Me = BotClient.GetMeAsync().Result;
                _ = SetMyCommandsAsync();
            }
            catch (Exception e)
            {
                Log.Error("Unable to log into Telegram, check the Debug log for more information. {0}", e.Message);
                Log.Debug(e, "Unable to log in to Telegram.");
                Environment.Exit(1);
            }

            BotClient.StartReceiving(async (c, u, t) =>
            {
                _ =  OnUpdate(c, u, t);
            }, OnError);

            Log.Information("Successfully logged in as {fullname}(@{username}) ", Me.GetFullName(), Me.Username);
        }

        private Task OnError(ITelegramBotClient client, Exception exception, CancellationToken token)
        {
            Log.Warning($"{exception.Message}. {exception.InnerException?.Message}");
            return Task.CompletedTask;
        }

        private async Task OnUpdate(ITelegramBotClient client, Update update, CancellationToken token)
        {
            long chatID;
            User user;
            DbUser dbUser;
            if (update.Message is { } message)
            {
                chatID = message.Chat.Id;
                user = message.From!;
                dbUser = new DbUser() { Id = user.Id, FullName = user.GetFullName() };
                repos.DbUsers.Storageable(dbUser).ExecuteCommand();
                //处理纯文字、音频消息
                try
                {
                    //文字
                    if (message.Text != null && message.Text.Length > 0)
                    {
                        Log.Information("{name}({id}): {text}", user.GetFullName(), user.Id, message.Text);
                        if (message.Text.StartsWith('/'))
                        {
                            string[] parts = message.Text[1..].Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                            string command = parts[0].Split('@')[0]; // 获取命令，去除@botname部分
                            string[] args = parts.Length > 1 ? parts[1].Split(' ', StringSplitOptions.RemoveEmptyEntries) : Array.Empty<string>();
                            await OnCommand(message, dbUser, command, args);
                        }
                        else
                        {
                            await OnText(message,dbUser,message.Text);
                        }
                    }
                    //音频
                    else if (update.Message.Audio is { } audio)
                    {
                        Log.Information("{name}({id}): {title} - {artist}", user.GetFullName(), user.Id, audio.Title,audio.Performer);
                        await OnAudio(message.Audio!, dbUser,message.Caption!,message);
                    }
                }
                #region 异常捕获
                catch (NotSupportedException ex)
                {
                    message.DeleteLater();
                    var tx = await message.FastReply($"{ex.Message}");
                    tx.DeleteLater();
                }
                catch (NotImplementedException)
                {
                    message.DeleteLater();
                    var tx = await message.FastReply($"这个功能还没有实现，咕咕咕");
                    tx.DeleteLater();
                }
                catch (Exception ex)
                {
                    message.DeleteLater();
                    Log.Warning(ex, "{name}({id}): {message}", user.GetFullName(), user.Id, message.Text);
                    var tx = await message.FastReply($"被玩坏了 {ex.GetType().Name}: {ex.Message}");
                    tx.DeleteLater();
                }
                #endregion
            }
            else if (update.CallbackQuery is { } query)
            {
                chatID = query.Message!.Chat.Id;
                user = query.From;
                dbUser = new DbUser() { Id = user.Id, FullName = user.GetFullName() };
                repos.DbUsers.Storageable(dbUser).ExecuteCommand();
                //Inline查询
                try
                {
                    if (query.Data != null && query.Data.Length > 0)
                    {
                        Log.Debug("{name}({id}): {text}", user.GetFullName(), user.Id, query.Data);
                        await OnInlineQuery(query,dbUser, query.Data);
                        _ = BotClient.AnswerCallbackQueryAsync(query.Id);
                    }
                }
                catch ( RequestException e )
                {
                    Log.Warning(e, "Exception during making request.");
                }
                catch (Exception e)
                {
                    Log.Error(e, "Unable to process Inline message.{@0}",query);
                    _ = BotClient.SendTextMessageAsync(query.Message.Chat.Id,"被玩坏了喵\n"+e.Message);
                }
            }
            else
            {
                return;
            }
        }
        #region 文字消息

        
        List<Func<Message, DbUser, string, Task>> textFunc = new();
        async Task OnText(Message message, DbUser dbUser, string text)
        {
            foreach (var item in textFunc)
            {
                await item.Invoke(message,dbUser,text);
            }
        }
        public void RegisterTextMessage(Func<Message, DbUser, string, Task> func)
        {
            textFunc.Add(func);
        }
        #endregion
        #region 指令
        private List<BotCommand> botCommands = new();
        private Dictionary<string, string> allowedPrivateCommand = new();
        private Dictionary<string, Func<Message, DbUser, string, string[], Task>> commandsFunction = new();

        private async Task OnCommand(Message message, DbUser dbUser, string command, string[] args)
        {
            if (message.Chat.Type != ChatType.Private && !message.Text!.Contains(Me.Username!)) return;
            User user = message.From!;
            long chatID = message.Chat.Id;
            //忽略机器人的消息
            if (user.IsBot) throw new NotSupportedException("不能使用频道马甲喵");

            if (!commandsFunction.ContainsKey(command)) return;

            if (message.Chat.Type == ChatType.Private && !allowedPrivateCommand.ContainsKey(command))
                return;
            await commandsFunction[command].Invoke(message, dbUser, command, args);
        }
        public void RegisterCommand(string command, Func<Message, DbUser, string, string[], Task> func, string? desc = null, bool allowPrivateChat = true)
        {
            Log.Information("Registering command {c} - {desc}", command, desc ?? "No Description");
            commandsFunction.Add(command, func);
            if (allowPrivateChat && desc != null) 
                allowedPrivateCommand[command] = desc;
            
            if (desc != null)
                botCommands.Add(new()
                {
                    Command = command,
                    Description = desc,
                });
        }
        public async Task SetMyCommandsAsync()
        {
            List<BotCommand> privateCommand = new List<BotCommand>();
            foreach (var item in allowedPrivateCommand)
            {
                privateCommand.Add(new()
                {
                    Command = item.Key,
                    Description = item.Value,
                });
            }
            try
            {
                await BotClient.SetMyCommandsAsync(privateCommand, new BotCommandScopeAllPrivateChats());
                await BotClient.SetMyCommandsAsync(botCommands, new BotCommandScopeChat() { ChatId = Env.WORK_GROUP });
            }
            catch (Exception e)
            {
                Log.Warning(e, "Unable to regiser command.");
            }
        }
        #endregion
        #region Inline回调
        private Dictionary<string, Func<CallbackQuery, DbUser, string, Task>> inlineQueryFunction = new();
        async Task OnInlineQuery(CallbackQuery query, DbUser dbUser, string data)
        {
            foreach (var item in inlineQueryFunction.Keys)
            {
                if (query.Data!.StartsWith(item))
                {
                    await inlineQueryFunction[item].Invoke(query, dbUser, data);
                }
            }
        }
        public void RegisterInlineQuery(string startWith, Func<CallbackQuery, DbUser, string, Task> func)
        {
            Log.Debug("Registering callback {c}.", startWith);
            inlineQueryFunction.Add(startWith, func);
        }
        #endregion
        #region 音频


        List<Func<Audio, DbUser,string,Message, Task>> audioFunc = new();
        async Task OnAudio(Audio audio, DbUser dbUser,string caption,Message rawMessage)
        {
            foreach (var item in audioFunc)
            {
                await item.Invoke(audio, dbUser,caption,rawMessage);
            }
        }
        public void RegisterAudioMessage(Func<Audio, DbUser, string, Message, Task> func)
        {
            audioFunc.Add(func);
        }
        #endregion

    }
}