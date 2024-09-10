using KoriMiyohashi.Modules;
using KoriMiyohashi.Modules.Types;
using System.Threading;
using Telegram.Bot.Types;
using KoriMiyohashi.Handlers;

Log.Information("Kori Miyohashi is a Telegram channel submission bot developed by Mamo. Current Build {0}", AppInfo.Version);
Log.Information("Kori Miyohashi is a character featured in Recette's game {0} and its spin-offs.", "しゅがてん！-sugarfull tempering-");
Log.Debug("Debug is enabled.");
Hosting.Register(s => s.AddSerilog());

Hosting.Register<Repos>();
Hosting.Register<Listener>();
Hosting.Build();

new InlineQuerys();
new Commands();
new TextMessgae();
new Audios();

var repos = Hosting.GetRequiredService<Repos>();

var unfinished = repos.Submissions.Queryable().Where(x => x.Status != "WAITING").First();
var text = $"你还有未完成的投稿！";


Thread.Sleep(-1);


