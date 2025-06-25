using System.Windows.Forms;

namespace BrokenHelper
{
    public class BufferedDataGridView : DataGridView
    {
        public BufferedDataGridView()
        {
            DoubleBuffered = true;
        }
    }
}
