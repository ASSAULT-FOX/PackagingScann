using Cognex.VisionPro;
using PackagingScann.Common;
using Sunny.UI;
using System;
using System.IO;
using System.Windows.Forms;
using TransformerData;

namespace CheckUSBAOI
{
    public partial class HonorVppShow : UIForm
    {
        public static HonorVppShow thisForm;

        public HonorVppShow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 单例模式(同时只允许打开一个窗口)
        /// </summary>
        /// <param name="isActive">是否触发窗体</param>
        public static HonorVppShow GetInstance(bool isActive = true)
        {
            //是否自动创建 不自动创建时
            if (thisForm == null || thisForm.IsDisposed)
            {
                //窗口首次打开 或者 被关闭后再次打开
                thisForm = new HonorVppShow();
            }
            else
            {
                if (isActive)
                {
                    //窗体设置焦点
                    thisForm.Activate();
                    //正常显示窗体
                    thisForm.WindowState = FormWindowState.Normal;
                }
            }
            return thisForm;
        }

        //当前调试的事件程序路径
        public string debugVppPath = "";

        /// <summary>
        /// 窗体初始化事件
        /// </summary>
        private void VPPDebug_Load(object sender, EventArgs e)
        {
            string VPPName = $"{ParameterSetClass.QueryHonorParameterInfo("物料编码", "视觉")}.vpp";
            debugVppPath = Path.Combine(Application.StartupPath, "Vpp", VPPName);
            toolBlockEdit.Subject = HonorForm.ScanCode1_Block;
        }

        //保存vpp
        private void uiSymbolButton5_Click(object sender, EventArgs e)
        {
            try
            {
                if (toolBlockEdit.Subject != null)
                {
                    CogSerializer.SaveObjectToFile(toolBlockEdit.Subject, debugVppPath);
                    ShowSuccessNotifier("保存成功");
                }
            }
            catch
            {

            }
        }
    }
}
