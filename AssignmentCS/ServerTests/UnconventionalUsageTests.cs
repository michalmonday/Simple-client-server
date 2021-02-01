using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using ClientNamespace;
using ServerNamespace;


namespace ServerTests {
    [TestClass]
    public class UnconventionalUsageTests
    {
        // Normally, only the client application should be used to interact with the server.
        // But a malicious person, or malfunctioning client could possibly send something
        // that the server cannot handle.
        // The purpose of this class is to send this kind of unconventional messages 
        // and ensure that server handles them well (e.g. server doesn't crash).

        private const int server_port = 8888;
        private const string server_path = "..\\..\\..\\Server\\bin\\Release\\";
        private Server server;
        private Thread thread_server;
        private TcpClient tcp_client;
        private StreamReader reader;
        private StreamWriter writer;
        private bool was_connected;
        public UnconventionalUsageTests()
        {
            was_connected = false;
            FunctionalityTests.killServerProcess();
            server = new Server();
            /*thread_server = new Thread();
            thread_server.IsBackground = true;
            thread_server.Start();
            Thread.Sleep(3000);*/
        }
        ~UnconventionalUsageTests()
        {
            // cleanup
            reader.Close();
            writer.Close();
            tcp_client.Close();
            server.Dispose();
            //thread_server.Abort();
            FunctionalityTests.killServerProcess();
        }

        private void ensureConnection()
        {
            if (was_connected)
            {
                reader.Close();
                writer.Close();
                tcp_client.Close();
            }

            tcp_client = new TcpClient("localhost", 8888);
            tcp_client.ReceiveTimeout = 10000; // was 2000

            reader = new StreamReader(tcp_client.GetStream());
            writer = new StreamWriter(tcp_client.GetStream());

            was_connected = true;

            // opening connection sequence
            writer.WriteLine("token");
            writer.Flush();
            string id_to_ignore = reader.ReadLine();
            Console.WriteLine($"ensureConnection id_to_ignore = {id_to_ignore}");
        }

        
        private bool isServerRunning()
        {
            Client client = new Client();
            client.connect();
            int start_time = Environment.TickCount;
            while (Environment.TickCount - start_time < 3000) {
                if (client.is_connected)
                    return true;
                else
                    Thread.Sleep(10);
            }
            return false;
        }

        [TestMethod]
        public void testLongMessage()
        {
            ensureConnection();

            for (int i = 0; i < 100000; i++)
                writer.Write("A");
            writer.Flush();
            try
            {
                Console.Write($"testLongMessage response = {reader.ReadToEnd()}");
            } catch (Exception e)
            {
                Console.Write($"testLongMessage exception when receiving response: {e.Message}");
            }

            Assert.IsTrue(isServerRunning(), "Server is not running anymore.");
        } 

        [TestMethod]
        public void testInvalidGiveStockID()
        {
            ensureConnection();
            foreach (string line in new string[] {
                "givestock string_instead_of_number", // string instead of number
                "givestock -1",     // negative number
                "givestock ",       // no number at all (with space)
                "givestock"})       // no number at all (without space)
            {
                

                writer.WriteLine(line);
                writer.Flush();
                string response = reader.ReadLine();
                Console.WriteLine($"testInvalidGiveStockID '{line}' response: '{response}'");

                Assert.IsTrue(isServerRunning(), $"Sending '{line}' probably crashed the server.");
            }
        }
    }
}
