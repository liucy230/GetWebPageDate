using GetWebPageDate.Util.Item;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace GetWebPageDate.Util.UpdatePrice
{
    public class TKUpdate
    {
        BaseReadWebPage TKItems = new BaseReadWebPage();

        private static Random random = new Random((int)DateTime.Now.Ticks);

        CookieContainer cookie = new CookieContainer();

        private string login_url = "https://admin.tkyfw.com/Account/loginAction";

        private string store_name = "长沙县001店";

        private string other_store_name = "巨野001店";

        private string pass = "15111193057xh";

        private string user_name = "hankang";

        private long fileName = DateTime.Now.Ticks;

        Dictionary<string, BaseItemInfo> items = new Dictionary<string, BaseItemInfo>();

        Dictionary<string, string> orderList = new Dictionary<string, string>();

        /// <summary>
        /// 获取物品价格
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public decimal GetItemPrice(BaseItemInfo item)
        {
            if (TKItems.ShopAllItems.Count == 0)
            {
                TKItems.ReadBaseItemInfo("TK/TK.csv", true);
            }

            string key = item.ItemName + item.Format + item.Created;

            if (TKItems.ShopAllItems.ContainsKey(key))
            {
                return TKItems.ShopAllItems[key].ShopPrice;
            }

            return item.ShopPrice;
        }

        private static bool CheckValidationResult(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
        {
            return true; //总是接受
        }

        //username:hankang
        //pass:15111193057xh
        //verify:
        //viewlicense:1
        /// <summary>
        /// 登录
        /// </summary>
        /// <returns></returns>
        public CookieContainer Login()
        {
            try
            {
                string postDataStr = string.Format("store_name={0}&username={1}&pass={2}&verify=&viewlicense=1", HttpUtility.UrlEncode(store_name), user_name, pass);

                GetWebPageDate.Http.HttpRequest http = new GetWebPageDate.Http.HttpRequest();

                CookieContainer cookies = new CookieContainer();

                byte[] postData = Encoding.UTF8.GetBytes(postDataStr);

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(login_url);
                ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(CheckValidationResult);
                request.ProtocolVersion = HttpVersion.Version11;
                // 这里设置了协议类型。
                ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;// SecurityProtocolType.Tls1.2; 
                request.Method = "POST";
                request.KeepAlive = false;
                request.Timeout = 1000000;
                request.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
                request.Referer = "https://admin.tkyfw.com/Seller_login.html";
                request.ContentLength = postData.Length;
                request.UserAgent = "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)";
                //cookie.SetCookies(request.RequestUri, "Qs_lvt_150743=1500685870%2C1500777689; NTKF_T2D_CLIENTID=guestBC81D14C-3099-FCD9-FE94-67D92C0B496A; Hm_lvt_ed70f863f631ecaac146592025767ed8=1500685870; Qs_pv_150743=1447053609678541000%2C3007060151905128000%2C355027317986517250%2C1201566385246198500%2C3405144643715530000; Hm_lvt_3ad2c5e8712b25159b989a93a9927632=1500685895; acw_tc=AQAAAIyCqm27gAQABUULt6WUgM5Ca2iH; PHPSESSID=1sighr4l5nqe1dj4536u1mh2v3; __guid=202919361.837275310285311200.1509859920993.7488; renren_tag_0526=isTag; sellerCooke=think%3A%7B%22sellerName%22%3A%22hankang%22%2C%22sellerStoreName%22%3A%22%25E9%2595%25BF%25E6%25B2%2599%25E5%258E%25BF001%25E5%25BA%2597%22%7D; Hm_lvt_7203bc79de07054ef3770e27e8ca9068=1509860118; Hm_lpvt_7203bc79de07054ef3770e27e8ca9068=1509860118; monitor_count=3");
                request.CookieContainer = new CookieContainer();
                Stream myRequestStream = request.GetRequestStream();
                myRequestStream.Write(postData, 0, postData.Length);
                myRequestStream.Close();

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                //response.Cookies = cookie.GetCookies(response.ResponseUri);

                Stream myResponseStream = response.GetResponseStream();
                cookies.Add(response.Cookies);
                StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.GetEncoding("utf-8"));
                string retString = myStreamReader.ReadToEnd();

                myStreamReader.Close();
                myResponseStream.Close();
                return cookies;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return Login();
            }

        }

        /// <summary>
        /// 开始自动接单
        /// </summary>
        public void StartAutoGetOrder()
        {
            //登录
            Http.HttpRequest http = new Http.HttpRequest();

            http.CookieContainer = Login();

            items = GetSellingItem(http);

            double startStamp = GetNowTimestampS();

            double loginStamp = GetNowTimestampS();

            while (true)
            {
                Console.WriteLine("{0}:GetOrdering...........", DateTime.Now);

                AutoGetOrder(http);

                //每天更新一次在售列表
                if (GetNowTimestampS() - startStamp > 24 * 3600)
                {
                    items = GetSellingItem(http);
                    startStamp = GetNowTimestampS();
                }

                //每半小时重新登录
                if (GetNowTimestampS() - startStamp > 1200)
                {
                    http.CookieContainer = Login();
                    loginStamp = GetNowTimestampS();
                }

                Thread.Sleep(1000);
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
        public bool AutoGetOrder(Http.HttpRequest http)
        {
            //获取订单
            string content = GetOrderList(http);

            MatchCollection ms = CommonFun.GetValues(content, "<tbody>", "</tbody>");

            foreach (Match m in ms)
            {
                ProcessOrder(m.Value, http);
            }

            return true;
        }

        /// <summary>
        /// 获取订单列表
        /// </summary>
        /// <returns></returns>
        public string GetOrderList(Http.HttpRequest http)
        {
            string url = "https://admin.tkyfw.com/Order/turn_out_pool";

            string content = http.HttpGet(url);

            return content;
        }

        /// <summary>
        /// 接单处理
        /// </summary>
        /// <param name="item"></param>
        /// <param name="http"></param>
        public void ProcessOrder(string item, Http.HttpRequest http)
        {
            if (CanTake(item, http))
            {
                string id = CommonFun.GetValue(item, "rel=\"", "\"");

                if (!orderList.ContainsKey(id) && TakeOrder(id, http))
                {
                    orderList.Add(id, id);
                    Console.WriteLine("Take Order !!!!!!!!!!!!!!!!!!");
                }
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
        public bool CanTake(string content, Http.HttpRequest http)
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
                    if (infoStr.Contains("在线支付") || (infoStr.Contains("货到付款") && item != null && item.Type == "201"))
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
                                    if (true || items[key].PlatformPrice <= price)
                                    {
                                        result = true;
                                    }
                                    else
                                    {
                                        Console.WriteLine("Price info orderPrice:{0}, sellPrice:{1}", price, items[key].PlatformPrice);
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
        public bool TakeOrder(string id, Http.HttpRequest http)
        {
            string url = "https://admin.tkyfw.com//Order/to_undertake";

            string postStr = string.Format("id={0}&Q=1", id);

            string result = http.HttpPost(url, postStr);

            return true;
        }

        /// <summary>
        /// 本地文件中获取在售列表
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, BaseItemInfo> GetSellingItem()
        {
            Dictionary<string, BaseItemInfo> items = new Dictionary<string, BaseItemInfo>();

            BaseReadWebPage read = new BaseReadWebPage();
            read.ReadBaseItemInfo("TKAdmin/TKAdmin.csv", true);

            foreach (BaseItemInfo item in read.ShopAllItems.Values)
            {
                string key = item.ID + item.Format;
                if (!items.ContainsKey(key))
                {
                    items.Add(key, item);
                }
                else
                {
                    Console.WriteLine("Have same item id:{0},format:{1}", item.ID, item.Format);
                }
            }

            return items;
        }

        /// <summary>
        /// 获取在售列表
        /// </summary>
        /// <param name="http"></param>
        /// <returns></returns>
        public Dictionary<string, BaseItemInfo> GetSellingItem(Http.HttpRequest http)
        {
            Dictionary<string, BaseItemInfo> items = new Dictionary<string, BaseItemInfo>();

            string url = "https://admin.tkyfw.com/Goods/throughAudit?/Goods/throughAudit=&status=1&state=1";

            string content = http.HttpGet(url);

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
                content = http.HttpGet("https://admin.tkyfw.com/Goods/throughAudit?/Goods/throughAudit=&status=1&state=1&p=" + i);

                itemStr = CommonFun.GetValue(content, "<form name=\"form\" method=\"post\">", "</form>");

                temp.AddRange(GetOnePageItems(itemStr));

                Console.WriteLine("{0}:GetSelling totalPage:{1}, curPage{2}.....", DateTime.Now, totalPage, i);
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

        /// <summary>
        /// 获取一页的所有物品
        /// </summary>
        /// <param name="itemStr"></param>
        /// <returns></returns>
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

        /// <summary>
        /// 获取所有在售物品
        /// </summary>
        /// <returns></returns>
        public bool ReadeAllSellingItme()
        {
            GetWebPageDate.Http.HttpRequest http = new GetWebPageDate.Http.HttpRequest();

            http.CookieContainer = Login();

            // http.Cookie = "acw_tc=AQAAAMj+8lkEeQUA3eT4OlKCFpqJmk95; Hm_lvt_3ad2c5e8712b25159b989a93a9927632=1510644101; Hm_lpvt_3ad2c5e8712b25159b989a93a9927632=1510645266; PHPSESSID=dem5mqnu6bfibviglvjh9d4ud1; Qs_lvt_150743=1509785002%2C1510643909%2C1510747212; nTalk_CACHE_DATA={uid:kf_9335_ISME9754_guestTEMP1758-0E75-6A,tid:1510643909116098}; NTKF_T2D_CLIENTID=guestTEMP1758-0E75-6A74-9EB9-8632DF7CEB19; Qs_pv_150743=2882295116553261000%2C1172614365052146400%2C979891720064693100%2C1505959923634373400%2C662315226244183900; Hm_lvt_ed70f863f631ecaac146592025767ed8=1509785002,1510643909; Hm_lpvt_ed70f863f631ecaac146592025767ed8=1510747235; sellerCooke=think%3A%7B%22sellerName%22%3A%22hankang%22%2C%22sellerStoreName%22%3A%22%25E9%2595%25BF%25E6%25B2%2599%25E5%258E%25BF001%25E5%25BA%2597%22%7D; Hm_lvt_7203bc79de07054ef3770e27e8ca9068=1509780503,1509934350,1510060776,1510543588; Hm_lpvt_7203bc79de07054ef3770e27e8ca9068=1510747284";

            string content = http.HttpGet("https://admin.tkyfw.com/Goods/throughAudit?/Goods/throughAudit=&status=1&state=1");

            string itemStr = CommonFun.GetValue(content, "<form name=\"form\" method=\"post\">", "</form>");

            UpdateAllSellingItemPrice(itemStr, http);

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
                if (i % 200 == 0)
                {
                    http.CookieContainer = Login();
                }

                content = http.HttpGet("https://admin.tkyfw.com/Goods/throughAudit?/Goods/throughAudit=&status=1&state=1&p=" + i);

                itemStr = CommonFun.GetValue(content, "<form name=\"form\" method=\"post\">", "</form>");

                UpdateAllSellingItemPrice(itemStr, http);

                Console.WriteLine("{2}:totalPage:{0}, curPage:{1}", totalPage, i, DateTime.Now);
            }

            return true;
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
            item.ViewCount = CommonFun.GetValue(itemStr, "value=\"", "\">");
            item.Inventory = CommonFun.GetValue(itemStr, "stockEd=\"", "\"");
            item.Type = CommonFun.GetValue(itemStr, "promotionEd=\"", "\"");
            string priceStr = CommonFun.GetValue(itemStr, "priceEd=\"", "\"");
            item.PlatformPrice = Convert.ToDecimal(priceStr);

            return item;
        }

        /// <summary>
        /// 更新价格
        /// </summary>
        /// <param name="item"></param>
        /// <param name="http"></param>
        /// <returns></returns>
        public string UpdatePrice(GetWebPageDate.Http.HttpRequest http, decimal price, string storeid, string tag, string stock)
        {
            string param = "business_item=&course=&mall_price={3}&market_price={0}&open_close=2&promotion={1}&storeid={2}&stock={4}";

            string inventory = !string.IsNullOrEmpty(tag) && tag == "201" ? stock : random.Next(30, 40).ToString();

            string postStr = string.Format(param, price, tag, storeid, price, inventory);

            string updateUrl = "https://admin.tkyfw.com/Goods/do_editGoods";

            string info = http.HttpPost(updateUrl, postStr);

            return info;
        }

        /// <summary>
        /// 更新库存
        /// </summary>
        /// <returns></returns>
        public bool UpdateInventory()
        {
            Http.HttpRequest http = new Http.HttpRequest();
            http.CookieContainer = Login();

            items = GetSellingItem(http);

            foreach (BaseItemInfo item in items.Values)
            {
                if (!string.IsNullOrEmpty(item.Inventory) && Convert.ToInt32(item.Inventory) >= 50)
                {
                    UpdatePrice(http, item.PlatformPrice, item.ViewCount, null, null);
                    Sleep();
                }
            }

            return true;
        }

        private void Sleep()
        {
            Thread.Sleep(random.Next(3, 6) * 1000);
        }

        /// <summary>
        /// 获取平台最低价
        /// </summary>
        /// <param name="id"></param>
        /// <param name="format"></param>
        /// <param name="http"></param>
        /// <returns></returns>
        public decimal GetPlatformPrice(string id, string format, Http.HttpRequest http, bool isSameOther = false)
        {
            try
            {
                decimal price = decimal.MaxValue;

                decimal otherPrice = decimal.MaxValue;

                int changeNum = 0;

                string url = string.Format("https://www.tkyfw.com/Ching_search.html?keyword={0}", id);

                string content = http.HttpGet(url);

                content = CommonFun.GetValue(content, "<ul class=\"seach_list\">", "</ul>");

                MatchCollection ms = CommonFun.GetValues(content, "<li>", "</li>");

                foreach (Match m in ms)
                {
                    string item_format = CommonFun.GetValue(m.Value, "规格：", "</p>");

                    if (item_format == format)
                    {
                        MatchCollection urlMs = CommonFun.GetValues(m.Value, "<a href=\"", "\"");

                        content = http.HttpGet("https://www.tkyfw.com" + urlMs[0].Value);

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
                                    else if (shopInfo != store_name)
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
                    return price;
                    //if (price != decimal.MaxValue)
                    //{
                    //    return (price - (decimal)0.01);
                    //}
                    //else
                    //{
                    //    return price;
                    //}
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
        /// 修改所有在售物品的价格
        /// </summary>
        /// <returns></returns>
        public bool UpdateAllSellingItemPrice(string itemStr, GetWebPageDate.Http.HttpRequest http, decimal upParam = 1)
        {
            itemStr = itemStr.Replace('\n', ' ');
            itemStr = itemStr.Replace('\t', ' ');
            MatchCollection ms = CommonFun.GetValues(itemStr, "<div class=\"tkprolistbox\">", "修改");

            foreach (Match m in ms)
            {
                ItemInfo item = GetOneItem(m.Value);

                string tag = CommonFun.GetValue(m.Value, "promotionEd=\"", "\"");

                decimal price = GetPlatformPrice(item.ID, item.Format, http);

                string info = "";

                if (item.Type != "333" && price != decimal.MaxValue && item.PlatformPrice != price)
                {
                    info = UpdatePrice(http, price * upParam, item.ViewCount, item.Type, item.Inventory);

                    Thread.Sleep(random.Next(3, 6) * 1000);

                    if (info != "1")
                    {
                        string priceStr = CommonFun.GetValue(info, ":\"", "\"");

                        price = 0;

                        try
                        {
                            price = Convert.ToDecimal(priceStr);
                            if (price != 1)
                            {
                                price = (decimal)((double)price - 0.01);
                            }
                            else
                            {
                                price = GetItemPrice(item);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                        }

                        if (price != item.PlatformPrice)
                        {
                            UpdatePrice(http, price, item.ViewCount, tag, item.Inventory);
                        }
                    }
                }
                item.ShopPrice = price;
                CommonFun.WriteCSV("TKAdmin/TKAdmin" + fileName + ".csv", item);
            }
            return true;
        }

        /// <summary>
        /// 比较新品并上架
        /// </summary>
        /// <returns></returns>
        public bool CarepareAndUpItem()
        {
            BaseReadWebPage newItem = new BaseReadWebPage();
            BaseReadWebPage downItem = new BaseReadWebPage();
            BaseReadWebPage onlineItem = new BaseReadWebPage();

            newItem.ReadBaseItemInfo("TK/低于20以上.csv", true);
            downItem.ReadBaseItemInfo("TKAdmin/down.csv", true);
            onlineItem.ReadBaseItemInfo("TKAdmin/TKAdmin.csv", true);

            Http.HttpRequest http = new Http.HttpRequest();
            http.CookieContainer = Login();

            foreach (string key in newItem.ShopAllItems.Keys)
            {
                if (onlineItem.ShopAllItems.ContainsKey(key))
                {
                    continue;
                }

                if (downItem.ShopAllItems.ContainsKey(key))
                {
                    downItem.ShopAllItems[key].ShopPrice = newItem.ShopAllItems[key].ShopPrice;
                    ReUpItem(downItem.ShopAllItems[key], http);
                    CommonFun.WriteCSV("TKAdmin/need_reUp.csv", downItem.ShopAllItems[key]);
                    continue;
                }

                UpNewItem(newItem.ShopAllItems[key], http);

                CommonFun.WriteCSV("TKAdmin/new" + fileName + ".csv", newItem.ShopAllItems[key]);
            }

            return true;
        }


        /// <summary>
        /// 获取下架列表
        /// </summary>
        /// <param name="cookie"></param>
        /// <returns></returns>
        public bool GetDownListItem(CookieCollection cookie)
        {
            GetWebPageDate.Http.HttpRequest http = new GetWebPageDate.Http.HttpRequest();

            string url = "https://admin.tkyfw.com/Goods/throughAudit?/Goods/throughAudit=&status=1&state=2";

            http.Cookie = "acw_tc=AQAAAMj+8lkEeQUA3eT4OlKCFpqJmk95; Qs_lvt_150743=1509785002%2C1510643909; PHPSESSID=0cv28td420q4ii7ult49qm4111; sellerCooke=think%3A%7B%22sellerName%22%3A%22hankang%22%2C%22sellerStoreName%22%3A%22%25E9%2595%25BF%25E6%25B2%2599%25E5%258E%25BF001%25E5%25BA%2597%22%7D; Qs_pv_150743=3457133030228596700%2C2642740307105821700%2C2882295116553261000%2C1172614365052146400%2C979891720064693100; Hm_lvt_ed70f863f631ecaac146592025767ed8=1509785002,1510643909; Hm_lpvt_ed70f863f631ecaac146592025767ed8=1510645265; nTalk_CACHE_DATA={uid:kf_9335_ISME9754_guestTEMP1758-0E75-6A,tid:1510643909116098}; NTKF_T2D_CLIENTID=guestTEMP1758-0E75-6A74-9EB9-8632DF7CEB19; Hm_lvt_3ad2c5e8712b25159b989a93a9927632=1510644101; Hm_lpvt_3ad2c5e8712b25159b989a93a9927632=1510645266; Hm_lvt_7203bc79de07054ef3770e27e8ca9068=1509780503,1509934350,1510060776,1510543588; Hm_lpvt_7203bc79de07054ef3770e27e8ca9068=1510647168";

            string content = http.HttpGet(url);

            string itemStr = CommonFun.GetValue(content, "<form name=\"form\" method=\"post\">", "</form>");

            MatchCollection ms = CommonFun.GetValues(itemStr, "<div class=\"white-box table-box\">", "<div class=\"span2 operationone\">");

            foreach (Match m in ms)
            {
                CommonFun.WriteCSV("TKAdmin/down.csv", GetOneItem(m.Value));
            }

            string pageStr = CommonFun.GetValue(content, "<div><li><span>", "\"><span>末页</span>");

            pageStr = pageStr.Substring(pageStr.LastIndexOf("=") + 1);

            int totalPage = 0;

            if (!string.IsNullOrEmpty(pageStr))
            {
                totalPage = Convert.ToInt32(pageStr);
            }

            for (int i = 2; i <= totalPage; i++)
            {
                content = http.HttpGet(url + "&p=" + i);

                itemStr = CommonFun.GetValue(content, "<form name=\"form\" method=\"post\">", "</form>");

                ms = CommonFun.GetValues(itemStr, "<div class=\"white-box table-box\">", "<div class=\"span2 operationone\">");

                foreach (Match m in ms)
                {
                    CommonFun.WriteCSV("TKAdmin/down.csv", GetOneItem(m.Value));
                }

                Console.WriteLine("{2}:totalPage:{0}, curPage:{1}", totalPage, i, DateTime.Now);
            }

            return true;
        }

        public bool UpAllNewItme()
        {
            BaseReadWebPage newItem = new BaseReadWebPage();
            newItem.ReadBaseItemInfo("TKAdmin/new.csv", true);

            Http.HttpRequest http = new Http.HttpRequest();

            int totalCount = newItem.ShopAllItems.Count;

            int curCount = 0;
            foreach (BaseItemInfo item in newItem.ShopAllItems.Values)
            {
                if (curCount % 100 == 0)
                {
                    http.CookieContainer = Login();
                }

                UpNewItem(item, http);

                curCount++;

                Console.WriteLine("{0}:UpNewItem,TotalCount{1}, curCount:{2}", DateTime.Now, totalCount, curCount);
            }

            return true;
        }

        /// <summary>
        /// 上架新物品
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool UpNewItem(BaseItemInfo item, Http.HttpRequest http)
        {
            if (item.ShopPrice < 5)
            {
                return false;
            }
            //find
            string findUrl = "https://admin.tkyfw.com/Goods/findExp";
            string findPostData = "store_id=34";
            http.HttpPost(findUrl, findPostData);

            //findApp
            string findAppUrl = "https://admin.tkyfw.com/Goods/findAppNum";
            string findAppPostData = string.Format("appNum:{0}", item.ID);
            http.HttpPost(findAppUrl, findAppPostData);

            string url = string.Format("https://admin.tkyfw.com/Goods/complateInfo?cat_first=&cat_second=&cat_third=&approval_number={0}", item.ID);

            string upNewItemUrl = "https://admin.tkyfw.com/Goods/findRepeatCheck";

            string checkUrl = "https://admin.tkyfw.com/Goods/checkGoods";

            string content = http.HttpGet(url);


            MatchCollection ms = CommonFun.GetValues(content, "gxtj_ul", "</li>");

            foreach (Match m in ms)
            {
                string fromat = CommonFun.GetValue(m.Value, "<span>", "<span>");

                fromat = CommonFun.FormatStr(fromat, item.Name);

                if (fromat == item.Format)
                {
                    string boxStr = CommonFun.GetValue(m.Value, "\"checkbox\"", "/>");

                    string value = CommonFun.GetValue(boxStr, "value=\"", "\"");

                    string result = http.HttpPost(upNewItemUrl, string.Format("value_arr={0}{1}", value, HttpUtility.UrlEncode(",")));

                    result = CommonFun.GetValue(result, ":", "}");

                    if (result == "2")
                    {
                        CommonFun.WriteCSV("TKAdmin/UpNew" + fileName + ".csv", item);
                        http.HttpPost(checkUrl, "", url, "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8", null);
                        Console.WriteLine("UpNewItem,name:{0}, id:{1}, fromat:{2}", item.Name, item.ID, item.Format);
                    }
                    Thread.Sleep(random.Next(3, 6) * 1000);


                    // UpdatePrice(item, http);
                }
            }

            return true;
        }

        /// <summary>
        /// 重新上架所有物品
        /// </summary>
        /// <returns></returns>
        public bool ReUpAllItem()
        {
            BaseReadWebPage newItem = new BaseReadWebPage();

            Http.HttpRequest http = new Http.HttpRequest();

            http.CookieContainer = Login();

            newItem.ReadBaseItemInfo("TKAdmin/need_reUp.csv", true);

            foreach (BaseItemInfo item in newItem.ShopAllItems.Values)
            {
                ReUpItem(item, http);
            }

            return true;
        }

        /// <summary>
        /// 从下架列表中重新上架
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool ReUpItem(BaseItemInfo item, Http.HttpRequest http)
        {
            if (item.ShopPrice > 5)
            {
                string url = "https://admin.tkyfw.com/Goods/edit_upGoods";

                string postStr = string.Format("storeid[]={0}", item.ViewCount);

                string result = http.HttpPost(url, postStr);
            }

            return true;
        }

        public bool ReUpItemByCSV(string fileName)
        {
            try
            {
                Http.HttpRequest http = new Http.HttpRequest();

                BaseReadWebPage newItem = new BaseReadWebPage();
                BaseReadWebPage downItemList = new BaseReadWebPage();

                newItem.ReadBaseItemInfo(fileName, true);
                downItemList.ReadBaseItemInfo("TKAdmin/down.csv", true);

                int count = 0;

                foreach (BaseItemInfo item in newItem.ShopAllItems.Values)
                {
                    foreach (BaseItemInfo downItem in downItemList.ShopAllItems.Values)
                    {
                        if (item.ID == downItem.ID)
                        {
                            if (count % 100 == 0)
                            {
                                http.CookieContainer = Login();
                            }

                            count++;

                            downItem.ShopPrice = downItem.PlatformPrice;

                            ReUpItem(downItem, http);

                            Thread.Sleep(random.Next(3, 6) * 1000);

                            break;
                        }
                    }
                }

                Console.WriteLine("UpItem count:{0}", count);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return true;
        }

        /// <summary>
        /// 下架物品
        /// </summary>
        /// <returns></returns>
        public bool DownItem()
        {
            GetWebPageDate.Http.HttpRequest http = new GetWebPageDate.Http.HttpRequest();

            http.Cookie = "Hm_lvt_ed70f863f631ecaac146592025767ed8=1509785002; Qs_lvt_150743=1509785002; Qs_pv_150743=3457133030228596700; NTKF_T2D_CLIENTID=guestTEMP1758-0E75-6A74-9EB9-8632DF7CEB19; acw_tc=AQAAALQugFvwMw4A3eT4OjtYTVthFUKH; PHPSESSID=cvbcvjhi3a70qu9drah6l31ht0; sellerCooke=think%3A%7B%22sellerName%22%3A%22hankang%22%2C%22sellerStoreName%22%3A%22%25E9%2595%25BF%25E6%25B2%2599%25E5%258E%25BF001%25E5%25BA%2597%22%7D; Hm_lvt_7203bc79de07054ef3770e27e8ca9068=1509780503,1509934350,1510060776; Hm_lpvt_7203bc79de07054ef3770e27e8ca9068=1510061193";

            string downUrl = "https://admin.tkyfw.com/Goods/edit_dowmGoods";

            BaseReadWebPage res = new BaseReadWebPage();
            res.ReadBaseItemInfo("TKAdmin/NotEixst.csv", true);

            BaseReadWebPage res1 = new BaseReadWebPage();
            res1.ReadBaseItemInfo("TKAdmin/低于20以上.csv", true);
            string postData = "storeid={0}&off_the_shelf=";

            int curCount = 0;

            int totalCount = res.ShopAllItems.Count + res1.ShopAllItems.Count;

            //foreach(BaseItemInfo item in res.ShopAllItems.Values)
            //{
            //    Console.WriteLine("curCount:{0}, totalCount:{1}", curCount++, totalCount);
            //    http.HttpPost(downUrl, string.Format(postData, item.ViewCount));
            //}

            foreach (BaseItemInfo item in res1.ShopAllItems.Values)
            {
                Console.WriteLine("curCount:{0}, totalCount:{1}", curCount++, totalCount);
                http.HttpPost(downUrl, string.Format(postData, item.ViewCount));
            }

            return true;
        }
    }
}
