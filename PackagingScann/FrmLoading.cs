using Cognex.VisionPro;
using Cognex.VisionPro.ToolBlock;
using HVisionC;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace TransformerData
{
    public partial class FrmLoading : Form
    {
        public FrmLoading()
        {
            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterScreen;
            Timer timer = new Timer();
            timer.Tick += new EventHandler(Timer_Tick); // 订阅Tick事件
            timer.Interval = 3000; // 设置定时器间隔为3秒
            timer.Start(); // 启动定时器
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            label1.Text = "请选择工作项目";
            button1.Visible = true;
            button2.Visible = true;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            HUAWEIForm huaweiForm = new HUAWEIForm();
            huaweiForm.Show();
            this.Hide();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            HonorForm honorForm = new HonorForm();
            honorForm.Show();
            this.Hide();
        }
    }
}
