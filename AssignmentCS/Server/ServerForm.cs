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

using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace ServerNamespace {
    public partial class ServerForm : Form
    {

        public Server server;

        private object stock_holder_id_lock;
        private int stock_holder_id;

        [STAThread]
        static void Main() {
            // Avoid running 2 servers at once.
            // Clients prevent it anyway using mutex but that doesn't cover 
            // scenario where server is manually started multiple times.
            if (Server.isServerAlreadyRunning())
                return;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            ServerForm server_form = new ServerForm();
            Application.Run(server_form);
        }

        public ServerForm() {
            stock_holder_id_lock = new object();

            InitializeComponent();
            listView_log.View = View.Details;
            listView_log.Columns.Add("Type", 50, HorizontalAlignment.Left);
            listView_log.Columns.Add("Time", 70, HorizontalAlignment.Left);
            listView_log.Columns.Add("Event", -2, HorizontalAlignment.Left);

            server = new Server(this);

            Thread.CurrentThread.Name = "ServerForm main thread";

            
            listBox_traders.DrawItem += new DrawItemEventHandler(listBox_traders_DrawItem);
        }

        public void addTrader(int id) {
            InvokeIfRequired(() => {
                listBox_traders.Items.Add(id);
                updateCount();
            });
        }

        public void removeTrader(int id) {
            InvokeIfRequired(() => {
                listBox_traders.Items.Remove(id);
                updateCount();
            });
        }

        public void Log(ListViewItem item) {
            InvokeIfRequired(() => {
                listView_log.Items.Add(item);
                item.EnsureVisible();
            });
        }

        public void updateStockHolder(int id) {
            InvokeIfRequired(() => {
                lock (stock_holder_id_lock)
                    stock_holder_id = id;

                //https://stackoverflow.com/questions/2376998/force-form-to-redraw
                listBox_traders.Refresh();
            });
        }

        
        private void listBox_traders_DrawItem(object sender, DrawItemEventArgs e) {
            // This will allow to set colour of listBox_trader to highlight stock owner
            // Based on:
            // https://stackoverflow.com/questions/91747/background-color-of-a-listbox-item-winforms
            if (e.Index == -1)
                return;
            e.DrawBackground();
            Graphics g = e.Graphics;
            ListBox lb = (ListBox)sender;
            string id = lb.Items[e.Index].ToString();
            // draw the background color you want
            // mine is set to olive, change it to whatever you want
            lock (stock_holder_id_lock) {
                if (stock_holder_id == int.Parse(id))
                    g.FillRectangle(new SolidBrush(Color.LightBlue), e.Bounds);
                else
                    g.FillRectangle(new SolidBrush(Color.White), e.Bounds);
            }
            // draw the text of the list item, not doing this will only show
            // the background color
            // you will need to get the text of item to display 
            g.DrawString(id, e.Font, new SolidBrush(Color.Black), new PointF(e.Bounds.X, e.Bounds.Y));
            e.DrawFocusRectangle();
        }

        private void updateCount() {
            label_traders.Text = $"Traders (count = {listBox_traders.Items.Count})";
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
