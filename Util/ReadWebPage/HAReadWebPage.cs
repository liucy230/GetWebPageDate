using GetWebPageDate.Util.Item;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GetWebPageDate.Util.ReadWebPage
{
    public class HAReadWebPage : BaseReadWebPage
    {
        private string userName;

        private string password;

        public HAReadWebPage()
        {
            string userInfo = ConfigurationManager.AppSettings["haUAndP"];
            string[] aUserInfo = userInfo.Split(',');

            userName = aUserInfo[0];
            password = aUserInfo[1];
        }

        public void login()
        {
            string loginURL = "http://58.20.39.35:9090/powerweb/login.action";

            string postData = string.Format("userCode={0}&password={1}&_eventId=submit&submit=", userName, password);

            request.Login(loginURL, postData);
        }

        /// <summary>
        /// 获取所有数据
        /// </summary>
        public void GetAllItem()
        {
            login();

            string url = "http://58.20.39.35:9090/powerweb/art/manage/gridArtOfQuery.action";

            string content = request.HttpPost(url, null);

            string totalData = CommonFun.GetValue(content, "<rows><results>", "</results>");

            double totalRows = Convert.ToDouble(totalData);

            int limit = 20 * 10;

            int totalPage = (int)Math.Ceiling(totalRows / limit);

            string postData = "start={0}&limit={1}&classId=&name=&factory=&ArtClassQuery_combo_id=&f_t_scm_article.spec=&f_t_scm_article.isonline%5EEQ=";

            for (int i = 0; i < totalPage; i++)
            {
                content = request.HttpPost(url, string.Format(postData, i * limit, limit));

                List<BaseItemInfo> items = GetSomeItem(content);
            }
        }

        private List<BaseItemInfo> GetSomeItem(string content)
        {
            List<BaseItemInfo> items = new List<BaseItemInfo>();

            if (!string.IsNullOrEmpty(content))
            {
                MatchCollection ms = CommonFun.GetValues(content, "<row>", "</row>");

                foreach (Match m in ms)
                {
                    try
                    {
                        BaseItemInfo item = new BaseItemInfo();
                        item.ViewCount = CommonFun.GetValue(m.Value, "<ARTID>", "</ARTID>");
                        item.Name = CommonFun.GetValue(m.Value, "<NAME>", "</NAME>");
                        item.Format = CommonFun.GetValue(m.Value, "<SPEC>", "</SPEC>");
                        item.Created = CommonFun.GetValue(m.Value, "<FACTORY>", "</FACTORY>");
                        item.ID = CommonFun.GetValue(m.Value, "<FILENO>", "</FILENO>");
                        string priceStr = CommonFun.GetValue(m.Value, "<PRICE>", "</PRICE>");
                        item.PlatformPrice = string.IsNullOrEmpty(priceStr) ? 0 : Convert.ToDecimal(priceStr);
                        item.Type = CommonFun.GetValue(m.Value, "<MEDIATYPENAME>", "</MEDIATYPENAME>");
                        items.Add(item);

                        AddItme(item, "HA/HA" + ticks + ".csv");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                }
            }
            return items;
        }

        public override bool ComparePrice(BaseItemInfo platformItem, BaseItemInfo info)
        {
            if (info.PlatformPrice > 0 && info.PlatformPrice * (decimal)0.8 >= platformItem.ShopSelaPrice)
            {
                info.ShopPrice = platformItem.ShopSelaPrice;
                info.Type = platformItem.Type;
                info.ViewCount = platformItem.ViewCount;

                CommonFun.WriteCSV("HA/20以上" + ticks + ".csv", info);

                return true;
            }
            return false;
        }
    }
}
