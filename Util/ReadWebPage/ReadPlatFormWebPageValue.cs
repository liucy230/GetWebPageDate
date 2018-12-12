using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using GetWebPageDate.Http;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Configuration;
using GetWebPageDate.Util.Item;

namespace GetWebPageDate.Util
{
    /// <summary>
    /// 药房网
    /// </summary>
    public class ReadPlatFormWebPageValue : BaseReadWebPage
    {
        private string username;

        private string password;

        private int finishCount;

        private int toltalCount;

        private string fileName = "Platform/Platform" + DateTime.Now.Ticks + ".csv";

        private string username1;

        private string password1;

        private string username2;

        private string password2;

        private string selfName = "佛山市南海区品珍药店";

        private int waitTime = 3000;

        private string filePath = "platform/";
        //private string downTime = "7:00-19:00";

        private string senderName = "王生";

        private string senderPhoneNumber = "18022768274";

        private ulong startOrderNO;

        private List<string> redTagList;

        /// <summary>
        /// 订单号是否为升序
        /// </summary>
        private bool isAsc;

        public ReadPlatFormWebPageValue()
        {
            string userInfo1 = ConfigurationManager.AppSettings["yfUsernameAndPossword1"];
            string[] aUserInfo1 = userInfo1.Split(',');
            string userInfo2 = ConfigurationManager.AppSettings["yfUsernameAndPossword2"];
            string[] aUserInfo2 = userInfo2.Split(',');
            string userInfo3 = ConfigurationManager.AppSettings["yfUsernameAndPossword3"];
            string[] aUserInfo3 = userInfo3.Split(',');
            username = aUserInfo1[0];
            password = aUserInfo1[1];
            username1 = aUserInfo2[0];
            password1 = aUserInfo2[1];
            username2 = aUserInfo3[0];
            password2 = aUserInfo3[1];
            string infoConfig = ConfigurationManager.AppSettings["senderInfoKey"];
            if (!string.IsNullOrEmpty(infoConfig))
            {
                string[] infoArray = infoConfig.Split(',');
                senderName = infoArray[0];
                senderPhoneNumber = infoArray[1];
            }
            selfName = ConfigurationManager.AppSettings["yfSelfName"];
            fileName = "YF/";
        }

        public override void ReadAllMenuURL()
        {
            string content = request.HttpGetPlatform("http://www.yaofangwang.com/Catalog-1.html");

            content = CommonFun.GetValue(content, "<div id=\"wrap\">", "<div class=\"block clearfix lazyload\">");

            MatchCollection ms = CommonFun.GetValues(content, "<a href=\"", "\"");

            foreach (Match m in ms)
            {
                AllMenuUrl.Add(m.Value);
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

        public override void ReadAllItem()
        {
            List<string> itemNames = new List<string>();

            try
            {
                int count = 0;

                foreach (string url in AllMenuUrl)
                {
                    count++;

                    string content = request.HttpGetPlatform("http://www.yaofangwang.com/" + url);

                    List<string> temp = GetItemName(content);

                    if (temp.Count > 0)
                    {
                        foreach (string name in temp)
                        {
                            if (!itemNames.Contains(name))
                            {
                                itemNames.Add(name);
                            }
                        }

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

                                foreach (string name in temp)
                                {
                                    if (!itemNames.Contains(name))
                                    {
                                        itemNames.Add(name);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("the url havn't content: {0}", url);
                    }

                    Console.WriteLine("Count:{0}, tatalCount:{1}, URL:{2}", count, AllMenuUrl.Count, url);
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

            MatchCollection ms = CommonFun.GetValues(itemContent, "<li", " </li>");

            foreach (Match m in ms)
            {
                items.Add(m.Value);
            }

            return items;
        }

        public void newPlate()
        {
            //搜索url
            string urlSeach = "https://open.yaofangwang.com/app_gateway.ashx?market=huawei&app_key=4fb44b67d0be2af36f7135586d38d658&account_id=1441303&keywords=%E5%9B%BD%E8%8D%AF%E5%87%86%E5%AD%97Z20030134&os=android&app_version=2.9.6&service=get_search_goods&page_index=1&latitude=0.0&timestamp=2018-04-19+17%3A14%3A12&longitude=0.0&sign=942b0d2bbe9ff02d1f317f12904ed625";

            //https://open.yaofangwang.com/app_gateway.ashx?market=huawei&app_key=4fb44b67d0be2af36f7135586d38d658&account_id=1441303&keywords=%E5%9B%BD%E8%8D%AF%E5%87%86%E5%AD%97Z20030134&os=android&app_version=2.9.6&service=get_search_goods&page_index=1&latitude=0.0&timestamp=2018-04-18+18%3A04%3A30&longitude=0.0&sign=4512a61f778a1f05ba6c72602a96fbf3
            //https://open.yaofangwang.com/app_gateway.ashx?market=huawei&app_key=4fb44b67d0be2af36f7135586d38d658&account_id=1441303&keywords=%E5%9B%BD%E8%8D%AF%E5%87%86%E5%AD%97Z20030134&os=android&app_version=2.9.6&service=get_search_goods&page_index=1&latitude=0.0&timestamp=2018-04-18+18%3A07%3A25&longitude=0.0&sign=da2c034f96625edfddf9a1d565495deb

            //0b0c7f38ba29bf6507e01d8ab79dc0e0
            //22b8d1f3e5f05084b3dcfe38dbbd5a43
            string content = request.HttpGetPlatform(urlSeach);
            //获取商品信息url
            string itemInfo = "https://open.yaofangwang.com/app_gateway.ashx?market=huawei&app_key=4fb44b67d0be2af36f7135586d38d658&account_id=1441303&os=android&app_version=2.9.6&service=get_goods_detail&latitude=0.0&id=571177&timestamp=2018-04-17+17%3A16%3A28&longitude=0.0&sign=29f140ad5a407d7ca29a729caf016a7d";

            //获取商家列表url
            string shopList = "https://open.yaofangwang.com/app_gateway.ashx?os=android&app_version=2.9.6&latitude=0.0&orderby=priceasc&market=huawei&account_id=1441303&app_key=4fb44b67d0be2af36f7135586d38d658&service=get_goods_shop&page_index=1&region_name=%E5%85%A8%E5%9B%BD&id=571177&timestamp=2018-04-17+17%3A20%3A53&longitude=0.0&sign=b62cddd38efa3d1cfbe3d9898f89ec67";
        }

        public decimal GetMinPrice(BaseItemInfo item, int inventoryMin)
        {
            try
            {
                string content;

                string prevUrl = null;

                content = request.HttpGet("http://www.yaofangwang.com/medicine-" + item.ItemName + ".html?sort=price&sorttype=asc", null, true);

                string selaStr = CommonFun.GetValue(content, "class=\"all default_cursor fb_red mr10\">", "折");

                do
                {
                    if (!string.IsNullOrEmpty(prevUrl))
                    {
                        prevUrl = "https://www.yaofangwang.com" + prevUrl;
                        if (!prevUrl.Contains("?sort=price&sorttype=asc"))
                        {
                            prevUrl += "?sort=price&sorttype=asc";
                        }
                        content = request.HttpGetPlatform(prevUrl);
                    }

                    string sellerCount = CommonFun.GetValue(content, "class=\"cur\">", "个零售商家报价");

                    if (sellerCount == "1")
                    {
                        //inventoryMin = 50;
                        Console.WriteLine("sellerCount :{0}", sellerCount);
                    }

                    string itemlist = CommonFun.GetValue(content, "<ul class=\"slist\">", "</ul>");

                    MatchCollection ms = CommonFun.GetValues(itemlist, "<li class=\"clearfix\">", "</li>");

                    foreach (Match m in ms)
                    {
                        string inventoryStr = CommonFun.GetValue(m.Value, "<label class=\"sreserve\">", "</label>");
                        string priceStr = CommonFun.GetValue(m.Value, "¥", "<");
                        string storeName = CommonFun.GetValue(m.Value, "主页\">", "</a>");

                        if (!string.IsNullOrEmpty(inventoryStr) && !string.IsNullOrEmpty(priceStr) && !IsBlacklistStore(storeName) && storeName != selfName)
                        {
                            if (Convert.ToInt32(inventoryStr) > inventoryMin)
                            {
                                BaseItemInfo info = new BaseItemInfo();

                                info.Sela = 10;

                                if (!string.IsNullOrEmpty(selaStr))
                                {
                                    if (selaStr.Contains("立享"))
                                    {
                                        selaStr = selaStr.Replace("立享", "");
                                        info.Sela = Convert.ToDecimal(selaStr);

                                    }
                                    else if (selaStr.Contains("最高返"))
                                    {
                                        selaStr = CommonFun.GetValue(selaStr, "最高返", "元");
                                        info.ReturnPrice = Convert.ToDecimal(selaStr);
                                    }
                                    else
                                    {
                                        info.Remark = selaStr;
                                    }
                                }
                                else
                                {
                                    selaStr = CommonFun.GetValue(m.Value, "返现", "元");

                                    if (!string.IsNullOrEmpty(selaStr))
                                    {
                                        info.ReturnPrice = Convert.ToDecimal(selaStr);
                                    }
                                }

                                info.ShopPrice = string.IsNullOrEmpty(priceStr) ? 0 : Convert.ToDecimal(priceStr);
                                info.ShopSelaPrice = CommonFun.TrunCate(info.ShopPrice * (info.Sela / 10) - info.ReturnPrice);
                                info.Inventory = inventoryStr;
                                // SetMenuInfo(info, content);

                                SetPriceInfo(info, content);

                                return info.ShopSelaPrice;
                            }
                        }
                        else if (content.Contains("安装APP查看价格"))
                        {
                            return decimal.MaxValue;
                        }
                    }

                    // nextStr = CommonFun.GetValue(content, "<a class=\"prev\" disabled=\"disabled\" style=\"margin-right:5px;\">", "</a>");
                    prevUrl = CommonFun.GetValue(content, "上一页", "</div>");
                    prevUrl = CommonFun.GetValue(prevUrl, "<a class=\"prev\" href=", "下一页");
                    prevUrl = string.IsNullOrEmpty("prevUrl") ? prevUrl : CommonFun.GetValue(prevUrl, "\"", "\"");
                } while (!string.IsNullOrEmpty(prevUrl));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            return decimal.MaxValue;
        }

        private BaseItemInfo GetItem(string item, int inventoryMin)
        {
            try
            {
                string content;

                string prevUrl = null;

                string nextStr = null;

                content = request.HttpGetPlatform("http:" + CommonFun.GetValue(item, "<a target=\"_blank\" href=\"", "\"") + "?sort=price&sorttype=asc");

                string selaStr = CommonFun.GetValue(content, "class=\"all default_cursor fb_red mr10\">", "折");

                do
                {
                    if (!string.IsNullOrEmpty(prevUrl))
                    {
                        prevUrl = "https://www.yaofangwang.com" + prevUrl;
                        if (!prevUrl.Contains("?sort=price&sorttype=asc"))
                        {
                            prevUrl += "?sort=price&sorttype=asc";
                        }
                        content = request.HttpGetPlatform(prevUrl);
                    }

                    string sellerCount = CommonFun.GetValue(content, "class=\"cur\">", "个零售商家报价");

                    if (sellerCount == "1")
                    {
                        inventoryMin = 50;
                        Console.WriteLine("sellerCount :{0}", sellerCount);
                    }

                    string itemlist = CommonFun.GetValue(content, "<ul class=\"slist\">", "</ul>");

                    MatchCollection ms = CommonFun.GetValues(itemlist, "<li class=\"clearfix\">", "</li>");

                    foreach (Match m in ms)
                    {
                        string inventoryStr = CommonFun.GetValue(m.Value, "<label class=\"sreserve\">", "</label>");
                        string priceStr = CommonFun.GetValue(m.Value, "¥", "<");
                        string storeName = CommonFun.GetValue(m.Value, "主页\">", "</a>");
                        if (!string.IsNullOrEmpty(inventoryStr) && !string.IsNullOrEmpty(priceStr) && !IsBlacklistStore(storeName))
                        {
                            if (Convert.ToInt32(inventoryStr) > inventoryMin)
                            {
                                BaseItemInfo info = new BaseItemInfo();

                                info.Format = CommonFun.GetValue(item, "规格：", "<");

                                info.Created = CommonFun.GetValue(item, "生产厂家：", "<");

                                info.ID = CommonFun.GetValue(item, "批准文号：", "<");

                                info.Sela = 10;

                                if (!string.IsNullOrEmpty(selaStr))
                                {
                                    if (selaStr.Contains("立享"))
                                    {
                                        selaStr = selaStr.Replace("立享", "");
                                        info.Sela = Convert.ToDecimal(selaStr);

                                    }
                                    else if (selaStr.Contains("最高返"))
                                    {
                                        selaStr = CommonFun.GetValue(selaStr, "最高返", "元");
                                        info.ReturnPrice = Convert.ToDecimal(selaStr);
                                    }
                                    else
                                    {
                                        info.Remark = selaStr;
                                    }
                                }
                                else
                                {
                                    selaStr = CommonFun.GetValue(m.Value, "返现", "元");

                                    if (!string.IsNullOrEmpty(selaStr))
                                    {
                                        info.ReturnPrice = Convert.ToDecimal(selaStr);
                                    }
                                }

                                info.ShopPrice = string.IsNullOrEmpty(priceStr) ? 0 : Convert.ToDecimal(priceStr);
                                info.ShopSelaPrice = info.ShopPrice * (info.Sela / 10) - info.ReturnPrice;
                                info.Inventory = inventoryStr;
                                // SetMenuInfo(info, content);

                                SetPriceInfo(info, content);

                                return info;
                            }
                        }
                        else if (content.Contains("安装APP查看价格"))
                        {
                            BaseItemInfo info = new BaseItemInfo();
                            info.ShopSelaPrice = Decimal.MaxValue;
                            return info;
                        }
                    }

                    // nextStr = CommonFun.GetValue(content, "<a class=\"prev\" disabled=\"disabled\" style=\"margin-right:5px;\">", "</a>");
                    prevUrl = CommonFun.GetValue(content, "上一页", "</div>");
                    prevUrl = CommonFun.GetValue(prevUrl, "<a class=\"prev\" href=", "下一页");
                    prevUrl = string.IsNullOrEmpty("prevUrl") ? prevUrl : CommonFun.GetValue(prevUrl, "\"", "\"");
                } while (!string.IsNullOrEmpty(prevUrl));

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            return null;
        }

        public List<BaseItemInfo> SeachInfoByID(string id, int inventoryMin = 5)
        {
            List<BaseItemInfo> infos = new List<BaseItemInfo>();

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
                        BaseItemInfo info = GetItem(item, inventoryMin);
                        result = true;
                        if (info != null)// && (info.ID.Trim() == id || info.Name.Trim() == id))
                        {
                            if (info.ShopSelaPrice != decimal.MaxValue)
                            {
                                infos.Add(info);
                            }
                            else
                            {
                                result = false;
                                Console.WriteLine("{0} Relogin............................", DateTime.Now);
                                Login();
                            }
                        }
                        //else
                        //{
                        //    result = false;
                        //    Console.WriteLine("{0} Item is null. Relogin............................", DateTime.Now);    
                        //    Login();
                        //}
                    }
                    catch (Exception ex)
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
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("content;{0}, error:{1}", temp, ex.ToString());
            }

            Console.WriteLine("PlatformItemsCount:{0}", ShopAllItems.Count);
        }

        public void CreateItemOtherInfo(object value)
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

                            BaseItemInfo info = new BaseItemInfo();

                            info.Format = CommonFun.GetValue(item, "规格：", "<");

                            info.Created = CommonFun.GetValue(item, "生产厂家：", "<");
                            //库存
                            info.ID = CommonFun.GetValue(item, "批准文号：", "<");

                            string priceStr = CommonFun.GetValue(item, "¥", "<");

                            if (string.IsNullOrEmpty(priceStr))
                            {
                                result = true;
                                continue;
                            }

                            info.ShopPrice = string.IsNullOrEmpty(priceStr) ? 0 : Convert.ToDecimal(priceStr);

                            content = tempRequest.HttpGetPlatform("http:" + CommonFun.GetValue(item, "<a target=\"_blank\" href=\"", "\""));

                            string selaStr = CommonFun.GetValue(content, "class=\"all default_cursor fb_red mr10\">", "折");

                            if (!string.IsNullOrEmpty(selaStr))
                            {
                                info.Sela = Convert.ToDecimal(selaStr);
                            }
                            else
                            {
                                info.Sela = 10;
                            }

                            info.ShopSelaPrice = info.ShopPrice * (info.Sela / 10);

                            // SetMenuInfo(info, content);

                            SetPriceInfo(info, content);

                            //SetMainInfo(info, content);

                            AddItme(info, fileName, false);

                            string key = info.ID + "{" + info.Format + "}";

                            if (ShopAllItems.ContainsKey(key))
                            {
                                if (ShopAllItems[key].ShopPrice > info.ShopPrice)
                                {
                                    ShopAllItems[key] = info;
                                    AddItme(info, "Platform/Platform.csv");
                                }
                            }
                            else
                            {
                                ShopAllItems.Add(key, info);
                                AddItme(info, fileName);
                            }

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

        /// <summary>
        /// 上架新品
        /// </summary>
        public void UpNowItem(BaseItemInfo item)
        {
            try
            {
                string sUrl = string.Format("https://yaodian.yaofangwang.com/Manage/Handler/Handler.ashx?method=GetMedicineListByAuthorizedCode&AuthorizedCode={0}&s=1536995477", item.ID); //get

                string subUrl = "https://yaodian.yaofangwang.com/Manage/Products/productAdd.aspx";//post



            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
        private void SetMenuInfo(ItemInfo info, string content)
        {
            string menuStr = CommonFun.GetValue(content, "breadcrumb", "</div>");

            MatchCollection menuMs = CommonFun.GetValues(menuStr, "\">", "</a>");

            info.Menu1 = menuMs[1].Value;

            info.Menu2 = menuMs[2].Value;

            info.Menu3 = menuMs[3].Value;

            info.ItemName = CommonFun.GetValue(menuStr, "<strong>", "</strong>").Replace("价格", "");
        }

        private void SetPriceInfo(BaseItemInfo info, string content)
        {
            int startIndex = content.IndexOf("<div class=\"share clearfix\">");

            int endIndx = content.IndexOf("id=\"priceA\">");

            string itemInfoStr = content.Substring(startIndex, endIndx - startIndex);

            info.ViewCount = CommonFun.GetValue(itemInfoStr, "<dt>最近浏览</dt><dd class=\"w1\">", "次");

            info.Name = CommonFun.GetValue(itemInfoStr, "<dd class=\"w2 l\"><strong>", "<");

            MatchCollection ms = CommonFun.GetValues(itemInfoStr, "<dd class=\"w3 l\">", "</");

            //string brandNameStr = ms[0].Value;

            //if (!string.IsNullOrEmpty(brandNameStr))
            //{
            //    info.BrandName = brandNameStr.Substring(0, brandNameStr.IndexOf('<'));
            //}

            info.Type = ms[1].Value;
        }

        private void SetMainInfo(ItemInfo info, string content)
        {
            string mainInfoStr = CommonFun.GetValue(content, "<dl class=\"maininfo clearfix\">", "</dl>");

            MatchCollection msTitle = CommonFun.GetValues(mainInfoStr, "<dt><strong>", "</strong></dt>");

            MatchCollection ms = CommonFun.GetValues(mainInfoStr, "<dd>", "</dd>");


            for (int i = 0; i < msTitle.Count; i++)
            {
                if (msTitle[i].Value.Contains("性状"))
                {
                    info.Character = ms[i].Value;
                }
                else if (msTitle[i].Value.Contains("主治") || msTitle[i].Value.Contains("适应症"))
                {
                    info.Function = ms[i].Value;
                }
                else if (msTitle[i].Value.Contains("不良反应"))
                {
                    info.AdverseReaction = ms[i].Value;
                }
                else if (msTitle[i].Value.Contains("禁忌症"))
                {
                    info.Contraindication = ms[i].Value;
                }
                else if (msTitle[i].Value.Contains("注意事项"))
                {
                    info.NoticMatters = ms[i].Value;
                }
                else if (msTitle[i].Value.Contains("用药"))
                {
                    info.Use += msTitle[i].Value + ms[i].Value;
                }
                else if (msTitle[i].Value.Contains("贮藏"))
                {
                    info.SaveType = ms[i].Value;
                }
            }

            info.PicturePath = @"Picture/" + info.Name + "/";

            MatchCollection msUrl = CommonFun.GetValues(mainInfoStr, "href=\"", "\"");

            int pictureCount = 0;
            foreach (Match url in msUrl)
            {
                SavePicture(url.Value, info.PicturePath + pictureCount++ + ".jpg");
            }
        }

        private void SavePicture(string url, string filePath)
        {
            Image image = request.HttpGetPicture(url);

            CommonFun.SavePicture(image, filePath);
        }

        public override void Login()
        {
            string login_url = "https://reg.yaofangwang.com/login.aspx";
            string postDataStr = string.Format("__EVENTTARGET=ctl00%24ContentPlaceHolder1%24t_login&__EVENTARGUMENT=&__VIEWSTATE=%2FwEPDwUINDE0MjQ5NzVkGAEFHl9fQ29udHJvbHNSZXF1aXJlUG9zdEJhY2tLZXlfXxYBBSVjdGwwMCRDb250ZW50UGxhY2VIb2xkZXIxJGNiX1JlbWVtYmVyfMPsdmjZlfvGbCxOD8u0r%2FRLbA8%3D&__VIEWSTATEGENERATOR=C2EE9ABB&__EVENTVALIDATION=%2FwEdAAosVaXMhV4q7s19NxAQU1EA%2FX3Fo%2FRaqYiLtErA%2B0XLEhccBLe9MIf%2BOeu1SwHT%2Fo2ng0PqTUAPaPYHk4tr%2FTPfKqmCwJguV16MgvgQAIOIM5gCICnqqodEefzHnITuNvGKN2iu4q6IDCzyu2cVK%2B2X9v9Eq8t4s5ZZ5SNoKNrRyao0KorV0rlA31R%2FnAfLx9YONrIXtTlQ%2FOFtaven3EmBxJNqndFqhuABr6rHIEGR90USvzc%3D&ctl00%24ContentPlaceHolder1%24txt_AccountName={0}&ctl00%24ContentPlaceHolder1%24txt_Password={1}&ctl00%24ContentPlaceHolder1%24txt_ValidateCode=&ctl00%24ContentPlaceHolder1%24txt_Mobile=&ctl00%24ContentPlaceHolder1%24txt_ValidateCode1=&ctl00%24ContentPlaceHolder1%24txtMobileCode=&ctl00%24ContentPlaceHolder1%24hf_type=default", username, password);

            request.Login(login_url, postDataStr);

            //SeachInfoByID("国药准字Z20090367");
        }

        public void Login(int type)
        {
            string login_url = "https://reg.yaofangwang.com/login.aspx";
            string postDataStr;
            switch (type)
            {
                case 1:
                    postDataStr = string.Format("__EVENTTARGET=ctl00%24ContentPlaceHolder1%24t_login&__EVENTARGUMENT=&__VIEWSTATE=%2FwEPDwUINDE0MjQ5NzVkGAEFHl9fQ29udHJvbHNSZXF1aXJlUG9zdEJhY2tLZXlfXxYBBSVjdGwwMCRDb250ZW50UGxhY2VIb2xkZXIxJGNiX1JlbWVtYmVyfMPsdmjZlfvGbCxOD8u0r%2FRLbA8%3D&__VIEWSTATEGENERATOR=C2EE9ABB&__EVENTVALIDATION=%2FwEdAAosVaXMhV4q7s19NxAQU1EA%2FX3Fo%2FRaqYiLtErA%2B0XLEhccBLe9MIf%2BOeu1SwHT%2Fo2ng0PqTUAPaPYHk4tr%2FTPfKqmCwJguV16MgvgQAIOIM5gCICnqqodEefzHnITuNvGKN2iu4q6IDCzyu2cVK%2B2X9v9Eq8t4s5ZZ5SNoKNrRyao0KorV0rlA31R%2FnAfLx9YONrIXtTlQ%2FOFtaven3EmBxJNqndFqhuABr6rHIEGR90USvzc%3D&ctl00%24ContentPlaceHolder1%24txt_AccountName={0}&ctl00%24ContentPlaceHolder1%24txt_Password={1}&ctl00%24ContentPlaceHolder1%24txt_ValidateCode=&ctl00%24ContentPlaceHolder1%24txt_Mobile=&ctl00%24ContentPlaceHolder1%24txt_ValidateCode1=&ctl00%24ContentPlaceHolder1%24txtMobileCode=&ctl00%24ContentPlaceHolder1%24hf_type=default", CommonFun.GetUrlEncode(username), password);
                    break;
                case 2:
                    postDataStr = string.Format("__EVENTTARGET=ctl00%24ContentPlaceHolder1%24t_login&__EVENTARGUMENT=&__VIEWSTATE=%2FwEPDwUINDE0MjQ5NzVkGAEFHl9fQ29udHJvbHNSZXF1aXJlUG9zdEJhY2tLZXlfXxYBBSVjdGwwMCRDb250ZW50UGxhY2VIb2xkZXIxJGNiX1JlbWVtYmVyfMPsdmjZlfvGbCxOD8u0r%2FRLbA8%3D&__VIEWSTATEGENERATOR=C2EE9ABB&__EVENTVALIDATION=%2FwEdAAosVaXMhV4q7s19NxAQU1EA%2FX3Fo%2FRaqYiLtErA%2B0XLEhccBLe9MIf%2BOeu1SwHT%2Fo2ng0PqTUAPaPYHk4tr%2FTPfKqmCwJguV16MgvgQAIOIM5gCICnqqodEefzHnITuNvGKN2iu4q6IDCzyu2cVK%2B2X9v9Eq8t4s5ZZ5SNoKNrRyao0KorV0rlA31R%2FnAfLx9YONrIXtTlQ%2FOFtaven3EmBxJNqndFqhuABr6rHIEGR90USvzc%3D&ctl00%24ContentPlaceHolder1%24txt_AccountName={0}&ctl00%24ContentPlaceHolder1%24txt_Password={1}&ctl00%24ContentPlaceHolder1%24txt_ValidateCode=&ctl00%24ContentPlaceHolder1%24txt_Mobile=&ctl00%24ContentPlaceHolder1%24txt_ValidateCode1=&ctl00%24ContentPlaceHolder1%24txtMobileCode=&ctl00%24ContentPlaceHolder1%24hf_type=default", CommonFun.GetUrlEncode(username1), password1);
                    break;
                case 3:
                    postDataStr = string.Format("__EVENTTARGET=ctl00%24ContentPlaceHolder1%24t_login&__EVENTARGUMENT=&__VIEWSTATE=%2FwEPDwUINDE0MjQ5NzVkGAEFHl9fQ29udHJvbHNSZXF1aXJlUG9zdEJhY2tLZXlfXxYBBSVjdGwwMCRDb250ZW50UGxhY2VIb2xkZXIxJGNiX1JlbWVtYmVyfMPsdmjZlfvGbCxOD8u0r%2FRLbA8%3D&__VIEWSTATEGENERATOR=C2EE9ABB&__EVENTVALIDATION=%2FwEdAAosVaXMhV4q7s19NxAQU1EA%2FX3Fo%2FRaqYiLtErA%2B0XLEhccBLe9MIf%2BOeu1SwHT%2Fo2ng0PqTUAPaPYHk4tr%2FTPfKqmCwJguV16MgvgQAIOIM5gCICnqqodEefzHnITuNvGKN2iu4q6IDCzyu2cVK%2B2X9v9Eq8t4s5ZZ5SNoKNrRyao0KorV0rlA31R%2FnAfLx9YONrIXtTlQ%2FOFtaven3EmBxJNqndFqhuABr6rHIEGR90USvzc%3D&ctl00%24ContentPlaceHolder1%24txt_AccountName={0}&ctl00%24ContentPlaceHolder1%24txt_Password={1}&ctl00%24ContentPlaceHolder1%24txt_ValidateCode=&ctl00%24ContentPlaceHolder1%24txt_Mobile=&ctl00%24ContentPlaceHolder1%24txt_ValidateCode1=&ctl00%24ContentPlaceHolder1%24txtMobileCode=&ctl00%24ContentPlaceHolder1%24hf_type=default", CommonFun.GetUrlEncode(username2), password2);
                    break;
                default:
                    postDataStr = string.Format("__EVENTTARGET=ctl00%24ContentPlaceHolder1%24t_login&__EVENTARGUMENT=&__VIEWSTATE=%2FwEPDwUINDE0MjQ5NzVkGAEFHl9fQ29udHJvbHNSZXF1aXJlUG9zdEJhY2tLZXlfXxYBBSVjdGwwMCRDb250ZW50UGxhY2VIb2xkZXIxJGNiX1JlbWVtYmVyfMPsdmjZlfvGbCxOD8u0r%2FRLbA8%3D&__VIEWSTATEGENERATOR=C2EE9ABB&__EVENTVALIDATION=%2FwEdAAosVaXMhV4q7s19NxAQU1EA%2FX3Fo%2FRaqYiLtErA%2B0XLEhccBLe9MIf%2BOeu1SwHT%2Fo2ng0PqTUAPaPYHk4tr%2FTPfKqmCwJguV16MgvgQAIOIM5gCICnqqodEefzHnITuNvGKN2iu4q6IDCzyu2cVK%2B2X9v9Eq8t4s5ZZ5SNoKNrRyao0KorV0rlA31R%2FnAfLx9YONrIXtTlQ%2FOFtaven3EmBxJNqndFqhuABr6rHIEGR90USvzc%3D&ctl00%24ContentPlaceHolder1%24txt_AccountName={0}&ctl00%24ContentPlaceHolder1%24txt_Password={1}&ctl00%24ContentPlaceHolder1%24txt_ValidateCode=&ctl00%24ContentPlaceHolder1%24txt_Mobile=&ctl00%24ContentPlaceHolder1%24txt_ValidateCode1=&ctl00%24ContentPlaceHolder1%24txtMobileCode=&ctl00%24ContentPlaceHolder1%24hf_type=default", CommonFun.GetUrlEncode(username), password);
                    break;
            }

            request.Login(login_url, postDataStr);
        }

        public override void Start()
        {
            Login();
            // SeachInfoByID("国药准字H20100129");

            ReadAllMenuURL();

            ReadAllItem();
        }

        public void Test()
        {
            Login(2);
            bool opt = true;
            BaseItemInfo item = new BaseItemInfo();
            item.ID = "国药准字Z20025028";
            item.ViewCount = "2461435";
            item.Format = "0.4gx54粒/瓶";
            item.Name = "前列倍喜胶囊";
            item.Created = "贵州太和制药有限公司";
            item.Type = "2017";
            item.Inventory = "52";
            item.ItemName = "196329";
            if (IsInTypeList(item.Type))
            {
                if (Convert.ToInt16(item.Inventory) > Convert.ToInt16(minStockList[0]))
                {
                    decimal minPrice = GetMinPrice(item, 10);

                    if (minPrice != decimal.MaxValue)
                    {
                        minPrice = minPrice - lPrice;

                        if (minPrice > 0 && minPrice != item.ShopPrice)
                        {
                            item.PlatformPrice = CommonFun.TrunCate(minPrice);
                            if (opt)
                            {
                                OptUpdatePrice(item);
                            }
                            Thread.Sleep(random.Next(5) * 1000);
                            CommonFun.WriteCSV("YF/updatePriceUpFive" + ticks + fileExtendName, item);
                        }
                    }
                }
                else
                {
                    decimal minPrice = GetMinPrice(item, 0);

                    if (minPrice != decimal.MaxValue)
                    {
                        minPrice = minPrice - Convert.ToDecimal(minStockList[1]);

                        if (minPrice > 0 && minPrice != item.ShopPrice)
                        {
                            item.PlatformPrice = CommonFun.TrunCate(minPrice);

                            if (opt)
                            {
                                OptUpdatePrice(item);
                            }
                            Thread.Sleep(random.Next(5) * 1000);
                            CommonFun.WriteCSV("YF/updatePriceLowFive" + ticks + fileExtendName, item);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 处理订单
        /// </summary>
        public void OptPrescription()
        {
            try
            {
                int page = 1;
                int totalPage = 0;
                Login(2);
                do
                {
                    try
                    {
                        string sUrl = string.Format("http://yaodian.yaofangwang.com/Manage/Sell/Order.aspx?status=AUDIT&page={0}", page);

                        string content = request.HttpGet(sUrl);

                        if (totalPage == 0)
                        {
                            string totalPageStr = CommonFun.GetValue(content, "条，共", "页，");

                            totalPage = Convert.ToInt32(totalPageStr);
                        }

                        Console.WriteLine("Running OptPrescription totalPage:{0} page:{1}", totalPage, page);

                        Dictionary<string, string> orderDic = new Dictionary<string, string>();
                        GetOrderNOAndDesc(content, orderDic, false);

                        foreach (string orderNO in orderDic.Keys)
                        {
                            string subUrl = "http://yaodian.yaofangwang.com/Manage/Handler/Handler.ashx";

                            string postData = string.Format("method=CustomerRxAuditResult&orderno={0}&audit=1&auditContent=%E5%A4%84%E6%96%B9%E7%85%A7%E7%89%87%E7%AC%A6%E5%90%88%E8%A6%81%E6%B1%82%E8%A7%84%E8%8C%83", orderNO);

                            string result = request.HttpPost(subUrl, postData);

                            if (result != "True")
                            {
                                Console.WriteLine(result + orderNO + "...........");
                            }

                            Console.WriteLine("{0} OptPrescription orderNO:{1}", DateTime.Now, orderNO);

                            Thread.Sleep(waitTime);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }

                } while (++page <= totalPage);



            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private string GetCreateDate()
        {
            DateTime date = DateTime.Now.AddMonths(-6);

            return date.ToString("yyyy-MM-dd");
        }

        private void GetOrderNOAndDesc(string content, Dictionary<string, string> orderDic, bool isGetDesc = true, bool isRank = false)
        {
            try
            {
                MatchCollection ms = CommonFun.GetValues(content, "<td class=\"bl0 qizi\">", "</td>");

                foreach (Match m in ms)
                {
                    string state = CommonFun.GetValue(m.Value, "rank='", "'");
                    if (isGetDesc)
                    {
                        if (!string.IsNullOrEmpty(state) && state == "00")
                        {
                            string desc = CommonFun.GetValue(m.Value, "desc='", "'");

                            if (!string.IsNullOrEmpty(desc))
                            {
                                string orderNO = CommonFun.GetValue(m.Value, "par_orderno='", "'");
                                if (desc.Length == 12 && desc == CommonFun.GetNum(desc))
                                {
                                    orderDic.Add(orderNO, desc);
                                }
                                else
                                {
                                    Console.WriteLine("{0} Desc Error orderNO:{1} desc:{2}", DateTime.Now, orderNO, desc);
                                }
                            }
                        }
                    }
                    else
                    {
                        if (!isRank || string.IsNullOrEmpty(state))
                        {
                            string orderNO = CommonFun.GetValue(m.Value, "par_orderno='", "'");
                            orderDic.Add(orderNO, "");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public void OptOrder()
        {
            //while(true)
            //{
            OptPrescription();

            OptLogistics();

            Console.WriteLine("{0} Finished.......................................", DateTime.Now);

            //Thread.Sleep(10 * 60 * 1000);
            //}
        }


        private void InitOptWaitingSendConfig()
        {
            string orderNO = ConfigurationManager.AppSettings["startNOKey"];

            if (string.IsNullOrEmpty(orderNO))
            {
                Console.WriteLine("please enter startNOKey......");

                orderNO = Console.ReadLine();
            }

            startOrderNO = Convert.ToUInt64(orderNO);

            redTagList = GetConfigList("redTagKey");

            string strDoAsc = ConfigurationManager.AppSettings["doAscKey"];

            isAsc = string.IsNullOrEmpty(strDoAsc) ? false : Convert.ToInt32(strDoAsc) > 0;
        }

        private bool IsRedTag(string type)
        {
            return redTagList.Contains(type);
        }

        /// <summary>
        /// 处理待发货订单
        /// </summary>
        public void OptWaitingSend()
        {
            try
            {
                Login(2);

                InitOptWaitingSendConfig();

                Dictionary<string, string> orderList = new Dictionary<string, string>();
                int page = 1;
                int totalPage = 0;
                do
                {
                    try
                    {
                        string sUrl = string.Format("http://yaodian.yaofangwang.com/Manage/Sell/Order.aspx?status=PAID&page={0}", page);

                        string content = request.HttpGet(sUrl);

                        if (totalPage == 0)
                        {
                            string totalPageStr = CommonFun.GetValue(content, "条，共", "页，");

                            totalPage = Convert.ToInt32(totalPageStr);
                        }

                        Console.WriteLine("Running OptLogistics totalPage:{0} page:{1}", totalPage, page);

                        GetOrderNOAndDesc(content, orderList, false, true);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                } while (++page <= totalPage);


                List<YFOrderWriteInfo> items = GetWaitingItems(orderList.Keys.ToList());

                UpdateTagState(items);

                WriteWaitingItemsToXLS(items);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        /// <summary>
        /// 获取待发货商品信息
        /// </summary>
        /// <param name="conent"></param>
        /// <returns></returns>
        public List<YFOrderWriteInfo> GetWaitingItems(List<string> orders)
        {
            List<YFOrderWriteInfo> items = new List<YFOrderWriteInfo>();

            string iUrl = "http://yaodian.yaofangwang.com/Manage/Sell/OrderView.aspx?OrderNo={0}";

            foreach (string order in orders)
            {
                try
                {
                    YFOrderWriteInfo wItem = new YFOrderWriteInfo();

                    string content = request.HttpGet(string.Format(iUrl, order));

                    string addressInfo = CommonFun.GetValue(content, "<i class=\"fa fa-truck\"></i>收货人信息", "<div class=\"row\">");

                    string itemInfo = CommonFun.GetValue(content, "<tbody>", "</tbody>");

                    YFOrderInfo item = new YFOrderInfo();
                    item.Created = order;
                    item.SenderName = senderName;
                    item.SenderPhoneNumber = senderPhoneNumber;
                    MatchCollection aMs = CommonFun.GetValues(addressInfo, "<div class=\"col-md-4\">", "</div>");

                    item.ReceiverName = aMs[0].Value.Trim(); ;
                    item.ReceiverPhoneNumber = aMs[1].Value.Trim().Replace("&nbsp;", "");
                    item.ReceiverAddress = CommonFun.GetValue(addressInfo, "<div class=\"col-md-10\">", "</div>");

                    MatchCollection rMs = CommonFun.GetValues(itemInfo, " <td class=\"tdcenter lh24\" style=\"width: 2%;\" rowspan=\"1\">", "</tr>");

                    int count = 0;
                    foreach (Match m in rMs)
                    {
                        string nameInfo = CommonFun.GetValue(m.Value, "<td class=\"tdcenter lh24\" rowspan=\"1\" title=\"", "/td>");
                        string name = CommonFun.GetValue(nameInfo, "target=\"_blank\">", "</a>");
                        item.ViewCount = CommonFun.GetValue(nameInfo, "</br>商品编号：", "<").Trim();

                        MatchCollection iMs = CommonFun.GetValues(m.Value, "<td class=\"tdcenter lh24\"   rowspan=\"1\">", "&");
                        MatchCollection iMs1 = CommonFun.GetValues(m.Value, "<td class=\"tdcenter lh24\" >", "</td>");
                        item.ID += name + " ";
                        item.ID += iMs[0].Value.Trim() + " ";
                        item.ID += iMs[1].Value.Trim() + " ";
                        string priceStr = CommonFun.GetValue(m.Value, "<td class=\"tdcenter lh24\"  rowspan=\"1\">¥", "<");
                        //item.ShopPrice = Convert.ToDecimal(priceStr.Trim());
                        //item.ID += iMs[2].Value.Trim() + " ";
                        item.ID += iMs1[1].Value.Trim().Replace("&nbsp;", "") + " ";

                        YFOrderInfo tItem = new YFOrderInfo();
                        tItem.ItemName = name;
                        tItem.ReceiverZipCode = iMs[0].Value.Trim();
                        tItem.BuyerId = iMs[1].Value.Trim();
                        tItem.ExpressName = iMs1[1].Value.Trim().Replace("&nbsp;", "");
                        tItem.ExpressTemplate = priceStr.Trim();
                        if(++count == 1)
                        {
                            tItem.SenderName = item.SenderName;
                            tItem.SenderPhoneNumber = item.SenderPhoneNumber;
                            tItem.ReceiverName = item.ReceiverName;
                            tItem.ReceiverPhoneNumber = item.ReceiverPhoneNumber;
                            tItem.ReceiverAddress = item.ReceiverAddress;
                        }
                        wItem.TotalItem.Add(tItem);
                    }

                    item.SellType = CommonFun.GetValue(content, " <div class=\"col-md-2 font-grey-mint\">发票信息： </div>", "</div>");

                    wItem.BaseItem = item;

                    items.Add(wItem);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }


            return items;
        }


        public void UpdateTagState(List<YFOrderWriteInfo> items)
        {
            try
            {
                string uUrl = "http://yaodian.yaofangwang.com/Manage/Handler/Handler.ashx";

                string postData = "method=OrderDescShop&CustomerShopOrderNo={0}&dec={1}&rank={2}";

                foreach (YFOrderWriteInfo wItem in items)
                {
                    BaseItemInfo item = wItem.BaseItem;

                    string orderNO = item.Created;
                    string rank = "00";
                    string dec = startOrderNO.ToString();

                    if (!IsRedTag(item.ViewCount))
                    {
                        rank = "05";
                        dec += "+2017发走";
                    }
                    else if (!item.SellType.Contains("无需发票"))
                    {
                        rank = "05";
                        dec += "开";
                    }

                    request.HttpPost(uUrl, string.Format(postData, orderNO, dec, rank));
                    item.Name = startOrderNO.ToString();
                    foreach(BaseItemInfo sItem in wItem.TotalItem)
                    {
                        YFOrderInfo yfSItem = sItem as YFOrderInfo;
                        yfSItem.ExpressNO = "'" + startOrderNO.ToString();
                    }

                    if (isAsc)
                    {
                        startOrderNO++;
                    }
                    else
                    {
                        startOrderNO--;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        /// <summary>
        /// 提取待发货信息到xls表
        /// </summary>
        /// <param name="items"></param>
        public void WriteWaitingItemsToXLS(List<YFOrderWriteInfo> items)
        {
            try
            {
                foreach (YFOrderWriteInfo wItem in items)
                {
                    BaseItemInfo item = wItem.BaseItem;

                    CommonFun.WriteCSV(filePath + "send1_" + ticks + fileExtendName, item);

                    YFOrderInfo yfItem = item as YFOrderInfo;

                    yfItem.ItemName = null;
                    yfItem.ExpressNO = null;
                    yfItem.ReceiverName = null;
                    yfItem.ReceiverPhoneNumber = null;
                    yfItem.SenderName = null;
                    yfItem.SenderPhoneNumber = null;
                    yfItem.ReceiverAddress = item.ID;
                    CommonFun.WriteCSV(filePath + "send2_" + ticks + fileExtendName, item);

                    foreach(BaseItemInfo sItem in wItem.TotalItem)
                    {
                        CommonFun.WriteCSV(filePath + "send3" + fileExtendName, sItem);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        /// <summary>
        /// 处理物流信息
        /// </summary>
        /// <returns></returns>
        public void OptLogistics()
        {
            try
            {
                Dictionary<string, string> orderList = new Dictionary<string, string>();
                int page = 1;
                int totalPage = 0;
                do
                {
                    try
                    {
                        string sUrl = string.Format("http://yaodian.yaofangwang.com/Manage/Sell/Order.aspx?status=PAID&page={0}", page);

                        string content = request.HttpGet(sUrl);

                        if (totalPage == 0)
                        {
                            string totalPageStr = CommonFun.GetValue(content, "条，共", "页，");

                            totalPage = Convert.ToInt32(totalPageStr);
                        }

                        Console.WriteLine("Running OptLogistics totalPage:{0} page:{1}", totalPage, page);

                        GetOrderNOAndDesc(content, orderList);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                } while (++page <= totalPage);
                Login(3);
                foreach (string orderNO in orderList.Keys)
                {
                    try
                    {
                        string tUrl = string.Format("https://yaodian.yaofangwang.com/Manage/Sell/OrderSend.aspx?OrderNo={0}", orderNO);

                        string content = request.HttpGet(tUrl);

                        if (!content.Contains("确认发货"))
                        {
                            OptPrescription();
                            content = request.HttpGet(tUrl);
                            Login(3);
                        }

                        string viewState = CommonFun.GetUrlEncode(CommonFun.GetValue(content, "id=\"__VIEWSTATE\" value=\"", "\""), false);

                        string generator = CommonFun.GetValue(content, "id=\"__VIEWSTATEGENERATOR\" value=\"", "\"");

                        List<string> dateStr = new List<string>();

                        List<string> orderNOStr = new List<string>();

                        content = CommonFun.GetValue(content, "<div class=\"bg-white \">", "确认发货");

                        MatchCollection ms = CommonFun.GetValues(content, "<div class=\"form-group input-inline  mb10\">", "</div>");

                        foreach (Match m in ms)
                        {
                            orderNOStr.Add(CommonFun.GetValue(m.Value, "parsl='", "'"));
                            dateStr.Add(GetCreateDate() + "," + CommonFun.GetValue(m.Value, " value='", "'"));

                            //C809201313164538-1#C809201313164538-2@2018-03-20,1#2018-06-20,5
                        }

                        string datePostStr = string.Join("#", orderNOStr.ToArray()) + "@" + string.Join("#", dateStr.ToArray());

                        string postData = string.Format("__EVENTTARGET=ctl00%24cph_content%24btn_Send&__EVENTARGUMENT=&__VIEWSTATE={0}&__VIEWSTATEGENERATOR={1}&ctl00%24cph_content%24hf_productnoandcustomerordermedicineno={2}&ctl00%24cph_content%24ddl_Logistics=2&ctl00%24cph_content%24txt_OrderWebNumber={3}&ctl00%24cph_content%24txt_InvoiceNo=&ctl00%24cph_content%24txt_code=&sendAddress=rb_Address&ctl00%24cph_content%24hf_AddressId={4}&returnAddress=rb_Address&ctl00%24cph_content%24hf_returnAddressId={5}&ctl00%24cph_content%24stype=0", viewState, generator, CommonFun.GetUrlEncode(datePostStr, false), orderList[orderNO], 1048, 2374);

                        string subUrl = string.Format("https://yaodian.yaofangwang.com/Manage/Sell/OrderSend.aspx?OrderNo={0}", orderNO);

                        string result = request.HttpPost(subUrl, postData);

                        Console.WriteLine("{0} OptLogistics orderNO:{1}, desc:{2}", DateTime.Now, orderNO, orderList[orderNO]);

                        Thread.Sleep(waitTime);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

        }

        public bool OptUpdatePrice(BaseItemInfo item)
        {
            int doCount = 0;
            bool result = false;
            do
            {
                if (doCount == 1)
                {
                    item.PlatformPrice = CommonFun.TrunCate(item.ShopPrice * (decimal)0.99);
                }

                result = UpdateItemInfo(item);
            } while (++doCount < 2 && !result);

            return result;
        }

        public int GetClickingRate(BaseItemInfo item)
        {
            try
            {
                string content = request.HttpGet("http://www.yaofangwang.com/medicine-" + item.ItemName + ".html?sort=price&sorttype=asc", null, true);

                string clickingRateStr = CommonFun.GetValue(content, "<dt>最近浏览</dt><dd class=\"w1\">", "次");

                return string.IsNullOrEmpty(clickingRateStr) ? 0 : Convert.ToInt32(clickingRateStr);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return 0;
        }

        /// <summary>
        /// 执行价格修改操作
        /// </summary>
        /// <param name="minPrice"></param>
        /// <param name="item"></param>
        private void OptUpdatePrice(decimal minPrice, BaseItemInfo item, decimal diffPrice, bool opt, bool isLimitDown, string iFileName, int downRate)
        {
            if (minPrice != decimal.MaxValue)
            {
                minPrice = minPrice - diffPrice;

                if (minPrice > 0 && minPrice != item.ShopPrice)
                {
                    if (!isLimitDown || item.ShopPrice * (100 - downRate) / 100M < minPrice)
                    {
                        item.PlatformPrice = CommonFun.TrunCate(minPrice);
                        if (opt)
                        {
                            OptUpdatePrice(item);
                        }
                        Thread.Sleep(random.Next(5) * 1000);
                        CommonFun.WriteCSV(iFileName + ticks + fileExtendName, item);
                    }
                    else
                    {
                        item.PlatformPrice = CommonFun.TrunCate(minPrice);
                        CommonFun.WriteCSV(fileName + "ToolowerPrice" + ticks + ".csv", item);
                    }
                }
            }
        }

        /// <summary>
        /// 自动上架
        /// </summary>
        public void AutoUpDown()
        {
            try
            {
                bool isDown = false;
                bool isUp = false;
                while (true)
                {
                    try
                    {
                        if (CommonFun.IsInTimeRange(downTime))
                        {
                            if (!isDown)
                            {
                                Dictionary<string, BaseItemInfo> items = GetSellingItems();

                                foreach (BaseItemInfo item in items.Values)
                                {
                                    if (IsInAutoUpDownTypeList(item.Type))
                                    {
                                        DownItem(item);
                                    }
                                }
                                isDown = true;
                                isUp = false;
                            }
                        }
                        else
                        {
                            if (!isUp)
                            {
                                Dictionary<string, BaseItemInfo> items = GetDownItems();
                                foreach (BaseItemInfo item in items.Values)
                                {
                                    if (IsInAutoUpDownTypeList(item.Type))
                                    {
                                        UpItem(item);
                                    }
                                }

                                isUp = true;
                                isDown = false;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                    Thread.Sleep(60 * 1000);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        /// <summary>
        /// 下架物品
        /// </summary>
        /// <param name="item"></param>
        public void DownItem(BaseItemInfo item)
        {
            try
            {
                item.PlatformPrice = item.ShopPrice;
                if (UpdateItemInfo(item, "-999"))
                {
                    CommonFun.WriteCSV("YF/downSuccess" + ticks + fileExtendName, item);
                }
                else
                {
                    CommonFun.WriteCSV("YF/downFailed" + ticks + fileExtendName, item);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        /// <summary>
        /// 上架物品
        /// </summary>
        /// <param name="item"></param>
        public void UpItem(BaseItemInfo item)
        {
            try
            {
                item.PlatformPrice = item.ShopPrice;
                if (UpdateItemInfo(item, "1"))
                {
                    CommonFun.WriteCSV("YF/upSuccess" + ticks + fileExtendName, item);
                }
                else
                {
                    CommonFun.WriteCSV("YF/upFailed" + ticks + fileExtendName, item);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public void UpdatePrice()
        {
            try
            {
                Login(2);

                bool opt = true;

                Thread autoUpDownThread = new Thread(AutoUpDown);
                autoUpDownThread.Start();

                DateTime startTime = DateTime.MinValue;

                while (true)
                {
                    try
                    {
                        if ((DateTime.Now - startTime).Hours > 2)
                        {
                            startTime = DateTime.Now;

                            ticks = startTime.Ticks;

                            Dictionary<string, BaseItemInfo> sItems = GetSellingItems();

                            int count = 0;
                            foreach (BaseItemInfo item in sItems.Values)
                            {
                                //在黑名单中自动下架
                                if (IsBlacklistStore(item.Created)|| IsBlackName(item.Name))
                                {
                                    DownItem(item);
                                    Console.WriteLine("{0} In black list down name:{1} create:{2}", DateTime.Now, item.Name, item.Created);
                                    CommonFun.WriteCSV(fileName + "BlackDown" + ticks + fileExtendName, item);
                                    continue;
                                }
                                Console.WriteLine("{2} Updating totaoCount:{0} curCount:{1}", sItems.Count, ++count, DateTime.Now);

                                //if (item.ID != "国药准字Z20080047")
                                //{
                                //    continue;
                                //}

                                if (IsInSpcTypeList(item.Type))
                                {
                                    decimal minPrice = decimal.MaxValue;

                                    minPrice = GetMinPrice(item, 0);

                                    OptUpdatePrice(minPrice, item, lPrice, opt, true, "YF/updatePriceSpc", spcMinDownRate);
                                }
                                else if (IsInTypeList(item.Type))
                                {
                                    int iClickingRate = GetClickingRate(item);

                                    decimal minPrice = decimal.MaxValue;

                                    int stock = minStock;

                                    string iFileName = "YF/updatePriceUpFive";

                                    decimal diffPrice = lPrice;

                                    bool isLimitDown = true;

                                    if (iClickingRate >= clickingRate)
                                    {
                                        if (Convert.ToInt16(item.Inventory) <= Convert.ToInt16(minStockList[0]))
                                        {
                                            stock = 0;
                                            iFileName = "YF/updatePriceLowFive";
                                            diffPrice = Convert.ToDecimal(minStockList[1]);
                                            isLimitDown = false;
                                        }
                                    }
                                    else
                                    {
                                        stock = 0;
                                    }

                                    minPrice = GetMinPrice(item, stock);

                                    OptUpdatePrice(minPrice, item, diffPrice, opt, isLimitDown, iFileName, minDownRate);
                                }
                            }

                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public List<BaseItemInfo> GetItems(string content)
        {
            List<BaseItemInfo> items = new List<BaseItemInfo>();

            try
            {
                MatchCollection ms = CommonFun.GetValues(content, "<tr class=\"bg-grey-cararra\">", "<td class=\"tdcenter\">");
                foreach (Match m in ms)
                {
                    BaseItemInfo item = new BaseItemInfo();


                    item.Type = CommonFun.GetValue(m.Value, "商品编号：", "</span>");

                    MatchCollection iMs = CommonFun.GetValues(m.Value, "<div class=\"text-left\">", "</div>");

                    item.Name = CommonFun.GetValue(iMs[0].Value, "title='", "'>");
                    item.ID = CommonFun.GetValue(iMs[1].Value, "title='", "'>");
                    item.Format = CommonFun.GetValue(iMs[2].Value, "title='", "'>");
                    item.Created = iMs[4].Value.Trim();
                    string priceStr = CommonFun.GetValue(m.Value, "<div par='price'>", "</div>");
                    priceStr = CommonFun.GetValue(priceStr, "¥", "<");
                    item.ShopPrice = string.IsNullOrEmpty(priceStr) ? 0 : Convert.ToDecimal(priceStr);
                    item.Inventory = CommonFun.GetValue(m.Value, "<div>库存", "件");


                    item.ItemName = CommonFun.GetValue(m.Value, "medicineId='", "'");
                    string midStr = CommonFun.GetValue(iMs[0].Value, "<a href=\"", "\"");
                    item.ViewCount = CommonFun.GetValue(midStr, "-", ".html");

                    items.Add(item);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            return items;
        }

        /// <summary>
        /// 更新物品信息
        /// </summary>
        /// <param name="item"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        public bool UpdateItemInfo(BaseItemInfo item, string status = null)
        {
            try
            {
                string iUrl = string.Format("https://yaodian.yaofangwang.com/Manage/Handler/Handler.ashx?method=GeFastEditMedicineInfo&smid={0}&s=1534320806748", item.ViewCount);

                string content = request.HttpGet(iUrl, null, true);

                if (!string.IsNullOrEmpty(content))
                {
                    JObject dJ = JsonConvert.DeserializeObject<JObject>(content);

                    JArray aj = JsonConvert.DeserializeObject<JArray>(dJ["JSON"].ToString());

                    JObject obj = (JObject)aj[0];

                    status = string.IsNullOrEmpty(status) ? obj["Status"].ToString() : status;

                    string url = string.Format("https://yaodian.yaofangwang.com/Manage/Handler/Handler.ashx?method=SaveFastEditMedicineInfo&smid={0}&price={1}&xnReserve={2}&discount={3}&sprice=&sdiscount={4}&weight={5}&isdiscount={6}&Status={7}&iserp={8}&scheduleddays={9}",
                        item.ViewCount, item.PlatformPrice, item.Inventory, 10, 10, obj["mWeight"].ToString() == "0" ? obj["Weight"] : obj["mWeight"], 0, status, obj["IsErp"].ToString() == "0" ? false : true, obj["ScheduledDays"]);

                    content = request.HttpGet(url, null, true);

                    if (content == "1")
                    {
                        return true;
                    }
                    else
                    {
                        item.Remark += content;
                        Console.WriteLine("UpdateItemInfo result:{0}", content);
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
        /// 获取主动下架列表
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, BaseItemInfo> GetDownItems()
        {
            return GetItemsByStatus(-999);
        }

        /// <summary>
        /// 获取在售列表
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, BaseItemInfo> GetSellingItems(bool isTest = false)
        {
            if (isTest)
            {
                Dictionary<string, BaseItemInfo> items = new Dictionary<string, BaseItemInfo>();
                BaseItemInfo item = new BaseItemInfo();
                item.ID = "国药准字H20061246";
                item.ViewCount = "2461435";
                item.Format = "0.25gx10片/瓶";
                item.Name = "头孢丙烯片";
                item.Created = "扬子江药业集团有限公司";
                item.Type = "2017";
                item.Inventory = "127";
                item.ItemName = "196329";
                items.Add("", item);
                return items;
            }
           
       
            return GetItemsByStatus(1);
        }

        /// <summary>
        /// 获取物品列表
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, BaseItemInfo> GetItemsByStatus(int status)
        {
            Dictionary<string, BaseItemInfo> items = new Dictionary<string, BaseItemInfo>();

            int page = 1;
            int totalPage = 0;
            do
            {
                try
                {
                    string url = string.Format("https://yaodian.yaofangwang.com/Manage/Products/Product.aspx?Status={1}&page={0}", page, status);

                    string content = request.HttpGet(url, null, true);

                    if (totalPage == 0)
                    {
                        string pageStr = CommonFun.GetValue(content, "<div class=\"dataTables_info\">共", "</div>");

                        pageStr = CommonFun.GetValue(pageStr, "共", "页");

                        totalPage = Convert.ToInt16(pageStr);
                    }
                    Console.WriteLine("{0} totalPage:{1} curPage:{2}", DateTime.Now, totalPage, page);
                    List<BaseItemInfo> itemList = GetItems(content);

                    foreach (BaseItemInfo item in itemList)
                    {
                        //string key = item.Name + item.Format + item.Created;
                        string key = item.ViewCount;
                        if (!items.ContainsKey(key))
                        {
                            items.Add(key, item);
                        }
                        else
                        {
                            Console.WriteLine("Error: is same key {0}", key);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            } while (++page <= totalPage);

            return items;
        }
    }
}
