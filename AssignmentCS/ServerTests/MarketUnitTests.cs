using System;
using System.IO;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ServerNamespace;

namespace ServerTests
{
    /*
        This class contains unit tests only.

        FuncitonalityTests.cs has tests focused 
        more on testing the server as a whole.
    */

    [TestClass]
    public class MarketUnitTests
    {

        private Market m;
        public MarketUnitTests() {
            m = new Market();
            m.reset();
        }

        [TestMethod]
        public void testSave()
        {
            //const string f_name = "testsave.txt";

            // market by default loads data at the begining
            // it should be cleared so it doesn't affect 
            // results of the test
            m.reset();

            m.addTrader("testSave_TOKEN_1");
            m.addTrader("testSave_TOKEN_2");
            m.addTrader("testSave_TOKEN_3");

            // trader 1 should be a token holder
            // trader 3 should have id equal to "highest id"
            

            m.save();
            Thread.Sleep(100);
            using (StreamReader sr = File.OpenText(Market.STATE_FILENAME))
            {
                // trader tokens/ids (ids start with 1 and get increased by 1 with each new trader)
                string s = sr.ReadLine();
                Assert.IsTrue(s != "");
                string[] entries = s.Split(';');
                Assert.IsTrue(entries[0].Split(',')[0] == "testSave_TOKEN_1");
                Assert.IsTrue(entries[0].Split(',')[1] == "1");
                Assert.IsTrue(entries[1].Split(',')[0] == "testSave_TOKEN_2");
                Assert.IsTrue(entries[1].Split(',')[1] == "2");
                Assert.IsTrue(entries[2].Split(',')[0] == "testSave_TOKEN_3");
                Assert.IsTrue(entries[2].Split(',')[1] == "3");

                // highest id
                s = sr.ReadLine();
                Assert.IsTrue(s != "");
                Assert.IsTrue(s.Trim() == "3");

                // stock holder
                s = sr.ReadLine();
                Assert.IsTrue(s != "");
                Assert.IsTrue(s.Trim() == "1");
            }
        }

        [TestMethod]
        public void testLoad()
        {
            //const string f_name = "testload.txt";
            // 1st scope (one market is created for the sake of saving data)
            {
                

                m.addTrader("testLoad_TOKEN_1");
                m.addTrader("testLoad_TOKEN_2");
                m.addTrader("testLoad_TOKEN_3");
                // trader 1 should be a token holder
                // trader 3 should have id equal to "highest id"

                // despite being removed, the "TOKEN_2" and its corresponding ID should be saved
                // and loaded later, so when client connects with "TOKEN_2" he should be assigned
                // his old ID (2), not "highest_id+1"
                m.removeTrader(2);
                m.save();
            }

            // 2nd scope (loading is done by another, freshly created market)
            {
                Market other_m = new Market();
                other_m.reset();

                other_m.load();
                Assert.IsTrue(other_m.highest_id == 3);
                Assert.IsTrue(other_m.StockHolderId == 1);

                // addTrader returns ID, if previously saved tokens were loaded
                // correctly, then IDs should match their previously saved tokens
                
                Assert.IsTrue(other_m.addTrader("testLoad_TOKEN_1") == 1);
                Assert.IsTrue(other_m.addTrader("testLoad_TOKEN_2") == 2);
                Assert.IsTrue(other_m.addTrader("testLoad_TOKEN_3") == 3);
            }

        }

        [TestMethod]
        public void testId()
        {
            // when market is created it should start assigning IDs from 1 upwards
            // 1st trader has ID 1, 2nd has ID 2 etc.
            Market m = new Market();
            m.reset();

            Assert.IsTrue(m.addTrader("testId_TOKEN_1") == 1);
            Assert.IsTrue(m.addTrader("testId_TOKEN_2") == 2);

            // ensure that ID 2 is not given to anyone else even if 
            // trader 2 was removed
            m.removeTrader(2);
            Assert.IsTrue(m.addTrader("testId_TOKEN_3") == 3);

            // ensure trader 2 receives his old ID back after joining with
            // the same token
            Assert.IsTrue(m.addTrader("testId_TOKEN_2") == 2);
        }

        [TestMethod]
        public void testStockHolderId()
        {
            // when market is created it should start assigning IDs from 1 upwards
            // 1st trader has ID 1, 2nd has ID 2 etc.
            Market m = new Market();
            m.reset();

            Assert.IsTrue(m.addTrader("testStockHolderId_TOKEN_1") == 1);
            Assert.IsTrue(m.addTrader("testStockHolderId_TOKEN_2") == 2);

            // stock should be given to 1st trader joining the market
            Assert.IsTrue(m.StockHolderId == 1);
            m.removeTrader(1);

            // stock should be transferred once the 1st trader leaves
            Assert.IsTrue(m.StockHolderId == 2);
            m.removeTrader(2);

            // stock should be taken (and set to -1) if no trader is left
            Assert.IsTrue(m.StockHolderId == -1);
        }

        [TestMethod]
        public void testHasTrader()
        {
            Market m = new Market();
            m.reset();
            for (int i = 1; i < 20; i++)
                m.addTrader($"testHasTrader_TOKEN_{i}");

            for (int i = 1; i < 20; i++)
                Assert.IsTrue(m.hasTrader(i));

            Assert.IsFalse(m.hasTrader(20));
            Assert.IsFalse(m.hasTrader(-1));
            Assert.IsFalse(m.hasTrader(0));
        }

        [TestMethod]
        public void testAddTrader()
        {
            Market m = new Market();
            m.reset();
            m.addTrader("testAddTrader_TOKEN_1");
            m.addTrader("testAddTrader_TOKEN_2");
            Assert.IsTrue(m.hasTrader(1), "Market does not contain 1st added trader.");
            Assert.IsTrue(m.hasTrader(2), "Market does not contain 2nd added trader.");
        }

        [TestMethod]
        public void testRemoveTrader()
        {
            Market m = new Market();
            m.reset();

            m.addTrader("testRemoveTrader_TOKEN_1");
            m.addTrader("testRemoveTrader_TOKEN_2");

            // ensure traders were added
            Assert.IsTrue(m.hasTrader(1), "Adding trader 1 failed.");
            Assert.IsTrue(m.hasTrader(2), "Adding trader 2 failed.");

            // remove traders
            m.removeTrader(1);
            m.removeTrader(2);

            // ensure traders were removed
            Assert.IsFalse(m.hasTrader(1), "Removal of trader 1 failed.");
            Assert.IsFalse(m.hasTrader(2), "Removal of trader 2 failed.");
        }

        [TestMethod]
        public void testGiveStock()
        {
            Market m = new Market();
            m.reset();
            m.addTrader("testGiveStock_TOKEN_1");
            m.addTrader("testGiveStock_TOKEN_2");
            m.addTrader("testGiveStock_TOKEN_3");
            m.addTrader("testGiveStock_TOKEN_4");

            Assert.IsTrue(m.StockHolderId == 1);

            // few givings that should not change the holder 
            // (because they are given to invalid ID)
            m.giveStock(5);
            Assert.IsTrue(m.StockHolderId == 1, "Stock was given to invalid ID (too high)");
            m.giveStock(0);
            Assert.IsTrue(m.StockHolderId == 1, "Stock was given to invalid ID (0)");
            m.giveStock(-1);
            Assert.IsTrue(m.StockHolderId == 1, "Stock was given to invalid ID (-1)");

            // givings to correct IDs that should change the holder
            for (int new_holder = 2; new_holder <= 4; new_holder++)
            {
                m.giveStock(new_holder);
                Assert.IsTrue(m.StockHolderId == new_holder, $"Stock holder {m.StockHolderId} was not updated accordingly after giving stock to new holder ({new_holder})");
            }

        }

        [TestMethod]
        public void testTradersStr()
        {
            Market m = new Market();
            m.reset();
            m.addTrader("testTradersStr_TOKEN_1");
            m.addTrader("testTradersStr_TOKEN_2");
            m.addTrader("testTradersStr_TOKEN_3");
            m.addTrader("testTradersStr_TOKEN_4");

            Assert.IsTrue(m.tradersCsv() == "1,2,3,4", "tradersStr return does not match expected format (coma separated trader IDs)");
        }
    }
}
