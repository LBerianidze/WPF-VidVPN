using Gma.System.MouseKeyHook;
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
using System.Windows.Shapes;

namespace VidVPN
{
    /// <summary>
    /// Логика взаимодействия для TrayWindow.xaml
    /// </summary>
    public partial class TrayWindow : Window
    {
        Window1 mainWindow;
        IKeyboardMouseEvents m_GlobaHook;

        public TrayWindow(Window1 window1)
        {
            InitializeComponent();
            m_GlobaHook = Hook.GlobalEvents();
            this.mainWindow = window1;
        }
        /// <summary>
        /// Устанавливает статус подключения в окне трея
        /// </summary>
        /// <param name="state">Статус подключения.0 - подключен,-1 - подключаемся, 1 - не подключен</param>
        /// <param name="server"></param>
        internal void UpdateStatus(int state, string server)
        {
            //On.ValueChanged -= OnToggleSwitch_ValueChanged;
            radLabel7.Text = server;
            On.Switch(state);
            //On.ValueChanged += OnToggleSwitch_ValueChanged;
        }
        public new void Show()
        {
            m_GlobaHook = Hook.GlobalEvents();

            m_GlobaHook.MouseDownExt += this.ShapedForm1_MouseDown;
            this.radLabel3.Text = this.mainWindow.ApiWorker.UserInfo.Balance + " ₽";
            if (this.mainWindow.ApiWorker.UserInfo.Plans_end != "0")
            {
                double daysleft = DateTime.ParseExact(this.mainWindow.ApiWorker.UserInfo.Plans_end, "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture).Subtract(DateTime.Now).TotalDays;
                this.radLabel4.Text = Convert.ToInt32(daysleft).ToString();
            }
            else
            {
                this.radLabel4.Text = "-";
                On.IsEnabled = false;
            }
            base.Show();
        }
        private void ShutDownGlobalHook()
        {
            try
            {
                m_GlobaHook.MouseDownExt += ShapedForm1_MouseDown;
                m_GlobaHook.Dispose();
                m_GlobaHook = null;
            }
            catch
            {

            }
        }

        private void ShapedForm1_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (this.IsVisible)
            {
                double x = this.Left;
                double y = this.Top;
                double width = this.Width;
                double height = this.Height;
                if (e.Location.X >= x && this.Left <= x + width && e.Location.Y >= y && e.Location.Y <= y + height)
                {

                }
                else
                {
                    ShutDownGlobalHook();
                    this.Hide();
                }
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ShutDownGlobalHook();
            //Thread.Sleep(500);
            this.Close();
            this.mainWindow.OpenApplication(2);
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            this.Hide();
            this.mainWindow.OpenApplication(1);
        }

        private void Button_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ShutDownGlobalHook();
            this.Hide();
        }

        private void On_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if(On.CurrentState == -1 || On.CurrentState == 0)
            {
                this.mainWindow.FinishVPN(true);
            }
            else
            {
                this.mainWindow.StartVPN();
            }
        }
    }
}
