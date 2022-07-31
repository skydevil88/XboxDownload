using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;

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
            List<Games> games = new List<Games>
            {
                new Games("Kinect 体育竞技", "国语配音", "brhsml8030zn", "http://dlassets.xboxlive.cn/public/content/fc400a87-6790-46ff-971d-9a583b3b3056/d00bbc1d-5b18-4c34-8461-a6aee9a36f9f/1.10.239.0.d0a23582-fede-439d-bdd0-2b2350a173cf/epochexe_1.10.239.0_x64__zjr0dfhgjwvde"),
                new Games("埃克朗守卫者", "", "bxb772lkh72z", "http://assets1.xboxlive.cn/11/3ccfa22c-9f64-46aa-9f0e-3179d0f8ec2a/4c81b24f-3735-48d4-949d-eaf62ea517af/1.0.0.1.5c787d8f-19cb-4220-a596-13954c28155f/F318D353-AED8-4EB1-B6A5-64576DA6D7FC_1.0.0.1_x64_CN_tw1pm6pjg7wkw"),
                new Games("超神跑者", "", "c3pb0bp0g04l", "http://assets1.xboxlive.cn/10/481aea68-6d91-4d6e-a17d-c94794a4b097/03d07f87-a10a-4c46-9be2-d8fcd9844b21/1.9.2.6.368ed026-9d61-4c98-a1dc-207cddfcc602/DoubleDutchGames.SpeedRunners_1.9.2.6_neutral_CN_663nrax0tz2d6"),
                new Games("大蛇无双2 终极版", "国服简中", "bs96nlsxq6zw", "http://assets1.xboxlive.cn/5/9bc865b8-f36d-4e62-b1dc-b3eace95ebb7/8a657f9c-8fc6-4d95-8aa1-f052b12ae22d/1.1.5.0.e732cfd0-1f05-42af-8b5d-1a817ba06b76/OROCHI2UltimateCN_1.1.5.0_x64__zph8pnx224h38"),
                new Games("动物园大亨", "只能在国服显示中文", "brwjs8p512vf", "http://dlassets.xboxlive.cn/public/content/0d1608fb-6778-4677-a347-e14eced0a0a6/dc0b0525-21f0-4b43-a00e-bfd0abc27b25/1.0.1502.50616.5bd00a39-533f-4c1a-85af-8689407951c2/MSTycoon.final_1.0.1502.50616_x64__fnyef6rby9x8m"),
                new Games("飞速骑行", "", "c5d7449r6sdr", "http://assets1.xboxlive.cn/9/c4019ca4-f894-410e-acc8-e0966a55b773/f21c9f4e-5cda-465a-af62-57a03189668e/1.0.0.0.96f1f474-764a-45bd-a9d0-bc968a10f85c/RideCN_1.0.0.0_x64__g5fme4n6j55tr"),
                new Games("光之子", "国语配音", "bq9q620nc614", "http://dlassets.xboxlive.cn/public/content/77d0d59a-34b7-4482-a1c7-c0abbed17de2/fe238acf-b298-408c-94c5-97c921640c02/1.0.0.1.7c96decd-b0bf-47e0-ab50-c593d2c2983a/ChildOfLight-CH_1.0.0.1_x64__b6krnev7r9sf8"),
                new Games("极限竞速5", "国语配音", "bqlk685tm311", "http://assets1.xboxlive.cn/14/bbdf448c-0c0f-40cf-bf56-a226e396deb0/814a20e5-8e33-4e54-bb88-bd666837710e/1.0.0.25.ae996efe-4af8-4d70-8370-d7b210ced6ca/Forza_1.0.0.25_x64__8wekyb3d8bbwe"),
                new Games("雷曼传奇", "国语配音", "c26k4dvgr45b", "http://assets1.xboxlive.cn/2/2c2ad744-e3cb-4745-b040-af74e152d858/76c9a201-ff14-4091-b009-3b7cbcd5b4a9/1.0.0.2.2b4add40-4969-4029-b7da-84299b95dc7c/RaymanLegendsCN27BDEA18_1.0.0.2_x64__b6krnev7r9sf8"),
                new Games("麦克斯：兄弟魔咒", "国语配音", "c0sfcf4pbrsz", "http://dlassets.xboxlive.cn/public/content/1d6640d3-3441-42bd-bffd-953d7d09ff5c/1e2131e1-299c-4df1-bdb6-84a38e07ea9f/1.5.0.0.6bd5e6cb-7547-4c90-a84c-ee06ba0bdf5b/Microsoft.Max_1.5.0.0_neutral__ph1m9x8skttmg"),
                new Games("明星高尔夫", "国语配音", "bnq94hh98ztp", "http://dlassets.xboxlive.cn/public/content/19ebfc97-9d8d-4cd7-8493-4ec727e8de46/26c330df-ce8a-4ee3-8ab9-ee99d7c40add/1.4.3.0.d2a5120c-72d2-4465-bf04-c1bd318dac70/PowerstarGolf_1.4.3.0_x64__zjr0dfhgjwvde"),
                new Games("摩托世界大奖赛2015", "", "bsh5fpmr3gd8", "http://assets1.xboxlive.cn/Z/6afbe86f-3a55-41f3-90fd-d7772f9703fc/a35655f7-004e-4245-b957-5836f32fda60/1.0.0.0.c3b05501-7031-425b-88a4-5bd7f0659bac/MotoGP15CN_1.0.0.0_x64__g5fme4n6j55tr"),
                new Games("桥", "", "c2p1cf27vvdf", "http://assets1.xboxlive.cn/Z/eb201b49-a0ad-4628-8ac1-91bdc644d1ea/cc73486d-c2a3-4c86-b843-5aa58f41a8ce/1.0.0.8.b55fe985-a11d-43e6-8df5-2310559902ed/QAG.TheBridgeCN_1.0.0.8_neutral__mekwkj6wfk9vj"),
                new Games("塞巴斯蒂安拉力赛：进化", "", "br5bk5pnzkvh", "http://assets1.xboxlive.cn/6/ebd4ff27-d348-4413-b255-0b3964ac0426/e21a722f-bb59-4400-bc8a-0fe56a3fbb03/1.0.0.2.b336821f-1bf9-4609-9ba8-63e24354528e/SLRCN_1.0.0.2_x64__g5fme4n6j55tr"),
                new Games("水果忍者：体感版2", "", "btkfdf4dhwrz", "http://assets1.xboxlive.cn/9/ea13c41d-cab3-4df2-b480-f4822b5c8842/4fe972c6-81c2-4299-9f28-58bd2a347969/1.0.24.0.e73d8d18-4411-44b7-8395-73324ac63708/FNK2_1.0.24.0_neutral__6rm40ky9yd5pg"),
                new Games("索尼克 力量", "", "bxk9z89s6rcx", "http://assets1.xboxlive.cn/3/1a01553f-81a1-4cf3-be9b-e16ef543ef62/59838b24-5548-4301-a1ea-f911aa8ff8e8/1.0.2.0.35b5058f-a7ac-48b4-8219-db475fcdbb5a/SONICFORCES-CN_1.0.2.0_x64__9sb0j41x6xs7p"),
                new Games("特技摩托：聚变", "", "bvcrsr6xsdw5", "http://assets1.xboxlive.cn/8/03550536-d1ed-4e83-8630-a80a853b7397/0119aa18-5425-4e8f-83ef-10673e679dc9/1.0.9.8.77a6bd34-29a7-4e01-874c-f392229551b9/TrialsFusion_1.0.9.8_x64__b6krnev7r9sf8"),
                new Games("体感功夫", "", "bvk1lrw59l64", "http://assets1.xboxlive.cn/8/8a7e15c4-9fc2-4461-b4df-3ae115c123b2/1d8ce2c2-981a-4705-9307-dd7ded8e0475/1.2.3126.0.4d660098-903d-47a2-b574-369699387f56/VAGC.KungFuCN_1.2.3126.0_neutral__tgrhcq0kdd47r"),
                new Games("体感节奏战", "", "bs1c1bs3ss0v", "http://assets1.xboxlive.cn/6/18866143-a591-4e70-b445-5a01645db477/de18ef9a-61f6-485a-a3fd-64e785b75d6c/1.2.951.0.02cd3147-6daf-40ba-b1da-0f151e314b2e/VAGC.BeatsplosionCN_1.2.951.0_neutral__tgrhcq0kdd47r"),
                new Games("体感碰碰球", "", "c4j8pcxk5xlq", "http://assets1.xboxlive.cn/1/943a8b0b-fdc1-4c55-a789-bf8b02219273/625844a1-72ed-403b-a424-82487447cec3/1.1.2602.0.4390b583-2118-4385-a7b9-5ef990c3e5b3/VAGC.BoomBallForKinectCN_1.1.2602.0_neutral__6e9vmp2r8f8s2"),
                new Games("体感碰碰球2", "", "c3ngpjhhwpw6", "http://assets1.xboxlive.cn/6/9ea6ef30-43a8-4ad5-81da-adc9e147eb2c/f33678c5-05c8-48c9-8e8a-49d71769f040/1.1.3423.0.7c63190a-d489-4723-b4ca-951600d16921/VAGC.BoomBall2CN_1.1.3423.0_neutral__6e9vmp2r8f8s2"),
                new Games("体感章鱼", "", "bsfzlnb9r9rx", "http://assets1.xboxlive.cn/4/14b767a5-6e20-4b19-becf-c9f19cd99152/c6b11dea-befa-4b44-adea-23cb0f5b9e57/1.2.1853.0.535a081c-3936-47ea-8bde-9af2adb8d978/VAGC.SquidHeroCN_1.2.1853.0_neutral__tgrhcq0kdd47r"),
                new Games("型可塑", "国语配音", "c1b6dl0t68q5", "http://dlassets.xboxlive.cn/public/content/1c4b6e60-b2e3-420c-a8a8-540fb14c9286/22f22996-d089-4cc2-9919-3b0ef9fa783f/1.0.0.6.ec5e1d6e-4d07-41d0-8312-66bf5bcd7815/SHPUPCH446612E0_1.0.0.6_x64__zjr0dfhgjwvde"),
                new Games("真•三国无双７ 帝国", "国服简中", "bvg8190qslw6", "http://assets1.xboxlive.cn/4/878a830c-f90a-490a-8fd5-66141c2b0a78/2d456c8a-fc28-4873-a536-bacff51bba25/1.2.1.0.d9ff4e56-dc51-451c-8937-ff4efcbcb376/SM7EMPCN_1.2.1.0_x64__zph8pnx224h38"),
                new Games("最终幻想15", "国服简中", "c45d79qvkztp", "http://assets1.xboxlive.cn/6/1de847b6-ea75-4b82-9409-0a7c8c8b2fd7/faa27552-be23-44e9-aec3-9e2c383dfef4/1.3.1.0.4e600e32-28f3-4097-9fae-01152ef2d92e/ffxv-China_1.3.1.0_x64__0ygzwnwk70gy4")
            };
            games.Sort((x, y) => string.Compare(x.name, y.name));

            List<DataGridViewRow> list = new List<DataGridViewRow>();
            foreach (var game in games)
            {
                DataGridViewRow dgvr = new DataGridViewRow();
                dgvr.CreateCells(dgvGames);
                dgvr.Resizable = DataGridViewTriState.False;
                dgvr.Cells[0].Value = game.name;
                dgvr.Cells[1].Value = game.note;
                dgvr.Cells[2].Value = game.productId.ToUpperInvariant();
                dgvr.Cells[3].Value = game.url;
                list.Add(dgvr);
            }
            if (list.Count >= 1)
            {
                dgvGames.Rows.AddRange(list.ToArray());
                dgvGames.ClearSelection();
            }
        }

        public string productid = null;

        private void DgvGames_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex == -1 || dgvGames.SelectedRows.Count != 1) return;
            DataGridViewRow dgvr = dgvGames.SelectedRows[0];
            string productId = dgvr.Cells["Col_ProductId"].Value.ToString();
            if (Regex.IsMatch(productId, @"^[a-zA-Z0-9]{12}$"))
            {
                this.productid = dgvr.Cells["Col_ProductId"].Value.ToString();
                this.Close();
            }
        }

        private void DgvGames_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex < 0 || e.Button != MouseButtons.Right) return;
            DataGridViewRow dgvr = dgvGames.Rows[e.RowIndex];
            dgvr.Selected = true;
            contextMenuStrip1.Show(MousePosition.X, MousePosition.Y);
        }

        private void TsmiCopyURL_Click(object sender, EventArgs e)
        {
            Clipboard.SetDataObject(dgvGames.SelectedRows[0].Cells["Col_Url"].Value.ToString());
        }

        private void DgvGames_RowPostPaint(object sender, DataGridViewRowPostPaintEventArgs e)
        {
            DataGridView dgv = (DataGridView)sender;
            Rectangle rectangle = new Rectangle(e.RowBounds.Location.X, e.RowBounds.Location.Y, dgv.RowHeadersWidth - 1, e.RowBounds.Height);
            TextRenderer.DrawText(e.Graphics, (e.RowIndex + 1).ToString(), dgv.RowHeadersDefaultCellStyle.Font, rectangle, dgv.RowHeadersDefaultCellStyle.ForeColor, TextFormatFlags.VerticalCenter | TextFormatFlags.Right);
        }

        private void LinkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("https://tieba.baidu.com/p/7302023199");
        }

        class Games
        {
            public String name;
            public String note;
            public String productId;
            public String url;

            public Games(String name, String note, String productid, string url)
            {
                this.name = name;
                this.note = note;
                this.productId = productid;
                this.url = url;
            }
        }
    }
}
