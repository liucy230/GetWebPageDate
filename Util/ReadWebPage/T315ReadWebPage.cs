using GetWebPageDate.Http;
using GetWebPageDate.Util.Item;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace GetWebPageDate.Util.ReadWebPage
{
    public class T315ReadWebPage : BaseReadWebPage
    {
        private string url = "https://www.315jiage.cn";

        private string username;

        private string password ;

        private string fileName = "315";

        private Dictionary<string, BaseItemInfo> dicFixedInfos = new Dictionary<string, BaseItemInfo>();

        private string selfStoreName = "长沙百寿康大药房";

        private int startNum = 100000;
        //private string[] unloadList;

        /// <summary>
        /// 上一次通知的id
        /// </summary>
        private int lastOrderId;

        /// <summary>
        /// 信息
        /// </summary>
        private string msg;

        private List<string> unsendlist = new List<string>();

        private string accessKeyId = "s11201";

        private string accessKeyIdSecret = "";

        private int sleepTime = 2000;

        public T315ReadWebPage()
        {
            lastOrderId = Convert.ToInt32(ConfigurationManager.AppSettings["orderIdKey"]);
            msg = ConfigurationManager.AppSettings["msgContentKey"];
            unsendlist = GetConfigList("unsendKey");

            string userInfo = ConfigurationManager.AppSettings["315UAndP"];

            accessKeyIdSecret = ConfigurationManager.AppSettings["315Secret"];

            string[] aUserInfo = userInfo.Split(',');

            username = aUserInfo[0];
            password = aUserInfo[1];
        }

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

        private int GetRmd()
        {
            ++startNum;

            if (startNum == 999999)
            {
                startNum = 100000;
            }

            return startNum;
        }
        public override void Login()
        {
            request.Cookie =string.Format( "__cfduid=d82ead74a72c7e771d1cd03fa9db77c581525918658; yjs_id=dacb83ba45b2bb8a30c8f49fc0099370; iwmsGid=0F1AJW58293AZVXOE2VQ; Hm_lvt_4cce664ec5d8326cc457ab09053c15b2=1528102845; ctrl_time=1; Hm_lpvt_4cce664ec5d8326cc457ab09053c15b2=1530000397; iwmsUser=%7Bver:4,id:525498,name:'{0}',psw:'{1}',admin:0,admPs:0,msg:0,sendOrder:0,mshopid:11201,mshopOrderQueue:43,mfactoryid:0,wx:1,keep:0,lastSet:9722406%7D", username, password);
            base.Login();
        }

        /// <summary>
        /// 可上架厂商
        /// </summary>
        /// <param name="created"></param>
        /// <returns></returns>
        public bool CanUpCreated(string created)
        {
            foreach (string blackCreated in createdBlackList)
            {
                if (created.Contains(blackCreated))
                {
                    return false;
                }
            }

            return true;
        }

     

        //private int GetStock(BaseItemInfo item)
        //{
        //    //if(string.item.Type)
        //}
        public void DoOpt(ReadPlatFormWebPageValue flatform, BaseItemInfo item)
        {
            try
            {
                //int totalCount = ShopAllItems.Count, curCount = 0;
                //foreach (KeyValuePair<string, BaseItemInfo> info in ShopAllItems)
                //{
                try
                {
                    //Console.WriteLine("{0},totalCount:{1}, curCount:{2}", DateTime.Now.ToString(), totalCount, ++curCount);

                    if ((DateTime.Now - startTime).TotalMinutes > 30)
                    {
                        //Login();
                        flatform.Login();
                        startTime = DateTime.Now;
                    }

                    //BaseItemInfo item = info.Value;

                    if (!CanUpCreated(item.Created))
                    {
                        if (item.SellType != "2")
                        {
                            item.ShopPrice = GetPrice(item.ViewCount);
                            UpdateItemInfo(item.ItemName, item.ShopPrice, "2", item.Type, item.Inventory, item);
                        }

                        return;
                    }

                    if (!string.IsNullOrEmpty(item.Type) || !CanLoad(item.Name, item.ID))
                    {
                        if (!CanLoad(item.Name, item.ID) && item.SellType != "2")
                        {
                            UpdateItemInfo(item.ItemName, item.ShopPrice, "2", item.Type, item.Inventory, item);
                        }
                        return;
                    }

                    string key = item.Name + item.Format + item.Created;

                    if (!CanUpDatePrice(key))
                    {
                        BaseItemInfo tItem = unUpdatePrice[key];

                        if (Convert.ToInt16(item.Inventory) <= 10 || item.PlatformPrice != tItem.ShopPrice)
                        {
                            item.Inventory = random.Next(30, 40).ToString();

                            UpdateItemInfo(item.ItemName, tItem.ShopPrice, item.SellType, item.Type, item.Inventory, item);

                            CommonFun.WriteCSV(fileName + "/onlyUpdateStock" + ticks + fileExtendName, item);
                        }

                        return;
                    }

                    item.ShopPrice = GetPrice(item.ViewCount);

                    //小于10的下架
                    if (item.ShopPrice > 0 && item.ShopPrice < 10)
                    {
                        if (item.SellType != "2")
                        {
                            UpdateItemInfo(item.ItemName, item.ShopPrice, "2", item.Type, "0", item);

                            CommonFun.WriteCSV(fileName + "/l10" + ticks + fileExtendName, item);
                        }

                        return;
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
                            if (item.ID.Trim() != compareItem.ID.Trim())
                            {
                                continue;
                            }

                            if (CommonFun.IsSameFormat(item.Format, compareItem.Format, item.Name, compareItem.Name))
                            {
                                isExist = true;

                                if (item.ShopPrice == 0)
                                {
                                    if (compareItem.ShopSelaPrice > 5)
                                    {
                                        decimal updatePrice = GetOlnyPrice(compareItem.ShopSelaPrice);
                                        item.ShopPrice = updatePrice;
                                        item.Inventory = random.Next(30, 40).ToString();
                                        UpdateItemInfo(item.ItemName, updatePrice, "0", item.Type, item.Inventory, item);
                                        CommonFun.WriteCSV(fileName + "/zreoUp" + ticks + ".csv", item);
                                    }
                                }
                                else
                                {
                                    if (!ComparePrice(compareItem, item))
                                    {
                                        item.ShopPrice = GetNoProfitItemPrice(compareItem.ShopSelaPrice);
                                        UpdateItemInfo(item.ItemName, item.ShopPrice, "0", item.Type, random.Next(30, 40).ToString(), item);
                                        CommonFun.WriteCSV(fileName + "/down" + ticks + ".csv", item);
                                    }
                                    else
                                    {
                                        //decimal updatePrice = item.ShopPrice - lPrice;
                                        if (CanChangePrice(item) || item.SellType != "0")
                                        {
                                            item.Inventory = random.Next(30, 40).ToString();
                                            UpdateItemInfo(item.ItemName, item.ShopPrice - lPrice, "0", item.Type, item.Inventory, item);
                                            CommonFun.WriteCSV(fileName + "/change" + ticks + ".csv", item);
                                        }
                                    }
                                }
                            }
                        }

                        if (!isExist)
                        {
                            UpdateItemInfo(item.ItemName, item.ShopPrice, "2", item.Type, "0", item);
                            CommonFun.WriteCSV(fileName + "/notFormat" + ticks + ".csv", item);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
                //}
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
                ticks = DateTime.Now.Ticks;

                Login();

                base.Start();

                ReadPlatFormWebPageValue platform = new ReadPlatFormWebPageValue();

                int totalCount = ShopAllItems.Count;
                int curCount = 0;

                foreach (BaseItemInfo item in ShopAllItems.Values)
                {
                    Console.WriteLine("{0} TotalCount:{1} CurCount:{2}", DateTime.Now, totalCount, ++curCount);
                    DoOpt(platform, item);
                }

                //UpdateFixedItem();


                //DoOpt();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public override void ReadAllMenuURL()
        {
            base.ReadAllMenuURL();
        }

        public override void ReadAllItemURL()
        {
            base.ReadAllItemURL();
        }

        private int GetTotalPage()
        {
            int totalPage = 1;
            int state = 0;
            DateTime start = DateTime.Now;
            try
            {
                bool haveMore = false;
                int page = 1;
                do
                {
                    if (state == 0)
                    {
                        page += 1000;
                    }
                    else if (state == 1)
                    {
                        page += 100;
                    }
                    else if (state == 2)
                    {
                        page += 10;
                    }
                    else
                    {
                        page += 1;
                    }

                    //string content = request.HttpGet(url, null, true);//"{\"success\":1,\"objs\":[{\"id\":\"1619918\",\"mid\":61482,\"aid\":\"148234\",\"name\":\"阿托伐他汀钙分散片\",\"pname\":\"京舒\",\"price\":\"100.00\",\"priceCap\":\"0.00\",\"state\":0,\"url\":\"\",\"date\":\"2018-06-04\",\"unit\":\"盒\",\"model\":\"\",\"nurl\":\"x-XueYeXi/148234.htm\",\"norm\":\"10mg*7片\",\"drugForm\":\"\",\"factory\":\"广东百科制药有限公司\",\"certification\":\"国药准字H20120021\",\"barcode\":\"6922662602122\",\"barcodeUser\":\"\",\"barcodeDate\":\"\",\"stock\":3,\"hasImage\":true,\"hasPremium\":false,\"hasExpirePremium\":false,\"warning\":0,\"pinned\":false}],\"haveMore\":0}";//
                    string content = null;
                    bool isSleep = false;

                    //do
                    //{
                    //https://www.315jiage.cn/apiShop.aspx?x-jg-action=items.get&page_no=1&page_size=50

                    string url = string.Format("https://www.315jiage.cn/memberViewPrices.aspx?act=ajax&cmd=get&p={0}&w=mname&k=&minStock=&maxStock=&hot=0&rnd={1}", page, GetRmd());

                    content = request.HttpGet(url, null, true);//"{\"success\":1,\"objs\":[{\"id\":\"1619918\",\"mid\":61482,\"aid\":\"148234\",\"name\":\"阿托伐他汀钙分散片\",\"pname\":\"京舒\",\"price\":\"100.00\",\"priceCap\":\"0.00\",\"state\":0,\"url\":\"\",\"date\":\"2018-06-04\",\"unit\":\"盒\",\"model\":\"\",\"nurl\":\"x-XueYeXi/148234.htm\",\"norm\":\"10mg*7片\",\"drugForm\":\"\",\"factory\":\"广东百科制药有限公司\",\"certification\":\"国药准字H20120021\",\"barcode\":\"6922662602122\",\"barcodeUser\":\"\",\"barcodeDate\":\"\",\"stock\":3,\"hasImage\":true,\"hasPremium\":false,\"hasExpirePremium\":false,\"warning\":0,\"pinned\":false}],\"haveMore\":0}";//

                    if (!string.IsNullOrEmpty(content) && content.Contains("{\"success\":0,\"message\":\"请不要频繁请求\"}"))
                    {
                        Console.WriteLine(string.Format("content:{0}, url:{1}, page:{2}", content, url, page));
                        Thread.Sleep(2000);
                        isSleep = true;
                    }
                    //} while (isSleep);

                    haveMore = CommonFun.GetValue(content, "\"haveMore\":", "}") == "1";

                    if (!haveMore && !isSleep)
                    {
                        if (state == 0)
                        {
                            page -= 1000;
                        }
                        else if (state == 1)
                        {
                            page -= 100;
                        }
                        else if (state == 2)
                        {
                            page -= 10;
                        }
                        else
                        {
                            totalPage = page;
                            Thread.Sleep(2000);
                            break;
                        }

                        state++;
                    }
                    Thread.Sleep(2000);
                } while (true);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            Console.WriteLine("start:{0} end:{1} useTime:{2} totalPage:{3}", start, DateTime.Now, (DateTime.Now - start).TotalSeconds, totalPage);
            return totalPage;
        }

        public void GetItemInfo()
        {
            try
            {
                string postData = "{\"page_no\":1, \"page_size\":50,}";

                string url = "https://www.315jiage.cn/apiShop.aspx";

                string action = "items.get";//323FE90CCC6C377E632ABFE2AB228126

                //string contentType = "application/json; charset=utf-8";
                string userAgent = "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/67.0.3396.87 Safari/537.36";

                string contentMD5 = CommonFun.MD5Str(postData);

                string date = CommonFun.GetGMTDate();

                byte[] hmacData = CommonFun.Hmac(contentMD5 + "\n" + date + "\n" + "items.get", accessKeyIdSecret);

                string signature = CommonFun.Base64(hmacData);

                string authorization = accessKeyId + ":" + signature;

                Dictionary<string, string> heads = new Dictionary<string, string>();

                heads.Add("Authorization", authorization);
                heads.Add("Content-MD5", contentMD5);
                heads.Add("x-jg-action", action);
                heads.Add("Date", date);
                //heads.Add("Content-Type", contentType);
                heads.Add("User-Agent", userAgent);
                //request.Cookie = string.Format("Authorization={0};ContentMD5={1};Date={2};x-jg-action;{3}", authorization, contentMD5, date, "items.get");

                string content = request.HttpPost(url, postData, heads);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }


        private void FastReadAllItem()
        {
            int totalPage = GetTotalPage();

            RequestInfo requestInfo = new RequestInfo();
            requestInfo.IsUseUserAgent = true;

            for (int i = 1; i <= totalPage; i++)
            {
                string url = string.Format("https://www.315jiage.cn/memberViewPrices.aspx?act=ajax&cmd=get&p={0}&w=mname&k=&minStock=&maxStock=&hot=0&rnd=111110", i);
                requestInfo.Urls.Add(url);
            }

            RequestUrls(requestInfo);

            foreach (string value in requestInfo.Contentes)
            {
                try
                {
                    string content = CommonFun.GetValue(value, "\"objs\":", "\"haveMore\":");

                    MatchCollection ms = CommonFun.GetValues(content, "{", "}");

                    foreach (Match m in ms)
                    {
                        BaseItemInfo item = GetItem(m.Value);

                        string key = item.ID + "{" + item.Format + "}";

                        if (string.IsNullOrEmpty(item.Type))
                        {
                            if (ShopAllItems.ContainsKey(key))
                            {
                                if (item.ShopPrice != 0 && ShopAllItems[key].ShopPrice > item.ShopPrice)
                                {
                                    ShopAllItems[key] = item;
                                }
                            }
                            else
                            {
                                ShopAllItems.Add(key, item);
                            }
                        }
                        else
                        {
                            dicFixedInfos.Add(key, item);
                            CommonFun.WriteCSV(fileName + "/fixed" + ticks + ".csv", item);
                        }

                        CommonFun.WriteCSV(fileName + "/315_" + ticks + ".csv", item);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
        }

        private void NormalReadAllItem()
        {
            int useTime = 3;
            //对比数据
            ReadPlatFormWebPageValue flatform = new ReadPlatFormWebPageValue();

            try
            {
                string content = null;

                List<int> allPageIndex = new List<int>();

                int totalPage = GetTotalPage();

                for (int i = 1; i <= totalPage; i++)
                {
                    allPageIndex.Add(i);
                }

                while (allPageIndex.Count > 0)
                {
                    DateTime startTime = DateTime.Now;
                    int index = random.Next(allPageIndex.Count);
                    int page = allPageIndex[index];
                    allPageIndex.RemoveAt(index);

                    try
                    {
                        string url = string.Format("https://www.315jiage.cn/memberViewPrices.aspx?act=ajax&cmd=get&p={0}&w=mname&k=&minStock=&maxStock=&hot=0&rnd={1}", page, GetRmd());

                        content = request.HttpGet(url, null, true);//"{\"success\":1,\"objs\":[{\"id\":\"1619918\",\"mid\":61482,\"aid\":\"148234\",\"name\":\"阿托伐他汀钙分散片\",\"pname\":\"京舒\",\"price\":\"100.00\",\"priceCap\":\"0.00\",\"state\":0,\"url\":\"\",\"date\":\"2018-06-04\",\"unit\":\"盒\",\"model\":\"\",\"nurl\":\"x-XueYeXi/148234.htm\",\"norm\":\"10mg*7片\",\"drugForm\":\"\",\"factory\":\"广东百科制药有限公司\",\"certification\":\"国药准字H20120021\",\"barcode\":\"6922662602122\",\"barcodeUser\":\"\",\"barcodeDate\":\"\",\"stock\":3,\"hasImage\":true,\"hasPremium\":false,\"hasExpirePremium\":false,\"warning\":0,\"pinned\":false}],\"haveMore\":0}";//

                        if (!string.IsNullOrEmpty(content) && content.Contains("{\"success\":0,\"message\":\"请不要频繁请求\"}"))
                        {
                            Console.WriteLine("dateTime{0}, content:{1}", DateTime.Now, content);
                            allPageIndex.Add(page);
                            Thread.Sleep(useTime * 1000);
                            //isSleep = true;
                        }
                        else
                        {
                            content = CommonFun.GetValue(content, "\"objs\":", "\"haveMore\":");

                            MatchCollection ms = CommonFun.GetValues(content, "{", "}");

                            foreach (Match m in ms)
                            {
                                try
                                {
                                    BaseItemInfo item = GetItem(m.Value, false);

                                    string key = item.Name + item.Format + item.Created; //item.ID + "{" + item.Format + "}";

                                    if (string.IsNullOrEmpty(item.Type))
                                    {
                                        if (ShopAllItems.ContainsKey(key))
                                        {
                                            if (item.ShopPrice != 0 && ShopAllItems[key].ShopPrice > item.ShopPrice)
                                            {
                                                ShopAllItems[key] = item;
                                            }
                                        }
                                        else
                                        {
                                            ShopAllItems.Add(key, item);
                                            //DoOpt(flatform, item);
                                        }
                                    }
                                    else
                                    {
                                        if (!dicFixedInfos.ContainsKey(key))
                                        {
                                            dicFixedInfos.Add(key, item);
                                            //UpdateFixedItem(item);
                                        }

                                        CommonFun.WriteCSV(fileName + "/fixed" + ticks + ".csv", item);
                                    }

                                    CommonFun.WriteCSV(fileName + "/315_" + ticks + ".csv", item);
                                    //Thread.Sleep(sleepTime);
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine(ex);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }

                    Console.WriteLine("{0} curPage:{1}, totalPage:{2}", DateTime.Now, page, allPageIndex.Count);

                    double useSeconds = (DateTime.Now - startTime).TotalSeconds;
                    if (useSeconds < useTime)
                    {
                        Thread.Sleep((int)Math.Floor((useTime - useSeconds) * 1000));
                    }
                };


            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

        }

        /// <summary>
        /// 更新固定价格表
        /// </summary>
        private void OptUnupdatePriceItem()
        {
            try
            {
                foreach (BaseItemInfo item in unUpdatePrice.Values)
                {
                    List<BaseItemInfo> sItems = Seach315Item(item.ID);

                    foreach (BaseItemInfo sItem in sItems)
                    {
                        if (CommonFun.IsSameFormat(item.Format, sItem.Format, item.Name, sItem.Name))
                        {
                            bool needUpdate = false;
                            if (sItem.PlatformPrice != item.ShopPrice)
                            {
                                sItem.PlatformPrice = item.ShopPrice;
                                needUpdate = true;
                            }
                            else if (Convert.ToInt16(sItem.Inventory) <= 10)
                            {
                                sItem.Inventory = random.Next(30, 40).ToString();
                                needUpdate = true;
                            }

                            if (needUpdate)
                            {
                                UpdateItemInfo(sItem.ItemName, sItem.PlatformPrice, "0", sItem.Type, sItem.Inventory, sItem);
                                CommonFun.WriteCSV(fileName + "/onlyUpdateStock" + ticks + fileExtendName, item);
                            }

                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public override void ReadAllItem()
        {
            //FastReadAllItem();

            UpFixedItem();

            NormalReadAllItem();



            //int page = 0;
            //bool haveMore = false;
            //do
            //{
            //    try
            //    {
            //        DateTime startTime = DateTime.Now;
            //        string content = null;
            //        bool isSleep = false;
            //        ++page;
            //        do
            //        {
            //            string url = string.Format("https://www.315jiage.cn/memberViewPrices.aspx?act=ajax&cmd=get&p={0}&w=mname&k=&minStock=&maxStock=&hot=0&rnd={1}", page, GetRmd());
            //            content = request.HttpGet(url, null, true);//"{\"success\":1,\"objs\":[{\"id\":\"1619918\",\"mid\":61482,\"aid\":\"148234\",\"name\":\"阿托伐他汀钙分散片\",\"pname\":\"京舒\",\"price\":\"100.00\",\"priceCap\":\"0.00\",\"state\":0,\"url\":\"\",\"date\":\"2018-06-04\",\"unit\":\"盒\",\"model\":\"\",\"nurl\":\"x-XueYeXi/148234.htm\",\"norm\":\"10mg*7片\",\"drugForm\":\"\",\"factory\":\"广东百科制药有限公司\",\"certification\":\"国药准字H20120021\",\"barcode\":\"6922662602122\",\"barcodeUser\":\"\",\"barcodeDate\":\"\",\"stock\":3,\"hasImage\":true,\"hasPremium\":false,\"hasExpirePremium\":false,\"warning\":0,\"pinned\":false}],\"haveMore\":0}";//

            //            if (!string.IsNullOrEmpty(content) && content.Contains("{\"success\":0,\"message\":\"请不要频繁请求\"}"))
            //            {
            //                Console.WriteLine("dateTime{0}, content:{1}", DateTime.Now, content);
            //                Thread.Sleep(3000);
            //                isSleep = true;
            //            }
            //        } while (isSleep);

            //        haveMore = CommonFun.GetValue(content, "\"haveMore\":", "}") == "1";

            //        content = CommonFun.GetValue(content, "\"objs\":", "\"haveMore\":");

            //        MatchCollection ms = CommonFun.GetValues(content, "{", "}");

            //        Console.WriteLine("{0} curPage:{1}", DateTime.Now, page);

            //        foreach (Match m in ms)
            //        {
            //            BaseItemInfo item = GetItem(m.Value);

            //            string key = item.ID + "{" + item.Format + "}";

            //            if (string.IsNullOrEmpty(item.Type))
            //            {
            //                if (ShopAllItems.ContainsKey(key))
            //                {
            //                    if (item.ShopPrice != 0 && ShopAllItems[key].ShopPrice > item.ShopPrice)
            //                    {
            //                        ShopAllItems[key] = item;
            //                    }
            //                }
            //                else
            //                {
            //                    ShopAllItems.Add(key, item);
            //                }
            //            }
            //            else
            //            {
            //                dicFixedInfos.Add(key, item);
            //                CommonFun.WriteCSV(fileName + "/fixed" + ticks + ".csv", item);
            //            }

            //            CommonFun.WriteCSV(fileName + "/315_" + ticks + ".csv", item);
            //        }
            //    }
            //    catch (Exception ex)
            //    {
            //        Console.WriteLine(ex);
            //    }
            //} while (haveMore);
        }

        private BaseItemInfo GetItem(string content, bool isNeedPrice = true)
        {
            try
            {
                BaseItemInfo item = new BaseItemInfo();

                item.Name = CommonFun.GetValue(content, "\"name\":\"", "\"");
                item.ID = CommonFun.GetValue(content, "\"certification\":\"", "\"");
                item.Created = CommonFun.GetValue(content, "\"factory\":\"", "\"");
                item.Format = CommonFun.GetValue(content, "\"norm\":\"", "\"");
                item.Type = CommonFun.GetValue(content, "\"url\":\"", "\"");
                item.ViewCount = CommonFun.GetValue(content, "\"nurl\":\"", "\"");
                item.PlatformPrice = Convert.ToDecimal(CommonFun.GetValue(content, "\"price\":\"", "\""));
                item.ItemName = CommonFun.GetValue(content, "\"mid\":", ",");
                item.Inventory = CommonFun.GetValue(content, "\"stock\":", ",");
                item.SellType = CommonFun.GetValue(content, "\"state\":", ",");

                if (CanLoad(item.Name, item.ID) && isNeedPrice)
                {
                    item.ShopPrice = GetPrice(item.ViewCount);
                }

                return item;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            return null;
        }

        public Dictionary<string, BaseItemInfo> GetSellingItem(string id, bool isName = false, bool isNeedPrice = true)
        {
            Dictionary<string, BaseItemInfo> items = new Dictionary<string, BaseItemInfo>();
            try
            {
                if (!string.IsNullOrEmpty(id))
                {
                    int count = 0;
                    do
                    {
                        if (count == 1)
                        {
                            if (id.Contains(" "))
                            {
                                id = id.Replace(" ", "");
                            }
                            else
                            {
                                string idStr = CommonFun.GetUnChinese(id);
                                id = id.Replace(idStr, " ");
                                id += idStr;
                            }
                        }
                        string url = string.Format("https://www.315jiage.cn/memberViewPrices.aspx?act=ajax&cmd=get&p=1&w={1}&k={0}&minStock=&maxStock=&hot=0&rnd=334376", id, isName ? "mname" : "certification");

                        string content = request.HttpGet(string.Format(url, id), null, true);

                        content = CommonFun.GetValue(content, "\"objs\":", "\"haveMore\":");

                        MatchCollection ms = CommonFun.GetValues(content, "{", "}");

                        foreach (Match m in ms)
                        {
                            BaseItemInfo item = GetItem(m.Value, isNeedPrice);

                            string key = item.ID + item.Format;

                            if (!items.ContainsKey(key))
                            {
                                items.Add(key, item);
                            }
                            else
                            {
                                Console.WriteLine("315 Error, Same Itme key:{0}", key);
                            }
                            //Thread.Sleep(sleepTime);
                        }
                    } while (items.Count == 0 && ++count == 1);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return items;
        }

        private OrderItemInfo GetOrderItem(string info)
        {
            try
            {
                OrderItemInfo item = new OrderItemInfo();

                item.ID = CommonFun.GetValue(info, "id=\"order-", "\"");
                item.PhoneNO = CommonFun.GetValue(info, "电话:", "，");
                item.PhoneNO = item.PhoneNO.Trim();//,
                item.Price = CommonFun.GetValue(info, "\"o_totalPrice\">", "<");
                item.Count = CommonFun.GetValue(info, "\"o_shippingCharges\">", "<");
                item.Address = CommonFun.GetValue(info, "QQ:", "br />");
                item.Address = CommonFun.GetValue(item.Address, "，", "<");
                item.UserName = (Convert.ToDecimal(item.Price) + Convert.ToDecimal(item.Count)).ToString();
                if (string.IsNullOrEmpty(item.Count) || item.Count == "0")
                {
                    item.Count = "包邮";
                }
                else
                {
                    item.Count = string.Format(",运费金额：{0}", item.Count);
                }

                //item.UserName = CommonFun.GetValue(info, "", "");

                return item;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return null;
        }

        /// <summary>
        /// 是否可发送
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        private bool CanSend(string address)
        {
            try
            {
                if (!string.IsNullOrEmpty(address))
                {
                    string[] addressInfo = address.Split(' ');

                    if (addressInfo.Length > 1)
                    {
                        foreach (string key in unsendlist)
                        {
                            if (addressInfo[1].Contains(key))
                            {
                                return false;
                            }
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return true;
        }
        public void NoticeUserPay()
        {
            try
            {
                DateTime startTime = DateTime.Now;
                do
                {
                    if (DateTime.Now.Date != startTime.Date)
                    {
                        startTime = DateTime.Now;
                        ticks = DateTime.Now.Ticks;
                    }

                    Login();

                    int curPage = 1;

                    int totalPage = 0;

                    bool isNoticeAll = false;

                    int baseOrderId = lastOrderId;

                    int maxOrderId = 0;

                    do
                    {
                        string url = string.Format("https://www.315jiage.cn/memberViewOrders.aspx?page={0}", curPage);

                        string content = request.HttpGet(url, null, true);

                        if (totalPage == 0)
                        {
                            string totalPageStr = CommonFun.GetValue(content, "<li class=\"p_total\">1/", "<");

                            if (!string.IsNullOrEmpty(totalPageStr))
                            {
                                totalPage = Convert.ToInt32(totalPageStr);
                            }
                        }

                        Console.WriteLine("{0} Sending totalPage:{1} page:{2}", DateTime.Now, totalPage, curPage);

                        MatchCollection ms = CommonFun.GetValues(content, "<table class=\"grid off-table\"", "打印订单");

                        foreach (Match m in ms)
                        {
                            OrderItemInfo item = GetOrderItem(m.Value);

                            int orderId = Convert.ToInt32(item.ID);

                            if (orderId > lastOrderId)
                            {
                                string phoneNum = CommonFun.GetNum(item.PhoneNO);
                                if (phoneNum.Length == 11 && CanSend(item.Address))
                                {
                                    string sendMsg = string.Format(msg, item.Price, item.Count, item.UserName);
                                    item.GetOrderTime = sendMsg;

                                    CommonFun.SendMsg(phoneNum, sendMsg);
                                }

                                CommonFun.WriteCSV(fileName + "/sendMsg" + ticks + fileExtendName, item);

                                if (maxOrderId < orderId)
                                {
                                    maxOrderId = orderId;
                                }
                            }
                            else
                            {
                                isNoticeAll = true;
                                break;
                            }
                        }
                    } while (++curPage <= totalPage && !isNoticeAll);

                    if (maxOrderId != 0)
                    {
                        lastOrderId = maxOrderId;

                        UpdateAppConfig("orderIdKey", lastOrderId.ToString());
                    }
                    Console.WriteLine("{0} sleeping next......", DateTime.Now);
                    Thread.Sleep(60 * 1000);
                } while (true);

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
        public void Test()
        {
            //request.HttpGet("https://www.yaofangwang.com");
            //Login();

            //BaseItemInfo item = new BaseItemInfo();

            //UpdateItemInfo();
            //Login();
            //ReadPlatFormWebPageValue plateform = new ReadPlatFormWebPageValue();
            //List<BaseItemInfo> items = Seach315Item("国药准字Z20110022");

            //foreach (BaseItemInfo item in items)
            //{
            //    string key = item.Name + item.Format + item.Created;


            //    if (!ShopAllItems.ContainsKey(key))
            //    {
            //        ShopAllItems.Add(key, item);
            //        DoOpt(plateform, item);
            //    }
            //    //UpdateItemInfo(item.ItemName, item.ShopPrice, "2", item.Type, item.Inventory, item);
            //}


            ticks = DateTime.Now.Ticks;

            Login();

            //base.Start();

            ReadPlatFormWebPageValue platform = new ReadPlatFormWebPageValue();

            int totalCount = ShopAllItems.Count;
            int curCount = 0;
            //zreoUp636659701752803209.csv
            //ReadBaseItemInfo("zreoUp636665565920580975.csv", true);
            ShopAllItems = ReadXlsItems("zreoUp636665565920580975.xlsx");
            //UpFixedItem();
            Dictionary<string, List<BaseItemInfo>> sItems = new Dictionary<string, List<BaseItemInfo>>();
            foreach (BaseItemInfo item in ShopAllItems.Values)
            {
                Console.WriteLine("{0} TotalCount:{1} CurCount:{2}", DateTime.Now, totalCount, ++curCount);
                if (!sItems.ContainsKey(item.ID))
                {
                    List<BaseItemInfo> itemList = Seach315Item(item.ID);
                    sItems.Add(item.ID, itemList);
                }

                List<BaseItemInfo> items = sItems[item.ID];

                foreach (BaseItemInfo sItem in items)
                {
                    if (CommonFun.IsSameFormat(item.Format, sItem.Format, item.Name, sItem.Name))
                    {
                        DoOpt(platform, sItem);
                        break;
                    }
                }
            }
        }
        /// <summary>
        /// 更新商品信息
        /// </summary>
        /// <param name="mid"></param>
        /// <param name="price"></param>
        /// <param name="state">0：零售，1：批发，2：缺货3：预定</param>
        /// <param name="url"></param>
        /// <param name="stock"></param>
        /// <returns></returns>
        public bool UpdateItemInfo(string mid, decimal price, string state, string url, string stock, BaseItemInfo item)
        {
            try
            {
                int count = 0;
                bool update = false;

                do
                {
                    string updateUrl = "https://www.315jiage.cn/memberViewPrices.aspx";

                    string postData = string.Format("act=ajax&cmd=edit&mid={0}&price={1}&state={2}&url={3}&stock={4}", mid, price, state, string.IsNullOrEmpty(url) ? "" : url, stock);

                    string content = request.HttpPost(updateUrl, postData);

                    if (content != null && content.Contains("\"success\":1"))
                    {
                        update = true;
                    }
                    else
                    {
                        item.Remark = content;
                        Thread.Sleep(2000);
                    }
                } while (++count < 3 && !update);

                if (update)
                {
                    CommonFun.WriteCSV(fileName + "/Update" + ticks + ".csv", item);
                }
                else
                {
                    CommonFun.WriteCSV(fileName + "/UpdateFail" + ticks + ".csv", item);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return false;
        }

        private List<BaseItemInfo> Seach315Item(string id)
        {
            List<BaseItemInfo> items = new List<BaseItemInfo>();
            try
            {

                string url = string.Format("https://www.315jiage.cn/memberViewPrices.aspx?act=ajax&cmd=get&p=1&w=certification&k={0}&minStock=&maxStock=&hot=0&rnd=704117", id);

                string content = request.HttpGet(url, null, true);

                content = CommonFun.GetValue(content, "\"objs\":", "\"haveMore\":");

                MatchCollection ms = CommonFun.GetValues(content, "{", "}");

                foreach (Match m in ms)
                {
                    BaseItemInfo item = GetItem(m.Value);
                    Thread.Sleep(sleepTime);
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
        /// 更新固定表的价格
        /// </summary>
        private void UpdateFixedItem(BaseItemInfo item)
        {
            try
            {
                if (CanChangePrice(item))
                {
                    UpdateItemInfo(item.ItemName, item.ShopPrice - lPrice, item.SellType, item.Type, item.Inventory, item);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }


        private bool CanChangePrice(BaseItemInfo item)
        {
            try
            {
                string key = item.Name + item.Format + item.Created;

                if (CanUpDatePrice(key))
                {
                    decimal sale = (decimal)1;
                    decimal updatePrice = item.ShopPrice - lPrice;

                    if (sale < 1)
                    {
                        if (updatePrice > 0 && item.PlatformPrice * sale < updatePrice)
                        {
                            return true;
                        }
                    }
                    else
                    {
                        if (item.PlatformPrice != updatePrice)
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

        private void UpFixedItem()
        {
            try
            {
                foreach (KeyValuePair<string, BaseItemInfo> info in unUpdate)
                {
                    string key = info.Key;
                    BaseItemInfo item = info.Value;
                    try
                    {
                        // 搜索物品
                        List<BaseItemInfo> sItems = Seach315Item(item.ID);

                        foreach (BaseItemInfo sItem in sItems)
                        {
                            if (!CanUpCreated(sItem.Created))
                            {
                                if (sItem.SellType != "2")
                                {
                                    UpdateItemInfo(sItem.ItemName, sItem.PlatformPrice, "2", sItem.Type, "0", sItem);
                                }

                                continue;
                            }

                            if (CommonFun.IsSameFormat(item.Format, sItem.Format, item.Name, sItem.Name))
                            {
                                // 是否需要更新(没有上架或者不是不是平台最低价或者没有别标记)
                                if (sItem.SellType == "2" || CanChangePrice(sItem) || string.IsNullOrEmpty(sItem.Type))
                                {
                                    decimal updatePrice = CanChangePrice(sItem) ? sItem.ShopPrice - lPrice : sItem.PlatformPrice;

                                    string url = string.IsNullOrEmpty(item.Type) ? sItem.Type : item.Type;

                                    string stock = sItem.Inventory == "0" ? random.Next(30, 40).ToString() : sItem.Inventory;

                                    sItem.ShopPrice = updatePrice;
                                    sItem.Type = url;
                                    sItem.Inventory = stock;

                                    UpdateItemInfo(sItem.ItemName, sItem.ShopPrice, "0", sItem.Type, sItem.Inventory, sItem);
                                }

                                break;
                            }
                        }

                        CommonFun.WriteCSV(fileName + "/UpdateFixedItem" + ticks + fileExtendName, item);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("ID:{0} Error:{1}", item.ID, ex);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private decimal GetPrice(string infoUrl)
        {
            decimal minPrice = decimal.MaxValue;
            try
            {
                string refererUrl = url + "/" + infoUrl;
                string content = request.HttpGet(refererUrl, null, true);

                string priceInfo = CommonFun.GetValue(content, "suppliers.load", ";");

                if (!string.IsNullOrEmpty(priceInfo))
                {
                    priceInfo = priceInfo.Substring(1, priceInfo.Length - 2);

                    string[] infoArray = priceInfo.Split(',');

                    string priceUrl = string.Format("https://www.315jiage.cn/ajaxSuppliers.aspx?id={0}&cid={1}", infoArray[0], infoArray[1]);

                    Dictionary<string, string> heads = new Dictionary<string, string>();
                    heads.Add("Referer", refererUrl);

                    content = request.HttpGet(priceUrl, null, true, heads);

                    content = CommonFun.GetValue(content, "<tbody>", "</tbody>");

                    MatchCollection ms = CommonFun.GetValues(content, "<tr align=\"center\">", "</tr>");

                    foreach (Match m in ms)
                    {
                        MatchCollection dMs = CommonFun.GetValues(m.Value, "<td>", "</td>");

                        if (m.Value.Contains("<a href=\"") && !m.Value.Contains(selfStoreName))
                        {
                            try
                            {
                                decimal price = decimal.MaxValue;

                                if (dMs[1].Value.Contains("<"))
                                {
                                    if (dMs[1].Value.Contains("hidden supPrice"))
                                    {
                                        string dateStr = dMs[0].Value;
                                        string[] dateInfo = dateStr.Trim().Split('-');
                                        DateTime date = new DateTime(Convert.ToInt32(dateInfo[0]), Convert.ToInt32(dateInfo[1]), Convert.ToInt32(dateInfo[2]));
                                        string info = CommonFun.GetValue(dMs[1].Value, "<span class=\"hidden supPrice\">", "</span>");
                                        price = Convert.ToDecimal(info);

                                        price -= 100 * (decimal)(date.DayOfWeek);

                                        price /= 100;
                                    }
                                    else
                                    {
                                        string info = dMs[1].Value;

                                        int index = info.IndexOf('<');

                                        info = info.Substring(0, index);

                                        string saleStr = CommonFun.GetValue(dMs[1].Value, ">", "折");

                                        decimal sale = string.IsNullOrEmpty(saleStr) ? 10 : Convert.ToDecimal(saleStr);
                                        sale /= 10;
                                        price = Convert.ToDecimal(info) * sale;
                                        price = Math.Round(price, 2);
                                    }
                                }
                                else
                                {
                                    price = Convert.ToDecimal(dMs[1].Value);
                                }

                                if (minPrice > price)
                                {
                                    minPrice = price;
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("info:{0}, Errr:{1}", dMs[1].Value, ex);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            return minPrice == decimal.MaxValue ? 0 : minPrice;
        }

        private decimal GetOlnyPrice(decimal plateformPrice)
        {
            //15块钱以下的百分之40个点    15到30的百分之30  30到50百分之25    50以上百分之20
            if (plateformPrice > 50)
            {
                return Math.Round(plateformPrice * (decimal)1.2, 2);
            }
            else if (plateformPrice > 30)
            {
                return Math.Round(plateformPrice * (decimal)1.25, 2);
            }
            else if (plateformPrice > 15)
            {
                return Math.Round(plateformPrice * (decimal)1.3, 2);
            }
            else if (plateformPrice > 5)
            {
                return Math.Round(plateformPrice * (decimal)1.4, 2);
            }
            return Math.Round(plateformPrice * 2, 2);
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
                    //info.Type = platformItem.Type;
                    info.ViewCount = platformItem.ViewCount;

                    CommonFun.WriteCSV(fileName + "/50以上" + ticks + ".csv", info);
                    result = true;
                }
            }
            else if (compacePrice > 30)
            {
                if (compacePrice * (decimal)0.75 >= platformItem.ShopSelaPrice)
                {
                    info.PlatformPrice = platformItem.ShopSelaPrice;
                    //info.Type = platformItem.Type;
                    info.ViewCount = platformItem.ViewCount;

                    CommonFun.WriteCSV(fileName + "/30-50" + ticks + ".csv", info);
                    result = true;
                }
            }
            else if (compacePrice > 15)
            {
                if (compacePrice * (decimal)0.7 >= platformItem.ShopSelaPrice)
                {
                    info.PlatformPrice = platformItem.ShopSelaPrice;
                    //info.Type = platformItem.Type;
                    info.ViewCount = platformItem.ViewCount;

                    CommonFun.WriteCSV(fileName + "/15-30" + ticks + ".csv", info);
                    result = true;
                }
            }
            else if (compacePrice > 5)
            {
                if (compacePrice * (decimal)0.6 >= platformItem.ShopSelaPrice)
                {
                    info.PlatformPrice = platformItem.ShopSelaPrice;
                    //info.Type = platformItem.Type;
                    info.ViewCount = platformItem.ViewCount;

                    CommonFun.WriteCSV(fileName + "/15以下" + ticks + ".csv", info);
                    result = true;
                }
            }

            return result;
        }
    }
}
