using MamoLib.Sql;
using KoriMiyohashi.Modules.Types;
using SqlSugar;

namespace KoriMiyohashi.Modules
{
    public class Repos
    {
        public ISqlSugarClient Client { get; set; }
        public DbUserRepo DbUsers { get; set; }
        public SongRepo Songs { get; set; }
        public SubmissionRepo Submissions { get; set; }

        public Repos()
        {
            Client = Connect();
            DbUsers = new DbUserRepo(Client);
            Songs = new SongRepo(Client);
            Submissions = new SubmissionRepo(Client);
            DbUsers.InitHeader();
            Songs.InitHeader();
            Submissions.InitHeader();
            Log.Debug("DB Init Done.");
        }
        ISqlSugarClient Connect()
        {
            Log.Debug("Connecting Database.");
            DbType dbType;
            string connectionString;
            switch (Env.DB_TYPE)
            {
                case "mysql":
                    connectionString = Env.DB_CONNECTION_STRING;
                    dbType = DbType.MySql;
                    break;

                case "sqlite":
                    connectionString = "datasource=" + Env.DB_FILE;
                    dbType = DbType.Sqlite;
                    break;

                default:
                    throw new ArgumentException($"Unsupported database types: {Env.DB_TYPE}");
            }
            var cf = new ConnectionConfig()
            {
                ConnectionString = connectionString,
                DbType = dbType,
                IsAutoCloseConnection = true
            };
            Log.Debug("Database config: {@0}",cf);
            var DbClient = new SqlSugarClient(cf, db =>
            {
                db.Aop.OnError = (e) => Log.Error(e, "Error executing SQL.");
            });

            if (!File.Exists(Env.DB_FILE) && Env.DB_TYPE == "sqlite")
            {
                Log.Warning("SQLITE file does not exist, creating a new {0}", Env.DB_FILE);
                File.Create(Env.DB_FILE).Close();
            }
            DbClient.Open();
            Log.Debug("Database Connected.");
            return DbClient;
        }
    }
    
    public class DbUserRepo(ISqlSugarClient content) : BaseRepository<DbUser>(content)
    { }

    public class SongRepo(ISqlSugarClient content) : BaseRepository<Song>(content)
    { }

    public class SubmissionRepo(ISqlSugarClient content) : BaseRepository<Submission>(content)
    { }
}