using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Telerik.WinControls;
using VidVPN.API;

namespace VidVPN
{
    public partial class AuthorizeForm : Telerik.WinControls.UI.RadForm
    {
        public AuthorizeForm()
        {
            InitializeComponent();
        }
        private void RadForm1_Load(object sender, EventArgs e)
        {
        }

        public new ApiWorker ShowDialog()
        {
            base.ShowDialog();
            return apiWorker;

        }
        ApiWorker apiWorker;
        private void LoginButton_Click(object sender, EventArgs e)
        {
            apiWorker = new ApiWorker(loginTextBox.Text, passwordTextBox.Text);
            var response = apiWorker.Authorize();
            if (response == "ok")
            {
                Properties.Settings.Default.Login = loginTextBox.Text;
                Properties.Settings.Default.Password = passwordTextBox.Text;
                Properties.Settings.Default.AuthorizeTime = DateTime.Now;
                Properties.Settings.Default.Save();
                if(!Directory.Exists(Application.StartupPath+"\\configs"))
                {
                    Directory.CreateDirectory(Application.StartupPath + "\\configs");
                }
                File.WriteAllText(Application.StartupPath + "\\configs\\account.config", $"{loginTextBox.Text}\n{passwordTextBox.Text}");
                this.Close();
            }
            else if (response==null)
            {
                RadMessageBox.Show("Сервер недоступен", "", MessageBoxButtons.OK, RadMessageIcon.Error);
            }
            else
            {
                loginTextBox.BackgroundImage = Properties.Resources.login_error;
                passwordTextBox.BackgroundImage = Properties.Resources.login_error;
                refreshButton.Visibility = ElementVisibility.Visible;
                refreshButton.Image = Properties.Resources.error_image;
            }
        }

        private void RadButtonTextBox2_TextChanged(object sender, EventArgs e)
        {
        }

        private void RadButtonTextBox2_Enter(object sender, EventArgs e)
        {
            loginTextBox.BackgroundImage = Properties.Resources.login_active;
            passwordTextBox.BackgroundImage = Properties.Resources.password_default;
            refreshButton.Visibility = ElementVisibility.Visible;
            refreshButton.Image = Properties.Resources.New_Project;
            panel2.Visible = false;

        }

        private void RadButtonTextBox2_Leave(object sender, EventArgs e)
        {
            loginTextBox.BackgroundImage = Properties.Resources.Login_default;
            passwordTextBox.BackgroundImage = Properties.Resources.password_default;
            refreshButton.Visibility = ElementVisibility.Hidden;

        }

        private void RefreshButton_Click(object sender, EventArgs e)
        {
            loginTextBox.Text = "";
            passwordTextBox.Text = "";
        }

        private void LinkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("https://vidvpn.cc/lk/request-password-reset");

        }
        private void LinkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("https://vidvpn.cc/lk/signup");
        }
        private void LoginTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.KeyCode == Keys.Enter)
            {
                LoginButton_Click(null, null);
            }
        }

        private void PasswordTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                LoginButton_Click(null, null);
            }

        }
    }
}
