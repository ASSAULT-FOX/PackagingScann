using Sunny.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using TouchSocket.Core;

namespace PackagingScann.Common
{
    public class LogHelper
    {
        public static string FilePath = Path.Combine(Path.GetPathRoot(AppDomain.CurrentDomain.BaseDirectory), "日志");
        public static string LogPath = string.Empty;
        public static string DirPath = string.Empty;
        public static string logEntry = string.Empty;

        private static readonly object _lock = new object();
        private static StreamWriter _StreamWriter;
        private static string _currentDate;
        private static readonly StringBuilder _StringBuilder = new StringBuilder();

        //public static readonly log4net.ILog loginfo = log4net.LogManager.GetLogger("loginfo");//这里的 loginfo 和 log4net.config 里的名字要一样
        //public static readonly log4net.ILog logerror = log4net.LogManager.GetLogger("logerror");//这里的 logerror 和 log4net.config 里的名字要一样
        public static void WriteLog(string info)
        {
            var now = DateTime.Now;
            var dateStr = now.ToString("yyyy-MM-dd");

            _StringBuilder.Clear();
            _StringBuilder.Append(now.ToString("HH:mm:ss")).Append(">>>").Append(info).Append("\r\n");
            logEntry = _StringBuilder.ToString();

            lock (_lock)
            {
                if (_currentDate != dateStr || _StreamWriter == null)
                {
                    _currentDate = dateStr;
                    DirPath = Path.Combine(FilePath, dateStr);
                    LogPath = Path.Combine(DirPath, "ErrorLog.txt");

                    _StreamWriter?.Dispose();

                    if (!Directory.Exists(DirPath))
                        Directory.CreateDirectory(DirPath);

                    _StreamWriter = new StreamWriter(LogPath, true, Encoding.UTF8, 65536); 
                }

                _StreamWriter.Write(logEntry);
                _StreamWriter.Flush();
            }
        }

        public static void Close()
        {
            lock (_lock)
            {
                _StreamWriter?.Flush();
                _StreamWriter?.Dispose();
                _StreamWriter = null;
            }
        }

        //public static void WriteLog(string info, Exception ex)
        //{
        //    if (logerror.IsErrorEnabled)
        //    {
        //        logerror.Error(info, ex);
        //    }
        //}
    }
}
