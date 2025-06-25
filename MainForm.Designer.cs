namespace BrokenHelper
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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

        private void InitializeComponent()
        {
            this.startStopButton = new System.Windows.Forms.Button();
            this.statusLabel = new System.Windows.Forms.Label();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fightsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.instancesMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // startStopButton
            // 
            this.startStopButton.Location = new System.Drawing.Point(12, 12);
            this.startStopButton.Name = "startStopButton";
            this.startStopButton.Size = new System.Drawing.Size(94, 29);
            this.startStopButton.TabIndex = 0;
            this.startStopButton.Text = "Start";
            this.startStopButton.UseVisualStyleBackColor = true;
            this.startStopButton.Click += new System.EventHandler(this.startStopButton_Click);
            // 
            // statusLabel
            //
            this.statusLabel.AutoSize = true;
            this.statusLabel.Location = new System.Drawing.Point(12, 55);
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new System.Drawing.Size(63, 20);
            this.statusLabel.TabIndex = 1;
            this.statusLabel.Text = "Stopped";

            // menuStrip1
            //
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fightsMenuItem,
            this.instancesMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(200, 28);
            this.menuStrip1.TabIndex = 2;
            this.menuStrip1.Text = "menuStrip1";

            // fightsMenuItem
            //
            this.fightsMenuItem.Name = "fightsMenuItem";
            this.fightsMenuItem.Size = new System.Drawing.Size(54, 24);
            this.fightsMenuItem.Text = "Walki";
            this.fightsMenuItem.Click += new System.EventHandler(this.fightsMenuItem_Click);

            // instancesMenuItem
            //
            this.instancesMenuItem.Name = "instancesMenuItem";
            this.instancesMenuItem.Size = new System.Drawing.Size(84, 24);
            this.instancesMenuItem.Text = "Instancje";
            this.instancesMenuItem.Click += new System.EventHandler(this.instancesMenuItem_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(200, 128);
            this.Controls.Add(this.statusLabel);
            this.Controls.Add(this.startStopButton);
            this.Controls.Add(this.menuStrip1);
            this.Name = "MainForm";
            this.Text = "Packet Listener";
            this.MainMenuStrip = this.menuStrip1;
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Button startStopButton;
        private System.Windows.Forms.Label statusLabel;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fightsMenuItem;
        private System.Windows.Forms.ToolStripMenuItem instancesMenuItem;
    }
}
