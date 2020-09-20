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
    public partial class UserControl1 : UserControl
    {
        CountriesContainerListView container;
        public UserControl1(CountriesContainerListView container,int startindex)
        {
            this.container = container;
            InitializeComponent();
            proxyCitiesContainer.Items.Add(new UserControl2() { Width = 255, Height = 50,Tag=startindex++ });
            proxyCitiesContainer.Items.Add(new UserControl2() { Width = 255, Height = 50,Tag=startindex++ });
            proxyCitiesContainer.Items.Add(new UserControl2() { Width = 255, Height = 50,Tag=startindex++ });
            proxyCitiesContainer.Items.Add(new UserControl2() { Width = 255, Height = 50,Tag=startindex++ });
            proxyCitiesContainer.Items.Add(new UserControl2() { Width = 255, Height = 50,Tag= startindex++ });
        }

        internal void UnSelectAll()
        {
            proxyCitiesContainer.UnselectAll();
        }

        public static readonly DependencyProperty FlagProperty = DependencyProperty.Register("FlagProperty", typeof(ImageSource), typeof(UserControl1), new PropertyMetadata(null, new PropertyChangedCallback(OnFlagImageChanged)));
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
        private static void OnFlagImageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as UserControl1).OnFlagImageChanged(e);
        }
        private void OnFlagImageChanged(DependencyPropertyChangedEventArgs e)
        {
            this.flagImage.Source = e.NewValue as ImageSource;
        }
        bool expanded = false;
        private void UIElement_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!expanded)
            {
                this.Height = ((proxyCitiesContainer.Items.Count + 1) * 50) + 22;
                proxyCitiesContainer.Height = ((proxyCitiesContainer.Items.Count + 1) * 50);
            }
            else
            {
                this.Height = 50;
                proxyCitiesContainer.Height = 0;

            }
            expanded = !expanded;
        }

        private void subItems_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
        }

        private void subItems_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var scrollViwer = GetScrollViewer(container as DependencyObject) as ScrollViewer;
            if (scrollViwer != null)
            {
                if (e.Delta < 0)
                {
                    scrollViwer.ScrollToVerticalOffset(scrollViwer.VerticalOffset + 15);
                }
                else if (e.Delta > 0)
                {
                    scrollViwer.ScrollToVerticalOffset(scrollViwer.VerticalOffset - 15);
                }
            }
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


        private void subItems_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
           var el =  proxyCitiesContainer.InputHitTest(e.GetPosition(container));
            
         }


        private void subItems_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(this.proxyCitiesContainer.SelectedIndex!=-1)
            container.UnSelectAll(this);
        }

        internal int GetSelectedProxy()
        {
           UserControl2 userControl2= (this.proxyCitiesContainer.SelectedItem as UserControl2);
            if(userControl2!=null)
            {
                return Convert.ToInt32(userControl2.Tag);
            }
            return -1;
        }
    }
}
