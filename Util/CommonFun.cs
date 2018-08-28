using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GetWebPageDate.Util
{
    public class CommonFun
    {

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


        public static string GetValue(string str, string s, string e)
        {
            Regex rg = new Regex("(?<=(" + s + "))[.\\s\\S]*?(?=(" + e + "))", RegexOptions.Multiline | RegexOptions.Singleline);

            return rg.Match(str).Value;
        }

        //public static bool IsNum(string str)
        //{
        //    Regex rg = new Regex("[\u4e00-\u9fa5]+", RegexOptions.Multiline | RegexOptions.Singleline);

        //    return !rg.IsMatch(str);
        //}


        public static void WriteCSV(string filePathName, ItemInfo itemInfo)
        {
            try
            {
                string filePath = filePathName;

                CheckAndCreateFolder(filePath);

                if (!File.Exists(filePath))
                {
                    using (StreamWriter fileWriter = new StreamWriter(filePath, true, Encoding.Default))
                    {
                        fileWriter.WriteLine(itemInfo.LogHeadLine);
                        fileWriter.Flush();
                        fileWriter.Close();
                    }
                }

                WriteCSV(filePath, true, itemInfo.getLogStrArr());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private static void WriteCSV(string filePathName, bool append, String[] ls)
        {
            string[] writeStr = new string[ls.Length];
            for (int i = 0; i < ls.Length; i++)
            {
                string rstr = ls[i].Replace("\"", "\"\""); //替换英文冒号 英文冒号需要换成两个冒号
                if (rstr.Contains(',') || rstr.Contains('"')
                    || rstr.Contains('\r') || rstr.Contains('\n')) //含逗号 冒号 换行符的需要放到引号中
                {
                    rstr = string.Format("\"{0}\"", rstr);
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

    }
}
