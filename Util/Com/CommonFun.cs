using GetWebPageDate.Http;
using GetWebPageDate.Util.Item;
using Microsoft.Office.Interop.Excel;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
//using Excel = Microsoft.Office.Interop.Excel;

namespace GetWebPageDate.Util
{
    public class CommonFun
    {
        private static long ticks = DateTime.Now.Ticks;

        private static string username = "罗艳";

        private static string password = "123123";

        public static System.Data.DataTable ReadXLS(string fileName, string sheetName = "Sheet1")
        {
            try
            {
                //var connectionString = "Provider=Microsoft.Jet.OleDb.4.0;" + "data source=" + Server.MapPath("ExcelFiles/MyExcelFile.xls") + ";Extended Properties='Excel 8.0; HDR=Yes; IMEX=1'"; //此连接只能操作Excel2007之前(.xls)文件
                //var connectionString = string.Format("Provider=Microsoft.Ace.OleDb.4.0;" + "data source={0};Extended Properties='Excel 8.0; HDR=Yes; IMEX=1'", fileName);//此连接只能操作Excel2007之前(.xls)文件


                //foreach (DataRow row in data.Rows)
                //{
                //    string rowStr = "";
                //    foreach (var clumon in row.ItemArray)
                //    {
                //        rowStr += clumon + ",";
                //    }
                //    Console.WriteLine(rowStr);
                //}
                if (!File.Exists(fileName))
                {
                    return null;
                }

                var connectionString = string.Format("Provider=Microsoft.Ace.OleDb.12.0;" + "data source={0};Extended Properties='Excel 12.0; HDR=Yes; IMEX=1'", fileName);

                var adapter = new OleDbDataAdapter(string.Format("SELECT * FROM [{0}$]", sheetName), connectionString);
                var ds = new DataSet();

                adapter.Fill(ds, "anyNameHere");
                adapter.Dispose();
                System.Data.DataTable data = ds.Tables["anyNameHere"];
                return data;
            }
            catch (Exception ex)
            {
                Console.WriteLine("fileName:{0} error:{1}", fileName, ex);
            }

            return null;
        }

        /// <summary>
        /// 获取url编码
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public static string GetUrlEncode(string param, bool isToUpper = true)
        {
            return isToUpper ? System.Web.HttpUtility.UrlEncode(param).ToUpper() : System.Web.HttpUtility.UrlEncode(param);
        }

        public static bool CreateXLS(string fileName)
        {
            Application app = null;
            Workbook workbook;
            Worksheet wSheet;
            try
            {
                string baseFilePath = System.Environment.CurrentDirectory;

                app = new Application();

                workbook = app.Workbooks.Add(Missing.Value);

                wSheet = workbook.Worksheets[1] as Worksheet;

                wSheet.SaveAs(baseFilePath + "/" + fileName, Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value);

                workbook.Save();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            if (app != null)
            {
                wSheet = null;
                workbook = null;
                app.Quit();
                app = null;
            }
            return true;
        }

        public static bool InsertToXLS(string fileName, string sheetName, string[] value)
        {
            sheetName = string.IsNullOrEmpty(sheetName) ? "info" : sheetName;
            OleDbConnection connection = null;
            try
            {
                var connectionString = string.Format("Provider=Microsoft.Ace.OleDb.12.0;" + "data source={0};Extended Properties='Excel 12.0; HDR=Yes; IMEX=0'", fileName);

                connection = new OleDbConnection(connectionString);

                connection.Open();

                string sqlStr;

                System.Data.DataTable dt = connection.GetOleDbSchemaTable(OleDbSchemaGuid.Tables_Info, null);
                bool existTable = false;
                foreach (DataRow dr in dt.Rows)//检查是否有信息表
                {
                    if (dr["TABLE_NAME"].ToString() == sheetName + "$")//要加个$号
                    {
                        existTable = true;
                        break;
                    }
                }

                if (!existTable)
                {
                    for (int i = 0; i < value.Length; i++)
                    {
                        value[i] = "[" + value[i].Trim() + "]";
                        //if (value[i].Contains("("))
                        //{
                        //    value[i] = value[i].Replace("(", "'('");
                        //}

                        //if (value[i].Contains(")"))
                        //{
                        //    value[i] = value[i].Replace(")", "')'");
                        //}
                    }
                    string valueStr = string.Join(" char(100),", value);
                    sqlStr = @"create table [" + sheetName + "] (" + valueStr + " char(100))";
                }
                else
                {
                    //下面的代码用OleDbCommand的parameter添加参数
                    string valueStr = string.Join("','", value);
                    sqlStr = "insert into [" + sheetName + "$] values('" + valueStr + "')";
                }

                OleDbCommand Cmd = new OleDbCommand(sqlStr, connection);
                Cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            if (connection != null)
            {
                connection.Close();
                connection.Dispose();
            }

            return true;
        }

        public static bool WriteXLS(string fileName, BaseItemInfo item)
        {
            string filePath = fileName;
            CheckAndCreateFolder(filePath);

            if (!File.Exists(filePath))
            {
                CreateXLS(filePath);
                InsertToXLS(fileName, "info", item.GetLogHeadLine().Split(','));
            }

            InsertToXLS(fileName, "info", item.GetLogStrArr());

            return true;
        }

        public static bool UpdateXLS(string fileName, string[] key, string[] value, string whereKey, string whereValue, string sheetName = "Sheet1$")
        {
            OleDbConnection connection = null;
            try
            {
                var connectionString = string.Format("Provider=Microsoft.Ace.OleDb.12.0;" + "data source={0};Extended Properties='Excel 12.0; HDR=Yes; IMEX=0'", fileName);

                connection = new OleDbConnection(connectionString);

                connection.Open();

                string updateStr = string.Format("UPDATE [{0}$] SET ", sheetName);

                for (int i = 0; i < key.Length; i++)
                {
                    if (i != 0)
                    {
                        updateStr += ",";
                    }
                    updateStr += string.Format("{0} = '{1}'", key[i], value[i]);
                }

                updateStr += string.Format(" WHERE {0} = '{1}'", whereKey, whereValue);

                var com = new OleDbCommand(updateStr, connection);

                int row = com.ExecuteNonQuery();

                //connection.Close();

                return row > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            if (connection != null)
            {
                connection.Close();
                connection.Dispose();
            }
            return false;
        }

        public static void Compare(string fielName, string targeFileName, string toFileName)
        {
            ReadPlatFormWebPageValue flatform = new ReadPlatFormWebPageValue();
            BaseReadWebPage res = new BaseReadWebPage();
            BaseReadWebPage target = new BaseReadWebPage();

            target.ReadBaseItemInfo(targeFileName, true);
            res.ReadBaseItemInfo(fielName, true);

            foreach (BaseItemInfo item in target.ShopAllItems.Values)
            {
                bool isEixst = false;
                foreach (BaseItemInfo platformItem in res.ShopAllItems.Values)
                {
                    bool isSame = false;
                    if (!string.IsNullOrEmpty(item.ID) && !string.IsNullOrEmpty(platformItem.ID))
                    {
                        if (item.ID.Trim() == platformItem.ID.Trim())
                        {
                            isSame = true;
                        }
                    }
                    if (isSame || (item.Name == platformItem.Name && item.Created == platformItem.Created))
                    {
                        if (IsSameFormat(platformItem.Format, item.Format, platformItem.Name, item.Name))
                        {
                            isEixst = true;
                            CompareTKPrice(platformItem, item, toFileName);
                            //ComparePlatformAndTKPrice(platformItem, item, toFileName);
                        }
                    }
                }

                //if (!isEixst)
                //{
                //    List<BaseItemInfo> seachItems = flatform.SeachInfoByID(item.ID);

                //    Dictionary<string, BaseItemInfo> saveInfos = new Dictionary<string, BaseItemInfo>();

                //    foreach (BaseItemInfo info in seachItems)
                //    {
                //        string key = info.Name + info.Format + info.Created;

                //        if (!res.ShopAllItems.ContainsKey(key))
                //        {
                //            if (item.ID == info.ID || (item.Name == info.Name && item.Created == info.Created))
                //            {
                //                if (IsSameFormat(info.Format, item.Format, info.Name, item.Name))
                //                {
                //                    isEixst = true;
                //                    ComparePlatformAndTKPrice(info, item, toFileName);
                //                }
                //            }

                //            res.ShopAllItems.Add(key, info);
                //            saveInfos.Add(key, info);
                //        }
                //        else if (res.ShopAllItems[key].ShopSelaPrice > info.ShopSelaPrice)
                //        {
                //            if (item.ID == info.ID || (item.Name == info.Name && item.Created == info.Created))
                //            {
                //                if (IsSameFormat(info.Format, item.Format, info.Name, item.Name))
                //                {
                //                    isEixst = true;
                //                    ComparePlatformAndTKPrice(info, item, toFileName);
                //                }
                //            }

                //            res.ShopAllItems[key] = info;
                //            saveInfos[key] = info;
                //        }
                //    }

                //    foreach (BaseItemInfo saveInfo in saveInfos.Values)
                //    {
                //        WriteCSV(fielName, saveInfo);
                //    }
                //}


                if (!isEixst)
                {
                    WriteCSV(toFileName + "/NotEixst" + ticks + ".csv", item);
                }
            }
        }

        private static void ComparePlatformAndTKPrice(BaseItemInfo platformItem, BaseItemInfo info, string fileName)
        {
            if (info.ShopPrice * (decimal)0.8 < platformItem.ShopSelaPrice)
            {
                info.PlatformPrice = platformItem.ShopSelaPrice;
                info.Type = platformItem.Type;

                WriteCSV(fileName + "/低于20以上.csv", info);
            }
        }

        private static void CompareTKPrice(BaseItemInfo platformItem, BaseItemInfo info, string fileName)
        {
            if (info.ShopPrice > 0 && info.ShopPrice * (decimal)0.8 >= platformItem.ShopSelaPrice)
            {
                info.PlatformPrice = platformItem.ShopSelaPrice;
                info.Type = platformItem.Type;

                WriteCSV(fileName + "/低于20以上" + ticks + ".csv", info);
            }
        }

        private static void ComparePrice(BaseItemInfo platformItem, BaseItemInfo info, string fileName)
        {
            if (info.ShopPrice * (decimal)1.25 <= platformItem.ShopPrice)
            {
                info.ID = platformItem.ID;
                info.PlatformPrice = platformItem.ShopPrice;
                info.ViewCount = platformItem.ViewCount;
                info.Type = platformItem.Type;

                CommonFun.WriteCSV(fileName + "/25以上.csv", info);
            }
            else if (info.ShopPrice * (decimal)1.15 <= platformItem.ShopPrice
                       && platformItem.ShopPrice * (decimal)1.25 > platformItem.ShopPrice)
            {
                info.ID = platformItem.ID;
                info.PlatformPrice = platformItem.ShopPrice;
                info.ViewCount = platformItem.ViewCount;
                info.Type = platformItem.Type;

                CommonFun.WriteCSV(fileName + "/15-25.csv", info);
            }
            else if (info.ShopPrice * (decimal)1.05 <= platformItem.ShopPrice
                 && platformItem.ShopPrice * (decimal)1.15 > platformItem.ShopPrice)
            {
                info.ID = platformItem.ID;
                info.PlatformPrice = platformItem.ShopPrice;
                info.ViewCount = platformItem.ViewCount;
                info.Type = platformItem.Type;

                CommonFun.WriteCSV(fileName + "/5-15.csv", info);
            }
            else if (info.ShopPrice == platformItem.ShopPrice
                && info.ShopPrice * (decimal)1.05 > platformItem.ShopPrice)
            {
                info.ID = platformItem.ID;
                info.PlatformPrice = platformItem.ShopPrice;
                info.ViewCount = platformItem.ViewCount;
                info.Type = platformItem.Type;

                CommonFun.WriteCSV(fileName + "/0-5.csv", info);
            }
            else
            {
                info.ID = platformItem.ID;
                info.PlatformPrice = platformItem.ShopPrice;
                info.ViewCount = platformItem.ViewCount;
                info.Type = platformItem.Type;

                CommonFun.WriteCSV(fileName + "/0以下.csv", info);
            }
        }

        private static void ComparePrice4(BaseItemInfo platformItem, BaseItemInfo info, string fileName)
        {
            if (info.ShopPrice * (decimal)1.25 <= platformItem.ShopSelaPrice)
            {
                info.ID = platformItem.ID;
                info.PlatformPrice = platformItem.ShopPrice;
                info.ShopSelaPrice = platformItem.ShopSelaPrice;
                info.Sela = platformItem.Sela;
                info.ViewCount = platformItem.ViewCount;
                info.Type = platformItem.Type;

                CommonFun.WriteCSV(fileName + "/25以上.csv", info);
            }
            else if (info.ShopPrice * (decimal)1.15 <= platformItem.ShopSelaPrice
                       && info.ShopPrice * (decimal)1.25 > platformItem.ShopSelaPrice)
            {
                info.ID = platformItem.ID;
                info.PlatformPrice = platformItem.ShopPrice;
                info.ShopSelaPrice = platformItem.ShopSelaPrice;
                info.Sela = platformItem.Sela;
                info.ViewCount = platformItem.ViewCount;
                info.Type = platformItem.Type;

                CommonFun.WriteCSV(fileName + "/15-25.csv", info);
            }
            else if (info.ShopPrice * (decimal)1.05 <= platformItem.ShopSelaPrice
                && info.ShopPrice * (decimal)1.15 > platformItem.ShopSelaPrice)
            {
                info.ID = platformItem.ID;
                info.PlatformPrice = platformItem.ShopPrice;
                info.ShopSelaPrice = platformItem.ShopSelaPrice;
                info.Sela = platformItem.Sela;
                info.ViewCount = platformItem.ViewCount;
                info.Type = platformItem.Type; ;

                CommonFun.WriteCSV(fileName + "/5-15.csv", info);
            }
            else if (info.ShopPrice <= platformItem.ShopSelaPrice
                && info.ShopPrice * (decimal)1.05 > platformItem.ShopSelaPrice)
            {
                info.ID = platformItem.ID;
                info.PlatformPrice = platformItem.ShopPrice;
                info.ShopSelaPrice = platformItem.ShopSelaPrice;
                info.Sela = platformItem.Sela;
                info.ViewCount = platformItem.ViewCount;
                info.Type = platformItem.Type;

                CommonFun.WriteCSV(fileName + "/0-5.csv", info);
            }
            else
            {
                info.ID = platformItem.ID;
                info.PlatformPrice = platformItem.ShopPrice;
                info.ShopSelaPrice = platformItem.ShopSelaPrice;
                info.Sela = platformItem.Sela;
                info.ViewCount = platformItem.ViewCount;
                info.Type = platformItem.Type;

                CommonFun.WriteCSV(fileName + "/0以下.csv", info);
            }
        }

        /// <summary>
        /// 获取非数字部分
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        //public static string GetUnNum(string value)
        //{
        //    return Regex.Replace(value, @"[\d.\d]", "");
        //}

        public static double GetFormatValue(string format)
        {
            double result = 0;

            try
            {
                format = format.Replace("毫克", "mg");
                format = format.Replace('克', 'g');
                format = format.Replace('G', 'g');
                format = format.Replace('*', 'x');
                format = format.Replace('×', 'x');
                format = format.Replace('：', ':');
                format = format.Replace('∶', ':');
                format = format.Replace("..", ".");
                format = format.Replace("毫升", "ml");

                List<string> valueList = new List<string>();

                string[] aFormat = format.Split('/');

                foreach (string iformat in aFormat)
                {
                    if (iformat.Contains('x'))
                    {
                        string[] aFormat1 = iformat.Split('x');

                        foreach (string iformat1 in aFormat1)
                        {
                            if (iformat1.Contains(':'))
                            {
                                valueList.AddRange(iformat1.Split(':'));
                            }
                            else
                            {
                                valueList.Add(iformat1);
                            }
                        }
                    }
                    else
                    {
                        valueList.Add(iformat);
                    }
                }

                foreach (string value in valueList)
                {
                    string numStr = CommonFun.GetNum(value);
                    double num = string.IsNullOrEmpty(numStr) ? 1 : Convert.ToDouble(numStr);

                    if (result == 0)
                    {
                        result = num * 100000;
                    }
                    else
                    {
                        result *= num;
                    }
                }

                if (format.Contains("mg"))
                {
                    result /= 1000;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return result;
        }

        public static bool IsSameItem(string id1, string id2, string format1, string format2, string name1, string name2)
        {
            id1 = CommonFun.GetNum(id1);
            id2 =  CommonFun.GetNum(id2);
            if (id1.Contains(id2) || id2.Contains(id1))
            {
                if(IsSameFormat(format1, format2) || IsSameFormat(format1, format2, name1, name2))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsSameFormat(string format1, string format2)
        {
            try
            {
                double value1 = GetFormatValue(format1);
                double value2 = GetFormatValue(format2);
                if (value1 != 0 && value1 == value2)
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            return false;
        }

        public static bool IsSameFormat(string format1, string format2, string name1, string name2)
        {
            string pFormat = FormatStr(format1, name1);

            string sFormat = FormatStr(format2, name2);

            string[] array1 = pFormat.Split('/');

            string[] array2 = sFormat.Split('/');

            try
            {
                if (array1[0].Contains('x'))
                {
                    string[] values = array1[0].Split('x');
                    decimal format = 1;
                    foreach (string value in values)
                    {
                        if (value.Contains(":"))
                        {
                            string[] sValues = value.Split(':');
                            foreach (string sValue in sValues)
                            {
                                string num = CommonFun.GetNum(sValue);
                                format *= (string.IsNullOrEmpty(num) ? 1 : Convert.ToDecimal(num));
                            }
                        }
                        else
                        {
                            string num = CommonFun.GetNum(value);
                            format *= (string.IsNullOrEmpty(num) ? 1 : Convert.ToDecimal(num));
                        }
                    }
                    string info = Regex.Replace(values[0], @"[\d.\d]", "");
                    if (string.IsNullOrEmpty(info) && !string.IsNullOrEmpty(values[1]))
                    {
                        info = Regex.Replace(values[1], @"[\d.\d]", "");
                    }

                    array1[0] = format + info;
                }

                if (array2[0].Contains('x'))
                {
                    string[] values = array2[0].Split('x');
                    decimal format = 1;
                    foreach (string value in values)
                    {
                        if (value.Contains(":"))
                        {
                            string[] sValues = value.Split(':');
                            foreach (string sValue in sValues)
                            {
                                string num = CommonFun.GetNum(sValue);
                                format *= (string.IsNullOrEmpty(num) ? 1 : Convert.ToDecimal(num));
                            }
                        }
                        else
                        {
                            string num = CommonFun.GetNum(value);
                            format *= (string.IsNullOrEmpty(num) ? 1 : Convert.ToDecimal(num));
                        }
                    }
                    string info = Regex.Replace(values[0], @"[\d.\d]", "");
                    array2[0] = format + info;
                }

                if (array1.Length == 2 && array2.Length == 2)
                {
                    if (array1[1].Contains('x'))
                    {
                        string[] values = array1[1].Split('x');
                        string info = Regex.Replace(array1[0], @"[\d.\d]", "");
                        array1[0] = Convert.ToDecimal(CommonFun.GetNum(values[1])) * Convert.ToDecimal(CommonFun.GetNum(array1[0])) + info;
                    }

                    if (array2[1].Contains('x'))
                    {
                        string[] values = array2[1].Split('x');
                        string info = Regex.Replace(array2[0], @"[\d.\d]", "");
                        array2[0] = Convert.ToDecimal(CommonFun.GetNum(values[1])) * Convert.ToDecimal(CommonFun.GetNum(array2[0])) + info;
                    }

                    if (array1[0] == array2[0])
                    {
                        return true;
                    }
                }
                else
                {
                    if (array1[0] == array2[0])
                    {
                        return true;
                    }
                    else
                    {
                        string[] value1 = array1[0].Split('x');
                        string[] value2 = array2[0].Split('x');

                        if (value1.Length != value2.Length)
                        {
                            if (value1[0] == value2[0])
                            {
                                return true;
                            }
                            //List<decimal> lValue1 = new List<decimal>();
                            //foreach(string value in value1)
                            //{
                            //    lValue1.Add(Convert.ToDecimal(CommonFun.GetNum(value)));
                            //}

                            //foreach (string value in value2)
                            //{
                            //    if(lValue1.Contains(Convert.ToDecimal(CommonFun.GetNum(value))))
                            //    {
                            //        return true;
                            //    }
                            //}
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return false;
        }



        /// <summary>
        /// 获取以S开始，以e结尾的数据
        /// </summary>
        /// <param name="str"></param>
        /// <param name="s"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        public static MatchCollection GetValues(string str, string s, string e)
        {
            Regex rg = new Regex("(?<=(" + s + "))[.\\s\\S]*?(?=(" + e + "))", RegexOptions.Multiline | RegexOptions.Singleline);
            return rg.Matches(str);
        }

        public static string MD5Str(string content)
        {
            try
            {
                MD5 md5 = MD5.Create();

                byte[] data = md5.ComputeHash(Encoding.UTF8.GetBytes(content));

                string md5Str = "";
                for (int i = 0; i < data.Length; i++)
                {
                    md5Str += data[i].ToString("X2");
                }

                return md5Str;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return null;
        }

        public static string GetGMTDate()
        {
            return DateTime.Now.ToUniversalTime().ToString("r");
        }
        /// <summary>
        /// HMAC
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public static byte[] Hmac(string content, string key)
        {
            try
            {
                HMACSHA1 myHMACSHA1 = new HMACSHA1(Encoding.UTF8.GetBytes(key));
                byte[] byteContent = myHMACSHA1.ComputeHash(Encoding.UTF8.GetBytes(content));

                return byteContent;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            return null;
        }

        public static string Base64(byte[] value)
        {
            return Convert.ToBase64String(value);
        }

        public static string GetValue(string str, string s, string e)
        {
            Regex rg = new Regex("(?<=(" + s + "))[.\\s\\S]*?(?=(" + e + "))", RegexOptions.Multiline | RegexOptions.Singleline);

            return rg.Match(str).Value;
        }

        public static string GetNum(string str)
        {
            return Regex.Replace(str, @"[^\d.\d]", "");
        }

        public static string GetUnChinese(string str)
        {
            return Regex.Replace(str, "[\u4e00-\u9fa5]+", "");
        }
        //public static bool IsNum(string str)
        //{
        //    Regex rg = new Regex("[\u4e00-\u9fa5]+", RegexOptions.Multiline | RegexOptions.Singleline);

        //    return !rg.IsMatch(str);
        //}

        public static void WriteUrlCSV(string filePathName, string url)
        {
            try
            {
                string filePath = filePathName;

                CheckAndCreateFolder(filePath);

                WriteCSV(filePath, true, url);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public static decimal GetFormatValue(string format, string name)
        {
            decimal result = 0;
            if (!string.IsNullOrEmpty(format))
            {
                result = 1;
                string newFormat = FormatStr(format, name);

                string[] infos = newFormat.Split('/');

                foreach (string info in infos)
                {
                    string[] formatItems = null;

                    if (info.Contains('*'))
                    {
                        formatItems = info.Split('*');
                    }
                    else if (info.Contains('X'))
                    {
                        formatItems = info.Split('X');
                    }
                    else if (info.Contains('x'))
                    {
                        formatItems = info.Split('x');
                    }
                    else if (info.Contains('×'))
                    {
                        formatItems = info.Split('×');
                    }
                    else if (info.Contains(':'))
                    {
                        formatItems = info.Split(':');
                    }
                    else if (info.Contains('：'))
                    {
                        formatItems = info.Split('：');
                    }
                    else
                    {
                        formatItems = new string[1];
                        formatItems[0] = info;
                    }

                    foreach (string formatItem in formatItems)
                    {
                        string valueStr = GetNum(formatItem);

                        decimal value = string.IsNullOrEmpty(valueStr) ? 1 : Convert.ToDecimal(valueStr);

                        result *= value;
                    }
                }
            }
            return result;
        }

        public static void WriteCSV(string filePathName, BaseItemInfo itemInfo)
        {
            try
            {
                string filePath = filePathName;

                CheckAndCreateFolder(filePath);

                if (!File.Exists(filePath))
                {
                    using (StreamWriter fileWriter = new StreamWriter(filePath, true, Encoding.Default))
                    {
                        fileWriter.WriteLine(itemInfo.GetLogHeadLine());
                        fileWriter.Flush();
                        fileWriter.Close();
                    }
                }

                WriteCSV(filePath, true, itemInfo.GetLogStrArr());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private static void WriteCSV(string filePathName, bool append, params string[] ls)
        {
            string[] writeStr = new string[ls.Length];
            for (int i = 0; i < ls.Length; i++)
            {
                string rstr = ls[i].Replace("\"", "\"\""); //替换英文冒号 英文冒号需要换成两个冒号
                if (rstr.Contains(',') || rstr.Contains('"')
                    || rstr.Contains('\r') || rstr.Contains('\n')) //含逗号 冒号 换行符的需要放到引号中
                {
                    rstr = string.Format("\"'{0}\"", rstr);
                }
                writeStr[i] = rstr;
            }
            using (StreamWriter fileWriter = new StreamWriter(filePathName, append, Encoding.Default))
            {
                fileWriter.WriteLine(String.Join(",", writeStr));
                fileWriter.Flush();
                fileWriter.Close();
            }
        }

        public static string FormatStr(string format, string name)
        {
            try
            {
                if (format.Contains('+'))
                {

                }

                if ((format.Contains('(') && format.Contains(')')) || (format.Contains('（') && format.Contains('）')))
                {
                    if ((format.Contains('(') && format.Contains(')')))
                    {
                        format = format.Substring(0, format.IndexOf('(')) + format.Substring(format.IndexOf(')') + 1, format.Length - format.IndexOf(')') - 1);
                    }
                    else
                    {
                        format = format.Substring(0, format.IndexOf('（')) + format.Substring(format.IndexOf('）') + 1, format.Length - format.IndexOf('）') - 1);
                    }
                }

                format = format.Replace("/盒/盒", "/盒");
                format = format.Replace("(", "");
                format = format.Replace(")", "");
                format = format.Replace("（", "");
                format = format.Replace("）", "");
                format = format.Replace("毫克", "mg");
                format = format.Replace('克', 'g');
                format = format.Replace('G', 'g');
                format = format.Replace('*', 'x');
                format = format.Replace('×', 'x');
                format = format.Replace('：', ':');
                format = format.Replace('∶', ':');
                format = format.Replace("..", ".");
                format = format.Replace("毫升", "ml");
                format = format.Replace("丸", "粒");
                format = format.Replace("张", "贴");
                format = format.Replace("瓶/盒", "/瓶");
                format = format.Replace("g/袋", "g");
                format = format.Replace("万单位", "IU");

                if (!string.IsNullOrEmpty(name))
                {
                    if (name.Contains("片"))
                    {
                        format = format.Replace('s', '片');
                        format = format.Replace('粒', '片');
                    }
                    else
                    {
                        format = format.Replace('s', '粒');
                    }
                }
                if (format.Contains("mg") || format.Contains("g"))
                {
                    string[] values = format.Split('x');

                    if (values[0].Contains(':'))
                    {
                        values[0] = values[0].Substring(0, values[0].IndexOf(':'));
                    }

                    decimal value1 = format.Contains("mg") ? (Convert.ToDecimal(CommonFun.GetNum(values[0])) / 1000) : Convert.ToDecimal(CommonFun.GetNum(values[0]));

                    format = "";

                    format += value1 + "g" + "x";

                    for (int i = 1; i < values.Length; i++)
                    {
                        format += values[i] + "x";
                    }

                    format = format.Substring(0, format.LastIndexOf('x'));
                }

                if (format.Contains("片") && format.Contains("板"))
                {
                    return SumFormat(format, "片", "板");
                }
                else if (format.Contains("粒") && format.Contains("板"))
                {
                    return SumFormat(format, "粒", "板");
                }
                else if (format.Contains("粒") && format.Contains("瓶"))
                {
                    return SumFormat(format, "粒", "瓶");
                }
                else if (format.Contains("片") && format.Contains("瓶"))
                {
                    return SumFormat(format, "片", "瓶");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(format + ex.ToString());
            }
            return format;
        }

        private static string SumFormat(string format, string key1, string key2)
        {
            if (format.Contains(key1) && format.Contains(key2))
            {
                string[] array1 = format.Split('/');

                if (array1.Length == 2)
                {
                    string[] array = array1[0].Split('x');

                    if (array.Length == 3)
                    {
                        decimal value = Convert.ToDecimal(CommonFun.GetNum(array[1])) * Convert.ToDecimal(CommonFun.GetNum(array[2]));

                        return array[0] + "x" + value + key1 + "/" + array1[1];
                    }
                }
                else
                {
                    string[] array = format.Split('x');
                    if (array.Length == 3)
                    {
                        decimal value = Convert.ToDecimal(CommonFun.GetNum(array[1])) * Convert.ToDecimal(CommonFun.GetNum(array[2]));

                        return array[0] + "x" + value + key1;
                    }
                }
            }

            return format;
        }

        public static string ReadCSV(string filePathName)
        {
            string content = "";

            using (StreamReader fileRead = new StreamReader(filePathName, Encoding.Default))
            {
                content = fileRead.ReadToEnd();
                fileRead.Close();
            }

            return content;
        }

        /// <summary>
        /// 检查路径文件夹是否存在,并创建
        /// </summary>
        /// <param name="filePathName"></param>
        public static void CheckAndCreateFolder(string filePathName)
        {
            string[] filePaths = filePathName.Split('/');
            string filePath = "";
            for (int i = 0; i < filePaths.Length - 1; i++)
            {
                filePath += filePaths[i];
                if (!Directory.Exists(filePath))
                {
                    Directory.CreateDirectory(filePath);
                }
                filePath += "/";
            }
        }

        public static bool SendMsg(string phoneNum, string msg)
        {
            try
            {
                HttpRequest http = new HttpRequest();

                string url = "http://139.224.13.203:8088/sms.aspx";

                string postData = string.Format("action=send&userid=3953&account=%E5%BF%85%E5%88%B0%E7%89%8C%E8%BD%AF%E4%BB%B6%3D%E7%BD%97%E8%89%B3&password=123123&mobile={0}&content={1}&sendTime=&extno=", phoneNum, msg);

                string content = http.HttpPost(url, postData);

                if (content.Contains("Success"))
                {
                    return true;
                }
                else
                {
                    Console.WriteLine(content);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            return false;
        }

        public static void SavePicture(Image image, string filePathName)
        {
            try
            {
                if (image != null)
                {

                    CheckAndCreateFolder(filePathName);

                    //Image newIamge = new Bitmap(image.Width, image.Height);

                    //Graphics draw = Graphics.FromImage(newIamge);

                    //draw.DrawImage(image, 0, 0);
                    image.Save(filePathName);
                    image.Dispose();

                    // newIamge.Save(filePathName);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error:{0}, FilePathName:{1}", ex.ToString(), filePathName);
                SavePicture(image, filePathName);
            }
            finally
            {
                if (image != null)
                {
                    image.Dispose();
                }
            }
        }

        /// <summary>
        /// 保留两位小数（两位后的全部舍掉）
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static decimal TrunCate(decimal value)
        {
            return Math.Truncate(value * 100) / 100;
        }

        public static bool IsInTimeRange(string range)
        {
            DateTime curTime = DateTime.Now;

            string[] timeInfo = range.Split('-');
            string[] startTimeInfo = timeInfo[0].Split(':');
            string[] entTimeInfo = timeInfo[1].Split(':');
            DateTime startTime = new DateTime(curTime.Year, curTime.Month, curTime.Day, Convert.ToInt32(startTimeInfo[0]), Convert.ToInt32(startTimeInfo[1]), 0);
            DateTime endTime = new DateTime(curTime.Year, curTime.Month, curTime.Day, Convert.ToInt32(entTimeInfo[0]), Convert.ToInt32(entTimeInfo[1]), 0);
            if (curTime >= startTime && curTime < endTime)
            {
                return true;
            }

            return false;
        }
    }
}
