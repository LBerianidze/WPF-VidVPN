using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace VidVPN
{
    public class CountriesContainerListView : ListView
    {
        public void AddCountry(object item)
        {
            this.Items.Add(item);
        }

        internal int GetSelectedProxy()
        {
            foreach (UserControl1 item in this.Items)
            {
                int selected = item.GetSelectedProxy();
                if (selected != -1)
                {
                    return selected;
                }
            }
            return -1;
        }
        public void UnSelectAll(object except)
        {
            foreach (UserControl1 item in this.Items)
            {
                if (except != item)
                    item.UnSelectAll();
            }
        }
    }
}
