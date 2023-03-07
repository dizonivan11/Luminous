// #undef DEBUG

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Luminous.Net {
    public class LuminousNetwork : UdpClient {
        // EVENT HANDLERS
        public delegate void OnReceivedEventHandler(IPEndPoint ep, string data);
        public event OnReceivedEventHandler OnReceived;
        public delegate void OnUpdateEventHandler();
        public event OnUpdateEventHandler OnUpdate;

        // SETTINGS
        public int Port { get; } // Port to listen to
        public int TickRate { get; } // Server tick rate (frequency of update per second)
        public int Timeout { get; } // Time (in milliseconds) it takes to disconnect the inactive client socket
        public Encoding Encoding { get; } // Type of encoding to use

        // VARIABLES
        public bool Listening { get; private set; }
        private readonly Stopwatch timeElapsed;
        private readonly long timePerTick;

        // GLOBAL METHODS
        public void Stop() { Listening = false; }

        /// <summary>
        /// FOR SERVER ONLY
        /// </summary>
        /// <param name="port"></param>
        /// <param name="timeout"></param>
        /// <param name="encoding"></param>
        public LuminousNetwork(int port, Encoding encoding = null, int tickRate = 64, int timeout = 5000) : base(port) {
            Port = port;
            TickRate = tickRate;
            Timeout = timeout;
            Encoding = encoding ?? Encoding.UTF8;
            Listening = false;
            timeElapsed = new Stopwatch();
            timePerTick = 1000 / tickRate;
        }

        public void Listen() {
            if (!Listening) {
                Listening = true;
                Update();
                Task.Run(async () => {
                    Console.WriteLine("Server Started");

                    while (Listening) {
                        try {
                            UdpReceiveResult result = await ReceiveAsync();
                            IPEndPoint client = result.RemoteEndPoint;
                            string dataReceived = Encoding.GetString(result.Buffer);
                            OnReceived(client, dataReceived);
                        } catch (Exception ex) {
#if DEBUG
                            Console.WriteLine(ex.ToString());
#endif
                        }
                    }
                });
            }
        }

        public void Send(IPEndPoint ep, string data) {
            Task.Run(async () => {
                byte[] bytesToSend = Encoding.GetBytes(data);
                await SendAsync(bytesToSend, bytesToSend.Length, ep);
            });
        }

        private void Update() {
            timeElapsed.Start();
            Task.Run(() => {
                try {
                    while (Listening) {
                        if (timeElapsed.ElapsedMilliseconds < timePerTick) continue;
                        OnUpdate();
                        timeElapsed.Restart();
                    }
                } catch (Exception ex) {
#if DEBUG
                    Console.WriteLine(ex.ToString());
#endif
                }
            });
        }

        /// <summary>
        /// FOR CLIENT ONLY
        /// </summary>
        /// <param name="timeout"></param>
        /// <param name="encoding"></param>
        public LuminousNetwork(Encoding encoding = null, int tickRate = 64, int timeout = 5000) {
            TickRate = tickRate;
            Timeout = timeout;
            Encoding = encoding ?? Encoding.UTF8;
            Listening = false;
            timePerTick = 1000 / tickRate;
        }

        public void Listen(IPEndPoint ep) {
            if (!Listening) {
                Listening = true;
                Connect(ep);
                Task.Run(async () => {
                    while (Listening) {
                        UdpReceiveResult result = await ReceiveAsync();
                        string dataReceived = Encoding.GetString(result.Buffer);
                        OnReceived(ep, dataReceived);
                    }
                });
            }
        }

        public void Send(string data) {
            Task.Run(async () => {
                byte[] bytesToSend = Encoding.GetBytes(data);
                await SendAsync(bytesToSend, bytesToSend.Length);
            });
        }
    }
}
