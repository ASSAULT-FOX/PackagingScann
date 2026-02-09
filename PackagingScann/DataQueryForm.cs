using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using PackagingScann.Common;

namespace TransformerData
{
    public partial class DataQueryForm : Form
    {
        public DataQueryForm()
        {
            InitializeComponent();
            this.dataGridView1.DataSource = DalHelper.GetDataInfo();
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
            this.zidongjiazairadio.Checked = true;
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void DataQueryForm_Load(object sender, EventArgs e)
        {
            this.pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
        }

        private void QueryDatabutton_Click(object sender, EventArgs e)
        {
            try
            {
                string tiaomainfo = this.tiaomainfo.Text;
                string xianghao = this.xianghao.Text;
                string MESResult = this.MESResult.Text;
                DataTable dt = DalHelper.QueryDataInfo(tiaomainfo, xianghao, MESResult);
                this.dataGridView1.DataSource = dt;
            }
            catch (Exception ex)
            {
                DalHelper.WriteLogInfo(ex.Message);
                MessageBox.Show("查询失败，原因：" + ex.Message);
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (this.zidongjiazairadio.Checked)
            {
                this.dataGridView1.DataSource = DalHelper.GetDataInfo();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                FolderBrowserDialog dialog = new FolderBrowserDialog();
                dialog.Description = "请选择文件路径";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    string foldPath = dialog.SelectedPath + @"\" + DateTime.Now.ToString("yyyyMMddHHmmssfff") + ".csv";
                    string tiaomainfo = this.tiaomainfo.Text;
                    string xianghao = this.xianghao.Text;
                    string MESResult = this.MESResult.Text;
                    DataTable dt = DalHelper.QueryDataInfo(tiaomainfo, xianghao, MESResult);
                    CSVFileHelper.SaveCSV(dt, foldPath);
                    MessageBox.Show("数据导出成功，导出路径为：" + foldPath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("导出CSV文件出现错误：" + ex.Message);
            }
        }
    }
}
