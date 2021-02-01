/*
    CE303 Client-server system
    Student ID: 1904535
    
    This file defines how the server handles each connection.
    For each connection 2 threads are created, 1 for reading
    and 1 for writing. 
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;

namespace ServerNamespace {
    class Connection : IDisposable {

        public static Market market { get; set; }

        // this will allow to send messages to all clients for immediate updates
        public static List<Connection> connections = new List<Connection>();
        private TcpClient tcp_client;
        private StreamWriter writer;
        private StreamReader reader;
        private Queue<string> messages_to_write;

        // reason behind having separate thread for reading and writing:
        //      I'd like to be able to send any message to any client at any moment.
        //      If reading and writing was done in the same thread then reading could block
        //      writing.
        private Thread thread_write;
        private Thread thread_receive;
        private int trader_id;
        private bool was_disposed;
        System.Windows.Forms.Timer keepalive_timer;
        Server server;

        private object lock_dispose_request;
        private volatile bool dispose_requested;
 
        public Connection(TcpClient tcp_client_, Server server_) {
            server = server_;
            lock_dispose_request = new object();
            dispose_requested = false;
            was_disposed = false;
            messages_to_write = new Queue<string>();
            trader_id = -1;
            tcp_client = tcp_client_;

            Stream stream = tcp_client.GetStream();
            writer = new StreamWriter(stream);
            reader = new StreamReader(stream);

            thread_receive = new Thread(Receiver);
            thread_receive.IsBackground = true;
            thread_receive.Name = $"Connection Receiver thread (managed id={thread_receive.ManagedThreadId})";
            thread_receive.Start();

            thread_write = new Thread(Writer);
            thread_write.IsBackground = true;
            thread_write.Name = $"Connection Writer thread (managed id={thread_receive.ManagedThreadId})";
            thread_write.Start();
        }

        public void addLineToSend(string line) {
            lock (messages_to_write)
                messages_to_write.Enqueue(line);
        }

        public static void sendLineToAll(string line) {
            lock (connections)
                foreach (Connection conn in connections)
                    conn.addLineToSend(line);
        }

        public static void sendLineToAllExcept(string line, int trader_id_excluded) {
            lock (connections)
                foreach (Connection conn in connections)
                    if (conn.trader_id != trader_id_excluded)
                        conn.addLineToSend(line);
        }

        public static void sendLineToSome(int[] ids, string line) {
            lock (connections)
                foreach (Connection conn in connections)
                    if (ids.Contains(conn.trader_id))
                        conn.addLineToSend(line);
        }


        private void Writer() {
            // runs as a thread
            keepalive_timer = new System.Windows.Forms.Timer();
            keepalive_timer.Tick += new EventHandler((Object o, EventArgs a) => {
                if (trader_id != -1)
                    addLineToSend(".");
            });
            keepalive_timer.Interval = 1000; // in miliseconds
            keepalive_timer.Start();

            while (true) {
                try {
                    lock (messages_to_write) {
                        while (messages_to_write.Count > 0) {
                            string msg = messages_to_write.Dequeue();
                            if (msg != ".")
                                Console.WriteLine($"Sending to trader {trader_id}: '{msg}'");
                            writer.WriteLine(msg);
                            writer.Flush();
                        }
                    }
                } catch (Exception e) {
                    Console.WriteLine($"Connection.Writer exception: {e.Message}");
                    Dispose();
                    return;
                }
                Thread.Sleep(50);

                lock (lock_dispose_request) {
                    if (dispose_requested) {
                        Dispose();
                        return;
                    }
                }
            } // while true
        }

        private void Receiver() {
            // runs as a thread

            try {
                // reading token, assigning ID
                openingSequence();

                while (true) {
                    string line;
                    lock (reader)
                        line = reader.ReadLine();

                    // Marquis of Lorne (stackoverflow user) about detecting disconnection:
                    //      "In TCP there is only one way to detect an orderly disconnect, 
                    //      and that is by getting zero as a return value from read()/recv()/recvXXX()
                    //      when reading.
                    //      There is also only one reliable way to detect a broken connection: by writing to it."
                    //      https://stackoverflow.com/a/17665015/4620679
                    if (line == null)
                        throw new Exception("Disconnected.");

                    string[] parts = line.Split(' ');
                    string command = parts[0].ToLower();
                    switch (command) {
                        // do nothing, it's just to recognize server going down
                        case ".":
                            break;
                        case "givestock":
                            int id_to_give;
                            try {
                                id_to_give = int.Parse(parts[1]);
                            } catch (Exception e) {
                                addLineToSend("givestock fail Wrong format used for 'givestock id'");
                                break;
                            }
                            handleGiveStock(id_to_give);

                            break;
                        default:
                            throw new Exception($"Unknown command: {parts[0]}.");
                    }
                }
            } catch (Exception e) {
                Console.WriteLine($"Connection.Reader exception: {e.Message}");
                /*try {
                    addLineToSend("ERROR " + e.Message);
                } catch {
                    Server.Log("Failed to send error message.");
                }*/
            } finally {
                lock (lock_dispose_request)
                    dispose_requested = true;
            }

        } // void Receiver

        private void openingSequence() {
            // communication that happens when new client connects, called by Receiver thread
            // client sends token, server checks if token was connected in the past and:
            //      - if token was found in "state.txt", it returns previously used ID (associated with token)
            //      - if token was not found in "state.txt", it creates new ID (which will be associated with the token in future)
            string token;
            lock (reader)
                token = reader.ReadLine();

            Console.WriteLine($"Adding trader with token = '{token}'");
            string msg;
            lock (market) {
                trader_id = market.addTrader(token);

                msg = $"{trader_id};{market.StockHolderId};{market.tradersCsv()}";//;{market.preResetTradersCsv()}";
                addLineToSend(msg);

                if (market.pre_reset_traders.Contains(trader_id)) {
                    market.pre_reset_traders.Remove(trader_id);

                    // reconnected is appended so client knows to not print any message
                    sendLineToAllExcept($"connected {trader_id} reconnected", trader_id);

                    server.Log($"Trader {trader_id} reconnected.");
                } else {
                    sendLineToAllExcept($"connected {trader_id}", trader_id);

                    server.Log($"New connection; trader ID {trader_id}");
                }

                lock (connections)
                    if (!connections.Contains(this))
                        connections.Add(this);
            }

            if (server.has_form)
                server.form.addTrader(trader_id);

            
        }

        private void handleGiveStock(int id_to_give) {
            // called by Receiver thread

            if (id_to_give == trader_id) {
                server.Log($"Trader {trader_id} gave himself the stock. Gross domestic product increased.");
                sendLineToAll($"givestock success {trader_id} {trader_id}");
                return;
            }

            lock (market) {
                if (!market.hasTrader(id_to_give)) {
                    server.Log($"Trader {trader_id} attempted to give the stock to a not existing trader {id_to_give}.");
                    addLineToSend($"givestock fail Supplied trader id ({id_to_give}) does not exist.");
                } else if (market.StockHolderId != trader_id) {
                    server.Log($"Trader {trader_id} attempted to give the stock without having it in the first place.");
                    addLineToSend($"givestock fail You are not in possession of a stock.");
                } else {
                    market.giveStock(id_to_give);
                    server.Log($"Trader {trader_id} gave trader {id_to_give} the stock.");
                    sendLineToAll($"givestock success {trader_id} {id_to_give}");
                }
            }
        }

        public void Dispose() {

            // this may deadlock but let's see
            // it feels necessary to ensure synchronization of market
            lock (market) {

                if (was_disposed)
                    return;
                keepalive_timer.Stop();
                was_disposed = true;

                server.Log($"Trader {trader_id} disconnected.");

                int stock_holder_id = market.StockHolderId;
                market.removeTrader(trader_id);

                if (stock_holder_id == trader_id) {
                    int new_holder_id = market.StockHolderId;

                    if (new_holder_id == -1) {
                        server.Log($"Trader {trader_id} was stock holder, he was the only trader, so the stock will be given to the first new trader.");
                    } else {
                        server.Log($"Trader {trader_id} was stock holder, the stock was given to trader {new_holder_id}.");
                        sendLineToAllExcept($"givestock left {trader_id} {new_holder_id}", trader_id);
                    }
                }



                if (Thread.CurrentThread == thread_receive)
                    thread_write.Abort();
                else if (Thread.CurrentThread == thread_write)
                    thread_receive.Abort();

                lock (reader)
                    reader.Close();
                writer.Close();
                tcp_client.Close();

                lock (connections)
                    connections.Remove(this);

                sendLineToAllExcept($"disconnected {trader_id}", trader_id);
            }

            if (server.has_form)
                server.form.removeTrader(trader_id);
        } // void Dispose

    } // class Connection
}
