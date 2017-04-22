using System;
using System.Windows.Forms;

namespace DoenaSoft.DVDProfiler.CleanUpPhotos
{
    public class CleanUpListBox : ListBox
    {
        public void ClearItems()
        {
            Items.Clear();
            OnSelectedIndexChanged(EventArgs.Empty);
        }
    }
}