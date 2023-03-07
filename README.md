# Luminous
Luminous is a high-level C# network library which can be used for Server-Client model. Library supports server tick rate and uses UDP protocol.
 
 Server example
 ```
using System;
using System.Text;
using System.Net;
using System.Collections.Concurrent;
using Luminous.Net;

public static class Program {
    public static void Main(string[] args) {
        Console.WriteLine("+-----------------+");
        Console.WriteLine("| Luminous Server |");
        Console.WriteLine("+-----------------+\n");

        ConcurrentDictionary<IPEndPoint, string> requests = new ConcurrentDictionary<IPEndPoint, string>();
        LuminousNetwork server = new LuminousNetwork(7447, Encoding.UTF8, 64);

        server.OnReceived += (IPEndPoint client, string data) => {
            if (!requests.ContainsKey(client)) requests.TryAdd(client, data);
        };

        server.OnUpdate += () => {
            Console.Clear();
            Console.WriteLine("Client requests: {0}", requests.Keys.Count);
            foreach (IPEndPoint client in requests.Keys) {
                string request = string.Empty;
                requests.TryRemove(client, out request);
                Console.WriteLine("{0} >> {1}", client.ToString(), request.Length);
                server.Send(client, "Received Data (" + request.Length + ")");
            }
        };

        server.Listen();

        string cmd = Console.ReadLine();
        while (cmd != "exit") {
            switch (cmd) {
                case "help":
                    Console.WriteLine("help: display all commands");
                    Console.WriteLine("client: Current client connected");
                    Console.WriteLine("exit: shutdown the server");
                    break;
                case "client":
                    // Console.WriteLine("Clients connected: {0}", server.Clients.Count);
                    break;
                default:
                    break;
            }
            cmd = Console.ReadLine();
        }
    }
}
 ```
