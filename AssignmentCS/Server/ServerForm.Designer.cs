namespace ServerNamespace
{
    partial class ServerForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ServerForm));
            this.listBox_traders = new System.Windows.Forms.ListBox();
            this.listView_log = new System.Windows.Forms.ListView();
            this.label_traders = new System.Windows.Forms.Label();
            this.label_events = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // listBox_traders
            // 
            this.listBox_traders.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.listBox_traders.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.listBox_traders.FormattingEnabled = true;
            this.listBox_traders.Location = new System.Drawing.Point(12, 25);
            this.listBox_traders.Name = "listBox_traders";
            this.listBox_traders.SelectionMode = System.Windows.Forms.SelectionMode.None;
            this.listBox_traders.Size = new System.Drawing.Size(98, 303);
            this.listBox_traders.Sorted = true;
            this.listBox_traders.TabIndex = 0;
            // 
            // listView_log
            // 
            this.listView_log.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.listView_log.HideSelection = false;
            this.listView_log.Location = new System.Drawing.Point(116, 25);
            this.listView_log.Name = "listView_log";
            this.listView_log.Size = new System.Drawing.Size(682, 303);
            this.listView_log.TabIndex = 2;
            this.listView_log.UseCompatibleStateImageBehavior = false;
            // 
            // label_traders
            // 
            this.label_traders.AutoSize = true;
            this.label_traders.Location = new System.Drawing.Point(12, 9);
            this.label_traders.Name = "label_traders";
            this.label_traders.Size = new System.Drawing.Size(43, 13);
            this.label_traders.TabIndex = 3;
            this.label_traders.Text = "Traders";
            // 
            // label_events
            // 
            this.label_events.AutoSize = true;
            this.label_events.Location = new System.Drawing.Point(113, 9);
            this.label_events.Name = "label_events";
            this.label_events.Size = new System.Drawing.Size(40, 13);
            this.label_events.TabIndex = 4;
            this.label_events.Text = "Events";
            // 
            // ServerForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Gold;
            this.ClientSize = new System.Drawing.Size(810, 339);
            this.Controls.Add(this.label_events);
            this.Controls.Add(this.label_traders);
            this.Controls.Add(this.listView_log);
            this.Controls.Add(this.listBox_traders);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "ServerForm";
            this.Text = "Server";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        public System.Windows.Forms.ListBox listBox_traders;
        public System.Windows.Forms.ListView listView_log;
        private System.Windows.Forms.Label label_traders;
        private System.Windows.Forms.Label label_events;
    }
}