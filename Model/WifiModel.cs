namespace Camera.Model
{
    public class WifiModel
    {
        public string SSID { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool IsConnected { get; set; } = false;

        public bool IsnotConnected { get; set; } = true;
        public string ServerIP { get; set; } = "192.168.4.1";
        public int ServerPort { get; set; } = 5000;

    }
}
