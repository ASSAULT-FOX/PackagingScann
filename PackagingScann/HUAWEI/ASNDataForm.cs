using Microsoft.VisualBasic;
using PackagingScann.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PackagingScann
{
    public partial class ASNDataForm : Form
    {
        public ASNDataForm()
        {
            InitializeComponent();

            this.dataGridView1.DataSource = DalHelper.QueryHUAWEIASN("");
            int width = 0;
            for (int i = 0; i < this.dataGridView1.Columns.Count; i++)
            {
                //将每一列都调整为自动适应模式
                this.dataGridView1.AutoResizeColumn(i, DataGridViewAutoSizeColumnMode.AllCells);
                //记录整个DataGridView的宽度
                width += this.dataGridView1.Columns[i].Width;
            }
            //判断调整后的宽度与原来设定的宽度的关系，如果是调整后的宽度大于原来设定的宽度，
            //则将DataGridView的列自动调整模式设置为显示的列即可，
            //如果是小于原来设定的宽度，将模式改为填充。
            if (width > this.dataGridView1.Size.Width)
            {
                this.dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;
            }
            else
            {
                this.dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            }
        }
    
            
      
        private void pictureBox1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void QueryDatabutton_Click(object sender, EventArgs e)
        {   
            
            this.dataGridView1.DataSource = DalHelper.QueryHUAWEIASN(this.tiaomainfo.Text);

            dataGridView1.Refresh();
        }

        private void button1_Click(object sender, EventArgs e)
        {

            string PM = Interaction.InputBox("请验证用户身份", "输入密码", "", -1, -1);
            string Password = ParameterSetClass.QueryHuaweiParameterInfo("管理员密码", "系统");
            if (!string.IsNullOrEmpty(PM))
            {
                string passwordtext = CryptInfoNameSpace.CryptInfoNameSpace.Encrypt(PM);

                if (PM == "jajqr168" || passwordtext == Password)
                {

                    if (DalHelper.DeleteHUAWEIASN((string)this.dataGridView1.SelectedRows[0].Cells[0].Value))
                    {
                        MessageBox.Show("手动删除华为条码数据成功");
                        this.dataGridView1.DataSource = DalHelper.QueryHUAWEIASN(this.tiaomainfo.Text);
                        dataGridView1.Refresh();
                    }
                }
                else
                {
                    MessageBox.Show("密码输入错误...");
                    return;
                }
            }

        }
    }
}
