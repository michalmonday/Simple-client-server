/*
    CE303 Client-server system
    Student ID: 1904535

    This file defines how the server listens to and accepts new connections.
    Once a new connection is made, it creates new "Connection" class object,
    of which behavior is defined in Connection.cs file.

    It also recognizes if the server went down while having connected clients.
    In such scenario it ensures that:
        - stock is given to another client if the original owner does not reconnect within 5 seconds
        - clients get notified about lack of reconnection if the previously connected clients don't reconnect within 5 seconds 
*/

using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;


using System.Windows.Forms;

namespace ServerNamespace {
    public class Server : IDisposable
    {
        public TcpListener listener { get; private set; }
        private const int port = 8888;
        public Market market { get; private set; }
        public ServerForm form { get; private set; }
        public bool has_form { get; private set; }
        public bool loaded { get; private set; }

        public Server(ServerForm form_ = null) {
            loaded = false;
            form = form_;
            has_form = form_ != null;

            market = new Market(this);
            Connection.market = market;

            Action on_load = () => {
                if (market.was_market_interrupted) {
                    Log("The server was closed or killed while having connected clients, data will be restored.", "WARNING");
                    Log($"If the stock holder ({market.StockHolderId}) does not return within 5 seconds, the stock will be released.", "WARNING");
                    scheduleStockReleaseIfOwnerAbandoned(5000);
                    scheduleDidntReconnectMessages(5000);
                }

                Thread t = new Thread(RunServer);
                // if has form then thread cannot exist without it
                // if does not have form, then thread can exist on it's own
                t.IsBackground = has_form;
                t.Name = $"RunServer thread";
                t.Start();
                loaded = true;
            };

            if (has_form)
                form.Load += new EventHandler((object o, EventArgs e) => on_load());
            else
                on_load();
        }

        private void RunServer() {
            // runs as a thread

            listener = new TcpListener(IPAddress.Loopback, port);

            while (!listener.Server.IsBound) {
                try {
                    // this fails if server is already running
                    // in the main function there's a check if this server is already running
                    // but still, this could possibly fail if the port is already used by some 
                    // other server
                    listener.Start();
                }  catch (SocketException e) {
                    Log($"RunServer failed start: {e.Message}", "WARNING");
                    Log($"RunServer failed start: Waiting 3 seconds and retrying...", "WARNING");
                    Thread.Sleep(3000);
                    Log("Retrying...", "WARNING");
                }
            }

            Log("Waiting for incoming connections...");

            // "listener.Pending()" is for the sake of unblocking listener 
            // (useful in case if Dispose was called from separate thread 
            // like unit tests, dispose acquires a lock on listener and closes it)
            while (true) {
                try {
                    lock (listener) 
                        if (listener.Pending()) 
                            new Connection(listener.AcceptTcpClient(), this);
                } catch (ObjectDisposedException e) {
                    return;
                } catch (InvalidOperationException) {
                    return;
                }
                // it may be surprising but this seemingly
                // insignificant delay decreases CPU usage from 25% to around 0%
                // it's like that because "listener.Pending" makes it not blocking.
                Thread.Sleep(1);
            }
        }

        private void scheduleStockReleaseIfOwnerAbandoned(int delay) {
            System.Windows.Forms.Timer one_shot = new System.Windows.Forms.Timer();
            Action release_stock = () => {
                bool came_back;
                lock (market)
                    came_back = market.stock_owner_came_back_after_reset;
                if (!came_back) {
                    int abandoned_trader, new_owner;
                    lock (market) {
                        abandoned_trader = market.StockHolderId;
                        new_owner = market.releaseStock();
                    }
                    if (new_owner != -1) {
                        Connection.sendLineToAll($"givestock didnt_reconnect {abandoned_trader} {new_owner}");
                        Log($"The stock owner didn't connect back within {delay}ms, stock was released from client {abandoned_trader} and given to client {new_owner}");
                    } else {
                        Log($"The stock owner didn't connect back within {delay}ms, stock was released from client {abandoned_trader} it will be given to the first new client.");
                    }
                }
            };
            delayedAction(release_stock, delay);
        }

        private void scheduleDidntReconnectMessages(int delay) {
            Action send_disconnect_messages = () => {
                lock (market)
                    if (market.pre_reset_traders.Count > 0)
                        Connection.sendLineToAll($"didnt_reconnect {market.preResetTradersCsv()}");
            };
            delayedAction(send_disconnect_messages, delay);
        }

        private void delayedAction(Action action, int delay) {
            System.Windows.Forms.Timer one_shot = new System.Windows.Forms.Timer();
            one_shot.Tick += (object sender, EventArgs a) => {
                action();
                ((System.Windows.Forms.Timer)sender).Dispose();
            };
            one_shot.Interval = delay;
            one_shot.Start();
        }

        public void Log(string msg, string type = "INFO") {
            if (has_form)
                form.Log(new ListViewItem(new[] { type, DateTime.Now.ToString("h:mm:ss tt"), msg }));
            Console.WriteLine(msg);
        }

        public void Dispose() {
            lock (listener) {
                listener.Server.Close();
                listener.Stop();
            }
        }

        static public bool isServerAlreadyRunning() {
            // check csharp
            if (Process.GetProcessesByName("Server").Length > 1)
                return true;

            // check java
            Process pProcess = new Process();
            pProcess.StartInfo.FileName = "cmd.exe";
            pProcess.StartInfo.Arguments = "/C jps | find \"ServerForm\"";
            pProcess.StartInfo.UseShellExecute = false;
            pProcess.StartInfo.RedirectStandardOutput = true;
            pProcess.StartInfo.CreateNoWindow = true;
            pProcess.Start();

            //Get program output
            return pProcess.StandardOutput.ReadToEnd().Contains("ServerForm");
        }
    }
}// namespace ServerNamespace
