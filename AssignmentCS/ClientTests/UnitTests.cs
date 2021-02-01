using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using ClientNamespace;
using System.Diagnostics;
using System.Threading;
using System.Collections.Generic;
using ServerNamespace;

namespace ClientTests {

    public static class Constants {
        // This "Constants" class is created to make calls to arbitraryResponseWait
        // from several classes in ClientTests namespace like:
        //    - UnitTests class
        //    - FunctionalityTests class

        // Using suggestion from: https://stackoverflow.com/questions/30206855/are-global-constants-possible/30206902

        // client receives/sends from 2 independent threads
        // RESPONSE_ALLOWANCE is the number of milliseconds
        // between sending a request for some update
        // and checking if the client was updated using 
        // received response
        public const int RESPONSE_ALLOWANCE = 500;

        public static void arbitraryResponseWait() { 
            Thread.Sleep(RESPONSE_ALLOWANCE);
        }
    }

    [TestClass]
    public class UnitTests {

        //https://stackoverflow.com/questions/9122708/unit-testing-private-methods-in-c-sharp
        private PrivateObject client_priv;

        public UnitTests() {
            Market.deleteStateFiles();
        }

        ~UnitTests() {
            FunctionalityTests.killServer();
        }

        [TestMethod]
        public void testGenerateUniqueToken()
        {
            Client client = new Client();
            client_priv = new PrivateObject(client);
            for (int i = 0; i < 100; i++)
            {

                string token = (string)client_priv.Invoke("generateUniqueToken");

                Assert.IsNotNull(token, "Token is null.");

                // ensure it does not contain comma, space or semicolon
                // coma and semicolon are used to separate ids/other tokens
                // (in order to restore state of server after restart)
                Assert.IsTrue(token.IndexOfAny(" ,;".ToCharArray()) == -1, "Token contains invalid characters (space, comma, semicolon).");

                // ensure tokens are unique
                string another_token = (string)client_priv.Invoke("generateUniqueToken");
                Assert.IsTrue(token != another_token, "Identical token was generated.");
            }
        }

        [TestMethod]
        public void testEnsureServerRuns()
        {
            Client client = new Client();

            // ensure no server runs
            FunctionalityTests.killServer();

            while (Process.GetProcessesByName("Server").Length > 0) { Thread.Sleep(10); }

            client.ensureServerRuns();
            Assert.IsTrue(Process.GetProcessesByName("Server").Length > 0, "Server process does not exist after calling 'ensureServerRuns'.");
        }

        [TestMethod]
        public void testRecover() {
            // recover restarts server if it's needed
            // and reconnects
            Client client = new Client();
            FunctionalityTests.killServer();
            client.recover();
            Assert.IsTrue(client.is_connected, "Client did not restart/reconnect properly.");
        }

        [TestMethod]
        public void testConnect() {
            Client client = new Client();
            client.ensureServerRuns();
            client.connect();
            Assert.IsTrue(client.is_connected, "Connecting failed.");
        }

        [TestMethod]
        public void testRequestStockTransfer() {
            FunctionalityTests.killServer();
            Client client = new Client();
            client.connect();
            
            // spawn another client and ensure he doesn't have the stock
            Client another_client = new Client();
            another_client.connect();
            Constants.arbitraryResponseWait();
            Console.Write($"testRequestStockTransfer another_client.id = {another_client.id}, another_client.stock_holder_id = {another_client.stock_holder}");
            Assert.IsFalse(another_client.has_stock, "another_client also has stock (after connecting), this shouldn't be the case.");

            // ensure stock is transfered
            client.requestStockTransfer(another_client.id);
            Constants.arbitraryResponseWait();
            Assert.IsTrue(another_client.has_stock, "another_client did not receive the stock after transfer.");
            Assert.IsFalse(client.has_stock, "client did not lose the stock after giving it.");
        }
    }
}
