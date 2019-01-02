using GetWebPageDate.Http;
using GetWebPageDate.Util.Item;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Media;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace GetWebPageDate.Util
{
    /// <summary>
    /// 同康
    /// </summary>
    public class ReadTKWebPageValue : BaseReadWebPage
    {
        private string host = "https://www.tkyfw.com";

        /// <summary>
        /// 获取在售列表
        /// </summary>
        private Dictionary<string, BaseItemInfo> sellItems;

        /// <summary>
        /// 获取下架列表
        /// </summary>
        private Dictionary<string, BaseItemInfo> downItems;

        /// <summary>
        /// 以搜索的物品
        /// </summary>
        Dictionary<string, string> seachedItemID = new Dictionary<string, string>();

        /// <summary>
        /// 药房平台搜索数据
        /// </summary>
        Dictionary<string, BaseItemInfo> seachPlatItems = new Dictionary<string, BaseItemInfo>();

        /// <summary>
        /// 药房平台所有列表
        /// </summary>
        Dictionary<string, List<BaseItemInfo>> seachItemList = new Dictionary<string, List<BaseItemInfo>>();

        private string login_url = "https://admin.tkyfw.com/Account/loginAction";

        private string store_name = "长沙县001店";

        private string other_store_name = "巨野001店";

        private DateTime startTime = DateTime.MinValue;

        Dictionary<string, BaseItemInfo> items = new Dictionary<string, BaseItemInfo>();

        Dictionary<string, string> orderList = new Dictionary<string, string>();

        private SoundPlayer s = new SoundPlayer("2674.wav");

        private DateTime lastDown = DateTime.Now;

        private int oneMinDownCount = 0;

        private string fileName = "TK/";

        private string username;

        private string password;

        private string yfSellingItemType = "666";

        ///// <summary>
        ///// 是否需要加载
        ///// </summary>
        ///// <param name="name"></param>
        ///// <returns></returns>
        //private bool CanLoad(string name)
        //{
        //    foreach (string nuload in unloadList)
        //    {
        //        if (name.Contains(nuload))
        //        {
        //            return false;
        //        }
        //    }

        //    return true;
        //}

        public ReadTKWebPageValue()
        {
            string userInfo = ConfigurationManager.AppSettings["tkUAndP"];
            string[] aUserInfo = userInfo.Split(',');

            username = aUserInfo[0];
            password = aUserInfo[1];
        }

        public override void ReadAllMenuURL()
        {
            string url = "https://www.tkyfw.com/";

            string content = request.HttpGet(url);

            content = CommonFun.GetValue(content, "<div class=\"nbanner\">", "<div class=\"banner\">");

            MatchCollection lMs = CommonFun.GetValues(content, "<li class=\"mod_cate\">", "</li>");

            foreach (Match lM in lMs)
            {
                string mName = CommonFun.GetValue(lM.Value, "<div class=\"icon", "</a>");

                if (mName.Contains("专科用药") || mName.Contains("中西医药") || mName.Contains("养生保健"))
                {
                    MatchCollection sLMs = CommonFun.GetValues(lM.Value, "<dl>", "</dl>");

                    foreach (Match sLM in sLMs)
                    {
                        string sMName = CommonFun.GetValue(sLM.Value, "\">", "</a></dt>");

                        if (sMName != "中药饮片")
                        {
                            MatchCollection ms = CommonFun.GetValues(sLM.Value, "<dd>", "</dd><dd>");

                            foreach (Match m in ms)
                            {
                                MatchCollection uMs = CommonFun.GetValues(m.Value, "<a href=\"", "\"");

                                foreach (Match uM in uMs)
                                {
                                    if (!AllMenuUrl.Contains(uM.Value))
                                    {
                                        AllMenuUrl.Add(uM.Value);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            ReadAllItem();
        }


        private List<string> GetItemUrls(string url)
        {
            List<string> itemUrls = new List<string>();

            try
            {
                string nextPage = "";
                do
                {
                    url = string.IsNullOrEmpty(nextPage) ? url : (host + "/" + nextPage);
                    string content = request.HttpGet(url);

                    nextPage = CommonFun.GetValue(content, "上一页", "</div>");
                    nextPage = string.IsNullOrEmpty(nextPage) ? content : nextPage;
                    nextPage = CommonFun.GetValue(nextPage, "<a class=\"prev\" href=\"", "\">下一页");

                    content = CommonFun.GetValue(content, "<div class=\"sl_sort list_sx\">", "<div class=\"pagination\">");

                    MatchCollection iMs = CommonFun.GetValues(content, "<div class=\"nubshop\">", "</div>");

                    //MatchCollection ms = CommonFun.GetValues(content, "<a href=\"", "\" class=\"left\" target=\"_blank\">查看详情</a>");

                    foreach (Match m in iMs)
                    {
                        string itemUrl = host + CommonFun.GetValue(m.Value, "<a href=\"", "\"");
                        if (!itemUrls.Contains(itemUrl))
                        {
                            itemUrls.Add(itemUrl);
                        }
                    }
                } while (!string.IsNullOrEmpty(nextPage));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return itemUrls;
        }

        public override void ReadAllItem()
        {
            int totalCount = AllMenuUrl.Count;
            int curCount = 0;
            foreach (string url in AllMenuUrl)
            {
                Console.WriteLine("totalCount:{0}, curCount:{1}, url:{2}", totalCount, curCount++, url);
                List<string> itemUrls = GetItemUrls(url);

                RequestInfo requestInfo = new RequestInfo();
                requestInfo.RequestParams.Add("requestType", "get");
                requestInfo.Urls = itemUrls;

                RequestUrls(requestInfo);

                foreach (string value in requestInfo.Contentes)
                {
                    try
                    {
                        //string content = request.HttpGet(itemUrl);

                        if (string.IsNullOrEmpty(value))
                        {
                            continue;
                        }

                        string content = value;

                        int startIndex = content.IndexOf("<div class=\"buying\">");

                        int endIndex = content.IndexOf("<div class=\"shop\">");

                        content = content.Substring(startIndex, endIndex - startIndex);

                        MatchCollection ms = CommonFun.GetValues(content, "<td class=\"td3\">", "</td>");

                        if (!string.IsNullOrEmpty(ms[0].Value))//&& CanLoad(ms[0].Value))
                        {
                            ItemInfo info = new ItemInfo();
                            info.Name = ms[0].Value;
                            info.ID = ms.Count > 2 ? ms[2].Value : "";
                            info.Created = CommonFun.GetValue(content, "<td class=\"td3\" rel=\"theqiye\">", "</td>");

                            string priceStr = CommonFun.GetValue(content, "<strong class=\"value yahei\">", "</strong> ");

                            info.ShopPrice = string.IsNullOrEmpty(priceStr) ? 0 : Convert.ToDecimal(priceStr);

                            info.Format = CommonFun.GetValue(content, "<li class=\"cur\"><span><a href=\"", "/a>");

                            info.Format = string.IsNullOrEmpty(info.Format) ? "" : CommonFun.GetValue(info.Format, ">", "<");

                            string key = info.ID + "{" + info.Format + "}";

                            if (ShopAllItems.ContainsKey(key))
                            {
                                if (info.ShopPrice != 0 && ShopAllItems[key].ShopPrice > info.ShopPrice)
                                {
                                    ShopAllItems[key] = info;
                                }
                            }
                            else
                            {
                                ShopAllItems.Add(key, info);
                            }

                            CommonFun.WriteCSV("TK/TK" + ticks + ".csv", info);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                }
            }
        }
        public override bool ComparePrice(BaseItemInfo platformItem, BaseItemInfo info)
        {
            //15块钱以下的百分之40个点    15到30的百分之30  30到50百分之25    50以上百分之20
            decimal compacePrice = info.ShopPrice;
            bool result = false;
            if (compacePrice > 50)
            {
                if (compacePrice * (decimal)0.8 >= platformItem.ShopSelaPrice)
                {
                    info.PlatformPrice = platformItem.ShopSelaPrice;
                    info.Type = platformItem.Type;
                    info.Inventory = platformItem.Inventory;
                    info.ViewCount = platformItem.ViewCount;

                    CommonFun.WriteCSV("TK/50以上" + ticks + ".csv", info);
                    result = true;
                }
            }
            else if (compacePrice > 30)
            {
                if (compacePrice * (decimal)0.75 >= platformItem.ShopSelaPrice)
                {
                    info.PlatformPrice = platformItem.ShopSelaPrice;
                    info.Type = platformItem.Type;
                    info.Inventory = platformItem.Inventory;
                    info.ViewCount = platformItem.ViewCount;

                    CommonFun.WriteCSV("TK/30-50" + ticks + ".csv", info);
                    result = true;
                }
            }
            else if (compacePrice > 15)
            {
                if (compacePrice * (decimal)0.7 >= platformItem.ShopSelaPrice)
                {
                    info.PlatformPrice = platformItem.ShopSelaPrice;
                    info.Type = platformItem.Type;
                    info.Inventory = platformItem.Inventory;
                    info.ViewCount = platformItem.ViewCount;

                    CommonFun.WriteCSV("TK/15-30" + ticks + ".csv", info);
                    result = true;
                }
            }
            else if (compacePrice > 5)
            {
                if (compacePrice * (decimal)0.6 >= platformItem.ShopSelaPrice)
                {
                    info.PlatformPrice = platformItem.ShopSelaPrice;
                    info.Type = platformItem.Type;
                    info.Inventory = platformItem.Inventory;
                    info.ViewCount = platformItem.ViewCount;

                    CommonFun.WriteCSV("TK/15以下" + ticks + ".csv", info);
                    result = true;
                }
            }

            return result;
        }

        public override void Login()
        {
            string loginUrl = "https://admin.tkyfw.com/Account/loginAction";
            string postDataStr = string.Format("store_name=%E9%95%BF%E6%B2%99%E5%8E%BF001%E5%BA%97&username={0}&pass={1}&verify=&viewlicense=1", username, password);
            request.Login(loginUrl, postDataStr);
        }

        public void UpdateNewFailed()
        {
            Login();

            ReadBaseItemInfo("TK/upNewFailed636553404656412971.csv", true);

            //读取在售列表
            sellItems = GetSellingItem();
            //读取下架列表
            downItems = GetDownListItem();

            bool opt = true;
            foreach (KeyValuePair<string, BaseItemInfo> info in ShopAllItems)
            {
                BaseItemInfo item = info.Value;

                string key = item.Name + item.Format + item.Created;

                if ((DateTime.Now - startTime).TotalMinutes > 30)
                {
                    Login();
                    startTime = DateTime.Now;
                }

                //不在在售列表需要上架
                if (!sellItems.ContainsKey(key))
                {
                    if (downItems.ContainsKey(key))
                    {
                        //需要重新上架
                        ReUpItem(downItems[key], opt);
                        Thread.Sleep(random.Next(2, 6) * 1000);
                    }
                    else
                    {
                        //需要新上架
                        UpNewItem(item, opt);
                    }
                    sellItems.Add(key, item);
                }
            }
        }

        /// <summary>
        /// 获取在售列表
        /// </summary>
        /// <param name="http"></param>
        /// <returns></returns>
        public Dictionary<string, BaseItemInfo> GetSellingItem(string id)
        {
            Dictionary<string, BaseItemInfo> items = new Dictionary<string, BaseItemInfo>();

            if (!string.IsNullOrEmpty(id))
            {
                string url = string.Format("https://admin.tkyfw.com/Goods/throughAudit.html?status=1&state=1&keyword={0}&condition=&reverse=&price_up=&price_down=&stock_up=&stock_down=&start_time=&end_time=", id);

                string content = request.HttpGet(url);

                string itemStr = CommonFun.GetValue(content, "<form name=\"form\" method=\"post\">", "</form>");

                List<BaseItemInfo> temp = GetOnePageItems(itemStr);

                string pageStr = CommonFun.GetValue(content, "下一页", "><span>末页</span>");
                pageStr = CommonFun.GetValue(pageStr, "&p=", "\"");
                pageStr = pageStr.Substring(pageStr.LastIndexOf("=") + 1);

                int totalPage = 0;

                if (!string.IsNullOrEmpty(pageStr))
                {
                    totalPage = Convert.ToInt32(pageStr);
                }

                for (int i = 2; i <= totalPage; i++)
                {
                    content = request.HttpGet("https://admin.tkyfw.com/Goods/throughAudit?/Goods/throughAudit=&status=1&state=1&p=" + i);

                    itemStr = CommonFun.GetValue(content, "<form name=\"form\" method=\"post\">", "</form>");

                    temp.AddRange(GetOnePageItems(itemStr));

                    Console.WriteLine("GetSellingItem {0}:GetSelling totalPage:{1}, curPage{2}.....", DateTime.Now, totalPage, i);
                }

                foreach (BaseItemInfo item in temp)
                {
                    string key = item.ID + item.Format;

                    if (!items.ContainsKey(key))
                    {
                        items.Add(key, item);
                    }
                    else
                    {
                        Console.WriteLine("Error, Same Itme key:{0}", key);
                    }
                }
            }

            return items;
        }

        private string GetDownItemStockId(BaseItemInfo item)
        {
            try
            {
                Dictionary<string, BaseItemInfo> items = GetDownItemList(item.ID);

                if (items.ContainsKey(item.ID + item.Format))
                {
                    return items[item.ID + item.Format].ViewCount;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return null;
        }

        private Dictionary<string, BaseItemInfo> GetDownItemList(string id)
        {
            try
            {
                Dictionary<string, BaseItemInfo> items = new Dictionary<string, BaseItemInfo>();

                string url = string.Format("https://admin.tkyfw.com/Goods/throughAudit.html?status=1&state=2&keyword={0}&condition=&reverse=&price_up=&price_down=&stock_up=&stock_down=&start_time=&end_time=", id);

                string content = request.HttpGet(url);

                string itemStr = CommonFun.GetValue(content, "<form name=\"form\" method=\"post\">", "</form>");

                List<BaseItemInfo> temp = GetOnePageItems(itemStr);

                string pageStr = CommonFun.GetValue(content, "下一页", "><span>末页</span>");
                pageStr = CommonFun.GetValue(pageStr, "&p=", "\"");
                pageStr = pageStr.Substring(pageStr.LastIndexOf("=") + 1);

                int totalPage = 0;

                if (!string.IsNullOrEmpty(pageStr))
                {
                    totalPage = Convert.ToInt32(pageStr);
                }

                for (int i = 2; i <= totalPage; i++)
                {
                    content = request.HttpGet("https://admin.tkyfw.com/Goods/throughAudit?/Goods/throughAudit=&status=1&state=2&p=" + i);

                    itemStr = CommonFun.GetValue(content, "<form name=\"form\" method=\"post\">", "</form>");

                    temp.AddRange(GetOnePageItems(itemStr));

                    Console.WriteLine("GetDownItemList {0}:GetDownItemList totalPage:{1}, curPage{2}.....", DateTime.Now, totalPage, i);
                }

                foreach (BaseItemInfo item in temp)
                {
                    string key = item.ID + item.Format;

                    if (!items.ContainsKey(key))
                    {
                        items.Add(key, item);
                    }
                    else
                    {
                        Console.WriteLine("Error, Same Itme key:{0}", key);
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

        private bool CanGetNewStock(BaseItemInfo item)
        {
            if (item.Type == "201")
            {
                return false;
            }
            else if (unUpdate.ContainsKey(item.Name + item.Format + item.Created))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 更新价格
        /// </summary>
        /// <param name="item"></param>
        /// <param name="http"></param>
        /// <returns></returns>
        public string UpdatePrice(BaseItemInfo item, string promotion = "115", int count = 0)
        {
            if (string.IsNullOrEmpty(item.ViewCount))
            {
                Dictionary<string, BaseItemInfo> sDic = GetSellingItem(item.ID);
                string key = item.ID + item.Format;
                foreach (BaseItemInfo sItem in sDic.Values)
                {
                    if (CommonFun.IsSameFormat(sItem.Format, item.Format))
                    {
                        item.ViewCount = sItem.ViewCount;
                        break;
                    }
                }
            }

            if (!string.IsNullOrEmpty(item.ViewCount))
            {
                string param = "business_item=&course=&mall_price={3}&market_price={0}&open_close=2&promotion={1}&storeid={2}&stock={4}";

                string inventory = !CanGetNewStock(item) ? item.Inventory : GetStock(item.ShopPrice).ToString();

                string postStr = string.Format(param, item.ShopPrice, promotion, item.ViewCount, item.ShopPrice, inventory);

                string updateUrl = "https://admin.tkyfw.com/Goods/do_editGoods";

                string info = request.HttpPost(updateUrl, postStr);

                if (info.Contains("2") && count == 0)
                {
                    item.ShopPrice = Convert.ToDecimal(CommonFun.GetValue(info, "info\":\"", "\""));
                    UpdatePrice(item, promotion, count++);
                }
                
                return info;
            }

            return null;
        }

        public void Test()
        {
            //Login();

            BaseItemInfo item = new BaseItemInfo();
            item.ID = "国食健字G20040785";
            item.Name = "全金R大豆异黄酮软胶囊";
            item.Created = "浙江全金药业股份有限公司";
            item.Format = "0.3g/粒*90粒";
            item.ShopPrice = 999;

            //UpNewItem(item, true);

            //UpdatePrice(item);

            ReadPlatFormWebPageValue plate = new ReadPlatFormWebPageValue();
            List<BaseItemInfo> infos = plate.SeachInfoByID("国食健字G20040785");

            foreach (BaseItemInfo info in infos)
            {
                if (CommonFun.IsSameFormat(info.Format, item.Format, info.Name, item.Name))
                {
                    Console.WriteLine("true");
                }
            }
        }

        public BaseItemInfo GetTKItemByPlateItem(BaseItemInfo item)
        {
            Dictionary<string, BaseItemInfo> tkItem = GetSellingItem(item.ID);

            foreach (BaseItemInfo sItem in tkItem.Values)
            {
                if (CommonFun.IsSameFormat(sItem.Format, item.Format))
                {
                    return sItem;
                }
            }

            return null;
        }

        public void UpdateFixedItem(bool opt)
        {
            //读取在售列表
            sellItems = GetSellingItem();
            //读取下架列表
            downItems = GetDownListItem();

            ReadPlatFormWebPageValue yf = new ReadPlatFormWebPageValue();
            yf.Login(2);

            Dictionary<string, BaseItemInfo> yfSellingItems = yf.GetSellingItems();

            foreach (KeyValuePair<string, BaseItemInfo> info in yfSellingItems)
            {
                BaseItemInfo item = info.Value;

                CommonFun.WriteCSV(fileName + "yfSelling" + ticks + fileExtendName, item);

                bool isSelling = false;
                foreach (KeyValuePair<string, BaseItemInfo> tkInfo in sellItems)
                {
                    BaseItemInfo tkItem = tkInfo.Value;
                    if (CommonFun.IsSameItem(item.ID, tkItem.ID, item.Format, tkItem.Format, item.Name, tkItem.Name))
                    {
                        isSelling = true;
                        break;
                    }
                }

                if (!isSelling)
                {
                    if (opt)
                    {
                        bool isDowning = false;
                        foreach (KeyValuePair<string, BaseItemInfo> tkDInfo in downItems)
                        {
                            BaseItemInfo tkDItem = tkDInfo.Value;
                            if (CommonFun.IsSameItem(item.ID, tkDItem.ID, item.Format, tkDItem.Format, item.Name, tkDItem.Name))
                            {
                                //重新上架
                                tkDItem.ShopPrice = tkDItem.PlatformPrice;
                                ReUpItem(downItems[tkDInfo.Key], opt);
                                UpdatePrice(downItems[tkDInfo.Key], yfSellingItemType);
                                isDowning = true;
                                break;
                            }
                        }

                        if (!isDowning)
                        {
                            //新发布
                            //item.ShopPrice = GetTKMinPrice(item.ID, item.Format);
                            if (UpNewItem(item, opt))
                            {
                                BaseItemInfo newItem = GetTKItemByPlateItem(item);
                                newItem.ShopPrice = GetTKMinPrice(newItem.ID, newItem.Format);
                                UpdatePrice(newItem, yfSellingItemType);
                            }
                        }
                    }

                    CommonFun.WriteCSV(fileName + "yfSellingUP" + ticks + fileExtendName, item);
                }
            }
        }

        public void ReUpDownItem()
        {
            try
            {
                DataTable data = CommonFun.ReadXLS("down636621014750660261.xlsx");

                Login();

                //读取下架列表
                downItems = GetDownListItem();

                Dictionary<string, BaseItemInfo> xlsDown = new Dictionary<string, BaseItemInfo>();
                for (int row = 0; row < data.Rows.Count; row++)
                {
                    try
                    {
                        BaseItemInfo item = new BaseItemInfo();
                        item.ID = data.Rows[row]["批准文号"].ToString();
                        item.Name = (string)data.Rows[row]["通用名称"].ToString();
                        item.Created = (string)data.Rows[row]["生产厂家"].ToString();
                        item.Format = (string)data.Rows[row]["包装规格"].ToString();
                        string priceStr = (string)data.Rows[row]["平台售价（最低价格）"].ToString();
                        item.ShopPrice = string.IsNullOrEmpty(priceStr) ? 9999 : Convert.ToDecimal(priceStr);
                        item.PlatformPrice = item.ShopPrice;
                        item.SellType = (string)data.Rows[row]["出售方式（零或整）"].ToString();
                        //item.Name = (string)data.Rows[row]["通用名称"].ToString();

                        string key = item.Name + item.Format + item.Created;
                        if (!xlsDown.ContainsKey(key))
                        {
                            xlsDown.Add(key, item);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                }

                foreach (KeyValuePair<string, BaseItemInfo> info in xlsDown)
                {
                    if (!info.Value.SellType.Contains("价格对比"))
                    {
                        if (downItems.ContainsKey(info.Key))
                        {
                            //重新上架
                            ReUpItem(downItems[info.Key], true);
                        }

                        CommonFun.WriteCSV("TK/ReUpDownItem" + ticks + ".csv", info.Value);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        /// <summary>
        /// 获取订单详细信息
        /// </summary>
        public void GetOrderDetail()
        {
            Login();

            try
            {
                int page = 1;

                int totalPage = 0;

                string allItemUrl = "https://admin.tkyfw.com/Order/allOrderList?pay=5&p={0}";

                do
                {
                    //1、获取所有已发货订单
                    string content = request.HttpGet(string.Format(allItemUrl, page));

                    if (!string.IsNullOrEmpty(content))
                    {
                        if (totalPage == 0)
                        {
                            string totalPageStr = CommonFun.GetValue(content, "共有", "条数据");

                            totalPage = Convert.ToInt32(totalPageStr) / 20;

                            totalPage += (Convert.ToInt32(totalPageStr) % 20 == 0 ? 0 : 1);
                        }

                        Console.WriteLine("Getting...... totalPage:{0},curPage{1}", totalPage, page);

                        MatchCollection ms = CommonFun.GetValues(content, "<br><a href=\"", "\"");

                        foreach (Match m in ms)
                        {
                            string info = request.HttpGet("https://admin.tkyfw.com" + m.Value);

                            BaseItemInfo item = GetDetailInfo(info);

                            if (item != null)
                            {
                                CommonFun.WriteCSV(fileName + "OrderDetail" + dateStr + ".csv", item);
                            }
                        }
                    }
                } while (++page <= totalPage);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        /// <summary>
        /// 获取物品价格
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public decimal GetItemPrice(BaseItemInfo item)
        {
            if (ShopAllItems.Count == 0)
            {
                ReadBaseItemInfo("TK/TK.csv", true);
            }

            string key = item.ItemName + item.Format + item.Created;
            if (ShopAllItems.ContainsKey(key))
            {
                return ShopAllItems[key].ShopPrice;
            }

            return item.ShopPrice;
        }

        /// <summary>
        /// 更新在售物品价格
        /// </summary>
        /// <param name="obj"></param>
        private void UpdateSellingItemPrice(object obj)
        {
            try
            {

                while (true)
                {
                    try
                    {
                        ticks = DateTime.Now.Ticks;

                        Dictionary<string, BaseItemInfo> sellingDic = GetSellingItem();

                        int totalCount = sellingDic.Count;

                        int curCount = 0;

                        foreach (BaseItemInfo item in sellingDic.Values)
                        {
                            decimal upParam = 1;

                            decimal price = GetTKMinPrice(item.ID, item.Format, false);

                            Console.WriteLine("UpdatePrice totalCount:{0}, curCount:{1}", totalCount, ++curCount);

                            if (item.Type != "333" && price != decimal.MaxValue && item.PlatformPrice != price && price > 0)
                            {
                                item.ShopPrice = price * upParam;
                                if (item.PlatformPrice * (decimal)0.9 < item.ShopPrice)
                                {
                                    string info = UpdatePrice(item, item.Type);

                                    Thread.Sleep(random.Next(3, 6) * 1000);

                                    if (info != "1")
                                    {
                                        string priceStr = CommonFun.GetValue(info, ":\"", "\"");

                                        item.SellType = info;
                                        //price = 0;

                                        //try
                                        //{
                                        //    price = Convert.ToDecimal(priceStr);
                                        //    if (price != 1)
                                        //    {
                                        //        price = (decimal)((double)price - 0.01);
                                        //    }
                                        //    else
                                        //    {
                                        //        price = GetItemPrice(item);
                                        //    }
                                        //}
                                        //catch (Exception ex)
                                        //{
                                        //    Console.WriteLine(ex);
                                        //}

                                        //if (price != item.PlatformPrice)
                                        //{
                                        //    UpdatePrice(http, price, item.ViewCount, tag, item.Inventory);
                                        //}
                                    }
                                }
                                else
                                {
                                    CommonFun.WriteCSV(fileName + "ToolowerPrice" + ticks + ".csv", item);
                                }
                            }
                            CommonFun.WriteCSV(fileName + "UpdatePrice" + ticks + ".csv", item);
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

        /// <summary>
        /// 下架库存不足的商品
        /// </summary>
        /// <param name="obj"></param>
        private void DownNoStockItem(object obj)
        {
            try
            {
                ReadPlatFormWebPageValue platForm = new ReadPlatFormWebPageValue();

                bool isOpt = true;

                while (true)
                {
                    try
                    {
                        Login();
                        Dictionary<string, BaseItemInfo> sellingDic = GetSellingItem();
                        int totalCount = sellingDic.Count;
                        int curCount = 0;
                        foreach (BaseItemInfo item in sellingDic.Values)
                        {
                            Console.WriteLine("DownNoStockItem totalCount:{0}, curCount:{1}", totalCount, ++curCount);

                            if (!IsEnoughStock(item, platForm))
                            {
                                DownItem(item, isOpt);
                                CommonFun.WriteCSV(fileName + "NoStock" + ticks + fileExtendName, item);
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

        private bool IsEnoughStock(BaseItemInfo item, ReadPlatFormWebPageValue platForm)
        {
            try
            {
                string key = item.Name + item.Format + item.Created;

                //不参加对比
                if (unUpdate.ContainsKey(key))
                {
                    return true;
                }

                string seachInfo = item.ID;

                //查找该商品
                if (!seachedItemID.ContainsKey(seachInfo))
                {
                    seachedItemID.Add(seachInfo, item.Name);

                    List<BaseItemInfo> item_list = platForm.SeachInfoByID(seachInfo, 10);

                    if (item_list.Count > 0)
                    {
                        //seachItemList[seachInfo] = item_list;
                        seachItemList.Add(seachInfo, item_list);
                    }
                    else
                    {
                        Console.WriteLine("not seach items id:{0}", seachInfo);
                    }
                }

                if (seachItemList.ContainsKey(seachInfo))
                {
                    List<BaseItemInfo> compareItems = seachItemList[seachInfo];

                    foreach (BaseItemInfo compareItem in compareItems)
                    {
                        if (CommonFun.IsSameFormat(item.Format, compareItem.Format, item.Name, compareItem.Name))
                        {
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return true;
            }
            return false;
        }



        /// <summary>
        /// 更新价格和下架没有库存的商品
        /// </summary>
        public void UpdatePriceAndDown()
        {
            try
            {
                Login();

                ThreadPool.QueueUserWorkItem(UpdateSellingItemPrice);

                ThreadPool.QueueUserWorkItem(DownNoStockItem);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }


        /// <summary>
        /// 获取平台最低价
        /// </summary>
        /// <param name="id"></param>
        /// <param name="format"></param>
        /// <param name="http"></param>
        /// <returns></returns>
        public decimal GetTKMinPrice(string id, string format, bool isSameOther = false, bool getOur = false)
        {
            try
            {
                decimal price = decimal.MaxValue;

                decimal otherPrice = decimal.MaxValue;

                int changeNum = 0;

                string url = string.Format("https://www.tkyfw.com/Ching_search.html?keyword={0}", id);

                string content = request.HttpGet(url);

                content = CommonFun.GetValue(content, "<ul class=\"seach_list\">", "</ul>");

                MatchCollection ms = CommonFun.GetValues(content, "<li>", "</li>");

                foreach (Match m in ms)
                {
                    string item_format = CommonFun.GetValue(m.Value, "规格：", "</p>");

                    if (item_format == format)
                    {
                        MatchCollection urlMs = CommonFun.GetValues(m.Value, "<a href=\"", "\"");

                        content = request.HttpGet("https://www.tkyfw.com" + urlMs[0].Value);

                        MatchCollection itemMs = CommonFun.GetValues(content, "<div class=\"merinfo\">", "</label>");

                        foreach (Match itemM in itemMs)
                        {
                            string priceStr = CommonFun.GetValue(itemM.Value, "￥", "</p>");
                            string shopInfo = CommonFun.GetValue(itemM.Value, "市<span>-</span>", "</a>");
                            try
                            {
                                if (!string.IsNullOrEmpty(priceStr))
                                {
                                    decimal temp = Convert.ToDecimal(priceStr);
                                    if (temp != price)
                                    {
                                        changeNum++;
                                    }
                                    if (isSameOther && shopInfo == other_store_name)
                                    {
                                        otherPrice = temp;
                                        break;
                                    }
                                    else if (getOur || shopInfo != store_name)
                                    {
                                        if (temp < price)
                                        {
                                            price = temp;
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex);
                            }
                        }

                        break;
                    }
                }

                if (otherPrice != decimal.MaxValue)
                {
                    return otherPrice;
                }
                else
                {
                    //if (changeNum == 1)
                    //{
                    //    return price;
                    //}

                    if (price != decimal.MaxValue)
                    {
                        return (price - lPrice);
                    }
                    else
                    {
                        return price;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return decimal.MaxValue;

            //return otherPrice != decimal.MaxValue ? otherPrice : (price - (decimal)0.01);
        }


        /// <summary>
        /// 获取订单详情
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        private BaseItemInfo GetDetailInfo(string info)
        {
            try
            {
                OrderItemInfo item = new OrderItemInfo();

                item.GetOrderTime = CommonFun.GetValue(info, "下单时间：", "<");

                string addrInfo = CommonFun.GetValue(info, "<b>收货信息：</b>", "<div class=\"clear\"></div>");
                MatchCollection ms = CommonFun.GetValues(addrInfo, "<span>", "</span>");
                item.UserName = ms[0].Value;
                item.PhoneNO = ms[1].Value;
                item.Address = ms[2].Value;

                string itemInfo = CommonFun.GetValue(info, "<div class=\"tkddqxqdname\">", "</tbody>");
                item.Name = CommonFun.GetValue(itemInfo, ">", "</a>");
                item.Created = CommonFun.GetValue(itemInfo, "</span>", "</p>");

                MatchCollection iMs = CommonFun.GetValues(itemInfo, "<td>", "</td>");
                item.ID = iMs[0].Value;
                item.Format = iMs[1].Value;
                item.Price = iMs[2].Value;
                item.Count = iMs[3].Value;

                return item;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return null;
        }

        private void AddItemListToShopList(Dictionary<string, BaseItemInfo> items, bool isGetPrice)
        {
            try
            {
                foreach (BaseItemInfo item in items.Values)
                {
                    string key = item.ID + "{" + item.Format + "}";
                    if (!ShopAllItems.ContainsKey(key))
                    {
                        if (isGetPrice)
                        {
                            decimal price = GetTKMinPrice(item.ID, item.Format, false, true);

                            price = (price == decimal.MaxValue ? 0 : price);

                            item.ShopPrice = price;
                        }

                        ShopAllItems.Add(key, item);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public override void Start()
        {
            try
            {
                while (true)
                {
                    ticks = DateTime.Now.Ticks;

                    bool opt = true;

                    Login();

                    //上架固定表
                    UpdateFixedItem(opt);

                    //读取在售列表
                    sellItems = GetSellingItem();
                    //读取下架列表
                    downItems = GetDownListItem();

                    //读取平台的所有数据
                    ReadAllMenuURL();
                    //对比数据
                    ReadPlatFormWebPageValue flatform = new ReadPlatFormWebPageValue();

                    //flatform.Login();
                    //flatform.newPlate();
                    seachedItemID.Clear();

                    seachItemList.Clear();

                    //BaseItemInfo itemT = new BaseItemInfo();
                    //itemT.ID = "国药准字H20140037";
                    //itemT.Name = "恩替卡韦分散片";
                    //itemT.Format = "0.5mg*7片/盒";
                    //itemT.Created = "安徽贝克生物制药有限公司";

                    //ShopAllItems.Add(itemT.ID, itemT);

                    AddItemListToShopList(sellItems, false);

                    AddItemListToShopList(downItems, true);

                    int totalCount = ShopAllItems.Count, curCount = 0;
                    foreach (KeyValuePair<string, BaseItemInfo> info in ShopAllItems)
                    {
                        try
                        {
                            Console.WriteLine("{0},totalCount:{1}, curCount:{2}", DateTime.Now.ToString(), totalCount, ++curCount);

                            if ((DateTime.Now - startTime).TotalMinutes > 30)
                            {
                                Login();
                                flatform.Login();
                                startTime = DateTime.Now;
                            }

                            BaseItemInfo item = info.Value;

                            string key = item.Name + item.Format + item.Created;

                            //禁售直接下架
                            if (!CanLoad(item.Name, item.ID))
                            {
                                if (sellItems.ContainsKey(key))
                                {
                                    sellItems[key].SellType = "禁售直接下架";
                                    DownItem(sellItems[key], opt);
                                    CommonFun.WriteCSV("TK/BlackListDown" + ticks + ".csv", item);
                                }
                                CommonFun.WriteCSV("TK/BlackList" + ticks + ".csv", item);
                                continue;
                            }

                            //不参加对比
                            if (item.Type == yfSellingItemType)
                            {
                                continue;
                            }

                            string seachInfo = item.ID;

                            //查找该商品
                            if (!seachedItemID.ContainsKey(seachInfo))
                            {
                                seachedItemID.Add(seachInfo, item.Name);

                                List<BaseItemInfo> item_list = flatform.SeachInfoByID(seachInfo, 10);

                                if (item_list.Count > 0)
                                {
                                    //seachItemList[seachInfo] = item_list;
                                    seachItemList.Add(seachInfo, item_list);
                                }
                                else
                                {
                                    Console.WriteLine("not seach items id:{0}", seachInfo);
                                }
                            }

                            if (seachItemList.ContainsKey(seachInfo))
                            {
                                List<BaseItemInfo> compareItems = seachItemList[seachInfo];
                                bool isExist = false;
                                foreach (BaseItemInfo compareItem in compareItems)
                                {
                                    if (CommonFun.IsSameFormat(item.Format, compareItem.Format) || CommonFun.IsSameFormat(item.Format, compareItem.Format, item.Name, compareItem.Name))
                                    {
                                        isExist = true;
                                        if (item.ShopPrice == 0)
                                        {
                                            item.ShopPrice = compareItem.ShopSelaPrice * (decimal)2;
                                            if (!sellItems.ContainsKey(key))
                                            {
                                                if (downItems.ContainsKey(key))
                                                {
                                                    ReUpItem(downItems[key], opt);
                                                }
                                                else
                                                {
                                                    UpNewItem(item, opt);
                                                }
                                                sellItems.Add(key, item);
                                            }
                                            UpdatePrice(item);
                                            Thread.Sleep(random.Next(2, 5) * 1000);
                                            break;
                                        }
                                        if (ComparePrice(compareItem, item))
                                        {
                                            //不在在售列表需要上架
                                            if (!sellItems.ContainsKey(key))
                                            {
                                                if (downItems.ContainsKey(key))
                                                {
                                                    //需要重新上架
                                                    ReUpItem(downItems[key], opt);
                                                }
                                                else
                                                {
                                                    //需要新上架
                                                    UpNewItem(item, opt);
                                                }
                                                sellItems.Add(key, item);
                                                UpdatePrice(item);
                                                Thread.Sleep(random.Next(2, 5) * 1000);
                                            }
                                            //else if (sellItems[key].Type == "333")
                                            //{
                                            //    UpdatePrice(item);
                                            //    Thread.Sleep(random.Next(2, 5) * 1000);
                                            //}

                                            break;
                                        }
                                        //else
                                        //{
                                        //    //修改为药房价*2
                                        //    if (!sellItems.ContainsKey(key))
                                        //    {
                                        //        if (downItems.ContainsKey(key))
                                        //        {
                                        //            ReUpItem(downItems[key], opt);
                                        //        }
                                        //        else
                                        //        {
                                        //            UpNewItem(item, opt);
                                        //        }
                                        //    }
                                        //    item.ShopPrice = GetNoProfitItemPrice(compareItem.ShopSelaPrice);
                                        //    item.PlatformPrice = compareItem.ShopSelaPrice;
                                        //    UpdatePrice(item, "333");
                                        //    CommonFun.WriteCSV("TK/UpdateDoublePrice" + ticks + ".csv", item);
                                        //}
                                        ////价格对比
                                        //if (sellItems.ContainsKey(key))
                                        //{
                                        //    if (item.ShopPrice * (decimal)0.9 < compareItem.ShopSelaPrice || string.IsNullOrEmpty(compareItem.Inventory) || Convert.ToInt32(compareItem.Inventory) < 10)
                                        //    {
                                        //        //需要下架
                                        //        sellItems[key].SellType = "价格对比" + DateTime.Now;
                                        //        sellItems[key].ShopSelaPrice = compareItem.ShopSelaPrice;
                                        //        sellItems[key].PlatformPrice = compareItem.ShopPrice;
                                        //        DownItem(sellItems[key], opt);
                                        //        downItems.Add(key, sellItems[key]);
                                        //        Thread.Sleep(random.Next(2, 5) * 1000);
                                        //    }

                                        //}
                                        break;
                                    }
                                }
                                if (!isExist)
                                {
                                    decimal value2 = CommonFun.GetFormatValue(item.Format, item.Name);
                                    foreach (BaseItemInfo compareItem in compareItems)
                                    {
                                        decimal value1 = CommonFun.GetFormatValue(compareItem.Format, compareItem.Name);

                                        if (value1 != 0 && value2 != 0 && value1 == value2)
                                        {
                                            item.PlatformPrice = compareItem.ShopSelaPrice;
                                            CommonFun.WriteCSV("TK/SpcFormat" + ticks + ".csv", item);
                                            isExist = true;
                                        }
                                    }

                                    if (!isExist)
                                    {
                                        seachInfo = item.Name;

                                        //查找该商品
                                        if (!seachedItemID.ContainsKey(seachInfo))
                                        {
                                            seachedItemID.Add(seachInfo, item.Name);

                                            List<BaseItemInfo> item_list = flatform.SeachInfoByID(seachInfo);

                                            seachItemList.Add(seachInfo, item_list);
                                        }

                                        if (seachItemList.ContainsKey(seachInfo))
                                        {
                                            compareItems = seachItemList[seachInfo];
                                            value2 = CommonFun.GetFormatValue(item.Format, item.Name);
                                            foreach (BaseItemInfo compareItem in compareItems)
                                            {
                                                decimal value1 = CommonFun.GetFormatValue(compareItem.Format, compareItem.Name);

                                                if (value1 != 0 && value2 != 0 && value1 == value2)
                                                {
                                                    item.PlatformPrice = compareItem.ShopSelaPrice;
                                                    CommonFun.WriteCSV("TK/SpcFormat" + ticks + ".csv", item);
                                                }
                                            }
                                        }
                                    }

                                    CommonFun.WriteCSV("TK/NoFormat" + ticks + ".csv", item);
                                    if (sellItems.ContainsKey(key))
                                    {
                                        if (seachItemList.ContainsKey(item.ID))
                                        {
                                            List<BaseItemInfo> temp = seachItemList[item.ID];
                                        }
                                        sellItems[key].SellType = "没有对应规格" + DateTime.Now;
                                        DownItem(sellItems[key], opt);
                                        downItems.Add(key, sellItems[key]);
                                        sellItems.Remove(key);
                                        CommonFun.WriteCSV("TK/NoFormatDown" + ticks + ".csv", item);
                                    }
                                }
                            }
                            else
                            {
                                CommonFun.WriteCSV("TK/NotExist" + ticks + ".csv", item);
                                if (sellItems.ContainsKey(key))
                                {
                                    sellItems[key].SellType = "没有对应商品" + DateTime.Now;
                                    DownItem(sellItems[key], opt);
                                    downItems.Add(key, sellItems[key]);
                                    sellItems.Remove(key);
                                    CommonFun.WriteCSV("TK/NotExistDown" + ticks + ".csv", item);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                        }
                    }


                    Console.WriteLine("{0}Restart______________________________________________________", DateTime.Now.ToString());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public void DownNoMoneyItem()
        {
            Login();
            //获取在售列表
            Dictionary<string, BaseItemInfo> selling_list = GetSellingItem();
            //对比药房价格
            ReadPlatFormWebPageValue flatform = new ReadPlatFormWebPageValue();

            flatform.Login();

            Dictionary<string, BaseItemInfo> platItems = new Dictionary<string, BaseItemInfo>();

            Dictionary<string, List<BaseItemInfo>> seachItems = new Dictionary<string, List<BaseItemInfo>>();

            Dictionary<string, string> seachedItemName = new Dictionary<string, string>();

            foreach (BaseItemInfo item in selling_list.Values)
            {
                try
                {
                    string key = item.Name + item.Format + item.Created;

                    //查找该商品
                    if (!seachedItemName.ContainsKey(item.ID))
                    {
                        seachedItemName.Add(item.ID, item.Name);

                        List<BaseItemInfo> item_list = flatform.SeachInfoByID(item.ID);

                        Dictionary<string, BaseItemInfo> minPricItems = new Dictionary<string, BaseItemInfo>();


                        foreach (BaseItemInfo sItem in item_list)
                        {
                            string sItemKey = sItem.ID + sItem.Format + sItem.Created;

                            if (!platItems.ContainsKey(sItemKey))
                            {
                                platItems.Add(sItemKey, sItem);
                                minPricItems.Add(sItemKey, sItem);
                            }
                            else if (platItems[sItemKey].ShopSelaPrice > sItem.ShopSelaPrice)
                            {
                                platItems[sItemKey] = sItem;
                                minPricItems[sItemKey] = sItem;
                            }
                        }

                        seachItems.Add(item.ID, minPricItems.Values.ToList());
                    }

                    if (seachItems.ContainsKey(item.ID))
                    {
                        List<BaseItemInfo> compareItems = seachItems[item.ID];
                        bool isExist = false;
                        foreach (BaseItemInfo compareItem in compareItems)
                        {
                            if (CommonFun.IsSameFormat(item.Format, compareItem.Format, item.Name, compareItem.Name))
                            {
                                isExist = true;
                                //价格对比
                                if (item.PlatformPrice > 0 && (item.PlatformPrice * (decimal)0.9 < compareItem.ShopSelaPrice || string.IsNullOrEmpty(compareItem.Inventory) || Convert.ToInt32(compareItem.Inventory) < 5))
                                {
                                    DownItem(item);
                                }
                                break;
                            }
                        }
                        if (!isExist)
                        {
                            CommonFun.WriteCSV("TK/NoFormat" + ticks + ".csv", item);
                        }

                    }
                    else
                    {
                        CommonFun.WriteCSV("TK/NotExist" + ticks + ".csv", item);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }

            }
        }

        /// <summary>
        /// 下架在售物品
        /// </summary>
        /// <param name="item"></param>
        public void DownItem(BaseItemInfo item, bool isOpt = false)
        {
            if ((DateTime.Now - lastDown).TotalMinutes < 1)
            {
                if (++oneMinDownCount > 10)
                {
                    Console.WriteLine("{0}  down item too many stop!!!!!!!!!!!!", DateTime.Now);

                    Thread.Sleep(120 * 60 * 1000);

                    Console.WriteLine("{0}  down item too many start!!!!!!!!!!!!", DateTime.Now);
                }
            }
            else
            {
                lastDown = DateTime.Now;
                oneMinDownCount = 0;
            }
            if (isOpt)
            {
                string downUrl = "https://admin.tkyfw.com/Goods/edit_dowmGoods";
                string postData = "storeid={0}&off_the_shelf=";
                request.HttpPost(downUrl, string.Format(postData, item.ViewCount));
            }

            CommonFun.WriteCSV("TK/down" + ticks + ".csv", item);
        }

        public void UpNewByCSV()
        {
            Login();

            ReadBaseItemInfo("TK/new636529896435922992.csv", true);

            foreach (KeyValuePair<string, BaseItemInfo> item in ShopAllItems)
            {
                if (UpNewItem(item.Value))
                {
                    CommonFun.WriteCSV("TK/new" + ticks + ".csv", item.Value);
                    Thread.Sleep(random.Next(3, 5) * 1000);
                }
                else
                {
                    CommonFun.WriteCSV("TK/newFailed" + ticks + ".csv", item.Value);
                }
            }
        }

        public void UpAllItem()
        {
            Login();
            //1、获取所有数据对比数据
            ReadBaseItemInfo("TK/30-50636525934878630989.csv", true);
            ReadBaseItemInfo("TK/15-30636525934878630989.csv", true);
            ReadBaseItemInfo("TK/15以下636525934878630989.csv", true);
            ReadBaseItemInfo("TK/50以上636525934878630989.csv", true);
            int count = ShopAllItems.Count;
            //2、获取在售列表
            Dictionary<string, BaseItemInfo> sellItems = GetSellingItem();
            //3、获取下架列表
            Dictionary<string, BaseItemInfo> downItems = GetDownListItem();
            //4、比对是否在在售列表或下架列表
            try
            {
                foreach (KeyValuePair<string, BaseItemInfo> item in ShopAllItems)
                {
                    if (sellItems.ContainsKey(item.Key))
                    {
                        CommonFun.WriteCSV("TK/selling" + ticks + ".csv", item.Value);
                    }
                    //重新上架
                    else if (downItems.ContainsKey(item.Key))
                    {
                        if (ReUpItem(downItems[item.Key]))
                        {
                            CommonFun.WriteCSV("TK/reUp" + ticks + ".csv", item.Value);
                            Thread.Sleep(random.Next(3, 5) * 1000);
                        }
                        else
                        {
                            CommonFun.WriteCSV("TK/reUpFailed" + ticks + ".csv", item.Value);
                        }
                    }
                    //新品
                    else
                    {
                        if (UpNewItem(item.Value))
                        {
                            CommonFun.WriteCSV("TK/new" + ticks + ".csv", item.Value);
                            Thread.Sleep(random.Next(3, 5) * 1000);
                        }
                        else
                        {
                            CommonFun.WriteCSV("TK/newFailed" + ticks + ".csv", item.Value);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public void ReUpItems(string fileName)
        {
            try
            {
                Login();

                ReadBaseItemInfo(fileName, true);
                int totalCount = ShopAllItems.Count;
                int curCount = 0;
                foreach (BaseItemInfo item in ShopAllItems.Values)
                {
                    Console.WriteLine("TotalCount:{0} CurCount:{1}", totalCount, ++curCount);
                    ReUpItem(item, true);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        /// <summary>
        /// 从下架列表中重新上架
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool ReUpItem(BaseItemInfo item, bool isOpt = false)
        {
            if (item.PlatformPrice > 5)
            {
                if (isOpt)
                {
                    item.ViewCount = GetDownItemStockId(item);

                    if (!string.IsNullOrEmpty(item.ViewCount))
                    {
                        string url = "https://admin.tkyfw.com/Goods/edit_upGoods";

                        string postStr = string.Format("storeid[]={0}", item.ViewCount);

                        string result = request.HttpPost(url, postStr);
                        item.Remark = result;
                        CommonFun.WriteCSV("TK/reup" + ticks + ".csv", item);
                    }
                }
                else
                {
                    CommonFun.WriteCSV("TK/reup" + ticks + ".csv", item);
                }
            }

            return true;
        }

        private int GetStock(decimal price)
        {
            if (price > 100)
            {
                return random.Next(15, 20);
            }
            else
            {
                return random.Next(30, 40);
            }
        }

        /// <summary>
        /// 上架新品
        /// </summary>
        /// <param name="item"></param>
        public bool UpNewItem(BaseItemInfo item, bool isOpt = false)
        {
            if (item.ShopPrice < 5)
            {
                return false;
            }

            if (isOpt)
            {
                //find
                string findUrl = "https://admin.tkyfw.com/Goods/findExp";
                string findPostData = "store_id=34";
                request.HttpPost(findUrl, findPostData);

                //findApp
                string findAppUrl = "https://admin.tkyfw.com/Goods/findAppNum";
                string findAppPostData = string.Format("appNum:{0}", item.ID);
                request.HttpPost(findAppUrl, findAppPostData);

                string url = string.Format("https://admin.tkyfw.com/Goods/complateInfo?cat_first=&cat_second=&cat_third=&approval_number={0}", CommonFun.GetUrlEncode(item.ID));

                string upNewItemUrl = "https://admin.tkyfw.com/Goods/findRepeatCheck";

                string checkUrl = "https://admin.tkyfw.com/Goods/checkGoods";

                string content = request.HttpGet(url);

                string postDataStr = "cat_first=&cat_second=&cat_third=&approval_number={0}&check[]={1}";

                string param = "&item[{0}][mall_price]={3}&item[{1}][stock]={4}&item[{2}][business_item]=";

                content = CommonFun.GetValue(content, "<div class=\"col-md-12 tkaddgxscbox\">", "<div class=\"tkaddgxscbtn\">");

                MatchCollection vMs = CommonFun.GetValues(content, "div class=\"tkaddgxsclibox\">", "<div class=\"clearfix\">");

                foreach (Match m in vMs)
                {
                    string format = CommonFun.GetValue(m.Value, "<span class=\"red\">", "</span>");

                    if (CommonFun.IsSameFormat(item.Format, format))
                    {
                        string value = CommonFun.GetValue(m.Value, "value=\"", "\"");

                        if (!string.IsNullOrEmpty(value))
                        {
                            string result = request.HttpPost(upNewItemUrl, string.Format("value_arr={0}{1}", value, HttpUtility.UrlEncode(",")));

                            result = CommonFun.GetValue(result, ":", "}");

                            if (result == "2")
                            {
                                string paramStr = "";

                                for (int i = 0; i < vMs.Count; i++)
                                {
                                    string idStr = CommonFun.GetValue(vMs[i].Value, "value=\"", "\"");
                                    paramStr += string.Format(param, idStr, idStr, idStr, item.ShopPrice, item.Inventory);
                                }

                                request.HttpPost(checkUrl, string.Format(postDataStr, HttpUtility.UrlEncode(item.ID).ToUpper(), value) + paramStr, url, "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8", null);
                                Console.WriteLine("UpNewItem,name:{0}, id:{1}, fromat:{2}", item.Name, item.ID, item.Format);
                                // UpdatePrice(item, http);
                                CommonFun.WriteCSV("TK/upNew" + ticks + ".csv", item);
                                return true;
                            }
                        }
                        break;
                    }
                }
            }

            CommonFun.WriteCSV("TK/upNewFailed" + ticks + ".csv", item);
            return false;
        }

        public List<BaseItemInfo> GetOnePageItems(string itemStr)
        {
            List<BaseItemInfo> items = new List<BaseItemInfo>();
            itemStr = itemStr.Replace('\n', ' ');
            itemStr = itemStr.Replace('\t', ' ');
            MatchCollection ms = CommonFun.GetValues(itemStr, "<div class=\"tkprolistbox\">", "修改");

            foreach (Match m in ms)
            {
                ItemInfo item = GetOneItem(m.Value);

                items.Add(item);
            }

            return items;
        }

        //<div class="span2" id="mall_price67369">20.99</div> 
        //<div class="span2" id="market_price67369">21.00</div> 
        //<div class="span1 money-box throughtmoney">
        //<div class="money" id="stock67369">115</div>
        //<div><span>商品规格：</span>3g*15袋/盒</div>
        //<div><span>批准文号：</span>国药准字H19990307</div>
        //<div><span>生产厂家：</span>先声药业有限公司</div>
        public ItemInfo GetOneItem(string itemStr)
        {
            ItemInfo item = new ItemInfo();
            item.Name = CommonFun.GetValue(itemStr, ";&nbsp;", "</a>");
            item.ID = CommonFun.GetValue(itemStr, "<span>批准文号：</span>", "</p>");
            item.Format = CommonFun.GetValue(itemStr, "<span>商品规格：</span>", "</p>");
            item.Created = CommonFun.GetValue(itemStr, "<span>生产企业：</span>", "</p>");
            item.ViewCount = CommonFun.GetValue(itemStr, "phoneStoreId=\"", "\"");
            item.Inventory = CommonFun.GetValue(itemStr, "stockEd=\"", "\"");
            item.Type = CommonFun.GetValue(itemStr, "promotionEd=\"", "\"");
            string priceStr = CommonFun.GetValue(itemStr, "priceEd=\"", "\"");
            item.PlatformPrice = Convert.ToDecimal(priceStr);

            return item;
        }

        /// <summary>
        /// 获取在售列表
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, BaseItemInfo> GetSellingItem(bool isUseId = false)
        {
            Dictionary<string, BaseItemInfo> items = new Dictionary<string, BaseItemInfo>();

            string url = "https://admin.tkyfw.com/Goods/throughAudit?/Goods/throughAudit=&status=1&state=1";

            string content = request.HttpGet(url);

            string itemStr = CommonFun.GetValue(content, "<form name=\"form\" method=\"post\">", "</form>");

            List<BaseItemInfo> temp = new List<BaseItemInfo>();

            string pageStr = CommonFun.GetValue(content, "下一页", "><span>末页</span>");
            pageStr = CommonFun.GetValue(pageStr, "&p=", "\"");
            pageStr = pageStr.Substring(pageStr.LastIndexOf("=") + 1);

            int totalPage = 0;

            if (!string.IsNullOrEmpty(pageStr))
            {
                totalPage = Convert.ToInt32(pageStr);
            }



            List<string> urls = new List<string>();

            for (int i = 1; i <= totalPage; i++)
            {
                string itemUrl = "https://admin.tkyfw.com/Goods/throughAudit?/Goods/throughAudit=&status=1&state=1&p=" + i;
                urls.Add(itemUrl);
                //content = request.HttpGet("https://admin.tkyfw.com/Goods/throughAudit?/Goods/throughAudit=&status=1&state=1&p=" + i);

                //itemStr = CommonFun.GetValue(content, "<form name=\"form\" method=\"post\">", "</form>");

                //temp.AddRange(GetOnePageItems(itemStr));

                Console.WriteLine("GetSellingItem {0}:GetSelling totalPage:{1}, curPage{2}.....", DateTime.Now, totalPage, i);
            }

            RequestInfo info = new RequestInfo();
            info.RequestParams.Add("requestType", "get");
            info.Urls = urls;
            RequestUrls(info);

            foreach (string value in info.Contentes)
            {
                string str = CommonFun.GetValue(value, "<form name=\"form\" method=\"post\">", "</form>");

                temp.AddRange(GetOnePageItems(str));
            }


            foreach (BaseItemInfo item in temp)
            {
                string key = item.Name + item.Format + item.Created;

                if (isUseId)
                {
                    key = item.ID + item.Format;
                }

                if (!items.ContainsKey(key))
                {
                    items.Add(key, item);
                }
                else
                {
                    Console.WriteLine("Error, Same Itme key:{0}", key);
                }
            }

            return items;
        }

        /// <summary>
        /// 获取下架列表
        /// </summary>
        /// <param name="cookie"></param>
        /// <returns></returns>
        public Dictionary<string, BaseItemInfo> GetDownListItem()
        {
            Dictionary<string, BaseItemInfo> items = new Dictionary<string, BaseItemInfo>();

            string url = "https://admin.tkyfw.com/Goods/throughAudit?/Goods/throughAudit=&status=1&state=2";

            string content = request.HttpGet(url);

            string itemStr = CommonFun.GetValue(content, "<form name=\"form\" method=\"post\">", "</form>");

            string pageStr = CommonFun.GetValue(content, "下一页", "><span>末页</span>");
            pageStr = CommonFun.GetValue(pageStr, "&p=", "\"");
            pageStr = pageStr.Substring(pageStr.LastIndexOf("=") + 1);

            int totalPage = 0;

            if (!string.IsNullOrEmpty(pageStr))
            {
                totalPage = Convert.ToInt32(pageStr);
            }

            List<string> urls = new List<string>();

            List<BaseItemInfo> temp = new List<BaseItemInfo>();

            for (int i = 1; i <= totalPage; i++)
            {
                urls.Add(url + "&p=" + i);
                //content = request.HttpGet(url + "&p=" + i);

                //itemStr = CommonFun.GetValue(content, "<form name=\"form\" method=\"post\">", "</form>");

                //temp.AddRange(GetOnePageItems(itemStr));

                //Console.WriteLine("{2}:totalPage:{0}, curPage:{1}", totalPage, i, DateTime.Now);
            }

            RequestInfo requestInfo = new RequestInfo();
            requestInfo.RequestParams.Add("requestType", "get");
            requestInfo.Urls = urls;

            RequestUrls(requestInfo);

            foreach (string value in requestInfo.Contentes)
            {
                itemStr = CommonFun.GetValue(value, "<form name=\"form\" method=\"post\">", "</form>");

                temp.AddRange(GetOnePageItems(itemStr));
            }

            foreach (BaseItemInfo item in temp)
            {
                string key = item.Name + item.Format + item.Created;

                if (!items.ContainsKey(key))
                {
                    items.Add(key, item);
                }
                else
                {
                    Console.WriteLine("Error, Same Itme key:{0}", key);
                }
            }

            return items;

        }
        bool isFirst = true;
        /// <summary>
        /// 开始自动接单
        /// </summary>
        public void StartAutoGetOrder()
        {
            double startStamp = GetNowTimestampS();

            double loginStamp = GetNowTimestampS();

            while (true)
            {
                Console.WriteLine("{0}:GetOrdering...........", DateTime.Now);

                //每半小时重新登录
                if (isFirst || GetNowTimestampS() - startStamp > 1200)
                {
                    Login();
                    loginStamp = GetNowTimestampS();
                }

                //每天更新一次在售列表
                if (isFirst || GetNowTimestampS() - startStamp > 24 * 3600)
                {
                    items = GetSellingItem(true);
                    startStamp = GetNowTimestampS();
                }

                isFirst = false;

                AutoGetOrder();

                AutoPass();

                Thread.Sleep(1000);
            }
        }


        public void AutoPass()
        {
            try
            {
                string url = "https://admin.tkyfw.com/Order/allOrderList?pay=1";

                string passUrl = "https://admin.tkyfw.com/Order/AjaxEditDemand";

                string nextPage = "";
                do
                {
                    url = string.IsNullOrEmpty(nextPage) ? url : ("https://admin.tkyfw.com/Order/allOrderList" + nextPage);
                    string content = request.HttpGet(url);

                    nextPage = CommonFun.GetValue(content, "上一页", "</div>");
                    nextPage = string.IsNullOrEmpty(nextPage) ? content : nextPage;
                    nextPage = CommonFun.GetValue(nextPage, "<a class=\"next\" href=\"", "\">下一页");

                    MatchCollection ms = CommonFun.GetValues(content, "<br><a href=\"", "\" target=\"_blank\">订单详情</a>");

                    foreach (Match m in ms)
                    {
                        string itemUrl = "https://admin.tkyfw.com" + m.Value;

                        content = request.HttpGet(itemUrl);

                        if (content.Contains(">通过<"))
                        {
                            string result = request.HttpPost(passUrl, string.Format("rx_id={0}&rx_state=1", CommonFun.GetNum(m.Value)));
                            if (CommonFun.GetValue(result, "\"status\":", ",") == "1")
                            {
                                Console.WriteLine("{0} pass rx_id suc:{1}", DateTime.Now, CommonFun.GetNum(m.Value));
                            }
                            else
                            {
                                Console.WriteLine("{0} pass rx_id failed:{1} result:{2}", DateTime.Now, CommonFun.GetNum(m.Value), result);
                            }
                        }
                    }
                }
                while (!string.IsNullOrEmpty(nextPage));

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public double GetNowTimestampS()
        {
            TimeSpan ts = DateTime.Now.ToUniversalTime() - new DateTime(1970, 1, 1);

            return ts.TotalSeconds;
        }
        /// <summary>
        /// 自动接单
        /// </summary>
        /// <returns></returns>
        public bool AutoGetOrder()
        {
            //获取订单
            string content = GetOrderList();

            MatchCollection ms = CommonFun.GetValues(content, "<tbody>", "</tbody>");

            foreach (Match m in ms)
            {
                ProcessOrder(m.Value);
            }

            return true;
        }

        /// <summary>
        /// 获取订单列表
        /// </summary>
        /// <returns></returns>
        public string GetOrderList()
        {
            string url = "https://admin.tkyfw.com/Order/turn_out_pool";

            string content = request.HttpGet(url);

            return content;
        }

        /// <summary>
        /// 接单处理
        /// </summary>
        /// <param name="item"></param>
        /// <param name="http"></param>
        public void ProcessOrder(string item)
        {
            if (CanTake(item))
            {
                string id = CommonFun.GetValue(item, "rel=\"", "\"");

                if (!orderList.ContainsKey(id) && TakeOrder(id))
                {
                    orderList.Add(id, id);
                    s.Play();
                    Console.WriteLine("Take Order !!!!!!!!!!!!!!!!!!");
                }
            }
        }

        /// <summary>
        /// 更新备注状态
        /// </summary>
        /// <param name="state"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        private bool UpdateRemartState(int state, string id)
        {
            try
            {
                string url = "https://admin.tkyfw.com/Order/ajaxSaveOrder";

                string postData = string.Format("id={0}&order_remarks=+&rem={1}", id, state);

                string content = request.HttpPost(url, postData);

                if (content == "1")
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

        /// <summary>
        /// 填写快递单
        /// </summary>
        /// <returns></returns>
        private bool WriteExpressOrder(string id, string orderNO = "700444412992")
        {
            try
            {
                //TK201805301118314753
                string url = "https://admin.tkyfw.com/Order/SetDeliveryAction";
                string postData = string.Format("id={0}&express_name=%E5%9C%86%E9%80%9A%E5%BF%AB%E9%80%92&number_logistics={1}", id, orderNO);

                string content = request.HttpPost(url, postData);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return false;
        }

        public void ProcessOrder()
        {
            try
            {
                Login();

                TagOutTimeItem();

                GetLogisticsInfo();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
        /// <summary>
        /// 标记超时状态（45小时）
        /// </summary>
        public void TagOutTimeItem()
        {
            try
            {
                string allItemUrl = "https://admin.tkyfw.com/Order/allOrderList?pay=1&p={0}";

                int page = 1;

                int totalPage = 0;

                do
                {
                    string content = request.HttpGet(allItemUrl);

                    if (!string.IsNullOrEmpty(content))
                    {
                        if (totalPage == 0)
                        {
                            string totalPageStr = CommonFun.GetValue(content, "共有", "条数据");

                            totalPage = Convert.ToInt32(totalPageStr) / 20;

                            totalPage += (Convert.ToInt32(totalPageStr) % 20 == 0 ? 0 : 1);
                        }

                        Console.WriteLine("Getting...... totalPage:{0},curPage{1}", totalPage, page);

                        MatchCollection ms = CommonFun.GetValues(content, "<div class=\"tkddbox\">", "取消订单</a>");

                        foreach (Match m in ms)
                        {
                            BaseItemInfo item = GetOrderItem(m.Value);

                            if (item != null && !string.IsNullOrEmpty(item.ItemName))
                            {
                                DateTime payTime = DateTime.ParseExact(item.ItemName, "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.CurrentCulture);
                                //超过45小时设置为发货
                                if ((DateTime.Now - payTime).TotalHours > 45)
                                {
                                    if (UpdateRemartState(2, item.ViewCount))
                                    {
                                        WriteExpressOrder(item.ViewCount);
                                        CommonFun.WriteCSV("TK/TagOutTime" + ticks + "csv", item);
                                    }
                                }
                            }

                        }
                    }
                } while (++page <= totalPage);

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private BaseItemInfo GetOrderItem(string content)
        {
            try
            {
                BaseItemInfo item = new BaseItemInfo();
                string info = CommonFun.GetValue(content, "<div class=\"tkddtop\">", "</div>");
                item.ID = CommonFun.GetValue(info, "订单编号：", "</span>");
                item.Name = CommonFun.GetValue(info, "下单时间：", "</span>");
                item.ItemName = CommonFun.GetValue(info, "付款时间：", "</span>").Trim();
                item.ViewCount = CommonFun.GetValue(content, "class=\"findOrderId enterOrderex\" rel=\"", "\"");
                return item;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            return null;
        }

        /// <summary>
        /// 获取物流信息
        /// </summary>
        public void GetLogisticsInfo()
        {
            try
            {
                int page = 1;

                int totalPage = 0;

                string allItemUrl = "https://admin.tkyfw.com/Order/allOrderList?pay=2&p={0}";

                string logisticsUrl = "https://admin.tkyfw.com/Order/findLogistics";

                string orderUrl = "https://admin.tkyfw.com/Order/findOrder";

                string logisticsData = "id={0}";

                string orderData = "id={0}";

                do
                {
                    //1、获取所有已发货订单
                    string content = request.HttpGet(string.Format(allItemUrl, page));

                    if (!string.IsNullOrEmpty(content))
                    {
                        if (totalPage == 0)
                        {
                            string totalPageStr = CommonFun.GetValue(content, "共有", "条数据");

                            totalPage = Convert.ToInt32(totalPageStr) / 20;

                            totalPage += (Convert.ToInt32(totalPageStr) % 20 == 0 ? 0 : 1);
                        }

                        Console.WriteLine("Getting...... totalPage:{0},curPage{1}", totalPage, page);

                        MatchCollection ms = CommonFun.GetValues(content, "<div class=\"tkddbox\">", "物流跟踪</a>");

                        foreach (Match m in ms)
                        {
                            //状态红色
                            if (m.Value.Contains("bgicon_2.png"))
                            {
                                //2、是否有物流信息
                                string id = CommonFun.GetValue(m.Value, "findlogistics\" rel=\"", "\"");
                                string logisticsInfo = request.HttpPost(logisticsUrl, string.Format(logisticsData, id));

                                BaseItemInfo item = new BaseItemInfo();
                                string info = CommonFun.GetValue(m.Value, "<div class=\"tkddtop\">", "</div>");
                                item.ID = CommonFun.GetValue(info, "订单编号：", "</span>");
                                item.Name = CommonFun.GetValue(info, "下单时间：", "</span>");
                                item.ItemName = CommonFun.GetValue(info, "付款时间：", "</span>");
                                item.ViewCount = CommonFun.GetValue(m.Value, "class=\"editRemark\" rel=\"", "\"");
                                //3、提取订单信息
                                if (logisticsInfo == "null")
                                {
                                    //string orderId = CommonFun.GetValue(m.Value, "class=\"editRemark\" rel=\"", "\"");
                                    //string orderInfo = request.HttpPost(orderUrl, string.Format(orderData, orderId));
                                    //string state =CommonFun.GetValue(orderInfo, "", "");

                                    CommonFun.WriteCSV("TK/LogisticsInfo" + ticks + ".csv", item);
                                }
                                else
                                {
                                    //改为绿色状态
                                    UpdateRemartState(6, item.ViewCount);
                                    CommonFun.WriteCSV("TK/LogisticsInfoGreen" + ticks + ".csv", item);
                                }
                            }
                        }

                    }

                } while (++page <= totalPage);
                Console.WriteLine("Get finished..........");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public bool IsSelfOrder(string content)
        {
            if (content.Contains("承接订单"))
            {
                return false;
            }

            return true;

            //string url = string.Format("https://admin.tkyfw.com/Order/turn_out_pool/sta/2.html?p=1&order={0}&goods=&start_time=&end_time=", orderNO);

            //string content = http.HttpGet(url);

            //string countStr = CommonFun.GetValue(content, "共有", "条数据");

            //int count = 0;

            //if (!string.IsNullOrEmpty(countStr))
            //{
            //    count = Convert.ToInt32(countStr);
            //}

            //return count > 0;
        }

        /// <summary>
        /// 是否可以接
        /// </summary>
        /// <returns></returns>
        public bool CanTake(string content)
        {
            string infoStr = CommonFun.GetValue(content, "<tr>", "</tr>");

            MatchCollection tdMs = CommonFun.GetValues(infoStr, "<td>", "</td>");

            string orderNO = CommonFun.GetValue(infoStr, "<td class=\"tablenow\">", "</td>");

            bool result = false;

            MatchCollection ms = CommonFun.GetValues(content, "<div class=\"tkzds\">", "</div>");

            string format = ms[0].Value;
            string id = ms[1].Value;
            string key = id + format;

            //是否TK平台的单
            if (orderNO.StartsWith("TK") && !IsSelfOrder(content))
            {
                string drs = tdMs[5].Value;

                //不是同城
                if (!drs.Contains("长沙"))
                {
                    BaseItemInfo item = items.ContainsKey(key) ? items[key] : null;

                    //在线支付
                    if (infoStr.Contains("在线支付") || (infoStr.Contains("货到付款") && item != null && unUpdate.ContainsKey(item.Name + item.Format + item.Created)))
                    {
                        string msg = CommonFun.GetValue(content, "<li>买家留言:<span>", "</span></li>");
                        string remark = CommonFun.GetValue(content, "<li>平台备注:<span>", "</span></li>");
                        //无需发票
                        if (!msg.Contains("发票") && !remark.Contains("发票"))
                        {
                            //MatchCollection ms = CommonFun.GetValues(content, "<div class=\"tkzds\">", "</div>");
                            //单品
                            if (ms.Count == 5)
                            {
                                //ms = CommonFun.GetValues(content, "span1 xiatiaomar\">", "</div>");
                                decimal price = Convert.ToDecimal(ms[2].Value);

                                //价格合理
                                if (items.ContainsKey(key))
                                {
                                    string totalPriceStr = tdMs[3].Value;
                                    decimal totalPrice = string.IsNullOrEmpty(totalPriceStr) ? 0 : Convert.ToDecimal(totalPriceStr);
                                    if (totalPrice >= 50 && items[key].PlatformPrice <= price)//&& item.Type != "333"
                                    {
                                        result = true;
                                    }
                                    else
                                    {
                                        Console.WriteLine("Price info orderPrice:{0}, sellPrice:{1}, Type:{2}", price, item.PlatformPrice, item.Type);
                                    }
                                }
                            }
                        }

                    }
                }
            }

            return result;
        }

        /// <summary>
        /// 接单
        /// </summary>
        /// <returns></returns>
        public bool TakeOrder(string id)
        {
            string url = "https://admin.tkyfw.com//Order/to_undertake";

            string postStr = string.Format("id={0}&Q=1", id);

            string result = request.HttpPost(url, postStr);

            return true;
        }

    }
}
