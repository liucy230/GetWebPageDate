﻿using Newtonsoft.Json;
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
using APP_YFT.DataService;
using YiBang.Framework.APP;
using YiBang.Framework.APP.Caches;
using YiBang.Framework.APP.Start;
using APP_YFT.Base;

namespace GetWebPageDate.Util
{
    /// <summary>
    /// 颜色标记
    /// </summary>
    public enum TagColor
    {
        Empty = 0,
        /// <summary>
        /// "1":"红色"
        /// </summary>
        Red,
        /// <summary>
        /// "2":"橙色"
        /// </summary>
        Orange,
        /// <summary>
        /// "3":"黄色"
        /// </summary>
        Yellow,
        /// <summary>
        /// "4":"绿色"
        /// </summary>
        Green,
        /// <summary>
        /// "5":"蓝色"
        /// </summary>
        Blue,
        /// <summary>
        /// "6":"紫色"
        /// </summary>
        Purple,
    }

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

        private HttpRequest userRequest = new HttpRequest();
        /// <summary>
        /// 订单号是否为升序
        /// </summary>
        private bool isAsc;

        Dictionary<string, string> heads = new Dictionary<string, string>();

        private string getedTag = "[GETED]";

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

            heads.Add("User-Agent", "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/67.0.3396.87 Safari/537.36");
            heads.Add("X-Requested-With", "XMLHttpRequest");
        }

        public override void ReadAllMenuURL()
        {
            string content = userRequest.HttpGet("https://www.yaofangwang.com/Catalog-1.html", null, true);

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

                    string content = userRequest.HttpGet("https://www.yaofangwang.com/" + url, null, true);

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
                                content = userRequest.HttpGet("https://www.yaofangwang.com/" + url.Substring(0, url.IndexOf(".")) + "-p" + i + ".html", null, true);

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

            string itemContent = CommonFun.GetValue(content, "<ul class=\"goodlist_search clearfix\">", "</ul>");

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
            string content = userRequest.HttpGet(urlSeach, null, true);
            //获取商品信息url
            string itemInfo = "https://open.yaofangwang.com/app_gateway.ashx?market=huawei&app_key=4fb44b67d0be2af36f7135586d38d658&account_id=1441303&os=android&app_version=2.9.6&service=get_goods_detail&latitude=0.0&id=571177&timestamp=2018-04-17+17%3A16%3A28&longitude=0.0&sign=29f140ad5a407d7ca29a729caf016a7d";

            //获取商家列表url
            string shopList = "https://open.yaofangwang.com/app_gateway.ashx?os=android&app_version=2.9.6&latitude=0.0&orderby=priceasc&market=huawei&account_id=1441303&app_key=4fb44b67d0be2af36f7135586d38d658&service=get_goods_shop&page_index=1&region_name=%E5%85%A8%E5%9B%BD&id=571177&timestamp=2018-04-17+17%3A20%3A53&longitude=0.0&sign=b62cddd38efa3d1cfbe3d9898f89ec67";
        }

        private BaseItemInfo GetItemInfo(string content, int inventoryMin, bool isChangeMin = false)
        {
            try
            {
                string sellerCount = CommonFun.GetValue(content, "class=\"cur\">", "个零售商家报价");

                if (sellerCount == "1")
                {
                    inventoryMin = isChangeMin ? 50 : inventoryMin;
                    Console.WriteLine("sellerCount :{0}", sellerCount);
                }

                string itemlist = CommonFun.GetValue(content, "<ul class=\"slist\">", "</ul>");

                MatchCollection ms = CommonFun.GetValues(itemlist, "<li class=\"clearfix\">", "</li>");

                foreach (Match m in ms)
                {
                    string inventoryStr = CommonFun.GetValue(m.Value, "<label class=\"sreserve\">", "</label>");
                    string priceStr = CommonFun.GetValue(m.Value, "¥", "<");
                    priceStr = priceStr.Trim();
                    string storeName = CommonFun.GetValue(m.Value, "<div class=\"shop\">", "</div>");
                    storeName = CommonFun.GetValue(storeName, "title=\"", "\"");
                    if (!string.IsNullOrEmpty(inventoryStr) && !string.IsNullOrEmpty(priceStr) && !IsBlacklistStore(storeName) && !selfName.Contains(storeName))
                    {
                        if (Convert.ToInt32(inventoryStr) > inventoryMin)
                        {
                            BaseItemInfo info = new BaseItemInfo();

                            info.Sela = 10;

                            //if (!string.IsNullOrEmpty(selaStr))
                            //{
                            //    if (selaStr.Contains("立享"))
                            //    {
                            //        selaStr = selaStr.Replace("立享", "");
                            //        info.Sela = Convert.ToDecimal(selaStr);

                            //    }
                            //    else if (selaStr.Contains("最高返"))
                            //    {
                            //        selaStr = CommonFun.GetValue(selaStr, "最高返", "元");
                            //        info.ReturnPrice = Convert.ToDecimal(selaStr);
                            //    }
                            //    else
                            //    {
                            //        info.Remark = selaStr;
                            //    }
                            //}
                            //else
                            //{
                            string selaStr = CommonFun.GetValue(m.Value, "返现", "元");

                            if (!string.IsNullOrEmpty(selaStr))
                            {
                                info.ReturnPrice = Convert.ToDecimal(selaStr);
                            }
                            //}

                            info.ShopPrice = string.IsNullOrEmpty(priceStr) ? 0 : Convert.ToDecimal(priceStr);
                            info.ShopSelaPrice = CommonFun.TrunCate(info.ShopPrice * (info.Sela / 10) - info.ReturnPrice);
                            info.Inventory = inventoryStr;
                            // SetMenuInfo(info, content);

                            SetPriceInfo(info, content);

                            return info;
                        }
                    }
                    else if (content.Contains("安装APP查看价格"))
                    {
                        Login(2);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            return null;
        }

        public decimal GetMinPrice(BaseItemInfo item, int inventoryMin)
        {
            try
            {
                string content;

                string prevUrl = null;

                content = userRequest.HttpGet("https://www.yaofangwang.com/medicine-" + item.ItemName + ".html?sort=price&sorttype=asc", null, true);

                string selaStr = CommonFun.GetValue(content, "class=\"all default_cursor fb_red mr10\">", "</a>");

                do
                {
                    if (!string.IsNullOrEmpty(prevUrl))
                    {
                        prevUrl = "https://www.yaofangwang.com" + prevUrl;
                        if (!prevUrl.Contains("?sort=price&sorttype=asc"))
                        {
                            prevUrl += "?sort=price&sorttype=asc";
                        }
                        content = userRequest.HttpGet(prevUrl, null, true);
                    }

                    BaseItemInfo rItem = GetItemInfo(content, inventoryMin);

                    if (rItem != null)
                    {
                        return rItem.ShopSelaPrice;
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

                content = userRequest.HttpGet("https:" + CommonFun.GetValue(item, "<a href=\"", "\"") + "?sort=price&sorttype=asc", null, true);

                do
                {
                    if (!string.IsNullOrEmpty(prevUrl))
                    {
                        prevUrl = "https://www.yaofangwang.com" + prevUrl;
                        if (!prevUrl.Contains("?sort=price&sorttype=asc"))
                        {
                            prevUrl += "?sort=price&sorttype=asc";
                        }
                        content = userRequest.HttpGet(prevUrl, null, true);
                    }

                    BaseItemInfo rItem = GetItemInfo(content, inventoryMin, true);

                    if (rItem != null)
                    {
                        rItem.Format = CommonFun.GetValue(item, "规格：", "<");

                        rItem.Created = CommonFun.GetValue(item, "生产厂家：", "<");

                        rItem.ID = CommonFun.GetValue(item, "批准文号：", "<");
                        return rItem;
                    }

                    // nextStr = CommonFun.GetValue(content, "<a class=\"prev\" disabled=\"disabled\" style=\"margin-right:5px;\">", "</a>");

                    prevUrl = CommonFun.GetValue(content, "上一页", "</div>");
                    prevUrl = CommonFun.GetValue(prevUrl, "<a title='下一页' href='", "'");
                    //prevUrl = string.IsNullOrEmpty("prevUrl") ? prevUrl : CommonFun.GetValue(prevUrl, "'", "'");
                } while (!string.IsNullOrEmpty(prevUrl));

                BaseItemInfo info = new BaseItemInfo();
                info.ShopSelaPrice = Decimal.MaxValue;
                return info;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            return null;
        }

        public string SeachMedicineIdByID(string id, string format)
        {
            try
            {
                string url = string.Format("https://www.yaofangwang.com/search.html?keyword={0}", id);

                string content = userRequest.HttpGet(url, null, true);

                List<string> items = GetItemStr(content);

                foreach (string item in items)
                {
                    if (format == CommonFun.GetValue(item, "规格：", "<"))
                    {
                        return CommonFun.GetValue(item, "href=\"//www.yaofangwang.com/medicine-", "\\.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            return "";
        }

        public List<BaseItemInfo> SeachInfoByID(string id, int inventoryMin = 5)
        {
            List<BaseItemInfo> infos = new List<BaseItemInfo>();

            string url = string.Format("https://www.yaofangwang.com/search.html?keyword={0}", id);

            string content = userRequest.HttpGet(url, null, true);

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

                //HttpRequest tempRequest = new HttpRequest();

                string url = string.Format("https://www.yaofangwang.com/search.html?keyword={0}", System.Web.HttpUtility.UrlEncode(name));

                string content = userRequest.HttpGet(url, null, true);

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

                            content = userRequest.HttpGet("http:" + CommonFun.GetValue(item, "<a target=\"_blank\" href=\"", "\""), null, true);

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
            //string login_url = "https://reg.yaofangwang.com/login.aspx";
            //string postDataStr = string.Format("__EVENTTARGET=ctl00%24ContentPlaceHolder1%24t_login&__EVENTARGUMENT=&__VIEWSTATE=%2FwEPDwUINDE0MjQ5NzVkGAEFHl9fQ29udHJvbHNSZXF1aXJlUG9zdEJhY2tLZXlfXxYBBSVjdGwwMCRDb250ZW50UGxhY2VIb2xkZXIxJGNiX1JlbWVtYmVyfMPsdmjZlfvGbCxOD8u0r%2FRLbA8%3D&__VIEWSTATEGENERATOR=C2EE9ABB&__EVENTVALIDATION=%2FwEdAAosVaXMhV4q7s19NxAQU1EA%2FX3Fo%2FRaqYiLtErA%2B0XLEhccBLe9MIf%2BOeu1SwHT%2Fo2ng0PqTUAPaPYHk4tr%2FTPfKqmCwJguV16MgvgQAIOIM5gCICnqqodEefzHnITuNvGKN2iu4q6IDCzyu2cVK%2B2X9v9Eq8t4s5ZZ5SNoKNrRyao0KorV0rlA31R%2FnAfLx9YONrIXtTlQ%2FOFtaven3EmBxJNqndFqhuABr6rHIEGR90USvzc%3D&ctl00%24ContentPlaceHolder1%24txt_AccountName={0}&ctl00%24ContentPlaceHolder1%24txt_Password={1}&ctl00%24ContentPlaceHolder1%24txt_ValidateCode=&ctl00%24ContentPlaceHolder1%24txt_Mobile=&ctl00%24ContentPlaceHolder1%24txt_ValidateCode1=&ctl00%24ContentPlaceHolder1%24txtMobileCode=&ctl00%24ContentPlaceHolder1%24hf_type=default", username, password);

            //request.Login(login_url, postDataStr);
            Login(2);
            //SeachInfoByID("国药准字Z20090367");
        }

        public void Login(int type)
        {
            for (int i = 0; i <= 1; i++)
            {
                bool isUser = i == 1;

                string login_url = "https://reg.yaofangwang.com/login.html";

                HttpRequest lRequest = request;

                if (isUser)
                {
                    type = 1;
                    lRequest = userRequest;
                }

                string token = lRequest.GetLogin(login_url, null);
                token = CommonFun.GetValue(token, "<input name=\"__RequestVerificationToken\" type=\"hidden\" value=\"", "\"");
                string postDataStr;
                switch (type)
                {
                    case 1:
                        postDataStr = string.Format("UserName={0}&Password={1}&isRemembered=true&ImageCode=&LoginType=0&__RequestVerificationToken={2}", CommonFun.GetUrlEncode(username), password, token);
                        break;
                    case 2:
                        postDataStr = string.Format("UserName={0}&Password={1}&isRemembered=true&ImageCode=&LoginType=0&__RequestVerificationToken={2}", CommonFun.GetUrlEncode(username1), password1, token);
                        break;
                    case 3:
                        postDataStr = string.Format("UserName={0}&Password={1}&isRemembered=true&ImageCode=&LoginType=0&__RequestVerificationToken={2}", CommonFun.GetUrlEncode(username2), password2, token);
                        break;
                    default:
                        postDataStr = string.Format("UserName={0}&Password={1}&isRemembered=true&ImageCode=&LoginType=0&__RequestVerificationToken={2}", CommonFun.GetUrlEncode(username), password, token);
                        break;
                }

                string content = lRequest.HttpPost(login_url, postDataStr, heads);

                Console.WriteLine("Login result {0}", content);
            }
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
            item.ID = "国药准字Z10940063";
            item.ViewCount = "235";
            item.Format = "10mlx24支/盒";
            item.Name = "复方双花口服液";
            item.Created = "北京康益药业有限公司";
            item.Type = "T-2020";
            item.Inventory = "31";
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
        /// 处理订单(审核处方)
        /// </summary>
        public void OptPrescription()
        {
            try
            {
                int page = 1;
                int totalPage = 0;
                Dictionary<string, string> orderDic = new Dictionary<string, string>();
                Login(2);
                do
                {
                    try
                    {
                        string sUrl = string.Format("https://yaodian.yaofangwang.com/order/List?status=AUDIT&page={0}", page);

                        string content = request.HttpGet(sUrl);

                        if (totalPage == 0)
                        {
                            string totalPageStr = CommonFun.GetValue(content, "条，共", "页，");

                            totalPage = Convert.ToInt32(totalPageStr);
                        }

                        Console.WriteLine("Running OptPrescription totalPage:{0} page:{1}", totalPage, page);

                        GetOrderNOAndDesc(content, orderDic, 1);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }

                } while (++page <= totalPage);

                Login(3);

                foreach (string orderNO in orderDic.Keys)
                {
                    string subUrl = string.Format("https://yaodian.yaofangwang.com/order/RX_Audit/{0}", orderNO);

                    request.HttpGet(subUrl);

                    string postData = string.Format("orderno={0}&content=%E5%A4%84%E6%96%B9%E7%85%A7%E7%89%87%E7%AC%A6%E5%90%88%E8%A6%81%E6%B1%82%E8%A7%84%E8%8C%83&valid=valid&X-Requested-With=XMLHttpRequest", orderNO);

                    string result = request.HttpPost(subUrl, postData);

                    string code = CommonFun.GetValue(result, "code\":", ",");
                    if (code != "1")
                    {
                        Console.WriteLine(result + "," + orderNO + "...........");
                    }

                    Console.WriteLine("{0} OptPrescription orderNO:{1}", DateTime.Now, orderNO);

                    Thread.Sleep(waitTime);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private string GetCreateDate()
        {
            DateTime date = DateTime.Now.AddMonths(6);

            return date.ToString("yyyy-MM-dd");
        }

        ///// <summary>
        ///// 获取订单和描述
        ///// </summary>
        ///// <param name="content"></param>
        ///// <param name="orderDic"></param>
        ///// <param name="isGetDesc">是否或备注</param>
        ///// <param name="isRank">是否被标记</param>
        ///// 

        /// <summary>
        /// 获取订单和描述
        /// </summary>
        /// <param name="content"></param>
        /// <param name="orderDic"></param>
        /// <param name="useType">用途：1、处理处方 2、标记填写备注 3、处理发货 4、已成交绿色标记订单</param>
        private void GetOrderNOAndDesc(string content, Dictionary<string, string> orderDic, int useType)
        {
            try
            {
                MatchCollection ms = CommonFun.GetValues(content, "<td class=\"bl0 qizi\">", "</td>");

                foreach (Match m in ms)
                {
                    string stateStr = CommonFun.GetValue(m.Value, "rank=\"", "\"");
                    if (!string.IsNullOrEmpty(stateStr))
                    {
                        string orderNO = CommonFun.GetValue(m.Value, "<a href=\"/order/EditDesc/", "rank=");
                        orderNO = orderNO.Substring(0, orderNO.Length - 1);
                        string desc = CommonFun.GetValue(m.Value, "title=\"", "\"");

                        if (useType == 1)
                        {
                            if (stateStr == Convert.ToString((int)TagColor.Green))
                            {
                                if (!orderDic.ContainsKey(orderNO))
                                {
                                    orderDic.Add(orderNO, desc);
                                }
                            }
                        }
                        else if (useType == 2)
                        {
                            if (stateStr == Convert.ToString((int)TagColor.Red))
                            {
                                if (!orderDic.ContainsKey(orderNO))
                                {
                                    orderDic.Add(orderNO, desc);
                                }
                            }
                        }
                        else if (useType == 3)
                        {
                            if (stateStr == Convert.ToString((int)TagColor.Green))
                            {
                                if (!string.IsNullOrEmpty(desc) && desc.Contains("(") && desc.Contains(")"))
                                {
                                    int startIndex = desc.IndexOf("(") + 1;
                                    int len = desc.IndexOf(")") - startIndex;
                                    desc = desc.Substring(startIndex, len);// CommonFun.GetValue(desc, "\'(\'", "\')\'");
                                    if (desc.Length == 12)
                                    {
                                        if (!orderDic.ContainsKey(orderNO))
                                        {
                                            orderDic.Add(orderNO, desc);
                                        }
                                    }
                                    else
                                    {
                                        Console.WriteLine("{0} Desc Error orderNO:{1} desc:{2}", DateTime.Now, orderNO, desc);
                                    }
                                }
                            }
                        }
                        else if (useType == 4)
                        {
                            if (stateStr == Convert.ToString((int)TagColor.Green))
                            {
                                //string desc = CommonFun.GetValue(m.Value, "title=\"", "\"");
                                //不含已取标识
                                if (string.IsNullOrEmpty(desc) || !desc.Contains(getedTag))
                                {
                                    if (!orderDic.ContainsKey(orderNO))
                                    {
                                        orderDic.Add(orderNO, desc);
                                    }
                                }
                            }
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
                        string sUrl = string.Format("https://yaodian.yaofangwang.com/order/List?status=PAID&page={0}", page);

                        string content = request.HttpGet(sUrl);

                        if (totalPage == 0)
                        {
                            string totalPageStr = CommonFun.GetValue(content, "条，共", "页，");

                            totalPage = Convert.ToInt32(totalPageStr);
                        }

                        Console.WriteLine("Running OptLogistics totalPage:{0} page:{1}", totalPage, page);

                        GetOrderNOAndDesc(content, orderList, 2);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                } while (++page <= totalPage);


                List<YFOrderWriteInfo> items = GetWaitingItems(orderList);

                UpdateTagState(items);

                WriteWaitingItemsToXLS(items);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        /// <summary>
        /// 获取指定状态的信息{"1":"红色","2":"橙色","3":"黄色","4":"绿色","5":"蓝色","6":"紫色"}
        /// </summary>
        /// <param name="state"></param>
        public void GetStateInfoToXLS(int state = 4)
        {
            try
            {
                bool opt = true;
                int page = 1;
                int totalPage = 0;
                bool isGetAll = ConfigurationManager.AppSettings["yfGetAllGreenInfo"] == null ? false : Convert.ToBoolean(ConfigurationManager.AppSettings["yfGetAllGreenInfo"]);
                Login(2);
                do
                {
                    try
                    {
                        Dictionary<string, string> orderList = new Dictionary<string, string>();

                        string sUrl = string.Format("https://yaodian.yaofangwang.com/order/List?status=CHENGJIAO&page={0}", page);

                        sUrl = isGetAll ? sUrl : sUrl + string.Format("&start_date={0}&end_date={1}", DateTime.Now.ToString("yyyy-mm-dd"), DateTime.Now.ToString("yyyy-mm-dd"));

                        string content = request.HttpGet(sUrl);

                        if (totalPage == 0)
                        {
                            string totalPageStr = CommonFun.GetValue(content, "条，共", "页，");

                            totalPage = Convert.ToInt32(totalPageStr);
                        }

                        GetOrderNOAndDesc(content, orderList, 4);

                        List<YFGreenOrderInfo> items = GetGreenOrderItems(orderList);

                        foreach (YFGreenOrderInfo item in items)
                        {
                            CommonFun.WriteCSV(fileName + "Green" + fileExtendName, item);

                            if (opt)
                            {
                                UpdateRankAndRemark(item.OrderNO, "4", item.Remark + getedTag);
                            }
                        }

                        Console.WriteLine("Running GetGreen info totalPage:{0} page:{1}", totalPage, page);
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

        public List<YFGreenOrderInfo> GetGreenOrderItems(Dictionary<string, string> orders)
        {
            List<YFGreenOrderInfo> items = new List<YFGreenOrderInfo>();

            string iUrl = "https://yaodian.yaofangwang.com/order/Detail/{0}";

            foreach (KeyValuePair<string, string> order in orders)
            {
                try
                {
                    string content = request.HttpGet(string.Format(iUrl, order.Key));

                    string addressInfo = CommonFun.GetValue(content, "<i class=\"fa fa-truck\"></i>收货人信息", "<div class=\"row\">");

                    string itemInfo = CommonFun.GetValue(content, "<tbody>", "</tbody>");


                    //item.SenderName = senderName;
                    //item.SenderPhoneNumber = senderPhoneNumber;
                    MatchCollection aMs = CommonFun.GetValues(addressInfo, "<div class=\"col-md-4\">", "</div>");

                    string createTime = CommonFun.GetValue(content, "订购时间： </div>", "订单总额");

                    createTime = CommonFun.GetValue(createTime, "<div class=\"col-md-4\">", "</div>");

                    MatchCollection rMs = CommonFun.GetValues(itemInfo, "<td rowspan=\"2\" class=\"tdcenter lh24\"", "</tr>");
                    int count = 0;
                    foreach (Match m in rMs)
                    {
                        YFGreenOrderInfo item = new YFGreenOrderInfo();
                        item.OrderNO = count++ > 0 ? "" : order.Key;
                        item.ReceiverName = aMs[0].Value.Trim(); ;
                        item.ReceiverPhoneNumber = aMs[1].Value.Trim().Replace("&nbsp;", "");
                        item.ReceiverAddress = CommonFun.GetValue(addressInfo, "<div class=\"col-md-10\">", "</div>");
                        item.CreateOrderTime = createTime;
                        string nameInfo = CommonFun.GetValue(m.Value, "<td class=\"tdcenter lh24\" rowspan=\"1\" title=\"", "/td>");
                        string name = CommonFun.GetValue(m.Value, "target=\"_blank\">", "</a>");

                        MatchCollection iMs = CommonFun.GetValues(m.Value, "<td rowspan=\"2\" class=\"tdcenter lh24\">", "</td>");
                        item.ViewCount = iMs[0].Value;
                        //MatchCollection iMs1 = CommonFun.GetValues(m.Value, "<td class=\"tdcenter lh24\" >", "</td>");
                        item.Name = name.Trim().Replace(" ", "").Replace("\r\n", "") + " ";
                        item.ID = iMs[1].Value.Trim() + " ";
                        item.Format = iMs[2].Value.Trim() + " ";

                        string priceStr = iMs[4].Value.Trim().Replace(" ", "");
                        priceStr = priceStr.Replace("¥", "");
                        item.ShopPrice = Convert.ToDecimal(priceStr);
                        //item.Inventory = 
                        //item.ShopPrice = Convert.ToDecimal(priceStr.Trim());
                        //item.ID += iMs[2].Value.Trim() + " ";
                        string toPriceStr = iMs[6].Value.Trim().Replace(" ", "");
                        toPriceStr = CommonFun.GetValue(toPriceStr, "¥", "<");
                        toPriceStr = toPriceStr.Trim();
                        item.TotalPrice = toPriceStr;
                        int itemCount = Convert.ToInt32(Convert.ToDecimal(toPriceStr) / Convert.ToDecimal(priceStr));
                        item.Inventory = itemCount.ToString();
                        item.Remark = order.Value;
                        items.Add(item);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }

            return items;
        }

        /// <summary>
        /// 获取待发货商品信息
        /// </summary>
        /// <param name="conent"></param>
        /// <returns></returns>
        public List<YFOrderWriteInfo> GetWaitingItems(Dictionary<string, string> orders)
        {
            List<YFOrderWriteInfo> items = new List<YFOrderWriteInfo>();

            string iUrl = "https://yaodian.yaofangwang.com/order/Detail/{0}";

            foreach (KeyValuePair<string, string> info in orders)
            {
                try
                {
                    string order = info.Key;

                    YFOrderWriteInfo wItem = new YFOrderWriteInfo();

                    string content = request.HttpGet(string.Format(iUrl, order));

                    string addressInfo = CommonFun.GetValue(content, "<i class=\"fa fa-truck\"></i>收货人信息", "<div class=\"row\">");

                    string itemInfo = CommonFun.GetValue(content, "<tbody>", "</tbody>");

                    YFOrderInfo item = new YFOrderInfo();
                    item.Remark = info.Value;
                    item.Created = order;
                    item.SenderName = senderName;
                    item.SenderPhoneNumber = senderPhoneNumber;
                    MatchCollection aMs = CommonFun.GetValues(addressInfo, "<div class=\"col-md-4\">", "</div>");

                    item.ReceiverName = aMs[0].Value.Trim(); ;
                    item.ReceiverPhoneNumber = aMs[1].Value.Trim().Replace("&nbsp;", "");
                    item.ReceiverAddress = CommonFun.GetValue(addressInfo, "<div class=\"col-md-10\">", "</div>");

                    MatchCollection rMs = CommonFun.GetValues(itemInfo, " <tr>", "</tr>");

                    int count = 0;
                    foreach (Match m in rMs)
                    {
                        string nameInfo = CommonFun.GetValue(m.Value, "<td rowspan=\"1\" class=\"tdcenter lh24\" ", "</td>");
                        if (string.IsNullOrEmpty(nameInfo))
                        {
                            continue;
                        }
                        string name = CommonFun.GetValue(nameInfo, "_blank\">", "</a>");

                        MatchCollection iMs = CommonFun.GetValues(m.Value, "<td rowspan=\"1\" class=\"tdcenter lh24\">", "<");
                        item.ViewCount = iMs[0].Value;

                        item.ID += name.Trim().Replace(" ", "").Replace("\r\n", "") + " ";
                        item.ID += iMs[1].Value.Trim() + " ";
                        item.ID += iMs[2].Value.Trim() + " ";

                        string priceStr = iMs[3].Value.Trim().Replace(" ", "");
                        priceStr = priceStr.Replace("¥", "");
                        //item.ShopPrice = Convert.ToDecimal(priceStr.Trim());
                        //item.ID += iMs[2].Value.Trim() + " ";
                        string toPriceStr = iMs[4].Value.Trim().Replace(" ", "");
                        toPriceStr = toPriceStr.Replace("¥", "");
                        int itemCount = Convert.ToInt32(Convert.ToDecimal(toPriceStr) / Convert.ToDecimal(priceStr));
                        string stock = CommonFun.GetValue(m.Value, "库存：", "\"");
                        item.ID += itemCount + " " + "剩余：";
                        item.ID += Convert.ToInt32(stock) - itemCount;

                        YFOrderInfo tItem = new YFOrderInfo();
                        tItem.ItemName = name;
                        tItem.ReceiverZipCode = iMs[1].Value.Trim();
                        tItem.BuyerId = iMs[2].Value.Trim();
                        tItem.ExpressName = stock;//iMs1[1].Value.Trim().Replace("&nbsp;", "");
                        tItem.ExpressTemplate = priceStr.Trim();
                        if (++count == 1)
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

        public string UpdateRankAndRemark(string orderNO, string rank, string dec)
        {
            string uUrl = "https://yaodian.yaofangwang.com/order/EditDesc/{0}";
            string postData = "orderno={0}&rank={1}&desc={2}&X-Requested-With=XMLHttpRequest"; //"method=OrderDescShop&CustomerShopOrderNo={0}&dec={1}&rank={2}";
            string result = null;
            try
            {
                result = request.HttpPost(string.Format(uUrl, orderNO), string.Format(postData, orderNO, rank, CommonFun.GetUrlEncode(dec)));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return result;
        }

        /// <summary>
        /// 更新标记状态
        /// </summary>
        /// <param name="items"></param>
        public void UpdateTagState(List<YFOrderWriteInfo> items)
        {
            try
            {
                foreach (YFOrderWriteInfo wItem in items)
                {
                    BaseItemInfo item = wItem.BaseItem;

                    string orderNO = item.Created;
                    string rank = Convert.ToString((int)TagColor.Green);
                    string dec = wItem.BaseItem.Remark + string.Format("({0})", startOrderNO.ToString());

                    //if (!IsRedTag(item.ViewCount))
                    //{
                    //    rank = TagColor.Green.ToString();
                    //    dec += "+2017发走";
                    //}
                    //else if (!item.SellType.Contains("无需发票"))
                    //{
                    //    rank = TagColor.Green.ToString();
                    //    dec += "开";
                    //}

                    string result = UpdateRankAndRemark(orderNO, rank, dec);

                    item.Name = startOrderNO.ToString();
                    //item.Remark = dec;

                    foreach (BaseItemInfo sItem in wItem.TotalItem)
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

                    string receiverStr = item.ID + " " + item.Remark;

                    item.Remark = null;

                    CommonFun.WriteCSV(filePath + "send1_" + ticks + fileExtendName, item);

                    YFOrderInfo yfItem = item as YFOrderInfo;

                    yfItem.ItemName = null;
                    yfItem.ExpressNO = null;
                    yfItem.ReceiverName = null;
                    yfItem.ReceiverPhoneNumber = null;
                    yfItem.SenderName = null;
                    yfItem.SenderPhoneNumber = null;
                    yfItem.ReceiverAddress = receiverStr;

                    CommonFun.WriteCSV(filePath + "send2_" + ticks + fileExtendName, item);

                    foreach (BaseItemInfo sItem in wItem.TotalItem)
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
        /// 处理物流信息（填写发货）
        /// </summary>
        /// <returns></returns>
        public void OptLogistics()
        {
            try
            {
                Dictionary<string, string> orderList = new Dictionary<string, string>();
                int page = 1;
                int totalPage = 0;
                Login(2);
                do
                {
                    try
                    {
                        string sUrl = string.Format("https://yaodian.yaofangwang.com/order/List?status=PAID&page={0}", page);

                        string content = request.HttpGet(sUrl);

                        if (totalPage == 0)
                        {
                            string totalPageStr = CommonFun.GetValue(content, "条，共", "页，");

                            totalPage = Convert.ToInt32(totalPageStr);
                        }

                        Console.WriteLine("Running OptLogistics totalPage:{0} page:{1}", totalPage, page);

                        GetOrderNOAndDesc(content, orderList, 3);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                } while (++page <= totalPage);

                foreach (string orderNO in orderList.Keys)
                {
                    try
                    {
                        string tUrl = string.Format("https://yaodian.yaofangwang.com/order/Send/{0}", orderNO);

                        string content = request.HttpGet(tUrl);

                        if (!content.Contains("确认发货"))
                        {
                            OptPrescription();
                            content = request.HttpGet(tUrl);
                            Login(2);
                        }

                        //string viewState = CommonFun.GetUrlEncode(CommonFun.GetValue(content, "id=\"__VIEWSTATE\" value=\"", "\""), false);

                        //string generator = CommonFun.GetValue(content, "id=\"__VIEWSTATEGENERATOR\" value=\"", "\"");

                        List<string> dateStr = new List<string>();

                        List<string> orderNOStr = new List<string>();

                        content = CommonFun.GetValue(content, " <tbody>", "</tbody>");

                        MatchCollection ms = CommonFun.GetValues(content, " <tr", "</tr>");

                        int count = 0;
                        string listPostStr = "&batch_list%5B{0}%5D%5Bqty%5D={1}&batch_list%5B{0}%5D%5Bbatchno%5D={2}&batch_list%5B{0}%5D%5Bmedicine_orderno%5D={3}";
                        string itemListPostStr = "";
                        foreach (Match m in ms)
                        {
                            //batch_list[0][qty]:2
                            //batch_list[0][batchno]:2018-12-31
                            //batch_list[0][medicine_orderno]:C9010610304980441-1
                            string qty = CommonFun.GetValue(m.Value, "<input type=\"number\" minvalue=\"1\" value=\"", "\"");
                            string batchno = GetCreateDate();
                            string medicine_orderno = CommonFun.GetValue(m.Value, "data-id=\"", "\"");
                            itemListPostStr += string.Format(listPostStr, count, qty, batchno, medicine_orderno);

                            count++;
                        }

                        //string datePostStr = string.Join("#", orderNOStr.ToArray()) + "@" + string.Join("#", dateStr.ToArray());

                        //string postData = string.Format("__EVENTTARGET=ctl00%24cph_content%24btn_Send&__EVENTARGUMENT=&__VIEWSTATE={0}&__VIEWSTATEGENERATOR={1}&ctl00%24cph_content%24hf_productnoandcustomerordermedicineno={2}&ctl00%24cph_content%24ddl_Logistics=2&ctl00%24cph_content%24txt_OrderWebNumber={3}&ctl00%24cph_content%24txt_InvoiceNo=&ctl00%24cph_content%24txt_code=&sendAddress=rb_Address&ctl00%24cph_content%24hf_AddressId={4}&returnAddress=rb_Address&ctl00%24cph_content%24hf_returnAddressId={5}&ctl00%24cph_content%24stype=0", viewState, generator, CommonFun.GetUrlEncode(datePostStr, false), orderList[orderNO], 1048, 2374);
                        string postData = string.Format("orderno={0}&trafficno={1}&invoiceno=&trafficid=2&traffic_name=%E5%9C%86%E9%80%9A%E5%BF%AB%E9%80%92&return_store_addressid=6834&store_addressid=6783&used_eew=0{2}", orderNO, orderList[orderNO], itemListPostStr);

                        string subUrl = "https://yaodian.yaofangwang.com/order/Send";

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

                result = ErpChangePrice(item); //UpdateItemInfo(item);
            } while (++doCount < 2 && !result);

            return result;
        }

        public int GetClickingRate(BaseItemInfo item)
        {
            try
            {
                string content = request.HttpGet("https://www.yaofangwang.com/medicine-" + item.ItemName + ".html?sort=price&sorttype=asc", null, true);

                string clickingRateStr = CommonFun.GetValue(content, "<dt>最近浏览</dt>", "</dd>");

                clickingRateStr = CommonFun.GetValue(clickingRateStr, ">", "次");

                clickingRateStr = string.IsNullOrEmpty(clickingRateStr) ? "0" : CommonFun.GetNum(clickingRateStr);

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
                        Thread.Sleep(random.Next(15, 20) * 1000);
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <param name="getPriceStock">有效价格库存</param>
        /// <param name="lowerStock">改价最小库存值</param>
        /// <param name="minClickCount">点击率</param>
        /// <param name="mtlowerPrice">大于最低库存量降价值</param>
        /// <param name="ltlowerPrice">小于最低库存量降价值</param>
        /// <param name="maxDownRate">最大降价幅度</param>
        /// <param name="opt"></param>
        private void UpdatePriceProcess(BaseItemInfo item, int getPriceStock, int lowerStock, int minClickCount, decimal mtlowerPrice, decimal ltlowerPrice, int maxDownRate, bool opt)
        {
            try
            {
                int iClickingRate = GetClickingRate(item);

                decimal minPrice = decimal.MaxValue;

                int stock = getPriceStock;

                string iFileName = "YF/updatePriceUpFive";

                decimal diffPrice = mtlowerPrice;

                bool isLimitDown = true;

                if (iClickingRate >= minClickCount)
                {
                    if (Convert.ToInt16(item.Inventory) <= lowerStock)
                    {
                        stock = 0;
                        iFileName = "YF/updatePriceLowFive";
                        diffPrice = ltlowerPrice;
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
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        /// <summary>
        /// 带后缀编码的商品处理
        /// </summary>
        /// <param name="item"></param>
        private void PointTypeProcess(BaseItemInfo item, bool opt)
        {
            try
            {
                UpdatePriceProcess(item, pointTypInfo.GetPriceStock, pointTypInfo.LowerStock, pointTypInfo.ClickCount, pointTypInfo.MTLowerPrice, pointTypInfo.LTLowerPrice, pointTypInfo.MaxDownRate, opt);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public void InitTCPConfig()
        {
            Common.TcpSetting = new TCPSetting
            {
                IP = "server-erp.yaofangwang.com",
                Secret = "ybyz!@#LHL8!234!$%&^@#BD1974&65",
                Port = 18280,
                IsSSL = false,
                BundleId = 2004,
                VersionId = 0,
                DbId = 2004
            };
            Common.TcpSetting_ForUpload = new TCPSetting
            {
                Upload_IP = "cdn.yaofangwang.com",
                Upload_Port = 18480,
                IsSSL = false
            };
        }

        public bool ErpLogin()
        {
            try
            {
                AppFramework.Start(true);
                InitTCPConfig();
                Common.CurrentAccount = new APP_YFT.DataService.SYS_Account().Login(username1, password1);
                new SYS_ShopConfig().LoadShopConfig(Common.CurrentAccount.ShopId);
                if (Common.CurrentAccount != null)
                {
                    Console.WriteLine("ERP Login Sccussed!!!!!!");
                }
                return Common.CurrentAccount != null;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return false;
        }
        /// <summary>
        /// erp系统中加载在售数据
        /// </summary>
        /// <returns></returns>
        public List<BaseItemInfo> ErpLoadShopData(int statu = 1)
        {
            try
            {
                APP_YFT.DataService.STK_StockBatch stk_StockBatch = new APP_YFT.DataService.STK_StockBatch();
                string retailprice = "";
                //if (this.NotSettingPrice)
                //{
                //    retailprice = "toquery";
                //}
                //string customerprice = "";
                //if (this.SettingCustomerPrice)
                //{
                //    customerprice = "toquery";
                //}
                int totalCount;
                List<object> priceList = stk_StockBatch.GetPriceList(null, new
                {
                    keywords = "",
                    retailprice = retailprice,
                    customerprice = "",
                    RatioBegin = "",
                    RatioEnd = ""
                }, 9999, 1, out totalCount);

                List<BaseItemInfo> items = new List<BaseItemInfo>();

                APP_YFT.DataService.STK_ShopMedicine stk_ShopMedicine = new APP_YFT.DataService.STK_ShopMedicine();

                int curCount = 0;
                foreach (object itemObj in priceList)
                {
                    Console.WriteLine("TotalCount:{0},CurCount:{1} loading........", totalCount, ++curCount);

                    string itemStr = itemObj.ToString();

                    BaseItemInfo item = new BaseItemInfo();
                    item.Inventory = CommonFun.GetValue(itemStr, "\"Reserve\": ", ",");
                    item.ID = CommonFun.GetValue(itemStr, "\"AuthorizedCode\": \"", "\"");
                    item.Name = CommonFun.GetValue(itemStr, "\"NameCN\": \"", "\"");
                    //item.ItemName = CommonFun.GetValue(itemStr, "\"ShopId\": ", ",");
                    item.ViewCount = CommonFun.GetValue(itemStr, "\"Id\": ", ",");
                    item.Type = CommonFun.GetValue(itemStr, "ProductNumber\": \"", "\"");
                    item.Format = CommonFun.GetValue(itemStr, "\"Standard\": \"", "\"");
                    item.Created = CommonFun.GetValue(itemStr, "\"MillTitle\": \"", "\"");
                    //在售列表

                    if (statu == 1 && item.Inventory != "0")
                    {
                        int tryConCount = 0;
                        bool isCon = false;
                        do
                        {
                            try
                            {
                                APP_YFT.Model.STK_ShopMedicine stk_ShopMedicine2 = stk_ShopMedicine.Get(item.ViewCount);
                                item.ItemName = stk_ShopMedicine2.MedicineId.ToString();
                                if (item.ItemName == "-1")
                                {
                                    try
                                    {
                                        item.ItemName = SeachMedicineIdByID(item.ID, item.Format);
                                    }
                                    catch (Exception e)
                                    {
                                        Console.WriteLine(e);
                                    }
                                }
                                //获取最大最小现价
                                if (stk_ShopMedicine2.MedicineId > 0)
                                {
                                    List<string> shopPriceArea = stk_ShopMedicine.GetShopPriceArea(stk_ShopMedicine2.MedicineId.ToString());
                                    item.ShopPriceMin = Convert.ToDouble(shopPriceArea[0]);
                                    item.ShopPriceMax = Convert.ToDouble(shopPriceArea[1]);
                                }
                                else
                                {
                                    item.ShopPriceMin = 0.00;
                                    item.ShopPriceMax = double.MaxValue;
                                }
                                isCon = true;

                                //商城返利
                                item.ReturnPrice = stk_ShopMedicine2.CashBackMoney;

                                item.PlatformPrice = Convert.ToDecimal(CommonFun.GetValue(itemStr, "\"RetailPrice\": ", ","));

                                item.ShopPrice = item.PlatformPrice - item.ReturnPrice;

                                items.Add(item);

                                //if (curCount % 2 == 0)
                                //{
                                //    Thread.Sleep(random.Next(1, 3) * 1000);
                                //}
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex);
                                Thread.Sleep(random.Next(3, 5) * 1000);
                            }
                        } while (!isCon && tryConCount++ < 3);

                        if (!isCon)
                        {
                            Console.WriteLine("load failed {0}..........", curCount);
                        }
                    }
                    else if (statu == 2 && item.Inventory == "0")   //下架列表
                    {
                        items.Add(item);
                    }
                }

                return items;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return null;
        }

        public bool ErpChangePrice(BaseItemInfo item)
        {
            try
            {
                STK_StockBatch stk_StockBatch = new STK_StockBatch();

                double num = Convert.ToDouble(item.PlatformPrice);  //出售价格
                double num2 = Convert.ToDouble(item.ReturnPrice);  //商城优惠金额
                string text = ""; //会员价

                if (num2 >= num)
                {
                    MsgBox.Show("商城优惠金额需小于零售价！");
                    return false;
                }


                STK_ShopMedicine stk_ShopMedicine = new STK_ShopMedicine();
                APP_YFT.Model.STK_ShopMedicine stk_ShopMedicine2 = stk_ShopMedicine.Get(item.ViewCount);
                if (stk_ShopMedicine2.MedicineType != 6)
                {
                    double num3 = num - num2;
                    double num4 = Convert.ToDouble(item.ShopPriceMin);
                    double num5 = Convert.ToDouble(item.ShopPriceMax);
                    if (((num4 > 0.0 && num3 < num4) || (num5 > 0.0 && num3 > num5)) && !MsgBox.Show("温馨提示", string.Concat(new string[]
					{
						"商城价：",
						num3.ToString("0.00"),
						" 不在商城控价区间 [",
						num4.ToString("0.00"),
						" - ",
						num5.ToString("0.00"),
						"] 内，\r\n保存后商城商品将会被强制下架，确定保存吗？"
					}), MsgBoxType.Warnning, MsgBoxShowType.OkAndCancel, "", "", ""))
                    {
                        return false;
                    }
                }

                return (bool)stk_StockBatch.EditPrice(Convert.ToInt64(item.ViewCount), num, BaseViewModel.AccountInfo.RealName, text, num2);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return false;
        }
        public void UpdatePrice()
        {
            try
            {
                ErpLogin();
                Login(2);

                bool opt = true;

                Thread autoUpDownThread = new Thread(AutoUpDown);
                //autoUpDownThread.Start();

                DateTime startTime = DateTime.MinValue;

                while (true)
                {
                    try
                    {
                        if (startTime == DateTime.MinValue || (DateTime.Now - startTime).Hours > 2)
                        {
                            startTime = DateTime.Now;

                            ticks = startTime.Ticks;

                            Dictionary<string, BaseItemInfo> sItems = GetSellingItems(!opt);

                            int count = 0;
                            foreach (BaseItemInfo item in sItems.Values)
                            {
                                //在黑名单中自动下架
                                if (IsBlacklistStore(item.Created) || IsBlackName(item.Name))
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

                                if (IsPointType(item.Type))
                                {
                                    PointTypeProcess(item, opt);
                                    continue;
                                }

                                if (IsInSpcTypeList(item.Type))
                                {
                                    decimal minPrice = decimal.MaxValue;

                                    minPrice = GetMinPrice(item, 0);

                                    OptUpdatePrice(minPrice, item, lPrice, opt, true, "YF/updatePriceSpc", spcMinDownRate);
                                }
                                else if (IsInTypeList(item.Type))
                                {
                                    UpdatePriceProcess(item, minStock, Convert.ToInt16(minStockList[0]), clickingRate, lPrice, Convert.ToDecimal(minStockList[1]), minDownRate, opt);
                                    //int iClickingRate = GetClickingRate(item);

                                    //decimal minPrice = decimal.MaxValue;

                                    //int stock = minStock;

                                    //string iFileName = "YF/updatePriceUpFive";

                                    //decimal diffPrice = lPrice;

                                    //bool isLimitDown = true;

                                    //if (iClickingRate >= clickingRate)
                                    //{
                                    //    if (Convert.ToInt16(item.Inventory) <= Convert.ToInt16(minStockList[0]))
                                    //    {
                                    //        stock = 0;
                                    //        iFileName = "YF/updatePriceLowFive";
                                    //        diffPrice = Convert.ToDecimal(minStockList[1]);
                                    //        isLimitDown = false;
                                    //    }
                                    //}
                                    //else
                                    //{
                                    //    stock = 0;
                                    //}

                                    //minPrice = GetMinPrice(item, stock);

                                    //OptUpdatePrice(minPrice, item, diffPrice, opt, isLimitDown, iFileName, minDownRate);
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

                    item.Name = CommonFun.GetValue(iMs[0].Value, "title=\"", "\">");
                    item.ID = CommonFun.GetValue(iMs[1].Value, "title=\"", "\">");
                    item.Format = CommonFun.GetValue(iMs[2].Value, "title=\"", "\">");
                    item.Format = string.IsNullOrEmpty(item.Format) ? CommonFun.GetValue(iMs[2].Value, "title=\"\">", "<") : item.Format;
                    item.Format = item.Format.Trim();
                    item.Created = iMs[4].Value.Trim();
                    string priceStr = CommonFun.GetValue(m.Value, "<div par='price'>", "</div>");
                    priceStr = string.IsNullOrEmpty(priceStr) ? CommonFun.GetValue(m.Value, "¥", "<") : CommonFun.GetValue(priceStr, "¥", "<");
                    item.ShopPrice = string.IsNullOrEmpty(priceStr) ? 0 : Convert.ToDecimal(priceStr);
                    item.Inventory = CommonFun.GetValue(m.Value, "库存", "件");
                    item.Inventory = item.Inventory.Trim();

                    item.ItemName = CommonFun.GetValue(m.Value, "<input type=\"hidden\" id=\"hf_MedicineId\" name=\"hf_MedicineId\" value=\"", "\"");
                    item.ViewCount = CommonFun.GetValue(m.Value, "<input type=\"hidden\" id=\"hf_ShopMedicineId\" name=\"hf_ShopMedicineId\" value=\"", "\"");

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
        /// 获取text的值
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private string GetTextVaule(string text)
        {
            return CommonFun.GetValue(text, "value=\"", "\"");
        }

        public void GetGoodsSales()
        {
            try
            {
                Login(2);


                string url = "https://yaodian.yaofangwang.com/goods_sales/List";

                string postDataStr = "pagination%5Bpage%5D={0}&pagination%5Bpages%5D={1}&pagination%5Bperpage%5D=50&pagination%5Btotal%5D={2}&query%5Bdatetype%5D=3&query%5B%5D=on&query%5Bbegindate%5D={3}&query%5Benddate%5D={4}";

                string content = request.HttpGet(url, null, true);
                int totalPage = 0;
                int page = 1;
                int onePageCount = 50;
                int totalCount = 100;
                string endTime = DateTime.Now.ToString("yyyy-MM-dd");
                string startTime = DateTime.Now.AddDays(-90).ToString("yyyy-MM-dd");

                do
                {
                    //request.HttpGet(url, null, true);
                    string postData = string.Format(postDataStr, page, onePageCount, totalCount, startTime, endTime);

                    content = request.HttpPost(url, postData, heads);


                    JObject job = (JObject)JsonConvert.DeserializeObject(content);

                    if (totalPage == 0)
                    {
                        toltalCount = Convert.ToInt32(job["result"]["meta"]["total"].ToString());
                        totalPage = Convert.ToInt32(Math.Ceiling(toltalCount / (double)onePageCount));
                    }

                    string dataStr = job["result"]["data"].ToString();

                    List<BaseItemInfo> items = GetOnePageSalesItems(dataStr);

                    foreach (BaseItemInfo item in items)
                    {
                        CommonFun.WriteCSV(fileName + "goodsSales" + ticks + fileExtendName, item);
                    }
                    Console.WriteLine("{0} totalPage:{1} page:{2}", DateTime.Now, totalPage, page);
                } while (totalPage >= ++page);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private List<BaseItemInfo> GetOnePageSalesItems(string content)
        {
            List<BaseItemInfo> items = new List<BaseItemInfo>();
            try
            {
                if (!string.IsNullOrEmpty(content))
                {
                    JArray pJob = (JArray)JsonConvert.DeserializeObject(content);

                    foreach (JObject job in pJob)
                    {
                        SalesItemInfo item = new SalesItemInfo();
                        item.SalesRanking = job["row_index"].ToString();
                        item.ItemName = job["namecn"].ToString();
                        item.Format = job["standard"].ToString();
                        item.Type = job["troche_type"].ToString();
                        item.SalesVolume = job["qty"].ToString();
                        item.SalesAmount = job["price"].ToString();
                        items.Add(item);
                    }
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
                string qUrl = "https://yaodian.yaofangwang.com/product/edit/{0}";

                string iUrl = string.Format(qUrl, item.ViewCount);

                string content = request.HttpGet(iUrl, null, true);

                if (!string.IsNullOrEmpty(content))
                {
                    string limitMinPriceStr = CommonFun.GetValue(content, "<input type=\"hidden\" ID=\"hf_lockPrice\" value=\"", "\"");

                    decimal limitMinPrice = string.IsNullOrEmpty(limitMinPriceStr) ? 0 : Convert.ToDecimal(limitMinPriceStr);

                    if (limitMinPrice > 0)
                    {
                        if (item.PlatformPrice == 0)
                        {
                            item.PlatformPrice = item.ShopPrice;
                        }

                        item.Remark = limitMinPrice.ToString();
                        CommonFun.WriteCSV(fileName + "limit" + ticks + fileExtendName, item);
                        if (limitMinPrice == item.ShopPrice)
                        {
                            return true;
                        }

                        item.PlatformPrice = item.PlatformPrice < limitMinPrice ? limitMinPrice : item.PlatformPrice;
                    }

                    MatchCollection ms = CommonFun.GetValues(content, "<input type=\"text\"", "/>");
                    int index = 1;
                    string authorizedCode = GetTextVaule(ms[index++].Value);
                    string namecn = GetTextVaule(ms[index++].Value);
                    string aliascn = GetTextVaule(ms[index++].Value);
                    string trocheType = GetTextVaule(ms[index++].Value);
                    string standard = GetTextVaule(ms[index++].Value);
                    string title = GetTextVaule(ms[index++].Value);
                    string number = GetTextVaule(ms[index++].Value);
                    //string weight = GetTextVaule(ms[index++].Value);
                    string barcode = GetTextVaule(ms[index++].Value);
                    string price = item.PlatformPrice.ToString(); // GetTextVaule(ms[9].Value);
                    string max_buyqty = GetTextVaule(ms[++index].Value);
                    string reserve = item.Inventory;// GetTextVaule(ms[11].Value);

                    string scheduledDays = CommonFun.GetValue(content, "<select id=\"ddl_ScheduledDays\"", "</select>");
                    MatchCollection sMs = CommonFun.GetValues(scheduledDays, "<option ", "</option>");
                    foreach (Match sM in sMs)
                    {
                        if (sM.Value.Contains("selected"))
                        {
                            scheduledDays = CommonFun.GetValue(sM.Value, "value=\"", "\"");
                            break;
                        }
                    }

                    if (string.IsNullOrEmpty(status))
                    {
                        status = CommonFun.GetValue(content, "<select id=\"ddl_Status\"", "</select>");
                        MatchCollection statusMs = CommonFun.GetValues(status, "<option ", "</option>");
                        foreach (Match sM in statusMs)
                        {
                            if (sM.Value.Contains("selected"))
                            {
                                status = CommonFun.GetValue(sM.Value, "value=\"", "\"");
                                break;
                            }
                        }
                    }

                    string typeid = "0";
                    string periodTo = "";

                    string postDateStr = "store_medicineid={0}&medicine_barcode={1}&authorized_code={2}&namecn={3}&standard={4}&troche_type={5}&aliascn={6}&scheduled_days={7}&store_medicine_status={8}&mill_title={9}&product_number={10}&store_medicine_typeid={11}&reserve={12}&max_buyqty=&price={13}&period_to=";

                    postDateStr = string.Format(postDateStr,
                         item.ViewCount,
                         barcode,
                         CommonFun.GetUrlEncode(authorizedCode, false),
                         CommonFun.GetUrlEncode(namecn, false),
                         CommonFun.GetUrlEncode(standard, false),
                         CommonFun.GetUrlEncode(trocheType, false),
                         CommonFun.GetUrlEncode(aliascn, false),
                         scheduledDays,
                         status,
                         CommonFun.GetUrlEncode(title, false),
                         number,
                         typeid,
                         reserve,
                         price);

                    content = request.HttpPost(iUrl, postDateStr, heads);
                    string result = CommonFun.GetValue(content, "\"code\":", ",");
                    if (result == "1")
                    {
                        return true;
                    }
                    else
                    {
                        item.Remark += content;
                        Console.WriteLine("UpdateItemInfo result:{0}", content);
                        if (content.Contains("价格每天只能修改一次"))
                        {
                            return true;
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
            Dictionary<string, BaseItemInfo> items = new Dictionary<string, BaseItemInfo>();

            if (isTest)
            {
                BaseItemInfo item = new BaseItemInfo();
                item.ID = "国药准字Z10940063";
                item.ViewCount = "235";
                item.Format = "10mlx24支/盒";
                item.Name = "复方双花口服液";
                item.Created = "北京康益药业有限公司";
                item.Type = "T-2020";
                item.Inventory = "31";
                item.ItemName = "631691";
                //item.ID = "注册证号H20160414";
                //item.ViewCount = "16323772";
                //item.Format = "50mgx10片x2板/盒";
                //item.Name = "盐酸伊托必利片";
                //item.Created = "MYLANEPDG.K.,KATSUYAMAPLANT";
                //item.Type = "2019";
                //item.Inventory = "8";
                //item.ItemName = "552603";
                items.Add("", item);
                return items;
            }

            List<BaseItemInfo> listItmes = ErpLoadShopData();

            foreach (BaseItemInfo item in listItmes)
            {
                string key = item.ViewCount;
                if (!items.ContainsKey(key))
                {
                    items.Add(key, item);
                }
                else
                {
                    Console.WriteLine("GetSelingItems is same item" + item.Name);
                }
            }

            return items;

            //return GetItemsByStatus(1);
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
                    string url = string.Format("https://yaodian.yaofangwang.com/product/list/?Status={1}&page={0}", page, status);

                    //Dictionary<string, string> heads = new Dictionary<string,string>();
                    //heads.Add("Upgrade-Insecure-Requests", "1");
                    string content = request.HttpGet(url, null, true);

                    if (totalPage == 0)
                    {
                        string pageStr = CommonFun.GetValue(content, "<div class=\"m-pager\"><div class='pager'><div class='info'>共", "</div>");

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
