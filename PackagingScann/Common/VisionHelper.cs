using System;
using System.Net;
using System.Text;
using TouchSocket.Core;
using TouchSocket.Sockets;

#region << 版 本 注 释 >>
/*----------------------------------------------------------------
 * 版权所有 (c)  2023 BYENCE  保留所有权利。
 * CLR版本：4.0.30319.42000
 * 公司名称：BYENCE
 * 命名空间：PackagingScann.Common
 * 唯一标识：7d3593ae-f6eb-477a-85ff-5374438752b7
 * 文件名：TCPSeviceHelper
 * 
 * 创建者：JasonHong@BYENCE
 * 电子邮箱：wenxiaofeng@byence.cn
 * 创建时间：2023/11/3 15:05:21
 * 版本：V1.0.0
 * 描述：
 *----------------------------------------------------------------*/
#endregion << 版 本 注 释 >>
namespace PackagingScann.Common
{
    public class VisionHelper
    {
        public TcpClient tcpClient = new TcpClient();
        public WaitData waitData = new WaitData();
        public VisionHelper(String IPAddress,int Port)
        {
                tcpClient.Setup(new TouchSocketConfig()
                .SetRemoteIPHost($"{IPAddress}:{Port}")
                .SetTcpDataHandlingAdapter(() => new TerminatorPackageAdapter("\r\n")));//载入配置
                tcpClient.ConnectAsync();//连接
        }
        public void SendData()
        {
            tcpClient.Send("1");
        }
    }

    public enum ScannerStatus
    {   
        InitForm,   //程序启动
        Connection, //通讯验证
        Success,    //待机状态
        BeginWork,  //运行状态
        Recevied,   //接收代码
        Verifying,  //条码校验
        Complement, //补码状态
        Sending,    //发送 MES
        Repulse,    //数据打回
        Passed,     //发送通过

    }
}
