using KoriMiyohashi.Handlers;
using KoriMiyohashi.Modules;
using KoriMiyohashi.Modules.Types;

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

var listener = Hosting.GetRequiredService<Listener>();
var repos = Hosting.GetRequiredService<Repos>();
_ = listener.SetMyCommandsAsync();
var owner = new DbUser()
{
    Id = Env.OWNER,
    FullName = "管理员",
    Aduit = true,
    Owner = true,
};
repos.DbUsers.Storageable(owner).ExecuteCommand();
Thread.Sleep(-1);