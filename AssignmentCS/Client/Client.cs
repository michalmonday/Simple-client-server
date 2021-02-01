/*
    CE303 Client-server system
    Student ID: 1904535
    
    This file defines communication logic used by the client
    to interact with the server. Additionally, it provides
    mechanism for restarting the server when it goes down.
    It uses a named mutex to recognize that 1 client running
    on the same system is already restarting the server.
    That is to prevent multiple servers being started at once.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Windows.Forms;

using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Diagnostics;
using System.Reflection;

namespace ClientNamespace {
    public class Client : IDisposable {
        const int port = 8888;
        
        private string token;
        private StreamReader reader;
        private StreamWriter writer;
        private bool connection_was_established;
        TcpClient tcp_client;
        private Thread thread_receiver;
        private System.Windows.Forms.Timer keepalive_timer;

        public bool is_connected { get; private set; }
        public List<string> traders { get; private set; }
        public int stock_holder { get; set; }
        public bool has_stock { get; private set; }

        private bool has_form;
        private ClientForm form;

        // public set is there for automated testing
        // where invalid value is set to ensure that original 
        // data is preserved and provided by the server after
        // restart
        public int id { get; set; }

        public Client(string token_ = "") {
            has_form = false;
            is_connected = false;
            traders = new List<string>();

            // Token will allow to identify client after server restart 
            // (to restore his old ID and possibly give him the stock)
            // It may be supplied in constructor (for unit testing)
            if (token_.Length == 0)
                token = generateUniqueToken();
            else
                token = token_;

            connection_was_established = false;

            keepalive_timer = new System.Windows.Forms.Timer();
            keepalive_timer.Tick += new EventHandler((Object o, EventArgs a) => {
                if (is_connected)
                    sendLine(".");
            });
            keepalive_timer.Interval = 1000; // in miliseconds
            keepalive_timer.Start();
        }

        public void setForm(ClientForm client_form) {
            form = client_form;
            has_form = true;
            form.Load += (object sender, EventArgs a) => {

                // it's started from separate thread so it doesn't block the window
                Thread t = new Thread(connect);
                t.IsBackground = true;
                t.Start();
            };
        }

        private void Receiver() {
            // runs as a thread

            while (true) {
                try {
                    string line;
                    lock (reader)
                        line = reader.ReadLine();

                    string[] parts = line.Split(' ');

                    if (line != ".") {
                        if (parts.Length < 2) {
                            Log($"Every message received from server should have at least 1 parameter, this one didn't: '{line}'");
                            continue;
                        } else {
                            Console.WriteLine($"ClientForm.Receiver line = '{line}'");
                        }
                    }

                    string command = parts[0].ToLower();
                    switch (command) {
                        case "connected":
                            string id_joined = parts[1];
                            if (id_joined != $"{id}" && !traders.Contains(id_joined)) {
                                if (has_form)
                                    form.addTrader(id_joined);
                                traders.Add(id_joined);

                                if (parts.Length == 3 && parts[2] == "reconnected") {
                                    // don't log anything
                                } else {
                                    Log($"New trader ({id_joined}) joined.");
                                }
                            }
                            break;
                        case "disconnected":
                            string id_left = parts[1];
                            if (has_form)
                                form.removeTrader(id_left);
                            traders.Remove(id_left);
                            Log($"Trader {id_left} left.");
                            break;
                        case "didnt_reconnect":
                            string[] ids = parts[1].Split(',');
                            if (ids.Length == 1)
                                Log($"Trader {ids[0]} did not reconnect after server restart (it is assumed he left).");
                            else if (ids.Length > 1)
                                Log($"Some traders (IDs: {parts[1]}) didn't reconnect after server restart (it is assumed they left).");

                            break;
                        case "givestock":
                            // 'result' could be: 
                            //      - success           (sent to all after normal transfer requested by stock owner)
                            //      - fail              (sent to stock owner only after unsuccessful transfer request)
                            //      - left              (sent to all after stock owner leaves the market)
                            //      - didnt_reconnect   (sent to all 5 seconds after server restart if owner does not reconnect)
                            string result = parts[1]; 
                            if (result == "success" || result == "left" || result == "didnt_reconnect") {
                                string id_from = parts[2];
                                string id_to = parts[3];
                                has_stock = int.Parse(id_to) == id;
                                stock_holder = int.Parse(id_to);
                                if (has_form)
                                    form.updateStockHolderGUI();
                                string receiver_str = has_stock ? "I" : $"Trader {id_to}";
                                string giver_str = id_from == $"{id}" ? "me" : $"trader {id_from}";
                                string msg = $"{receiver_str} received stock from {giver_str}";
                                if (result == "left") 
                                    msg += " who left the server while holding stock.";
                                else if (result == "didnt_reconnect") 
                                    msg += " who didn't reconnect following server restart.";
                                else 
                                    msg += ".";

                                Log(msg);
                            } else if (result == "fail") {
                                string reason = line.Replace("givestock fail ", "");
                                Log($"Giving stock failed, reason: {reason}");
                            }
                            break;
                        case ".":
                            // do nothing, it's just to check that client didn't go down
                            break; 
                        default:
                            Log("Unrecognized command received from server.", "WARNING");
                            Log($"Unrecognized command: {line}.", "WARNING");
                            break;
                    }
                } catch (IOException e) {
                    //Log($"ClientForm.Receiver, server may be down, IOException: {e.Message}");

                    // let the "keepalive" timer handle reconnection and just wait
                    Thread.Sleep(100);
                } catch (Exception e) {
                    //Log($"ClientForm.Receiver exception: {e.Message}");

                    // let the "keepalive" timer handle reconnection and just wait
                    Thread.Sleep(100);
                }
            } // while true 
        } // void Receiver

        public void recover() {
            is_connected = false;
            ensureServerRuns();
            connect();   
        }

        public void connect() {
            try {
                if (connection_was_established) {
                    tcp_client.Close();
                    writer.Close();
                    lock (reader)
                        reader.Close();
                    thread_receiver.Abort();
                }

                tcp_client = new TcpClient("localhost", port);
                tcp_client.ReceiveTimeout = 10000;// was 2000
                NetworkStream stream = tcp_client.GetStream();
                
                // lock object can't be null
                if (reader == null) reader = new StreamReader(stream);
                else lock (reader) reader = new StreamReader(stream);

                writer = new StreamWriter(stream);

                openingSequence();

                thread_receiver = new Thread(Receiver);
                thread_receiver.IsBackground = true;
                thread_receiver.Name = "Client Receiver thread";
                thread_receiver.Start();

                connection_was_established = true;
                is_connected = true;

            } catch (SocketException e) {
                //Log($"ClientForm.connect SocketException: {e.Message}");
                recover();
            } catch (IOException e) {
                //Log($"ClientForm.connect IOException: {e.Message}");
                recover();
            }
        }

        private void openingSequence() {
            string[] parts;
            lock (reader) {
                sendLine(token);
                parts = reader.ReadLine().Split(';');
            }
            Log(connection_was_established ? "Reconnected." : "Connected");

            // parsing values
            id = int.Parse(parts[0]);
            stock_holder = int.Parse(parts[1]);
            string[] trader_ids = parts[2].Split(',').Where(id_ => int.Parse(id_) != id).ToArray(); // exclude self id

            // using id
            if (has_form)
                form.setWindowTitle($"Client (id = {id})");

            // using stock holder
            has_stock = stock_holder == id;
            if (has_form)
                form.updateStockHolderGUI();

            // using trader ids
            traders.Clear();
            traders.AddRange(trader_ids);
            traders.Sort();

            if (has_form)
                form.setTraders(traders.ToArray());
        }
        
        public void requestStockTransfer(int id_to_give) {
            sendLine($"givestock {id_to_give}");
        }

        public void sendLine(string line) {
            try {
                lock (writer) {
                    writer.WriteLine(line);
                    writer.Flush();
                }
            } catch (Exception e) {
                //Console.WriteLine($"ClientForm.sendLine EXCEPTION: {e.Message}");
                recover();
            }
        }

        public void Dispose() {
            lock (reader) {
                reader.Close();
            }
            writer.Close();
            if (tcp_client.Connected)
                tcp_client.GetStream().Close();
            tcp_client.Close();
            thread_receiver.Abort();
        }

        public void ensureServerRuns() {
            Mutex mutex = new Mutex(false, "CE303_server_restart");

            // https://docs.microsoft.com/en-us/dotnet/api/system.threading.mutex?view=netcore-3.1
            // "0" as a parameter to WaitOne makes it proceed to "else" after 0 miliseconds of waiting to acquire mutex.
            // This way it does not wait to acquire mutex if another process already acquired it (and started the server).
            if (mutex.WaitOne(0)) {
                Log("Server is not running. Starting it...");
                try {
                    string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location).Replace(
                        "AssignmentCS\\Client\\bin",
                        "AssignmentCS\\Server\\bin");
                    Process.Start(path + "\\Server.exe");
                    waitTillServerRestarts();

                    // keep mutex locked for some additional time, so clients who realized a bit later that
                    // server is not running, will not run into open mutex and start server again
                    Thread.Sleep(1000); 
                } catch (Exception e) {
                    Log($"Restarting server failed, reason: {e.Message}", "WARNING");
                } finally {
                    mutex.ReleaseMutex();
                }
            } else {
                Log("Server is not running. One client is already restarting the server.");
                waitTillServerRestarts();
            }
        }

        private bool isServerRunning() {
            // check C#
            if (Process.GetProcessesByName("Server").Length > 0)
                return true;

            // check Java
            Process pProcess = new Process();
            pProcess.StartInfo.FileName = "cmd.exe";
            pProcess.StartInfo.Arguments = "/C jps | find \"ServerForm\"";
            pProcess.StartInfo.UseShellExecute = false;
            pProcess.StartInfo.RedirectStandardOutput = true;
            pProcess.StartInfo.CreateNoWindow = true;
            pProcess.Start();
            return pProcess.StandardOutput.ReadToEnd().Contains("ServerForm");
        }

        private void waitTillServerRestarts() {
            //Log("Waiting till server restarts...");
            while (!isServerRunning())
                Thread.Sleep(50);
        }

        private string generateUniqueToken() {
            // adapted from https://stackoverflow.com/a/730418/4620679
            Guid g = Guid.NewGuid();
            return Convert.ToBase64String(g.ToByteArray());
        }

        public void Log(string msg, string type = "INFO") {
            if (has_form)
                form.Log(new ListViewItem(new[] { type, DateTime.Now.ToString("h:mm:ss tt"), msg }));

            Console.WriteLine(msg);
        }
    }
}

