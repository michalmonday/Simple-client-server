package test;

import main.Client;
import main.JavaFix;
import org.junit.jupiter.api.AfterEach;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;

import static org.junit.jupiter.api.Assertions.*;

class ClientTest {

    Client client_1;
    Client client_2;

    @BeforeEach
    void setUp() {
        client_1 = new Client();
        client_2 = new Client();

        client_1.ensureServerRuns();
        client_1.connect();
        // delay before new connection to ensure that client_1 will be the stock holder
        try { Thread.sleep(500); } catch (InterruptedException e) { }

        client_2.connect();

        // allow client_1 to recognize client_2 connected
        try { Thread.sleep(500); } catch (InterruptedException e) { }

        // if server was interrupted, it may take 5 seconds for stock to be released to client_1
        while (!client_1.has_stock) {
            try { Thread.sleep(10); } catch (InterruptedException e) { }
        }
    }

    @AfterEach
    void tearDown() {
        try {
            client_1.close();
            client_2.close();
        } catch (Exception e) {
            e.printStackTrace();
        }
        client_1 = null;
        client_2 = null;
    }

    @Test
    void requestStockTransfer() throws InterruptedException {
        assertFalse(client_2.has_stock, "2nd client shouldn't have stock at this point.");

        client_1.requestStockTransfer(client_2.id);
        Thread.sleep(500);
        assertTrue(client_2.has_stock, "2nd client did not receive stock.");
        assertFalse(client_1.has_stock, "1st client shouldn't have stock at this point.");
    }

    @Test
    void sendLine() throws InterruptedException {
        assertFalse(client_2.has_stock, "2nd client shouldn't have stock at this point.");

        client_1.sendLine(String.format("givestock %d", client_2.id));
        Thread.sleep(500);
        assertTrue(client_2.has_stock, "2nd client did not receive stock.");
        assertFalse(client_1.has_stock, "1st client shouldn't have stock at this point.");
    }

    @Test
    void ensureServerRuns() throws InterruptedException {
        // most tests in this class use 2 clients that get connected before each function begins
        // so these are disconnected before server is killed
        try {
            client_1.close();
            client_2.close();
        } catch (Exception e) {
            e.printStackTrace();
        }

        JavaFix.killServer();
        Thread.sleep(2000);
        assertFalse(JavaFix.isServerRunning());
        Client client = new Client();
        client.ensureServerRuns();
        assertTrue(JavaFix.isServerRunning());
    }

    @Test
    void serverReset() throws InterruptedException {
        int client_2_id = client_2.id;
        int stock_holder_id = client_2.stock_holder;

        // set ID and stock holder of client_3 to fake values to ensure that server
        // restores proper ones after reset
        client_2.id = 0xAAAAA;
        client_2.stock_holder = 0xAAAAA;

        JavaFix.killServer();
        // allow some time to close
        Thread.sleep(1500);

        while (!client_2.is_connected) {
            Thread.sleep(10);
        }
        assertEquals(client_2_id, client_2.id, "Client ID didn't get set back to original value after server restart.");
        assertEquals(stock_holder_id, client_2.stock_holder, "Stock holder didn't get set back to original value after server restart.");

        try {
            client_2.close();
        } catch (Exception e) {
            e.printStackTrace();
        }
    }
}