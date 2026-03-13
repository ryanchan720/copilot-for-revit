using System.Net;
using System.Linq;
using System.Windows;

namespace Main.Core.Views
{
    public partial class WebWindow : Window
    {
        public enum RunType
        {
            Prod,
            Dev
        }
        public WebWindow(RunType runType)
        {
            InitializeComponent();
            // Configure the chat server URL for your deployment:
            // - Dev mode: localhost development server
            // - Prod mode: your production server URL (default placeholder shown below)
            string chatServerUrl = runType == RunType.Dev
                ? "http://localhost:3300/chat/single"
                : "http://localhost:3300/chat/single"; // Replace with your production URL
            InitializeWebView(chatServerUrl);
        }

        // Initialize WebView control
        private async void InitializeWebView(string url)
        {
            await webView.EnsureCoreWebView2Async(null);

            // Get local IPv4 address (excluding loopback and vEthernet virtual adapters)
            string localIp = "";
            var host = Dns.GetHostEntry(Dns.GetHostName());
            var ip = host.AddressList
                .Select((address, index) => new { Address = address, Index = index })
                .FirstOrDefault(a =>
                    a.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork &&
                    !a.Address.ToString().StartsWith("127.") &&
                    !GetNetworkInterfaceNameByIp(a.Address.ToString()).Contains("vEthernet")
                );
            if (ip != null)
                localIp = ip.Address.ToString();

            // Store local IP in localStorage after page navigation completes
            System.EventHandler<Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs> handler = null;
            handler = (s, e) =>
            {
                webView.CoreWebView2.NavigationCompleted -= handler;
                webView.CoreWebView2.ExecuteScriptAsync($"localStorage.setItem('host', '{localIp}');");
            };
            webView.CoreWebView2.NavigationCompleted += handler;

            webView.CoreWebView2.Navigate(url);
        }

        // Helper method: Get network interface name by IP address
        private static string GetNetworkInterfaceNameByIp(string ipAddress)
        {
            foreach (var ni in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
            {
                var ipProps = ni.GetIPProperties();
                foreach (var addr in ipProps.UnicastAddresses)
                {
                    if (addr.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork &&
                        addr.Address.ToString() == ipAddress)
                    {
                        return ni.Name;
                    }
                }
            }
            return string.Empty;
        }
    }

}