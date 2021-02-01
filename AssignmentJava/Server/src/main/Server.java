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

package main;

import java.io.IOException;
import java.net.ServerSocket;
import java.net.Socket;
import java.time.LocalDateTime;
import java.time.format.DateTimeFormatter;
import java.util.Timer;
import java.util.TimerTask;

public class Server implements AutoCloseable {

    // equivalent to TcpListener in C#
    public ServerSocket server_socket;

    private final int port = 8888;
    public Market market;
    public ServerForm form;
    public boolean has_form;

    public Server() {
        has_form = false;
    }

    public void init()
    {
        market = new Market();
        Connection.market = market;

        if (market.was_market_interrupted) {
            Log("The server was closed or killed while having connected clients, data will be restored.");
            Log("If the stock holder (" + Integer.toString(market.stock_holder_id) + ") does not return within 5 seconds, the stock will be released.");
            scheduleStockReleaseIfOwnerAbandoned(5000);
            scheduleDidntReconnectMessages(5000);
        }

        Thread t = new Thread(new RunServer(this));
        // if has form then thread cannot exist without it
        // if does not have form, then thread can exist on it's own
        // that's what "setDeamon(has_form)" does
        t.setDaemon(has_form);
        t.setName("Thread - RunServer");
        t.start();
    }

    public void setForm(ServerForm form_) {
        form = form_;
        has_form = true;
    }

    class RunServer implements Runnable {
        private Server server;

        public RunServer(Server s) {
            server = s;
        }

        @Override
        public void run() {
            server_socket = null;
            while (server_socket == null) {
                try {
                    server_socket = new ServerSocket(port);
                } catch (IOException e) {
                    System.out.println("WARNING: Couldn't create ServerSocket");
                    try { Thread.sleep(1000); } catch (InterruptedException ex) {}
                    server_socket = null;
                }
            }

            Log("Waiting for incoming connections...");

            while (true) {
                try {
                    Socket socket = server_socket.accept();

                    // Connection could be ran as a separate thread to improve speed,
                    // but it spawns 2 threads on it's own anyway, aside of that it just
                    // initializes few variables and exits, so it doesn't take much more time than needed.
                    new Connection(socket, server);

                } catch (IOException e) {
                    System.out.println("WARNING: Accepting connection failed.");
                    try { Thread.sleep(1000); } catch (InterruptedException ex) {}
                }
            }
        }
    }

    private void scheduleStockReleaseIfOwnerAbandoned(int delay) {
        TimerTask release_stock = new TimerTask() {
            public void run() {
            boolean came_back;
            synchronized (market) {
                came_back = market.stock_owner_came_back_after_reset;
                if (!came_back) {
                    int abandoned_trader, new_owner;
                    synchronized (market) {
                        abandoned_trader = market.stock_holder_id;
                        new_owner = market.releaseStock();
                    }
                    if (new_owner != -1) {
                        Connection.sendLineToAll(String.format("givestock didnt_reconnect %d %d", abandoned_trader, new_owner));
                        Log(String.format("The stock owner didn't connect back within %dms, stock was released from client %d and given to client %d", delay, abandoned_trader, new_owner));
                    } else {
                        Log(String.format("The stock owner didn't connect back within %dms, stock was released from client %d it will be given to the first new client.", delay, abandoned_trader));
                    }

                    if (has_form)
                        form.updateStockHolder(new_owner);
                }
            }
            }
        };
        Timer timer = new Timer();
        timer.schedule(release_stock, delay);
    }

    private void scheduleDidntReconnectMessages(int delay) {
        TimerTask send_disconnect_messages = new TimerTask() {
            public void run() {
            synchronized (market) {
                if (!market.pre_reset_traders.isEmpty())
                    Connection.sendLineToAll("didnt_reconnect " + market.preResetTradersCsv());
            }
            }
        };
        Timer timer = new Timer();
        timer.schedule(send_disconnect_messages, delay);
    }

    public void Log(String msg) {
        DateTimeFormatter dtf = DateTimeFormatter.ofPattern("HH:mm:ss");
        LocalDateTime now = LocalDateTime.now();
        msg = String.format("%-10s%s", dtf.format(now), msg);
        if (has_form)
            form.Log(msg);
        System.out.println(msg);
    }

    @Override
    public void close() throws IOException {
        synchronized (server_socket) {
            server_socket.close();
        }
    }
}
