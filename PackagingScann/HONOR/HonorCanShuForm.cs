using PackagingScann.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using TransformerData;

namespace PackagingScann
{
    public partial class HonorCanShuForm : Form
    {
        public const string defaultAPPass = "123456";
        public HonorCanShuForm()
        {
            InitializeComponent();
        }

        PackagingScann.ServiceReference1.DataServiceSoapClient ws = new PackagingScann.ServiceReference1.DataServiceSoapClient("DataServiceSoap"); //WebService接口
        private void HonorCanShuForm_Load(object sender, EventArgs e)
        {
            #region 初始化MES参数
            this.textBox1.Text = ParameterSetClass.QueryHonorParameterInfo("工号","MES");
            this.textBox6.Text = ParameterSetClass.QueryHonorParameterInfo("站点", "MES");
            this.textBox2.Text = ParameterSetClass.QueryHonorParameterInfo("站点序号", "MES");
            this.textBox7.Text = ParameterSetClass.QueryHonorParameterInfo("扫描序号", "MES");
            this.textBox3.Text = ParameterSetClass.QueryHonorParameterInfo("电脑主机", "MES");
            this.textBox8.Text = ParameterSetClass.QueryHonorParameterInfo("线体", "MES");
            this.textBox4.Text = ParameterSetClass.QueryHonorParameterInfo("工单号", "MES");
            this.textBox9.Text = ParameterSetClass.QueryHonorParameterInfo("料号", "MES");
            this.textBox5.Text = ParameterSetClass.QueryHonorParameterInfo("条码样式", "MES");
            this.comboBox1.Text = ParameterSetClass.QueryHonorParameterInfo("是否上传MES", "MES");
            this.comboBox2.Text = ParameterSetClass.QueryHonorParameterInfo("外箱是否重工", "MES");
            this.comboBox3.Text = ParameterSetClass.QueryHonorParameterInfo("产品是否重工", "MES");
            this.textBox11.Text = ParameterSetClass.QueryHonorParameterInfo("箱号", "MES");
            this.comboBox10.Text = ParameterSetClass.QueryHonorParameterInfo("是否箱包装", "MES");
            this.textBox13.Text = ParameterSetClass.QueryHonorParameterInfo("当前工单总数量", "MES");
            this.textBox14.Text = ParameterSetClass.QueryHonorParameterInfo("当前工单扫描箱数", "MES");
            this.textBox16.Text = ParameterSetClass.QueryHonorParameterInfo("当前工单每箱数量", "MES");
            this.textBox15.Text = ParameterSetClass.QueryHonorParameterInfo("当前箱已扫描数量", "MES");

            this.DATATime.Format = DateTimePickerFormat.Custom;
            this.DATATime.CustomFormat = "yyyy年M月d日";
            DateTime dt = System.DateTime.ParseExact(ParameterSetClass.QueryHonorParameterInfo("工单日期", "MES"), "yyyy-MM-dd", CultureInfo.InvariantCulture);
            this.DATATime.Value = dt;

            this.comboBox11.Text = ParameterSetClass.QueryHonorParameterInfo("产品是否维修品", "MES");
            #endregion

            #region 初始化系统参数
            this.textBox10.Text = CryptInfoNameSpace.CryptInfoNameSpace.Decrypt(ParameterSetClass.QueryHonorParameterInfo("管理员密码", "系统"));
            this.textBox19.Text = (ParameterSetClass.QueryHonorParameterInfo("TaskTime", "系统"));
            this.textBox20.Text = (ParameterSetClass.QueryHonorParameterInfo("TaskCycle", "系统"));

            //yangk 240819 报警处理密码相关
            string encAPPass = ParameterSetClass.QueryHonorParameterInfo("报警处理密码", "系统");
            if (string.IsNullOrEmpty(encAPPass))
            {
                this.tbAlarmProcessPwd.Text = defaultAPPass;
            }
            else
            {
                this.tbAlarmProcessPwd.Text = CryptInfoNameSpace.CryptInfoNameSpace.Decrypt(encAPPass);
            }

            string sisAlarmProcessPwdEnabled = ParameterSetClass.QueryHonorParameterInfo("允许报警处理密码", "系统");
            if (sisAlarmProcessPwdEnabled == "Y")
            {
                cbEnableAlarmProcessPwd.Checked = true;
            }
            else
            {
                cbEnableAlarmProcessPwd.Checked = false;
            }
            #endregion
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        #region 保存MES参数

        #region 保存工号
        private void button1_Click(object sender, EventArgs e)
        {
            this.button1.Enabled = false;
            try
            {
                if (!string.IsNullOrEmpty(this.textBox1.Text))
                {
                    ParameterSetClass.UpdateHonorData("工号", this.textBox1.Text.Trim(), "MES");
                    MessageBox.Show("参数保存成功");
                }
                else
                {
                    MessageBox.Show("值不可为空！");
                }
                this.button1.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("参数保存失败！原因：" + ex.Message);
                this.button1.Enabled = true;
            }
        }
        #endregion

        #region 保存站点
        private void button6_Click(object sender, EventArgs e)
        {
            this.button6.Enabled = false;
            try
            {
                if (!string.IsNullOrEmpty(this.textBox6.Text))
                {
                    ParameterSetClass.UpdateHonorData("站点", this.textBox6.Text.Trim(), "MES");
                    MessageBox.Show("参数保存成功");
                }
                else
                {
                    MessageBox.Show("值不可为空！");
                }
                this.button6.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("参数保存失败！原因：" + ex.Message);
                this.button6.Enabled = true;
            }
        }
        #endregion

        #region 保存站点序号
        private void button2_Click(object sender, EventArgs e)
        {
            this.button2.Enabled = false;
            try
            {
                if (!string.IsNullOrEmpty(this.textBox2.Text))
                {
                    ParameterSetClass.UpdateHonorData("站点序号", this.textBox2.Text.Trim(), "MES");
                    MessageBox.Show("参数保存成功");
                }
                else
                {
                    MessageBox.Show("值不可为空！");
                }
                this.button2.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("参数保存失败！原因：" + ex.Message);
                this.button2.Enabled = true;
            }
        }
        #endregion

        #region 保存电脑主机
        private void button3_Click(object sender, EventArgs e)
        {
            this.button3.Enabled = false;
            try
            {
                if (!string.IsNullOrEmpty(this.textBox3.Text))
                {
                    ParameterSetClass.UpdateHonorData("电脑主机", this.textBox3.Text.Trim(), "MES");
                    MessageBox.Show("参数保存成功");
                }
                else
                {
                    MessageBox.Show("值不可为空！");
                }
                this.button3.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("参数保存失败！原因：" + ex.Message);
                this.button3.Enabled = true;
            }
        }
        #endregion

        #region 保存线体
        //private void button8_Click(object sender, EventArgs e)
        //{
        //    this.button8.Enabled = false;
        //    try
        //    {
        //        if (!string.IsNullOrEmpty(this.textBox8.Text))
        //        {
        //            ParameterSetClass.UpdateData("线体", this.textBox8.Text.Trim(), "MES");
        //            MessageBox.Show("参数保存成功");
        //        }
        //        else
        //        {
        //            MessageBox.Show("值不可为空！");
        //        }
        //        this.button8.Enabled = true;
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show("参数保存失败！原因：" + ex.Message);
        //        this.button8.Enabled = true;
        //    }
        //}
        #endregion

        #region 保存工单号
        private void button4_Click(object sender, EventArgs e)
        {
            this.button4.Enabled = false;
            try
            {
                if (!string.IsNullOrEmpty(this.textBox4.Text))
                {
                    string str = ws.GetAutoATEGetPackData_New(this.textBox4.Text.Trim(), this.DATATime.Value.ToString("yyyy-MM-dd"));
                    if (!str.StartsWith("NG"))
                    {
                        string[] condition = { ";" };
                        string[] result = str.Split(condition, StringSplitOptions.None);
                        if (result.Length > 0)
                        {
                            string liaohao = result[0].ToString().Trim();
                            string yangshi = result[1].ToString().Trim();
                            string gongdanshuliang = result[2].ToString().Trim();
                            string xiangshu = result[3].ToString().Trim();
                            string yingzhuang = result[4].ToString().Trim();
                            string xianti = result[5].ToString().Trim();
                            ParameterSetClass.UpdateHonorData("工单号", this.textBox4.Text.Trim(), "MES");
                            ParameterSetClass.UpdateHonorData("料号", liaohao, "MES");
                            ParameterSetClass.UpdateHonorData("条码样式", yangshi, "MES");
                            ParameterSetClass.UpdateHonorData("当前工单总数量", gongdanshuliang, "MES");
                            ParameterSetClass.UpdateHonorData("当前工单扫描箱数", xiangshu, "MES");
                            ParameterSetClass.UpdateHonorData("当前工单每箱数量", yingzhuang, "MES");
                            ParameterSetClass.UpdateHonorData("线体", xianti, "MES");
                            this.textBox8.Text = xianti;
                            this.textBox9.Text = liaohao;
                            this.textBox5.Text = yangshi;
                            this.textBox13.Text = gongdanshuliang;
                            this.textBox14.Text = xiangshu;
                            this.textBox16.Text = yingzhuang;
                            HonorForm f = (HonorForm)this.Owner;
                            f.sttryy();
                            MessageBox.Show("参数保存成功");
                        }
                        else {
                            MessageBox.Show("MES接口返回参数不完整，MES接口返回：" + str);
                            return;                        
                        }
                    }
                    else
                    {
                        MessageBox.Show("未查询到此工单号的料号信息和条码样式，请检查工单号是否正确！！！");
                        return;
                    }
                }
                else
                {
                    MessageBox.Show("值不可为空！");
                }
                this.button4.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("参数保存失败！原因：" + ex.Message);
                this.button4.Enabled = true;
            }
        }
        #endregion

        #region 保存扫描序号
        private void button7_Click(object sender, EventArgs e)
        {
            this.button7.Enabled = false;
            try
            {
                if (!string.IsNullOrEmpty(this.textBox7.Text))
                {
                    ParameterSetClass.UpdateHonorData("扫描序号", this.textBox7.Text.Trim(), "MES");
                    MessageBox.Show("参数保存成功");
                }
                else
                {
                    MessageBox.Show("值不可为空！");
                }
                this.button7.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("参数保存失败！原因：" + ex.Message);
                this.button7.Enabled = true;
            }
        }
        #endregion

        #region 保存是否上传MES
        private void button11_Click(object sender, EventArgs e)
        {
            this.button11.Enabled = false;
            try
            {
                if (!string.IsNullOrEmpty(this.comboBox1.Text))
                {
                    ParameterSetClass.UpdateHonorData("是否上传MES", this.comboBox1.Text.Trim(), "MES");
                    MessageBox.Show("参数保存成功");
                }
                else
                {
                    MessageBox.Show("值不可为空！");
                }
                this.button11.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("参数保存失败！原因：" + ex.Message);
                this.button11.Enabled = true;
            }
        }
        #endregion

        #region 保存外箱是否重工
        private void button12_Click(object sender, EventArgs e)
        {
            this.button12.Enabled = false;
            try
            {
                if (!string.IsNullOrEmpty(this.comboBox2.Text))
                {
                    ParameterSetClass.UpdateHonorData("外箱是否重工", this.comboBox2.Text.Trim(), "MES");
                    MessageBox.Show("参数保存成功");
                }
                else
                {
                    MessageBox.Show("值不可为空！");
                }
                this.button12.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("参数保存失败！原因：" + ex.Message);
                this.button12.Enabled = true;
            }
        }
        #endregion

        #region 保存产品是否重工
        private void button13_Click(object sender, EventArgs e)
        {
            this.button13.Enabled = false;
            try
            {
                if (!string.IsNullOrEmpty(this.comboBox3.Text))
                {
                    ParameterSetClass.UpdateHonorData("产品是否重工", this.comboBox3.Text.Trim(), "MES");
                    MessageBox.Show("参数保存成功");
                }
                else
                {
                    MessageBox.Show("值不可为空！");
                }
                this.button13.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("参数保存失败！原因：" + ex.Message);
                this.button13.Enabled = true;
            }
        }
        #endregion

        #region 保存是否箱包装
        private void button15_Click(object sender, EventArgs e)
        {
            this.button15.Enabled = false;
            try
            {
                if (!string.IsNullOrEmpty(this.comboBox10.Text))
                {
                    ParameterSetClass.UpdateHonorData("是否箱包装", this.comboBox10.Text.Trim(), "MES");
                    MessageBox.Show("参数保存成功");
                }
                else
                {
                    MessageBox.Show("值不可为空！");
                }
                this.button15.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("参数保存失败！原因：" + ex.Message);
                this.button15.Enabled = true;
            }
        }
        #endregion

        #region  保存工单日期
        private void DATATime_ValueChanged(object sender, EventArgs e)
        {
            ParameterSetClass.UpdateHonorData("工单日期", this.DATATime.Value.ToString("yyyy-MM-dd"), "MES");
        }
        #endregion

        #region  保存产品是否维修品
        private void buttonRepaired_Click(object sender, EventArgs e)
        {
            this.buttonRepaired.Enabled = false;
            try
            {
                if (!string.IsNullOrEmpty(this.comboBox11.Text))
                {
                    if(this.comboBox11.Text=="Y")
                    {
                        ParameterSetClass.UpdateHonorData("产品是否维修品", "Y", "MES");
                        MessageBox.Show("参数保存成功");
                    }

                    else
                    {
                        ParameterSetClass.UpdateHonorData("产品是否维修品", "N", "MES");
                        MessageBox.Show("参数保存成功");
                    }
                        
                }
                else
                {
                    MessageBox.Show("值不可为空！");
                }
                this.buttonRepaired.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("参数保存失败！原因：" + ex.Message);
                this.buttonRepaired.Enabled = true;
            }
        }
        #endregion

        #endregion

        #region 保存系统参数

        #region 保存管理员密码
        private void button10_Click(object sender, EventArgs e)
        {
            this.button10.Enabled = false;
            try
            {
                if (!string.IsNullOrEmpty(this.textBox10.Text))
                {
                    string Password = CryptInfoNameSpace.CryptInfoNameSpace.Encrypt(this.textBox10.Text);
                    ParameterSetClass.UpdateHonorData("管理员密码", Password, "系统");
                    MessageBox.Show("密码修改成功");
                }
                else
                {
                    MessageBox.Show("值不可为空！");
                }
                this.button10.Enabled = true;
            }
            catch (Exception ex)
            {
                this.button10.Enabled = true;
                MessageBox.Show("密码修改失败！原因：" + ex.Message);
            }
        }
        #endregion

        private void button16_Click(object sender, EventArgs e)
        {
            try
            {
                HonorForm f = (HonorForm)this.Owner;
                f.CodeByID.Clear();
                f.FailID.Clear();
                MessageBox.Show("清除成功！！！");
            }
            catch (Exception ex)
            {
                MessageBox.Show("程序出现错误：" + ex.Message);
            }

        }

        #endregion

        private void button9_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("是否把当前时间更新为定时任务基准时间,即以当前时间+周期时间为下一个任务执行时间。", "注意!!", MessageBoxButtons.OKCancel) == DialogResult.OK)
            {
                ParameterSetClass.UpdateHonorData("TaskTime", System.DateTime.Now.ToString(), "系统");
                this.textBox19.Text = (ParameterSetClass.QueryHonorParameterInfo("TaskTime", "系统"));
            }

        }
       
        private void button17_Click(object sender, EventArgs e)
        {
            this.button17.Enabled = false;
            try
            {
                if (!string.IsNullOrEmpty(this.textBox20.Text))
                {
                    ParameterSetClass.UpdateHonorData("TaskCycle", this.textBox20.Text, "系统");
                    MessageBox.Show("参数保存成功");
                }
                else
                {
                    MessageBox.Show("值不可为空！");
                }
                this.button17.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("参数保存失败！原因：" + ex.Message);
                this.button17.Enabled = true;
            }
        }

        #region 报警处理密码相关
        //yangk 240819
        private void bSaveAlarmProcessPwd_Click(object sender, EventArgs e)
        {
            try 
            {
                this.bSaveAlarmProcessPwd.Enabled = false;
                if (!string.IsNullOrEmpty(this.tbAlarmProcessPwd.Text))
                {
                    string Password = CryptInfoNameSpace.CryptInfoNameSpace.Encrypt(this.tbAlarmProcessPwd.Text);
                    ParameterSetClass.UpdateHonorData("报警处理密码", Password, "系统", true);
                }
                else
                {
                    MessageBox.Show(this, "值不能为空！", "参数画面", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                this.bSaveAlarmProcessPwd.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("报警处理密码修改失败！原因：" + ex.Message);
            }
            
            //string PM = Microsoft.VisualBasic.Interaction.InputBox("请输入旧的报警处理密码", "输入旧报警处理密码", "", -1, -1);
            //string encPM = CryptInfoNameSpace.CryptInfoNameSpace.Encrypt(PM);
            //string encAPPass = ParameterSetClass.QueryHonorParameterInfo("报警处理密码", "系统");
            //bool isPass;
            //if (string.IsNullOrEmpty(encAPPass) && encPM == CryptInfoNameSpace.CryptInfoNameSpace.Encrypt(defaultAPPass))
            //{
            //    isPass = true;
            //}
            //else if (encPM == encAPPass)
            //{
            //    isPass = true;
            //}
            //else
            //{
            //    isPass = false;
            //}
            //if (!isPass)
            //{
            //    MessageBox.Show(this, "旧密码错误，不允许报警处理密码修改！", "参数画面", MessageBoxButtons.OK, MessageBoxIcon.Error);
            //}
            //try
            //{
            //    if (!string.IsNullOrEmpty(this.tbAlarmProcessPwd.Text))
            //    {
            //        string Password = CryptInfoNameSpace.CryptInfoNameSpace.Encrypt(this.tbAlarmProcessPwd.Text);
            //        ParameterSetClass.UpdateHonorData("报警处理密码", Password, "系统", true);
            //    }
            //    else
            //    {
            //        MessageBox.Show(this, "值不能为空！", "参数画面", MessageBoxButtons.OK, MessageBoxIcon.Error);
            //    }
            //    this.bSaveAlarmProcessPwd.Enabled = true;
            //}
            //catch (Exception ex)
            //{
            //    this.bSaveAlarmProcessPwd.Enabled = true;
            //    MessageBox.Show("报警处理密码修改失败！原因：" + ex.Message);
            //}
        }

        private void cbEnableAlarmProcessPwd_Click(object sender, EventArgs e)
        {
            try 
            {
                cbEnableAlarmProcessPwd.Enabled = false;
                string sEnableAlarmProcessPwd = cbEnableAlarmProcessPwd.Checked ? "Y" : "N";
                ParameterSetClass.UpdateHonorData("允许报警处理密码", sEnableAlarmProcessPwd, "系统", true);
                cbEnableAlarmProcessPwd.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("报警处理是否启用修改失败！原因：" + ex.Message);
            }

            //string PM = Microsoft.VisualBasic.Interaction.InputBox("请输入报警处理密码", "输入报警处理密码", "", -1, -1);
            //string encPM = CryptInfoNameSpace.CryptInfoNameSpace.Encrypt(PM);
            //string encAPPass = ParameterSetClass.QueryHonorParameterInfo("报警处理密码", "系统");
            //bool isPass;
            //if (string.IsNullOrEmpty(encAPPass) && encPM == CryptInfoNameSpace.CryptInfoNameSpace.Encrypt(defaultAPPass))
            //{
            //    isPass = true;
            //}
            //else if (encPM == encAPPass)
            //{
            //    isPass = true;
            //}
            //else
            //{
            //    isPass = false;
            //}

            //if (isPass)
            //{
            //    string sEnableAlarmProcessPwd = cbEnableAlarmProcessPwd.Checked ? "Y" : "N";
            //    cbEnableAlarmProcessPwd.Enabled = false;
            //    try
            //    {
            //        ParameterSetClass.UpdateHonorData("允许报警处理密码", sEnableAlarmProcessPwd, "系统", true);
            //    }
            //    catch (Exception ex)
            //    {
            //        MessageBox.Show("报警处理是否启用修改失败！原因：" + ex.Message);
            //    }
            //    cbEnableAlarmProcessPwd.Enabled = true;
            //}
            //else
            //{
            //    cbEnableAlarmProcessPwd.Checked = !cbEnableAlarmProcessPwd.Checked;
            //    MessageBox.Show(this, "报警处理密码输入有误，修改此项失败", "参数画面", MessageBoxButtons.OK, MessageBoxIcon.Error);
            //    return;
            //}

        }
        private void button18_Click(object sender, EventArgs e)
        {
            bool enable = ParameterSetClass.QueryHonorParameterInfo("允许报警处理密码", "系统") == "Y" ? true : false;
            this.Invoke((MethodInvoker)delegate
            {
                WarnForm warnForm = new WarnForm("存在读码失败，请人工确认！！！\r\n可能错误：\r\n1.混料。条码不符\r\n2.条码脏污/缺码\r\n\r\n确认：视觉重新扫描.\r\n取消：人工补码", "视觉重扫", "人工补码", enable, ParameterSetClass.QueryHonorParameterInfo("报警处理密码", "系统"));
                DialogResult result = warnForm.ShowDialog();
            });
        }
        #endregion

        private void HonorCanShuForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            var honorForm = Application.OpenForms["HonorForm"] as HonorForm;
            HonorForm.WXBMessage(honorForm);  
        }

    }
}


