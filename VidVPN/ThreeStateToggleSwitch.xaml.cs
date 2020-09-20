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
    /// Логика взаимодействия для ThreeStateToggleSwitch.xaml
    /// </summary>
    public partial class ThreeStateToggleSwitch : UserControl
    {
        public ThreeStateToggleSwitch()
        {
            InitializeComponent();
        }
        int currentState = 1;

        public int CurrentState => this.currentState;

        public void Switch(int state)
        {
            currentState = state;
            Ellipse_PreviewMouseLeftButtonDown(null, null);
        }
        private void Ellipse_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if(currentState==-1)
            {
                switcher.Margin =(Thickness) this.Resources["Center"];

                BitmapImage logo = new BitmapImage();
                logo.BeginInit();
                logo.UriSource = new Uri("pack://application:,,,/VidVPN;component/Images/TrayToggle_Connecting.png");
                logo.EndInit();

                switcher.Fill = new ImageBrush(logo);
            }
            else if (currentState==0)
            {
                switcher.Margin =(Thickness) this.Resources["Right"];

                BitmapImage logo = new BitmapImage();
                logo.BeginInit();
                logo.UriSource = new Uri("pack://application:,,,/VidVPN;component/Images/TrayToggle_Connected.png");
                logo.EndInit();

                switcher.Fill = new ImageBrush(logo);

            }
            else
            {
                switcher.Margin = (Thickness)this.Resources["Left"];

                BitmapImage logo = new BitmapImage();
                logo.BeginInit();
                logo.UriSource = new Uri("pack://application:,,,/VidVPN;component/Images/TrayToggle_Disconnected.png");
                logo.EndInit();

                switcher.Fill = new ImageBrush(logo);

            }
        }
    }
}
