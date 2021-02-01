/*
    CE303 Client-server system
    Student ID: 1904535

    This file defines market operations such as:
        - adding and removing traders,
        - passing the stock
    It also has some helper functions for getting/printing
    all traders, or all traders that were connected when
    the server went down.

    Additionally, it handles saving and loading:
        - highest ID (server increases it by 1 for every previously unseen client)
        - tokens of previously connected clients and their corresponding IDs
        - ID of trader who has the stock (so it can be returned to him upon reconnection)
        - IDs of all connected traders (to recognize reconnection and lack of reconnections within 5 second timeframe)
*/

package main;

import java.io.*;
import java.nio.file.Files;
import java.nio.file.StandardCopyOption;
import java.util.*;

public class Market {
    public int stock_holder_id;
    public int highest_id;
    public List<Integer> traders;

    // pre_reset_traders will have few seconds to reconnect
    // if they don't reconnect then appropriate message
    // should be sent to all clients
    public List<Integer> pre_reset_traders;

    // identification of previously connected traders
    private Map<String, Integer> token_id_dict;

    // https://stackoverflow.com/questions/10563148/where-is-the-correct-place-to-store-my-application-specific-data
    // Program data doesn't work on lab PCs, so app data is used instead
    public static String APP_DATA_PATH = System.getenv("appdata");
    public static String STATE_FILENAME = String.format("%s%cCE303_server_state.txt", APP_DATA_PATH, File.separatorChar);
    public static String STATE_BACKUP_FILENAME = String.format("%s%cCE303_server_backup_state.txt", APP_DATA_PATH, File.separatorChar);

    private static Object lock_save = new Object();

    public boolean was_market_interrupted;
    public boolean stock_owner_came_back_after_reset;

    public Market() {
        traders = new ArrayList<>();
        pre_reset_traders = new ArrayList<>();
        stock_holder_id = -1;
        highest_id = 0;
        token_id_dict = new HashMap<String, Integer>();

        load();

        stock_owner_came_back_after_reset = false;
        was_market_interrupted = stock_holder_id != -1;

    }

    private String token_id_mapAsString() {
        String ret = "";
        for (String key : token_id_dict.keySet())
            ret += String.format("%s,%d;", key, token_id_dict.get(key));

        // remove last semicolon
        if (!ret.isEmpty())
            return ret.substring(0, ret.length() - 1);
        return ret;
    }

    public void save() {
        synchronized (lock_save) {
            try {
                File file = new File(STATE_FILENAME);
                file.createNewFile();
                FileWriter fw = new FileWriter(file);
                fw.write(token_id_mapAsString() + "\n");
                fw.write(highest_id + "\n");
                fw.write(stock_holder_id + "\n");
                fw.write(tradersCsv() + "\n");
                fw.write("END\n");
                fw.flush();
                fw.close();
                try {
                    File file_backup = new File(STATE_BACKUP_FILENAME);
                    file_backup.createNewFile();
                    Files.copy(file.toPath(), file_backup.toPath(), StandardCopyOption.REPLACE_EXISTING);
                } catch (Exception e) {
                    e.printStackTrace();
                }
            } catch (Exception e) {
                e.printStackTrace();
            }
        }
    }

    private boolean isStateFileValid(String f_name) {
        // Adapted from from https://stackoverflow.com/questions/1277880/how-can-i-get-the-count-of-line-in-a-file-in-an-efficient-way/1277904
        int lines = 0;
        try (BufferedReader reader = new BufferedReader(new FileReader(f_name))) {
            while (reader.readLine() != null)
                lines++;
        } catch (IOException e) {
            e.printStackTrace();
        }
        return lines == 5;
    }

    public void load() {
        String [] f_names = {STATE_FILENAME, STATE_BACKUP_FILENAME};

        for (String f_name : f_names) {
            if (!isStateFileValid(f_name))
                continue;

            try (BufferedReader reader = new BufferedReader(new FileReader(f_name))) {
                String s = "";

                // trader tokens/ids
                if ((s = reader.readLine()) != null) {

                    for (String entry : s.split(";")) {
                        String token = entry.split(",")[0];
                        int id = Integer.parseInt(entry.split(",")[1]);
                        token_id_dict.put(token, id);
                    }
                }

                // highest id
                if ((s = reader.readLine()) != null) {
                    highest_id = Integer.parseInt(s.trim());
                }

                // stock holder
                if ((s = reader.readLine()) != null) {
                    stock_holder_id = Integer.parseInt(s.trim());
                }

                // pre_reset_traders
                if ((s = reader.readLine()) != null) {
                    if (!s.isEmpty()) {
                        pre_reset_traders.clear();
                        for (String id : s.split(","))
                            pre_reset_traders.add(Integer.parseInt(id));
                    }
                }
            } catch (IOException e) {
                e.printStackTrace();
            }
        }
    }

    public void reset() {
        // method useful for unit testing save and load methods
        // to ensure that previously saved data does not affect
        // the results of the tests

        token_id_dict.clear();
        highest_id = 0;
        stock_holder_id = -1;
        traders.clear();
        pre_reset_traders.clear();
    }

    public int stockHolderId() { return stock_holder_id; }

    public boolean hasTrader(int id) { return traders.contains(id); }

    public int releaseStock() {
        if (traders.size() > 0)
            giveStock(traders.get(0));
        else {
            stock_holder_id = -1;
            save();
        }
        return stock_holder_id;
    }

    public int addTrader(String token) {

        int id = token_id_dict.containsKey(token) ? token_id_dict.get(token) : ++highest_id;
        traders.add(id);

        if (stock_holder_id == -1)
            giveStock(id);

        token_id_dict.put(token, id);

        if (was_market_interrupted)
            if (stock_holder_id == id)
                stock_owner_came_back_after_reset = true;

        save();
        return id;
    }

    public void removeTrader(int trader_id) {
        if (trader_id == stock_holder_id) {
            if (traders.size() == 1)
                stock_holder_id = -1;
            else {
                for (int id : traders) {
                    if (id != trader_id) {
                        giveStock(id);
                        break;
                    }
                }
            }
        }
        traders.remove(Integer.valueOf(trader_id));
        save();
    }

    public boolean giveStock(int trader_id) {
        if (!hasTrader(trader_id))
            return false;

        stock_holder_id = trader_id;
        save();
        return true;
    }


    public String tradersCsv() {
        String csv = "";
        for (int i = 0; i < traders.size() ; i++)
            csv += Integer.toString(traders.get(i)) + (i == traders.size()-1 ? "" : ",");
        return csv;
    }

    public String preResetTradersCsv() {
        String csv = "";
        for (int i = 0; i < pre_reset_traders.size() ; i++)
            csv += Integer.toString(pre_reset_traders.get(i)) + (i == pre_reset_traders.size()-1 ? "" : ",");
        return csv;
    }
}

