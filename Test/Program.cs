// See https://aka.ms/new-console-template for more information
using Shared.Logic;

Console.WriteLine("Hello, World!");

//Console.WriteLine("Input: ");
//var input = Console.ReadLine();

var cmd = new c2s_signup("mh", "123");

var bytes = StructByteConverter.ToBytes(cmd);

Console.WriteLine($" Size ");
