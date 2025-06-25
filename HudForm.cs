using System.Windows.Forms;

namespace BrokenHelper
{
    public class HudForm : Form
    {
        private readonly Label _label = new() { Text = "HUD", AutoSize = true };

        public HudForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;
            Width = 200;
            Height = 100;
            BackColor = System.Drawing.Color.Black;
            ForeColor = System.Drawing.Color.Lime;

            Controls.Add(_label);
            _label.Left = 10;
            _label.Top = 10;
        }
    }
}
