/*
    CE303 Client-server system
    Student ID: 1904535

    This file contains the entry point of the program (Main function).
    It defines thread-safe functions used to interact with GUI.
*/

package main;

import javax.swing.*;
import java.awt.*;
import java.awt.event.ActionEvent;
import java.awt.event.ActionListener;
import java.util.List;

import static javax.swing.SwingUtilities.invokeLater;

public class ClientForm {
    private JLabel label_traders;
    private JPanel panel;

    private JList<String> list_traders;
    private DefaultListModel<String> list_traders_model;

    private JList<String> list_events;
    private DefaultListModel<String> list_events_model;

    private JButton button_give_stock;
    private JLabel label_events;
    private JLabel label_stock_holder;

    // Why client could be a part of the ClientForm?
    // Because that form is always going to have a Client,
    // and client does not necessarily going to have a form.
    // Idk if that's good design practice, maybe the right thing
    // would be to ask someone about it...
    private Client client;


    private JFrame frame;

    public static void main(String[] args) throws Exception {
        ClientForm client_frame = new ClientForm();
    }

    public ClientForm() throws Exception {
        frame = new JFrame("Client (id = ?)");
        frame.setContentPane(panel);
        frame.setDefaultCloseOperation(JFrame.EXIT_ON_CLOSE);
        frame.pack();
        frame.setVisible(true);

        list_traders_model = new DefaultListModel<>();
        list_traders.setModel(list_traders_model);

        list_events_model = new DefaultListModel<>();
        list_events.setModel(list_events_model);
        button_give_stock.addActionListener(new ActionListener() {
            @Override
            public void actionPerformed(ActionEvent e) {
                if (list_traders.isSelectionEmpty()) {
                    JavaFix.MessageBox_show("Select receiver first");
                    return;
                }

                int id_to_give = Integer.parseInt(list_traders.getSelectedValue());
                client.requestStockTransfer(id_to_give);
            }
        });

        client = new Client();
        client.setForm(this);
    }

    public void updateStockHolderGUI() {
        invokeLater(() -> {
            Color clr = new Color(173,216,230);
            String stock_holder_label = "Stock holder = ME";;
            if (!client.has_stock) {
                stock_holder_label = String.format("Stock holder = %d", client.stock_holder);
                clr = new Color(240,240,240);
            } else {
                frame.toFront();
                frame.requestFocus();
            }
            label_stock_holder.setText(stock_holder_label);
            frame.getContentPane().setBackground(clr);
            frame.setAlwaysOnTop(client.has_stock);
            button_give_stock.setEnabled(client.has_stock);
            list_traders.clearSelection();
        });
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

    public void setTraders(List<String> names) {
        invokeLater(() -> {
            list_traders_model.clear();
            for (String name : names)
                list_traders_model.addElement(name);
            updateCount();
        });
    }

    public void setWindowTitle(String title) {
        invokeLater(() -> {
            frame.setTitle(title);
        });
    }

    public void Log(String line) {
        invokeLater(() -> {
            list_events_model.addElement(line);
            //for (int col = 0; col < list_events_model.Columns.Count; col++)
            //    listView_log.AutoResizeColumn(col, ColumnHeaderAutoResizeStyle.ColumnContent);
            list_events.ensureIndexIsVisible(list_events_model.size()-1);
        });
    }

    private void updateCount() {
        invokeLater(() -> {
            label_traders.setText("Traders (count = " + Integer.toString(list_traders_model.size() + 1) + ")");
        });
    }

    //https://stackoverflow.com/questions/7229284/refreshing-gui-by-another-thread-in-java-swing
    //https://stackoverflow.com/questions/2186931/java-pass-method-as-parameter/25005082

    /*private void InvokeIfRequired(Runnable runnable) {
        if (SwingUtilities.isEventDispatchThread())
            runnable.run();
        else
            invokeLater(runnable);
    }*/

}
