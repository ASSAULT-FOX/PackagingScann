using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;

namespace PackagingScann.Common
{
    public class ParameterSetClass
    {
        public static string QueryHuaweiParameterInfo(string ParameterName, string ParameterType)
        {
            DataTable dt = new DataTable();
            string config = System.Configuration.ConfigurationManager.ConnectionStrings["sqlite"].ConnectionString;
            using (SQLiteConnection conn = new SQLiteConnection(config))
            {
                conn.Open();
                string sql = @"SELECT FN_ID,
                                   ParameterName,
                                   ParameterValue,
                                   ParameterType,
                                   UpdateTime
                              FROM HUAWEI_ParameterSet
                              WHERE ParameterName='" + ParameterName + "' AND ParameterType='" + ParameterType + "'";

                using (SQLiteDataAdapter ap = new SQLiteDataAdapter(sql, conn))
                {
                    ap.Fill(dt);
                }
            }
            if (dt.Rows.Count > 0)
            {
                return dt.Rows[0]["ParameterValue"].ToString().Trim();
            }
            else
            {
                return null;
            }
        }

        public static void UpdateHuaweiData(string ParameterName, string ParameterValue, string ParameterType)
        {
            string config = System.Configuration.ConfigurationManager.ConnectionStrings["sqlite"].ConnectionString;
            SQLiteConnection conn = new SQLiteConnection(config);
            conn.Open();
            try
            {
                SQLiteCommand cmd = new SQLiteCommand();
                cmd.Connection = conn;
                cmd.CommandText = @"UPDATE HUAWEI_ParameterSet
                                       SET ParameterValue = @ParameterValue,
                                           UpdateTime = @UpdateTime
                                     WHERE   ParameterName =@ParameterName AND
                                           ParameterType = @ParameterType;";
                cmd.Parameters.Add("ParameterName", DbType.String).Value = ParameterName;
                cmd.Parameters.Add("ParameterValue", DbType.String).Value = ParameterValue;
                cmd.Parameters.Add("ParameterType", DbType.String).Value = ParameterType;
                cmd.Parameters.Add("UpdateTime", DbType.DateTime).Value = DateTime.Now;
                cmd.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                throw e;
            }
            finally
            {
                if (conn != null) conn.Close();
            }
        }

        //yangk 240819 修改数据库增加 修改或插入 的新版方法
        public static int UpdateHuaweiData(string ParameterName, string ParameterValue, string ParameterType, bool InsertWhenNonExist)
        {
            int r = 0;
            string config = System.Configuration.ConfigurationManager.ConnectionStrings["sqlite"].ConnectionString;
            SQLiteConnection conn = new SQLiteConnection(config);
            conn.Open();
            try
            {
                SQLiteCommand cmd = new SQLiteCommand();
                cmd.Connection = conn;
                cmd.CommandText = @"UPDATE HUAWEI_ParameterSet
                                       SET ParameterValue = @ParameterValue,
                                           UpdateTime = @UpdateTime
                                     WHERE   ParameterName =@ParameterName AND
                                           ParameterType = @ParameterType;";
                cmd.Parameters.Add("ParameterName", DbType.String).Value = ParameterName;
                cmd.Parameters.Add("ParameterValue", DbType.String).Value = ParameterValue;
                cmd.Parameters.Add("ParameterType", DbType.String).Value = ParameterType;
                cmd.Parameters.Add("UpdateTime", DbType.DateTime).Value = DateTime.Now;
                r = cmd.ExecuteNonQuery();

                if (r < 1 && InsertWhenNonExist)
                {
                    cmd.Connection = conn;
                    cmd.CommandText = @"INSERT INTO HUAWEI_ParameterSet (ParameterName, ParameterType, ParameterValue, UpdateTime)
                                       VALUES (@ParameterName, @ParameterType, @ParameterValue, @UpdateTime);";
                    cmd.Parameters.Add("ParameterName", DbType.String).Value = ParameterName;
                    cmd.Parameters.Add("ParameterType", DbType.String).Value = ParameterType;
                    cmd.Parameters.Add("ParameterValue", DbType.String).Value = ParameterValue;
                    cmd.Parameters.Add("UpdateTime", DbType.DateTime).Value = DateTime.Now;
                    r = cmd.ExecuteNonQuery();
                }
            }
            catch (Exception e)
            {
                throw e;
            }
            finally
            {
                if (conn != null) conn.Close();
            }
            return r;
        }

        public static string QueryHonorParameterInfo(string ParameterName, string ParameterType)
        {
            DataTable dt = new DataTable();
            string config = System.Configuration.ConfigurationManager.ConnectionStrings["sqlite"].ConnectionString;
            using (SQLiteConnection conn = new SQLiteConnection(config))
            {
                conn.Open();
                string sql = @"SELECT FN_ID,
                                   ParameterName,
                                   ParameterValue,
                                   ParameterType,
                                   UpdateTime
                              FROM HONOR_ParameterSet
                              WHERE ParameterName='" + ParameterName + "' AND ParameterType='" + ParameterType + "'";

                using (SQLiteDataAdapter ap = new SQLiteDataAdapter(sql, conn))
                {
                    ap.Fill(dt);
                }
            }
            if (dt.Rows.Count > 0)
            {
                return dt.Rows[0]["ParameterValue"].ToString().Trim();
            }
            else
            {
                return null;
            }
        }

        public static void UpdateHonorData(string ParameterName, string ParameterValue, string ParameterType)
        {
            string config = System.Configuration.ConfigurationManager.ConnectionStrings["sqlite"].ConnectionString;
            SQLiteConnection conn = new SQLiteConnection(config);
            conn.Open();
            try
            {
                SQLiteCommand cmd = new SQLiteCommand();
                cmd.Connection = conn;
                cmd.CommandText = @"UPDATE HONOR_ParameterSet
                                       SET ParameterValue = @ParameterValue,
                                           UpdateTime = @UpdateTime
                                     WHERE   ParameterName =@ParameterName AND
                                           ParameterType = @ParameterType;";
                cmd.Parameters.Add("ParameterName", DbType.String).Value = ParameterName;
                cmd.Parameters.Add("ParameterValue", DbType.String).Value = ParameterValue;
                cmd.Parameters.Add("ParameterType", DbType.String).Value = ParameterType;
                cmd.Parameters.Add("UpdateTime", DbType.DateTime).Value = DateTime.Now;
                cmd.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                throw e;
            }
            finally
            {
                if (conn != null) conn.Close();
            }
        }

        //yangk 240819 修改数据库增加 修改或插入 的新版方法
        public static int UpdateHonorData(string ParameterName, string ParameterValue, string ParameterType, bool InsertWhenNonExist)
        {
            int r = 0;
            string config = System.Configuration.ConfigurationManager.ConnectionStrings["sqlite"].ConnectionString;
            SQLiteConnection conn = new SQLiteConnection(config);
            conn.Open();
            try
            {
                SQLiteCommand cmd = new SQLiteCommand();
                cmd.Connection = conn;
                cmd.CommandText = @"UPDATE HONOR_ParameterSet
                                       SET ParameterValue = @ParameterValue,
                                           UpdateTime = @UpdateTime
                                     WHERE   ParameterName =@ParameterName AND
                                           ParameterType = @ParameterType;";
                cmd.Parameters.Add("ParameterName", DbType.String).Value = ParameterName;
                cmd.Parameters.Add("ParameterValue", DbType.String).Value = ParameterValue;
                cmd.Parameters.Add("ParameterType", DbType.String).Value = ParameterType;
                cmd.Parameters.Add("UpdateTime", DbType.DateTime).Value = DateTime.Now;
                r = cmd.ExecuteNonQuery();

                if (r < 1 && InsertWhenNonExist)
                {
                    cmd.Connection = conn;
                    cmd.CommandText = @"INSERT INTO HONOR_ParameterSet (ParameterName, ParameterType, ParameterValue, UpdateTime)
                                       VALUES (@ParameterName, @ParameterType, @ParameterValue, @UpdateTime);";
                    cmd.Parameters.Add("ParameterName", DbType.String).Value = ParameterName;
                    cmd.Parameters.Add("ParameterType", DbType.String).Value = ParameterType;
                    cmd.Parameters.Add("ParameterValue", DbType.String).Value = ParameterValue;
                    cmd.Parameters.Add("UpdateTime", DbType.DateTime).Value = DateTime.Now;
                    r = cmd.ExecuteNonQuery();
                }
            }
            catch (Exception e)
            {
                throw e;
            }
            finally
            {
                if (conn != null) conn.Close();
            }
            return r;
        }

        public static void HonorTaskTimeCheck()
        {
            String taskTime = (ParameterSetClass.QueryHonorParameterInfo("TaskTime", "系统"));
            Console.WriteLine(taskTime);
            if (taskTime != null)
            {
                DateTime taskDateTime = DateTime.Parse(taskTime);
                TimeSpan timeDifference = DateTime.Now - taskDateTime;

                if (timeDifference.TotalMilliseconds >= 7 * 24 * 60 * 60 * 1000)
                {
                    DalHelper.ClearHONORBox();
                    DalHelper.DELETEData();
                    // 更新 TaskTime 的值为当前时间
                    ParameterSetClass.UpdateHonorData("TaskTime", DateTime.Now.ToString(), "系统");
                }
                
            }
            else
            {
                // 如果TaskTime为空，更新为当前时间
                ParameterSetClass.UpdateHonorData("TaskTime", DateTime.Now.ToString(), "系统");
            }

        }
        public static void HuaweiTaskTimeCheck()
        {
            String taskTime = (ParameterSetClass.QueryHuaweiParameterInfo("TaskTime", "系统"));
            Console.WriteLine(taskTime);
            if (taskTime != null)
            {
                DateTime taskDateTime = DateTime.Parse(taskTime);
                TimeSpan timeDifference = DateTime.Now - taskDateTime;

                if (timeDifference.TotalMilliseconds >= 7 * 24 * 60 * 60 * 1000)
                {
                    DalHelper.ClearHUAWEIBox();
                    DalHelper.DELETEData();
                    // 更新 TaskTime 的值为当前时间
                    ParameterSetClass.UpdateHuaweiData("TaskTime", DateTime.Now.ToString(), "系统");
                }

            }
            else
            {
                // 如果TaskTime为空，更新为当前时间
                ParameterSetClass.UpdateHuaweiData("TaskTime", DateTime.Now.ToString(), "系统");
            }

        }
    }
}
