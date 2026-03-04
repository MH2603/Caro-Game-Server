// See https://aka.ms/new-console-template for more information

using Client;
using System.Net;
using System.Net.Sockets;

Console.WriteLine("Hello, World!");

GameManager gameManager = new GameManager();


while (true)
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("===== Let write your command ==== ");
    ShowCmd();
    Console.ResetColor();

    var cmd = Prompt(" Cmd: ");
    switch (cmd)
    {
        case "cmd":
            ShowCmd();
            break;
        case "login":
            Login();
            break;
        case "signup":
            SignUp();
            break;
        case "find_match":
            FindMatch();
            break;
        case "move":
            ExecuteTurn();
            break;
        default:
            Console.WriteLine($" Not found cmd {cmd}");
            break;
    }

    Thread.Sleep(500); 

    Console.WriteLine("---------------------");
}

void ExecuteTurn()
{
    var x = int.Parse(Prompt(" x :"));
    var y = int.Parse(Prompt(" y :"));
    gameManager.TryExecuteTurn(x, y);
}

void FindMatch()
{
    gameManager.FindMatch();
}

void SignUp()
{
    var username = Prompt("Username: ");
    var pw = Prompt("Password: ");

    gameManager.SignUp(username, pw);   
}

string Prompt(string message)
{
    Console.Write(message);
    return Console.ReadLine();
}

void ShowCmd()
{
    Console.WriteLine("# List cmd: cmd,login, signup, find_match, invite_match, move");
}

void Login()
{
    var username = Prompt("Username: ");
    var pw = Prompt("Password: ");

    gameManager.Login(username, pw);
}

