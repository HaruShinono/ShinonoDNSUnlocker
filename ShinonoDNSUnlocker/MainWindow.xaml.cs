using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace ShinonoDNSUnlocker
{
    public partial class MainWindow : Window
    {
        private bool isVietnamese = true;
        private DispatcherTimer pingTimer;
        private static readonly HttpClient httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Log("Khởi động ShinonoDNSUnlocker (SDU) thành công...");
            UpdateLanguage();
            LoadNetworkAdapters();

            // Chạy Ping ngay lập tức
            await PingDNSAsync();

            // Setup bộ hẹn giờ Ping mỗi 5 giây
            pingTimer = new DispatcherTimer();
            pingTimer.Interval = TimeSpan.FromSeconds(5);
            pingTimer.Tick += async (s, args) => await PingDNSAsync();
            pingTimer.Start();
        }

        #region --- NGÔN NGỮ (LANGUAGE) ---
        private void BtnLangVN_Click(object sender, RoutedEventArgs e) { isVietnamese = true; UpdateLanguage(); }
        private void BtnLangEN_Click(object sender, RoutedEventArgs e) { isVietnamese = false; UpdateLanguage(); }

        private void UpdateLanguage()
        {
            lblAdapter.Text = isVietnamese ? "Chọn Card Mạng:" : "Select Adapter:";
            btnRefreshAdapters.Content = isVietnamese ? "Làm mới" : "Refresh";

            btnSetCF.Content = isVietnamese ? "1. Đổi DNS" : "1. Set DNS";
            btnSetCFDoH.Content = isVietnamese ? "2. Đổi DNS + DoH (Mạnh)" : "2. Set DNS + DoH (Strong)";
            btnSetGG.Content = btnSetCF.Content;
            btnSetGGDoH.Content = btnSetCFDoH.Content;

            lblHostsDesc.Text = isVietnamese ? "Dùng khi DNS/DoH không hoạt động. Ghi IP trực tiếp vào hệ thống." : "Use when DNS fails. Hardcode Steam IP into system directly.";
            btnApplyHosts.Content = isVietnamese ? "ÁP DỤNG BYPASS" : "APPLY BYPASS";
            btnRemoveHosts.Content = isVietnamese ? "XÓA BYPASS" : "REMOVE BYPASS";

            btnRestore.Content = isVietnamese ? "Khôi phục Mặc định (DHCP)" : "Restore Default (DHCP)";
            btnTestSteam.Content = isVietnamese ? "Kiểm tra Kết nối Steam" : "Test Steam Connection";
            Log(isVietnamese ? "Đã đổi ngôn ngữ sang Tiếng Việt." : "Language changed to English.");
        }
        #endregion

        #region --- TÍNH NĂNG MẠNG & PING (Auto 5s) ---
        private void LoadNetworkAdapters()
        {
            cmbAdapters.Items.Clear();
            var nics = NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType != NetworkInterfaceType.Loopback);

            foreach (var nic in nics) cmbAdapters.Items.Add(nic.Name);
            if (cmbAdapters.Items.Count > 0) cmbAdapters.SelectedIndex = 0;
            Log(isVietnamese ? "Đã cập nhật danh sách Card mạng." : "Updated network adapters.");
        }

        private void BtnRefreshAdapters_Click(object sender, RoutedEventArgs e) => LoadNetworkAdapters();

        private async Task PingDNSAsync()
        {
            // Không log ra Console để tránh rác màn hình
            string cfPing = await GetPingResult("1.1.1.1");
            string ggPing = await GetPingResult("8.8.8.8");

            lblPingCF.Text = $"Ping: {cfPing}";
            lblPingGG.Text = $"Ping: {ggPing}";
        }

        private async Task<string> GetPingResult(string ip)
        {
            try
            {
                using (Ping pingSender = new Ping())
                {
                    PingReply reply = await pingSender.SendPingAsync(ip, 2000);
                    return reply.Status == IPStatus.Success ? $"{reply.RoundtripTime} ms" : "Timeout";
                }
            }
            catch { return "Error"; }
        }
        #endregion

        #region --- XỬ LÝ DNS & POWERSHELL ---
        private void Log(string message)
        {
            Dispatcher.Invoke(() =>
            {
                string time = DateTime.Now.ToString("HH:mm:ss");
                txtLog.Text += $"[{time}] {message}\n";
                LogScroll.ScrollToEnd();
            });
        }

        private void RunPowerShell(string script, string successMsg)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo()
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (Process process = Process.Start(psi))
                {
                    process.WaitForExit();
                    if (process.ExitCode == 0)
                    {
                        Log(successMsg);
                        FlushDNS();
                    }
                    else
                    {
                        string error = process.StandardError.ReadToEnd();
                        Log("LỖI (ERROR): " + error);
                    }
                }
            }
            catch (Exception ex) { Log("Lỗi hệ thống: " + ex.Message); }
        }

        private void FlushDNS()
        {
            Process.Start(new ProcessStartInfo("ipconfig", "/flushdns") { CreateNoWindow = true, UseShellExecute = false });
            Log(isVietnamese ? "Đã xóa bộ nhớ đệm DNS (Flush DNS)." : "Flushed DNS Cache.");
        }

        private string GetSelectedAdapter()
        {
            if (cmbAdapters.SelectedItem == null)
            {
                MessageBox.Show(isVietnamese ? "Vui lòng chọn Card mạng!" : "Please select an adapter!");
                return null;
            }
            return cmbAdapters.SelectedItem.ToString();
        }

        private void SetDNS(string v4_1, string v4_2, string v6_1, string v6_2)
        {
            string adapter = GetSelectedAdapter();
            if (adapter == null) return;
            Log(isVietnamese ? $"Đang áp dụng DNS cho [{adapter}]..." : $"Applying DNS for [{adapter}]...");

            string script = $@"
                netsh interface ipv4 set dnsservers name='{adapter}' source=static address={v4_1} validate=no;
                netsh interface ipv4 add dnsservers name='{adapter}' address={v4_2} index=2 validate=no;
                netsh interface ipv6 set dnsservers name='{adapter}' source=static address={v6_1} validate=no;
                netsh interface ipv6 add dnsservers name='{adapter}' address={v6_2} index=2 validate=no;
            ";
            RunPowerShell(script, isVietnamese ? "Cài đặt DNS thành công!" : "DNS Set Successfully!");
        }

        private void EnableDoH(string v4_1, string v4_2, string template)
        {
            string script = $@"
                try {{
                    Set-DnsClientDohServerAddress -ServerAddress {v4_1} -DohTemplate '{template}' -AllowFallbackToUdp $false -AutoUpgrade $true -ErrorAction Stop;
                    Set-DnsClientDohServerAddress -ServerAddress {v4_2} -DohTemplate '{template}' -AllowFallbackToUdp $false -AutoUpgrade $true -ErrorAction Stop;
                    exit 0;
                }} catch {{ exit 1; }}
            ";
            RunPowerShell(script, isVietnamese ? "Kích hoạt DNS over HTTPS (DoH) thành công!" : "Enabled DNS over HTTPS (DoH) successfully!");
        }

        // --- BUTTON EVENTS ---
        private void BtnSetCF_Click(object sender, RoutedEventArgs e) => SetDNS("1.1.1.1", "1.0.0.1", "2606:4700:4700::1111", "2606:4700:4700::1001");

        private void BtnSetCFDoH_Click(object sender, RoutedEventArgs e)
        {
            SetDNS("1.1.1.1", "1.0.0.1", "2606:4700:4700::1111", "2606:4700:4700::1001");
            EnableDoH("1.1.1.1", "1.0.0.1", "https://cloudflare-dns.com/dns-query");
        }

        private void BtnSetGG_Click(object sender, RoutedEventArgs e) => SetDNS("8.8.8.8", "8.8.4.4", "2001:4860:4860::8888", "2001:4860:4860::8844");

        private void BtnSetGGDoH_Click(object sender, RoutedEventArgs e)
        {
            SetDNS("8.8.8.8", "8.8.4.4", "2001:4860:4860::8888", "2001:4860:4860::8844");
            EnableDoH("8.8.8.8", "8.8.4.4", "https://dns.google/dns-query");
        }

        private void BtnRestore_Click(object sender, RoutedEventArgs e)
        {
            string adapter = GetSelectedAdapter();
            if (adapter == null) return;
            string script = $@"
                netsh interface ipv4 set dnsservers name='{adapter}' source=dhcp;
                netsh interface ipv6 set dnsservers name='{adapter}' source=dhcp;
            ";
            RunPowerShell(script, isVietnamese ? "Đã khôi phục DNS về mặc định nhà mạng (DHCP)." : "Restored DNS to default (DHCP).");
        }
        #endregion

        #region --- STEAM HOSTS BYPASS ---
        private readonly string hostsPath = @"C:\Windows\System32\drivers\etc\hosts";
        private readonly string bypassMarker = "# SDU_STEAM_BYPASS_START";
        private readonly string bypassEndMarker = "# SDU_STEAM_BYPASS_END";

        private readonly string steamHostsData = @"
104.18.32.115 store.steampowered.com
104.18.32.115 steamcommunity.com
23.220.106.120 steamcommunity.com
";

        private void BtnApplyHosts_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                BtnRemoveHosts_Click(null, null); // Clear old first
                File.AppendAllText(hostsPath, $"\n{bypassMarker}\n{steamHostsData.Trim()}\n{bypassEndMarker}\n");
                FlushDNS();
                Log(isVietnamese ? "Đã áp dụng Hosts Bypass thành công! IP Steam đã được lưu cứng." : "Hosts Bypass applied successfully!");
            }
            catch (Exception ex) { Log("Lỗi ghi file Hosts: " + ex.Message); }
        }

        private void BtnRemoveHosts_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!File.Exists(hostsPath)) return;
                string[] lines = File.ReadAllLines(hostsPath);
                string resultText = "";
                bool inBlock = false;

                foreach (var line in lines)
                {
                    if (line.Contains(bypassMarker)) { inBlock = true; continue; }
                    if (line.Contains(bypassEndMarker)) { inBlock = false; continue; }
                    if (!inBlock && !string.IsNullOrWhiteSpace(line)) resultText += line + "\n";
                }

                File.WriteAllText(hostsPath, resultText);
                if (sender != null) Log(isVietnamese ? "Đã xóa Steam Hosts Bypass khỏi hệ thống." : "Removed Steam Hosts Bypass.");
            }
            catch (Exception ex) { Log("Lỗi xóa file Hosts: " + ex.Message); }
        }
        #endregion

        #region --- NATIVE STEAM CONNECTION TEST (C# HttpClient) ---
        private async void BtnTestSteam_Click(object sender, RoutedEventArgs e)
        {
            btnTestSteam.IsEnabled = false;
            Log(isVietnamese ? "Đang kiểm tra kết nối tới máy chủ Steam..." : "Testing connection to Steam servers...");

            await TestUrlAsync("https://store.steampowered.com/", "Steam Store");
            await TestUrlAsync("https://steamcommunity.com/", "Steam Community");

            Log(isVietnamese ? "Hoàn tất kiểm tra." : "Test finished.");
            btnTestSteam.IsEnabled = true;
        }

        private async Task TestUrlAsync(string url, string name)
        {
            try
            {
                HttpResponseMessage response = await httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    Log($"[PASS] {name} -> Truy cập THÀNH CÔNG (HTTP {(int)response.StatusCode})");
                }
                else
                {
                    Log($"[WARNING] {name} -> Kết nối được nhưng trả về lỗi: {(int)response.StatusCode}");
                }
            }
            catch (HttpRequestException ex)
            {
                Log($"[FAIL] {name} -> BỊ CHẶN HOẶC LỖI MẠNG! ({ex.Message})");
            }
            catch (TaskCanceledException)
            {
                Log($"[FAIL] {name} -> HẾT THỜI GIAN CHỜ (Timeout)! Nhà mạng có thể đang chặn.");
            }
        }
        #endregion
    }
}