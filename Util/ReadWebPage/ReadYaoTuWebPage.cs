using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GetWebPageDate.Util.ReadWebPage
{
    public class ReadYaoTuWebPage : BaseReadWebPage
    {
        private string username;

        private string password;
        public ReadYaoTuWebPage()
        {
            string userInfo = ConfigurationManager.AppSettings["ytUAndP"];
            string[] aUserInfo = userInfo.Split(',');

            username = aUserInfo[0];
            password = aUserInfo[1];
        }

        private static bool CheckValidationResult(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
        {
            return true; //总是接受
        }

        public override void Login()
        {
            string login_url = "http://reg.yaofangwang.cn/login.aspx";

            string postDataStr = string.Format("__EVENTTARGET=ctl00%24ContentPlaceHolder1%24t_login&__EVENTARGUMENT=&__VIEWSTATE=%2FwEPDwULLTE5NDEyNTg5OTlkGAEFHl9fQ29udHJvbHNSZXF1aXJlUG9zdEJhY2tLZXlfXxYBBSVjdGwwMCRDb250ZW50UGxhY2VIb2xkZXIxJGNiX1JlbWVtYmVycDfW9s57mnaJlPJZ%2FE5mlJKdAKY%3D&__VIEWSTATEGENERATOR=C2EE9ABB&__EVENTVALIDATION=%2FwEdAAXeB%2FnDyv7zWG1mzCLKNLgh%2FX3Fo%2FRaqYiLtErA%2B0XLEhccBLe9MIf%2BOeu1SwHT%2Fo0qqYLAmC5XXoyC%2BBAAg4gzqjQqitXSuUDfVH%2BcB8vH1lMEkbab73pTz5gdxWw0I0ubruCR&ctl00%24ContentPlaceHolder1%24txt_AccountName={0}&ctl00%24ContentPlaceHolder1%24txt_Password={1}", username, password);

            try
            {

                GetWebPageDate.Http.HttpRequest http = new GetWebPageDate.Http.HttpRequest();

                CookieContainer cookies = new CookieContainer();

                byte[] postData = Encoding.UTF8.GetBytes(postDataStr);

                HttpWebRequest loginRequest = (HttpWebRequest)WebRequest.Create(login_url);
                ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(CheckValidationResult);
                loginRequest.ProtocolVersion = HttpVersion.Version11;
                // 这里设置了协议类型。
                ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;// SecurityProtocolType.Tls1.2; 
                loginRequest.Method = "POST";
                loginRequest.KeepAlive = false;
                loginRequest.Timeout = 1000000;
                loginRequest.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
                loginRequest.Referer = "https://admin.tkyfw.com/Seller_login.html";
                loginRequest.ContentLength = postData.Length;
                loginRequest.UserAgent = "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)";
                //cookie.SetCookies(request.RequestUri, "Qs_lvt_150743=1500685870%2C1500777689; NTKF_T2D_CLIENTID=guestBC81D14C-3099-FCD9-FE94-67D92C0B496A; Hm_lvt_ed70f863f631ecaac146592025767ed8=1500685870; Qs_pv_150743=1447053609678541000%2C3007060151905128000%2C355027317986517250%2C1201566385246198500%2C3405144643715530000; Hm_lvt_3ad2c5e8712b25159b989a93a9927632=1500685895; acw_tc=AQAAAIyCqm27gAQABUULt6WUgM5Ca2iH; PHPSESSID=1sighr4l5nqe1dj4536u1mh2v3; __guid=202919361.837275310285311200.1509859920993.7488; renren_tag_0526=isTag; sellerCooke=think%3A%7B%22sellerName%22%3A%22hankang%22%2C%22sellerStoreName%22%3A%22%25E9%2595%25BF%25E6%25B2%2599%25E5%258E%25BF001%25E5%25BA%2597%22%7D; Hm_lvt_7203bc79de07054ef3770e27e8ca9068=1509860118; Hm_lpvt_7203bc79de07054ef3770e27e8ca9068=1509860118; monitor_count=3");
                loginRequest.CookieContainer = new CookieContainer();
                Stream myRequestStream = loginRequest.GetRequestStream();
                myRequestStream.Write(postData, 0, postData.Length);
                myRequestStream.Close();

                HttpWebResponse response = (HttpWebResponse)loginRequest.GetResponse();

                //response.Cookies = cookie.GetCookies(response.ResponseUri);

                Stream myResponseStream = response.GetResponseStream();
                cookies.Add(response.Cookies);
                StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.GetEncoding("utf-8"));
                string retString = myStreamReader.ReadToEnd();

                myStreamReader.Close();
                myResponseStream.Close();
                //request.CookieContainer = cookies;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Login();
            }

            request.Cookie = "__jsluid=00722fe4a2305a1f7e6e44e846a302ce; ASP.NET_SessionId=nmfxaoaonht2wzmiafye2w4v; Hm_lvt_3f534012f862ad7faa732b5b0655ce2f=1514443356; historysearch=; Hm_lpvt_3f534012f862ad7faa732b5b0655ce2f=1514456516; YaoTuWang_Passport_Identity=B833BEC308E7119F829C15C2FA15A1E061121AE12FE2BA42ED50F150A203050AE7F7DA8CE4DAC6D45CBD54C7ADF227F785BB989415C6391B326494E5D01B1B3B4DC1915F92E0B76DF720317BF99B4CCD681FA0E7149E1C60CD034E71B080AE5F6043F51A40C0BF376D052D7940681289FD4447184D2B44BE78BC10346F24E69310F61960C7098B8D95DF57FF6BA9B85081DEF0C1";
        }

        public override void ReadAllMenuURL()
        {
            string content = request.HttpGet(url);

            string mainMenuStr = CommonFun.GetValue(content, "nav bb1", "<div class=\"subcat-more\">");

            MatchCollection ms = CommonFun.GetValues(mainMenuStr, "<div class=\"other\">", "</div>");

            foreach (Match m in ms)
            {
                MatchCollection urlMs = CommonFun.GetValues(m.Value, "href=\"", "\"");

                foreach (Match urlM in urlMs)
                {
                    if (!AllMenuUrl.Contains(urlM.Value))
                    {
                        AllMenuUrl.Add(urlM.Value);
                    }
                }
            }

            int startIndex = content.IndexOf("<div class=\"subcat-more\">");

            int endIndex = content.LastIndexOf("<em>&gt;</em></a></div></div>");

            string sumMenuStr = content.Substring(startIndex, endIndex - startIndex);

            MatchCollection ms1 = CommonFun.GetValues(sumMenuStr, "<a href=\"", "\"");

            foreach (Match m in ms1)
            {
                string tempUrl = m.Value;

                if (!AllItemUrl.Contains(tempUrl))
                {

                    if (!tempUrl.Contains("http"))
                    {
                        tempUrl = "http://www.yaofangwang.cn" + tempUrl;
                    }

                    AllMenuUrl.Add(tempUrl);
                }
            }
        }

        public override void ReadAllItem()
        {
            List<string> itemUrl = new List<string>();

            int totalCount = AllMenuUrl.Count;
            int curCount = 0;
            foreach (string menuUrl in AllMenuUrl)
            {
                try
                {
                    string content = request.HttpGet(menuUrl);

                    string totalPage = CommonFun.GetValue(content, "</span><span>/</span><span>", "</span>");

                    GetOnePageItem(content);

                    string pageUrl = menuUrl.Substring(0, menuUrl.LastIndexOf('.'));

                    if (!string.IsNullOrEmpty(totalPage))
                    {
                        int total = Convert.ToInt32(totalPage);

                        for (int i = 2; i <= total; i++)
                        {
                            if (pageUrl == "http://www.yaofangwang.cn/product-35539" && i >= 10)
                            {
                                break;
                            }

                            string url = string.Format("{0}-p{1}.html", pageUrl, i);


                            content = request.HttpGet(url);

                            GetOnePageItem(content);

                            Console.WriteLine("TotalPage:{0}, CurCount:{1}", total, i);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(menuUrl);
                    Console.WriteLine(ex.ToString());
                }
                Console.WriteLine("TotalCount:{0}, CurCount:{1}", totalCount, ++curCount);
            }
        }

        private void GetOnePageItem(string content)
        {
            if (!string.IsNullOrEmpty(content))
            {
                content = CommonFun.GetValue(content, "<div class=\"prod-list\">", "<div class=\"page mt20 mb130 clearfix\">");

                GetOnePageBaseItemInfo(content);
            }
        }

        private void GetOnePageBaseItemInfo(string content)
        {
            MatchCollection ms = CommonFun.GetValues(content, "<div class=\"desc\">", "<div class=\"other\">");

            foreach (Match m in ms)
            {
                if (m.Value.Contains("noprice"))
                {
                    return;
                }

                string priceStr = CommonFun.GetValue(m.Value, "</small>", "</span>");

                if (string.IsNullOrEmpty(priceStr))
                {
                    Console.WriteLine("Login failed!!!!");
                    request.Cookie = "__jsluid=00722fe4a2305a1f7e6e44e846a302ce; ASP.NET_SessionId=nmfxaoaonht2wzmiafye2w4v; Hm_lvt_3f534012f862ad7faa732b5b0655ce2f=1514443356; historysearch=; Hm_lpvt_3f534012f862ad7faa732b5b0655ce2f=1514508794; YaoTuWang_Passport_Identity=9937796DFDBDE9A6A8AED4D5CF64EA4C22E6DA8F1256977C5DDB6BE83BA2792836A2A6441BE475E64955CB954B8A3C9BB962787AB25BA4E7E92EB98327D6A27934100ADE884D0F28E4D4DC2D60157EFCEA7568AFE2B3E7F059A623B3EE22F4024587087B0E98014BFD4E6AFEBA9D49A55B8B8335492218875C700F2DCBEC637CED603E8A15396800BC68A7A867559AEB744195D1";
                    return;
                }

                string name = CommonFun.GetValue(m.Value, "\">", "</a>");

                if (name.Contains("注射"))
                {
                    return;
                }
                else
                {
                    BaseItemInfo info = new BaseItemInfo();

                    string[] nameArray = name.Split(' ');

                    if (nameArray.Length > 1)
                    {
                        info.Name = nameArray[1];
                    }
                    else
                    {
                        info.Name = nameArray[0];
                    }

                    MatchCollection msFormatAndCreated = CommonFun.GetValues(m.Value, "<div class=\"h1\">", "</div>");

                    info.Format = msFormatAndCreated[1].Value;
                    info.Created = msFormatAndCreated[2].Value;
                    info.PlatformPrice = Convert.ToDecimal(priceStr);

                    AddItme(info, "YaoTu/YaoTu" + ticks + ".csv");
                }
            }
        }

        /// <summary>
        /// 对比价格(在线数据)
        /// </summary>
        public void OnlineComparePrice()
        {
            ReadPlatFormWebPageValue flatform = new ReadPlatFormWebPageValue();

            Dictionary<string, BaseItemInfo> platItems = new Dictionary<string, BaseItemInfo>();

            Dictionary<string, List<BaseItemInfo>> seachItems = new Dictionary<string, List<BaseItemInfo>>();

            Dictionary<string, string> seachedItemName = new Dictionary<string, string>();

            foreach (BaseItemInfo item in ShopAllItems.Values)
            {
                string key = item.Name + item.Format + item.Created;

                //查找该商品
                if (!seachedItemName.ContainsKey(item.Name))
                {
                    seachedItemName.Add(item.Name, item.Name);

                    List<BaseItemInfo> item_list = flatform.SeachInfoByID(item.Name);

                    Dictionary<string, BaseItemInfo> minPricItems = new Dictionary<string, BaseItemInfo>();


                    foreach (BaseItemInfo sItem in item_list)
                    {
                        string sItemKey = sItem.Name + sItem.Format + sItem.Created;

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

                    seachItems.Add(item.Name, minPricItems.Values.ToList());
                }

                //对比价格
                if (seachItems.ContainsKey(item.Name))
                {
                    List<BaseItemInfo> compareItems = seachItems[item.Name];
                    bool isExist = false;
                    foreach (BaseItemInfo compareItem in compareItems)
                    {
                        if (item.Created == compareItem.Created)
                        {
                            if (CommonFun.IsSameFormat(item.Format, compareItem.Format, item.Name, compareItem.Name))
                            {
                                isExist = true;
                                //价格对比
                                if (item.ShopPrice > 0 && item.ShopPrice * (decimal)0.75 >= compareItem.ShopSelaPrice)
                                {
                                    //浏览量对比
                                    if (!string.IsNullOrEmpty(compareItem.ViewCount) && Convert.ToInt32(compareItem.ViewCount) >= 500)
                                    {
                                        item.ID = compareItem.ID;
                                        item.ShopSelaPrice = compareItem.ShopSelaPrice;
                                        item.Type = compareItem.Type;
                                        item.ViewCount = compareItem.ViewCount;

                                        CommonFun.WriteCSV("25%" + ticks + ".csv", item);
                                    }
                                }
                                break;
                            }
                        }
                    }
                    if (!isExist)
                    {
                        CommonFun.WriteCSV("YaoTu/NoFormat" + ticks + ".csv", item);
                    }
                }
                else
                {
                    CommonFun.WriteCSV("YaoTu/NotExist" + ticks + ".csv", item);
                }
            }

            foreach (BaseItemInfo item in platItems.Values)
            {
                CommonFun.WriteCSV("YaoTu/Plateform" + ticks + ".csv", item);
            }
        }

        public void ComparePrice()
        {
            ReadPlatFormWebPageValue flatform = new ReadPlatFormWebPageValue();
            BaseReadWebPage res = new BaseReadWebPage();
            BaseReadWebPage target = new BaseReadWebPage();

            target.ReadBaseItemInfo("YaoTu/YaoTu636500822100643979.csv", true);
            res.ReadBaseItemInfo("YaoTu/Plateform636500822100643979.csv", true);

            foreach (BaseItemInfo item in target.ShopAllItems.Values)
            {
                foreach (BaseItemInfo platformItem in res.ShopAllItems.Values)
                {

                    if ((item.Name == platformItem.Name && item.Created == platformItem.Created))
                    {
                        if (CommonFun.IsSameFormat(platformItem.Format, item.Format, platformItem.Name, item.Name))
                        {
                            //浏览量对比
                            //if (!string.IsNullOrEmpty(platformItem.ViewCount) && Convert.ToInt32(platformItem.ViewCount) >= 500)
                            //{

                            if (platformItem.ShopSelaPrice * (decimal)0.75 >= item.ShopPrice)
                            {
                                item.ID = platformItem.ID;
                                item.ShopSelaPrice = platformItem.ShopSelaPrice;
                                item.Type = platformItem.Type;
                                item.ViewCount = platformItem.ViewCount;

                                CommonFun.WriteCSV("YaoTu/25%_" + ticks + ".csv", item);
                            }
                            else if (platformItem.ShopSelaPrice * (decimal)0.8 >= item.ShopPrice)
                            {
                                item.ID = platformItem.ID;
                                item.ShopSelaPrice = platformItem.ShopSelaPrice;
                                item.Type = platformItem.Type;
                                item.ViewCount = platformItem.ViewCount;

                                CommonFun.WriteCSV("YaoTu/20%_" + ticks + ".csv", item);
                            }
                            else if (platformItem.ShopSelaPrice * (decimal)0.85 >= item.ShopPrice)
                            {
                                item.ID = platformItem.ID;
                                item.ShopSelaPrice = platformItem.ShopSelaPrice;
                                item.Type = platformItem.Type;
                                item.ViewCount = platformItem.ViewCount;

                                CommonFun.WriteCSV("YaoTu/15%_" + ticks + ".csv", item);
                            }
                            //}
                        }
                    }
                }
            }
        }

        public override void Start()
        {
            try
            {
                userName = "汉康大药房有限公司";
                url = "http://www.yaofangwang.cn/product-3613.html";
                password = "hk*1989.";

                Login();

                ReadAllMenuURL();

                ReadAllItem();

                OnlineComparePrice();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

        }
    }
}
