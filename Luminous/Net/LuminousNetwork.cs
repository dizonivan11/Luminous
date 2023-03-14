// #undef DEBUG

using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
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
        /// For server only
        /// </summary>
        /// <param name="port">Port to listen to</param>
        /// <param name="timeout">Time (in milliseconds) it takes to disconnect the inactive client socket</param>
        /// <param name="tickRate">Server tick rate (frequency of update per second)</param>
        /// <param name="encoding">Type of encoding to use</param>
        public LuminousNetwork(int port, Encoding encoding = null, int tickRate = 64, int timeout = 5000) : base(port) {
            Port = port;
            TickRate = tickRate;
            Timeout = timeout;
            Encoding = encoding ?? Encoding.UTF8;
            Listening = false;
            timeElapsed = new Stopwatch();
            timePerTick = 1000 / tickRate;
        }

        /// <summary>
        /// Start listening to remote sockets
        /// </summary>
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

        /// <summary>
        /// Send data to a specific endpoint
        /// </summary>
        /// <param name="ep">Receiving endpoint</param>
        /// <param name="data">Data to be sent</param>
        public void Send(IPEndPoint ep, string data) {
            Task.Run(async () => {
                byte[] bytesToSend = Encoding.GetBytes(data);
                await SendAsync(bytesToSend, bytesToSend.Length, ep);
            });
        }

        /// <summary>
        /// Update server on interval based on defined tick rate
        /// </summary>
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
        /// For client only
        /// </summary>
        /// <param name="encoding">Type of encoding to use</param>
        public LuminousNetwork(Encoding encoding = null) {
            Port = -1;
            TickRate = -1;
            Timeout = -1;
            Encoding = encoding ?? Encoding.UTF8;
            Listening = false;
            timeElapsed = null;
            timePerTick = -1;
        }

        /// <summary>
        /// Start listening to host socket
        /// </summary>
        /// <param name="ep">Host endpoint</param>
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

        /// <summary>
        /// Send data to a connected host endpoint
        /// </summary>
        /// <param name="data">Data to be sent</param>
        public void Send(string data) {
            Task.Run(async () => {
                byte[] bytesToSend = Encoding.GetBytes(data);
                await SendAsync(bytesToSend, bytesToSend.Length);
            });
        }
    }
}
