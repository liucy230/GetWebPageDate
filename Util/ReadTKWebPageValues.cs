using GetWebPageDate.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GetWebPageDate.Util
{
    public class ReadTKWebPageValues : IReadWebPage
    {
        private HttpRequest request = new HttpRequest();

        private List<string> allMenuURL = new List<string>();

        private Dictionary<string, ItemInfo> allItems = new Dictionary<string, ItemInfo>();

        public Dictionary<string, ItemInfo> AllItems
        {
            get { return allItems; }
            set { allItems = value; }
        }

        public void ReadAllMenuURL(string url, string data, string cookie)
        {
            string content = request.HttpGet(url, data, cookie);

            int startInfex = content.IndexOf("daohangn");

            int endIndex = content.IndexOf("<script>");
        }

        public void ReadAllItem()
        {
            string url = "https://www.tkyfw.com/Ching_slist_{0}.html";

            for (int i = 0; i < 30000; i++)
            {
                string content = request.HttpGet(string.Format(url, i), "");

                content = CommonFun.GetValue(content, "<div class=\"buying\">", "</div>");

                MatchCollection ms = CommonFun.GetValues(content, "<td class=\"td3\">", "</td>");

                if (!string.IsNullOrEmpty(ms[0].Value))
                {
                    try
                    {
                        ItemInfo item = new ItemInfo();
                        item.Name = ms[0].Value;
                        item.ID = ms.Count > 3 ? ms[2].Value : "";
                        item.Created = CommonFun.GetValue(content, "rel=\"theqiye\">", "</td>");
                        string priceStr = CommonFun.GetValue(content, "<strong class=\"value yahei\">", "</strong>");
                        try
                        {
                            if(string.IsNullOrEmpty(priceStr))
                            {
                                Console.WriteLine("Price　is null ID;{0}, url:{1}", item.ID, string.Format(url, i));
                                continue;
                            }
                            item.ShopPrice = Convert.ToDecimal(priceStr);
                        }
                        catch(Exception ex)
                        {
                            Console.WriteLine(ex.ToString() + priceStr);
                        }
                       
                        item.Format = CommonFun.GetValue(content, "<li class=\"cur\">", "</li>");
                        item.Format = CommonFun.GetValue(item.Format, "\">", "</a>");
                        string key = item.ID + "{" + item.Format + "}";

                        if (allItems.ContainsKey(key))
                        {
                            if (allItems[key].ShopPrice > item.ShopPrice)
                            {
                                allItems[key] = item;
                            }
                        }
                        else
                        {
                            allItems.Add(key, item);
                        }

                        CommonFun.WriteCSV("TK.csv", item);
                    }
                    catch(Exception  ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }
                }
            }
        }
    }
}
