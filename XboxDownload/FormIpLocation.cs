using System.Text.Json;
using System.Text.RegularExpressions;

namespace XboxDownload
{
    public partial class FormIpLocation : Form
    {
        public string key = string.Empty;

        public FormIpLocation()
        {
            InitializeComponent();
        }

        readonly CancellationTokenSource cts = new();
        private async void FormIpLocation_Load(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(Properties.Settings.Default.IpLocation))
            {
                textBox1.Text = Properties.Settings.Default.IpLocation;
            }
            else
            {
                using HttpResponseMessage? response1 = await ClassWeb.HttpResponseMessageAsync("https://qifu-api.baidubce.com/ip/local/geo/v1/district", "GET", null, null, null, 3000, null, cts.Token);
                if (response1 != null && response1.IsSuccessStatusCode)
                {
                    JsonDocument? jsonDocument = null;
                    try
                    {
                        jsonDocument = JsonDocument.Parse(response1.Content.ReadAsStringAsync().Result);
                    }
                    catch { }
                    if (jsonDocument != null)
                    {
                        JsonElement root = jsonDocument.RootElement;
                        if (root.TryGetProperty("data", out JsonElement je))
                        {
                            string country = je.TryGetProperty("country", out JsonElement jeCountry) ? jeCountry.ToString().Trim() : "";
                            string prov = je.TryGetProperty("prov", out JsonElement jeProv) ? jeProv.ToString().Trim() : "";
                            if (!cts.IsCancellationRequested && country == "中国" && !Regex.IsMatch(prov, @"香港|澳门|台湾"))
                            {
                                prov = prov switch
                                {
                                    "内蒙古自治区" => "内蒙古",
                                    "广西壮族自治区" => "广西",
                                    "西藏自治区" => "西藏",
                                    "宁夏回族自治区" => "宁夏",
                                    "新疆维吾尔自治区" => "新疆",
                                    _ => Regex.Replace(prov, @"省|市", ""),
                                };
                                textBox1.Text = prov;
                                textBox1.SelectAll();
                                Properties.Settings.Default.IpLocation = prov;
                                Properties.Settings.Default.Save();
                            }
                        }
                    }
                }
            }
        }

        private void TextBox1_KeyDown(object sender, KeyEventArgs e)
        {
            cts.Cancel();
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            key = textBox1.Text.Trim();
            if (string.IsNullOrEmpty(key)) return;
            Properties.Settings.Default.IpLocation = key;
            Properties.Settings.Default.Save();
            this.Close();
        }
    }
}
