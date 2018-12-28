using System;

namespace QharonSzyne.UI.Utilities
{
    public class Win32Window : System.Windows.Forms.IWin32Window
    {
        public Win32Window(IntPtr handle)
        {
            this.Handle = handle;
        }

        public IntPtr Handle { get; }
    }
}
