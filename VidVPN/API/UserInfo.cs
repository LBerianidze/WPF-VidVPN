namespace VidVPN.API
{
    public class UserInfo
    {
        public string Username { get; }
        public string Email { get; }
        public double Balance { get; }
        public string Plans_end { get; set; }
        public string DateNow { get; set; }
        public UserInfo(dynamic json)
        {
            this.Username = json.username;
            this.Email = json.email;
            this.Balance = json.balance;
            this.Plans_end = json.plans_end;
            this.DateNow = json.time_now;
        }
    }
}
