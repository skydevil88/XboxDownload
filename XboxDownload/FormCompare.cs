using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace XboxDownload
{
    public partial class FormCompare : Form
    {
        private readonly string productId;
        private readonly ConcurrentDictionary<String, DataGridViewRow> dicDgvr = new();
        private bool discount_ListPrice_1 = false, discount_ListPrice_2 = false, discount_WholesalePrice_1 = false, discount_WholesalePrice_2 = false;
        private string? member = null;

        public FormCompare(object js, int index)
        {
            InitializeComponent();

            if (Form1.dpiFactor > 1)
            {
                dataGridView1.RowHeadersWidth = (int)(dataGridView1.RowHeadersWidth * Form1.dpiFactor);
                foreach (DataGridViewColumn col in dataGridView1.Columns)
                    col.Width = (int)(col.Width * Form1.dpiFactor);
            }

            var json = (ClassGame.Game)js;
            var product = json.Products[index];
            this.productId = product.ProductId;

            int cbWidth = (int)(135 * Form1.dpiFactor), cbHeight = (int)(17 * Form1.dpiFactor);
            List<Market> lsMarket = new();
            lsMarket.AddRange(new List<Market>
            {
                new("Algeria", "阿尔及利亚", "DZ", "ar-DZ"),
                new("Oman", "阿曼", "OM", "ar-OM"),
                new("Egypt", "埃及", "EG", "ar-EG"),
                new("Pakistan", "巴基斯坦", "PK", "en-PK"),
                new("Bahrain", "巴林", "BH", "ar-BH"),
                new("Bulgaria", "保加利亚", "BG", "bg-BG"),
                new("Iceland", "冰岛","IS",  "is-IS"),
                new("Philippines", "菲律宾", "PH", "en-PH"),
                new("Costa Rica", "哥斯达黎加", "CR", "es-CR"),
                new("Kazakhstan", "哈萨克斯坦", "KZ", "ru-KZ"),
                new("Qatar", "卡塔尔", "QA", "en-QA"),
                new("Kuwait", "科威特", "KW", "ar-KW"),
                new("Kenya", "肯尼亚", "KE", "en-KE"),
                new("Lebanon", "黎巴嫩", "LB", "ar-LB"),
                new("Liechtenstein", "列支敦士登", "LI", "de-LI"),
                new("Romania", "罗马尼亚", "RO", "ro-RO"),
                new("Malaysia", "马来西亚", "MY", "en-MY"),
                new("Mauritania", "毛里塔尼亚乌吉亚", "MR", "ar-MR"),
                new("Bengal", "孟加拉", "BD", "en-BD"),
                new("Peru", "秘鲁", "PE", "es-PE"),
                new("Nigeria", "尼日利亚", "NG", "en-NG"),
                new("Serbia", "塞尔维亚", "RS", "en-RS"),
                new("Thailand", "泰国", "TH", "th-TH"),
                new("Trinidad and Tobago", "特立尼达和多巴哥", "TT", "en-TT"),
                new("Tunisia", "突尼斯", "TN", "ar-TN"),
                new("Guatemala", "危地马拉", "GT", "es-GT"),
                new("Ukraine", "乌克兰", "UA", "uk-UA"),
                new("Iraq", "伊拉克", "IQ", "ar-IQ"),
                new("Indonesia", "印度尼西亚", "ID", "id-ID"),
                new("Jordan", "约旦", "JO", "ar-JO"),
                new("Vietnam", "越南", "VN", "vi-VN")
            }.ToArray());

            List<Market> ls = Form1.lsMarket.Union(lsMarket).ToList<Market>();
            ls.Sort((x, y) => string.Compare(x.cname, y.cname));
            foreach (Market market in ls)
            {
                string code = market.code;
                string name = market.cname;
                string lang = market.language;
                switch (code)
                {
                    case "DE":
                    case "NL":
                    case "FR":
                    case "SK":
                    case "PT":
                    case "FI":
                    case "IE":
                    case "AT":
                    case "IT":
                    case "BE":
                    case "GR":
                    case "ES":
                        code = "DE";
                        name = "欧元区";
                        lang = "de-DE";
                        break;
                }
                if (dicDgvr.ContainsKey(code)) continue;
                CheckBox cb = new()
                {
                    Text = name,
                    Size = new Size(cbWidth, cbHeight),
                    Parent = this.flowLayoutPanel1
                };
                cb.CheckedChanged += new EventHandler(CheckBox_CheckedChanged);
                DataGridViewRow dgvr = new();
                dgvr.CreateCells(dataGridView1);
                dgvr.Resizable = DataGridViewTriState.False;
                dgvr.Cells[0].Value = code;
                dgvr.Cells[1].Value = lang;
                dgvr.Cells[2].Value = name;
                dgvr.Cells[11].Value = "双击前往";
                cb.Tag = dgvr;
                dicDgvr.TryAdd(code, dgvr);
            }

            double MSRP = product.DisplaySkuAvailabilities[0].Availabilities[0].OrderManagementData.Price.MSRP;
            double ListPrice_1 = product.DisplaySkuAvailabilities[0].Availabilities[0].OrderManagementData.Price.ListPrice;
            double ListPrice_2 = product.DisplaySkuAvailabilities[0].Availabilities.Count >= 2 ? product.DisplaySkuAvailabilities[0].Availabilities[1].OrderManagementData.Price.ListPrice : 0;
            double WholesalePrice_1 = product.DisplaySkuAvailabilities[0].Availabilities[0].OrderManagementData.Price.WholesalePrice;
            double WholesalePrice_2 = product.DisplaySkuAvailabilities[0].Availabilities.Count >= 2 ? product.DisplaySkuAvailabilities[0].Availabilities[1].OrderManagementData.Price.WholesalePrice : 0;

            double priceRatio = 0;
            if (ListPrice_1 > 0 && ListPrice_1 != MSRP)
            {
                discount_ListPrice_1 = true;
                priceRatio = Math.Round(ListPrice_1 / MSRP * 100, 0, MidpointRounding.AwayFromZero);
            }
            if (ListPrice_2 > 0 && ListPrice_2 < ListPrice_1 && ListPrice_2 != MSRP)
            {
                discount_ListPrice_2 = true;
                priceRatio = Math.Round(ListPrice_2 / MSRP * 100, 0, MidpointRounding.AwayFromZero);
                this.member = (product.DisplaySkuAvailabilities[0].Availabilities[1].Properties.MerchandisingTags != null && product.DisplaySkuAvailabilities[0].Availabilities[1].Properties.MerchandisingTags.Length >= 1 && product.DisplaySkuAvailabilities[0].Availabilities[1].Properties.MerchandisingTags[0] == "LegacyDiscountEAAccess") ? "EA Play" : "会员";
                dataGridView1.Columns["Col_ListPrice_2"].HeaderText = this.member + "折扣";
            }
            if (WholesalePrice_1 > 0)
            {
                discount_WholesalePrice_1 = true;
                discount_WholesalePrice_2 = WholesalePrice_2 > 0 && WholesalePrice_2 < WholesalePrice_1;
            }
            dataGridView1.Columns["Col_ListPrice_1"].Visible = discount_ListPrice_1;
            dataGridView1.Columns["Col_ListPrice_2"].Visible = discount_ListPrice_2;
            dataGridView1.Columns["Col_WholesalePrice_1"].Visible = discount_WholesalePrice_1;
            dataGridView1.Columns["Col_WholesalePrice_2"].Visible = discount_WholesalePrice_2;

            if (discount_ListPrice_1 || discount_ListPrice_2 || discount_WholesalePrice_2)
                groupBox2.Text = product.LocalizedProperties[0].ProductTitle + " (折扣: " + priceRatio + "%，剩余：" + (new TimeSpan(product.DisplaySkuAvailabilities[0].Availabilities[0].Conditions.EndDate.Ticks - DateTime.Now.Ticks).Days) + "天，打折时段：" + product.DisplaySkuAvailabilities[0].Availabilities[0].Conditions.StartDate + " - " + product.DisplaySkuAvailabilities[0].Availabilities[0].Conditions.EndDate + ")";
            else if (product.LocalizedProperties[0].EligibilityProperties != null && product.LocalizedProperties[0].EligibilityProperties.Affirmations.Length >= 1)
            {
                string description = product.LocalizedProperties[0].EligibilityProperties.Affirmations[0].Description;
                if (description.Contains("EA Play"))
                    groupBox2.Text = product.LocalizedProperties[0].ProductTitle + " (使用您的 EA Play 会员资格，游戏可享最高9折优惠)";
                else if (description.Contains("Xbox Game Pass"))
                    groupBox2.Text = product.LocalizedProperties[0].ProductTitle + " (使用您的 Xbox Game Pass 会员资格，游戏可享最高8折优惠，附加内容最高9折优惠)";
                else
                    groupBox2.Text = product.LocalizedProperties[0].ProductTitle + " (" + description + ")";
            }
            else
                groupBox2.Text = product.LocalizedProperties[0].ProductTitle;
        }

        private void FormCompare_Load(object sender, EventArgs e)
        {
            LinkLabel1_LinkClicked(sender, null);
        }

        private void CheckBox_CheckedChanged(object? sender, EventArgs? e)
        {
            if (sender is CheckBox cb)
            {
                DataGridViewRow? dgvr = cb.Tag as DataGridViewRow;
                if (cb.Checked)
                    dataGridView1.Rows.Add(dgvr);
                else
                    dataGridView1.Rows.Remove(dgvr);
                dataGridView1.ClearSelection();
                groupBox1.Text = "选择商店 (" + dataGridView1.Rows.Count + ")";
            }
        }

        private void DataGridView1_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == -1 || e.RowIndex == -1) return;
            if (dataGridView1.Columns[e.ColumnIndex].Name != "Col_Purchase") return;
            DataGridViewRow dgvr = dataGridView1.Rows[e.RowIndex];
            Process.Start(new ProcessStartInfo("https://www.microsoft.com/" + dgvr.Cells["Col_Lang"].Value + "/p/_/" + this.productId) { UseShellExecute = true });
        }
        private void DataGridView2_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == -1 || e.RowIndex == -1) return;
            if (dataGridView1.Columns[e.ColumnIndex].Name != "Col_Purchase2") return;
            DataGridViewRow dgvr = dataGridView1.Rows[e.RowIndex];
            Process.Start(new ProcessStartInfo("ms-windows-store://pdp/?productid=" + this.productId) { UseShellExecute = true });
        }

        private void DataGridView1_RowPostPaint(object sender, DataGridViewRowPostPaintEventArgs e)
        {
            DataGridView dgv = (DataGridView)sender;
            Rectangle rectangle = new(e.RowBounds.Location.X, e.RowBounds.Location.Y, dgv.RowHeadersWidth - 1, e.RowBounds.Height);
            TextRenderer.DrawText(e.Graphics, (e.RowIndex + 1).ToString(), dgv.RowHeadersDefaultCellStyle.Font, rectangle, dgv.RowHeadersDefaultCellStyle.ForeColor, TextFormatFlags.VerticalCenter | TextFormatFlags.Right);
        }

        private void LinkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs? e)
        {
            foreach (Control control in flowLayoutPanel1.Controls)
            {
                if (control is CheckBox cb)
                {
                    cb.Checked = true;
                }
            }
        }

        private void LinkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            foreach (Control control in flowLayoutPanel1.Controls)
            {
                if (control is CheckBox cb)
                {
                    cb.Checked = false;
                }
            }
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            List<DataGridViewRow> list = new();
            foreach (DataGridViewRow dgvr in dataGridView1.Rows)
            {
                if (!dgvr.Visible) continue;
                list.Add(dgvr);
            }
            if (list.Count >= 1)
            {
                button1.Enabled = false;
                ThreadPool.QueueUserWorkItem(delegate { Price(list); });
            }
        }

        private void FormCompare_FormClosing(object sender, FormClosingEventArgs e)
        {
            cts?.Cancel();
        }

        CancellationTokenSource? cts = null;
        private void Price(List<DataGridViewRow> list)
        {
            cts = new CancellationTokenSource();
            Task[] tasks = new Task[list.Count];
            for (int i = 0; i < list.Count; i++)
            {
                if (cts.IsCancellationRequested) break;
                int index = i;
                tasks[index] = new Task(() =>
                {
                    DataGridViewRow dgvr = list[index];
                    if (dgvr.Tag == null)
                    {
                        string url = "https://displaycatalog.mp.microsoft.com/v7.0/products?bigIds=" + productId + "&market=" + dgvr.Cells["Col_Code"].Value + "&languages=neutral&MS-CV=DGU1mcuYo0WMMp+F.1";
                        string html = ClassWeb.HttpResponseContent(url, "GET", null, null, null, 30000, null, cts.Token);
                        if (Regex.IsMatch(html, @"^{.+}$", RegexOptions.Singleline))
                        {
                            var json = JsonSerializer.Deserialize<ClassGame.Game>(html, Form1.jsOptions);
                            if (json != null && json.Products != null && json.Products.Count >= 1)
                            {
                                var product = json.Products[0];
                                if (product.LocalizedProperties != null)
                                {
                                    string CurrencyCode = product.DisplaySkuAvailabilities[0].Availabilities[0].OrderManagementData.Price.CurrencyCode.ToUpperInvariant();
                                    double MSRP = product.DisplaySkuAvailabilities[0].Availabilities[0].OrderManagementData.Price.MSRP;
                                    double ListPrice_1 = product.DisplaySkuAvailabilities[0].Availabilities[0].OrderManagementData.Price.ListPrice;
                                    double ListPrice_2 = product.DisplaySkuAvailabilities[0].Availabilities.Count >= 2 ? product.DisplaySkuAvailabilities[0].Availabilities[1].OrderManagementData.Price.ListPrice : 0;
                                    double WholesalePrice_1 = product.DisplaySkuAvailabilities[0].Availabilities[0].OrderManagementData.Price.WholesalePrice;
                                    double WholesalePrice_2 = product.DisplaySkuAvailabilities[0].Availabilities.Count >= 2 ? product.DisplaySkuAvailabilities[0].Availabilities[1].OrderManagementData.Price.WholesalePrice : 0;
                                    if (ListPrice_1 > MSRP) MSRP = ListPrice_1;
                                    if (!string.IsNullOrEmpty(CurrencyCode) && MSRP > 0 && CurrencyCode != "CNY" && !Form1.dicExchangeRate.ContainsKey(CurrencyCode))
                                    {
                                        ClassGame.ExchangeRate(CurrencyCode);
                                    }
                                    double ExchangeRate = Form1.dicExchangeRate.ContainsKey(CurrencyCode) ? Form1.dicExchangeRate[CurrencyCode] : 0;
                                    dgvr.Tag = true;
                                    if (MSRP > 0)
                                    {
                                        dgvr.Cells["Col_CurrencyCode"].Value = CurrencyCode;
                                        dgvr.Cells["Col_MSRP"].Value = MSRP;
                                        if (ListPrice_1 > 0 && ListPrice_1 != MSRP)
                                        {
                                            dgvr.Cells["Col_ListPrice_1"].Value = ListPrice_1;
                                            discount_ListPrice_1 = true;
                                        }
                                        if (ListPrice_2 > 0 && ListPrice_2 < ListPrice_1 && ListPrice_2 != MSRP)
                                        {
                                            dgvr.Cells["Col_ListPrice_2"].Value = ListPrice_2;
                                            discount_ListPrice_2 = true;
                                            if (string.IsNullOrEmpty(this.member))
                                                this.member = (product.DisplaySkuAvailabilities[0].Availabilities[1].Properties.MerchandisingTags != null && product.DisplaySkuAvailabilities[0].Availabilities[1].Properties.MerchandisingTags[0] == "LegacyDiscountEAAccess") ? "EA Play" : "会员";
                                        }
                                        if (WholesalePrice_1 > 0)
                                        {
                                            dgvr.Cells["Col_WholesalePrice_1"].Value = WholesalePrice_1;
                                            discount_WholesalePrice_1 = true;
                                            if (WholesalePrice_2 > 0 && WholesalePrice_2 < WholesalePrice_1)
                                            {
                                                dgvr.Cells["Col_WholesalePrice_2"].Value = WholesalePrice_2;
                                                discount_WholesalePrice_2 = true;
                                            }
                                        }
                                        if (ExchangeRate > 0)
                                        {
                                            if (ListPrice_2 > 0 && ListPrice_2 < ListPrice_1 && ListPrice_2 != MSRP)
                                                dgvr.Cells["Col_CNY"].Value = ListPrice_2 * ExchangeRate;
                                            else if (ListPrice_1 > 0 && ListPrice_1 != MSRP)
                                                dgvr.Cells["Col_CNY"].Value = ListPrice_1 * ExchangeRate;
                                            else
                                                dgvr.Cells["Col_CNY"].Value = MSRP * ExchangeRate;
                                            dgvr.Cells["Col_CNYExchangeRate"].Value = ExchangeRate;
                                        }
                                        else if (CurrencyCode == "CNY")
                                        {
                                            if (ListPrice_2 > 0 && ListPrice_2 < ListPrice_1 && ListPrice_2 != MSRP)
                                                dgvr.Cells["Col_CNY"].Value = ListPrice_2;
                                            else if (ListPrice_1 > 0 && ListPrice_1 != MSRP)
                                                dgvr.Cells["Col_CNY"].Value = ListPrice_1;
                                            else
                                                dgvr.Cells["Col_CNY"].Value = MSRP;
                                        }
                                    }
                                    else dgvr.Cells["Col_CurrencyCode"].Value = "不可用";
                                }
                            }
                        }
                    }
                    else if (dgvr.Cells["Col_CNYExchangeRate"].Value == null && dgvr.Cells["Col_MSRP"].Value != null && dgvr.Cells["Col_CurrencyCode"].Value.ToString() != "CNY")
                    {
                        string CurrencyCode = dgvr.Cells["Col_CurrencyCode"].Value.ToString() ?? string.Empty;
                        double MSRP = Convert.ToDouble(dgvr.Cells["Col_MSRP"].Value);
                        if (MSRP > 0)
                        {
                            if (!string.IsNullOrEmpty(CurrencyCode) && MSRP > 0 && CurrencyCode != "CNY" && !Form1.dicExchangeRate.ContainsKey(CurrencyCode))
                            {
                                ClassGame.ExchangeRate(CurrencyCode);
                            }
                            double ExchangeRate = Form1.dicExchangeRate.ContainsKey(CurrencyCode) ? Form1.dicExchangeRate[CurrencyCode] : 0;
                            if (ExchangeRate > 0)
                            {
                                double ListPrice_1 = Convert.ToDouble(dgvr.Cells["Col_ListPrice_1"].Value);
                                double ListPrice_2 = Convert.ToDouble(dgvr.Cells["Col_ListPrice_2"].Value);
                                if (ListPrice_2 > 0 && ListPrice_2 < ListPrice_1 && ListPrice_2 != MSRP)
                                    dgvr.Cells["Col_CNY"].Value = ListPrice_2 * ExchangeRate;
                                else if (ListPrice_1 > 0 && ListPrice_1 != MSRP)
                                    dgvr.Cells["Col_CNY"].Value = ListPrice_1 * ExchangeRate;
                                else
                                    dgvr.Cells["Col_CNY"].Value = MSRP * ExchangeRate;
                                dgvr.Cells["Col_CNYExchangeRate"].Value = ExchangeRate;
                            }
                        }
                    }
                }, cts.Token);
                tasks[index].Start();
            }
            try
            {
                Task.WaitAll(tasks, cts.Token);
            }
            catch { };
            if (cts.IsCancellationRequested) return;
            this.Invoke(new Action(() =>
            {
                List<DataGridViewRow> lsDgvr = new();
                for (int i = dataGridView1.Rows.Count - 1; i >= 0; i--)
                {
                    if (dataGridView1.Rows[i].Cells["Col_CNY"].Value == null)
                    {
                        lsDgvr.Add(dataGridView1.Rows[i]);
                        dataGridView1.Rows.RemoveAt(i);
                    }
                }
                dataGridView1.Sort(dataGridView1.Columns["Col_CNY"], ListSortDirection.Ascending);
                if (lsDgvr.Count >= 1)
                {
                    lsDgvr.Reverse();
                    dataGridView1.Rows.AddRange(lsDgvr.ToArray());
                }
                if (dataGridView1.Rows[0].Visible)
                {
                    dataGridView1.Rows[0].Cells["Col_Store"].Selected = true;
                }
                dataGridView1.Columns["Col_ListPrice_2"].HeaderText = this.member + "折扣";
                dataGridView1.Columns["Col_ListPrice_1"].Visible = discount_ListPrice_1;
                dataGridView1.Columns["Col_ListPrice_2"].Visible = discount_ListPrice_2;
                dataGridView1.Columns["Col_WholesalePrice_1"].Visible = discount_WholesalePrice_1;
                dataGridView1.Columns["Col_WholesalePrice_2"].Visible = discount_WholesalePrice_2;
                button1.Enabled = true;
            }));
            cts = null;
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }
    }
}
