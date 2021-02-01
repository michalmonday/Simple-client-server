namespace ClientNamespace
{
    partial class ClientForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ClientForm));
            this.button_give_stock = new System.Windows.Forms.Button();
            this.listBox_traders = new System.Windows.Forms.ListBox();
            this.label_stock_holder = new System.Windows.Forms.Label();
            this.label_traders = new System.Windows.Forms.Label();
            this.listView_log = new System.Windows.Forms.ListView();
            this.label_events = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // button_give_stock
            // 
            this.button_give_stock.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.button_give_stock.Location = new System.Drawing.Point(12, 161);
            this.button_give_stock.Name = "button_give_stock";
            this.button_give_stock.Size = new System.Drawing.Size(98, 29);
            this.button_give_stock.TabIndex = 0;
            this.button_give_stock.Text = "Give stock";
            this.button_give_stock.UseVisualStyleBackColor = true;
            this.button_give_stock.Click += new System.EventHandler(this.button_give_stock_Click);
            // 
            // listBox_traders
            // 
            this.listBox_traders.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.listBox_traders.FormattingEnabled = true;
            this.listBox_traders.Location = new System.Drawing.Point(12, 25);
            this.listBox_traders.Name = "listBox_traders";
            this.listBox_traders.Size = new System.Drawing.Size(98, 108);
            this.listBox_traders.Sorted = true;
            this.listBox_traders.TabIndex = 1;
            // 
            // label_stock_holder
            // 
            this.label_stock_holder.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label_stock_holder.AutoSize = true;
            this.label_stock_holder.Location = new System.Drawing.Point(9, 145);
            this.label_stock_holder.Name = "label_stock_holder";
            this.label_stock_holder.Size = new System.Drawing.Size(73, 13);
            this.label_stock_holder.TabIndex = 4;
            this.label_stock_holder.Text = "Stock holder: ";
            // 
            // label_traders
            // 
            this.label_traders.AutoSize = true;
            this.label_traders.Location = new System.Drawing.Point(12, 9);
            this.label_traders.Name = "label_traders";
            this.label_traders.Size = new System.Drawing.Size(43, 13);
            this.label_traders.TabIndex = 6;
            this.label_traders.Text = "Traders";
            // 
            // listView_log
            // 
            this.listView_log.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.listView_log.HideSelection = false;
            this.listView_log.Location = new System.Drawing.Point(116, 25);
            this.listView_log.Name = "listView_log";
            this.listView_log.Size = new System.Drawing.Size(491, 165);
            this.listView_log.TabIndex = 7;
            this.listView_log.UseCompatibleStateImageBehavior = false;
            // 
            // label_events
            // 
            this.label_events.AutoSize = true;
            this.label_events.Location = new System.Drawing.Point(113, 9);
            this.label_events.Name = "label_events";
            this.label_events.Size = new System.Drawing.Size(40, 13);
            this.label_events.TabIndex = 9;
            this.label_events.Text = "Events";
            // 
            // ClientForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.ClientSize = new System.Drawing.Size(619, 202);
            this.Controls.Add(this.label_events);
            this.Controls.Add(this.listView_log);
            this.Controls.Add(this.label_traders);
            this.Controls.Add(this.label_stock_holder);
            this.Controls.Add(this.listBox_traders);
            this.Controls.Add(this.button_give_stock);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ClientForm";
            this.Text = "Client (id = ?)";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button button_give_stock;
        private System.Windows.Forms.ListBox listBox_traders;
        private System.Windows.Forms.Label label_stock_holder;
        private System.Windows.Forms.Label label_traders;
        public System.Windows.Forms.ListView listView_log;
        private System.Windows.Forms.Label label_events;
    }
}

