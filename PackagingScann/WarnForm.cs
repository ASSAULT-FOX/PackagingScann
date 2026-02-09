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
    public partial class WarnForm : Form
    {
        bool isPasswordRequired = false;
        bool isPasswordAccepted = false;
        string encPassword;
        public WarnForm(String titleText,String okText,String canelText, bool isPasswordRequired = false, string encPassword = "")
        {
            InitializeComponent();
            label1.Text = titleText;
            button1.Text = okText;
            button2.Text = canelText;

            this.isPasswordRequired = isPasswordRequired;
            this.encPassword = encPassword;

            if (this.isPasswordRequired)
            {
                lockDialog();
            }
            else
            {
                pPassword.Visible = false;
            }
        }
        public WarnForm(String titleText)
        {
            InitializeComponent();
            label1.Text = titleText;
        }
        public WarnForm(String titleText, bool isPasswordRequired = false, string encPassword = "")
        {
            InitializeComponent();
            label1.Text = titleText;

            this.isPasswordRequired = isPasswordRequired;
            this.encPassword = encPassword;

            if (this.isPasswordRequired)
            {
                lockDialog();
            }
            else
            {
                pPassword.Visible = false;
            }
        }
        private void button1_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
        protected override bool ProcessDialogKey(Keys keyData) //需要密码且密码正确时，才允许用按键关闭
        {
            if (isPasswordRequired && isPasswordAccepted)
            {
                return base.ProcessDialogKey(keyData);
            }
            else if (!isPasswordRequired)
            {
                return base.ProcessDialogKey(keyData);
            }
            else
            {
                return false;
            }
        }
        private void lockDialog()
        {
            button1.Visible = false;
            button2.Visible = false;
            pPassword.Left = button1.Left;
            pPassword.Top = button1.Top;
            pPassword.Height = button1.Height;
            pPassword.Width = button2.Left + button2.Width - button1.Left;
        }
        private void unlockDialog()
        {
            pPassword.Visible = false;
            button1.Visible = true;
            button2.Visible = true;
        }
        private void unlockDialog(string pwd)
        {
            if(string.IsNullOrEmpty(pwd))
            {
                isPasswordAccepted = false;
                MessageBox.Show("密码不可为空！");
                return;
            }

            string encPM = CryptInfoNameSpace.CryptInfoNameSpace.Encrypt(pwd);
            if(encPM != encPassword)
            {
                isPasswordAccepted = false;
                MessageBox.Show("密码不正确！");
                return;
            }

            isPasswordAccepted = true;
            unlockDialog();
        }

        private void bEnterPass_Click(object sender, EventArgs e)
        {
            unlockDialog(tbPassword.Text);
        }

        private void WarnForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            //解锁前，不允许关闭对话框
            if (isPasswordRequired && !isPasswordAccepted)
            {
                DialogResult = DialogResult.None;
            }
        }

        private void tbPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.KeyCode == Keys.Enter)
            {
                unlockDialog(tbPassword.Text);
            }
        }
    }
}
