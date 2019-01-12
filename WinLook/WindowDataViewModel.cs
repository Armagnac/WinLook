using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace WinLook
{
    public class WindowDataViewModel
    {
        public IntPtr Handle
        {
            get { return _Window.Handle; }
            set { _Window.Handle = value; }
        }

        public ImageSource Icon
        {
            get { return _Window.Icon; }
            set { _Window.Icon = value; }
        }

        public String Title
        {
            get { return _Window.Title; }
            set { _Window.Title = value; }
        }

        public Boolean Flashing
        {
            get { return _Window.Flashing; }
            set { _Window.Flashing = value; }
        }

        private readonly WindowData _Window;

        public WindowDataViewModel(WindowData window)
        {
            _Window = window;
        }
    }
}
