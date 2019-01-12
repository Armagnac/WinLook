using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace WinLook
{
    public class WindowData
    {
        public IntPtr Handle;
        public ImageSource Icon;
        public String Title;
        public Boolean Flashing;

        public WindowData(IntPtr handle, ImageSource icon, String title, Boolean flashing)
        {
            Handle = handle;
            Icon = icon;
            Title = title;
            Flashing = flashing;
        }
    }
}
