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


using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;


namespace ServerNamespace {
    public class Market {
        private int stock_holder_id;
        public int StockHolderId { 
            get {
                return stock_holder_id;
            }

            private set {
                if (server != null)
                    if (server.has_form)
                        server.form.updateStockHolder(value);

                stock_holder_id = value;
            }
        }

        //private readonly Dictionary<int, Trader> traders = new Dictionary<int, Trader>();
        public int highest_id { get; private set; }
        public List<int> traders { get; private set; }

        // pre_reset_traders will have few seconds to reconnect
        // if they don't reconnect then appropriate message 
        // should be sent to all clients
        public List<int> pre_reset_traders;

        // identification of previously connected traders
        private Dictionary<string, int> token_id_dict;


        // https://stackoverflow.com/questions/10563148/where-is-the-correct-place-to-store-my-application-specific-data
        // program data directory saving doesn't work on lab PCs so app data is used instead
        public static string APP_DATA_PATH = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        public static string STATE_FILENAME = $"{APP_DATA_PATH}/CE303_server_state.txt";
        public static string STATE_BACKUP_FILENAME = $"{APP_DATA_PATH}/CE303_server_backup_state.txt";

        // save method was making serious issues upon server restart,
        // because multiple clients reconnected at the same time
        // which caused "save" method try to overwrite file at the same time
        // from several threads, which lead to the following error:
        //
        //     "process can't open file because it's opened by another process"
        //
        // so "lock_save" is there to prevent it as described in:
        // https://stackoverflow.com/questions/12269935/will-lock-prevent-the-error-the-process-cannot-access-the-file-because-it-is-be

        private static object lock_save = new object();

        public bool was_market_interrupted { get; private set; }
        public bool stock_owner_came_back_after_reset { get; private set; }

        private Server server;

        public Market(Server server_ = null) {
            server = server_;

            traders = new List<int>();
            pre_reset_traders = new List<int>();
            StockHolderId = -1;
            highest_id = 0;
            token_id_dict = new Dictionary<string, int>();

            load();

            stock_owner_came_back_after_reset = false;
            was_market_interrupted = StockHolderId != -1;
        }

        public void save() {
            // it creates a file in %appdata% with highest ID,
            // tokens of previously connected clients and their 
            // corresponding IDs, ID of trader who has the stock,
            // IDs of all connected traders
            // If the file was overwritten successfully it creates 
            // a backup copy of it. This solves the issues that could 
            // arise if power was turned off while overwriting
            // the file.

            lock (lock_save) {
                try {
                    using (StreamWriter sw = new StreamWriter(STATE_FILENAME, false)) {
                        //https://stackoverflow.com/a/3871782/4620679
                        string token_id_string = string.Join(";", token_id_dict.Select(x => x.Key + "," + x.Value).ToArray());
                        sw.WriteLine(token_id_string);
                        sw.WriteLine(highest_id);
                        sw.WriteLine(StockHolderId);
                        sw.WriteLine(tradersCsv());
                        sw.WriteLine("END");
                        sw.Flush();
                    }

                    try {
                        File.Copy(STATE_FILENAME, STATE_BACKUP_FILENAME, true);
                    } catch (Exception e) {
                        server.Log($"Couldn't make backup of market state file. Exception: {e.Message}", "Warning");
                    }
                } catch (Exception e) {
                    server.Log($"Couldn't save market state. Exception: {e.Message}", "Warning");
                }
            }
        }

        private bool isStateFileValid(string f_name) {
            // if the file was overwritten corretcly, it's going to have 5 lines
            return File.ReadLines(f_name).Count() == 5;
        }

        public void load() {
            // if the first filename is not a valid file (e.g. due to power off while saving) 
            string[] f_names = { STATE_FILENAME, STATE_BACKUP_FILENAME };
            int non_existing_files_count = 0;

            foreach (string f_name in f_names) {
                if (!File.Exists(f_name)) {
                    using (FileStream fs = File.Create(f_name)) { }
                    non_existing_files_count++;
                    continue;
                }

                if (isStateFileValid(f_name)) {
                    // Open the stream and read it back.
                    using (StreamReader sr = File.OpenText(f_name)) {
                        string s = "";

                        // trader tokens/ids
                        if ((s = sr.ReadLine()) != null) {
                            Console.WriteLine($"reading tokens = {s}");
                            foreach (string entry in s.Split(';')) {
                                string token = entry.Split(',')[0];
                                int id = int.Parse(entry.Split(',')[1]);
                                token_id_dict[token] = id;
                            }
                        }

                        // highest id
                        if ((s = sr.ReadLine()) != null) {
                            Console.WriteLine($"reading highest id = {s}");
                            highest_id = int.Parse(s.Trim());
                        }

                        // stock holder
                        if ((s = sr.ReadLine()) != null) {
                            Console.WriteLine($"reading StockHolderId = {s}");
                            StockHolderId = int.Parse(s.Trim());
                        }

                        // pre_reset_traders
                        if ((s = sr.ReadLine()) != null) {
                            Console.WriteLine($"reading pre_reset_traders = {s}");
                            if (s != "")
                                pre_reset_traders = s.Split(',').Select(x => Int32.Parse(x)).ToList();
                        }
                    }
                    return;
                }
            }
        }

        public static void deleteStateFiles() {
            // this method is used only for unit testing
            bool files_deleted = false;
            while (!files_deleted) {
                try {
                    File.Delete(STATE_FILENAME);
                    File.Delete(STATE_BACKUP_FILENAME);
                    files_deleted = true;
                } catch (Exception e) {
                    Console.WriteLine("Couldn't delete server state files. Trying again...");
                    Thread.Sleep(50);
                }
            }
        }


        public void reset() {
            // method useful for unit testing save and load methods
            // to ensure that previously saved data does not affect 
            // the results of the tests

            token_id_dict.Clear();
            highest_id = 0;
            StockHolderId = -1;
            traders.Clear();
            pre_reset_traders.Clear();
        }

        public bool hasTrader(int id) { return traders.Contains(id); }

        public int releaseStock() {
            if (traders.Count > 0)
                giveStock(traders[0]);
            else {
                StockHolderId = -1;
                save();
            }
            return stock_holder_id;
        }

        public int addTrader(string token) {

            int id = token_id_dict.ContainsKey(token) ? token_id_dict[token] : ++highest_id;
            traders.Add(id);

            if (stock_holder_id == -1)
                giveStock(id);

            token_id_dict[token] = id;
            
            if (was_market_interrupted)
                if (stock_holder_id == id)
                    stock_owner_came_back_after_reset = true;

            save();
            return id;
        }

        public void removeTrader(int trader_id) {
            if (trader_id == stock_holder_id) {
                if (traders.Count == 1)
                    StockHolderId = -1;
                else {
                    foreach (int id in traders) {
                        if (id != trader_id) {
                            giveStock(id);
                            break;
                        }
                    }
                }
            }
            traders.Remove(trader_id);
            save();
        }

        public bool giveStock(int trader_id) {
            if (!hasTrader(trader_id))
                return false;

            StockHolderId = trader_id;

            save();
            return true;
        }


        public string tradersCsv() {
            return String.Join<int>(",", traders);
        }
        public string preResetTradersCsv() {
            return String.Join<int>(",", pre_reset_traders);
        }

        public void printTraders() {
            StreamWriter console_writer = new StreamWriter(Console.OpenStandardOutput());
            printTraders(ref console_writer);
        }

        public void printTraders(ref StreamWriter writer) {
            if (traders.Count == 0) {
                writer.WriteLine("No traders left.");
            } else {
                writer.WriteLine("Connected traders:");
                foreach (int id in traders) {
                    if (id == stock_holder_id)
                        writer.WriteLine($"{id} (has stock)");
                    else
                        writer.WriteLine(id);
                }
            }
            writer.Flush();
        }
    }
}
