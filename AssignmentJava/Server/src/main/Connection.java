/*
    CE303 Client-server system
    Student ID: 1904535

    This file defines how the server handles each connection.
    For each connection 2 threads are created, 1 for reading
    and 1 for writing.
 */

package main;

import java.io.IOException;
import java.io.PrintWriter;
import java.net.Socket;
import java.util.*;


public class Connection implements AutoCloseable {
    public static Market market;

    // this will allow to send messages to all clients for immediate updates
    public static List<Connection> connections = new ArrayList<>();
    private Socket socket;
    private PrintWriter writer;
    private Scanner reader;
    private Queue<String> messages_to_write;

    // reason behind having separate thread for reading and writing:
    //      I'd like to be able to send any message to any client at any moment.
    //      If reading and writing was done in the same thread then reading could block
    //      writing.
    private Thread thread_write;
    private Thread thread_receive;

    private int trader_id;
    private boolean was_disposed;
    private Timer keepalive_timer;
    private Server server;
    private Boolean dispose_requested;

    public Connection(Socket socket_, Server server_) throws IOException {
        server = server_;
        dispose_requested = false;
        was_disposed = false;
        messages_to_write = new LinkedList<>();
        trader_id = -1;
        socket = socket_;
        writer = new PrintWriter(socket.getOutputStream(), true);
        reader = new Scanner(socket.getInputStream());
        thread_receive = new Thread(new Receiver());
        thread_receive.setDaemon(true);
        //thread_receive.Name = $"Thread - Connection receiver (thread id={thread_receive.ManagedThreadId})";
        thread_receive.start();
        thread_write = new Thread(new Writer());
        thread_write.setDaemon(true);
        //thread_write.Name = $"Thread - Connection writer (thread id={thread_receive.ManagedThreadId})";
        thread_write.start();
    }

    public void addLineToSend(String line) {
        synchronized (messages_to_write) {
            messages_to_write.add(line);
        }
    }

    public static void sendLineToAll(String line) {
        synchronized (connections) {
            for (Connection conn : connections)
                conn.addLineToSend(line);
        }
    }

    public static void sendLineToAllExcept(String line, int trader_id_excluded) {
        synchronized (connections) {
            for (Connection conn : connections)
            if (conn.trader_id != trader_id_excluded)
                conn.addLineToSend(line);
        }
    }

    class Writer implements Runnable {
        // runs as a thread

        @Override
        public void run() {
            keepalive_timer = new Timer();
            keepalive_timer.scheduleAtFixedRate(new TimerTask() {
                public void run() { if (trader_id != -1) addLineToSend("."); }
            }, 1000, 1000);

            while (true) {
                try {
                    synchronized (messages_to_write) {
                        while (!messages_to_write.isEmpty()) {
                            String msg = messages_to_write.remove();
                            if (!msg.equals("."))
                                System.out.println("Sending to trader " + trader_id + ": '" + msg + "'");
                            writer.println(msg);
                        }
                    }
                } catch (Exception e) {
                    System.out.println("Connection.Writer exception: " + e.getMessage());
                    try { close(); } catch (Exception ex) { }
                    return;
                }

                try { Thread.sleep(50); } catch (InterruptedException e) { }
                synchronized (dispose_requested) {
                    if (dispose_requested) {
                        try { close(); } catch (Exception ex) { }
                        return;
                    }
                }
            }
        }
    }

    class Receiver implements  Runnable{
        // runs as a thread

        @Override
        public void run() {
            try {
                // reading token, assigning ID
                openingSequence();

                while (true) {
                    String line;
                    synchronized (reader) {
                        // "No line found" exception is thrown, I guess it comes from here
                        // so "line == null" below is probably never true
                        line = reader.nextLine();
                    }

                    // Marquis of Lorne (stackoverflow user) about detecting disconnection:
                    //      "In TCP there is only one way to detect an orderly disconnect,
                    //      and that is by getting zero as a return value from read()/recv()/recvXXX()
                    //      when reading.
                    //      There is also only one reliable way to detect a broken connection: by writing to it."
                    //      https://stackoverflow.com/a/17665015/4620679
                    if (line == null)
                        throw new Exception("Disconnected.");

                    String[] parts = line.split("\\s");
                    String command = parts[0].toLowerCase();
                    switch (command) {
                        /*case "id":         addLineToSend($"id {trader_id}");  break;*/
                        //case "ping":       addLineToSend("pong");             break;
                        // do nothing, it's just to recognize server going down
                        case ".":
                            break;
                        case "givestock":
                            int id_to_give;
                            try {
                                id_to_give = Integer.parseInt(parts[1]);
                            } catch (Exception e) {
                                addLineToSend("givestock fail Wrong format used for 'givestock id'");
                                break;
                            }
                            handleGiveStock(id_to_give);
                            break;
                        default:
                            throw new Exception("Unknown command: " + parts[0]);
                    }
                }
            } catch (Exception e) {
                System.out.println("Connection.Reader exception: " + e.getMessage());
            } finally {
                synchronized (dispose_requested) {
                    dispose_requested = true;
                }
            }
        }// run
    } // void Receiver

    private void openingSequence() {
        // communication that happens when new client connects, called by Receiver thread
        // client sends token, server checks if token was connected in the past and:
        //      - if token was found in "state.txt", it returns previously used ID (associated with token)
        //      - if token was not found in "state.txt", it creates new ID (which will be associated with the token in future)

        String token;
        synchronized (reader) {
            token = reader.nextLine();
        }

        System.out.println("Adding trader with token = '" + token + "'");

        // this will be helpful for using "Threads" window (images for the report)
        // this messes up program
        //thread_receive.Name = $"Thread - Connection receiver (trader id={trader_id}, thread id={thread_receive.ManagedThreadId}, {new Random().Next(15000)}), ";
        //thread_write.Name = $"Thread - Connection writer (id={trader_id}, thread id={thread_write.ManagedThreadId}, {new Random().Next(15000)})";

        String msg;
        synchronized (market) {
            trader_id = market.addTrader(token);

            msg = String.format("%d;%d;%s", trader_id, market.stockHolderId(), market.tradersCsv());
            addLineToSend(msg);

            if (trader_id == market.stockHolderId())
                if (server.has_form)
                    server.form.updateStockHolder(trader_id);

            if (market.pre_reset_traders.contains(trader_id)) {
                market.pre_reset_traders.remove(Integer.valueOf(trader_id));

                // reconnected is appended so client knows to not print any message
                sendLineToAllExcept(String.format("connected %d reconnected", trader_id), trader_id);
                server.Log(String.format("Trader %d reconnected.", trader_id));
            } else {
                sendLineToAllExcept(String.format("connected %d", trader_id), trader_id);
                server.Log(String.format("New connection; trader ID %d", trader_id));
            }

            synchronized (connections) {
                if (!connections.contains(this))
                    connections.add(this);
            }
        }

        if (server.has_form)
            server.form.addTrader(Integer.toString(trader_id));
    }

    private void handleGiveStock(int id_to_give) {
        // called by Receiver thread

        if (id_to_give == trader_id) {
            server.Log(String.format("Trader %d gave himself the stock. Gross domestic product increased.", trader_id));
            sendLineToAll(String.format("givestock success %d %d", trader_id, trader_id));
            return;
        }

        synchronized (market) {
            if (!market.hasTrader(id_to_give)) {
                server.Log(String.format("Trader %d attempted to give the stock to a not existing trader %d.", trader_id, id_to_give));
                addLineToSend(String.format("givestock fail Supplied trader id (%d) does not exist.", id_to_give));
            } else if (market.stockHolderId() != trader_id) {
                server.Log(String.format("Trader %d attempted to give the stock without having it in the first place.", trader_id));
                addLineToSend("givestock fail You are not in possession of a stock.");
            } else {
                market.giveStock(id_to_give);
                server.Log(String.format("Trader %d gave trader %d the stock.", trader_id, id_to_give));
                sendLineToAll(String.format("givestock success %d %d", trader_id, id_to_give));
                if (server.has_form)
                    server.form.updateStockHolder(id_to_give);
            }
        }
    }

    @Override
    public void close() throws Exception {

        synchronized (market) {

            if (was_disposed)
                return;
            keepalive_timer.cancel();
            was_disposed = true;
            server.Log(String.format("Trader %d disconnected.", trader_id));
            int stock_holder_id = market.stockHolderId();
            market.removeTrader(trader_id);

            if (stock_holder_id == trader_id) {
                int new_holder_id = market.stockHolderId();

                if (new_holder_id == -1) {
                    server.Log(String.format("Trader %d was stock holder, he was the only trader, so the stock will be given to the first new trader.", trader_id));
                } else {
                    server.Log(String.format("Trader %d was stock holder, the stock was given to trader %d.", trader_id, new_holder_id));
                    sendLineToAllExcept(String.format("givestock left %d %d", trader_id, new_holder_id), trader_id);
                }
                if (server.has_form)
                    server.form.updateStockHolder(new_holder_id);
            }


            if (Thread.currentThread() == thread_receive)
                thread_write.interrupt();
            else if (Thread.currentThread() == thread_write)
                thread_receive.interrupt();

            synchronized (reader) {
                reader.close();
            }
            writer.close();
            socket.close();

            synchronized (connections) {
                connections.remove(this);
            }

            sendLineToAllExcept(String.format("disconnected %d", trader_id), trader_id);
        }

        if (server.has_form)
            server.form.removeTrader(Integer.toString(trader_id));
    }
}

