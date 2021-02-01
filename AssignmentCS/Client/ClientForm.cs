/*
    CE303 Client-server system
    Student ID: 1904535

    This file contains the entry point of the program (Main function).
    It defines thread-safe functions used to interact with GUI.
*/
using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;


namespace ClientNamespace {
    public partial class ClientForm : Form {
        // This class contains functions helpful to keep Client class code clean
        // Additionally, separation of GUI form and Client into separate classes
        // helps with unit testing (where GUI is not really needed).


        Client client;

        [STAThread]
        static void Main() {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new ClientForm());
        }

        public ClientForm() { 
            InitializeComponent();
            this.Text = $"Client (id = ?)";
            listView_log.View = View.Details;
            listView_log.Columns.Add("Type", 50, HorizontalAlignment.Left);
            listView_log.Columns.Add("Time", 70, HorizontalAlignment.Left);
            listView_log.Columns.Add("Event", -2, HorizontalAlignment.Left);

            client = new Client();
            client.setForm(this);

            Thread.CurrentThread.Name = "ClientForm main thread";
        }

        private void button_give_stock_Click(object sender, EventArgs e) {
            if (listBox_traders.SelectedIndex == -1) {
                MessageBox.Show("Select receiver first");
                return;
            }

            int id_to_give = int.Parse(listBox_traders.SelectedItem.ToString());
            client.requestStockTransfer(id_to_give);
        }

        public void updateStockHolderGUI() {
            InvokeIfRequired(() => {
                if (client.has_stock) {
                    label_stock_holder.Text = $"Stock holder = ME";
                    BackColor = Color.FromName("LightBlue");
                    Activate();
                    TopMost = true;
                    button_give_stock.Enabled = true;
                } else {
                    label_stock_holder.Text = $"Stock holder = {client.stock_holder}";
                    BackColor = default(Color);
                    TopMost = false;
                    button_give_stock.Enabled = false;
                }
                listBox_traders.ClearSelected();
            });
        }

        public void addTrader(string name) {
            InvokeIfRequired(() => {
                listBox_traders.Items.Add(name);
                updateCount();
            });
        }

        public void removeTrader(string name) {
            InvokeIfRequired(() => {
                listBox_traders.Items.Remove(name);
                updateCount();
            });
        }

        public void setTraders(string[] names) {
            InvokeIfRequired(() => {
                listBox_traders.Items.Clear();
                listBox_traders.Items.AddRange(names);
                updateCount();
            });
        }

        public void setWindowTitle(string title) {
            InvokeIfRequired(() => this.Text = title);
        }

        public void Log(ListViewItem item) {
            InvokeIfRequired(() => {
                listView_log.Items.Add(item);
                for (int col = 0; col < listView_log.Columns.Count; col++)
                    listView_log.AutoResizeColumn(col, ColumnHeaderAutoResizeStyle.ColumnContent);
                item.EnsureVisible();
            });
        }

        private void updateCount() {
            label_traders.Text = $"Traders (count = {listBox_traders.Items.Count + 1})";
        }

        private void InvokeIfRequired(Action action) {
            // based on:
            //https://stackoverflow.com/questions/2367718/automating-the-invokerequired-code-pattern

            if (InvokeRequired)
                Invoke(action);
            else
                action();
        }
    }
}
