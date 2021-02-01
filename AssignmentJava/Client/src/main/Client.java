/*
    CE303 Client-server system
    Student ID: 1904535

    This file defines communication logic used by the client
    to interact with the server. Additionally, it provides
    mechanism for restarting the server when it goes down.
    It uses a named mutex to recognize that 1 client running
    on the same system is already restarting the server.
    That is to prevent multiple servers being started at once.
*/

package main;

import java.io.*;
import java.net.Socket;
import java.net.URISyntaxException;
import java.net.UnknownHostException;
import java.time.LocalDateTime;
import java.time.format.DateTimeFormatter;
import java.util.*;

import java.net.URI;

import static java.util.Collections.sort;

public class Client implements AutoCloseable {
    final int port = 8888;

    private String token;
    private boolean connection_was_established;
    private Thread thread_receiver;

    public boolean is_connected;
    public List<String> traders;
    public int stock_holder;
    public boolean has_stock;

    private boolean has_form;
    private ClientForm form;

    // "public" is there for automated testing
    // where invalid value is set to ensure that original
    // data is preserved and provided by the server after
    // restart
    public int id;

    private Socket socket;
    private Scanner reader;
    private PrintWriter writer;

    // in c# timers were single threaded, in here, timer runs
    // as a separate thread, so whatever happens inside
    // must be synchronized
    private Timer keepalive_timer;

    private Object lock_is_connected;


    public Client(){
        has_form = false;
        is_connected = false;
        lock_is_connected = new Object();

        traders = new ArrayList<>();

        // it will help to identify client after server restart
        // (to restore his old ID and possibly give him the stock)
        token = generateUniqueToken();
        connection_was_established = false;

        keepalive_timer = new Timer();
        keepalive_timer.scheduleAtFixedRate(new TimerTask() {
            public void run() {
                boolean is_connected_local;
                synchronized (lock_is_connected) { is_connected_local = is_connected; }
                if (is_connected_local)
                        sendLine(".");
            }
        }, 1000, 1000);
    }

    public void setForm(ClientForm client_form) {
        form = client_form;
        has_form = true;
        connect();
    }

    public class Receiver implements Runnable {
        @Override
        public void run() {
            while (true) {
                try {
                    String line;
                    synchronized (reader) {
                        line = reader.nextLine();
                    }

                    String[] parts = line.split("\\s");

                    if (!line.equals(".")) {
                        if (parts.length < 2) {
                            Log("Every message received from server should have at least 1 parameter, this one didn't: '" + line + "'");
                            continue;
                        } else {
                            System.out.println("ClientForm.Receiver line = '" + line + "'");
                        }
                    }

                    String command = parts[0].toLowerCase();
                    switch (command) {
                        case "connected":
                            String id_joined = parts[1];
                            if (Integer.parseInt(id_joined) != id && !traders.contains(id_joined)) {
                                if (has_form)
                                    form.addTrader(id_joined);
                                traders.add(id_joined);

                                if (parts.length == 3 && parts[2].equals("reconnected")) {
                                    // don't log anything
                                } else {
                                    Log("New trader (" + id_joined + ") joined.");
                                }
                            }
                            break;
                        case "disconnected":
                            String id_left = parts[1];
                            if (has_form)
                                form.removeTrader(id_left);
                            traders.remove(Integer.valueOf(id_left));
                            Log("Trader " + id_left + " left.");
                            break;
                        case "didnt_reconnect":
                            if (parts.length > 1) {
                                String[] ids = parts[1].split(",");
                                if (ids.length == 1)
                                    Log("Trader "+ ids[0] + " did not reconnect after server restart (it is assumed he left).");
                                else if (ids.length > 1) {
                                    Log(String.format("Some traders (IDs: %s) didn't reconnect after server restart (it is assumed they left).", parts[1]));
                                }
                            }
                            break;
                        case "givestock":
                            String result = parts[1]; // could be: success, fail, left, didnt_reconnect
                            if (result.equals("success") || result.equals("left") || result.equals("didnt_reconnect")) {
                                String id_from = parts[2];
                                String id_to = parts[3];
                                has_stock = Integer.parseInt(id_to) == id;
                                stock_holder = Integer.parseInt(id_to);
                                if (has_form)
                                    form.updateStockHolderGUI();
                                String receiver_str = has_stock ? "I" : "Trader " + id_to;
                                String giver_str = Integer.parseInt(id_from) == id ? "me" : "trader " + id_from;
                                String msg = receiver_str + " received stock from " + giver_str;
                                if (result.equals("left")) {
                                    msg += " who left the server while holding stock.";
                                } else if (result.equals("didnt_reconnect")) {
                                    msg += " who didn't reconnect following server restart.";
                                } else {
                                    msg += ".";
                                }

                                Log(msg);
                            } else if (result.equals("fail")) {
                                String reason = line.replaceFirst("givestock fail ", "");
                                Log("Giving stock failed, reason: " + reason);
                            }
                            break;
                        case ".": break; // do nothing, it's just to check that client didn't go down
                        default:
                            Log("Unrecognized command received from server.");
                            Log("Unrecognized command: '" + line + "'.");
                            break;
                    }
                }
                catch (Exception e2) {
                    // let the "keepalive" timer handle reconnection and just wait
                    try {
                        Thread.sleep(100);
                    } catch (InterruptedException e) {
                        return;
                    }
                }
            } // while true
        }
    }

    public void recover() {
        synchronized (lock_is_connected) {
            is_connected = false;
        }
        ensureServerRuns();
        connect();
    }

    public void connect() {
        try {
            if (connection_was_established) {
                writer.close();
                synchronized (reader) {
                    reader.close();
                }
                thread_receiver.interrupt();
                socket.close();
            }

            socket = new Socket("localhost", port);
            //socket.setSoTimeout(1500);

            if (reader == null)
                reader = new Scanner(socket.getInputStream());
            else {
                synchronized (reader) {
                    reader = new Scanner(socket.getInputStream());
                }
            }

            writer = new PrintWriter(socket.getOutputStream(), true);

            openingSequence();

            thread_receiver = new Thread(new Receiver());
            thread_receiver.setDaemon(true);
            thread_receiver.start();

            connection_was_established = true;
            synchronized (lock_is_connected) {
                is_connected = true;
            }
        } catch (UnknownHostException e) {
            //e.printStackTrace();
            recover();
        } catch (IOException e) {
            //e.printStackTrace();
            recover();
        }
    }


    private void openingSequence() {
        String[] parts;
        synchronized (reader) {
            sendLine(token);
            parts = reader.nextLine().split(";");
        }
        //Log($"Connected using token: {token}");
        Log(connection_was_established ? "Reconnected." : "Connected");

        // parsing values
        id = Integer.parseInt(parts[0]);
        stock_holder = Integer.parseInt(parts[1]);

        String[] trader_ids = parts[2].split(",");

        // using id
        if (has_form)
            form.setWindowTitle(String.format("Client (id = %d)", id));

        // using stock holder
        has_stock = stock_holder == id;
        if (has_form)
            form.updateStockHolderGUI();

        // using trader ids
        traders.clear();
        traders.addAll(Arrays.asList(trader_ids));
        traders.removeIf(x -> Integer.parseInt(x) == id);
        sort(traders);

        if (has_form)
            form.setTraders(traders);
    }

    public void requestStockTransfer(int id_to_give) {
        sendLine("givestock " + Integer.toString(id_to_give));
    }

    public void sendLine(String line) {
        if (line.equals("."))
            System.out.print(line);
        else
            System.out.println("Sending: " + line);

        try {
            synchronized (writer) {
                writer.println(line);
                if (writer.checkError())
                    throw new Exception("\nWriting error occured.");
            }
        } catch (Exception e) {
            //Log("ClientForm.sendLine EXCEPTION: " + e.getMessage());
            recover();
        }
    }

    @Override
    public void close() throws Exception {
        keepalive_timer.cancel();
        keepalive_timer.purge();
        synchronized (reader) {
            reader.close();
        }
        writer.close();
        thread_receiver.interrupt();
        socket.close();

        System.out.println("Client.close was called");
    }

    public void ensureServerRuns() {
        int mutex_handle = JavaFix.CreateMutex("CE303_server_restart");
        if (mutex_handle != 0) {
            if (JavaFix.waitOne(mutex_handle, 0) == 0) {
                Log("Server is not running. Starting it...");
                try {
                    String path = new URI(Client.class.getProtectionDomain().getCodeSource().getLocation().toString()).getPath().replace(
                            "Client/out/production/Client",
                            "Server/out/production/Server");
                    String command = "java -classpath \"" + path + "\" main.ServerForm";
                    Runtime.getRuntime().exec(command);

                    // allow some time for other clients to realize server is not running
                    // so they have time to fail acquiring mutex
                    try { Thread.sleep(1000); } catch (InterruptedException e) { JavaFix.ReleaseMutex(mutex_handle); return; }
                    waitTillServerRestarts();
                } catch (IOException e) {
                    Log("Restarting server failed, reason: " + e.getMessage());
                } catch (URISyntaxException uri_e) {
                    Log("Restarting server failed, reason: " + uri_e.getMessage());
                }
                JavaFix.ReleaseMutex(mutex_handle);
                return;
            }
        }

        Log("Server is not running. One client is already restarting the server.");
        waitTillServerRestarts();
    }

    private void waitTillServerRestarts() {
        while (!JavaFix.isServerRunning())
            try { Thread.sleep(50); } catch (InterruptedException e) { return; }
    }

    private String generateUniqueToken() {
        return Base64.getEncoder().encodeToString(UUID.randomUUID().toString().getBytes());
    }

    public void Log(String msg) {
        DateTimeFormatter dtf = DateTimeFormatter.ofPattern("HH:mm:ss");
        LocalDateTime now = LocalDateTime.now();
        msg = String.format("%-10s%s", dtf.format(now), msg);
        if (has_form)
            form.Log(msg);
        System.out.println(msg);
    }
}
