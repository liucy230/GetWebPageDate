using GetWebPageDate.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GetWebPageDate.Util
{
    public class ReadShopWebPageValue : IReadWebPage
    {
        /// <summary>
        /// 商城所有菜单URL
        /// </summary>
        private List<string> shopAllMenuURL;

        /// <summary>
        /// 商城所有商品
        /// </summary>
        private Dictionary<string, ItemInfo> shopAllItems;

        public Dictionary<string, ItemInfo> ShopAllItems
        {
            get { return shopAllItems; }
            set { shopAllItems = value; }
        }

        private HttpRequest request;

        /// <summary>
        /// 
        /// </summary>
        public ReadShopWebPageValue()
        {
            shopAllMenuURL = new List<string>();

            shopAllItems = new Dictionary<string, ItemInfo>();

            request = new HttpRequest();
        }

        public void LoginShop(string url)
        {
            string dataStr = "TextBox1=thcgn0135&TextBox2=mengjie165";
            string content = request.HttpPost(url, dataStr, null);

            Console.WriteLine(content);
        }

        /// <summary>
        /// 读取商城所有的菜单URL
        /// </summary>
        public void ReadShopALLMenuURL(string url, string data, string cookie)
        {
           /// string content = request.HttpGet("http://www.hyey.cn/Drug/DrugList.aspx", "fl=2&syz=14&ypmc=盐酸多巴酚丁胺注射液");

            string content = request.HttpGet(url, data, cookie);

            int startIndex = content.IndexOf("item bo");

            int endIndex = content.LastIndexOf("</a></em>");

            content = content.Substring(startIndex, endIndex - startIndex);

            MatchCollection ms = CommonFun.GetValues(content, "<em><a", "</a></em>");

            foreach (Match m in ms)
            {
                if (!m.Value.Contains("注射"))
                {
                    string value = CommonFun.GetValue(m.Value, "href=\"", "\"");

                    shopAllMenuURL.Add(value);

                    // Console.WriteLine("value:{0}", value);
                }
            }
        }

        /// <summary>
        /// 获取所有商城数据
        /// </summary>
        public void ReadShopAllItem()
        {
            List<string> items = new List<string>();

            try
            {
                int count = 0;
                foreach (string url in shopAllMenuURL)
                {
                    count++;
                    string content = request.HttpGet("http://www.hyey.cn" + url, null);

                    List<string> temp = new List<string>();

                    temp = GetItmeStr(content);

                    if (temp.Count > 0)
                    {
                        items.AddRange(temp);

                        string pageStr = CommonFun.GetValue(content, "<span id=\"lblTotalPageV\" style=\"border-color: #00a1fd;\">", "</span>");

                        if (string.IsNullOrEmpty(pageStr))
                        {
                            Console.WriteLine(pageStr);
                        }
                        else
                        {
                            int pageIndex = Convert.ToInt32(pageStr);

                            for (int i = 1; i < pageIndex; i++)
                            {
                                string postString = null;

                                string otherParam = GetOtherParam(temp);

                                postString = string.Format("__EVENTTARGET=&__EVENTARGUMENT=&__VIEWSTATE={0}&__EVENTVALIDATION={1}&TextBox1=&txtSelect=&HiddenField1=&hidd=&hiddLV=1&{2}btnNextV={3}&txtPageSizeV=&hidPageV=1", System.Web.HttpUtility.UrlEncode(GetParamValue(content, "__VIEWSTATE")), System.Web.HttpUtility.UrlEncode(GetParamValue(content, "__EVENTVALIDATION")), otherParam, "下一页");

                                content = request.HttpPost("http://www.hyey.cn" + url, postString, "");

                                temp = GetItmeStr(content);

                                items.AddRange(temp);
                            }
                        }

                        // Console.WriteLine(content);
                    }
                    else
                    {
                        Console.WriteLine(url);
                    }

                    Console.WriteLine("Count:{0}, tatalCount:{1}, URL:{2}", count, shopAllMenuURL.Count, url);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            CreateItemInfo(items);
        }

        /// <summary>
        /// 生成商城信息
        /// </summary>
        /// <param name="items"></param>
        private void CreateItemInfo(List<string> items)
        {
            string temp = null;
            try
            {
                Console.WriteLine("ItemsCount:{0}", items.Count);

                foreach (string item in items)
                {
                    temp = item;

                    ItemInfo info = new ItemInfo();
                    MatchCollection ms = CommonFun.GetValues(item, "value=\"", "\"");
                    info.Name = ms[0].Value;

                    if (info.Name.Contains("注射"))
                    {
                        continue;
                    }

                    info.Format = ms[1].Value + "/" + ms[3];
                    info.Format = info.Format.Replace("*", "x");
                    info.Format = info.Format.Replace("s", "片");
                    info.Format = info.Format.Replace("代", "袋");

                    info.Created = ms[4].Value;

                    info.SellType = CommonFun.GetValue(item, "【", "】");
                    
                    if (info.SellType.Count() > 1)
                    {
                        info.SellType = CommonFun.GetValue(info.SellType, ">", "<");
                    }

                    info.Inventory = CommonFun.GetValue(item, "数量：", "<");
                    info.ID = "国药准字" + CommonFun.GetValue(item, "批准文号：", "<");
                    info.ShopPrice = Convert.ToDecimal(CommonFun.GetValue(item, "价格：<span>", "元"));

                    string key = info.ID + "{" + info.Format + "}";

                    if (shopAllItems.ContainsKey(key))
                    {
                        if (shopAllItems[key].ShopPrice > info.ShopPrice)
                        {
                            shopAllItems[key] = info;
                        }
                    }
                    else
                    {
                        shopAllItems.Add(key, info);
                    }

                    // CommonFun.WriteCSV("商城信息.csv", info);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("content;{0}, error:{1}", temp, ex.ToString());
            }

            Console.WriteLine("ShopAllItemCount:{0}", shopAllItems.Count);
        }


        private string GetOtherParam(List<string> items)
        {
            string param = "";
            foreach (string item in items)
            {
                MatchCollection msName = CommonFun.GetValues(item, "name=\"", "\"");

                MatchCollection msValue = CommonFun.GetValues(item, "value=\"", "\"");

                int i = 0;
                foreach (Match name in msName)
                {
                    string nameStr = System.Web.HttpUtility.UrlEncode(name.ToString());
                    if (i == 7)
                    {
                        param += (nameStr + "=&");
                    }
                    else
                    {
                        param += (nameStr + "=" + System.Web.HttpUtility.UrlEncode(msValue[i].ToString()) + "&");
                    }

                    i++;

                }
            }
            //Console.WriteLine(param);
            return param;
        }

        private string GetParamValue(string content, string name)
        {
            string viewStateFlag = "id=\"" + name + "\" value=\"";
            int len1 = content.IndexOf(viewStateFlag) + viewStateFlag.Length;
            int len2 = content.IndexOf("\"", len1);
            string viewState = content.Substring(len1, len2 - len1);

            return viewState;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        private List<string> GetItmeStr(string content)
        {
            List<string> items = new List<string>();

            MatchCollection ms = CommonFun.GetValues(content, "bigimage\">", "<div></div>");

            foreach (Match m in ms)
            {
                items.Add(m.Value);

                //Console.WriteLine(m.Value);
            }

            return items;
        }
        /// <summary>
        /// 获取平台数据
        /// </summary>
        public void ReadPlatformItem()
        {

        }

        /// <summary>
        /// 比较差价
        /// </summary>
        /// <param name="item"></param>
        /// <param name="platformItem"></param>
        /// <returns></returns>
        public decimal ComparePrice(ItemInfo item, ItemInfo platformItem)
        {
            return 0;
        }

        public void ReadAllMenuURL(string url, string data, string cookie)
        {
            ReadShopALLMenuURL(url, data, cookie);
        }

        public void ReadAllItem()
        {
            ReadShopAllItem();
        }
    }
}
