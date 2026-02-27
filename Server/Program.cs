// See https://aka.ms/new-console-template for more information
using Server;
using Server.Database;
using Server.GameLogic;
using SQLitePCL;

Console.WriteLine("Hello, World!");

// 1. Setup DB
Batteries.Init();
var db = new SQLiteDbContext("mygame.db");
var playerRepo = new SQLitePlayerRepository(db);
await playerRepo.Init();
//ServiceLocator.RegisterService<IPlayerRepository>(playerRepo);


// init sessions
SessionManager.Instance.Start();

// starting listen client connect
var networkListener = new NetworkListener();    
networkListener.Start();


var playerManager = new PlayerManager(playerRepo);

var matchManager = new MatchManager(playerManager);


while (true)
{

}