using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Windows;

namespace ShinonoDNSUnlocker
{
    public partial class MainWindow : Window
    {
        private bool isVietnamese = true;

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Log("Khởi động ShinonoDNSUnlocker (SDU) thành công...");
            UpdateLanguage();
            LoadNetworkAdapters();
            await PingDNSAsync();
        }

        #region --- NGÔN NGỮ (LANGUAGE) ---
        private void BtnLangVN_Click(object sender, RoutedEventArgs e) { isVietnamese = true; UpdateLanguage(); }
        private void BtnLangEN_Click(object sender, RoutedEventArgs e) { isVietnamese = false; UpdateLanguage(); }

        private void UpdateLanguage()
        {
            lblAdapter.Text = isVietnamese ? "Chọn Card Mạng:" : "Select Adapter:";
            btnRefreshAdapters.Content = isVietnamese ? "Làm mới" : "Refresh";
            btnSetCF.Content = isVietnamese ? "1. Đổi DNS (Khuyên dùng)" : "1. Set DNS (Recommended)";
            btnSetCFDoH.Content = isVietnamese ? "2. DNS + DoH (Mạnh hơn)" : "2. DNS + DoH (Stronger)";
            btnSetGG.Content = btnSetCF.Content;
            btnSetGGDoH.Content = btnSetCFDoH.Content;
            btnSetCustom.Content = isVietnamese ? "Áp dụng Custom DNS" : "Apply Custom DNS";
            lblHostsDesc.Text = isVietnamese ? "Dùng khi đổi DNS không hiệu quả. Ghi cứng IP Steam vào file hệ thống." : "Use when DNS fails. Hardcode Steam IP into system hosts file.";
            btnApplyHosts.Content = isVietnamese ? "Áp dụng Hosts Bypass" : "Apply Hosts Bypass";
            btnRemoveHosts.Content = isVietnamese ? "Xóa Hosts Bypass" : "Remove Hosts Bypass";
            btnRestore.Content = isVietnamese ? "Khôi phục Mặc định (DHCP)" : "Restore Default (DHCP)";
            btnTestSteam.Content = isVietnamese ? "Kiểm tra Kết nối Steam" : "Test Steam Connection";
            Log(isVietnamese ? "Đã đổi ngôn ngữ sang Tiếng Việt." : "Language changed to English.");
        }
        #endregion

        #region --- TÍNH NĂNG MẠNG & PING ---
        private void LoadNetworkAdapters()
        {
            cmbAdapters.Items.Clear();
            var nics = NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType != NetworkInterfaceType.Loopback);

            foreach (var nic in nics)
            {
                cmbAdapters.Items.Add(nic.Name);
            }
            if (cmbAdapters.Items.Count > 0) cmbAdapters.SelectedIndex = 0;
            Log(isVietnamese ? "Đã tải danh sách Card mạng." : "Loaded network adapters.");
        }

        private void BtnRefreshAdapters_Click(object sender, RoutedEventArgs e) => LoadNetworkAdapters();

        private async Task PingDNSAsync()
        {
            lblPingCF.Text = await GetPingResult("1.1.1.1", "Cloudflare");
            lblPingGG.Text = await GetPingResult("8.8.8.8", "Google");
        }

        private async Task<string> GetPingResult(string ip, string name)
        {
            try
            {
                Ping pingSender = new Ping();
                PingReply reply = await pingSender.SendPingAsync(ip, 2000);
                if (reply.Status == IPStatus.Success)
                    return $"Ping {name}: {reply.RoundtripTime} ms";
                return $"Ping {name}: Timeout";
            }
            catch { return $"Ping {name}: Error"; }
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
                        Log("ERROR: " + error);
                    }
                }
            }
            catch (Exception ex)
            {
                Log("Exception: " + ex.Message);
            }
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

        private void BtnSetCustom_Click(object sender, RoutedEventArgs e)
        {
            string dns1 = txtCustomDNS1.Text.Trim();
            string dns2 = txtCustomDNS2.Text.Trim();
            if (string.IsNullOrEmpty(dns1)) { MessageBox.Show("Nhập ít nhất 1 IP"); return; }
            SetDNS(dns1, string.IsNullOrEmpty(dns2) ? "1.1.1.1" : dns2, "::1", "::1"); // IPv6 stub
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

        // IP tĩnh của Akamai/Steam. (Cập nhật được nếu Steam đổi IP)
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
                Log(isVietnamese ? "Đã áp dụng Hosts Bypass thành công!" : "Hosts Bypass applied successfully!");
            }
            catch (Exception ex) { Log("Lỗi ghi file Hosts (Chưa có quyền Admin?): " + ex.Message); }
        }

        private void BtnRemoveHosts_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string[] lines = File.ReadAllLines(hostsPath);
                var newLines = lines.SkipWhile(l => l.Contains(bypassMarker))
                                    .TakeWhile(l => !l.Contains(bypassEndMarker))
                                    .ToList(); // Logic simplified for safety: remove block

                string resultText = "";
                bool inBlock = false;
                foreach (var line in lines)
                {
                    if (line.Contains(bypassMarker)) { inBlock = true; continue; }
                    if (line.Contains(bypassEndMarker)) { inBlock = false; continue; }
                    if (!inBlock) resultText += line + "\n";
                }

                File.WriteAllText(hostsPath, resultText.TrimEnd() + "\n");
                if (sender != null) Log(isVietnamese ? "Đã xóa Steam Hosts Bypass." : "Removed Steam Hosts Bypass.");
            }
            catch (Exception ex) { Log("Lỗi xóa file Hosts: " + ex.Message); }
        }
        #endregion

        #region --- STEAM CONNECTION TEST ---
        private void BtnTestSteam_Click(object sender, RoutedEventArgs e)
        {
            Log(isVietnamese ? "Đang chạy bài Test. Vui lòng đợi..." : "Running tests. Please wait...");
            string script = @"
                try{$r=Invoke-WebRequest 'https://store.steampowered.com/' -UseBasicParsing -TimeoutSec 5; Write-Output ('Store: HTTP '+$r.StatusCode)}catch{Write-Output ('Store: '+$_.Exception.Message)};
                try{$r=Invoke-WebRequest 'https://steamcommunity.com/' -UseBasicParsing -TimeoutSec 5; Write-Output ('Community: HTTP '+$r.StatusCode)}catch{Write-Output ('Community: '+$_.Exception.Message)}
            ";
            RunPowerShell(script, isVietnamese ? "Hoàn thành kiểm tra (Xem log phía trên)" : "Test finished (See log above)");
        }
        #endregion
    }
}