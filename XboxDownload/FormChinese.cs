using System.Diagnostics;

namespace XboxDownload
{
    public partial class FormChinese : Form
    {
        public FormChinese()
        {
            InitializeComponent();

            if (Form1.dpixRatio > 1)
            {
                dgvGames.RowHeadersWidth = (int)(dgvGames.RowHeadersWidth * Form1.dpixRatio);
                foreach (DataGridViewColumn col in dgvGames.Columns)
                    col.Width = (int)(col.Width * Form1.dpixRatio);
            }
        }

        private void FormChinese_Load(object sender, EventArgs e)
        {
            List<Games> games = new()
            {
                new Games("Kinect 体育竞技", "国语配音", "brhsml8030zn"),
                new Games("埃克朗守卫者", "", "bxb772lkh72z"),
                new Games("超神跑者", "", "c3pb0bp0g04l"),
                new Games("大蛇无双2 终极版", "国服简中", "bs96nlsxq6zw"),
                new Games("动物园大亨", "只能在国服显示中文", "brwjs8p512vf"),
                new Games("飞速骑行", "", "c5d7449r6sdr"),
                new Games("光之子", "国语配音", "bq9q620nc614"),
                new Games("极限竞速5", "国语配音", "bqlk685tm311"),
                new Games("雷曼传奇", "国语配音", "c26k4dvgr45b"),
                new Games("麦克斯：兄弟魔咒", "国语配音", "c0sfcf4pbrsz"),
                new Games("明星高尔夫", "国语配音", "bnq94hh98ztp"),
                new Games("摩托世界大奖赛2015", "", "bsh5fpmr3gd8"),
                new Games("桥", "", "c2p1cf27vvdf"),
                new Games("塞巴斯蒂安拉力赛：进化", "", "br5bk5pnzkvh"),
                new Games("水果忍者：体感版2", "", "btkfdf4dhwrz"),
                new Games("索尼克 力量", "", "bxk9z89s6rcx"),
                new Games("特技摩托：聚变", "", "bvcrsr6xsdw5"),
                new Games("体感功夫", "", "bvk1lrw59l64"),
                new Games("体感节奏战", "", "bs1c1bs3ss0v"),
                new Games("体感碰碰球", "", "c4j8pcxk5xlq"),
                new Games("体感碰碰球2", "", "c3ngpjhhwpw6"),
                new Games("体感章鱼", "", "bsfzlnb9r9rx"),
                new Games("型可塑", "国语配音", "c1b6dl0t68q5"),
                new Games("真•三国无双７ 帝国", "国服简中", "bvg8190qslw6"),
                new Games("最终幻想15", "国服简中", "c45d79qvkztp")
            };
            //games.Sort((x, y) => string.Compare(x.name, y.name));

            List<DataGridViewRow> list = new();
            foreach (var game in games)
            {
                DataGridViewRow dgvr = new();
                dgvr.CreateCells(dgvGames);
                dgvr.Resizable = DataGridViewTriState.False;
                dgvr.Cells[0].Value = game.name;
                dgvr.Cells[1].Value = game.note;
                dgvr.Cells[2].Value = game.productId;
                list.Add(dgvr);
            }
            if (list.Count >= 1)
            {
                dgvGames.Rows.AddRange(list.ToArray());
                dgvGames.ClearSelection();
            }
        }

        public string? productid = null;
        private void DgvGames_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex == -1 || dgvGames.SelectedRows.Count != 1) return;
            DataGridViewRow dgvr = dgvGames.SelectedRows[0];
            this.productid = dgvr.Cells["Col_ProductId"].Value.ToString();
            this.Close();
        }

        private void DgvGames_RowPostPaint(object sender, DataGridViewRowPostPaintEventArgs e)
        {
            DataGridView dgv = (DataGridView)sender;
            Rectangle rectangle = new(e.RowBounds.Location.X, e.RowBounds.Location.Y, dgv.RowHeadersWidth - 1, e.RowBounds.Height);
            TextRenderer.DrawText(e.Graphics, (e.RowIndex + 1).ToString(), dgv.RowHeadersDefaultCellStyle.Font, rectangle, dgv.RowHeadersDefaultCellStyle.ForeColor, TextFormatFlags.VerticalCenter | TextFormatFlags.Right);
        }

        private void LinkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://tieba.baidu.com/p/7302023199") { UseShellExecute = true });
        }


        class Games
        {
            public String name;
            public String note;
            public String productId;

            public Games(String name, String note, String productid)
            {
                this.name = name;
                this.note = note;
                this.productId = productid;
            }
        }
    }
}
