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
            this.hudCheckBox = new System.Windows.Forms.CheckBox();
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
            // hudCheckBox
            //
            this.hudCheckBox.AutoSize = true;
            this.hudCheckBox.Location = new System.Drawing.Point(12, 90);
            this.hudCheckBox.Name = "hudCheckBox";
            this.hudCheckBox.Size = new System.Drawing.Size(89, 24);
            this.hudCheckBox.TabIndex = 2;
            this.hudCheckBox.Text = "Show HUD";
            this.hudCheckBox.UseVisualStyleBackColor = true;
            this.hudCheckBox.CheckedChanged += new System.EventHandler(this.hudCheckBox_CheckedChanged);
            //
            // statusLabel
            //
            this.statusLabel.AutoSize = true;
            this.statusLabel.Location = new System.Drawing.Point(12, 55);
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new System.Drawing.Size(63, 20);
            this.statusLabel.TabIndex = 1;
            this.statusLabel.Text = "Stopped";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(220, 130);
            this.Controls.Add(this.statusLabel);
            this.Controls.Add(this.startStopButton);
            this.Controls.Add(this.hudCheckBox);
            this.Name = "MainForm";
            this.Text = "Packet Listener";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Button startStopButton;
        private System.Windows.Forms.Label statusLabel;
        private System.Windows.Forms.CheckBox hudCheckBox;
    }
}
