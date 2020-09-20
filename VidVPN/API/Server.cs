namespace VidVPN.API
{
    public class Server
    {
        public int ID { get; set; }
        public string Type { get; set; }
        public string Country { get; set; }
        public string Flag { get; set; }
        public string Config { get; set; }
        public int Version { get; set; }
        public string Ip { get; set; }
        public int Ping { get; set; }
        public Server(dynamic server)
        {
            this.ID = server.server_id;
            this.Type = server.type;
            this.Country = server.country_name;
            this.Flag = server.flag;
            this.Config = server.config_path;
            this.Version = server.version;
            this.Ip = server.server_ip;
        }
    }
}
