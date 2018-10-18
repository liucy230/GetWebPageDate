using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Web;
using System.Text.RegularExpressions;
using System.Configuration;
using GetWebPageDate.Util.Item;

namespace GetWebPageDate.Util.ReadWebPage
{
    public class T12YaoReadWebPage : BaseReadWebPage
    {
        public Dictionary<string, string> menuList = new Dictionary<string, string>();

        private static long tick = DateTime.Now.Ticks;

        private string fileName = "TYaoFang/TYaoFang" + tick + ".csv";

        private string loginUrl = "http://www.12yao.com/pharmacy/CheckLogin.php";

        private string username;

        private string password;

        public T12YaoReadWebPage()
        {
            string userInfo = ConfigurationManager.AppSettings["12UAndP"];
            string[] aUserInfo = userInfo.Split(',');

            username = aUserInfo[0];
            password = aUserInfo[1];
        }

        public override void Login()
        {
            string postDataStr = string.Format("PharmacyCode={0}&PharmacyTitle={0}&PassWord={1}&action=login&rememberme=forever", username, password);
            request.Login(loginUrl, postDataStr);
        }


        /// <summary>
        /// 上架新品
        /// </summary>
        public void UpNewItem()
        {
            Login();

            BaseReadWebPage read = new BaseReadWebPage();
            read.ReadBaseItemInfo("TYaoFang/20以上636493584245833259--------------.csv", true);

            int count = 0;

            foreach (BaseItemInfo item in read.ShopAllItems.Values)
            {
                try
                {
                    if (count++ % 200 == 0)
                    {
                        Login();
                    }

                    int startIndex = item.ID.IndexOf('字');
                    startIndex = (startIndex > 0 ? startIndex : item.ID.IndexOf('号')) + 1;
                    string id = item.ID.Substring(startIndex, item.ID.Length - startIndex);

                    // 查找商品模板
                    string seachUrl = "http://www.12yao.com/pharmacy/ajax_productinfo.php";
                    string postDataStr = "action=getProductSearch&keywords={0}&searchType=1";

                    string data = string.Format(postDataStr, id);

                    string content = request.HttpPost(seachUrl, data, "http://www.12yao.com/pharmacy/productRelease.php?rootID=1", null, Encoding.GetEncoding("gb2312"));
                    MatchCollection ms = CommonFun.GetValues(content, "<li>", "</li>");

                    foreach (Match m in ms)
                    {
                        //对比格式
                        string format = CommonFun.GetValue(m.Value, "<b>", "</b>");

                        if (format == item.Format)
                        {
                            string productID = CommonFun.GetValue(m.Value, "value='", "'");
                            string postDataStrInfo = "action=getProductInfo&ProductID={0}";

                            content = request.HttpPost(seachUrl, string.Format(postDataStrInfo, productID));
                            // 保存价格数据
                            string saveUrl = "http://www.12yao.com/pharmacy/SaveProduct.php";

                            string postDataSave = "PreferentialPrice={1}&RetailPrice={1}&ProductStock={2}&Weight=6&ProductCode={0}&standardCode=&REPID=&Base_ProductID={0}&action=addproductrelease&Submit=%CB%D1%CB%F7";
                            Random random = new Random((int)DateTime.Now.Ticks);
                            content = request.HttpPost(saveUrl, string.Format(postDataSave, productID, item.PlatformPrice, random.Next(30, 40)));

                            //获取待上架列表
                            string willUpUrl = string.Format("http://www.12yao.com/pharmacy/ProductUnderFrameList.php?ProductName=&ReasonShelves=0&ProductBatch={0}&CompanyTitle=&ProductCode=&ProductType=0&time1=&time2=", HttpUtility.UrlEncode(item.ID, Encoding.GetEncoding("gb2312")).ToUpper());

                            content = request.HttpGet(willUpUrl, null, Encoding.GetEncoding("gb2312"));

                            bool isUp = false;

                            if (!string.IsNullOrEmpty(content))
                            {
                                MatchCollection upIdMs = CommonFun.GetValues(content, " <tr id=\"", " </tr>");

                                foreach (Match upIdM in upIdMs)
                                {
                                    string willUpFormat = CommonFun.GetValue(upIdM.Value, "<td class=\"dgui\">", "</td>");

                                    if (willUpFormat == item.Format)
                                    {
                                        string upId = CommonFun.GetValue(upIdM.Value, " value=\"", "\"");

                                        // 上架商品
                                        string upUrl = "http://www.12yao.com/pharmacy/ProductsUnderFrame.php";

                                        string postDataUp = "SelectPid={0}&action=setMultipleProductUpFrame";

                                        string reuslt = request.HttpPost(upUrl, string.Format(postDataUp, upId));

                                        isUp = true;
                                    }
                                }
                            }

                            if (!isUp)
                            {
                                CommonFun.WriteCSV("TYaoFang/faild" + tick + ".csv", item);
                            }
                        }
                    }

                }
                catch (Exception ex)
                {
                    CommonFun.WriteCSV("TYaoFang/faild" + tick + ".csv", item);
                    Console.WriteLine(ex);
                }
            }
        }
        public override void ReadAllMenuURL()
        {
            string menuUrl = "http://api.12yao.com/v1003/cats/list?j={0}&t=151306264 ";

            for (int i = 1; i <= 5; i += 2)
            {
                string content = request.HttpGet(string.Format(menuUrl, i));

                JObject jo = (JObject)JsonConvert.DeserializeObject(content);

                JArray jArray = (JArray)JsonConvert.DeserializeObject(jo["d"].ToString());

                for (int j = 0; j < jArray.Count; j++)
                {
                    string list = jArray[j]["j"].ToString();

                    if (!menuList.ContainsKey(list))
                    {
                        menuList.Add(list, list);
                    }
                }
            }
        }

        public override void ReadAllItem()
        {
            Dictionary<string, string> itemIDAndPrices = new Dictionary<string, string>();

            string pageListUrl = "http://api.12yao.com/v1003/products/list?j={0}&p1=&r={1}&t=151306281";

            string itemUrl = "http://api.12yao.com/v1003/products/pharmacy_list?g=0&id={0}&t=151306508";

            int totalCount = menuList.Count;
            int curCount = 0;
            foreach (string j in menuList.Keys)
            {
                try
                {
                    int r = 1;

                    string url = string.Format(pageListUrl, j, r);

                    string content = request.HttpGet(url);

                    JObject jo = (JObject)JsonConvert.DeserializeObject(content);

                    int totalPage = (int)Math.Ceiling(Convert.ToDouble(jo["d"]["k"]) / 20);

                    GetItemIDs(content, itemIDAndPrices);

                    for (r++; r <= totalPage; r++)
                    {
                        url = string.Format(pageListUrl, j, r);

                        content = request.HttpGet(url);

                        GetItemIDs(content, itemIDAndPrices);

                        Console.WriteLine("TotalPage:{0}, curPage:{1}", totalPage, r);
                    }

                    Console.WriteLine("TotalCount:{0}, CurCount:{1}", totalCount, ++curCount);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("err:{0}, list:{1}", ex.ToString(), j);
                }

            }

            foreach (string id in itemIDAndPrices.Keys)
            {
                string url = string.Format(itemUrl, id);

                string content = request.HttpGet(url);

                BaseItemInfo item = GetItemInfo(content, itemIDAndPrices[id]);

                if (item != null)
                {
                    string key = item.ID + item.Format + item.Created;
                    if (!ShopAllItems.ContainsKey(key))
                    {
                        ShopAllItems.Add(key, item);
                    }
                    else if (ShopAllItems[key].PlatformPrice > item.PlatformPrice)
                    {
                        ShopAllItems[key].PlatformPrice = item.PlatformPrice;
                    }
                }
                else
                {
                    Console.WriteLine("Item is null,id:{0}", id);
                }
            }

            foreach (BaseItemInfo item in ShopAllItems.Values)
            {
                CommonFun.WriteCSV(fileName, item);
            }
        }

        /// <summary>
        /// 获取物品id
        /// </summary>
        /// <param name="content"></param>
        /// <param name="itemIDs"></param>
        private void GetItemIDs(string content, Dictionary<string, string> itemIDs)
        {
            try
            {
                JObject jo = (JObject)JsonConvert.DeserializeObject(content);
                string value = jo["d"]["u"].ToString();
                JArray list = (JArray)JsonConvert.DeserializeObject(value);

                for (int i = 0; i < list.Count; i++)
                {
                    string id = list[i]["id"].ToString();

                    if (!itemIDs.ContainsKey(id))
                    {
                        itemIDs.Add(id, list[i]["ip"].ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("err:{0}, content:{1}", ex.ToString(), content);
            }
        }

        private BaseItemInfo GetItemInfo(string content, string price)
        {
            try
            {
                BaseItemInfo item = new BaseItemInfo();

                JObject jo = (JObject)JsonConvert.DeserializeObject(content);

                item.Name = jo["d"]["n"].ToString();
                item.Format = jo["d"]["e"].ToString();
                item.ID = jo["d"]["h"].ToString();
                item.PlatformPrice = Convert.ToDecimal(price);
                item.Created = jo["d"]["q"].ToString();
                item.ViewCount = jo["d"]["id"].ToString();
                return item;
            }
            catch (Exception ex)
            {
                Console.WriteLine("err:{0}, content:{1}", ex.ToString(), content);
            }

            return null;
        }

        /// <summary>
        /// 提取数据
        /// </summary>
        public override void Start()
        {
            ReadAllMenuURL();

            ReadAllItem();
        }

        /// <summary>
        /// 数据对比
        /// </summary>
        public void Compare()
        {
            ReadPlatFormWebPageValue flatform = new ReadPlatFormWebPageValue();
            BaseReadWebPage platform = new BaseReadWebPage();
            BaseReadWebPage shop = new BaseReadWebPage();
            platform.ReadBaseItemInfo("Platform/Platform636488666326049889.csv", true);
            shop.ReadBaseItemInfo("TYaoFang/NotIsEixst636493584245833259.csv", true);

            foreach (BaseItemInfo item in shop.ShopAllItems.Values)
            {
                bool isEixst = false;
                string itemName = item.Name;

                if (item.Name.Contains("】"))
                {
                    int itemNameIndex = item.Name.LastIndexOf("】") + 1;
                    itemName = item.Name.Substring(itemNameIndex, item.Name.Length - itemNameIndex);
                }

                if (itemName.Contains("("))
                {
                    int itemNameIndex = itemName.IndexOf('(');

                    itemName = itemName.Substring(0, itemNameIndex);
                }

                foreach (BaseItemInfo platformItem in platform.ShopAllItems.Values)
                {
                    bool isSame = false;
                    if (!string.IsNullOrEmpty(item.ID) && !string.IsNullOrEmpty(platformItem.ID))
                    {
                        if (item.ID.Trim() == platformItem.ID.Trim())
                        {
                            isSame = true;
                        }
                    }


                    if (isSame || (itemName == platformItem.Name && item.Created == platformItem.Created))
                    {
                        if (CommonFun.IsSameFormat(platformItem.Format, item.Format, platformItem.Name, item.Name))
                        {
                            isEixst = true;
                            if (ComparePrice(platformItem, item))
                            {
                                break;
                            }
                        }
                    }
                }

                //if (!isEixst)
                //{
                //    List<BaseItemInfo> seachItems = flatform.SeachInfoByID(item.ID);

                //    Dictionary<string, BaseItemInfo> saveInfos = new Dictionary<string, BaseItemInfo>();

                //    foreach (BaseItemInfo info in seachItems)
                //    {
                //        string key = info.Name + info.Format + info.Created;

                //        if (!platform.ShopAllItems.ContainsKey(key))
                //        {
                //            if (item.ID == info.ID || (itemName == info.Name && item.Created == info.Created))
                //            {
                //                if (CommonFun.IsSameFormat(info.Format, item.Format, info.Name, item.Name))
                //                {
                //                    isEixst = true;
                //                    ComparePrice(info, item);
                //                }
                //            }

                //            platform.ShopAllItems.Add(key, info);
                //            saveInfos.Add(key, info);
                //        }
                //        else if (platform.ShopAllItems[key].ShopSelaPrice > info.ShopSelaPrice)
                //        {
                //            if (item.ID == info.ID || (itemName == info.Name && item.Created == info.Created))
                //            {
                //                if (CommonFun.IsSameFormat(info.Format, item.Format, info.Name, item.Name))
                //                {
                //                    isEixst = true;
                //                    ComparePrice(info, item);
                //                }
                //            }

                //            platform.ShopAllItems[key] = info;
                //            saveInfos[key] = info;
                //        }
                //    }

                //    foreach (BaseItemInfo saveInfo in saveInfos.Values)
                //    {
                //        CommonFun.WriteCSV("Platform/Platform636488666326049889.csv", saveInfo);
                //    }
                //}

                if (!isEixst)
                {
                    CommonFun.WriteCSV("TYaoFang/NotIsEixst" + tick + ".csv", item);
                }
            }
        }

        private bool ComparePrice(BaseItemInfo platformItem, BaseItemInfo info)
        {
            if (info.PlatformPrice > 0 && info.PlatformPrice * (decimal)0.8 >= platformItem.ShopSelaPrice)
            {
                info.ShopPrice = platformItem.ShopSelaPrice;
                info.Type = platformItem.Type;

                CommonFun.WriteCSV("TYaoFang/20以上" + 636493584245833259 + ".csv", info);
                return true;
            }
            return false;
        }
    }
}
