using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace HVisionC
{
    public static class LightPort
    {
        public static SerialPort Port { get; private set; }

        //public static event Action<string> OnDataReceived;

        public static void Init(
            string portName,
            int baudRate,
            Parity parity = Parity.None,
            int dataBits = 8,
            StopBits stopBits = StopBits.One)
        {
            if (Port != null)
            {
                if (Port.IsOpen) Port.Close();
                Port.Dispose();
            }

            Port = new SerialPort(portName, baudRate, parity, dataBits, stopBits)
            {
                Handshake = Handshake.None,
                ReadTimeout = 500,
                WriteTimeout = 500
            };
        }

        //打开串口
        public static bool Open()
        {
            try
            {
                if (Port != null && !Port.IsOpen)
                    Port.Open();
                return true;
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show("打开串口失败: " + ex.Message);
                return false;
            }
        }

        // 关闭串口
        public static void Close()
        {
            if (Port != null && Port.IsOpen)
                Port.Close();
        }

        // 写字符串
        public static void Write(string data)
        {
            if (Port != null && Port.IsOpen)
                Port.Write(data);
        }

        // 写十六进制
        public static void WriteHex(byte[] data)
        {
            if (Port.IsOpen)
            {
                Port.Write(data, 0, data.Length);
            }  
        }
    }
}
