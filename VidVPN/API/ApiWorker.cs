using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Text;
using System.Timers;

namespace VidVPN.API
{
    public class ApiWorker
    {
       public string Login { get; }
       public string Password { get; }
        public List<Server> Servers { get; set; }
        public UserInfo UserInfo { get; set; }
        public ApiWorker(string login, string password)
        {
            this.Login = login;
            this.Password = password;
        }
        public string Authorize()
        {
            try
            {
                var values = new NameValueCollection();
                values["username"] = Login;
                values["password"] = Password;
                var responseb = (new WebClient()).UploadValues("https://vidvpn.cc/api/v1/authdesktop", values);
                var response = Encoding.UTF8.GetString(responseb);



                dynamic json = JObject.Parse(response);
                int status = json.status;
                if (status == 200)
                {
                    UserInfo = new UserInfo(json.userdata);
                    Servers = new List<Server>();
                    if (json.servers != null)
                        foreach (var item in json.servers)
                        {
                            Server serv = new Server(item);
                            Servers.Add(serv);
                        }
                    Servers = Servers.OrderBy(t => t.Country).ToList();
                    return "ok";
                }
                return json.error?.ToString();
            }
            catch
            {
                return null;
            }
        }
    }
}
