using DNBSoft.WPF;
using Gma.System.MouseKeyHook;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using Telerik.WinControls;
using VidVPN.API;
using RadioButton = System.Windows.Controls.RadioButton;
using RoutedEventArgs = System.Windows.RoutedEventArgs;

namespace VidVPN
{
    /// <summary>
    /// Логика взаимодействия для Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        #region externs

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        #endregion externs

        private const string currentVersion = "1.5";
        private readonly ConfigsController configsController = new ConfigsController();

        private readonly System.Timers.Timer connectedTimer;
        private string ApplicationStartUpPath = System.AppDomain.CurrentDomain.BaseDirectory;
        private bool close;
        private TrayWindow notifierForm;
        private NotifyIcon notifyIcon1 = new NotifyIcon();
        private System.Timers.Timer timer;
        private string vpnLogPath;
        private Process vpnProcess;
        private IKeyboardMouseEvents m_GlobaHook;
        private int LastRunServerID;
        private bool blockReconnectOnServerChanged = true;

        public Window1()
        {
            try
            {
                this.InitializeComponent();
                Telerik.WinControls.Themes.FluentTheme f = new Telerik.WinControls.Themes.FluentTheme();
                f.Load();
                RadMessageBox.SetThemeName("Fluent");
                WindowResizer resiz = new WindowResizer(this);
                resiz.addResizerDown(this.resizer);
                resiz.addResizerUp(this.resizerTop);
                this.OptionsButton_OnClick(null, null);
                if (System.Environment.OSVersion.Version.Major == 7)
                {
                    this.OpenVPNExePath = this.ApplicationStartUpPath + "\\bin7\\openvpn.exe";
                }
                else
                {
                    this.OpenVPNExePath = this.ApplicationStartUpPath + "\\bin\\openvpn.exe";
                }
                this.radLabel25.Text = "v" + currentVersion;
                this.radLabel26.Text = "v" + this.GetLastVersion();
                if (this.radLabel26.Text == this.radLabel25.Text)
                {
                    this.updateButton.Visibility = Visibility.Collapsed;
                }
                string login = Properties.Settings.Default.Login;
                string password = Properties.Settings.Default.Password;
                this.autoStartUpCheckBox.IsChecked = Properties.Settings.Default.AutoStartUp;
                this.rememberMeCheckBox.IsChecked = Properties.Settings.Default.RememberMe;
                this.useTrayCheckBox.IsChecked = !Properties.Settings.Default.UseTray;
                this.autoConnectOnStartUp.IsChecked = Properties.Settings.Default.ConnectOnStartUp;
                this.LastRunServerID = Properties.Settings.Default.LastRunServerID;
                TimeSpan timeDifference = DateTime.Now.Subtract(Properties.Settings.Default.AuthorizeTime);
                if (Properties.Settings.Default.RememberMe)
                {
                    if (timeDifference.TotalDays <= 7)
                    {
                        this.ApiWorker = new ApiWorker(login, password);
                        string resp = this.ApiWorker.Authorize();
                        if (resp == null)
                        {
                            this.Authorize();
                        }
                        else if (this.ApiWorker == null || this.ApiWorker.Servers == null || this.ApiWorker.UserInfo == null)
                        {
                            this.Authorize();
                        }
                    }
                    else
                    {
                        this.Authorize();
                    }
                }
                else
                {
                    this.Authorize();
                }
                //this.UpdateInfo();
                this.configsController.Load();
                this.OpenVPNRadioButton_Checked(this.openVPNRadioButton, null);
                this.notifierForm = new TrayWindow(this);
                this.connectedTimer = new System.Timers.Timer
                {
                    Interval = 1000
                };
                this.connectedTimer.Elapsed += this.ConnectedTimer_Elapsed;
                this.connectedTimer.Start();

                this.notifyIcon1.Text = "VidVPN";
                this.notifyIcon1.DoubleClick += this.NotifyIcon1_DoubleClick;
                this.notifyIcon1.MouseClick += this.NotifyIcon1_MouseClick;
                Stream iconStream = System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/VidVPN;component/vpn_icon.ico")).Stream;
                this.notifyIcon1.Icon = new System.Drawing.Icon(iconStream);
                this.notifyIcon1.Visible = true;
                Task.Run(() =>
                {
                    List<Task> tasks = new List<Task>();
                    foreach (Server item in this.ApiWorker.Servers)
                    {
                        Server l = item;
                        tasks.Add(Task.Run(() =>
                        {
                            System.Net.NetworkInformation.Ping ping = new System.Net.NetworkInformation.Ping();
                            System.Net.NetworkInformation.PingReply pingReply = ping.Send(l.Ip);
                            int roundTripTime = (int)pingReply.RoundtripTime;
                            l.Ping = roundTripTime;
                        }));
                    }
                    Task.WaitAll(tasks.ToArray());
                    this.ApiWorker.Servers = this.ApiWorker.Servers.OrderBy(t => t.Ping).ToList();
                    this.Dispatcher.BeginInvoke((Action)(() =>
                    {
                        this.OpenVPNRadioButton_Checked(this.openVPNRadioButton, null);
                    }));
                    this.Dispatcher.BeginInvoke((Action)(() =>
                    {
                        this.loadingGrid.Children.Clear();
                        this.navigationDrawer.Visibility = Visibility.Visible;
                    }));
                });
                this.m_GlobaHook = Hook.GlobalEvents();
                this.m_GlobaHook.KeyDown += this.M_GlobaHook_KeyDown;
            }
            catch(Exception ex)
            {
                File.WriteAllText("error.txt", ex.Message);
            }
        }

        public API.ApiWorker ApiWorker { get; set; }

        public bool Connected { get; set; }

        private string OpenVPNExePath { get; set; }

        private int UsingConfigID { get; set; }

        public void FinishVPN(bool doAnyway = false)
        {
            if (!doAnyway)
            {
                if (!this.Connected)
                {
                    return;
                }
            }
            this.UsingConfigID = 0;
            this.UpdateSelectedServer();
            this.ChangeServerChooseStatus(false);
            this.connectButtonImage.Source = new Uri("pack://application:,,,/VidVPN;component/Images/disconnected1.svg");
            Stream iconStream = System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/VidVPN;component/vpn_icon.ico")).Stream;
            this.notifyIcon1.Icon = new System.Drawing.Icon(iconStream);
            this.notifierForm.UpdateStatus(1, "Не выбран");
            this.ChangeServerChooseStatus(true);
            this.statusLabel.Text = "Не подключен";
            this.statusLabel.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(237, 85, 101));
            this.Connected = false;
            this.timer?.Stop();
            this.configsController.SavedConfigs.Find(t => t.ID == this.UsingConfigID /*Tag is server id*/)?.Stop();
            this.ProcessClose();
        }

        public string GetLastVersion()
        {
            WebClient wb = new WebClient();
            string result = wb.DownloadString("https://vidvpn.cc/frontend/web/update/GetLastVersion.php");
            return result;
        }

        public void OpenApplication(int result)
        {
            if (result == 1)
            {
                this.WindowState = WindowState.Normal;
                this.ShowInTaskbar = true;
            }
            else
            {
                this.close = true;
                this.Close();
            }
        }

        public void UpdateApplication()
        {
            Task.Run(() =>
            {
                NameValueCollection values = new NameValueCollection
                {
                    ["login"] = this.ApiWorker.Login,
                    ["password"] = this.ApiWorker.Password,
                    ["version"] = currentVersion
                };
                byte[] responseb = (new WebClient()).UploadValues("https://vidvpn.cc/update/", values);
                string response = Encoding.Default.GetString(responseb);
                if (response != "ok")
                {
                    string downloadUrl = response;
                    if (!string.IsNullOrEmpty(downloadUrl))
                    {
                        string filepath = System.IO.Path.GetTempPath() + "\\VidVPN GUI Setup.exe";

                        using (WebClient webClient = new WebClient())
                        {
                            webClient.DownloadFile(downloadUrl, filepath);
                        }
                        this.Dispatcher.BeginInvoke((Action)(() =>
                        {
                            DialogResult result = RadMessageBox.Show("Найдена новой версия программы.\nЖелаете запустить установщик обновления?", "Обновления", MessageBoxButtons.YesNo, RadMessageIcon.Info);
                            if (result == System.Windows.Forms.DialogResult.Yes)
                            {
                                Process.Start(filepath);
                                this.close = true;
                                this.Close();
                            }
                        }));
                    }
                }
                //else
                //{
                //    this.Dispatcher.BeginInvoke((Action)(() =>
                //    {
                //        this.updateButton.Enabled = false;
                //    }));
                //}
            });
        }

        internal void StartVPN(Server server = null)
        {
            this.ProcessClose();
            if (server == null)
            {
                server = this.ApiWorker.Servers.First(t => t.Type == "single");
                this.openVPNRadioButton.IsChecked = true;
                this.proxyListBox.SelectedIndex = 0;
            }
            if (server == null)
            {
                return;
            }
            if (!Directory.Exists(this.ApplicationStartUpPath + "\\logs"))
            {
                Directory.CreateDirectory(this.ApplicationStartUpPath + "\\logs");
            }
            string configName = server.Config.Split('/').Last();
            string vpnconfigPath = this.ApplicationStartUpPath + @"\configs\" + configName;
            this.vpnLogPath = this.ApplicationStartUpPath + "\\logs\\" + configName.Replace("ovpn", "log"); //create log path
            int iqe = this.UpdateConfig(vpnconfigPath, server);
            if (iqe == -1)
            {
                return;
            }

            ProcessStartInfo info = new ProcessStartInfo(this.OpenVPNExePath, $"\"{vpnconfigPath}\"")
            {
                WindowStyle = ProcessWindowStyle.Hidden
            };
            this.vpnProcess = Process.Start(info);

            try
            {
                this.ChangeServerChooseStatus(false);
                this.connectButtonImage.Source = new Uri("pack://application:,,,/VidVPN;component/Images/Connecting.svg");

                this.statusLabel.Text = "Подключение...";
                this.statusLabel.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 243, 156, 18));
                this.timer = new System.Timers.Timer
                {
                    Interval = 5000
                };
                this.UsingConfigID = server.ID;
                this.LastRunServerID = server.ID;
                Server current = this.ApiWorker.Servers.Find(t => t.ID == this.UsingConfigID);
                BitmapImage logo = new BitmapImage();
                logo.BeginInit();
                logo.UriSource = new Uri("pack://application:,,,/VidVPN;component/Flags/" + current.Flag);
                logo.EndInit();
                this.ConnectedServerInfoContainerBorder.Visibility = Visibility.Visible;
                this.connectedServerImage.Source = logo;
                this.connectedServerName.Text = current.Country;
                this.UpdateSelectedServer();
                this.timer.Elapsed += this.Timer_Elapsed;
                this.timer.Start();
                this.notifierForm.UpdateStatus(-1, this.ApiWorker.Servers.Find(t => t.ID == this.UsingConfigID)?.Country);
            }
            catch
            {
                this.ChangeServerChooseStatus(true);
            }
        }

        private void Authorize()
        {
            //AuthorizeForm radForm1 = new AuthorizeForm();
            //this.ApiWorker = radForm1.ShowDialog();
            if (this.ApiWorker == null || this.ApiWorker.Servers == null || this.ApiWorker.UserInfo == null)
            {
                //Environment.Exit(0);
            }
        }
        private void ChangeServerChooseStatus(bool status)
        {
            //this.proxyListBox.IsEnabled = status;
            this.openVPNRadioButton.IsEnabled = status;
            this.doubleVPNRadioButton.IsEnabled = status;
            this.withoutEnctryptionVPNRadioButton.IsEnabled = status;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.connectedTimer.Stop();
            base.Close();
        }

        private void CloseButton_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            this.closeButton.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 0, 0));
            this.closeButtonPath.Stroke = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255));
        }

        private void CloseButton_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            this.closeButtonPath.Stroke = new SolidColorBrush(System.Windows.Media.Color.FromRgb(171, 171, 171));
            this.closeButton.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(0, 255, 255, 255));
        }

        private void ClosePageButton_Click(object sender, RoutedEventArgs e)
        {
            this.navigationPanelListView.SelectedIndex = 0;
        }

        private void ConnectButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (this.ApiWorker.UserInfo.Plans_end == "0")
                {
                    RadMessageBox.Show("Подписка аккаунта истечена. Активация VPN невозможно", "Внимание", MessageBoxButtons.OK, RadMessageIcon.Error);
                    return;
                }
                if (this.Connected || this.timer.Enabled)
                {
                    this.FinishVPN(true);
                    return;
                }
                else
                {
                    this.ProcessClose();
                }
            }
            catch
            {
            }
            int localId = -1;
            if (this.proxyListBox.SelectedIndex == -1 && this.UsingConfigID == 0)
            {
                this.UsingConfigID = 0;
                RadMessageBox.Show("Сервер не выбран", "Ошибка", MessageBoxButtons.OK, RadMessageIcon.Error);
                return;
            }
            else if (this.proxyListBox.SelectedIndex != -1)
            {
                localId = (int)((VPNServerContainer)this.proxyListBox.Items[this.proxyListBox.SelectedIndex]).Tag;
            }
            else
            {
                localId = this.UsingConfigID;
            }
            Server server = this.ApiWorker.Servers.Find(t => t.ID == localId);
            if (server == null)
            {
                RadMessageBox.Show("Сервер не выбран", "Ошибка", MessageBoxButtons.OK, RadMessageIcon.Error);
                return;
            }
            this.StartVPN(server);
        }

        private void ConnectedTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Config server = this.configsController.SavedConfigs.Find(t => t.ID == this.UsingConfigID);
            if (server != null)
            {
                TimeSpan usageTime = TimeSpan.FromSeconds(server.GetRunningTime());
                int totalHours = usageTime.Hours + ((int)usageTime.TotalDays * 24);
                string usageTimestr;
                if (totalHours != 0)
                {
                    usageTimestr = $"{usageTime.Hours + ((int)usageTime.TotalDays * 24)} часов {usageTime.Minutes} минут";
                }
                else
                {
                    usageTimestr = $"{usageTime.Minutes} минут";

                }
                this.Dispatcher.BeginInvoke((ThreadStart)(() =>
                {
                    this.connectedTime.Text = usageTimestr;
                    if (this.connectedTimeContainer.IsVisible == false)
                    {
                        this.connectedTimeContainer.Visibility = Visibility.Visible;
                        this.connectedIpContainer.Visibility = Visibility.Visible;
                    }
                }));
            }
            else
            {
                this.Dispatcher.BeginInvoke((ThreadStart)(() =>
                {
                    this.connectedTime.Text = "Не определено";
                    if (this.connectedTimeContainer.IsVisible == true || this.navigationDrawer.Visibility == Visibility.Hidden)
                    {
                        this.connectedTimeContainer.Visibility = Visibility.Collapsed;
                        this.connectedIpContainer.Visibility = Visibility.Collapsed;
                    }
                }));
            }
        }

        private void LeaveAccountButton_Click(object sender, MouseButtonEventArgs e)
        {
            DialogResult answer = RadMessageBox.Show("Вы уверены что хотите выйти из приложения?", "Внимание", MessageBoxButtons.YesNo, RadMessageIcon.Info);
            if (answer == System.Windows.Forms.DialogResult.No)
            {
                return;
            }

            Properties.Settings.Default.Login = "";
            Properties.Settings.Default.Password = "";
            Properties.Settings.Default.Save();
            this.Hide();
            AuthorizeForm radForm1 = new AuthorizeForm();
            this.ApiWorker = radForm1.ShowDialog();
            if (this.ApiWorker == null || this.ApiWorker.Servers == null || this.ApiWorker.UserInfo == null)
            {
                Environment.Exit(0);
            }
            this.UpdateInfo();
            this.configsController.Load();
            this.navigationPanelListView.SelectedIndex = 0;
            this.OpenVPNRadioButton_Checked(this.openVPNRadioButton, null);
            this.Show();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void NotifyIcon1_DoubleClick(object sender, EventArgs e)
        {
            this.OpenApplication(1);
        }

        private void NotifyIcon1_MouseClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            try
            {
                if (e.Button == MouseButtons.Right)
                {
                    int y = System.Windows.Forms.Cursor.Position.Y;
                    int taskbar = Screen.PrimaryScreen.Bounds.Height - Screen.PrimaryScreen.WorkingArea.Height;
                    if (Screen.PrimaryScreen.Bounds.Height - taskbar < y)
                    {
                        y = Screen.PrimaryScreen.Bounds.Height - taskbar;
                    }

                    this.notifierForm.Left = System.Windows.Forms.Cursor.Position.X - 300;
                    this.notifierForm.Top = y - 374;
                    this.notifierForm.Show();
                }
            }
            catch
            {
            }
        }

        private void OpenVPNRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (!(bool)((RadioButton)sender).IsChecked || this.proxyListBox == null)
            {
                return;
            }
            this.blockReconnectOnServerChanged = true;
            this.proxyListBox.Items.Clear();
            List<Server> servers = new List<Server>();
            if ((RadioButton)sender == this.openVPNRadioButton)
            {
                servers = this.ApiWorker.Servers.Where(t => t.Type == "single").ToList();
            }
            else if ((RadioButton)sender == this.doubleVPNRadioButton)
            {
                servers = this.ApiWorker.Servers.Where(t => t.Type == "double").ToList();
            }
            else if ((RadioButton)sender == this.withoutEnctryptionVPNRadioButton)
            {
                servers = this.ApiWorker.Servers.Where(t => t.Type == "without_encryption").ToList();
            }
            foreach (Server item in servers)
            {
                BitmapImage logo = new BitmapImage();
                logo.BeginInit();
                logo.UriSource = new Uri("pack://application:,,,/VidVPN;component/Flags/" + item.Flag);
                logo.EndInit();
                VPNServerContainer vvv = new VPNServerContainer(this.proxyListBox)
                {
                    Width = 257,
                    Height = 50,
                    Flag = logo
                };
                string lcountry = item.Country;
                FormattedText formattedText = new FormattedText(
                lcountry,
                CultureInfo.CurrentCulture,
                System.Windows.FlowDirection.LeftToRight,
                new Typeface(vvv.proxyName.FontFamily, vvv.proxyName.FontStyle, vvv.proxyName.FontWeight, vvv.proxyName.FontStretch),
                vvv.proxyName.FontSize,
                System.Windows.Media.Brushes.Black,
                new NumberSubstitution());

                if (formattedText.Width >= 182)
                {
                    while (formattedText.Width >= 173)
                    {
                        formattedText = new FormattedText(
                    lcountry,
                    CultureInfo.CurrentCulture,
                    System.Windows.FlowDirection.LeftToRight,
                    new Typeface(vvv.proxyName.FontFamily, vvv.proxyName.FontStyle, vvv.proxyName.FontWeight, vvv.proxyName.FontStretch),
                    vvv.proxyName.FontSize,
                    System.Windows.Media.Brushes.Black,
                    new NumberSubstitution());
                        lcountry = lcountry.Substring(0, lcountry.Length - 1);
                    }
                    lcountry += "...";
                }


                vvv.proxyName.Text = lcountry;
                vvv.Tag = item.ID;

                if (item.Ping > 130)
                {
                    vvv.p3.Fill = new SolidColorBrush(System.Windows.Media.Color.FromRgb(212, 212, 212));
                    if (item.Ping > 180)
                    {
                        vvv.p2.Fill = new SolidColorBrush(System.Windows.Media.Color.FromRgb(212, 212, 212));
                        if (item.Ping > 300)
                        {
                            vvv.p1.Fill = new SolidColorBrush(System.Windows.Media.Color.FromRgb(212, 212, 212));
                        }
                    }
                }

                this.proxyListBox.Items.Add(vvv);
            }
            this.proxyListBox.SelectedIndex = -1;
            this.blockReconnectOnServerChanged = false;
            this.UpdateSelectedServer();
        }
        private void OptionsButton_OnClick(object sender, RoutedEventArgs e)
        {
            {
                Storyboard storyboard = new Storyboard();

                Duration duration = new Duration(TimeSpan.FromMilliseconds(250));

                DoubleAnimation animation = new DoubleAnimation
                {
                    Duration = duration
                };
                storyboard.Children.Add(animation);

                Storyboard.SetTarget(animation, this.col1);
                Storyboard.SetTargetProperty(animation, new PropertyPath("(ColumnDefinition.MaxWidth)"));
                if (!int.TryParse(this.col1.MaxWidth.ToString(),out int f))
                {
                    this.col1.MaxWidth = 170;
                }

                if (this.col1.MaxWidth != 0)
                {
                    animation.From = this.col1.MaxWidth;
                    animation.To = 0;
                }
                else
                {
                    animation.From = this.col1.MaxWidth;
                    animation.To = 170;
                }
                storyboard.Begin();
            }
            {
                Storyboard storyboard = new Storyboard();

                Duration duration = new Duration(TimeSpan.FromMilliseconds(250));

                DoubleAnimation animation = new DoubleAnimation
                {
                    Duration = duration
                };
                storyboard.Children.Add(animation);

                Storyboard.SetTarget(animation, this);
                Storyboard.SetTargetProperty(animation, new PropertyPath("Width"));
                if (!int.TryParse(this.Width.ToString(),out int f))
                {
                    this.Width = 490;
                }

                if (this.Width != 320)
                {
                    animation.From = this.Width;
                    animation.To = 320;
                }
                else
                {
                    animation.From = this.Width;
                    animation.To = 490;

                }
                storyboard.Begin();


            }
        }

        private void pagesTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private void ProcessClose()
        {
            try
            {
                this.ConnectedServerInfoContainerBorder.Visibility = Visibility.Collapsed;
                this.vpnProcess.Kill();
            }
            catch (Exception)
            {
            }
        }

        private void SaveSettingsButton_Click(object sender, MouseButtonEventArgs e)
        {
            Properties.Settings.Default.AutoStartUp = this.autoStartUpCheckBox.IsChecked == true ? true : false;
            Properties.Settings.Default.RememberMe = this.rememberMeCheckBox.IsChecked == true ? true : false;
            Properties.Settings.Default.UseTray = this.useTrayCheckBox.IsChecked == true ? false : true;
            Properties.Settings.Default.ConnectOnStartUp = this.autoConnectOnStartUp.IsChecked == true ? true : false;
            Properties.Settings.Default.Save();
            this.navigationPanelListView.SelectedIndex = 0;
        }

        private void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            for (int i = 1; i <= 4; i++)
            {
                this.Resources["ListViewItemSelectedColor" + i] = new SolidColorBrush(Colors.Black);
            }
            int index = this.navigationPanelListView.SelectedIndex + 1;
            if (index != -1)
            {
                (this.Resources["ListViewItemSelectedColor" + index] as SolidColorBrush).Color = System.Windows.Media.Color.FromArgb(255, 1, 168, 225);
            }

            switch (index)
            {
                case 1:
                    {
                        this.pagesTabControl.SelectedIndex = 0;
                        break;
                    }

                case 2:
                    {
                        this.pagesTabControl.SelectedIndex = 1;

                        break;
                    }
                case 3:
                    {
                        this.pagesTabControl.SelectedIndex = 2;

                        break;
                    }
                case 4:
                    {
                        this.pagesTabControl.SelectedIndex = 3;

                        break;
                    }
            }
        }

        private void ShutDownVPN(bool showmessage)
        {
            this.timer.Stop();
            this.statusLabel.Text = "Не подключен";
            this.statusLabel.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(237, 85, 101));

            this.connectButtonImage.Source = new Uri("pack://application:,,,/VidVPN;component/Images/disconnected1.svg");
            int index = this.configsController.SavedConfigs.FindIndex(t => t.ID == this.UsingConfigID);
            if (index != -1)
            {
                this.configsController.SavedConfigs[index].Stop();
            }
            this.ChangeServerChooseStatus(true);
            this.notifierForm.UpdateStatus(1, "Не выбран");
            this.radLabel3.Text = "Не выбран";
            this.Connected = false;
            this.UsingConfigID = 0;
            this.UpdateSelectedServer();
            Stream iconStream = System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/VidVPN;component/vpn_icon.ico")).Stream;
            Icon ic = new System.Drawing.Icon(iconStream);

            if (this.notifyIcon1.Icon != ic)
            {
                this.notifyIcon1.Icon = ic;
            }

            if (this.ShowInTaskbar && showmessage)
            {
                DialogResult result = RadMessageBox.Show("Возникла ошибка во время работы OpenVPN.\nОткрыть файл лога?", "Ошибка", MessageBoxButtons.YesNo, RadMessageIcon.Error);
                if (result == System.Windows.Forms.DialogResult.Yes)
                {
                    Process.Start(this.vpnLogPath);
                }
            }
        }

        private void TextBlock_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Process.Start("https://vidvpn.cc/lk/request-password-reset");
        }

        private void UpdateSelectedServer(bool disable = false)
        {
            IEnumerable<VPNServerContainer> all = this.proxyListBox.Items.Cast<VPNServerContainer>();
            foreach (VPNServerContainer item in all)
            {
                item.ell.Visibility = Visibility.Hidden;
            }
            if (disable)
            {
                return;
            }

            Server s = this.ApiWorker.Servers.Find(t => t.ID == this.UsingConfigID);
            if (s != null)
            {
                VPNServerContainer container = this.proxyListBox.Items.Cast<VPNServerContainer>().FirstOrDefault(t => (int)t.Tag == s.ID);
                if (container != null)
                {
                    container.ell.Visibility = Visibility.Visible;
                }
            }
        }
        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (File.Exists(this.vpnLogPath))
            {
                FileStream stream = new FileStream(this.vpnLogPath, FileMode.Open, FileAccess.Read, FileShare.Write);
                List<string> logsContent = new StreamReader(stream).ReadToEnd().Split(new string[] { "\r\n" }, StringSplitOptions.None).ToList();
                stream.Close();
                if (logsContent.FindIndex(t => t.Contains("There are no TAP-Windows adapters on this system.")) != -1)
                {
                    this.Dispatcher.BeginInvoke((ThreadStart)(() =>
                    {
                        this.ShutDownVPN(false);

                        DialogResult result = RadMessageBox.Show("Tap адаптер не установлен!\nЗапустить установщик?", "Ошибка", MessageBoxButtons.YesNo, RadMessageIcon.Error);
                        if (result == System.Windows.Forms.DialogResult.Yes)
                        {
                            Process pr = new Process
                            {
                                StartInfo = new ProcessStartInfo(this.ApplicationStartUpPath + "\\tap-fixer.exe")
                                {
                                    UseShellExecute = true
                                }
                            };
                            pr.Start();
                        }
                    }));
                }
                if (this.vpnProcess.HasExited)
                {
                    this.Dispatcher.BeginInvoke((ThreadStart)(() =>
                    {
                        this.ShutDownVPN(true);
                    }));
                    return;
                }
                if (logsContent.FindIndex(t => t.EndsWith("Initialization Sequence Completed")) != -1 && !this.Connected)
                {
                    string ip = Helper.GetLocalIPAddress();

                    this.Dispatcher.BeginInvoke((ThreadStart)(() =>
                    {
                        this.statusLabel.Text = "Подключен к:";
                        this.radLabel3.Text = this.ApiWorker.Servers.Find(t => t.ID == this.UsingConfigID)?.Ip;
                        this.statusLabel.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(96, 186, 47));
                        int index = this.configsController.SavedConfigs.FindIndex(t => t.ID == this.UsingConfigID);
                        this.connectButtonImage.Source = new Uri("pack://application:,,,/VidVPN;component/Images/Connected.svg");
                        Server current = this.ApiWorker.Servers.Find(t => t.ID == this.UsingConfigID);

                        if (index != -1)
                        {
                            this.configsController.SavedConfigs[index].Run();
                        }
                        Stream iconStream = System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/VidVPN;component/ConnectedIcon.ico")).Stream;
                        Icon ic = new System.Drawing.Icon(iconStream);
                        this.notifyIcon1.Icon = ic;
                        this.notifierForm.UpdateStatus(0, this.ApiWorker.Servers.Find(t => t.ID == this.UsingConfigID)?.Country);
                        this.Connected = true;
                        this.ChangeServerChooseStatus(true);
                    }));
                    return;
                }
            }
            else
            {
                this.Dispatcher.BeginInvoke((ThreadStart)(() =>
                {
                    this.ShutDownVPN(true);
                }));
            }
        }

        private void UpdateApplicationButton_Click(object sender, RoutedEventArgs e)
        {
            this.UpdateApplication();
        }
        string[] sp = new string[] { "\r\n","\n" };
        private int UpdateConfig(string vpnconfigPath, Server server)
        {
            try
            {
                if (File.Exists(vpnconfigPath))
                {
                    int index = this.configsController.SavedConfigs.FindIndex(t => t.ID == server.ID);
                    if (index != -1 && this.configsController.SavedConfigs[index].Version != server.Version)
                    {
                        WebClient client = new WebClient();
                        List<string> fileContent = client.DownloadString(server.Config).Split(sp,StringSplitOptions.None).ToList();
                        int authIndex = fileContent.FindIndex(t => t == "auth-user-pass");
                        fileContent[authIndex] = (fileContent[authIndex] + $" \"{this.ApplicationStartUpPath}\\configs\\account.config\"").Replace("\\", "\\\\"); //insert account info file path

                        fileContent.Insert(authIndex + 1, ("log \"" + this.vpnLogPath + "\"").Replace("\\", "\\\\")); //set log path

                        File.WriteAllLines(vpnconfigPath, fileContent); //save config

                        this.configsController.SavedConfigs[index].Version = server.Version;//update config version
                        this.configsController.Save();//save config controller
                    }
                    else if (index == -1)
                    {
                        WebClient client = new WebClient();
                        List<string> fileContent = client.DownloadString(server.Config).Split(sp, StringSplitOptions.None).ToList(); // download file
                        int authIndex = fileContent.FindIndex(t => t == "auth-user-pass"); // search for auth
                        fileContent[authIndex] = (fileContent[authIndex] + $" \"{this.ApplicationStartUpPath}\\configs\\account.config\"").Replace("\\", "\\\\"); //update auth options
                        fileContent.Insert(authIndex + 1, ("log \"" + this.vpnLogPath + "\"").Replace("\\", "\\\\")); //set log path
                                                                                                                      //save config
                        File.WriteAllLines(vpnconfigPath, fileContent);
                        this.configsController.SavedConfigs.Add(new Config(server.ID, vpnconfigPath, server.Version));
                        this.configsController.Save();
                    }
                }
                else
                {
                    WebClient client = new WebClient();
                    var str = client.DownloadString(server.Config);
                    List<string> fileContent = client.DownloadString(server.Config).Split(sp, StringSplitOptions.None).ToList();
                    int authIndex = fileContent.FindIndex(t => t == "auth-user-pass");
                    fileContent[authIndex] = (fileContent[authIndex] + $" \"{this.ApplicationStartUpPath}\\configs\\account.config\"").Replace("\\", "\\\\");
                    fileContent.Insert(authIndex + 1, ("log \"" + this.vpnLogPath + "\"").Replace("\\", "\\\\"));
                    File.WriteAllLines(vpnconfigPath, fileContent);
                    this.configsController.SavedConfigs.Add(new Config(server.ID, vpnconfigPath, server.Version));
                    this.configsController.Save();
                }
                return 0;
            }
            catch
            {
                return -1;
            }
        }

        private void UpdateInfo()
        {
            this.accountLogin.Text = this.ApiWorker.Login;
            this.balanceLabel.Text = this.ApiWorker.UserInfo.Balance.ToString() + "₽";
            if (this.ApiWorker.UserInfo.Plans_end != "0")
            {
                double daysleft = DateTime.ParseExact(this.ApiWorker.UserInfo.Plans_end, "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture).Subtract(DateTime.Now).TotalDays;
                this.subscriptionLeftContentTextBlock.Text = Convert.ToInt32(daysleft).ToString();
                this.subscriptionLeftLabelTextBlock.Text = "Осталось дней подписки: ";
                this.connectButtonImage.Source = new Uri("pack://application:,,,/VidVPN;component/Images/disconnected1.svg");
                this.connectButton.IsEnabled = true;
                this.activeTill.Text = this.ApiWorker.UserInfo.Plans_end;
            }
            else
            {
                this.subscriptionLeftContentTextBlock.Text = "";
                this.subscriptionLeftLabelTextBlock.Text = "Подписка истечена";
                this.connectButtonImage.Source = new Uri("pack://application:,,,/VidVPN;component/Images/disabled.svg");

                this.connectButton.IsEnabled = false;
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Properties.Settings.Default.LastRunServerID = this.LastRunServerID;
            Properties.Settings.Default.Save();

            this.connectedTimer.Close();
            this.ProcessClose();
            for (int i = 0; i < this.configsController.SavedConfigs.Count; i++)
            {
                this.configsController.SavedConfigs[i].Stop();
            }
            this.configsController.Save();
            this.notifyIcon1.Visible = false;
            Environment.Exit(0);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (this.close || !Properties.Settings.Default.UseTray)
            {
                this.notifierForm = null;
                return;
            }

            e.Cancel = true;
            this.WindowState = WindowState.Minimized;
            this.ShowInTaskbar = false;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //this.UpdateInfo();
            if ((bool)this.autoConnectOnStartUp.IsChecked)
            {
                Server s = this.ApiWorker.Servers.Find(t => t.ID == this.LastRunServerID);
                if (s != null)
                {
                    this.StartVPN(s);
                }
            }
        }

        private void WindowsDragGrid_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);

            this.DragMove();
        }
        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.StartVPN();
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftAlt) && Keyboard.IsKeyDown(Key.PrintScreen))
            {

            }
        }
        private void M_GlobaHook_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            if (e.KeyCode == Keys.PrintScreen && e.Alt && this.IsActive)
            {
                CopyUIElementToClipboard(this);
            }
        }
        public static void CopyUIElementToClipboard(Window1 element)
        {
            double width = element.ActualWidth;
            double height = element.ActualHeight;
            RenderTargetBitmap bmpCopied = new RenderTargetBitmap((int)Math.Round(width), (int)Math.Round(height), 96, 96, PixelFormats.Default);
            DrawingVisual dv = new DrawingVisual();
            using (DrawingContext dc = dv.RenderOpen())
            {
                VisualBrush vb = new VisualBrush(element);
                dc.DrawRectangle(vb, null, new Rect(new System.Windows.Point(), new System.Windows.Size(width, height)));
            }
            bmpCopied.Render(dv);
            System.Windows.Clipboard.SetImage(bmpCopied);
        }
        private void proxyListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.RemovedItems.Count != 0)
            {
                //(e.RemovedItems[0] as VPNServerContainer).ell.Visibility = Visibility.Visible;
                //(e.RemovedItems[0] as VPNServerContainer).xx.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255));
                //(e.RemovedItems[0] as VPNServerContainer).LeaveActive = false;
            }
            if (e.AddedItems.Count != 0)
            {
                //(e.RemovedItems[0] as VPNServerContainer).ell.Visibility = Visibility.Hidden;

                //(e.AddedItems[0] as VPNServerContainer).xx.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(239, 239, 239));
                //(e.AddedItems[0] as VPNServerContainer).LeaveActive = true;
            }
            if (this.Connected && !this.blockReconnectOnServerChanged)
            {
                DialogResult answer = RadMessageBox.Show("Вы уже подключены к другому серверу. Желаете переподключиться?", "Внимание", MessageBoxButtons.YesNo, RadMessageIcon.Question);
                if (answer == System.Windows.Forms.DialogResult.Yes)
                {
                    this.FinishVPN(true);
                    this.ConnectButton_PreviewMouseDown(null, null);
                }
            }
        }

        private void proxyListBox_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            object item = this.proxyListBox.SelectedItem;
            if (item != null)
            {
                this.FinishVPN(true);
                this.ConnectButton_PreviewMouseDown(null, null);
            }
        }
    }
}