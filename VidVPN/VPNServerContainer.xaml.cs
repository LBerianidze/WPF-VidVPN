using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace VidVPN
{
    /// <summary>
    /// Логика взаимодействия для UserControl1.xaml
    /// </summary>
    public partial class VPNServerContainer : UserControl
    {
        ListBox container;
        public VPNServerContainer(ListBox container)
        {
            this.container = container;
            InitializeComponent();
        }


        public static readonly DependencyProperty FlagProperty = DependencyProperty.Register("FlagProperty", typeof(ImageSource), typeof(VPNServerContainer), new PropertyMetadata(null, new PropertyChangedCallback(OnFlagImageChanged)));
        public ImageSource Flag
        {
            get
            {
                return (ImageSource)GetValue(FlagProperty);
            }
            set
            {
                SetValue(FlagProperty, value);
            }
        }

        public bool LeaveActive { get; set; }

        private static void OnFlagImageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as VPNServerContainer).OnFlagImageChanged(e);
        }
        private void OnFlagImageChanged(DependencyPropertyChangedEventArgs e)
        {
            this.flagImage.Source = e.NewValue as ImageSource;
        }
        public static DependencyObject GetScrollViewer(DependencyObject o)
        {
            if (o is ScrollViewer)
            { return o; }

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(o); i++)
            {
                var child = VisualTreeHelper.GetChild(o, i);

                var result = GetScrollViewer(child);
                if (result == null)
                {
                    continue;
                }
                else
                {
                    return result;
                }
            }
            return null;
        }


        private void xx_MouseLeave(object sender, MouseEventArgs e)
        {
            //if(LeaveActive)
            //{
            //    xx.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(239, 239, 239));
            //    e.Handled = true;
            //}
        }
    }
}
