using GetWebPageDate.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace GetWebPageDate.Util
{
    public class ReadPlatFormWebPageValue : IReadWebPage
    {
        /// <summary>
        /// 商城所有菜单URL
        /// </summary>
        private List<string> shopAllMenuURL;

        /// <summary>
        /// 平台的商品
        /// </summary>
        private Dictionary<string, ItemInfo> platformItems;


        private HttpRequest request;

        private int finishCount;

        private int toltalCount;

        public ReadPlatFormWebPageValue()
        {
            shopAllMenuURL = new List<string>();

            platformItems = new Dictionary<string, ItemInfo>();

            request = new HttpRequest();
        }

        public void ReadAllMenuURL()
        {
            string content = request.HttpGetPlatform("http://www.yaofangwang.com/Catalog-1.html");

            content = CommonFun.GetValue(content, "<div id=\"wrap\">", "<div class=\"block clearfix lazyload\">");

            MatchCollection ms = CommonFun.GetValues(content, "<a href=\"", "\"");

            foreach (Match m in ms)
            {
                shopAllMenuURL.Add(m.Value);
            }
        }

        public bool IsInCatalog(string catalog)
        {
            switch (catalog)
            {
                case "Catalog-36.html":
                case "Catalog-37.html":
                case "Catalog-38.html":
                case "Catalog-39.html":
                case "Catalog-40.html":
                case "Catalog-41.html":
                case "Catalog-42.html":
                case "Catalog-43.html":
                case "Catalog-44.html":
                    return true;
            }

            return false;
        }

        public void ReadAllItem()
        {
            List<string> itemNames = new List<string>();

            try
            {
                int count = 0;

                foreach (string url in shopAllMenuURL)
                {
                    count++;

                    string content = request.HttpGetPlatform("http://www.yaofangwang.com/" + url);

                    List<string> temp = GetItemName(content);

                    if (temp.Count > 0)
                    {
                        itemNames.AddRange(temp);

                        string pageStr = CommonFun.GetValue(content, "<span class=\"num\"><label>1</label> /", "<");

                        if (string.IsNullOrEmpty(pageStr))
                        {
                            Console.WriteLine("The url is null: {0}", url);
                        }
                        else
                        {
                            int pageCount = Convert.ToInt32(pageStr);

                            for (int i = 2; i <= pageCount; i++)
                            {
                                content = request.HttpGetPlatform("http://www.yaofangwang.com/" + url.Substring(0, url.IndexOf(".")) + "-p" + i + ".html");

                                temp = GetItemName(content);

                                itemNames.AddRange(temp);
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("the url havn't content: {0}", url);
                    }

                    Console.WriteLine("Count:{0}, tatalCount:{1}, URL:{2}", count, shopAllMenuURL.Count, url);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            CreateItemInfo(itemNames);

        }

        /// <summary>
        /// 获取药品名称
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        private List<string> GetItemName(string content)
        {
            List<string> items = new List<string>();

            string itemContent = CommonFun.GetValue(content, "<ul class=\"goodlist clearfix\">", "<div class=\"pager clearfix\">");

            MatchCollection ms = CommonFun.GetValues(itemContent, "alt=\"", "\"");

            foreach (Match m in ms)
            {
                items.Add(m.Value);
            }

            return items;
        }

        /// <summary>
        /// 提取药品信息
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        private List<string> GetItemStr(string content)
        {
            List<string> items = new List<string>();

            string itemContent = CommonFun.GetValue(content, "<ul class=\"goodlist clearfix\">", "</ul>");

            MatchCollection ms = CommonFun.GetValues(itemContent, "<li>", " </li>");

            foreach (Match m in ms)
            {
                items.Add(m.Value);
            }

            return items;
        }

        public List<ItemInfo> SeachInfoByID(string id)
        {
            List<ItemInfo> infos = new List<ItemInfo>();

            string url = string.Format("http://www.yaofangwang.com/search.html?keyword={0}", id);

            string content = request.HttpGetPlatform(url);

            List<string> items = GetItemStr(content);

            foreach (string item in items)
            {
                bool result = false;
                do
                {
                    try
                    {
                        ItemInfo info = new ItemInfo();

                        info.Format = CommonFun.GetValue(item, "规格：", "<");
                        info.Created = CommonFun.GetValue(item, "生产厂家：", "<");
                        //库存
                        info.ID = CommonFun.GetValue(item, "批准文号：", "<");

                        string priceStr = CommonFun.GetValue(item, "¥", "<");

                        info.ShopPrice = string.IsNullOrEmpty(priceStr) ? 0 : Convert.ToDecimal(priceStr);

                        content = request.HttpGetPlatform(CommonFun.GetValue(item, "<a target=\"_blank\" href=\"", "\""));

                        int startIndex = content.IndexOf("<div class=\"share clearfix\">");

                        int endIndx = content.IndexOf("id=\"priceA\">");

                        content = content.Substring(startIndex, endIndx - startIndex);

                        info.ViewCount = CommonFun.GetValue(content, "<dt>最近浏览</dt><dd class=\"w1\">", "次");

                        info.Name = CommonFun.GetValue(content, "<dd class=\"w2 l\"><strong>", "<");

                        MatchCollection ms = CommonFun.GetValues(content, "<dd class=\"w3 l\">", "</");

                        info.Type = ms[1].Value;

                        infos.Add(info);

                        result = true;
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                } while (!result);
            }

            return infos;
        }

        private void CreateItemInfo(List<string> itemNames)
        {
            string temp = null;

            try
            {
                ThreadPool.SetMaxThreads(10, 10);

                Console.WriteLine("ItemsCount:{0}", itemNames.Count);
                toltalCount = itemNames.Count;
                foreach (string name in itemNames)
                {
                    ThreadPool.QueueUserWorkItem(CreateItemOtherInfo, name);

                    //if (name.Contains("注射"))
                    //{
                    //    continue;
                    //}

                    //string url = string.Format("http://www.yaofangwang.com/search.html?keyword={0}", System.Web.HttpUtility.UrlEncode(name));

                    //string content = request.HttpGetPlatform(url);

                    //List<string> items = GetItemStr(content);

                    //foreach (string item in items)
                    //{
                    //    temp = item;

                    //    ItemInfo info = new ItemInfo();

                    //    info.Format = CommonFun.GetValue(item, "规格：", "<");
                    //    info.Created = CommonFun.GetValue(item, "生产厂家：", "<");
                    //    //库存
                    //    info.ID = CommonFun.GetValue(item, "批准文号：", "<");

                    //    string priceStr = CommonFun.GetValue(item, "¥", "<");

                    //    try
                    //    {
                    //        info.ShopPrice = string.IsNullOrEmpty(priceStr) ? 0 : Convert.ToDecimal(priceStr);
                    //    }
                    //    catch (Exception ex)
                    //    {
                    //        Console.WriteLine(ex);
                    //    }


                    //    content = request.HttpGetPlatform(CommonFun.GetValue(item, "<a target=\"_blank\" href=\"", "\""));

                    //    content = CommonFun.GetValue(content, "<div class=\"share clearfix\">", "id=\"priceA\">");

                    //    info.ViewCount = CommonFun.GetValue(content, "<dt>最近浏览</dt><dd class=\"w1\">", "次");

                    //    info.Name = CommonFun.GetValue(content, "<dd class=\"w2 l\"><strong>", "<");

                    //    MatchCollection ms = CommonFun.GetValues(content, "<dd class=\"w3 l\">", "</");

                    //    info.Type = ms[1].Value;

                    //    string key = info.ID + "{" + info.Format + "}";

                    //    if (platformItems.ContainsKey(key))
                    //    {
                    //        if (platformItems[key].ShopPrice > info.ShopPrice)
                    //        {
                    //            platformItems[key] = info;
                    //        }
                    //    }
                    //    else
                    //    {
                    //        platformItems.Add(key, info);
                    //    }

                    //    CommonFun.WriteCSV("平台信息.csv", info);
                    //}
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("content;{0}, error:{1}", temp, ex.ToString());
            }

            Console.WriteLine("PlatformItemsCount:{0}", platformItems.Count);
        }

        private void CreateItemOtherInfo(object value)
        {
            string temp = null;

            try
            {
                string name = (string)value;

                if (name.Contains("注射"))
                {
                    return;
                }

                HttpRequest tempRequest = new HttpRequest();

                string url = string.Format("http://www.yaofangwang.com/search.html?keyword={0}", System.Web.HttpUtility.UrlEncode(name));

                string content = tempRequest.HttpGetPlatform(url);

                List<string> items = GetItemStr(content);

                foreach (string item in items)
                {
                    bool result = false;
                    do
                    {

                        try
                        {
                            temp = item;

                            ItemInfo info = new ItemInfo();

                            info.Format = CommonFun.GetValue(item, "规格：", "<");
                            info.Created = CommonFun.GetValue(item, "生产厂家：", "<");
                            //库存
                            info.ID = CommonFun.GetValue(item, "批准文号：", "<");

                            string priceStr = CommonFun.GetValue(item, "¥", "<");

                            info.ShopPrice = string.IsNullOrEmpty(priceStr) ? 0 : Convert.ToDecimal(priceStr);

                            content = tempRequest.HttpGetPlatform(CommonFun.GetValue(item, "<a target=\"_blank\" href=\"", "\""));

                            int startIndex = content.IndexOf("<div class=\"share clearfix\">");

                            int endIndx = content.IndexOf("id=\"priceA\">");

                            content = content.Substring(startIndex, endIndx - startIndex);

                            info.ViewCount = CommonFun.GetValue(content, "<dt>最近浏览</dt><dd class=\"w1\">", "次");

                            info.Name = CommonFun.GetValue(content, "<dd class=\"w2 l\"><strong>", "<");

                            MatchCollection ms = CommonFun.GetValues(content, "<dd class=\"w3 l\">", "</");

                            info.Type = ms[1].Value;

                            string key = info.ID + "{" + info.Format + "}";

                            if (platformItems.ContainsKey(key))
                            {
                                if (platformItems[key].ShopPrice > info.ShopPrice)
                                {
                                    platformItems[key] = info;
                                }
                            }
                            else
                            {
                                platformItems.Add(key, info);
                            }

                            CommonFun.WriteCSV("平台信息.csv", info);
                            result = true;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.ToString());
                        }
                    } while (!result);
                }

                Console.WriteLine("finishName;{0}, finishCoutn:{1}, totalCount:{2}", name, ++finishCount, toltalCount);

                if (finishCount == toltalCount)
                {
                    Console.WriteLine("Finished !!!!!!!!!!!!!!!!!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("content;{0}, error:{1}", temp, ex.ToString());
            }

        }

        public void ReadAllMenuURL(string url, string data, string cookie)
        {
            throw new NotImplementedException();
        }
    }
}
