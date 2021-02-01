using ClientNamespace;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ServerNamespace;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace ServerTests {
    [TestClass]
    public class FunctionalityTests {
        private const string server_path = "..\\..\\..\\Server\\bin\\Release\\";
        private const string client_path = "..\\..\\..\\Client\\bin\\Release\\";

        private Market market;
        private Server server;
        private Thread thread_server;

        public FunctionalityTests() {
            //Market.deleteStateFiles();
            market = null;
            server = new Server();
            /*thread_server = new Thread(server.init);
            thread_server.IsBackground = true;
            thread_server.Start();*/
        }

        ~FunctionalityTests() {
            killServerProcess();
            foreach (Process client_proc in Process.GetProcessesByName("Client"))
                client_proc.Kill();

            //if (thread_server != null)
            //    thread_server.Abort();

            if (server != null)
                server.Dispose();
        }

        [TestMethod]
        public void testMultipleLaunches() {
            // when server executable is ran multiple times, it should terminate
            // itself, so only 1 server process (first) stays alive

            Process.Start($"{server_path}Server.exe");
            Process.Start($"{server_path}Server.exe");
            Process.Start($"{server_path}Server.exe");

            Thread.Sleep(2000);
            Process[] processes = Process.GetProcessesByName("Server");
            Assert.IsTrue(processes.Length < 2, $"{processes.Length} server processes are running.");
        }

        [TestMethod]
        public void testFaqSuggestions() {
            // "Another example is a server - side unit test that initiates the server, 
            // starts several clients(by starting corresponding executables), verifies 
            // that they all connect to the server, closes some of them (by killing 
            // corresponding processes), and checks that the server correctly identifies
            // the remaining players."

            while (market == null) {
                market = server.market;
                Thread.Sleep(10);
            }


            // start 5 clients
            List<Process> clients = new List<Process>();
            for (int i = 0; i < 5; i++)
                clients.Add(startClient());

            Thread.Sleep(4000);


            Assert.IsTrue(market.traders.Count == 5, $"5 traders were added but traders count is {market.traders.Count}.");

            // kill clients 1 by 1 and check if the number of connected traders
            // changes accordingly (expected_client_count is decreased
            // with each iteration using "--" operator)
            int expected_client_count = market.traders.Count;
            foreach (Process client in clients) {
                // get id by extracting it from client window title 
                // (@ ensures that backslash is treated as backslash 
                // and not as escape char)
                int removed_client_id = int.Parse(Regex.Match(client.MainWindowTitle, @"\d+").Value);

                // ensure that the market contains the client id that is about
                // to be removed
                Assert.IsTrue(market.hasTrader(removed_client_id), $"Trader to be removed ({removed_client_id}) isn't present in the market even before removal.");
                client.Kill();
                Thread.Sleep(1000);

                // ensure that the number of traders decreased
                Assert.AreEqual(market.traders.Count, --expected_client_count, $"The number of traders in the market was not updated appropriately after removal of client {removed_client_id}.");

                // ensure that the market does not contain the removed client id anymore
                Assert.IsFalse(market.hasTrader(removed_client_id), $"Following removal of client {removed_client_id}, the market still kept him in.");
            }

        }


        [TestMethod]
        public void testStockManipulation() {
            // this should test all situations:
            // - client leaving, stock given to another trader
            // - client leaving, stock taken away (because no one is online)
            // - first client joining, stock given to him
            // - client not reconnecting within 5 seconds, stock given to another trader
            // - client not reconnecting within 5 seconds, stock taken away (because no one is online)

            // similar looking test is done in ClientsTests.FunctionalityTests.testServerReset
            // but this one using using market perspective
            Dictionary<int, Client> clients = joinClients(2);

            // ensureServerRuns checks for "Server" process, but when unit testing, the process isn't called "Server"
            // so it starts a new one, which is bad
            //clients.First().Value.ensureServerRuns();
            while (market == null) {
                market = server.market;
                Thread.Sleep(10);
            }
            // (just in case if server was killed last time and stock was about to be given/released within 5 seconds)
            // (to original owner)
            if (!market.hasTrader(market.StockHolderId))
                market.releaseStock();

            // disconnect stock holder
            int stock_holder_id = market.StockHolderId;
            clients[stock_holder_id].Dispose();
            clients.Remove(stock_holder_id);
            Thread.Sleep(1500);

            Client remaining_client = clients.First().Value;
            int remaining_client_id = remaining_client.id;

            // only 1 trader is remaining at this point, so he should be the new stock holder
            Assert.AreEqual(remaining_client_id, market.StockHolderId, "The only remaining client didn't get stock after stock holder left.");

            // disconnect the only client
            remaining_client.Dispose();

            // at this point the stock should belong to no one
            Thread.Sleep(1500);
            Assert.AreEqual(-1, market.StockHolderId, $"Stock wasn't released after the last client left.");

            //Dictionary<int, Client> new_clients = joinClients(1);
            Client client = joinClients(1).First().Value;
            Thread.Sleep(1500);
            int client_id = client.id;
            Assert.AreEqual(client_id, market.StockHolderId, $"Stock wasn't given to the first connected client.");


            // join additional clients and add stock to one of them
            clients = joinClients(5);
            Client new_stock_holder = clients.Last().Value;
            client.requestStockTransfer(new_stock_holder.id);
            Thread.Sleep(1500);

            Assert.AreEqual(new_stock_holder.id, market.StockHolderId, "Stock wasn't given to arbitrary (not-first) trader.");

            /* restarting server (not as a separate process) doesn't work well here
                * because clients restart it on their own...
                * 
                * killServer();
            Assert.IsNull(market, "Market isn't null after server reset (it should be to ensure the values got restored, and didn't just stay).");

            //startServer();

            Thread.Sleep(4000);
            foreach (Client c in clients.Values)
                c.connect();

            client.connect();
            Thread.Sleep(3000);

            Assert.AreEqual(new_stock_holder.id, market.stock_holder_id, "Stock wasn't restored to original stock holder after server restart.");*/


        }

        public static Dictionary<int, Client> joinClients(int number_of_clients) {
            List<Client> clients = new List<Client>();
            for (int i = 0; i < number_of_clients; i++)
                clients.Add(new Client());

            Dictionary<int, Client> clients_ids = new Dictionary<int, Client>();
            List<Thread> threads = new List<Thread>();
            foreach (Client client in clients) {
                threads.Add(new Thread(() => {
                    client.connect();
                    while (!client.is_connected)
                        Thread.Sleep(10);
                    lock (clients_ids)
                        clients_ids[client.id] = client;
                }));
                threads.Last().IsBackground = true;
                threads.Last().Start();
            }

            foreach (Thread t in threads)
                t.Join();
            return clients_ids;
        }

        public void testTransparentServerRestart() {
            // this needs to ensure that the market is in the same state it was after reset 
            // - the stock holder being the same
            // - reconnected clients being given the same IDs

        }

        public static void killServerProcess() {
            foreach (Process proc in Process.GetProcessesByName("Server"))
                proc.Kill();
        }

        public static bool isServerRunning() {
            // this doesn't work when server is created for unit tests, like:
            // Server server = new Server();
            return Process.GetProcessesByName("Server").Length > 0;
        }

        private Process startClient() {
            ProcessStartInfo _processStartInfo = new ProcessStartInfo();
            _processStartInfo.WorkingDirectory = client_path;
            _processStartInfo.FileName = "Client.exe";
            return Process.Start(_processStartInfo);
        }
    }
}
