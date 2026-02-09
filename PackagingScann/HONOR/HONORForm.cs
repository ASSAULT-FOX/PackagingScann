using CheckUSBAOI;
using Cognex.VisionPro;
using Cognex.VisionPro.Blob;
using Cognex.VisionPro.CalibFix;
using Cognex.VisionPro.Display;
using Cognex.VisionPro.ID;
using Cognex.VisionPro.ImageFile;
using Cognex.VisionPro.ImageProcessing;
using Cognex.VisionPro.ResultsAnalysis;
using Cognex.VisionPro.ToolBlock;
using HVisionC;
using Microsoft.VisualBasic;
using PackagingScann;
using PackagingScann.Common;
using Sunny.UI.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.ServiceModel.Channels;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TransformerData
{
    public partial class HonorForm : Form
    {
        private BackgroundWorker worker = new BackgroundWorker();
        Process objProcess = Process.GetCurrentProcess();
        string RenderImageSavePath = string.Empty;
        PackagingScann.ServiceReference1.DataServiceSoapClient ws = new PackagingScann.ServiceReference1.DataServiceSoapClient("DataServiceSoap"); //WebService接口
        public DateTime BoxDateHonor; //荣耀工单日期  用于请求产品条码格式 20240704
        public volatile int Work_Run_Case = 0;
        bool UseLight = SerialPort.GetPortNames().Any(port => port.Equals("COM3", StringComparison.OrdinalIgnoreCase));

        public Dictionary<int, String> CodeByID = new Dictionary<int, String>();
        public List<int> FailID = new List<int>();
        public Dictionary<int, double> IDAngle = new Dictionary<int, double>();

        Regex regex = new Regex(@"(?<=[:])\d+", RegexOptions.Compiled);

        public HonorForm()
        {
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Height = 1677;
            this.Width = 1103;
            InitializeComponent();
            ParameterSetClass.HonorTaskTimeCheck();

            //this.KeyPreview = true;

            // 注册事件处理程序
            //ScannerStatusAction += HandleScannerStatusUpdated;
        }

        private Thread WorkRun_thread;

        public static Cognex.VisionPro.ToolBlock.CogToolBlock ScanCode1_Block = new Cognex.VisionPro.ToolBlock.CogToolBlock();

        // 缓存按钮引用
        private readonly Dictionary<int, Button> _buttonCache = new Dictionary<int, Button>();

        /// <summary>
        /// 软件开启时执行方法
        /// </summary>
        private void HonorForm_Load(object sender, EventArgs e)
        {
            try
            {
                //检测是否重复打开程序窗体
                Process[] cur = Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName);
                if (cur.Length > 1)
                {
                    MessageBox.Show("检测到进程中已有相同软件打开,请勿重复打开软件！");
                    LogHelper.WriteLog("检测到进程中已有相同软件打开,请勿重复打开软件！");
                    objProcess.Kill();
                }

                //视觉图片保存路径
                RenderImageSavePath = Path.GetPathRoot(Assembly.GetExecutingAssembly().Location);
                RenderImageSavePath = Path.Combine(RenderImageSavePath, "视觉扫码图片");

                WXBMessage(this);

                //计时器启动
                this.timer2.Start();

                //注册键盘触发事件
                this.KeyDown += new KeyEventHandler(this.HonorForm_KeyDown);

                //检测是否存在COM3
                if (UseLight)
                {
                    //通过串口链接VC300的光源控制器
                    LightPort.Init("COM3", 115200);
                    LightPort.Open();

                    //关闭光源
                    byte[] OffLight1 = { 0xa5, 0x00, 0x01, 0x04, 0x64, 0x00, 0x02, 0x00, 0x01, 0x00, 0x00, 0x00 };
                    LightPort.WriteHex(OffLight1);
                    byte[] OffLight2 = { 0xa5, 0x00, 0x03, 0x04, 0x64, 0x00, 0x02, 0x00, 0x01, 0x00, 0x00, 0x00 };
                    LightPort.WriteHex(OffLight2);
                }

                //加载VPP
                string VPPName = ParameterSetClass.QueryHonorParameterInfo("物料编码", "视觉");
                string VPPFolder = Path.Combine(Application.StartupPath, "Vpp");
                if (VPPExists(VPPName, VPPFolder))
                {
                    ScanCode1_Block = CogSerializer.LoadObjectFromFile(Path.Combine(VPPFolder, $"{VPPName}.vpp")) as CogToolBlock;
                    comboBox1.DropDownStyle = ComboBoxStyle.DropDownList;
                    comboBox1.BackColor = Color.White;
                    comboBox1.Items.Add(VPPName);
                    comboBox1.SelectedItem = VPPName;
                }
                else
                {
                    this.label3.Text = $"{VPPName} VPP文件不存在！";
                    this.label3.ForeColor = Color.Red;
                }    

                WorkRun_thread = new Thread(new ThreadStart(WorkRun));
                WorkRun_thread.IsBackground = true;
                WorkRun_thread.Start();

                for (int i = 1; i <= 50; i++)
                {
                    // 一次性反射
                    var field = this.GetType().GetField($"BarCode{i}",
                        BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase);
                    _buttonCache[i] = (Button)field.GetValue(this);
                }

                worker.WorkerSupportsCancellation = true;
                worker.DoWork += new DoWorkEventHandler(FrmLoad);
                worker.RunWorkerAsync();
            }
            catch (Exception ex)
            {
                DalHelper.WriteLogInfo(ex.Message);
                this.BeginInvoke(new Action(() =>
                {
                    this.chengxujilu.AppendText($"【{DateTime.Now:HH:mm:ss:ffff}】程序初始化出现错误：{ex.Message}" + Environment.NewLine);
                }));
            }
        }

        /// <summary>
        /// 检测参数，以及上一箱是不是扫完
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FrmLoad(object sender, DoWorkEventArgs e)
        {
            try
            {
                string xiaohao = ParameterSetClass.QueryHonorParameterInfo("箱号", "MES");
                string BoxID = ParameterSetClass.QueryHonorParameterInfo("箱码", "MES");
                string UPNum = ParameterSetClass.QueryHonorParameterInfo("当前箱已扫描数量", "MES");

                string workorder = ParameterSetClass.QueryHonorParameterInfo("工单号", "MES");
                string ItemNo = ParameterSetClass.QueryHonorParameterInfo("料号", "MES");
                string site = ParameterSetClass.QueryHonorParameterInfo("站点", "MES");
                string LineBody = ParameterSetClass.QueryHonorParameterInfo("线体", "MES");
                string ComputerHost = ParameterSetClass.QueryHonorParameterInfo("电脑主机", "MES");
                string UserID = ParameterSetClass.QueryHonorParameterInfo("工号", "MES");
                string PackagingType = ParameterSetClass.QueryHonorParameterInfo("是否箱包装", "MES");
                string CPIsHeavyIndustry = ParameterSetClass.QueryHonorParameterInfo("产品是否重工", "MES");
                string WXIsHeavyIndustry = ParameterSetClass.QueryHonorParameterInfo("外箱是否重工", "MES");
                string WXIsRepaired = ParameterSetClass.QueryHonorParameterInfo("产品是否维修品", "MES");
                if (!string.IsNullOrEmpty(workorder) && !string.IsNullOrEmpty(ItemNo) && !string.IsNullOrEmpty(site) && !string.IsNullOrEmpty(LineBody) && !string.IsNullOrEmpty(ComputerHost) && !string.IsNullOrEmpty(UserID) && !string.IsNullOrEmpty(PackagingType) && !string.IsNullOrEmpty(CPIsHeavyIndustry) && !string.IsNullOrEmpty(WXIsHeavyIndustry) && !string.IsNullOrEmpty(WXIsRepaired))
                {
                    sttryy();

                    //如果箱号存在，那说明箱子还没扫完
                    if (!string.IsNullOrEmpty(xiaohao))
                    {
                        WarnForm boxWarnForm = new WarnForm($"{xiaohao.Substring(68, 16)}\r\n还没扫描完，只扫描了 {UPNum} PCS\r\n\r\n继续扫描：继续扫描未完成的箱子\r\n扫描新箱：扫描新的箱子", "继续扫描", "扫描新箱", WarnFormPasswordEnabled, WarnFormPasswordEnc);
                        if (boxWarnForm.ShowDialog() == DialogResult.OK)
                        {
                            this.Invoke((Action)(() =>
                            {
                                strNumber();
                                this.textBox1.Text = BoxID;
                                this.textBox1.Enabled = false;
                                LastXH = BoxID;
                                this.label21.Text = xiaohao.Substring(68, 16);
                                this.label19.Text = $"{UPNum} PCS";
                                this.label3.Text = "已恢复之前的数据，可以开始扫描产品！";
                                this.label3.ForeColor = Color.Green;
                                this.textBox2.Focus();
                            }));
                            LogHelper.WriteLog($"【{DateTime.Now:HH:mm:ss:ffff}】恢复{xiaohao.Substring(68, 16)}的数据，继续开始扫描" + Environment.NewLine);
                        }
                        else
                        {
                            //清空箱号和已扫描数量
                            ParameterSetClass.UpdateHonorData("箱号", null, "MES");
                            ParameterSetClass.UpdateHonorData("箱码", null, "MES");
                            ParameterSetClass.UpdateHonorData("当前箱已扫描数量", "0", "MES");
                            this.Invoke((Action)(() =>
                            {
                                this.textBox1.Focus();
                            }));
                            LogHelper.WriteLog($"【{DateTime.Now:HH:mm:ss:ffff}】放弃{xiaohao.Substring(68, 16)}的数据，开始扫描新箱" + Environment.NewLine);
                        }
                    }
                    else
                    {
                        this.Invoke((Action)(() =>
                        {
                            this.textBox1.Focus();
                        }));
                    }
                }
                else
                {
                    DalHelper.WriteLogInfo("参数:[工单号/料号/站点/线体/电脑主机/工号/包装类型/产品是否重工/外箱是否重工]信息不完整");
                    this.BeginInvoke((Action)(() =>
                    {
                        this.chengxujilu.AppendText($"【{DateTime.Now:HH:mm:ss:ffff}】参数:[工单号/料号/站点/线体/电脑主机/工号/包装类型/产品是否重工/外箱是否重工]信息不完整" + Environment.NewLine);
                    }));
                }
            }
            catch (Exception ex)
            {
                DalHelper.WriteLogInfo(ex.Message);
                LogHelper.WriteLog($"【{DateTime.Now:HH:mm:ss:ffff}】程序初始化出现错误：{ex.Message}" + Environment.NewLine);
                this.BeginInvoke(new Action(() =>
                {
                    this.chengxujilu.AppendText($"【{DateTime.Now:HH:mm:ss:ffff}】程序初始化出现错误：{ex.Message}" + Environment.NewLine);
                }));
            }
        }

        /// <summary>
        /// 查询VPP文件是否存在
        /// </summary>
        /// <param name="vppName"></param>
        /// <returns></returns>
        public bool VPPExists(string vppName,string vppFolder)
        {
            if (string.IsNullOrWhiteSpace(vppName))
                return false;

            try
            {
                // 文件夹是否存在
                if (!Directory.Exists(vppFolder))
                    return false;

                foreach (string filePath in Directory.EnumerateFiles(vppFolder, "*.vpp"))
                {
                    string fileName = Path.GetFileNameWithoutExtension(filePath);

                    if (fileName == vppName)
                    {
                        return true; 
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                DalHelper.WriteLogInfo(ex.Message);
                this.chengxujilu.AppendText($"【{DateTime.Now:HH:mm:ss:ffff}】查找VPP文件是否存在时出错：{ex.Message}" + Environment.NewLine);
                return false;
            }
        }

        #region 切换物料
        /// <summary>
        /// 查找文件夹内所有VPP文件
        /// </summary>
        public void SearchVppFile()
        {
            try
            {
                // 清空ComboBox现有项
                comboBox1.Items.Clear();

                // 查找所有.vpp文件
                string VppFolder = Path.Combine(Application.StartupPath, "Vpp");
                string[] VppFiles = Directory.GetFiles(VppFolder, "*.vpp");

                // 添加到列表
                foreach (string FilePath in VppFiles)
                {
                    // 获取VPP名称
                    string VPPName = Path.GetFileNameWithoutExtension(FilePath);
                    comboBox1.Items.Add(VPPName);
                }
            }
            catch (Exception ex)
            {
                DalHelper.WriteLogInfo("查找VPP文件时出错: " + ex.Message);
                this.BeginInvoke((Action)(() =>
                {
                    this.chengxujilu.AppendText($"【{DateTime.Now:HH:mm:ss:ffff}】查找VPP文件时出错: {ex.Message}" + Environment.NewLine);
                }));
            }
        }
        /// <summary>
        /// 当下拉框展开时候，触发查找方法
        /// </summary>
        private void ComboBox1_DropDown(object sender, System.EventArgs e)
        {
            SearchVppFile();
        }
        /// <summary>
        /// 当下拉框关闭时候，判断要不要切换VPP
        /// </summary>
        private void ComboBox1_DropDownClosed(object sender, EventArgs e)
        {
            string LastVPPName = ParameterSetClass.QueryHonorParameterInfo("物料编码", "视觉");

            //有记录的值时切换
            if (comboBox1.SelectedItem != null)
            {
                try
                {
                    // 释放旧资源
                    ScanCode1_Block.Dispose();

                    // 加载新VPP
                    ScanCode1_Block = CogSerializer.LoadObjectFromFile(Path.Combine(Application.StartupPath, "Vpp", $"{comboBox1.SelectedItem.ToString()}.vpp")) as CogToolBlock;

                    // 数据库保存
                    ParameterSetClass.UpdateHonorData("物料编码", comboBox1.SelectedItem.ToString(), "视觉");
                }
                catch (Exception ex)
                {
                    DalHelper.WriteLogInfo(ex.Message);
                    this.BeginInvoke((Action)(() =>
                    {
                        this.chengxujilu.AppendText($"【{DateTime.Now:HH:mm:ss:ffff}】切换VPP文件时出错: {ex.Message}" + Environment.NewLine);
                    }));
                }
            }
            else
            {
                comboBox1.Items.Add(LastVPPName);
                comboBox1.SelectedItem = LastVPPName;
            }    
        }
        #endregion

        private void WorkRun()
        {
            while (true)
            {
                try
                {
                    if (Work_Run_Case > 0)
                    {
                        HandleScannerStatusUpdated();
                    }
                }
                catch (Exception ex)
                {
                    this.BeginInvoke(new Action(() =>
                    {
                        this.chengxujilu.AppendText($"【{DateTime.Now:HH:mm:ss:ffff}】{ex.Message}" + Environment.NewLine);
                    }));
                    LogHelper.WriteLog(ex.Message);
                }
            }
        }

        /// <summary>
        /// 退出软件
        /// </summary>
        private void HonorForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            DialogResult result = MessageBox.Show("是否确认退出系统", "退出系统", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
            if (result == DialogResult.OK)
            {
                LogHelper.WriteLog("退出系统");
                this.Dispose();
                LightPort.Close();
                LogHelper.Close();
                objProcess.Kill();
            }
        }

        #region 视觉扫码流程
        /// <summary>
        /// 扫码VPP流程
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        int 无码数量;
        int 脏污数量;
        int 放反数量;
        int RowNum;
        int ColumnNum;
        bool VppResult;
        private void VPPRun()
        {
            try
            {
                VppResult = true;

                CogToolBlock toolBlock = ScanCode1_Block.Tools["ScanCode"] as CogToolBlock;
                CogAcqFifoTool cogAcqFifoTool = toolBlock.Tools["CogAcqFifoTool1"] as CogAcqFifoTool;
                CogIDTool cogIDTool1 = toolBlock.Tools["CogIDTool1"] as CogIDTool;
                CogFixtureTool cogFixtureTool1 = toolBlock.Tools["CogFixtureTool1"] as CogFixtureTool;
                CogHistogramTool cogHistogramTool1 = toolBlock.Tools["CogHistogramTool1"] as CogHistogramTool;
                CogBlobTool cogBlobTool1 = toolBlock.Tools["CogBlobTool1"] as CogBlobTool;
                CogBlobTool cogBlobTool2 = toolBlock.Tools["CogBlobTool2"] as CogBlobTool;
                CogBlobTool cogBlobTool3 = toolBlock.Tools["CogBlobTool3"] as CogBlobTool;
                CogBlobTool cogBlobTool4 = toolBlock.Tools["CogBlobTool4"] as CogBlobTool;

                CogRectangle cogRectangleID = new CogRectangle();

                string IDma = string.Empty;
                bool isOK = true;
                int IDNum = 0;
                无码数量 = 0;
                脏污数量 = 0;
                放反数量 = 0;
                IDAngle.Clear();

                //扫描框开始的位置
                double FirstX = Convert.ToDouble(toolBlock.Inputs["FirstX"].Value);
                double FirstY = Convert.ToDouble(toolBlock.Inputs["FirstY"].Value);

                //扫描框每次移动的距离
                double X = Convert.ToDouble(toolBlock.Inputs["X"].Value);
                double Y = Convert.ToDouble(toolBlock.Inputs["Y"].Value);

                //获取ID框尺寸
                double WidthID = Convert.ToDouble(toolBlock.Inputs["WidthID"].Value);
                double HighID = Convert.ToDouble(toolBlock.Inputs["HighID"].Value);

                //获取灰度值下限
                double Grayscale = Convert.ToDouble(toolBlock.Inputs["Grayscale"].Value);

                //获取行和列的数量
                RowNum = Convert.ToInt32(toolBlock.Inputs["RowNum"].Value);
                ColumnNum = Convert.ToInt32(toolBlock.Inputs["ColumnNum"].Value);

                if (UseLight)
                {
                    //打开光源
                    byte[] OnLight1 = { 0xa5, 0x00, 0x01, 0x04, 0x64, 0x00, 0x01, 0x00, 0x01, 0x00, 0x00, 0x00 };
                    LightPort.WriteHex(OnLight1);
                    byte[] OnLight2 = { 0xa5, 0x00, 0x03, 0x04, 0x64, 0x00, 0x01, 0x00, 0x01, 0x00, 0x00, 0x00 };
                    LightPort.WriteHex(OnLight2);
                }

                Thread.Sleep(20);

                //采集图像
                cogAcqFifoTool.Run();

                if (UseLight)
                {
                    //关闭光源
                    byte[] OffLight1 = { 0xa5, 0x00, 0x01, 0x04, 0x64, 0x00, 0x02, 0x00, 0x01, 0x00, 0x00, 0x00 };
                    LightPort.WriteHex(OffLight1);
                    byte[] OffLight2 = { 0xa5, 0x00, 0x03, 0x04, 0x64, 0x00, 0x02, 0x00, 0x01, 0x00, 0x00, 0x00 };
                    LightPort.WriteHex(OffLight2);
                }

                //如果采集图像失败，就终止
                if (cogAcqFifoTool.RunStatus.Result != CogToolResultConstants.Accept)
                {
                    this.BeginInvoke(new Action(() =>
                    {
                        this.label3.Text = "相机采图失败...";
                        this.label3.ForeColor = Color.Red;
                        this.UILabel1.Visible = false;
                    }));
                    VppResult = false;
                    return;
                }

                //循环运行扫码工具
                for (int A = 1; A <= RowNum; A++)
                {
                    for (int B = 1; B <= ColumnNum; B++)
                    {
                        //先清理
                        isOK = true;
                        IDma = string.Empty;

                        //编号累加
                        IDNum++;

                        //扫码
                        cogRectangleID.SetCenterWidthHeight(FirstX, FirstY, WidthID, HighID);
                        cogIDTool1.Region = cogRectangleID;
                        cogIDTool1.Run();

                        //没码
                        if (cogIDTool1.Results.Count != 0)
                        {
                            //脏污检测框定位跟随
                            cogFixtureTool1.Run();

                            //灰度值检测
                            cogHistogramTool1.Run();

                            //收集条码角度，用来后面判断有没有放反
                            IDAngle[IDNum] = Math.Round(cogIDTool1.Results[0].Angle, 1);

                            IDma = cogIDTool1.Results[0].DecodedData.DecodedString;

                            //如果平均灰度大于设定值，就是白纸壳，需要判断脏污，反之就是黑纸壳，不需要判断脏污
                            if (Grayscale < cogHistogramTool1.Result.Mean)
                            {
                                //识别上,左,下,右脏污
                                cogBlobTool1.Run();
                                cogBlobTool2.Run();
                                cogBlobTool3.Run();
                                cogBlobTool4.Run();

                                //有脏污,NG
                                if (cogBlobTool1.Results.GetBlobs().Count != 0 ||
                                    cogBlobTool2.Results.GetBlobs().Count != 0 ||
                                    cogBlobTool3.Results.GetBlobs().Count != 0 ||
                                    cogBlobTool4.Results.GetBlobs().Count != 0)
                                {
                                    isOK = false;
                                    IDma = "脏污";
                                    脏污数量++;
                                }
                            }
                        }
                        else
                        {
                            isOK = false;
                            IDma = "无码";
                            无码数量++;
                        }

                        CodeByID[IDNum] = IDma;

                        //调整识别框位置，S形顺序
                        if (B != ColumnNum)
                        {
                            if (A % 2 != 0)
                            {
                                FirstX -= X;
                            }
                            else
                            {
                                FirstX += X;
                            }
                        }

                        //图片保存
                        if (!isOK)
                        {
                            SaveImage(cogRecordDisplay1, isOK, RenderImageSavePath, $"Cam1_");
                        }
                    }
                    FirstY -= Y;
                }

                //更新图片显示
                cogRecordDisplay1.AutoFit = true;
                cogRecordDisplay1.Fit();
                cogRecordDisplay1.Record = ScanCode1_Block.CreateLastRunRecord().SubRecords[0];

                //挑出方向不一样的
                int PositiveCount = IDAngle.Count(kvp => kvp.Value > 0);
                int NegativeCount = IDAngle.Count(kvp => kvp.Value < 0);
                var AngleIsNG = IDAngle
                                    .Where(kvp => PositiveCount > NegativeCount ? kvp.Value < 0 : kvp.Value > 0)
                                    .Select(kvp => kvp.Key)
                                    .ToList();
                foreach (int Key in AngleIsNG)
                {
                    CodeByID[Key] = "放反";
                    放反数量++;
                }

                //按钮渲染
                foreach (var kvp in CodeByID)
                {
                    if (System.Text.RegularExpressions.Regex.IsMatch(kvp.Value, @"[\u4e00-\u9fa5]"))
                    {
                        sign(false, kvp.Value, kvp.Key.ToString());
                        FailID.Add(kvp.Key);
                    }
                    else
                    {
                        sign(true, kvp.Value, kvp.Key.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                this.BeginInvoke(new Action(() =>
                {
                    this.label3.Text = "视觉流程异常...";
                    this.label3.ForeColor = Color.Red;
                    this.chengxujilu.AppendText($"【{DateTime.Now:HH:mm:ss:ffff}】VPP执行失败：{ex.Message}" + Environment.NewLine);
                    this.UILabel1.Visible = false;
                }));
                LogHelper.WriteLog(ex.Message);
                VppResult = false;
            }
        }
        #endregion

        /// <summary>
        /// 保存图片
        /// </summary>
        public void SaveImage(CogRecordDisplay cogRecord, bool result, string savePath, string cameraName)
        {
            try
            {
                string filename = string.Empty;

                if (result)
                {
                    if (!Directory.Exists(Path.Combine(savePath, "OK", DateTime.Now.ToString("yyyy-MM-dd"))))
                    {
                        Directory.CreateDirectory(Path.Combine(savePath, "OK", DateTime.Now.ToString("yyyy-MM-dd")));
                    }
                    filename = $"{Path.Combine(savePath, "OK", DateTime.Now.ToString("yyyy-MM-dd"))}\\{cameraName}_{DateTime.Now.ToString("HH_mm_ss_fff")}.jpg";
                }
                else
                {
                    if (!Directory.Exists(Path.Combine(savePath, "NG", DateTime.Now.ToString("yyyy-MM-dd"))))
                    {
                        Directory.CreateDirectory(Path.Combine(savePath, "NG", DateTime.Now.ToString("yyyy-MM-dd")));
                    }
                    filename = $"{Path.Combine(savePath, "NG", DateTime.Now.ToString("yyyy-MM-dd"))}\\{DateTime.Now.ToString("HH_mm_ss_fff")}.jpg";
                }
                Bitmap bitmap = cogRecord.CreateContentBitmap(Cognex.VisionPro.Display.CogDisplayContentBitmapConstants.Image) as Bitmap;
                bitmap.Save(filename, ImageFormat.Jpeg);
            }
            catch (Exception ex)
            {
                this.BeginInvoke(new Action(() =>
                {
                    this.chengxujilu.AppendText($"【{DateTime.Now:HH:mm:ss:ffff}】图片保存失败：{ex.Message}" + Environment.NewLine);
                }));
                LogHelper.WriteLog(ex.Message);
            }
        }

        /// <summary>
        /// 删除保存的图片和日志
        /// </summary>
        /// <param name="parentPath"></param>
        /// <param name="daysToKeep"></param>
        private void CleanDirectory(string parentPath, int daysToKeep)
        {
            try
            {
                var dirPaths = Directory.GetDirectories(parentPath);
                if (dirPaths.Length <= daysToKeep) return;

                var dateInfos = new List<(string Path, DateTime Date)>(dirPaths.Length);

                foreach (var path in dirPaths)
                {
                    if (DateTime.TryParseExact(Path.GetFileName(path),
                                                "yyyy-MM-dd",
                                                CultureInfo.InvariantCulture,
                                                DateTimeStyles.None,
                                                out var dt))
                    {
                        dateInfos.Add((path, dt));
                    }
                }

                if (dateInfos.Count <= daysToKeep) return;

                var toDelete = dateInfos
                              .OrderByDescending(x => x.Item2)
                              .Skip(daysToKeep)
                              .Select(x => x.Path)
                              .ToList(); 

                Parallel.ForEach(toDelete, path =>
                {
                    Directory.Delete(path, true);
                });
            }
            catch(Exception ex) 
            {
                this.BeginInvoke(new Action(() =>
                {
                    this.chengxujilu.AppendText($"【{DateTime.Now:HH:mm:ss:ffff}】删除流程执行失败：{ex.Message}" + Environment.NewLine);
                }));
                LogHelper.WriteLog(ex.Message);
            }
        }

        #region 重置格子
        /// <summary>
        /// 状态格恢复默认
        /// </summary>
        private void ResetPackageData()
        {
            try
            {
                this.Invoke((Action)(() =>
                {
                    for (int i = 1; i <= 50; i++)
                    {
                        if (_buttonCache.TryGetValue(i, out var btn))
                        {
                            btn.Text = $"{i}";
                            btn.BackColor = Color.White;
                        }
                    }
                }));
            }
            catch (Exception ex)
            {
                this.BeginInvoke((Action)(() =>
                {
                    this.chengxujilu.AppendText($"【{DateTime.Now:HH:mm:ss:ffff}】按钮设置出现错误：{ex.Message}" + Environment.NewLine);
                }));
            }
        }
        #endregion

        #region  按钮状态更新
        /// <summary>
        /// 按钮状态更新
        /// </summary>
        /// <param name="flag"></param>
        /// <param name="message"></param>
        /// <param name="btxh"></param>
        public void sign(bool flag, string message, string btxh)
        {
            try
            {
                this.Invoke((Action)(() =>
                {
                    if (_buttonCache.TryGetValue(int.Parse(btxh), out var btn))
                    {
                        btn.Text = message;
                        btn.BackColor = flag ? Color.YellowGreen : Color.Red;
                    }
                }));
            }
            catch (Exception ex)
            {
                this.BeginInvoke(new Action(() =>
                {
                    this.chengxujilu.AppendText($"【{DateTime.Now:HH:mm:ss:ffff}】人工补扫程序出现错误：{ex.Message}" + Environment.NewLine);
                }));
            }
        }
        #endregion

        /// <summary>
        /// 主流程
        /// </summary>
        string Msg = string.Empty;
        string BarCodeStyle = string.Empty;
        private void HandleScannerStatusUpdated()
        {
            switch (Work_Run_Case)
            {
                case 1:
                    CodeByID.Clear();
                    FailID.Clear();
                    Msg = string.Empty;
                    BarCodeStyle = string.Empty;
                    this.BeginInvoke((Action)(() =>
                    {
                        this.chengxujilu.AppendText($"【{DateTime.Now:HH:mm:ss:ffff}】开始工作，接收读码信息：" + Environment.NewLine);
                    }));
                    //解除键盘触发扫码事件
                    this.KeyDown -= new KeyEventHandler(this.HonorForm_KeyDown);
                    Work_Run_Case = 2;
                    break;
                case 2:
                    Work_Run_Case = 0;
                    //获取条码样式
                    BarCodeStyle = ParameterSetClass.QueryHonorParameterInfo("条码样式", "MES");
                    this.Invoke((Action)(() =>
                    {
                        this.label3.Text = "";
                        this.UILabel1.Visible = true;
                    }));
                    //视觉扫码流程运行
                    VPPRun();
                    this.Invoke((Action)(() =>
                    {
                        this.UILabel1.Visible = false;
                    }));
                    //如果VPP运行失败
                    if (!VppResult)
                    {
                        this.BeginInvoke((Action)(() =>
                        {
                            this.label3.Text = "VPP运行失败...";
                            this.label3.ForeColor = Color.Red;
                        }));
                        this.KeyDown += new KeyEventHandler(this.HonorForm_KeyDown);
                        return;
                    }
                    //判断是不是所有码都ok
                    if (FailID.Count > 0)
                    {
                        Msg = "扫描";
                        this.Invoke((Action)(() =>
                        {
                            WarnForm warnForm = new WarnForm($"存在下列不良\r\n\r\n无码数量：{无码数量}\r\n脏污数量：{脏污数量}\r\n放反数量：{放反数量}", "视觉重扫", "人工补码", false, WarnFormPasswordEnc);
                            DialogResult result = warnForm.ShowDialog();
                            if (result == DialogResult.OK)
                            {
                                //视觉重新检测读码
                                Work_Run_Case = 1;
                            }
                            else
                            {
                                //扫码枪人工补码
                                Work_Run_Case = 4;
                            }
                        }));
                    }
                    else
                    {
                        //检测重码
                        var SameID = CodeByID.GroupBy(x => x.Value)
                             .Where(g => g.Count() > 1)
                             .SelectMany(g => g)
                             .ToList();
                        if (SameID.Any())
                        {
                            foreach (var kvp in SameID)
                            {
                                sign(false, "重码", kvp.Key.ToString());
                            }

                            this.BeginInvoke((Action)(() =>
                            {
                                this.label3.Text = "有重复的阵列码，需要重新启动扫描";
                                this.label3.ForeColor = Color.Red;
                            }));

                            this.KeyDown += new KeyEventHandler(this.HonorForm_KeyDown);
                        }
                        else
                        {
                            Work_Run_Case = 3;
                        }
                    }
                    break;
                case 3:
                    Work_Run_Case = 0;
                    this.BeginInvoke((Action)(() =>
                    {
                        this.label3.Text = "条码校验中...";
                        this.label3.ForeColor = Color.Green;
                        this.chengxujilu.AppendText($"【{DateTime.Now:HH:mm:ss:ffff}】正在校验视觉系统读码信息：" + Environment.NewLine); 
                    }));
                    //条码校验
                    foreach (var kvp in CodeByID)
                    {
                        if (!CheckBarCodeStyle(kvp.Value, BarCodeStyle))
                        {
                            FailID.Add(kvp.Key);
                            sign(false, "混料", kvp.Key.ToString());
                        }
                    }
                    //如果有条码样式校验失败的
                    if (FailID.Count > 0)
                    {
                        Msg = "校验";
                        this.Invoke((Action)(() =>
                        {
                            GetWarnFormPassword();
                            WarnForm warnForm = new WarnForm($"存在 {FailID.Count} 个混料，MES下发的条码样式为\r\n{BarCodeStyle}\r\n\r\n扫描到的为\r\n{CodeByID[FailID[0]]}", "人工补码", "结束作业", WarnFormPasswordEnabled, WarnFormPasswordEnc);
                            DialogResult result = warnForm.ShowDialog();
                            if (result == DialogResult.OK)
                            {
                                //扫码枪人工补码
                                Work_Run_Case = 4;
                            }
                            else
                            {
                                this.label3.Text = "已结束作业，等处理混料后，再次扫描整盘";
                                this.label3.ForeColor = Color.Red;
                                CodeByID.Clear();
                                FailID.Clear();
                                this.KeyDown += new KeyEventHandler(this.HonorForm_KeyDown);
                            }
                        }));
                    }
                    else
                    {
                        //50个全都成功就进入上传流程
                        Work_Run_Case = 5;
                    }
                    break;
                case 4:
                    Work_Run_Case = 0;
                    this.Invoke((Action)(() =>
                    {
                        textBox2.Focus();
                    }));
                    SetFocusBtnColor(FailID[0]);
                    this.textBox2.KeyDown += new KeyEventHandler(this.textBox2_KeyDown);
                    break;
                case 5:
                    Work_Run_Case = 0;
                    this.BeginInvoke((Action)(() =>
                    {
                        this.chengxujilu.AppendText($"【{DateTime.Now:HH:mm:ss:ffff}】正在上传Mes系统：" + Environment.NewLine); 
                    }));
                    //MES上传
                    UploadMES();
                    break;
                case 6:
                    Work_Run_Case = 0;
                    this.Invoke((Action)(() =>
                    {
                        textBox2.Focus();
                    }));
                    SetFocusBtnColor(FailID[0]);
                    this.textBox2.KeyDown += new KeyEventHandler(this.textBox2_Repulse_KeyDown);
                    break;
                case 7:
                    Work_Run_Case = 0;
                    this.KeyDown += new KeyEventHandler(this.HonorForm_KeyDown);
                    this.BeginInvoke((Action)(() =>
                    {
                        this.label3.Text = $"该盘{ColumnNum * RowNum}个已全部上传完成！请更换.";
                        this.label3.ForeColor = Color.Green;
                        this.chengxujilu.AppendText($"【{DateTime.Now:HH:mm:ss:ffff}】该盘{ColumnNum * RowNum}个已全部上传完成！请更换." + Environment.NewLine); 
                    }));
                    ResetPackageData();
                    break;
            }
        }

        /// <summary>
        /// 键盘↓键触发工作流
        /// </summary>
        private void HonorForm_KeyDown(object sender, KeyEventArgs e)
        {
            // 检测按下的键是否是↓键
            if (e.KeyCode == Keys.Down)
            {
                //检测箱码有没有扫
                if (!string.IsNullOrEmpty(textBox1.Text))
                {
                    //流程码
                    Work_Run_Case = 1;
                }
                else
                {
                    MessageBox.Show("箱码没有扫描！需要先扫描箱码！");
                }
            }
        }

        /**------------------非业务代码 -------------------------**/
        #region 参数设置页面
        private void pictureBox1_Click(object sender, EventArgs e)
        {
            //string PM = Interaction.InputBox("请输入密码", "输入密码", "", -1, -1);
            //string Password = ParameterSetClass.QueryHonorParameterInfo("管理员密码", "系统");
            //if (!string.IsNullOrEmpty(PM))
            //{
            //    string passwordtext = CryptInfoNameSpace.CryptInfoNameSpace.Encrypt(PM);

            //    if (PM == "0" || passwordtext == Password)
            //    {
            //        HonorCanShuForm csf = new HonorCanShuForm();
            //        csf.ShowDialog(this);
            //    }
            //    else
            //    {
            //        MessageBox.Show("密码输入错误...");
            //        return;
            //    }
            //}
            HonorCanShuForm csf = new HonorCanShuForm();
            csf.ShowDialog(this);
        }
        #endregion

        #region 人工补码按钮UI显示
        private int LastID = 0;
        /// <summary>
        /// 人工补码按钮UI显示
        /// </summary>
        /// <param name="ID"></param>
        private void SetFocusBtnColor(int ID)
        {
            // 恢复上一次选中按钮的状态
            if (LastID != 0 && _buttonCache.TryGetValue(LastID, out var lastBtn))
            {
                this.Invoke((Action)(() =>
                {
                    lastBtn.FlatStyle = FlatStyle.Standard;
                    lastBtn.FlatAppearance.BorderColor = Color.Black;
                    lastBtn.FlatAppearance.BorderSize = 1;
                }));
            }

            // ID为0时复位
            if (ID == 0)
            {
                LastID = 0;
                return;
            }

            // 设置当前按钮高亮
            if (_buttonCache.TryGetValue(ID, out var btn))
            {
                this.Invoke((Action)(() =>
                {
                    this.label25.Text = ID.ToString();
                    btn.FlatStyle = FlatStyle.Flat;
                    btn.FlatAppearance.BorderColor = Color.Blue;
                    btn.FlatAppearance.BorderSize = 10;
                }));
            }

            LastID = ID;
        }
        #endregion
        /** -----------------------------------------------------旧源码----------------------------------------------------- **/

        #region 调用MES过站接口
        private void UploadMES()
        {
            try
            {
                if(CodeByID.Count != (ColumnNum * RowNum))
                {
                    this.BeginInvoke((Action)(() =>
                    {
                        this.label3.Text = $"上传的条码不满{ColumnNum * RowNum}个，当前条码数目：{CodeByID.Count.ToString()}";
                        this.label3.ForeColor = Color.Red;
                        this.chengxujilu.AppendText($"【{DateTime.Now:HH:mm:ss:ffff}】false：上传的条码不满{ColumnNum * RowNum}个" + Environment.NewLine);
                        LogHelper.WriteLog($"【{DateTime.Now:HH:mm:ss:ffff}】false：上传的条码不满{ColumnNum * RowNum}个" + Environment.NewLine);
                    }));

                    this.KeyDown += new KeyEventHandler(this.HonorForm_KeyDown);
                    return;
                }

                string workorder = null, ItemNo = null, site = null, sitexuhao = null,
                Scanxuhao = null, LineBody = null, ComputerHost = null, UserID = null,
                PackagingType = null, CaseNumber = null, IsHeavyIndustry = null, WXIsRepaired = null;

                Parallel.Invoke(
                    () => workorder = ParameterSetClass.QueryHonorParameterInfo("工单号", "MES"),
                    () => ItemNo = ParameterSetClass.QueryHonorParameterInfo("料号", "MES"),
                    () => site = ParameterSetClass.QueryHonorParameterInfo("站点", "MES"),
                    () => sitexuhao = ParameterSetClass.QueryHonorParameterInfo("站点序号", "MES"),
                    () => Scanxuhao = ParameterSetClass.QueryHonorParameterInfo("扫描序号", "MES"),
                    () => LineBody = ParameterSetClass.QueryHonorParameterInfo("线体", "MES"),
                    () => ComputerHost = ParameterSetClass.QueryHonorParameterInfo("电脑主机", "MES"),
                    () => UserID = ParameterSetClass.QueryHonorParameterInfo("工号", "MES"),
                    () => PackagingType = ParameterSetClass.QueryHonorParameterInfo("是否箱包装", "MES"),
                    () => CaseNumber = ParameterSetClass.QueryHonorParameterInfo("箱号", "MES"),
                    () => IsHeavyIndustry = ParameterSetClass.QueryHonorParameterInfo("产品是否重工", "MES"),
                    () => WXIsRepaired = ParameterSetClass.QueryHonorParameterInfo("产品是否维修品", "MES")
                );

                if (string.IsNullOrEmpty(workorder) ||
                   string.IsNullOrEmpty(ItemNo) ||
                   string.IsNullOrEmpty(site) ||
                   string.IsNullOrEmpty(LineBody) ||
                   string.IsNullOrEmpty(ComputerHost) ||
                   string.IsNullOrEmpty(UserID) ||
                   string.IsNullOrEmpty(PackagingType) ||
                   string.IsNullOrEmpty(CaseNumber) ||
                   string.IsNullOrEmpty(IsHeavyIndustry) ||
                   string.IsNullOrEmpty(WXIsRepaired))   
                {
                    this.BeginInvoke((Action)(() =>
                    {
                        this.label3.Text = $"上传参数不完整";
                        this.label3.ForeColor = Color.Red;
                        this.chengxujilu.AppendText($"【{DateTime.Now:HH:mm:ss:ffff}】false：上传参数不完整" + Environment.NewLine);
                        LogHelper.WriteLog($"【{DateTime.Now:HH:mm:ss:ffff}】false：上传参数不完整" + Environment.NewLine);
                    }));

                    this.KeyDown -= new KeyEventHandler(this.HonorForm_KeyDown);
                    this.KeyDown += new KeyEventHandler(this.HonorForm_KeyDown);
                    return;
                }

                string str = string.Empty;
                string UPNum = string.Empty;
                bool FullBox = false;
                string BoxSN = textBox1.Text.Trim();

                this.BeginInvoke((Action)(() =>
                {
                    this.label3.Text = "正在上传数据，请勿操作设备！！！";
                    this.label3.ForeColor = Color.Green;
                }));

                //循环上传列表中的全部条码
                foreach (var code in CodeByID)
                {
                    //调用MES方法过站
                    str = ws.SetSimplePackScan(code.Value, workorder, ItemNo, LineBody, ComputerHost, UserID, site, sitexuhao, Scanxuhao, "1", CaseNumber, "", PackagingType, "N", WXIsRepaired, "N");

                    if (str.StartsWith("OK"))
                    {
                        UPNum = regex.Match(str).Value;

                        //按钮状态更新
                        sign(true, "已上传", code.Key.ToString());

                        this.BeginInvoke(new Action(() =>
                        {
                            //文字内容更新
                            this.label19.Text = $"{UPNum} PCS";
                            //通知栏更新
                            this.chengxujilu.AppendText($"【{DateTime.Now.ToString("HH:mm:ss:ffff")}】SN {code.Value} 过站成功，MES返回结果: {str}" + Environment.NewLine);
                        }));

                        if (str.EndsWith("该箱已满,请扫描下一箱"))
                        {
                            FullBox = true;
                            break;
                        }
                    }
                    else
                    {
                        FailID.Add(code.Key);

                        sign(false, str.Substring(str.IndexOf(' ') + 1), code.Key.ToString());

                        this.BeginInvoke(new Action(() =>
                        {
                            this.chengxujilu.AppendText($"【{DateTime.Now.ToString("HH:mm:ss:ffff")}】产品过站失败：{str}" + Environment.NewLine);
                        }));
                        LogHelper.WriteLog($"【{DateTime.Now.ToString("HH:mm:ss:ffff")}】产品过站失败：{str}" + Environment.NewLine);
                    }

                    //保存原始数据
                    Task.Run(() => DalHelper.WriteData(code.Value, CaseNumber, BoxSN, null, str));
                }

                //上传完后清空
                CodeByID.Clear();

                //保存已上传数量
                ParameterSetClass.UpdateHonorData("当前箱已扫描数量", UPNum, "MES");

                //如果有上传失败的
                if (FailID.Count > 0)
                {
                    this.Invoke((Action)(() =>
                    {
                        GetWarnFormPassword();
                        WarnForm warnForm = new WarnForm($"有 {FailID.Count} 个上传失败的产品，请人工确认！！" + "\r\n\r\n确认：现场人工补码.\r\n取消：联系品管处理后，重新扫描整盘", "人工补码", "结束作业", WarnFormPasswordEnabled, WarnFormPasswordEnc);
                        DialogResult result = warnForm.ShowDialog();
                        if (result == DialogResult.OK)
                        {
                            //人工补码
                            Work_Run_Case = 6;
                            this.label3.Text = "部分数据上传完毕！！！还有 " + FailID.Count + " 个条码未通过";
                            this.label3.ForeColor = Color.OrangeRed;
                        }
                        else
                        {
                            this.label3.Text = "结束工作，待品管处理后，再重新扫描整盘";
                            this.KeyDown += new KeyEventHandler(this.HonorForm_KeyDown);
                        }
                    }));
                    return;
                }

                //如果满箱
                if (FullBox)
                {
                    //弹出满箱校验的窗口
                    this.Invoke((Action)(() =>
                    {
                        this.textBox3.Enabled = true;
                        this.textBox3.Text = "";
                        this.textBox3.Focus();
                        using (WaitForScanner waitForm = new WaitForScanner("该箱已满,请扫描荣耀PSN"))
                        {
                            if (waitForm.ShowDialog() == DialogResult.OK)
                            {
                                string scannerText = waitForm.ScannerText;
                                // 在这里可以使用返回的数据做其他操作
                                textBox3.Text = scannerText;
                                KeyEventArgs keyEventArgs = new KeyEventArgs(Keys.Enter);
                                //对比箱号是不是一样
                                textBox3_KeyDown(textBox3, keyEventArgs);
                            }
                        }
                    }));
                }
                else
                {
                    Work_Run_Case = 7;
                }
            }
            catch (Exception ex)
            {
                this.BeginInvoke(new Action(() =>
                {
                    this.label3.Text = "上传出现异常...";
                    this.label3.ForeColor = Color.Red;
                    this.chengxujilu.AppendText($"【{DateTime.Now:HH:mm:ss:ffff}】上传出现异常：{ex.Message}" + Environment.NewLine);
                }));
                LogHelper.WriteLog(ex.Message);
                this.KeyDown += new KeyEventHandler(this.HonorForm_KeyDown);
            }
        }
        #endregion

        #region 外箱条码框回车事件
        /// <summary>
        /// 外箱条码框回车事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        string LastXH = string.Empty;
        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyValue == 13)
            {
                string workorder = ParameterSetClass.QueryHonorParameterInfo("工单号", "MES");
                string ItemNo = ParameterSetClass.QueryHonorParameterInfo("料号", "MES");
                string site = ParameterSetClass.QueryHonorParameterInfo("站点", "MES");
                string IsHeavyIndustry = ParameterSetClass.QueryHonorParameterInfo("外箱是否重工", "MES");
                string LineBody = ParameterSetClass.QueryHonorParameterInfo("线体", "MES");
                string UserID = ParameterSetClass.QueryHonorParameterInfo("工号", "MES");
                if (!string.IsNullOrEmpty(workorder) && !string.IsNullOrEmpty(ItemNo) && !string.IsNullOrEmpty(site) && !string.IsNullOrEmpty(IsHeavyIndustry))
                {
                    string OuterBoxBarcode = this.textBox1.Text.Trim();

                    if (!string.IsNullOrEmpty(OuterBoxBarcode))
                    {
                        //webservice接口
                        string MESstr = ws.GetCheckHWASNLXASN(workorder, ItemNo, site, IsHeavyIndustry, "3", "", OuterBoxBarcode, LineBody, UserID);

                        if (MESstr.StartsWith("OK"))
                        {
                            string xianghao = MESstr.Substring(3, MESstr.Length - 3);

                            //如果这个箱码之前已扫描过，就删除全部SN
                            if (DalHelper.CheckHonorBox(OuterBoxBarcode))
                            {
                                DalHelper.DeleteHONORBox(OuterBoxBarcode);
                                DalHelper.DelBoxSN(xianghao);
                            }

                            //保存箱号和箱码
                            ParameterSetClass.UpdateHonorData("箱号", xianghao, "MES");
                            ParameterSetClass.UpdateHonorData("箱码", OuterBoxBarcode, "MES");

                            //记录这一次的外箱条码
                            LastXH = OuterBoxBarcode;

                            strNumber();

                            this.label21.Text = xianghao.Substring(68, 16);
                            this.textBox1.Enabled = false;
                            this.label3.Text = "OK: 扫描成功，可以开始扫描产品";
                            this.label3.ForeColor = Color.Green;
                            this.chengxujilu.AppendText($"【{DateTime.Now:HH:mm:ss:ffff}】{MESstr}" + Environment.NewLine);

                            //保存原始数据
                            DalHelper.WriteData("", xianghao, OuterBoxBarcode, null, MESstr);
                            DalHelper.InsertHonorBox(OuterBoxBarcode);
                        }
                        else
                        {
                            this.textBox1.Text = "";
                            this.textBox1.Focus();
                            this.label3.Text = MESstr;
                            this.label3.ForeColor = Color.Red;
                            this.chengxujilu.AppendText($"【{DateTime.Now:HH:mm:ss:ffff}】{MESstr}" + Environment.NewLine);

                            //保存原始数据
                            DalHelper.WriteData("", "", OuterBoxBarcode, null, MESstr);
                            LogHelper.WriteLog($"【{DateTime.Now:HH:mm:ss:ffff}】{MESstr}" + Environment.NewLine);

                            GetWarnFormPassword();
                            WarnForm boxWarnForm = new WarnForm("箱码未通过MES校验", WarnFormPasswordEnabled, WarnFormPasswordEnc);
                            if (boxWarnForm.ShowDialog() == DialogResult.OK)
                            {
                                this.chengxujilu.AppendText($"【{DateTime.Now:HH:mm:ss:ffff}】: 重新扫描箱码" + Environment.NewLine);
                            }
                            else
                            {
                                this.chengxujilu.AppendText($"【{DateTime.Now:HH:mm:ss:ffff}】: 联系品管处理" + Environment.NewLine);
                            }
                        }
                    }
                    else
                    {
                        this.label3.Text = "箱码不能为空！";
                        this.label3.ForeColor = Color.Red;
                        this.textBox1.Focus();
                        this.chengxujilu.AppendText($"【{DateTime.Now:HH:mm:ss:ffff}】外箱条码不能为空！！！" + Environment.NewLine);
                    }
                }
                else
                {
                    this.label3.Text = "请先设置参数(工单号/料号/站点/是否重工)";
                    this.label3.ForeColor = Color.Red;
                    this.chengxujilu.AppendText($"【{DateTime.Now:HH:mm:ss:ffff}】请先设置参数(工单号/料号/站点/是否重工)！！！" + Environment.NewLine);
                }
            }
        }
        #endregion

        #region 荣耀PSN条码回车事件
        /// <summary>
        /// 荣耀PSN条码回车事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textBox3_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyValue == 13)
            {
                string OuterBoxBarcode = textBox3.Text.Trim();

                if (!string.IsNullOrEmpty(OuterBoxBarcode))
                {
                    if (OuterBoxBarcode == LastXH)
                    {
                        this.chengxujilu.AppendText($"【{DateTime.Now:HH:mm:ss:ffff}】荣耀PSN校验成功：" + Environment.NewLine);

                        this.label3.Text = "PSN校验成功，完成这箱的扫描！";
                        this.label3.ForeColor = Color.Green;
                        this.label19.Text = "";
                        this.label21.Text = "";
                        this.textBox1.Enabled = true;
                        this.textBox1.Text = "";
                        this.textBox3.Text = "";
                        this.textBox3.Enabled = false;
                        this.textBox1.Focus();

                        LastXH = string.Empty;

                        //清空箱号，ASN，已扫描数量
                        ParameterSetClass.UpdateHonorData("箱号", null, "MES");
                        ParameterSetClass.UpdateHonorData("ASN", null, "MES");
                        ParameterSetClass.UpdateHonorData("当前箱已扫描数量", "0", "MES");

                        this.KeyDown += new KeyEventHandler(this.HonorForm_KeyDown);

                        ResetPackageData();
                    }
                    else
                    {
                        this.textBox3.Text = "";
                        this.textBox3.Focus();
                        using (WaitForScanner waitForm = new WaitForScanner("PSN和上一次的不一样！"))
                        {
                            if (waitForm.ShowDialog() == DialogResult.OK)
                            {
                                string scannerText = waitForm.ScannerText;
                                // 在这里可以使用返回的数据做其他操作
                                textBox3.Text = scannerText;
                                KeyEventArgs keyEventArgs = new KeyEventArgs(Keys.Enter);
                                textBox3_KeyDown(textBox3, keyEventArgs);
                            }
                        }

                        this.chengxujilu.AppendText($"【{DateTime.Now:HH:mm:ss:ffff}】PSN和上一次的不一样！" + Environment.NewLine);
                    }
                }
                else
                {
                    this.textBox3.Text = "";
                    this.textBox3.Focus();
                    this.chengxujilu.AppendText($"【{DateTime.Now:HH:mm:ss:ffff}】PSN条码不能为空！！！" + Environment.NewLine);
                    using (WaitForScanner waitForm = new WaitForScanner("PSN条码不能为空！"))
                    {
                        if (waitForm.ShowDialog() == DialogResult.OK)
                        {
                            string scannerText = waitForm.ScannerText;
                            // 在这里可以使用返回的数据做其他操作
                            textBox3.Text = scannerText;
                            KeyEventArgs keyEventArgs = new KeyEventArgs(Keys.Enter);
                            // 对比箱号是不是一样
                            textBox3_KeyDown(textBox3, keyEventArgs);
                        }
                    }
                }
            }
        }
        #endregion

        #region 软件退出按钮
        private void pictureBox3_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("是否确认退出系统", "退出系统", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
            if (result == DialogResult.OK)
            {
                LogHelper.WriteLog("退出系统");
                LightPort.Close();
                LogHelper.Close();
                this.Dispose();
                objProcess.Kill();
            }
        } 
        #endregion

        #region 程序记录文本框
        private void chengxujilu_TextChanged(object sender, EventArgs e)
        {
            this.chengxujilu.Select(this.chengxujilu.Text.Length, 0);
            this.chengxujilu.ScrollToCaret();
        } 
        #endregion

        #region 查询历史数据按钮
        private void pictureBox2_Click(object sender, EventArgs e)
        {
            this.Invoke((Action)(() =>
            {
                DataQueryForm dqf = new DataQueryForm();
                dqf.ShowDialog();
            }));
        }
        #endregion

        #region  校验条码样式方法
        /// <summary>
        /// 校验条码样式方法
        /// </summary>
        /// <param name="SN"></param>
        /// <returns></returns>
        public static bool CheckBarCodeStyle(string sn, string barCodeStyle)
        {
            if (string.IsNullOrEmpty(sn) || string.IsNullOrEmpty(barCodeStyle))
                return false;

            if (sn.Length != barCodeStyle.Length)
                return false;

            for (int i = 0; i < barCodeStyle.Length; i++)
            {
                char c = barCodeStyle[i];
                if (c != '*' && c != sn[i])
                    return false;
            }
            return true;
        }
        #endregion

        #region 获取工单号
        /// <summary>
        /// 获取工单号
        /// </summary>
        public void strNumber()
        {
            string workorder = ParameterSetClass.QueryHonorParameterInfo("工单号", "MES");
            string boxDate = ParameterSetClass.QueryHonorParameterInfo("工单日期", "MES");  //20240704
            try
            {
                if (!string.IsNullOrEmpty(workorder))
                {
                    string str = ws.GetAutoATEGetPackData_New(workorder.Trim(), boxDate);
                    if (!str.StartsWith("NG"))
                    {
                        string[] condition = { ";" };
                        string[] result = str.Split(condition, StringSplitOptions.None);
                        if (result.Length > 0)
                        {
                            string liaohao = result[0].Trim();
                            string yangshi = result[1].Trim();
                            string gongdanshuliang = result[2].Trim();
                            string xiangshu = (Int32.Parse(result[3])).ToString().Trim();
                            string yingzhuang = result[4].Trim();
                            string xianti = result[5].Trim();
                            ParameterSetClass.UpdateHonorData("料号", liaohao, "MES");
                            ParameterSetClass.UpdateHonorData("条码样式", yangshi, "MES");
                            ParameterSetClass.UpdateHonorData("当前工单总数量", gongdanshuliang, "MES");
                            ParameterSetClass.UpdateHonorData("当前工单扫描箱数", xiangshu, "MES");
                            ParameterSetClass.UpdateHonorData("当前工单每箱数量", yingzhuang, "MES");
                            ParameterSetClass.UpdateHonorData("线体", xianti, "MES");
                            this.Invoke((Action)(() => 
                            {
                                this.label16.Text = gongdanshuliang + " PCS";
                                this.label17.Text = xiangshu + " 箱";
                                this.label18.Text = yingzhuang + " PCS";
                                this.label19.Text = "0 PCS";
                            }));
                        }
                        else
                        {
                            this.BeginInvoke((Action)(() =>
                            {
                                this.chengxujilu.AppendText($"【{DateTime.Now:HH:mm:ss:ffff}】MES接口返回参数不完整，MES接口返回：{str}" + Environment.NewLine);
                            }));
                            DalHelper.WriteLogInfo($"【{DateTime.Now:HH:mm:ss:ffff}】MES接口返回参数不完整，MES接口返回：{str}" + Environment.NewLine);
                        }
                    }
                    else
                    {
                        this.BeginInvoke((Action)(() =>
                        {
                            this.chengxujilu.AppendText($"【{DateTime.Now:HH:mm:ss:ffff}】未查询到此工单号的料号信息和条码样式，请检查工单号是否正确！！！" + Environment.NewLine);
                        }));
                        DalHelper.WriteLogInfo($"【{DateTime.Now:HH:mm:ss:ffff}】未查询到此工单号的料号信息和条码样式，请检查工单号是否正确！！！" + Environment.NewLine);
                    }
                }
            }
            catch (Exception ex)
            {
                this.BeginInvoke((Action)(() =>
                {
                    this.chengxujilu.AppendText($"【{DateTime.Now:HH:mm:ss:ffff}】获取生产数量接口出错：{ex.Message}" + Environment.NewLine);
                }));
                DalHelper.WriteLogInfo($"【{DateTime.Now:HH:mm:ss:ffff}】获取生产数量接口出错：{ex.Message}" + Environment.NewLine);
            }
        }
        #endregion

        #region 状态栏数据
        /// <summary>
        /// 加载状态栏数据
        /// </summary>
        public void sttryy()
        {
            this.Invoke((Action)(() => 
            {
                this.label15.Text = ParameterSetClass.QueryHonorParameterInfo("工单号", "MES");
                this.label16.Text = ParameterSetClass.QueryHonorParameterInfo("当前工单总数量", "MES") + " PCS";
                //this.label17.Text = ParameterSetClass.QueryHonorParameterInfo("当前工单扫描箱数", "MES") + " 箱";
                this.label18.Text = ParameterSetClass.QueryHonorParameterInfo("当前工单每箱数量", "MES") + " PCS";
                //this.label21.Text = ParameterSetClass.QueryHonorParameterInfo("箱号", "MES");
            }));
        }
        #endregion

        #region 显示按钮上的数据
        private void BarCode1_Click(object sender, EventArgs e)
        {
            string text = this.BarCode1.Text;

            if (text.StartsWith("NG") || this.BarCode1.BackColor == Color.Red)
            {
                MessageBox.Show(text);
                this.label25.Text = "1";
            }
        }

        private void BarCode2_Click(object sender, EventArgs e)
        {
            string text = this.BarCode2.Text;

            if (text.StartsWith("NG") || this.BarCode2.BackColor == Color.Red)
            {
                MessageBox.Show(text);
                this.label25.Text = "2";
            }
        }

        private void BarCode3_Click(object sender, EventArgs e)
        {
            string text = this.BarCode3.Text;

            if (text.StartsWith("NG") || this.BarCode3.BackColor == Color.Red)
            {
                MessageBox.Show(text);
                this.label25.Text = "3";
            }
        }

        private void BarCode4_Click(object sender, EventArgs e)
        {
            string text = this.BarCode4.Text;

            if (text.StartsWith("NG") || this.BarCode4.BackColor == Color.Red)
            {
                MessageBox.Show(text);
                this.label25.Text = "4";
            }
        }

        private void BarCode5_Click(object sender, EventArgs e)
        {
            string text = this.BarCode5.Text;

            if (text.StartsWith("NG") || this.BarCode5.BackColor == Color.Red)
            {
                MessageBox.Show(text);
                this.label25.Text = "5";
            }
        }

        private void BarCode6_Click(object sender, EventArgs e)
        {
            string text = this.BarCode6.Text;

            if (text.StartsWith("NG") || this.BarCode6.BackColor == Color.Red)
            {
                MessageBox.Show(text);
                this.label25.Text = "6";
            }
        }

        private void BarCode7_Click(object sender, EventArgs e)
        {
            string text = this.BarCode7.Text;

            if (text.StartsWith("NG") || this.BarCode7.BackColor == Color.Red)
            {
                MessageBox.Show(text);
                this.label25.Text = "7";
            }
        }

        private void BarCode8_Click(object sender, EventArgs e)
        {
            string text = this.BarCode8.Text;

            if (text.StartsWith("NG") || this.BarCode8.BackColor == Color.Red)
            {
                MessageBox.Show(text);
                this.label25.Text = "8";
            }
        }

        private void BarCode9_Click(object sender, EventArgs e)
        {
            string text = this.BarCode9.Text;

            if (text.StartsWith("NG") || this.BarCode9.BackColor == Color.Red)
            {
                MessageBox.Show(text);
                this.label25.Text = "9";
            }
        }

        private void BarCode10_Click(object sender, EventArgs e)
        {
            string text = this.BarCode10.Text;

            if (text.StartsWith("NG") || this.BarCode10.BackColor == Color.Red)
            {
                MessageBox.Show(text);
                this.label25.Text = "10";
            }
        }

        private void BarCode11_Click(object sender, EventArgs e)
        {
            string text = this.BarCode11.Text;

            if (text.StartsWith("NG") || this.BarCode11.BackColor == Color.Red)
            {
                MessageBox.Show(text);
                this.label25.Text = "11";
            }
        }

        private void BarCode12_Click(object sender, EventArgs e)
        {
            string text = this.BarCode12.Text;

            if (text.StartsWith("NG") || this.BarCode12.BackColor == Color.Red)
            {
                MessageBox.Show(text);
                this.label25.Text = "12";
            }
        }

        private void BarCode13_Click(object sender, EventArgs e)
        {
            string text = this.BarCode13.Text;

            if (text.StartsWith("NG") || this.BarCode13.BackColor == Color.Red)
            {
                MessageBox.Show(text);
                this.label25.Text = "13";
            }
        }

        private void BarCode14_Click(object sender, EventArgs e)
        {
            string text = this.BarCode14.Text;
            if (text.StartsWith("NG") || this.BarCode14.BackColor == Color.Red)
            {
                MessageBox.Show(text);
                this.label25.Text = "14";
            }
        }

        private void BarCode15_Click(object sender, EventArgs e)
        {
            string text = this.BarCode15.Text;
            if (text.StartsWith("NG") || this.BarCode15.BackColor == Color.Red)
            {
                MessageBox.Show(text);
                this.label25.Text = "15";
            }
        }

        private void BarCode16_Click(object sender, EventArgs e)
        {
            string text = this.BarCode16.Text;
            if (text.StartsWith("NG") || this.BarCode16.BackColor == Color.Red)
            {
                MessageBox.Show(text);
                this.label25.Text = "16";
            }
        }

        private void BarCode17_Click(object sender, EventArgs e)
        {
            string text = this.BarCode17.Text;
            if (text.StartsWith("NG") || this.BarCode17.BackColor == Color.Red)
            {
                MessageBox.Show(text);
                this.label25.Text = "17";
            }
        }

        private void BarCode18_Click(object sender, EventArgs e)
        {
            string text = this.BarCode18.Text;
            if (text.StartsWith("NG") || this.BarCode18.BackColor == Color.Red)
            {
                MessageBox.Show(text);
                this.label25.Text = "18";
            }
        }

        private void BarCode19_Click(object sender, EventArgs e)
        {
            string text = this.BarCode19.Text;
            if (text.StartsWith("NG") || this.BarCode19.BackColor == Color.Red)
            {
                MessageBox.Show(text);
                this.label25.Text = "19";
            }
        }

        private void BarCode20_Click(object sender, EventArgs e)
        {
            string text = this.BarCode20.Text;
            if (text.StartsWith("NG") || this.BarCode20.BackColor == Color.Red)
            {
                MessageBox.Show(text);
                this.label25.Text = "20";
            }
        }

        private void BarCode21_Click(object sender, EventArgs e)
        {
            string text = this.BarCode21.Text;
            if (text.StartsWith("NG") || this.BarCode21.BackColor == Color.Red)
            {
                MessageBox.Show(text);
                this.label25.Text = "21";
            }
        }

        private void BarCode22_Click(object sender, EventArgs e)
        {
            string text = this.BarCode22.Text;
            if (text.StartsWith("NG") || this.BarCode22.BackColor == Color.Red)
            {
                MessageBox.Show(text);
                this.label25.Text = "22";
            }
        }

        private void BarCode23_Click(object sender, EventArgs e)
        {
            string text = this.BarCode23.Text;
            if (text.StartsWith("NG") || this.BarCode23.BackColor == Color.Red)
            {
                MessageBox.Show(text);
                this.label25.Text = "23";
            }
        }

        private void BarCode24_Click(object sender, EventArgs e)
        {
            string text = this.BarCode24.Text;
            if (text.StartsWith("NG") || this.BarCode24.BackColor == Color.Red)
            {
                MessageBox.Show(text);
                this.label25.Text = "24";
            }
        }

        private void BarCode25_Click(object sender, EventArgs e)
        {
            string text = this.BarCode25.Text;
            if (text.StartsWith("NG") || this.BarCode25.BackColor == Color.Red)
            {
                MessageBox.Show(text);
                this.label25.Text = "25";
            }
        }

        private void BarCode26_Click(object sender, EventArgs e)
        {
            string text = this.BarCode26.Text;
            if (text.StartsWith("NG") || this.BarCode26.BackColor == Color.Red)
            {
                MessageBox.Show(text);
                this.label25.Text = "26";
            }
        }

        private void BarCode27_Click(object sender, EventArgs e)
        {
            string text = this.BarCode27.Text;
            if (text.StartsWith("NG") || this.BarCode27.BackColor == Color.Red)
            {
                MessageBox.Show(text);
                this.label25.Text = "27";
            }
        }

        private void BarCode28_Click(object sender, EventArgs e)
        {
            string text = this.BarCode28.Text;
            if (text.StartsWith("NG") || this.BarCode28.BackColor == Color.Red)
            {
                MessageBox.Show(text);
                this.label25.Text = "28";
            }
        }

        private void BarCode29_Click(object sender, EventArgs e)
        {
            string text = this.BarCode29.Text;
            if (text.StartsWith("NG") || this.BarCode29.BackColor == Color.Red)
            {
                MessageBox.Show(text);
                this.label25.Text = "29";
            }
        }

        private void BarCode30_Click(object sender, EventArgs e)
        {
            string text = this.BarCode30.Text;
            if (text.StartsWith("NG") || this.BarCode30.BackColor == Color.Red)
            {
                MessageBox.Show(text);
                this.label25.Text = "30";
            }
        }

        private void BarCode31_Click(object sender, EventArgs e)
        {
            string text = this.BarCode31.Text;
            if (text.StartsWith("NG") || this.BarCode31.BackColor == Color.Red)
            {
                MessageBox.Show(text);
                this.label25.Text = "31";
            }
        }

        private void BarCode32_Click(object sender, EventArgs e)
        {
            string text = this.BarCode32.Text;
            if (text.StartsWith("NG") || this.BarCode32.BackColor == Color.Red)
            {
                MessageBox.Show(text);
                this.label25.Text = "32";
            }
        }

        private void BarCode33_Click(object sender, EventArgs e)
        {
            string text = this.BarCode33.Text;
            if (text.StartsWith("NG") || this.BarCode33.BackColor == Color.Red)
            {
                MessageBox.Show(text);
                this.label25.Text = "33";
            }
        }

        private void BarCode34_Click(object sender, EventArgs e)
        {
            string text = this.BarCode34.Text;
            if (text.StartsWith("NG") || this.BarCode34.BackColor == Color.Red)
            {
                MessageBox.Show(text);
                this.label25.Text = "34";
            }
        }

        private void BarCode35_Click(object sender, EventArgs e)
        {
            string text = this.BarCode35.Text;
            if (text.StartsWith("NG") || this.BarCode35.BackColor == Color.Red)
            {
                MessageBox.Show(text);
                this.label25.Text = "35";
            }
        }

        private void BarCode36_Click(object sender, EventArgs e)
        {
            string text = this.BarCode36.Text;
            if (text.StartsWith("NG") || this.BarCode36.BackColor == Color.Red)
            {
                MessageBox.Show(text);
                this.label25.Text = "36";
            }
        }

        private void BarCode37_Click(object sender, EventArgs e)
        {
            string text = this.BarCode37.Text;
            if (text.StartsWith("NG") || this.BarCode37.BackColor == Color.Red)
            {
                MessageBox.Show(text);
                this.label25.Text = "37";
            }
        }

        private void BarCode38_Click(object sender, EventArgs e)
        {
            string text = this.BarCode38.Text;
            if (text.StartsWith("NG") || this.BarCode38.BackColor == Color.Red)
            {
                MessageBox.Show(text);
                this.label25.Text = "38";
            }
        }

        private void BarCode39_Click(object sender, EventArgs e)
        {
            string text = this.BarCode39.Text;
            if (text.StartsWith("NG") || this.BarCode39.BackColor == Color.Red)
            {
                MessageBox.Show(text);
                this.label25.Text = "39";
            }
        }

        private void BarCode40_Click(object sender, EventArgs e)
        {
            string text = this.BarCode40.Text;
            if (text.StartsWith("NG") || this.BarCode40.BackColor == Color.Red)
            {
                MessageBox.Show(text);
                this.label25.Text = "40";
            }
        }

        private void BarCode41_Click(object sender, EventArgs e)
        {
            string text = this.BarCode41.Text;
            if (text.StartsWith("NG") || this.BarCode41.BackColor == Color.Red)
            {
                MessageBox.Show(text);
                this.label25.Text = "41";
            }
        }

        private void BarCode42_Click(object sender, EventArgs e)
        {
            string text = this.BarCode42.Text;
            if (text.StartsWith("NG") || this.BarCode42.BackColor == Color.Red)
            {
                MessageBox.Show(text);
                this.label25.Text = "42";
            }
        }

        private void BarCode43_Click(object sender, EventArgs e)
        {
            string text = this.BarCode43.Text;
            if (text.StartsWith("NG") || this.BarCode43.BackColor == Color.Red)
            {
                MessageBox.Show(text);
                this.label25.Text = "43";
            }
        }

        private void BarCode44_Click(object sender, EventArgs e)
        {
            string text = this.BarCode44.Text;
            if (text.StartsWith("NG") || this.BarCode44.BackColor == Color.Red)
            {
                MessageBox.Show(text);
                this.label25.Text = "44";
            }
        }

        private void BarCode45_Click(object sender, EventArgs e)
        {
            string text = this.BarCode45.Text;
            if (text.StartsWith("NG") || this.BarCode45.BackColor == Color.Red)
            {
                MessageBox.Show(text);
                this.label25.Text = "45";
            }
        }

        private void BarCode46_Click(object sender, EventArgs e)
        {
            string text = this.BarCode46.Text;
            if (text.StartsWith("NG") || this.BarCode46.BackColor == Color.Red)
            {
                MessageBox.Show(text);
                this.label25.Text = "46";
            }
        }

        private void BarCode47_Click(object sender, EventArgs e)
        {
            string text = this.BarCode47.Text;
            if (text.StartsWith("NG") || this.BarCode47.BackColor == Color.Red)
            {
                MessageBox.Show(text);
                this.label25.Text = "47";
            }
        }

        private void BarCode48_Click(object sender, EventArgs e)
        {
            string text = this.BarCode48.Text;
            if (text.StartsWith("NG") || this.BarCode48.BackColor == Color.Red)
            {
                MessageBox.Show(text);
                this.label25.Text = "48";
            }
        }

        private void BarCode49_Click(object sender, EventArgs e)
        {
            string text = this.BarCode49.Text;
            if (text.StartsWith("NG") || this.BarCode49.BackColor == Color.Red)
            {
                MessageBox.Show(text);
                this.label25.Text = "49";
            }
        }

        private void BarCode50_Click(object sender, EventArgs e)
        {
            string text = this.BarCode50.Text;
            if (text.StartsWith("NG") || this.BarCode50.BackColor == Color.Red)
            {
                MessageBox.Show(text);
                this.label25.Text = "50";
            }
        }
        #endregion

        #region 人工补扫框回车事件
        /// <summary>
        /// 人工补扫框回车事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textBox2_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyValue == 13)
            {
                string ProductBarcode = this.textBox2.Text.Trim();

                int barcodeID = FailID[0];

                if (string.IsNullOrEmpty(ProductBarcode))
                {
                    this.textBox2.Text = "";
                    this.textBox2.Focus();
                    this.label3.Text = "空码，需要重新扫描";
                    this.label3.ForeColor = Color.Red;
                    return;
                }

                if (Msg == "校验")
                {
                    if (!CheckBarCodeStyle(ProductBarcode, BarCodeStyle))
                    {
                        this.textBox2.Text = "";
                        this.textBox2.Focus();
                        this.label3.Text = "条码样式不对，需要重新扫描";
                        this.label3.ForeColor = Color.Red;
                        return;
                    }
                }

                CodeByID[barcodeID] = ProductBarcode;

                sign(true, ProductBarcode, barcodeID.ToString());

                FailID.RemoveAt(0);

                if (FailID.Count > 0)
                {
                    this.textBox2.Text = "";
                    this.textBox2.Focus();
                    this.label3.Text = "NG产品补扫成功，当前还需补扫 " + FailID.Count;
                    this.label3.ForeColor = Color.Green;

                    SetFocusBtnColor(FailID[0]);
                }
                else
                {
                    //全部补码成功后解绑事件
                    this.textBox2.KeyDown -= new KeyEventHandler(this.textBox2_KeyDown);

                    SetFocusBtnColor(0);

                    this.textBox2.Text = "";
                    this.textBox2.Focus();
                    this.label25.Text = "0";
                    this.label3.Text = "NG产品全部补扫成功";
                    this.label3.ForeColor = Color.Green;

                    //检查重码
                    var SameID = CodeByID.GroupBy(x => x.Value)
                             .Where(g => g.Count() > 1)
                             .SelectMany(g => g)
                             .ToList();
                    if (SameID.Any())
                    {
                        foreach (var kvp in SameID)
                        {
                            sign(false, "重码", kvp.Key.ToString());
                        }

                        this.label3.Text = "有重复的阵列码，需要重新启动扫描";
                        this.label3.ForeColor = Color.Red;

                        this.KeyDown += new KeyEventHandler(this.HonorForm_KeyDown);
                    }
                    else
                    {
                        if (Msg == "校验")
                        {
                            Work_Run_Case = 5;
                        }
                        else if (Msg == "扫描")
                        {
                            Work_Run_Case = 3;
                        }
                    }

                    Msg = string.Empty;
                }
            }
        }
        #endregion

        #region MES打回 人工补扫框回车事件
        /// <summary>
        /// MES打回 人工补扫框回车事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textBox2_Repulse_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyValue == 13)
            {
                string workorder = ParameterSetClass.QueryHonorParameterInfo("工单号", "MES");
                string ItemNo = ParameterSetClass.QueryHonorParameterInfo("料号", "MES");
                string site = ParameterSetClass.QueryHonorParameterInfo("站点", "MES");
                string sitexuhao = ParameterSetClass.QueryHonorParameterInfo("站点序号", "MES");
                string Scanxuhao = ParameterSetClass.QueryHonorParameterInfo("扫描序号", "MES");
                string LineBody = ParameterSetClass.QueryHonorParameterInfo("线体", "MES");
                string ComputerHost = ParameterSetClass.QueryHonorParameterInfo("电脑主机", "MES");
                string UserID = ParameterSetClass.QueryHonorParameterInfo("工号", "MES");
                string PackagingType = ParameterSetClass.QueryHonorParameterInfo("包装类型", "MES");
                string CaseNumber = ParameterSetClass.QueryHonorParameterInfo("箱号", "MES");
                string IsHeavyIndustry = ParameterSetClass.QueryHonorParameterInfo("产品是否重工", "MES");
                string WXIsRepaired = ParameterSetClass.QueryHonorParameterInfo("产品是否维修品", "MES");

                string ProductBarcode = this.textBox2.Text.Trim();

                int barcodeID = FailID[0];

                if (!string.IsNullOrEmpty(ProductBarcode) && CheckBarCodeStyle(ProductBarcode, BarCodeStyle))
                {
                    string str = ws.SetSimplePackScan(ProductBarcode, workorder, ItemNo, LineBody, ComputerHost, UserID, site, sitexuhao, Scanxuhao, "1", CaseNumber, "", PackagingType, "N", WXIsRepaired, "N");

                    LogHelper.WriteLog($"{ProductBarcode} :MES返回结果: {str}");

                    if (str.StartsWith("OK"))
                    {
                        string UPNum = regex.Match(str).Value;

                        this.label19.Text = $"{UPNum} PCS";

                        sign(true, "已上传", barcodeID.ToString());

                        //保存已上传数量
                        ParameterSetClass.UpdateHonorData("当前箱已扫描数量", UPNum, "MES");

                        FailID.RemoveAt(0);

                        DalHelper.WriteData(ProductBarcode, CaseNumber, textBox1.Text.Trim(), null, str);

                        if (str.EndsWith("该箱已满,请扫描下一箱"))
                        {
                            SetFocusBtnColor(0);

                            this.textBox2.KeyDown -= new KeyEventHandler(this.textBox2_Repulse_KeyDown);

                            this.textBox3.Enabled = true;
                            this.textBox3.Text = "";
                            this.textBox3.Focus();
                            using (WaitForScanner waitForm = new WaitForScanner("该箱已满,请扫描荣耀PSN"))
                            {
                                if (waitForm.ShowDialog() == DialogResult.OK)
                                {
                                    string scannerText = waitForm.ScannerText;
                                    // 在这里可以使用返回的数据做其他操作
                                    textBox3.Text = scannerText;
                                    KeyEventArgs keyEventArgs = new KeyEventArgs(Keys.Enter);
                                    textBox3_KeyDown(textBox3, keyEventArgs);
                                }
                            }
                        }
                        else
                        {
                            if (FailID.Count > 0)
                            {
                                textBox2.Text = "";
                                this.textBox2.Focus();
                                this.label3.Text = "NG产品过站成功，当前还需补扫" + FailID.Count;
                                this.label3.ForeColor = Color.Green;
                                this.chengxujilu.AppendText($"【{DateTime.Now:HH:mm:ss:ffff}】产品过站成功：{ProductBarcode} MES返回结果:{str}" + Environment.NewLine);
                                SetFocusBtnColor(FailID[0]);
                            }
                            else
                            {
                                textBox2.Text = "";
                                this.textBox2.Focus();
                                this.label25.Text = "0";
                                this.label3.Text = "NG产品已全部过站完成";
                                this.label3.ForeColor = Color.Green;
                                this.chengxujilu.AppendText($"【{DateTime.Now:HH:mm:ss:ffff}】NG产品已全部过站完成：" + Environment.NewLine);

                                SetFocusBtnColor(0);
                                this.textBox2.KeyDown -= new KeyEventHandler(this.textBox2_Repulse_KeyDown);
                                Work_Run_Case = 7;
                            }
                        }
                    }
                    else
                    {
                        sign(false, str.Substring(str.IndexOf(' ') + 1), barcodeID.ToString());
                        this.label3.Text = "产品过站失败" + str;
                        this.label3.ForeColor = Color.Red;
                        this.textBox2.Text = "";
                        this.textBox2.Focus();
                        this.chengxujilu.AppendText($"【{DateTime.Now:HH:mm:ss:ffff}】产品过站失败：{ProductBarcode} MES返回结果: {str}" + Environment.NewLine);
                        LogHelper.WriteLog(($"【{DateTime.Now:HH:mm:ss:ffff}】产品过站失败：{ProductBarcode} MES返回结果: {str}" + Environment.NewLine));
                    }
                }
                else
                {
                    this.label3.Text = "未扫到码或者SN条码样式校验失败！！！";
                    this.label3.ForeColor = Color.Red;
                    this.textBox2.Text = "";
                    this.textBox2.Focus();
                    this.chengxujilu.AppendText($"【{DateTime.Now:HH:mm:ss:ffff}】未扫到码或者SN条码样式校验失败！！！" + Environment.NewLine);
                }
            }
        }
        #endregion

        /// <summary>
        /// 定时删除日志和保存的图片
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timer2_Tick(object sender, EventArgs e)
        {
            //DalHelper.DELETEData();
            //CleanImageDirectory(Path.Combine(RenderImageSavePath, "OK", 2);
            CleanDirectory(Path.Combine(RenderImageSavePath, "NG"), 2);
            CleanDirectory(Path.Combine(Path.GetPathRoot(AppDomain.CurrentDomain.BaseDirectory), "日志"), 7);
        }

        private void pictureBox4_Click(object sender, EventArgs e)
        {
            BoxDataForm boxDataForm = new BoxDataForm("HONOR");
            boxDataForm.ShowDialog();
        }

        #region 弹窗密码相关
        string WarnFormPasswordEnc = "";
        bool WarnFormPasswordEnabled = false;
        /// <summary>
        /// 弹出报警框
        /// </summary>
        private void GetWarnFormPassword()
        {
            string sWarnFormPasswordEnabled = ParameterSetClass.QueryHonorParameterInfo("允许报警处理密码", "系统");
            if(string.IsNullOrEmpty(sWarnFormPasswordEnabled) || sWarnFormPasswordEnabled != "Y")
            {
                WarnFormPasswordEnabled = false;
            }
            else
            {
                WarnFormPasswordEnabled = true;

                string wfPasswordEnc = ParameterSetClass.QueryHonorParameterInfo("报警处理密码", "系统");
                if (!string.IsNullOrEmpty(wfPasswordEnc))
                {
                    WarnFormPasswordEnc = wfPasswordEnc;
                }
                else
                {
                    WarnFormPasswordEnc = CryptInfoNameSpace.CryptInfoNameSpace.Encrypt(HonorCanShuForm.defaultAPPass);
                }
            }
        }
        #endregion

        /// <summary>
        /// 显示VPP界面
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void uiButton2_Click(object sender, EventArgs e)
        {
            HonorVppShow.GetInstance().Show();
        }

        /// <summary>
        /// 光源控制
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnLightControl_Click(object sender, EventArgs e)
        {
            //检测是否存在COM3
            if (UseLight)
            {
                if (btnLightControl.Text == "打开光源")
                {
                    btnLightControl.Text = "关闭光源";

                    //打开光源
                    byte[] OnLight1 = { 0xa5, 0x00, 0x01, 0x04, 0x64, 0x00, 0x01, 0x00, 0x01, 0x00, 0x00, 0x00 };
                    LightPort.WriteHex(OnLight1);
                    byte[] OnLight2 = { 0xa5, 0x00, 0x03, 0x04, 0x64, 0x00, 0x01, 0x00, 0x01, 0x00, 0x00, 0x00 };
                    LightPort.WriteHex(OnLight2);
                }
                else
                {
                    btnLightControl.Text = "打开光源";

                    //关闭光源
                    byte[] OffLight1 = { 0xa5, 0x00, 0x01, 0x04, 0x64, 0x00, 0x02, 0x00, 0x01, 0x00, 0x00, 0x00 };
                    LightPort.WriteHex(OffLight1);
                    byte[] OffLight2 = { 0xa5, 0x00, 0x03, 0x04, 0x64, 0x00, 0x02, 0x00, 0x01, 0x00, 0x00, 0x00 };
                    LightPort.WriteHex(OffLight2);
                }
            }
        }

        /// <summary>
        /// 手动运行视觉扫码
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void uiButton1_Click(object sender, EventArgs e)
        {
            ResetPackageData();
            VPPRun();
        }

        /// <summary>
        /// 返回桌面按钮
        /// </summary>
        private void uiButton3_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        /// <summary>
        /// 软件重启按钮
        /// </summary>
        private void uiButton4_Click(object sender, EventArgs e)
        {
            LogHelper.WriteLog("软件重新初始化");
            //开启新的程序
            Process.Start(Application.ExecutablePath);
            //关闭当前实例
            Process.GetCurrentProcess().Kill();
        }

        /// <summary>
        /// 电脑关机按钮
        /// </summary>
        private void uiButton5_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("是否确认电脑关机", "电脑关机", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
            if (result == DialogResult.OK)
            {
                LogHelper.WriteLog("电脑关机");
                //关闭所有串口
                LightPort.Close();
                LogHelper.Close();
                try
                {
                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = "shutdown",
                        Arguments = "/s /t 10",  // 10秒后关机
                        CreateNoWindow = true,   // 不创建窗口
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        WindowStyle = ProcessWindowStyle.Hidden // 隐藏窗口
                    };

                    Process.Start(psi);
                }
                catch { }

                this.Dispose();
                objProcess.Kill();
            }
        }

        /// <summary>
        /// 维修品文字显示
        /// </summary>
        /// <param name="form"></param>
        public static void WXBMessage(HonorForm form)  
        {
            form.label29.Visible = ParameterSetClass.QueryHonorParameterInfo("产品是否维修品", "MES") == "Y";
        }
    }
}
