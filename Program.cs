using GetWebPageDate.Http;
using GetWebPageDate.Util;
using GetWebPageDate.Util.Item;
using GetWebPageDate.Util.ReadWebPage;
using GetWebPageDate.Util.SyncStock;
using GetWebPageDate.Util.UpdatePrice;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace GetWebPageDate
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                #region 药途网
                ReadYaoTuWebPage yaoTuRead = new ReadYaoTuWebPage();
                //yaoTuRead.Start();
                //yaoTuRead.ComparePrice();
                #endregion

                #region 药房网数据提取操作
                ReadPlatFormWebPageValue readPlatform = new ReadPlatFormWebPageValue();
                //readPlatform.Test();
                //readPlatform.Start();
                //readPlatform.UpdatePrice();
                //readPlatform.OptOrder();
                //readPlatform.OptWaitingSend();
                #endregion

                #region tk上架操作
                ReadTKWebPageValue tkRead = new ReadTKWebPageValue();
                //tkRead.ReUpItems("TK/reup636663235433244307.csv");
                //tkRead.UpdatePriceAndDown();
                //tkRead.Test();
                //tkRead.ReadAllMenuURL();
                //tkRead.StartAutoGetOrder();
                //tkRead.Start();
                //tkRead.DownNoMoneyItem();
                //tkRead.UpNewByCSV();
                //tkRead.UpAllItem();
                //tkRead.Compare("Platform/Platform636523829470669326.csv", "TK/NotIsEixst636525934878630989.csv", "TK", true);
                //tkRead.ReadAllItem();
                //tkRead.ProcessOrder();
                //tkRead.GetOrderDetail();
                //TKUpdate update = new TKUpdate();
                //update.UpdateInventory();
                //update.UpAllNewItme();
                //update.ReUpItemByCSV("TKAdmin/down_reUp.csv");
                //update.StartAutoGetOrder();
                //update.GetDownListItem();
                //update.DownItem();
                //update.ReadeAllSellingItme();
                //update.Login();
                //CommonFun.Compare("Platform/Platform636488666326049889.csv", "TKAdmin/NotEixst.csv", "TKAdmin");
                //update.CarepareAndUpItem();
                #endregion

                #region 12药房数据提取操作
                T12YaoReadWebPage _12YaoRead = new T12YaoReadWebPage();
                // _12YaoRead.Compare();
                // _12YaoRead.Start();
                //_12YaoRead.UpNewItem();
                #endregion

                #region 鸿安数据提取操作
                HAReadWebPage haRead = new HAReadWebPage();
                //haRead.Compare(null, "HA/HA636510181846753529.csv", "HA");
                //haRead.GetAllItem();
                #endregion

                #region 1药网
                //string menuIndex = Console.ReadLine();
                YiYaoWebRead yiYaoread = new YiYaoWebRead();
                //yiYaoread.SyncYFItems();
                //yiYaoread.GetSellingItem("1601910842", DateTime.Now);
                //yiYaoread.Start();
                //yiYaoread.GetOrderDetail();
                yiYaoread.UpdatePrice();
                //yiYaoread.Test();
                //yiYaoread.MenuIndex = Convert.ToInt32(menuIndex);
                //yiYaoread.Start();
                //yiYaoread.Compare("Platform/Platform636523829470669326.csv", "YiYao/YiYao636519686372752317_0.csv", "YiYao");
                #endregion

                #region 八百方
                EightHundredF eHFRead = new EightHundredF();
                //eHFRead.Test();
                //eHFRead.Start();
                //eHFRead.Compare();
                #endregion

                # region 七乐
                QLReadWebPage qlRead = new QLReadWebPage();
                //qlRead.Start();
                //qlRead.Login();
                //qlRead.Test();
                #endregion

                #region 315
                T315ReadWebPage threeOneFive = new T315ReadWebPage();
                //threeOneFive.GetItemInfo();
                //threeOneFive.Start();
                //threeOneFive.Test();
                //threeOneFive.NoticeUserPay();
                #endregion

                #region 库存同步
                //CommonFun.UpdateXLS("KTUnUpdate.xlsx", new string[] { "出售方式（零或整）", "库存" }, new string[] { DateTime.Now.ToString(), "5" }, "剂型", "1601916959,1601912112");
                SyncItemStock sync = new SyncItemStock();
                //sync.Start();
                #endregion
                //CommonFun.ReadXLS("YaoTu/YaoTu636500822100643979.csv");

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            string info = "";

            do
            {
                info = Console.ReadLine();
            }
            while ("quit" != info);

        }

        private static Dictionary<string, BaseItemInfo> GetTKData()
        {
            ReadTKWebPageValue read = new ReadTKWebPageValue();

            read.ReadAllItem();

            return read.ShopAllItems;
        }

        private static void DeleteRepeatData(string filePathName)
        {
            Dictionary<string, ItemInfo> items = new Dictionary<string, ItemInfo>();

            string content = CommonFun.ReadCSV(filePathName);

            string[] lines = content.Split('\r');

            int repeatCount = 0;
            foreach (string line in lines)
            {
                try
                {
                    if (line.Contains('\n'))
                    {
                        string[] infoStr = line.Split(',');
                        int index = 0;
                        if (infoStr.Length > 1)
                        {
                            ItemInfo info = new ItemInfo();
                            info.ID = infoStr[index++].Substring(1);
                            info.Name = infoStr[index++];
                            // info.SellType = infoStr[index++];


                            if (infoStr.Length > 7)
                            {
                                string created = "";

                                for (int i = 0; i <= infoStr.Length - 7; i++)
                                {
                                    created += (infoStr[index++] + ",");
                                }

                                info.Created = created.Substring(0, created.LastIndexOf(','));
                            }
                            else
                            {
                                info.Created = infoStr[index++];
                            }
                            info.Format = infoStr[index++];
                            info.ShopPrice = Convert.ToDecimal(infoStr[index++]);
                            //info.PlatformPrice = Convert.ToDecimal(infoStr[index++]);
                            info.Type = infoStr[index++];
                            //info.Weight = Convert.ToInt32(infoStr[index++]);
                            //info.Inventory = infoStr[index++];
                            info.ViewCount = infoStr[index++];

                            string key = info.ID + "{" + info.Format + "}" + "{" + info.Created + "}";
                            if (items.ContainsKey(key))
                            {
                                repeatCount++;
                            }
                            else
                            {
                                items.Add(key, info);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }

            Console.WriteLine("RepeatCount:{0}", repeatCount);

            foreach (ItemInfo info in items.Values)
            {
                CommonFun.WriteCSV("DeleteRepeat.csv", info);
            }
        }

        private static void Compare(ItemInfo platformItem, ItemInfo info)
        {
            if (platformItem.ShopPrice * (decimal)1.25 <= info.ShopPrice)
            {
                info.PlatformPrice = platformItem.ShopPrice;
                info.ViewCount = platformItem.ViewCount;
                info.Type = platformItem.Type;

                CommonFun.WriteCSV("25以上.csv", info);
            }
            else if (platformItem.ShopPrice * (decimal)1.1 < info.ShopPrice
                       && platformItem.ShopPrice * (decimal)1.25 > info.ShopPrice)
            {
                info.PlatformPrice = platformItem.ShopPrice;
                info.ViewCount = platformItem.ViewCount;
                info.Type = platformItem.Type;

                CommonFun.WriteCSV("10-25.csv", info);
            }
            else if (platformItem.ShopPrice >= info.ShopPrice
                && platformItem.ShopPrice * (decimal)1.1 > info.ShopPrice)
            {
                info.PlatformPrice = platformItem.ShopPrice;
                info.ViewCount = platformItem.ViewCount;
                info.Type = platformItem.Type;

                CommonFun.WriteCSV("10-0以下.csv", info);
            }
        }

        private static void CompareData(Dictionary<string, ItemInfo> kadData)
        {
            Dictionary<string, ItemInfo> tkData = GetData("TK-汇总.csv");

            Dictionary<string, ItemInfo> plateData = GetData("test.csv");

            Dictionary<string, List<ItemInfo>> tkDatas = new Dictionary<string, List<ItemInfo>>();

            Dictionary<string, List<ItemInfo>> plateDatas = new Dictionary<string, List<ItemInfo>>();
            foreach (ItemInfo info in tkData.Values)
            {
                if (tkDatas.ContainsKey(info.ID))
                {
                    tkDatas[info.ID].Add(info);
                }
                else
                {

                    tkDatas.Add(info.ID, new List<ItemInfo>() { info });
                }
            }


            foreach (ItemInfo info in plateData.Values)
            {
                if (plateDatas.ContainsKey(info.ID))
                {
                    plateDatas[info.ID].Add(info);
                }
                else
                {

                    plateDatas.Add(info.ID, new List<ItemInfo>() { info });
                }
            }

            foreach (ItemInfo info in kadData.Values)
            {
                bool isSame = false;

                if (tkDatas.ContainsKey(info.ID))
                {
                    foreach (ItemInfo item in tkDatas[info.ID])
                    {
                        if (IsSameFormat(info.Format, item.Format))
                        {
                            isSame = true;
                            break;
                        }
                    }

                    if (isSame)
                    {
                        continue;
                    }
                }


                if ((info.ID.Contains("国药准字") || info.ID.Contains("注册证号") || info.ID.Contains("国食")))
                {
                    if (plateDatas.ContainsKey(info.ID))
                    {
                        foreach (ItemInfo item in plateDatas[info.ID])
                        {
                            if (IsSameFormat(info.Format, item.Format))
                            {
                                info.ShopPrice = item.ShopPrice;
                                info.ViewCount = item.ViewCount;
                            }
                        }
                        //ItemInfo info = plateData[key];
                        //kadData[key].PlatformPrice = info.ShopPrice;
                        //kadData[key].ViewCount = info.ViewCount;
                    }

                    CommonFun.WriteCSV("360Kad_Compare_3.csv", info);
                }
            }
        }

        private static void GetCompareData(Dictionary<string, ItemInfo> items)
        {
            ReadPlatFormWebPageValue platformRead = new ReadPlatFormWebPageValue();

            Dictionary<string, List<ItemInfo>> platformItems = GetDataDicByID("DeleteRepeat.csv");

            foreach (ItemInfo info in items.Values)
            {
                bool isSame = false;

                if (platformItems.ContainsKey(info.ID))
                {
                    foreach (ItemInfo item in platformItems[info.ID])
                    {
                        if (IsSameFormat(info.Format, item.Format))
                        {
                            isSame = true;
                            Compare(item, info);
                        }
                    }
                }
                else
                {
                    //Dictionary<string, ItemInfo> seachDicItems = new Dictionary<string, ItemInfo>();
                    //List<ItemInfo> seachItems = platformRead.SeachInfoByID(info.ID);

                    //foreach (ItemInfo item in seachItems)
                    //{
                    //    string key = item.ID + "{" + item.Format + "}";

                    //    if (seachDicItems.ContainsKey(key))
                    //    {
                    //        if (seachDicItems[key].ShopPrice > item.ShopPrice)
                    //        {
                    //            seachDicItems[key] = item;
                    //        }
                    //    }
                    //    else
                    //    {
                    //        seachDicItems.Add(key, item);
                    //    }

                    //    CommonFun.WriteCSV("DeleteRepeat.csv", item);
                    //}

                    //platformItems.Add(info.ID, seachDicItems.Values.ToList());

                    //if (platformItems.ContainsKey(info.ID))
                    //{
                    //    foreach (ItemInfo item in platformItems[info.ID])
                    //    {
                    //        if (IsSameFormat(info.Format, item.Format))
                    //        {
                    //            isSame = true;
                    //            Compare(item, info);
                    //        }
                    //    }
                    //}
                }

                if (!isSame)
                {
                    CommonFun.WriteCSV("Format.csv", info);
                }
            }
        }

        private static Dictionary<string, List<ItemInfo>> GetDataDicByID(string filePathName)
        {
            Dictionary<string, List<ItemInfo>> result = new Dictionary<string, List<ItemInfo>>();

            List<ItemInfo> items = GetItemInfoList(filePathName);

            foreach (ItemInfo item in items)
            {
                if (result.ContainsKey(item.ID))
                {
                    result[item.ID].Add(item);
                }
                else
                {
                    result.Add(item.ID, new List<ItemInfo>() { item });
                }
            }

            return result;
        }

        /// <summary>
        /// 获取列名索引号
        /// </summary>
        /// <param name="name"></param>
        /// <param name="menu"></param>
        /// <returns></returns>
        private static int GetMenuIndex(string name, string[] menu)
        {
            for (int i = 0; i < menu.Length; i++)
            {
                if (menu[i] == name)
                {
                    return i;
                }
            }
            return -1;
        }

        private static List<ItemInfo> GetItemInfoList(string filePathName)
        {
            List<ItemInfo> items = new List<ItemInfo>();

            string content = CommonFun.ReadCSV(filePathName);

            string[] lines = content.Split('\r');

            string[] menu = null;
            foreach (string line in lines)
            {
                try
                {
                    if (!line.Contains('\n'))
                    {
                        menu = line.Split(',');
                    }
                    else
                    {
                        string created = CommonFun.GetValue(line, "\"", "\"");

                        string lineStr = line;

                        if (!string.IsNullOrEmpty(created))
                        {
                            lineStr = line.Replace("\"" + created + "\"", "");
                        }

                        string[] infoStr = lineStr.Split(',');

                        try
                        {
                            if (infoStr.Length > 1)
                            {
                                ItemInfo info = new ItemInfo();

                                string[] menuName = info.GetLogHeadLine().Split(',');

                                foreach (string name in menuName)
                                {
                                    int index = GetMenuIndex(name, menu);

                                    if (index != -1)
                                    {
                                        if (name == "批准文号")
                                        {
                                            info.ID = infoStr[index].Replace("\n", "");
                                        }
                                        else if (name == "通用名称")
                                        {
                                            info.Name = infoStr[index];
                                        }
                                        else if (name == "商品名称")
                                        {
                                            info.ItemName = infoStr[index];
                                        }
                                        else if (name == "出售方式（零或整）")
                                        {
                                            info.SellType = infoStr[index];
                                        }
                                        else if (name == "生产厂家")
                                        {
                                            //if (infoStr.Length > menuName.Length)
                                            //{
                                            //    string created = "";
                                            //    for (int i = 0; i <= infoStr.Length - menuName.Length; i++)
                                            //    {
                                            //        created += (infoStr[index++] + ",");
                                            //    }
                                            //    info.Created = created.Substring(0, created.LastIndexOf(','));
                                            //}
                                            //else
                                            //{
                                            //    info.Created = infoStr[index];
                                            //}
                                            if (string.IsNullOrEmpty(created))
                                            {
                                                info.Created = infoStr[index];
                                            }
                                            else
                                            {
                                                info.Created = created;
                                            }
                                        }
                                        else if (name == "包装规格")
                                        {
                                            info.Format = infoStr[index];
                                        }
                                        else if (name == "商城售价(最低价格)")
                                        {
                                            string p = infoStr[index];
                                            info.ShopPrice = string.IsNullOrEmpty(p) ? 0 : Convert.ToDecimal(p);
                                        }
                                        else if (name == "平台售价（最低价格）")
                                        {
                                            string p = infoStr[index];
                                            info.PlatformPrice = string.IsNullOrEmpty(p) ? 0 : Convert.ToDecimal(p);
                                        }
                                        else if (name == "剂型")
                                        {
                                            info.Type = infoStr[index];
                                        }
                                        else if (name == "重量（克）")
                                        {
                                            string p = infoStr[index];
                                            try
                                            {
                                                //info.Weight = string.IsNullOrEmpty(p) ? 0 : Convert.ToInt32(p);
                                            }
                                            catch (Exception ex)
                                            {
                                                Console.WriteLine("Error:{0}, Value:{1}", ex, p);
                                            }
                                        }
                                        else if (name == "库存")
                                        {
                                            info.Inventory = infoStr[index];
                                        }
                                        else if (name == "最近浏览")
                                        {
                                            info.ViewCount = infoStr[index];
                                        }
                                        else if (name == "一级菜单")
                                        {
                                            info.Menu1 = infoStr[index];
                                        }
                                        else if (name == "二级菜单")
                                        {
                                            info.Menu2 = infoStr[index];
                                        }
                                        else if (name == "三级菜单")
                                        {
                                            info.Menu3 = infoStr[index];
                                        }
                                        else if (name == "品牌名")
                                        {
                                            info.BrandName = infoStr[index];
                                        }
                                        else if (name == "药品属性")
                                        {
                                            info.DrugProtery = infoStr[index];
                                        }
                                        else if (name == "药品分类")
                                        {
                                            info.DrugType = infoStr[index];
                                        }
                                        else if (name == "功能主治")
                                        {
                                            info.Function = infoStr[index];
                                        }
                                        else if (name == "储藏方法")
                                        {
                                            info.SaveType = infoStr[index];
                                        }
                                        else if (name == "主要成分")
                                        {
                                            info.Basis = infoStr[index];
                                        }
                                        else if (name == "性状")
                                        {
                                            info.Character = infoStr[index];
                                        }
                                        else if (name == "用法用量")
                                        {
                                            info.Use = infoStr[index];
                                        }
                                        else if (name == "不良反应")
                                        {
                                            info.AdverseReaction = infoStr[index];
                                        }
                                        else if (name == "禁忌症")
                                        {
                                            info.Contraindication = infoStr[index];
                                        }

                                        else if (name == "注意事项")
                                        {
                                            info.NoticMatters = infoStr[index];
                                        }
                                        else if (name == "图片路径")
                                        {
                                            info.PicturePath = infoStr[index];
                                        }
                                    }
                                }

                                items.Add(info);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Error:{0}, Info:{1}", ex.ToString(), line);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }

            return items;
        }

        private static Dictionary<string, ItemInfo> GetData(string filePathName)
        {
            Dictionary<string, ItemInfo> dicItems = new Dictionary<string, ItemInfo>();

            List<ItemInfo> items = GetItemInfoList(filePathName);

            foreach (ItemInfo item in items)
            {
                string key = item.ID + "{" + item.Format + "}";
                if (dicItems.ContainsKey(key))
                {
                    if (dicItems[key].ShopPrice != 0 && dicItems[key].ShopPrice > item.ShopPrice)
                    {
                        dicItems[key] = item;
                    }
                }
                else
                {
                    dicItems.Add(key, item);
                }
            }

            return dicItems;
        }



        private static void GetShopData()
        {
            ReadShopWebPageValue read = new ReadShopWebPageValue();

            read.ReadAllMenuURL();

            read.ReadAllItem();
        }

        private static Dictionary<string, BaseItemInfo> Get360KadData()
        {
            Read360KadWebPageValue read = new Read360KadWebPageValue();

            read.ReadAllMenuURL();

            read.ReadAllItem();

            return read.ShopAllItems;
        }

        private static void GetPlatformData()
        {
            ReadPlatFormWebPageValue platformRead = new ReadPlatFormWebPageValue();

            platformRead.ReadAllMenuURL();

            platformRead.ReadAllItem();

        }

        private static bool IsSameFormat(string format1, string format2)
        {
            string[] array1 = format1.Split('/');

            string[] array2 = format2.Split('/');

            if (array1.Length == 2 && array2.Length == 2)
            {

                if (array1[0] == array2[0] && array1[1] == array2[1])
                {
                    return true;
                }
            }
            else if (array1[0] == array2[0])
            {
                return true;
            }

            return false;
        }
    }
}
