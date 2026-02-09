using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;

namespace PackagingScann.Common
{
    public class DalHelper
    {
        private static SQLiteConnection CreateConnection()
        {
            string config = System.Configuration.ConfigurationManager.ConnectionStrings["sqlite"].ConnectionString;
            return new SQLiteConnection(config);
        }

        #region 将原始数据存入SQLite数据库
        public static void WriteData(string ProductSN, string CaseNumber, string OuterBoxSN, string HuaWeiSN, string MesResult)
        {
            using (SQLiteConnection conn = CreateConnection())
            {
                conn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandText = @"INSERT INTO ProductionData (
                               ProductSN,
                               CaseNumber,
                               OuterBoxSN,
                               HuaWeiSN,
                               MesResult,
                               UpdateTime
                           )
                           VALUES (
                               @ProductSN,
                               @CaseNumber,
                               @OuterBoxSN,
                               @HuaWeiSN,
                               @MesResult,
                               @UpdateTime
                           );";

                    cmd.Parameters.Add("ProductSN", DbType.String).Value = ProductSN;
                    cmd.Parameters.Add("CaseNumber", DbType.String).Value = CaseNumber;
                    cmd.Parameters.Add("OuterBoxSN", DbType.String).Value = OuterBoxSN;
                    cmd.Parameters.Add("HuaWeiSN", DbType.String).Value = HuaWeiSN;
                    cmd.Parameters.Add("MesResult", DbType.String).Value = MesResult;
                    cmd.Parameters.Add("UpdateTime", DbType.DateTime).Value = DateTime.Now;
                    cmd.ExecuteNonQuery();
                }
            }
        }
        #endregion

        #region 查询测试原始数据
        public static DataTable GetDataInfo()
        {
            DataTable dt = new DataTable();
            using (SQLiteConnection conn = CreateConnection())
            {
                conn.Open();
                string sql = @"SELECT  ProductSN AS 产品条码,
       CaseNumber AS 箱号,
       OuterBoxSN AS 外箱条码,
       HuaWeiSN AS 华为条码,
       MesResult MES结果,
       UpdateTime AS 更新时间
  FROM ProductionData
   ORDER BY FN_ID DESC limit 0,1000;
";
                using (SQLiteDataAdapter ap = new SQLiteDataAdapter(sql, conn))
                {
                    ap.Fill(dt);
                }
            }
            return dt;
        }
        #endregion

        #region 根据SN查询测试原始数据
        public static DataTable QueryDataInfo(string ProductSN, string CaseNumber, string MesResult)
        {
            DataTable dt = new DataTable();
            using (SQLiteConnection conn = CreateConnection())
            {
                conn.Open();
                string sql = @"SELECT  ProductSN AS 产品条码,
       CaseNumber AS 箱号,
       OuterBoxSN AS 外箱条码,
       HuaWeiSN AS 华为条码,
       MesResult MES结果,
       UpdateTime AS 更新时间
  FROM ProductionData
  WHERE 1=1";
                var parameters = new List<SQLiteParameter>();

                if (!string.IsNullOrEmpty(ProductSN))
                {
                    sql += " AND ProductSN LIKE @ProductSN";
                    parameters.Add(new SQLiteParameter("@ProductSN", $"%{ProductSN}%"));
                }
                if (!string.IsNullOrEmpty(CaseNumber))
                {
                    sql += " AND CaseNumber LIKE @CaseNumber";
                    parameters.Add(new SQLiteParameter("@CaseNumber", $"%{CaseNumber}%"));
                }
                if (!string.IsNullOrEmpty(MesResult))
                {
                    sql += " AND MesResult LIKE @MesResult";
                    parameters.Add(new SQLiteParameter("@MesResult", $"%{MesResult}%"));
                }
                sql += " ORDER BY FN_ID DESC limit 0,1000;";
                using (SQLiteCommand cmd = new SQLiteCommand(sql, conn))
                {
                    if (parameters.Count > 0)
                    {
                        cmd.Parameters.AddRange(parameters.ToArray());
                    }
                    using (SQLiteDataAdapter ap = new SQLiteDataAdapter(cmd))
                    {
                        ap.Fill(dt);
                    }
                }
            }
            return dt;
        }

        #endregion

        public static void WriteLogInfo(string MessageInfo)
        {
            using (SQLiteConnection conn = CreateConnection())
            {
                conn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandText = @"INSERT INTO SystemLog (
                              MessageInfo,UpdateTime 
                          )
                          VALUES (
                              @MessageInfo,@UpdateTime
                          );";
                    cmd.Parameters.Add("MessageInfo", DbType.String).Value = MessageInfo;
                    cmd.Parameters.Add("UpdateTime", DbType.DateTime).Value = DateTime.Now;
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static void DELETEData()
        {
            using (SQLiteConnection conn = CreateConnection())
            {
                conn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandText = @"DELETE FROM ProductionData WHERE date('now', '-7 day') > date(UpdateTime);DELETE FROM SystemLog WHERE date('now', '-7 day') > date(UpdateTime);";
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static void InsertHonorBox(String code)
        {
            using (SQLiteConnection conn = CreateConnection())
            {
                conn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandText = @"INSERT INTO HONOR_BOX (
                              Code,
                              UpdateTime 
                          )
                          VALUES (
                              @Code,
                              @UpdateTime
                          );";
                    cmd.Parameters.Add("Code", DbType.String).Value = code;
                    cmd.Parameters.Add("UpdateTime", DbType.DateTime).Value = DateTime.Now;
                    cmd.ExecuteNonQuery();
                }
            }
        }
        public static void InsertHuaweiBox(String code)
        {
            using (SQLiteConnection conn = CreateConnection())
            {
                conn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandText = @"INSERT INTO HUAWEI_BOX (
                              Code,
                              UpdateTime 
                          )
                          VALUES (
                              @Code,
                              @UpdateTime
                          );";
                    cmd.Parameters.Add("Code", DbType.String).Value = code;
                    cmd.Parameters.Add("UpdateTime", DbType.DateTime).Value = DateTime.Now;
                    cmd.ExecuteNonQuery();
                }
            }
        }
        
        public static void InsertHuaweiASN(String code)
        {
            using (SQLiteConnection conn = CreateConnection())
            {
                conn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandText = @"INSERT INTO HUAWEI_ASN (
                              Code,
                              UpdateTime 
                          )
                          VALUES (
                              @Code,
                              @UpdateTime
                          );";
                    cmd.Parameters.Add("Code", DbType.String).Value = code;
                    cmd.Parameters.Add("UpdateTime", DbType.DateTime).Value = DateTime.Now;
                    cmd.ExecuteNonQuery();
                }
            }
        }


        public static DataTable QueryHUAWEIBox(String Code)
        {
            DataTable dt = new DataTable();
            using (SQLiteConnection conn = CreateConnection())
            {
                conn.Open();
                string sql = @"SELECT  Code AS 外箱条码,
                               UpdateTime AS 更新时间
                              FROM HUAWEI_BOX
                              WHERE 1=1";

                if (!string.IsNullOrEmpty(Code))
                {
                    sql += " AND Code LIKE @Code";
                }
                using (SQLiteCommand cmd = new SQLiteCommand(sql, conn))
                {
                    if (!string.IsNullOrEmpty(Code))
                    {
                        cmd.Parameters.AddWithValue("@Code", $"%{Code}%");
                    }
                    using (SQLiteDataAdapter ap = new SQLiteDataAdapter(cmd))
                    {
                        ap.Fill(dt);
                    }
                }
            }
            return dt;
        }
        public static DataTable QueryHUAWEIASN(String Code)
        {
            DataTable dt = new DataTable();
            using (SQLiteConnection conn = CreateConnection())
            {
                conn.Open();
                string sql = @"SELECT  Code AS 华为条码,
                               UpdateTime AS 更新时间
                              FROM HUAWEI_ASN
                              WHERE 1=1";

                if (!string.IsNullOrEmpty(Code))
                {
                    sql += " AND Code LIKE @Code";
                }
                using (SQLiteCommand cmd = new SQLiteCommand(sql, conn))
                {
                    if (!string.IsNullOrEmpty(Code))
                    {
                        cmd.Parameters.AddWithValue("@Code", $"%{Code}%");
                    }
                    using (SQLiteDataAdapter ap = new SQLiteDataAdapter(cmd))
                    {
                        ap.Fill(dt);
                    }
                }
            }
            return dt;
        }
        public static DataTable QueryHONORBox(String Code)
        {
            DataTable dt = new DataTable();
            using (SQLiteConnection conn = CreateConnection())
            {
                conn.Open();
                string sql = @"SELECT  Code AS 外箱条码,
                               UpdateTime AS 更新时间
                              FROM HONOR_BOX
                              WHERE 1=1";

                if (!string.IsNullOrEmpty(Code))
                {
                    sql += " AND Code LIKE @Code";
                }
                using (SQLiteCommand cmd = new SQLiteCommand(sql, conn))
                {
                    if (!string.IsNullOrEmpty(Code))
                    {
                        cmd.Parameters.AddWithValue("@Code", $"%{Code}%");
                    }
                    using (SQLiteDataAdapter ap = new SQLiteDataAdapter(cmd))
                    {
                        ap.Fill(dt);
                    }
                }
            }
            return dt;
        }

        public static bool CheckHonorBox(String code)
        {
            using (SQLiteConnection conn = CreateConnection())
            {
                conn.Open();
                string sql = @"SELECT COUNT(*) FROM HONOR_BOX WHERE Code = @Code";

                using (SQLiteCommand cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Code", code);
                    int count = Convert.ToInt32(cmd.ExecuteScalar());

                    if (count > 0)
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
            }
        }
        public static bool CheckHuaweiBox(String code)
        {
            using (SQLiteConnection conn = CreateConnection())
            {
                conn.Open();
                string sql = @"SELECT COUNT(*) FROM HUAWEI_BOX WHERE Code = @Code";

                using (SQLiteCommand cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Code", code);
                    int count = Convert.ToInt32(cmd.ExecuteScalar());

                    if (count > 0)
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
            }
        }
        public static bool CheckHuaweiASN(String code)
        {
            using (SQLiteConnection conn = CreateConnection())
            {
                conn.Open();
                string sql = @"SELECT COUNT(*) FROM HUAWEI_ASN WHERE Code = @Code";

                using (SQLiteCommand cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Code", code);
                    int count = Convert.ToInt32(cmd.ExecuteScalar());

                    if (count > 0)
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
            }
        }

        public static bool DeleteHUAWEIBox(String code)
        {
            using (SQLiteConnection conn = CreateConnection())
            {
                conn.Open();
                string sql = @"DELETE FROM HUAWEI_BOX WHERE Code = @Code;";

                using (SQLiteCommand cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Code", code);
                    cmd.ExecuteNonQuery();
                    return true;
                }
            }
        }
        public static bool DeleteHUAWEIASN(String code)
        {
            using (SQLiteConnection conn = CreateConnection())
            {
                conn.Open();
                string sql = @"DELETE FROM HUAWEI_ASN WHERE Code = @Code;";

                using (SQLiteCommand cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Code", code);
                    cmd.ExecuteNonQuery();
                    return true;
                }
            }
        }
        public static bool DeleteHONORBox(String code)
        {
            using (SQLiteConnection conn = CreateConnection())
            {
                conn.Open();
                string sql = @"DELETE FROM HONOR_BOX WHERE Code = @Code;";

                using (SQLiteCommand cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Code", code);
                    cmd.ExecuteNonQuery();
                    return true;
                }
            }
        }

        public static bool ClearHONORBox()
        {
            using (SQLiteConnection conn = CreateConnection())
            {
                conn.Open();
                string sql = @"DELETE FROM HONOR_BOX;";

                using (SQLiteCommand cmd = new SQLiteCommand(sql, conn))
                {
                    int count = Convert.ToInt32(cmd.ExecuteNonQuery());
                    return true;
                }
            }
        }
        public static bool ClearHUAWEIBox()
        {
            using (SQLiteConnection conn = CreateConnection())
            {
                conn.Open();

                using (SQLiteCommand cmd = new SQLiteCommand(@"DELETE FROM HUAWEI_BOX;", conn))
                {
                    int count = Convert.ToInt32(cmd.ExecuteNonQuery());
                }

                using (SQLiteCommand cmd = new SQLiteCommand(@"DELETE FROM HUAWEI_ASN;", conn))
                {
                    int count = Convert.ToInt32(cmd.ExecuteNonQuery());

                }
                return true;
            }
        }

        /// <summary>
        /// 删除这个箱号的全部SN记录
        /// </summary>
        /// <param name="code"></param>
        public static void DelBoxSN(string code)
        {
            using (var conn = CreateConnection())
            using (var cmd = new SQLiteCommand("DELETE FROM ProductionData WHERE CaseNumber = @CaseNumber", conn))
            {
                conn.Open();
                cmd.Parameters.AddWithValue("@CaseNumber", code);
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// 检查已存在的SN（只统计以OK开头的记录）
        /// </summary>
        public static HashSet<string> CheckExistingProductSN(string caseNumber, List<string> productSNs)
        {
            if (productSNs == null || productSNs.Count == 0)
            {
                return new HashSet<string>();
            }

            string inClause = string.Join(",", productSNs.Select((_, i) => $"@p{i}"));

            string sql = $@"
                        SELECT ProductSN 
                        FROM ProductionData 
                        WHERE CaseNumber = @caseNumber 
                        AND ProductSN IN ({inClause})
                        AND MesResult LIKE 'OK%'";

            using (var conn = CreateConnection())
            using (var cmd = new SQLiteCommand(sql, conn))
            {
                conn.Open();

                cmd.Parameters.AddWithValue("@caseNumber", caseNumber);

                // 绑定条码参数
                for (int i = 0; i < productSNs.Count; i++)
                {
                    cmd.Parameters.AddWithValue($"@p{i}", productSNs[i]);
                }

                var existing = new HashSet<string>();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        existing.Add(reader.GetString(0));
                    }
                }
                return existing;
            }
        }
    }
}
