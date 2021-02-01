/*
    CE303 Client-server system
    Student ID: 1904535

    This file contains the entry point of the program (Main function).
    It defines thread-safe functions used to interact with GUI.
    At the begining of Main function it checks if 1 instance of server
    is already running (either C# or Java based) and terminates
    if that is the case (clients handle it using Mutex anyway but
    this mechanism additionally protects from manually running the server
    multiple times).
*/

package main;

import javax.swing.*;
import java.awt.*;

import static javax.swing.SwingUtilities.invokeLater;

public class ServerForm {
    private JPanel panel;

    private JList<String> list_traders;
    private DefaultListModel<String> list_traders_model;

    private JList<String> list_events;
    private DefaultListModel<String> list_events_model;

    private JLabel label_events;
    private JLabel label_traders;
    private JFrame frame;

    private int stock_holder_id;
    private final Object stock_holder_id_lock = new Object();

    public static void main(String[] args) {
        ServerForm server_form = new ServerForm();
    }

    private Server server;

    public ServerForm() {
        // Avoid running 2 servers at once.
        // Clients prevent it anyway using mutex but that doesn't cover
        // scenario where server is manually started multiple times.
        if (JavaFix.isServerAlreadyRunning()) {
            System.out.println("One server is already running so this one will terminate.");
            System.exit(1);
        }

        frame = new JFrame("Server");
        frame.setContentPane(panel);
        frame.setDefaultCloseOperation(JFrame.EXIT_ON_CLOSE);
        frame.pack();
        frame.setVisible(true);

        list_traders_model = new DefaultListModel<>();
        list_traders.setModel(list_traders_model);
        list_traders.setSelectionModel(new JavaFix.NoSelectionModel());

        // it will colour stock holder JList (list_traders) entry with blue
        list_traders.setCellRenderer(new StockHolderPainter());

        list_events_model = new DefaultListModel<>();
        list_events.setModel(list_events_model);

        server = new Server();
        server.setForm(this);
        server.init();
    }

    public void addTrader(String name) {
        invokeLater(() -> {
            list_traders_model.addElement(name);
            updateCount();
        });
    }

    public void removeTrader(String name) {
        invokeLater(() -> {
            list_traders_model.removeElement(name);
            updateCount();
        });
    }

    public void Log(String line) {
        invokeLater(() -> {
            list_events_model.addElement(line);
            list_events.ensureIndexIsVisible(list_events_model.size()-1);
        });
    }

    public void updateStockHolder(int id) {
        invokeLater(() -> {
            synchronized (stock_holder_id_lock) {
                stock_holder_id = id;
            }
            list_traders.updateUI();
        });
    }

    class StockHolderPainter extends DefaultListCellRenderer {
        @Override
        public Component getListCellRendererComponent(JList list, Object value, int index, boolean isSelected, boolean cellHasFocus) {
            Component c = super.getListCellRendererComponent(list, value, index, isSelected, cellHasFocus);
            synchronized (stock_holder_id_lock) {
                Color clr = Integer.parseInt((String) value) == stock_holder_id ? new Color(173,216,230) : Color.WHITE;
                setBackground(clr);
            }
            return c;
        }
    }

    private void updateCount() {
        invokeLater(() -> {
            label_traders.setText("Traders (count = " + Integer.toString(list_traders_model.size()) + ")");
        });
    }

    /*private void InvokeIfRequired(Runnable runnable) {
        if (isEventDispatchThread())
            runnable.run();
        else
            invokeLater(runnable);
    }*/
}
