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
using VidVPN.API;

namespace VidVPN
{
    /// <summary>
    /// Логика взаимодействия для AccountPage.xaml
    /// </summary>
    public partial class ConnectionPage : Page
    {
        public ConnectionPage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            ApiWorker apiWorker = new ApiWorker("admin", "admin1");
            apiWorker.Authorize();
            foreach (var item in apiWorker.Servers)
            {
                BitmapImage logo = new BitmapImage();
                logo.BeginInit();
                logo.UriSource = new Uri("pack://application:,,,/VidVPN;component/Flags/" + item.Flag);
                logo.EndInit();
                VPNServerContainer vvv = new VPNServerContainer(lviev)
                {
                    Width = 295,
                    Height = 50,
                    Flag = logo
                };
                vvv.proxyName.Text = item.Country;

                lviev.Items.Add(vvv);

            }
        }

        private void connectButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            connectButton.Background = null;
            
        }
    }
}
