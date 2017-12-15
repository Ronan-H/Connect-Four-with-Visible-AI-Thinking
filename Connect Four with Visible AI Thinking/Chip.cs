using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace Connect_Four_with_Visible_AI_Thinking
{
    class Chip : INotifyPropertyChanged
    {
        public SolidColorBrush _color;

        public Chip(SolidColorBrush color)
        {
            ChipChanged = color;
        }

        public SolidColorBrush ChipChanged
        {
            get
            {
                return _color;
            }

            set
            {
                _color = value;
                NotifyPropertyChanged();
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            if (PropertyChanged != null)
            {
                Debug.WriteLine("Property Name: " + propertyName);
                
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
