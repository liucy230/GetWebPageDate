using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace GetWebPageDate.Util.ReadWebPage
{
    public class QLReadWebPage : BaseReadWebPage
    {
        public QLReadWebPage()
        {
            password = ConfigurationManager.AppSettings["qlP"];
        }

        public override void Login()
        {
            base.Login();

            try
            {
                string loginKey = GetLoginKey();

                if (!string.IsNullOrEmpty(loginKey))
                {
                    string loginUrl = "http://ad.dabai.7lk.com/login";
                    string loginPostData = string.Format("password={0}&doctorId=321968&loginKey=BEpHCnHIAB321968&deviceSN=868805030552379", password);

                    string content = request.HttpPost(loginUrl, loginPostData);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public void Test()
        {
            try
            {
                string url = "http://ad.dabai.7lk.com/findMedication/patCategories";

                string postData = "doctorId=321967&userId=321967&token=9d571f1dc32cadf0ea38";

                string content = request.HttpPost(url, postData);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
        private string GetLoginKey()
        {
            string loginKeyUrl = "http://ad.dabai.7lk.com/login/genLoginKey";

            string loginPostData = "phoneNum=18569117890";

            string content = request.HttpPost(loginKeyUrl, loginPostData);

            return content;
        }

        public override bool ComparePrice(BaseItemInfo platformItem, BaseItemInfo info)
        {
            //15块钱以下的百分之40个点    15到30的百分之30  30到50百分之25    50以上百分之20
            decimal compacePrice = platformItem.ShopSelaPrice;
            decimal infoPrice = info.ShopPrice;
            bool result = false;
            if (compacePrice > 50)
            {
                if (compacePrice * (decimal)0.8 >= infoPrice)
                {
                    info.PlatformPrice = platformItem.ShopSelaPrice;
                    info.Type = platformItem.Type;
                    info.ViewCount = platformItem.ViewCount;

                    CommonFun.WriteCSV("QLK/50以上" + ticks + ".csv", info);
                    result = true;
                }
            }
            else if (compacePrice > 30)
            {
                if (compacePrice * (decimal)0.75 >= infoPrice)
                {
                    info.PlatformPrice = platformItem.ShopSelaPrice;
                    info.Type = platformItem.Type;
                    info.ViewCount = platformItem.ViewCount;

                    CommonFun.WriteCSV("QLK/30-50" + ticks + ".csv", info);
                    result = true;
                }
            }
            else if (compacePrice > 15)
            {
                if (compacePrice * (decimal)0.7 >= infoPrice)
                {
                    info.PlatformPrice = platformItem.ShopSelaPrice;
                    info.Type = platformItem.Type;
                    info.ViewCount = platformItem.ViewCount;

                    CommonFun.WriteCSV("QLK/15-30" + ticks + ".csv", info);
                    result = true;
                }
            }
            else if (compacePrice > 5)
            {
                if (compacePrice * (decimal)0.6 >= infoPrice)
                {
                    info.PlatformPrice = platformItem.ShopSelaPrice;
                    info.Type = platformItem.Type;
                    info.ViewCount = platformItem.ViewCount;

                    CommonFun.WriteCSV("QLK/15以下" + ticks + ".csv", info);
                    result = true;
                }
            }

            return result;
        }

        public override void ReadAllMenuURL()
        {
            try
            {
                string url = "http://ad.dabai.7lk.com/findMedication/patCategories";

                string postData = "doctorId=321967&userId=321967&token=9d571f1dc32cadf0ea38";

                string content = request.HttpPost(url, postData);

                MatchCollection ms = CommonFun.GetValues(content, "\"children\":", "\"sortable\":");

                foreach (Match m in ms)
                {
                    MatchCollection mMs = CommonFun.GetValues(m.Value, "{", "}");

                    foreach (Match mM in mMs)
                    {
                        string pMenuName = CommonFun.GetValue(mM.Value, "\"parent\":\"", "\"");
                        string sMenuName = CommonFun.GetValue(mM.Value, "\"title\":\"", "\"");
                        Console.WriteLine("pMenuName:{0}, sMenuName:{1}", pMenuName, sMenuName);
                        string menuData = string.Format("doctorId=321967&firTitle={0}&orderBy=default&secTitle={1}&userId=321967&token=9d571f1dc32cadf0ea38", HttpUtility.UrlEncode(pMenuName), HttpUtility.UrlEncode(sMenuName));
                        if (!AllMenuUrl.Contains(menuData))
                        {
                            AllMenuUrl.Add(menuData);
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
            try
            {
                foreach (string id in AllItemUrl)
                {
                    try
                    {
                        string itemPriceUrl = "http://ad.dabai.7lk.com/custom/price/info";

                        string itemInfoUrl = "https://ad.dabai.7lk.com/medication/detail";

                        string itemInfoPostData = "pid={0}&doctorId=321967&token=9d571f1dc32cadf0ea38";

                        string itemPricePostData = "doctorId=321967&userId=321967&skuId={0}&token=9d571f1dc32cadf0ea38";

                        string content = request.HttpPost(itemInfoUrl, string.Format(itemInfoPostData, id));

                        BaseItemInfo item = GetItem(content);

                        string skuId = CommonFun.GetValue(content, "\"skuId\":", ",");

                        content = request.HttpPost(itemPriceUrl, string.Format(itemPricePostData, skuId));

                        string priceStr = CommonFun.GetValue(content, "\"priceMin\":", ",");

                        item.ShopPrice = string.IsNullOrEmpty(priceStr) ? 0 : Convert.ToDecimal(priceStr) / 100;

                        string key = item.ID + "{" + item.Format + "}";

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

                        CommonFun.WriteCSV("QLK/QL" + ticks + ".csv", item);
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

        private BaseItemInfo GetItem(string content)
        {
            try
            {
                BaseItemInfo item = new BaseItemInfo();
                item.ID = CommonFun.GetValue(content, "\"approvalNum\":\"", "\"");
                item.Created = CommonFun.GetValue(content, "\"manufacturer\":\"", "\"");
                item.Format = CommonFun.GetValue(content, "\"packaging\":\"", "\"");
                item.Name = CommonFun.GetValue(content, "\"commonName\":\"", "\"");
                item.ItemName = CommonFun.GetValue(content, "\"name\":\"", "\"");
                item.ViewCount = CommonFun.GetValue(content, "\"skuId\":", ",");
                return item;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            return null;
        }
        public override void ReadAllItemURL()
        {
            try
            {
                string muenUrl = "http://ad.dabai.7lk.com/medication/search";
                foreach (string menuInfo in AllMenuUrl)
                {
                    int page = 1;
                    int totalPage = 0;
                    do
                    {
                        string content = request.HttpPost(muenUrl, menuInfo + string.Format("&page={0}", page));

                        MatchCollection ms = CommonFun.GetValues(content, "\"id\":", ",");

                        if (totalPage == 0)
                        {
                            string totalPageStr = CommonFun.GetValue(content, "\"totalPages\":", ",");
                            totalPage = Convert.ToInt32(totalPageStr);
                        }

                        foreach (Match m in ms)
                        {
                            if (!AllItemUrl.Contains(m.Value))
                            {
                                AllItemUrl.Add(m.Value);
                            }
                        }
                    } while (++page <= totalPage);
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
                ticks = DateTime.Now.Ticks;

                base.Start();

                //对比数据
                ReadPlatFormWebPageValue flatform = new ReadPlatFormWebPageValue();

                int totalCount = ShopAllItems.Count, curCount = 0;
                foreach (KeyValuePair<string, BaseItemInfo> info in ShopAllItems)
                {
                    try
                    {
                        Console.WriteLine("{0},totalCount:{1}, curCount:{2}", DateTime.Now.ToString(), totalCount, ++curCount);

                        if ((DateTime.Now - startTime).TotalMinutes > 30)
                        {
                            //Login();
                            flatform.Login();
                            startTime = DateTime.Now;
                        }


                        BaseItemInfo item = info.Value;

                        string key = item.Name + item.Format + item.Created;

                        string seachInfo = item.ID;

                        //查找该商品
                        if (!seachedItemID.ContainsKey(seachInfo))
                        {
                            seachedItemID.Add(seachInfo, item.Name);

                            List<BaseItemInfo> item_list = flatform.SeachInfoByID(seachInfo);

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
                                if (CommonFun.IsSameFormat(item.Format, compareItem.Format, item.Name, compareItem.Name))
                                {
                                    ComparePrice(compareItem, item);
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
    }
}
