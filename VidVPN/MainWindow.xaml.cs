using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using VidVPN.API;

namespace VidVPN
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            fr.LoadCompleted += this.Fr_LoadCompleted;
            fr.Navigate(new Uri("/Pages/ConnectionPage.xaml", UriKind.Relative));
            (new Window1()).Show();

        }
        bool first = true;
        private void Fr_LoadCompleted(object sender, NavigationEventArgs e)
        {
            if(first)
            {
                page = fr.Content;
                first = false;
            }
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            {
                Storyboard storyboard = new Storyboard();

                Duration duration = new Duration(TimeSpan.FromMilliseconds(250));

                DoubleAnimation animation = new DoubleAnimation();
                animation.Duration = duration;
                storyboard.Children.Add(animation);

                Storyboard.SetTarget(animation, col1);
                Storyboard.SetTargetProperty(animation, new PropertyPath("(ColumnDefinition.MaxWidth)"));
                if (col1.MaxWidth.ToString() == "∞")
                    col1.MaxWidth = 170;
                if (col1.MaxWidth != 0)
                {
                    animation.From = col1.MaxWidth;
                    animation.To = 0;

                }
                else
                {
                    animation.From = col1.MaxWidth;
                    animation.To = 170;
                }
                storyboard.Begin();
            }
            {
                Storyboard storyboard = new Storyboard();

                Duration duration = new Duration(TimeSpan.FromMilliseconds(250));

                DoubleAnimation animation = new DoubleAnimation();
                animation.Duration = duration;
                storyboard.Children.Add(animation);

                Storyboard.SetTarget(animation, this);
                Storyboard.SetTargetProperty(animation, new PropertyPath("Width"));
                if (this.Width.ToString() == "∞")
                    this.Width = 470;
                if (this.Width != 300)
                {
                    animation.From = this.Width;
                    animation.To = 300;

                }
                else
                {
                    animation.From = this.Width;
                    animation.To = 470;
                }
                storyboard.Begin();
            }

        }
        object page;
        private void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            for (int i = 1; i <= 4; i++)
            {
                this.Resources["ListViewItemSelectedColor" + i] = new SolidColorBrush(Colors.Black);


            }
            int index = sidebar_ListView.SelectedIndex + 1;
            if (index != -1)
                (this.Resources["ListViewItemSelectedColor" + index] as SolidColorBrush).Color = Color.FromArgb(255, 1, 168, 225);
            switch(index)
            {
                case 1:
                    {
                        fr.Content = page;
                        break;

                    }

                case 2:
                    {
                        fr.Navigate(new Uri("/Pages/SettingsPage.xaml", UriKind.Relative));

                        break;

                    }
                case 3:
                    {
                        fr.Navigate(new Uri("/Pages/AccountPage.xaml", UriKind.Relative));

                        break;

                    }
                case 4:
                    {
                        fr.Navigate(new Uri("/Pages/UpdatePage.xaml", UriKind.Relative));

                        break;
                    }
            }
        }

        private void UIElement_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);

            DragMove();
        }
        private void UIElement_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
        }
    }
}
