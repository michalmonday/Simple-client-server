using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

using ClientNamespace;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using ServerNamespace;

namespace ClientTests {

    [TestClass]
    public class FunctionalityTests {
        public static void killServer() {
            try {
                foreach (Process proc in Process.GetProcessesByName("Server"))
                    proc.Kill();
            } catch (Win32Exception e) {
                Console.WriteLine($"killServer - Win32Exception: {e.Message}");
            }
        }
        
        public FunctionalityTests () {
            killServer();
            Market.deleteStateFiles();
        }

        ~FunctionalityTests() {
            killServer();
        }

        [TestMethod]
        public void testServerReset() {
            // it will start the server, add multiple clients
            // then reset the server and check if the client data will
            // be preserved, in particular:
            // - stock holder is the same
            // - IDs are the same
            // - new client that connects after reset won't get previously used ID
            //      (so his ID is the highest)

            const int number_of_clients = 5;

            List<Client> clients = new List<Client>();
            for (int i = 0; i < number_of_clients; i++)
                clients.Add(new Client());

            // ensureServerRuns is a private method
            //PrivateObject client_1_priv = new PrivateObject(clients[0]);
            //client_1_priv.Invoke("ensureServerRuns");

            clients[0].ensureServerRuns();

            Dictionary<Client, int> client_ids = new Dictionary<Client, int>();
            List<Thread> threads = new List<Thread>();
            foreach (Client client in clients) {
                threads.Add(new Thread(() => {
                    client.connect();
                    Thread.Sleep(1000);
                    lock(client_ids)
                        client_ids[client] = client.id;
                }));
                threads.Last().IsBackground = true;
                threads.Last().Start();
            }

            foreach (Thread t in threads)
                t.Join();

            Constants.arbitraryResponseWait();

            // every client is updated with the stockholder so I'll use 1st to get it
            int stock_holder = clients[0].stock_holder;

            // give stock to client with ID 4
            // ensure it was received successfully
            Client client_stock_holder = client_ids.FirstOrDefault(x => x.Value == stock_holder).Key;
            client_stock_holder.requestStockTransfer(4);

            Constants.arbitraryResponseWait();
            Constants.arbitraryResponseWait();

            foreach (Client client in clients) {
                // ID is assigned upon reconnection, which happens 
                // within "client.recover" method. For that reason, 
                // this is the right place to assign clients invalid ID, 
                // this way it will be certain that server restored the 
                // right ID (using provided token, the same which was 
                // used before server was reset)
                client.id = 0xAAAAAA;
                client.stock_holder = 0xAAAAAA;
            }

            killServer();

            // server process should be automatically restarted by the client
            // but "ensureServerRuns" will block further code execution until this happens
            clients[0].ensureServerRuns();

            threads.Clear();
            foreach (Client client in clients) {
                threads.Add(new Thread(() => {
                    // let clients recognize the server is not running
                    // client.recover is called automatically when connection is lost
                    // that's the point of sending "." command
                    client.sendLine(".");
                    while (!client.is_connected)
                        Thread.Sleep(10);
                    Assert.IsTrue(client.is_connected, "Client isn't connected...");

                }));
                threads.Last().Start();
            }
            foreach (Thread t in threads)
                t.Join();


            // ensure that IDs didn't change in the process
            foreach (Client client in clients)
                Assert.IsTrue(client.id == client_ids[client], $"client.id = {client.id}, should be {client_ids[client]}");

            // check stock holder
            Console.WriteLine();
            Assert.IsTrue(clients[0].stock_holder == 4, $"clients[0].StockHolder = {clients[0].stock_holder}, but should be restored to client 4.");
        }
    }
}
