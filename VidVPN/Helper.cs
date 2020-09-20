using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace VidVPN
{
    public static class Helper
    {
        public static Bitmap ResizeBitmap(this Bitmap bmp, int width, int height)
        {
            Bitmap result = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(result))
            {
                g.DrawImage(bmp, 0, 0, width, height);
            }

            return result;
        }
        public static string GetLocalIPAddress()
        {
            string externalip = new WebClient().DownloadString("http://icanhazip.com");
            return externalip.Trim();
        }
    }
}
