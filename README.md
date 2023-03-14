# Luminous
Luminous is a high-level C# network library which can be used for Server-Client model. Library supports server tick rate and uses UDP protocol.
 
 Server Example
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
Client Example
```
using System;
using System.Timers;
using System.Diagnostics;
using System.Net;
using Luminous.Net;

public static class Program {
    static void Main() {
        Console.WriteLine("+-----------------+");
        Console.WriteLine("| Luminous Client |");
        Console.WriteLine("+-----------------+\n");

        int lineNumber = 0;
        Random rnd = new Random();

        IPEndPoint server = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 7447);
        LuminousNetwork client = new LuminousNetwork();
        client.Listen(server);

        Stopwatch sw = new Stopwatch();
        Timer timer = new Timer(1);
        timer.Elapsed += (object sender, ElapsedEventArgs e) => {
            string msg = String.Empty;
            for (int n = 0; n < 64; n++) msg += rnd.Next(10).ToString();
            client.Send(msg);
        };
        timer.AutoReset = true;
        timer.Enabled = true;
        sw.Start();

        client.OnReceived += (IPEndPoint ep, string data) => {
            Console.WriteLine("{0} >> {1} | Time: {2}", lineNumber++, data, sw.ElapsedMilliseconds);
            sw.Restart();
        };

        string cmd = Console.ReadLine();
        while (cmd != "exit") {
            switch (cmd) {
                case "!stress":
                    string msg = String.Empty;
                    for (int n = 0; n < 10240; n++) msg += rnd.Next(10).ToString();
                    client.Send(msg);
                    break;
                default:
                    client.Send(cmd);
                    break;
            }
            cmd = Console.ReadLine();
        }
    }
}
```
