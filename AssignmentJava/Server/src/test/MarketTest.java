package test;

import main.Market;
import org.junit.jupiter.api.AfterEach;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;

import static org.junit.jupiter.api.Assertions.*;


class  MarketTest {
    Market m;

    @BeforeEach
    void setUp() {
        m = new Market();
    }

    @AfterEach
    void tearDown() {
    }

    @Test
    void save_load() {
        m.reset();
        m.addTrader("testSave_TOKEN_1");
        m.addTrader("testSave_TOKEN_2");
        m.addTrader("testSave_TOKEN_3");

        int stock_holder = m.stock_holder_id;
        int highest_id = m.highest_id;
        m.save();
        m.reset();
        m.load();
        assertEquals(stock_holder, m.stock_holder_id,"Stock holder should be loaded with previously saved value.");
        assertEquals(highest_id, m.highest_id, "Highest id should be loaded with previously saved value.");
    }

    @Test
    void reset() {
        int trader_id = m.addTrader("testReset_TOKEN_1");
        m.reset();
        assertFalse(m.hasTrader(trader_id), "It shouldn't have any traders following reset.");
        assertEquals(-1, m.stock_holder_id,"Stock holder should be reset to starting value which is -1.");
        assertEquals(0, m.highest_id, "Highest id should be reset to starting value which is 0.");
        assertTrue(m.tradersCsv().isEmpty(), "There should be no traders following the reset.");
        assertTrue(m.preResetTradersCsv().isEmpty(), "There should be no 'pre_reset_traders' following reset ('pre_reset' refers to server going down, not 'market.reset()' method).");
    }

    @Test
    void stockHolderId() {
        m.reset();
        int trader_id = m.addTrader("testStockHolderId_TOKEN_1");
        assertEquals(trader_id, m.stockHolderId());
    }

    @Test
    void hasTrader() {
        m.reset();
        assertFalse(m.hasTrader(1), "Market shouldn't have any traders following reset.");
        m.addTrader("testHasTrader_TOKEN_1");
        assertTrue(m.hasTrader(1), "First trader was added but hasTrader(1) returns false.");
    }

    @Test
    void releaseStock() {
        // release stock method is called 5 seconds after server restart if the owner doesn't come back
        // if there are any clients remaining, the first of them should receive it
        // if there are no clients remaining, the stock holder should be set to -1
        int trader_1_id = m.addTrader("testReleaseStock_TOKEN_1");
        int trader_2_id = m.addTrader("testReleaseStock_TOKEN_2");
        m.giveStock(trader_2_id);
        assertEquals(trader_2_id, m.stockHolderId(), "2nd trader didn't receive the stock.");

        // stock should be given back to trader 1
        m.releaseStock();
        assertEquals(trader_1_id, m.stockHolderId(), "Stock wasn't transferred to the first trader.");

        // now test the same but without any traders left
        m.traders.clear();
        m.releaseStock();
        assertEquals(-1, m.stockHolderId(), "Stock holder value should be -1 if the stock is released without any remaining traders on the market.");
    }

    @Test
    void addTrader() {
        int trader_id = m.addTrader("testAddTrader_TOKEN_1");
        assertTrue(m.hasTrader(trader_id), "Trader wasn't added.");
    }

    @Test
    void removeTrader() {
        int trader_id = m.addTrader("testRemoveTrader_TOKEN_1");
        assertTrue(m.hasTrader(trader_id), "Trader wasn't added.");
        m.removeTrader(trader_id);
        assertFalse(m.hasTrader(trader_id), "Trader wasn't removed.");
    }

    @Test
    void giveStock() {
        int trader_1_id = m.addTrader("testGiveStock_TOKEN_1");
        int trader_2_id = m.addTrader("testGiveStock_TOKEN_2");
        m.giveStock(trader_2_id);
        assertEquals(trader_2_id, m.stockHolderId(), "Stock wasn't transferred following giveStock.");
        m.giveStock(trader_1_id);
        assertEquals(trader_1_id, m.stockHolderId(), "Stock wasn't transferred following giveStock.");
    }

    @Test
    void tradersCsv() {
        m.reset();
        int trader_1_id = m.addTrader("testTradersCsv_TOKEN_1");
        int trader_2_id = m.addTrader("testTradersCsv_TOKEN_2");
        String expected_csv = String.format("%d,%d", trader_1_id, trader_2_id);
        assertTrue(m.tradersCsv().equals(expected_csv), "tradersCsv() return doesn't match expected value.");
    }
}