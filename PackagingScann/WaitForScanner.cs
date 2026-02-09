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
    public partial class WaitForScanner : Form
    {
        private Timer timer;
        private int elapsedTime;
        public string ScannerText { get; set; }
        public WaitForScanner(String Title)
        {   
            InitializeComponent();
            label1.Text = Title;
            textBox1.Focus();
        }
        private void WaitForScanner_Load(object sender, EventArgs e)
        {
            timer = new Timer();
            
            timer.Interval = 1000; // 1秒间隔
            timer.Tick += Timer_Tick;
            timer.Start();
        }
        private void Timer_Tick(object sender, EventArgs e)
        {
            elapsedTime += 1;
            label2.Text = "等待时间：" + elapsedTime.ToString() + "秒";
        }

        private void WaitForScanner_FormClosing(object sender, FormClosingEventArgs e)
        {
            if(string.IsNullOrEmpty(textBox1.Text))
            {
                // 阻止窗口关闭
                e.Cancel = true;
            }
            else
            {
                timer.Stop(); // 停止计时器
                timer.Dispose();
            }
        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyValue == 13)
            {
                ScannerText = textBox1.Text;
                e.SuppressKeyPress = true; // 阻止按键声音
                DialogResult = DialogResult.OK;
            }
        }

        private void textBox1_Leave(object sender, EventArgs e)
        {
            textBox1.Focus();
        }
    }
}
