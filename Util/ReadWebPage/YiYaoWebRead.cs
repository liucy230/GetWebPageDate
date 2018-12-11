using GetWebPageDate.Util.Item;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Media;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace GetWebPageDate.Util.ReadWebPage
{
    public class YiYaoWebRead : BaseReadWebPage
    {

        //www.111.com.cn/product/51026872.html
        private int menuIndex = -1;

        private string name = "百寿康药房";

        //private string titleName = "二十四小时内发货";

        //private string autoUpDownTitleName = "二十四小时内发货.";

        // 下架时间
        //private string downTime = "7:30-18:30";

        private bool isDown = false;

        private bool isUp = false;
        // 需要定时下架的列表
        private List<BaseItemInfo> autoDownItems = new List<BaseItemInfo>();

        // 需要定时上架的列表
        private List<BaseItemInfo> autoUpItems = new List<BaseItemInfo>();

        private SoundPlayer s = new SoundPlayer("4331.wav");

        private string fileName = "YiYao/";

        private string username;

        private List<string> testId = new List<string>() { "国药准字H20055465", "国药准字Z10920030"};

        public YiYaoWebRead()
        {
            username = ConfigurationManager.AppSettings["yyU"];
        }

        /// <summary>
        /// 是否在测试列表中
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private bool IsInTest(string id)
        {
            return testId.Contains(id);
        }

        /// <summary>
        /// 药房所有物品
        /// </summary>
        private Dictionary<string, List<BaseItemInfo>> pItmes = new Dictionary<string, List<BaseItemInfo>>();

        public int MenuIndex
        {
            get { return menuIndex; }
            set { menuIndex = value; }
        }

        public override void Login()
        {
            request.Cookie = string.Format("yao_IDENTIFYING=63bb0df60a674a04b46f0120ba5d25098ic; hasPwd=1; fpdMobile={0}; token=0606c486310e418e97d66316986c78e0; nickName={0}; newtoken=WEkrakpTa3pwZUh2S2tHVWxjdEN2dWdZTkVidEtZRTc4SU9rQkVtV3NDWWt2NWFuVDBkaHJGZnIwTWtpZUFZVWl3akdBZnYrUXVuUjNmUlREK2tZd0NYN0ZkWVd3ZVQ0b0t5VnpQU1ErL2phRTNtZi8xQlFVdURLSk82NXdVK0ZBT3hsS1cxcHVPKzRhcm03cVhjYW8wNmh3Zk5BUU0rOEVkR0JobnplY01rR2lkanh3cXF0bWx1T09MK1o4bFZFP2FwcElkPTExMjcma2V5SWQ9MTEyNw%3D%3D; JUN={0}; UserInfo=UserId%3D160988607%26UserName%3D20171225190108_I3W6Y0X7TW%26NickName%3D{0}%26Token%3D0606c486310e418e97d66316986c78e0%26Security%3D%2B9iqRLqX6x0z0jfOldNpGg%3D%3D%26userLeverId%3D1%26userType%3D0; JUM=20171225190108_I3W6Y0X7TW; userId=160988607; userName=20171225190108_I3W6Y0X7TW; security=\"+9iqRLqX6x0z0jfOldNpGg==\"; gltoken=WEkrakpTa3pwZUh2S2tHVWxjdEN2dWdZTkVidEtZRTc4SU9rQkVtV3NDWWt2NWFuVDBkaHJGZnIwTWtpZUFZVWl3akdBZnYrUXVuUjNmUlREK2tZd0NYN0ZkWVd3ZVQ0b0t5VnpQU1ErL2phRTNtZi8xQlFVdURLSk82NXdVK0ZBT3hsS1cxcHVPKzRhcm03cVhjYW8wNmh3Zk5BUU0rOEVkR0JobnplY01rR2lkanh3cXF0bWx1T09MK1o4bFZFP2FwcElkPTExMjcma2V5SWQ9MTEyNw%3D%3D; tan_status=0; yz_token=2190774_f210e8cbe39385b8cbcbe1123476ec15_1519786367098; mobile={0}; JUD=160988607; phone={0}; email=\"\"; login_uname={0}; JSESSIONID=7131A9F037F3EEA766B663A23A21EBCE", username);
        }

        /// <summary>
        /// 获取订单详情
        /// </summary>
        public void GetOrderDetail()
        {
            try
            {
                Login();

                int page = 1;
                int totalPage = 0;
                do
                {
                    string url = "http://popadmin.111.com.cn/admin/order/findOrder.action";
                    string postData = string.Format("queryBean.orderDateBegin=&queryBean.orderDateEnd=&queryBean.paymentDateBegin=&queryBean.paymentDateEnd=&queryBean.childOrderId=&queryBean.customerName=&queryBean.consigneeTelphone=&queryBean.goodsCode=&queryBean.goodsName=&queryBean.timeoutStay=0&queryBean.sorting=3&queryBean.type=11&queryBean.pageSize=10&queryBean.pageNo={0}&queryBean.consigneeName=&queryBean.waybillCode=&pageIndex={1}&pageShowAmount=10", page, page);

                    string content = request.HttpPost(url, postData);

                    if (totalPage == 0)
                    {
                        totalPage = Convert.ToInt32(CommonFun.GetValue(content, "pageCount\":\"", "\""));
                    }

                    Console.WriteLine("Getting...... totalPage:{0},curPage{1}", totalPage, page);

                    MatchCollection ms = CommonFun.GetValues(content, "\"carrierId\":", "\"yesShipped\":");

                    foreach (Match m in ms)
                    {
                        BaseItemInfo item = GetDetailInfo(m.Value);

                        if (item != null)
                        {
                            CommonFun.WriteCSV(fileName + "OrderDetail" + dateStr + ".csv", item);
                        }
                    }

                }
                while (++page <= totalPage);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private BaseItemInfo GetDetailInfo(string info)
        {
            try
            {
                OrderItemInfo item = new OrderItemInfo();

                item.GetOrderTime = CommonFun.GetValue(info, "\"orderDate\":\"", "\"");

                string addrInfo = CommonFun.GetValue(info, "\"provinceName\":\"", "\"");
                addrInfo += CommonFun.GetValue(info, "\"cityName\":\"", "\"");
                addrInfo += CommonFun.GetValue(info, "\"areaName\":\"", "\"");
                addrInfo += CommonFun.GetValue(info, "\"addresss\":\"", "\"");

                item.UserName = CommonFun.GetValue(info, "\"consignee\":\"", "\"");
                item.PhoneNO = CommonFun.GetValue(info, "\"consigneeMobile\":\"", "\"");
                item.Address = addrInfo;

                string itemInfo = CommonFun.GetValue(info, "\"goodsName\":\"", "\"");
                itemInfo = itemInfo.Trim();
                string[] arryItemInfo = itemInfo.Split(' ');

                if (arryItemInfo.Length == 1)
                {
                    item.Name = arryItemInfo[0];
                }
                else
                {
                    item.Name = arryItemInfo[arryItemInfo.Length - 2];
                    item.Format = arryItemInfo[arryItemInfo.Length - 1];
                }

                item.Price = CommonFun.GetValue(info, "\"salesPrice\":", ",");
                item.Count = CommonFun.GetValue(info, "\"rmaCount\":", ",");

                return item;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return null;
        }

        /// <summary>
        /// 获取历史数据
        /// </summary>
        /// <returns></returns>
        private Dictionary<string, BaseItemInfo> ReadHistoryItems()
        {
            Dictionary<string, BaseItemInfo> items = new Dictionary<string, BaseItemInfo>();
            DataTable data = CommonFun.ReadXLS(fileName + "history.xls", "info");
            if (data != null)
            {
                foreach (DataRow row in data.Rows)
                {
                    try
                    {
                        BaseItemInfo item = new BaseItemInfo();

                        PropertyInfo[] propertyArray = item.GetType().GetProperties();

                        string[] headArray = item.GetLogHeadLine().Split(',');

                        for (int i = 0; i < propertyArray.Length; i++)
                        {
                            try
                            {
                                string value = row[i].ToString();

                                object setValue = Convert.ChangeType(value, propertyArray[i].PropertyType);

                                propertyArray[i].SetValue(item, setValue);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex);
                            }
                        }
                        //string key = item.Name + item.Format + item.Created;
                        string key = item.ViewCount;
                        if (!items.ContainsKey(key))
                        {
                            items.Add(key, item);
                        }
                        else
                        {
                            Console.WriteLine("ReadHistoryTtems have same one........");
                        }

                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                }
            }


            return items;
        }

        /// <summary>
        /// 移除药房已下架的商品
        /// </summary>
        /// <param name="yfSellingItems"></param>
        /// <param name="historyItems"></param>
        /// <returns></returns>
        private Dictionary<string, BaseItemInfo> RemoveHistoryItem(Dictionary<string, BaseItemInfo> yfSellingItems, Dictionary<string, BaseItemInfo> historyItems)
        {
            Dictionary<string, BaseItemInfo> items = new Dictionary<string, BaseItemInfo>();
            for (int i = historyItems.Count; i < 0; i--)
            {
                BaseItemInfo item = historyItems.Values.ToArray()[i];
                string key = item.Name + item.Format + item.Created;

                if (!yfSellingItems.ContainsKey(key))
                {
                    items.Add(key, item);
                    historyItems.Remove(key);
                    CommonFun.WriteCSV(fileName + "/RemoveHistory" + ticks + fileExtendName, item);
                }
            }

            return items;
        }

        private int GetHistoryStock(string key, Dictionary<string, BaseItemInfo> historyItems)
        {
            if (historyItems.ContainsKey(key))
            {
                return Convert.ToInt32(historyItems[key].Inventory);
            }

            return int.MinValue;
        }

        /// <summary>
        /// 写历史记录
        /// </summary>
        /// <param name="item"></param>
        private void AddHistoryStock(BaseItemInfo item)
        {
            CommonFun.WriteXLS(fileName + "history.xls", item);
        }

        private void UpdateHistoryStock(BaseItemInfo item)
        {
            CommonFun.UpdateXLS(fileName + "history.xls", new string[] { "库存" }, new string[] { item.Inventory }, "最近浏览", item.ViewCount, "info");
        }

        public bool DeleteItem(BaseItemInfo item)
        {
            try
            {
                string url = string.Format("http://popadmin.111.com.cn/admin/itemlist/deleteProduct.action?popItemIds={0}", item.ViewCount);

                string content = request.HttpGet(url);

                if (content.Contains("1"))
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
        /// 下架重复商品
        /// </summary>
        public void DownDoubleItems()
        {
            try
            {
                Login();

                Dictionary<string, BaseItemInfo> dicItems = GetStorehouseItems();

                List<string> itemsCountInfo = new List<string>();

                foreach (BaseItemInfo item in dicItems.Values)
                {
                    if (itemsCountInfo.Contains(item.ItemName.Trim()))
                    {
                        DeleteItem(item);
                        CommonFun.WriteCSV(fileName + "double" + ticks + fileExtendName, item);
                    }
                    else
                    {
                        itemsCountInfo.Add(item.ItemName.Trim());
                    }
                }

                Console.WriteLine("Finished!!!!!!!!!!!!!!!!");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public override void Start()
        {
            bool isTest = false;

            DateTime startTime = DateTime.MinValue;

            while (true)
            {
                try
                {
                    if (startTime == DateTime.MinValue || (DateTime.Now - startTime).Minutes > 10)
                    {
                        Login();

                        Dictionary<string, BaseItemInfo> yfHistoryItems = ReadHistoryItems();

                        ReadPlatFormWebPageValue yf = new ReadPlatFormWebPageValue();
                        yf.Login(2);
                        Dictionary<string, BaseItemInfo> yfSellingItems = yf.GetSellingItems();

                        Dictionary<string, BaseItemInfo> yySellingItems = GetSellingItems();

                        Dictionary<string, BaseItemInfo> yyStorehouseItems = GetStorehouseItems();

                        Dictionary<string, BaseItemInfo> yyReadyPublishItems = GetReadyPublishItems();

                        Dictionary<string, BaseItemInfo> downItems = RemoveHistoryItem(yfSellingItems, yfHistoryItems);

                        //下架
                        int count = 0;
                        foreach (KeyValuePair<string, BaseItemInfo> downValue in downItems)
                        {
                            Console.WriteLine("{0} down item totalCount:{1} curCount:{2}", DateTime.Now, downItems.Count, ++count);
                            BaseItemInfo downItem = downValue.Value;
                            foreach (BaseItemInfo item in yySellingItems.Values)
                            {
                                if (CommonFun.IsSameItem(downItem.ID, item.ID, downItem.Format, item.Format, downItem.Name, item.Name))
                                {
                                    DownItem(item);
                                    CommonFun.WriteCSV(fileName + "down" + ticks + fileExtendName, item);
                                }
                            }
                        }

                        count = 0;
                        foreach (KeyValuePair<string, BaseItemInfo> value in yfSellingItems)
                        {
                            try
                            {
                                Console.WriteLine("{0} up and sync item totalCount:{1} curCount:{2}", DateTime.Now, yfSellingItems.Count, ++count);
                                BaseItemInfo yfItem = value.Value;
                                if (IsInTest(yfItem.ID))
                                {

                                }
                                //是否在售
                                bool isSelling = false;
                                foreach (BaseItemInfo item in yySellingItems.Values)
                                {
                                    if (CommonFun.IsSameItem(item.ID, yfItem.ID, item.Format, yfItem.Format, item.Name, yfItem.Name))
                                    {
                                        if(yfItem.Inventory == "0")
                                        {
                                            DownItem(item);
                                            CommonFun.WriteCSV(fileName + "zeroStockDown" + ticks + fileExtendName, item);
                                            continue;
                                        }
                                        isSelling = true;
                                        int historyCount = GetHistoryStock(value.Key, yfHistoryItems);
                                        //TODO 同步库存
                                        if (historyCount != int.MinValue)
                                        {
                                            int yfCount = Convert.ToInt32(yfItem.Inventory);
                                            int yyCount = Convert.ToInt32(item.Inventory);

                                            int diff = historyCount - yfCount;
                                            //diff += historyCount - yyCount;

                                            if (diff > 0)
                                            {
                                                historyCount -= diff;
                                                string historyCountStr = historyCount.ToString();
                                                UpdateStock(item.ViewCount, historyCountStr);
                                                yfHistoryItems[value.Key].Inventory = historyCountStr;
                                                UpdateHistoryStock(yfHistoryItems[value.Key]);
                                                item.Inventory = historyCountStr;
                                                yf.UpdateItemInfo(item);
                                                CommonFun.WriteCSV(fileName + "change_stock" + ticks + fileExtendName, yfItem);
                                            }
                                            else
                                            {
                                                Console.WriteLine("Stock error history:{0} yf:{1} yy:{2}", historyCount, yfCount, yyCount);
                                            }
                                        }
                                        else
                                        {
                                            //重新同步库存已药房的为基础库存
                                            Console.WriteLine("sync stock key;{0} stock:{1}...........", value.Key, yfItem.Inventory);
                                            CommonFun.WriteCSV(fileName + "sync_stock" + ticks + fileExtendName, yfItem);
                                            yfHistoryItems.Add(value.Key, yfItem);
                                            UpdateStock(item.ViewCount, yfItem.Inventory);
                                            AddHistoryStock(yfItem);
                                        }

                                        break;
                                    }
                                }

                                if (!isSelling)
                                {
                                    //是否在仓库
                                    bool isInStorehouse = false;

                                    foreach (BaseItemInfo item in yyStorehouseItems.Values)
                                    {
                                        if (CommonFun.IsSameItem(item.ID, yfItem.ID, item.Format, yfItem.Format, item.Name, yfItem.Name))
                                        {
                                            if (IsDownTime() && IsInAutoUpDownTypeList((item as ItemInfo).Use))
                                            {
                                                break;
                                            }
                                            //重新上架 TODO
                                            isInStorehouse = true;
                                            item.Inventory = yfItem.Inventory;
                                            if (!isTest)
                                            {
                                                if (UpItem(item))
                                                {
                                                    UpdatePirceAndQuantity(item.ViewCount, "100", "103", yfItem.Inventory);

                                                    if (yfHistoryItems.ContainsKey(value.Key))
                                                    {
                                                        yfHistoryItems[value.Key].Inventory = yfItem.Inventory;
                                                        UpdateHistoryStock(yfHistoryItems[value.Key]);
                                                    }
                                                    else
                                                    {
                                                        yfHistoryItems.Add(value.Key, yfItem);
                                                        AddHistoryStock(yfItem);
                                                    }
                                                }
                                            }

                                            CommonFun.WriteCSV(fileName + "upItem" + ticks + fileExtendName, item);

                                            break;
                                        }
                                    }

                                    if (!isInStorehouse)
                                    {
                                        //是否待发不
                                        bool isInReadyPublish = false;

                                        foreach (BaseItemInfo item in yyReadyPublishItems.Values)
                                        {
                                            if (CommonFun.IsSameItem(item.ID, yfItem.ID, item.Format, yfItem.Format, item.Name, yfItem.Name))
                                            {
                                                isInReadyPublish = true;
                                                CommonFun.WriteCSV(fileName + "publishItem" + ticks + fileExtendName, item);
                                                break;
                                            }
                                        }

                                        if (!isInReadyPublish)
                                        {
                                            //上架新品 TODO
                                            if (UpNewItem(yfItem))
                                            {
                                                CommonFun.WriteCSV(fileName + "upNewItemSuccessed" + ticks + fileExtendName, yfItem);
                                            }
                                            else
                                            {
                                                CommonFun.WriteCSV(fileName + "upNewItemFailed" + ticks + fileExtendName, yfItem);
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

                        Console.WriteLine("{0} Finshed.....................", DateTime.Now);
                        startTime = DateTime.Now;
                        ticks = startTime.Ticks;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }

            //ReadAllMenuURL();

            //GetWinItem();
        }



        public void Play()
        {
            s.Play();
        }

        /// <summary>
        /// 是否上架时间
        /// </summary>
        /// <returns></returns>
        private bool IsDownTime()
        {
            return CommonFun.IsInTimeRange(downTime);
        }

        private void AutoDownItem()
        {
            Dictionary<string, BaseItemInfo> temp = GetSellingItems();

            foreach (BaseItemInfo item in temp.Values)
            {
                if (IsInAutoUpDownTypeList((item as ItemInfo).Use))
                {
                    if (!DownItem(item))
                    {
                        Console.WriteLine("{2} Down failed itemId;{0}, ItemName:{1}", item.ViewCount, item.ItemName, DateTime.Now.ToString());
                    }
                    else
                    {
                        Console.WriteLine("{2} Down success itemId;{0}, ItemName:{1}", item.ViewCount, item.ItemName, DateTime.Now.ToString());
                    }
                }
            }
            isDown = true;
        }

        private void AutoUpItem()
        {
            Dictionary<string, BaseItemInfo> temp = GetDownItems();
            foreach (BaseItemInfo item in temp.Values)
            {
                if (IsInAutoUpDownTypeList((item as ItemInfo).Use))
                {
                    if (!UpItem(item))
                    {
                        Console.WriteLine("{2} Up failed itemId;{0}, ItemName:{1}", item.ViewCount, item.ItemName, DateTime.Now.ToString());
                    }
                    else
                    {
                        Console.WriteLine("{2} Up success itemId;{0}, ItemName:{1}", item.ViewCount, item.ItemName, DateTime.Now.ToString());
                    }
                }
            }
            isDown = false;
        }

        /// <summary>
        /// 上架物品
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private bool DownItem(BaseItemInfo item)
        {
            string url = "http://popadmin.111.com.cn/admin/itemlist/downProduct.action?item.popItemIds={0}";

            string result = request.HttpGet(string.Format(url, item.ViewCount));

            if (result == "suc")
            {
                return true;
            }
            item.Remark = result;
            return false;
        }

        /// <summary>
        /// 上架物品
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private bool UpItem(BaseItemInfo item)
        {
            string url = "http://popadmin.111.com.cn/admin/itemlist/upProduct.action?item.popItemIds={0}";

            string result = request.HttpGet(string.Format(url, item.ViewCount));

            if (result == "suc")
            {
                return true;
            }
            item.Remark = result;
            return false;
        }
        public void UpdatePrice()
        {
            bool isTest = true;
            Login();

            Thread upDownThread = new Thread(AutoOpr);
            upDownThread.Start();

            Thread notic = new Thread(NoticPlay);
            notic.Start();

            DateTime startDate = DateTime.MinValue;

            while (true)
            {
                ticks = DateTime.Now.Ticks;
                try
                {
                    if (startDate == DateTime.MinValue || (DateTime.Now - startDate).Minutes > 10)
                    {
                        startDate = DateTime.Now;
                        //获取在售列表
                        sellItems = GetSellingItems();
                        //搜索平台同种商品
                        int totalCount = sellItems.Count, curCount = 0;

                        foreach (BaseItemInfo item in sellItems.Values)
                        {
                            //if (isTest && IsInTest(item.ID))
                            //{

                            //}
                            //特殊标识不改价
                            if (item.ItemName.Contains("单品包邮"))
                            {
                                CommonFun.WriteCSV(fileName + "unUpdatePrice" + ticks + fileExtendName, item);
                                break;
                            }

                            Console.WriteLine("Update new.........{0},TotalCount:{1},CurCount:{2}", DateTime.Now, totalCount, ++curCount);
                            bool isHave = false;
                            for (int i = 0; i < 2; i++)
                            {
                                BaseItemInfo sItem = GetYiYaoMinPriceItem((ItemInfo)item, false, i == 1, false);

                                if (sItem != null)
                                {
                                    isHave = true;
                                    decimal minPrice = sItem.ShopPrice - lPrice; //(sItem.ShopPrice - (decimal)0.1);
                                    //判断是否需要改价
                                    if (item.ShopPrice != minPrice)
                                    {
                                        item.PlatformPrice = minPrice;

                                        if (item.ShopPrice * (decimal)(100 - minDownRate) / 100M < minPrice)
                                        {
                                            UpdatePirceAndQuantity(item.ViewCount, minPrice.ToString(), null, null, item.ShopPrice.ToString());
                                            CommonFun.WriteCSV(fileName + "updatePrice" + ticks + ".csv", item);
                                        }
                                        else
                                        {
                                            CommonFun.WriteCSV(fileName + "ToolowerPrice" + ticks + ".csv", item);
                                        }
                                    }
                                    break;
                                }
                            }

                            if (!isHave)
                            {
                                CommonFun.WriteCSV(fileName + "NotFindOrOlny" + ticks + fileExtendName, item);
                            }
                        }
                        Console.WriteLine("{0} Finshed.......................", DateTime.Now.ToString());
                    }
                    else
                    {
                        Console.WriteLine("{0} Sleepping.......................", DateTime.Now.ToString());
                        Thread.Sleep(60 * 1000);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
        }

        public Dictionary<string, BaseItemInfo> GetSellingItem(string id, DateTime beginTime)
        {
            Dictionary<string, BaseItemInfo> items = new Dictionary<string, BaseItemInfo>();
            try
            {
                //Login();
                if (!string.IsNullOrEmpty(id))
                {
                    string[] ids = id.Split(',');

                    string url = "http://popadmin.111.com.cn/admin/itemlist/showItem.action";
                    string postData = string.Format("venderService=0&tagInx=2&itemCondition.pageIndex=1&itemCondition.searchType=1&itemCondition.productName=&itemCondition.productNos={0}&itemCondition.catalogName=&itemCondition.catalogId=&itemCondition.catalogLevel=&itemCondition.inshopCatalogName=&itemCondition.inshopCatalogId=&itemCondition.inshopCatalogLevel=&itemCondition.pageSize=10", ids[0]);

                    string content = request.HttpPost(url, postData);

                    BaseItemInfo item = new BaseItemInfo();
                    item.Type = id;

                    item.Inventory = CommonFun.GetValue(content, "<span id=\"quantitySpan", "/span>");
                    item.Inventory = CommonFun.GetValue(item.Inventory, ">", "<");
                    item.Inventory = item.Inventory;

                    if (string.IsNullOrEmpty(item.Inventory))
                    {
                        return items;
                    }

                    int page = 1;
                    int totalPage = 0;
                    int count = 0;
                    foreach (string idStr in ids)
                    {
                        do
                        {
                            string findUrl = "http://popadmin.111.com.cn/admin/order/findOrder.action";
                            string findPostData = string.Format("queryBean.orderDateBegin=&queryBean.orderDateEnd=&queryBean.paymentDateBegin=&queryBean.paymentDateEnd=&queryBean.childOrderId=&queryBean.customerName=&queryBean.consigneeTelphone=&queryBean.goodsCode={0}&queryBean.goodsName=&queryBean.timeoutStay=0&queryBean.sorting=3&queryBean.type=1&queryBean.pageSize=10&queryBean.pageNo={1}&queryBean.consigneeName=&queryBean.waybillCode=&pageIndex={2}&pageShowAmount=10", idStr, page, page);
                            content = request.HttpPost(findUrl, findPostData);

                            if (totalPage == 0)
                            {
                                string temp = CommonFun.GetValue(content, "pageCount\":\"", "\"");
                                totalPage = Convert.ToInt32(CommonFun.GetValue(content, "pageCount\":\"", "\""));
                            }

                            //Console.WriteLine("Getting...... totalPage:{0},curPage{1}", totalPage, page);

                            MatchCollection ms = CommonFun.GetValues(content, "\"carrierId\":", "\"yesShipped\":");

                            foreach (Match m in ms)
                            {
                                string sign = CommonFun.GetValue(m.Value, "\"sign\":", "}");
                                if (sign == "1")
                                {
                                    if (beginTime == null || DateTime.ParseExact(CommonFun.GetValue(m.Value, "\"orderDate\":\"", "\""), "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.CurrentCulture) >= beginTime)
                                    {
                                        string countStr = CommonFun.GetValue(m.Value, "\"rmaCount\":", ",");
                                        if (!string.IsNullOrEmpty(countStr))
                                        {
                                            count += Convert.ToInt32(countStr);
                                        }
                                    }
                                }
                            }
                        }
                        while (++page <= totalPage);
                    }
                    item.Inventory = (Convert.ToInt32(item.Inventory) - count).ToString();
                    items.Add(id, item);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("id:{0}, error:{1}", id, ex);
            }

            return items;
        }

        private void NoticPlay(object state)
        {
            int last_count = 0;
            int noticeCount = 0;
            while (true)
            {
                int cur_count = 0;
                try
                {
                    string url = "http://popadmin.111.com.cn/admin/order/findOrderCount.action";
                    string postStr = "queryBean.orderDateBegin=&queryBean.orderDateEnd=&queryBean.paymentDateBegin=&queryBean.paymentDateEnd=&queryBean.childOrderId=&queryBean.customerName=&queryBean.consigneeTelphone=&queryBean.goodsCode=&queryBean.goodsName=&queryBean.timeoutStay=0&queryBean.sorting=3&queryBean.type=1&queryBean.pageSize=10&queryBean.pageNo=1&queryBean.consigneeName=&queryBean.waybillCode=&pageIndex=1&pageShowAmount=10";

                    string content = request.HttpPost(url, postStr);

                    string count = CommonFun.GetValue(content, "noAudit\":\"", "\"");

                    if (!string.IsNullOrEmpty(count))
                    {
                        cur_count = Convert.ToInt32(count);
                    }

                    if (cur_count > 0)
                    {
                        if (last_count != cur_count || noticeCount < 3)
                        {
                            if (last_count != cur_count)
                            {
                                noticeCount = 0;
                            }
                            else
                            {
                                noticeCount++;
                            }

                            Play();
                            last_count = cur_count;
                            Console.WriteLine("{0}  have new msg.......................", DateTime.Now.ToString());
                            Thread.Sleep(60 * 1000);
                        }
                    }
                    else
                    {
                        Thread.Sleep(1000);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
        }

        /// <summary>
        /// 自动上下架操作
        /// </summary>
        private void AutoOpr(object state)
        {
            while (true)
            {
                try
                {
                    if (IsDownTime())
                    {
                        if (!isDown)
                        {
                            AutoDownItem();
                            isDown = true;
                            isUp = false;
                        }
                    }
                    else
                    {
                        if (!isUp)
                        {
                            AutoUpItem();
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

        private ItemInfo GetOneFromDownList(string content)
        {
            ItemInfo item = new ItemInfo();
            try
            {

                string itemStr = CommonFun.GetValue(content, "<div id=\"tempMProductSave_new_div_19\" class=\"firstStep\">", "	<div class=\"goods_choose_mainGoods\" id=\"mainGoodsDivId\">");

                MatchCollection ms = CommonFun.GetValues(itemStr, "<div", "</div>");

                //string title = CommonFun.GetValue(itemStr, "tempProduct.productSubTitle", "/>");

                //string name = CommonFun.GetValue(itemStr, "tempProduct.productName", "/>");

                item.ItemName = CommonFun.GetValue(ms[0].Value, "value=\"", "\"");

                item.Use = CommonFun.GetValue(ms[1].Value, "value=\"", "\"");

                item.ID = CommonFun.GetValue(ms[3].Value, "value=\"", "\"");

                item.Format = CommonFun.GetValue(ms[4].Value, "value=\"", "\"");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            return item;
        }

        public Dictionary<string, BaseItemInfo> GetDownItems(int tagInx = 1)
        {
            Dictionary<string, BaseItemInfo> dic = new Dictionary<string, BaseItemInfo>();
            List<string> itemUrls = new List<string>();
            Dictionary<string, string> itemInfo = new Dictionary<string, string>();
            string url = string.Format("http://popadmin.111.com.cn/admin/itemlist/showItem.action?tagInx={0}", tagInx);

            string content = request.HttpGet(url);

            string pageInfo = CommonFun.GetValue(content, "<div class=\"pagination\">", "</div>");

            string pageStr = CommonFun.GetValue(pageInfo, "共", "页");

            int page = Convert.ToInt32(pageStr);

            string nextPageUrl = "http://popadmin.111.com.cn/admin/itemlist/showItem.action";

            string postData = "venderService=0&tagInx=1&itemCondition.pageIndex={0}&itemCondition.searchType=&itemCondition.productName=&itemCondition.productNos=&itemCondition.catalogName=&itemCondition.catalogId=&itemCondition.catalogLevel=&itemCondition.inshopCatalogName=&itemCondition.inshopCatalogId=&itemCondition.inshopCatalogLevel=&itemCondition.pageSize=10";


            for (int i = 1; i <= page; i++)
            {
                Console.WriteLine("{0},DownList totalPage:{1}, curPage:{2}", DateTime.Now.ToString(), page, i);
                content = request.HttpPost(nextPageUrl, string.Format(postData, i));

                Dictionary<string, string> infos = GetOnePageItemIdAndUrls(content);

                if (infos != null)
                {
                    foreach (KeyValuePair<string, string> info in infos)
                    {
                        itemInfo.Add(info.Key, info.Value);
                    }
                }
            }

            int totalCount = itemInfo.Count, curCount = 0;

            foreach (KeyValuePair<string, string> iUrl in itemInfo)
            {
                string infoUrl = string.Format("http://popadmin.111.com.cn/admin/item/queryItemByPop.action?popItemId={0}&tagInx=1&brandCheckStatus=2", iUrl.Key);
                Console.WriteLine("{0},DownList totalCount:{1}, curCount:{2}", DateTime.Now.ToString(), totalCount, ++curCount);
                content = request.HttpGet(infoUrl);

                ItemInfo item = GetOneFromDownList(content);

                if (item != null)
                {
                    item.ViewCount = iUrl.Key;
                    string key = item.ViewCount;

                    if (!dic.ContainsKey(key))
                    {
                        dic.Add(key, item);
                    }
                }
            }

            return dic;
        }

        /// <summary>
        /// 获取仓库列表
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, BaseItemInfo> GetStorehouseItems()
        {
            return GetItemsByTagInx(1);
        }

        /// <summary>
        /// 获取待发列表
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, BaseItemInfo> GetReadyPublishItems()
        {
            return GetItemsByTagInx(0);
        }

        public Dictionary<string, BaseItemInfo> GetItemsByTagInx(int tagInx)
        {
            Dictionary<string, BaseItemInfo> dic = new Dictionary<string, BaseItemInfo>();
            try
            {
                Dictionary<string, string> itemInfo = new Dictionary<string, string>();
                string url = string.Format("http://popadmin.111.com.cn/admin/itemlist/showItem.action?tagInx={0}", tagInx);

                string content = request.HttpGet(url);

                string pageInfo = CommonFun.GetValue(content, "<div class=\"pagination\">", "</div>");

                string pageStr = CommonFun.GetValue(pageInfo, "共", "页");

                int page = Convert.ToInt32(pageStr);

                string nextPageUrl = "http://popadmin.111.com.cn/admin/itemlist/showItem.action";

                string postData = "venderService=0&tagInx={1}&itemCondition.pageIndex={0}&itemCondition.searchType=&itemCondition.productName=&itemCondition.productNos=&itemCondition.catalogName=&itemCondition.catalogId=&itemCondition.catalogLevel=&itemCondition.inshopCatalogName=&itemCondition.inshopCatalogId=&itemCondition.inshopCatalogLevel=&itemCondition.pageSize=10";

                List<string> postDatas = new List<string>();

                for (int i = 1; i <= page; i++)
                {
                    Console.WriteLine("{0}, totalPage:{1}, curPage:{2}", DateTime.Now.ToString(), page, i);

                    postDatas.Add(string.Format(postData, i, tagInx));
                }

                RequestInfo requestInfo = new RequestInfo();
                requestInfo.Urls.Add(nextPageUrl);
                requestInfo.PostDatas = postDatas;
                requestInfo.RequestParams.Add("requestType", "post");

                RequestUrls(requestInfo);

                foreach (string value in requestInfo.Contentes)
                {
                    Dictionary<string, string> infos = GetOnePageItemIdAndUrls(value);

                    if (infos != null)
                    {
                        foreach (KeyValuePair<string, string> info in infos)
                        {
                            if (!itemInfo.ContainsKey(info.Key))
                            {
                                itemInfo.Add(info.Key, info.Value);
                            }
                        }
                    }
                }

                int totalCount = itemInfo.Count, curCount = 0;

                foreach (KeyValuePair<string, string> iUrl in itemInfo)
                {
                    Console.WriteLine("{0}, totalCount:{1}, curCount:{2}", DateTime.Now.ToString(), totalCount, ++curCount);

                    BaseItemInfo item = null;

                    if (tagInx == 2)
                    {
                        content = request.HttpGet(iUrl.Value, Encoding.GetEncoding("gb2312"));
                        item = GetOneItem(content);
                    }
                    else
                    {
                        content = request.HttpGet(iUrl.Value);

                        item = GetOneFromDownList(content);
                    }

                    //if (!string.IsNullOrEmpty(iUrl.Value))
                    //{

                    //}
                    //else
                    //{
                    //    string infoUrl = string.Format("http://popadmin.111.com.cn/admin/item/queryItemByPop.action?popItemId={0}&tagInx=1&brandCheckStatus=2", iUrl.Key);


                    //}

                    if (item != null)
                    {
                        item.ViewCount = iUrl.Key;
                        string key = item.ViewCount;
                        if (!string.IsNullOrEmpty(item.Created))
                        {
                            key = item.ID + item.Format + item.Created;
                        }

                        if (!dic.ContainsKey(key))
                        {
                            dic.Add(key, item);
                        }
                        else
                        {
                            Console.WriteLine("key:{0}", key);
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return dic;
        }

        /// <summary>
        /// 获取在售列表
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, BaseItemInfo> GetSellingItems()
        {
            return GetItemsByTagInx(2);
            //Dictionary<string, BaseItemInfo> dic = new Dictionary<string, BaseItemInfo>();
            //Dictionary<string, string> itemInfo = new Dictionary<string, string>();
            //string url = "http://popadmin.111.com.cn/admin/itemlist/showItem.action?tagInx=2";

            //string content = request.HttpGet(url);

            //string pageInfo = CommonFun.GetValue(content, "<div class=\"pagination\">", "</div>");

            //string pageStr = CommonFun.GetValue(pageInfo, "共", "页");

            //int page = Convert.ToInt32(pageStr);

            //string nextPageUrl = "http://popadmin.111.com.cn/admin/itemlist/showItem.action";

            //string postData = "venderService=0&tagInx=2&itemCondition.pageIndex={0}&itemCondition.searchType=&itemCondition.productName=&itemCondition.productNos=&itemCondition.catalogName=&itemCondition.catalogId=&itemCondition.catalogLevel=&itemCondition.inshopCatalogName=&itemCondition.inshopCatalogId=&itemCondition.inshopCatalogLevel=&itemCondition.pageSize=10";

            //List<string> postDatas = new List<string>();

            //for (int i = 1; i <= page; i++)
            //{
            //    Console.WriteLine("{0}, totalPage:{1}, curPage:{2}", DateTime.Now.ToString(), page, i);

            //    postDatas.Add(string.Format(postData, i));
            //}

            //RequestInfo requestInfo = new RequestInfo();
            //requestInfo.Urls.Add(nextPageUrl);
            //requestInfo.PostDatas = postDatas;
            //requestInfo.RequestParams.Add("requestType", "post");

            //RequestUrls(requestInfo);

            //foreach (string value in requestInfo.Contentes)
            //{
            //    Dictionary<string, string> infos = GetOnePageItemIdAndUrls(value);

            //    if (infos != null)
            //    {
            //        foreach (KeyValuePair<string, string> info in infos)
            //        {
            //            itemInfo.Add(info.Key, info.Value);
            //        }
            //    }
            //}

            //int totalCount = itemInfo.Count, curCount = 0;

            //foreach (KeyValuePair<string, string> iUrl in itemInfo)
            //{
            //    Console.WriteLine("{0}, totalCount:{1}, curCount:{2}", DateTime.Now.ToString(), totalCount, ++curCount);
            //    content = request.HttpGet(iUrl.Value, Encoding.GetEncoding("gb2312"));

            //    ItemInfo item = GetOneItem(content);

            //    if (item != null)
            //    {
            //        item.ViewCount = iUrl.Key;
            //        string key = item.ID + item.Format + item.Created;
            //        if (!dic.ContainsKey(key))
            //        {
            //            dic.Add(key, item);
            //        }
            //    }
            //}

            //return dic;
        }

        public ItemInfo GetOneItem(string content)
        {
            try
            {
                string title = CommonFun.GetValue(content, "<span class=\"red giftRed\">", "</span>");
                title = title.Trim();

                string itemId = CommonFun.GetValue(content, "item_id = '", "'");

                string url = string.Format("http://www.111.com.cn/items/getComboInfo.action?id={0}&type=ZSP&_=1520495498787", itemId);

                string priceStr = request.HttpGet(url);

                MatchCollection ms = CommonFun.GetValues(priceStr, "{", "}");

                foreach (Match m in ms)
                {
                    string id = CommonFun.GetValue(m.Value, "\"id\":", ",");

                    if (id == itemId)
                    {
                        priceStr = CommonFun.GetValue(m.Value, "originalPrice\":", ",");
                    }
                }


                content = CommonFun.GetValue(content, "<div class=\"goods_intro\">", "</div>");
                if (!string.IsNullOrEmpty(content))
                {
                    ItemInfo item = new ItemInfo();
                    MatchCollection td3Ms = CommonFun.GetValues(content, "<td colspan=\"3\">", "</td>");
                    MatchCollection tdMs = CommonFun.GetValues(content, "<td>", "</td>");

                    item.ID = tdMs[4].Value.IndexOf('(') > 0 ? tdMs[4].Value.Substring(0, tdMs[4].Value.IndexOf('(')) : "";
                    item.ID = item.ID.Trim();
                    item.ID = item.ID.Replace(" ", "");
                    item.Format = tdMs[1].Value;
                    item.Created = tdMs[3].Value;
                    item.BrandName = tdMs[0].Value;
                    item.ItemName = td3Ms[0].Value;
                    item.ShopPrice = string.IsNullOrEmpty(priceStr) ? 0 : Convert.ToDecimal(priceStr);
                    item.Use = title;
                    string[] arrInfo = td3Ms[0].Value.Split(' ');
                    if (arrInfo.Length < 3)
                    {
                        int index = td3Ms[0].Value.IndexOf('（');
                        if (index > 0)
                        {
                            item.Name = td3Ms[0].Value.Substring(0, index);
                        }
                        else
                        {
                            if (arrInfo.Length > 1)
                            {
                                string str = CommonFun.GetNum(arrInfo[1]);

                                if (!string.IsNullOrEmpty(str))
                                {
                                    index = arrInfo[1].IndexOf(str[0]);

                                    item.Name = arrInfo[1].Substring(0, index);
                                }
                            }

                            if (string.IsNullOrEmpty(item.Name))
                            {
                                item.Name = arrInfo[0];
                            }
                        }
                    }
                    else
                    {
                        item.Name = string.IsNullOrEmpty(arrInfo[1]) ? arrInfo[2] : arrInfo[1];
                    }


                    return item;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return null;
        }

        public void UpdateStock(string id, string stock)
        {
            try
            {
                if (!string.IsNullOrEmpty(id))
                {
                    string[] ids = id.Split(',');

                    string url = "http://popadmin.111.com.cn/admin/itemlist/showItem.action";
                    string postData = string.Format("venderService=0&tagInx=2&itemCondition.pageIndex=1&itemCondition.searchType=1&itemCondition.productName=&itemCondition.productNos={0}&itemCondition.catalogName=&itemCondition.catalogId=&itemCondition.catalogLevel=&itemCondition.inshopCatalogName=&itemCondition.inshopCatalogId=&itemCondition.inshopCatalogLevel=&itemCondition.pageSize=10", id);

                    string content = request.HttpPost(url, postData);

                    content = CommonFun.GetValue(content, "</thead>", "</tbody>");

                    MatchCollection tMs = CommonFun.GetValues(content, "<tr>", "</tr>");

                    foreach (Match tM in tMs)
                    {
                        string idInfo = CommonFun.GetValue(tM.Value, "<img id=\"", "\"");

                        UpdatePirceAndQuantity(idInfo, null, null, stock);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public Dictionary<string, string> GetOnePageItemIdAndUrls(string content)
        {
            Dictionary<string, string> itemIdAndUrls = new Dictionary<string, string>();

            content = CommonFun.GetValue(content, "</thead>", "</tbody>");

            MatchCollection tMs = CommonFun.GetValues(content, "<tr>", "</tr>");

            foreach (Match tM in tMs)
            {
                string trInfo = CommonFun.GetValue(tM.Value, "<p class=\"goods_name\">", "</p>");

                string itemUrl = CommonFun.GetValue(trInfo, "href=\"", "\"");
                if (string.IsNullOrEmpty(itemUrl))
                {
                    itemUrl = CommonFun.GetValue(tM.Value, "onclick=\"editNewTempProduct", "\"");
                    itemUrl = itemUrl.Replace("(", "").Replace(")", "");
                    itemUrl = itemUrl.Replace("'", "");
                    string[] itemInfoArray = itemUrl.Split(',');

                    itemUrl = string.Format("http://popadmin.111.com.cn/admin/item/queryItemByPop.action?popItemId={0}&tagInx={1}&brandCheckStatus={2}", itemInfoArray[0], itemInfoArray[1], itemInfoArray[2]);
                }
                string idInfo = CommonFun.GetValue(tM.Value, "<img id=\"", "\"");

                itemIdAndUrls.Add(idInfo, itemUrl);
            }

            return itemIdAndUrls;
        }

        public List<BaseItemInfo> GetOneItemInfo(string content)
        {
            List<BaseItemInfo> items = new List<BaseItemInfo>();

            try
            {
                content = CommonFun.GetValue(content, "</thead>", "</tbody>");

                MatchCollection tMs = CommonFun.GetValues(content, "<tr>", "</tr>");

                foreach (Match tM in tMs)
                {
                    string trInfo = CommonFun.GetValue(tM.Value, "<p class=\"goods_name\">", "</p>");


                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return items;
        }

        private string GetBrandFilter(string content, string brandName)
        {
            string brandId = null;

            string info = CommonFun.GetValue(content, "filterBrandList clearfix\">", "</ul>");

            MatchCollection ms = CommonFun.GetValues(info, "<li>", "</li>");


            foreach (Match m in ms)
            {
                string title = CommonFun.GetValue(m.Value, "<span class=\"\">", "</span>");
                if (title == brandName)
                {
                    brandId = CommonFun.GetValue(m.Value, "attrid=\"", "\"");
                    break;
                }
            }

            return brandId;
        }

        private string GetFormat(string content)
        {
            content = content.Replace(" ", "");
            content = content.Replace('S', '粒');
            content = content.Replace('s', '粒');
            content = content.Replace("毫克", "mg");
            content = content.Replace("x", "*");
            content = content.Replace("×", "*");
            content = content.Replace("(", "");
            content = content.Replace("（", "");
            content = content.Replace("）", "");
            content = content.Replace(")", "");
            return content;
        }

        /// <summary>
        /// 获取最低物品
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public ItemInfo GetYiYaoMinPriceItem(ItemInfo toItem, bool isUseName = true, bool isUseOldFun = false, bool isUseBrand = true)
        {
            try
            {
                int page = 1;
                int totalPage = 0;

                do
                {
                    bool isSpc = IsInSpcTypeList(toItem.Use);

                    string seachName = toItem.Name;//isUseName ? toItem.Name : toItem.ItemName;

                    string itemName = HttpUtility.UrlEncode(seachName, Encoding.GetEncoding("utf-8")).ToUpper();
                    itemName = HttpUtility.UrlEncode(itemName, Encoding.GetEncoding("utf-8")).ToUpper();

                    if (!isUseOldFun)
                    {
                        itemName += CommonFun.GetUrlEncode(toItem.Format, false);
                    }

                    string cUrl = "http://www.111.com.cn/interfaces/series.action";

                    string sUrl = "http://www.111.com.cn/search/search.action?keyWord={0}&&sort=50&gotoPage={1}&rqstMode=asynchronous&t=0.9971701451027082&_=1521252418092";

                    string brandFilter = "";

                    string pageStr = "";

                    string content = "";

                    string paramBrandFilter = "&brandFilter={0}";

                    if (isUseBrand && totalPage == 0)
                    {
                        string bContent = request.HttpGet(string.Format(sUrl, itemName, page), Encoding.GetEncoding("gb2312"));

                        brandFilter = GetBrandFilter(bContent, toItem.BrandName);

                        pageStr = CommonFun.GetValue(bContent, "<span class=\"pageOp\">共", "页");

                        if (string.IsNullOrEmpty(pageStr))
                        {
                            brandFilter = GetBrandFilter(bContent, "其他");
                            bContent = request.HttpGet(string.Format(sUrl, itemName, brandFilter, 1), Encoding.GetEncoding("gb2312"));
                            pageStr = CommonFun.GetValue(brandFilter, "<span class=\"pageOp\">共", "页");
                        }
                    }

                    if (string.IsNullOrEmpty(pageStr))
                    {
                        brandFilter = "";
                        content = request.HttpGet(string.Format(sUrl, itemName, page), Encoding.GetEncoding("gb2312"));
                        pageStr = CommonFun.GetValue(content, "<span class=\"pageOp\">共", "页");
                        totalPage = string.IsNullOrEmpty(pageStr) ? 0 : Convert.ToInt32(pageStr);
                    }
                    else
                    {
                        string qUrl = string.Format(sUrl, itemName, brandFilter, page);
                        qUrl += (string.IsNullOrEmpty(brandFilter) ? "" : string.Format(paramBrandFilter, brandFilter));
                        content = request.HttpGet(qUrl, Encoding.GetEncoding("gb2312"));
                    }


                    List<string> urls = isUseOldFun ? GetOnePageSeachItemUrls(content) : GetOnePageSeachItemUrls(content, toItem.ItemName);

                    //RequestInfo info = new RequestInfo();
                    //info.Urls = urls;
                    //info.RequestParams.Add("requestType", "get");
                    //info.RequestParams.Add("encoding", "gb2312");

                    //RequestUrls(info);

                    ItemInfo resItem = null;

                    foreach (string url in urls)
                    {
                        string itemInfoStr = request.HttpGet(url, Encoding.GetEncoding("gb2312"));

                        string sellNameInfo = CommonFun.GetValue(itemInfoStr, "<div class=\"right_property\">", "</div>");

                        sellNameInfo = CommonFun.GetValue(itemInfoStr, "<h3>", "</h3>");

                        if (!string.IsNullOrEmpty(sellNameInfo) && sellNameInfo != name)
                        {
                            //int count = 50;

                            //if (toItem.Use != titleName)
                            //{
                            string itemId = CommonFun.GetValue(itemInfoStr, "itemid=\"", "\"");
                            string pno = CommonFun.GetValue(itemInfoStr, "item_pno = '", "'");
                            string provinceId = "20";
                            string postData = string.Format("itemid={0}&pno={1}&provinceId={2}", itemId, pno, provinceId);
                            string countInfo = request.HttpGet(cUrl + "?" + postData);
                            //string countInfo = request.HttpPost(cUrl, postData);
                            countInfo = CommonFun.GetValue(countInfo, "\\[\"", "\"");
                            string[] arrCountInfo = countInfo.Split('_');
                            int count = Convert.ToInt32(arrCountInfo[2]);
                            //}

                            if (count > 0)
                            {
                                if (isSpc || count > minStock)
                                //if (toItem.Use != titleName && count > 10)
                                {
                                    ItemInfo item = GetOneItem(itemInfoStr);

                                    string mformat = GetFormat(toItem.Format);

                                    string pformat = GetFormat(item.Format);

                                    string mItemNam = GetFormat(toItem.ItemName);

                                    string pItemNam = GetFormat(item.ItemName);

                                    if (isUseName)
                                    {
                                        if (CommonFun.GetNum(item.ID) == CommonFun.GetNum(toItem.ID) && mformat == pformat && item.Created == toItem.Created && mItemNam == pItemNam)
                                        {
                                            if (resItem == null || (resItem.ShopPrice > item.ShopPrice && item.ShopPrice > 0))
                                            {
                                                resItem = item;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (resItem == null || (resItem.ShopPrice > item.ShopPrice && item.ShopPrice > 0))
                                        {
                                            string toItemName = GetFormat(toItem.ItemName);

                                            string gItemName = GetFormat(item.ItemName);

                                            toItemName = CommonFun.GetNum(toItemName);

                                            gItemName = CommonFun.GetNum(gItemName);

                                            if (CommonFun.GetNum(item.ID) == CommonFun.GetNum(toItem.ID))
                                            {
                                                if (toItemName == gItemName || CommonFun.GetFormatValue(toItem.ItemName) == CommonFun.GetFormatValue(item.ItemName))
                                                {
                                                    resItem = item;
                                                }

                                            }
                                        }
                                    }
                                }
                            }
                            if (resItem != null)
                            {
                                return resItem;
                            }
                        }
                    }
                } while (++page <= totalPage);



                //string sUrl = string.Format("http://www.111.com.cn/search/search.action?keyWord={0}", itemName);

                //string urlPage = "http://www.111.com.cn/search/search.action?keyWord={0}&sort=50&gotoPage={1}&rqstMode=asynchronous&t=0.8157034667475365&_=1520481340048";

                ////string cUrl = "http://www.111.com.cn/interfaces/series.action";






                //string brandFilter = GetBrandFilter(content, toItem.BrandName);

                //string sBrandUrl = "http://www.111.com.cn/search/search.action?keyWord={0}&brandFilter={1}&sort=50&gotoPage={2}&rqstMode=asynchronous&t=0.9971701451027082&_=1521252418092";

                //content = request.HttpGet(string.Format(sBrandUrl, itemName, brandFilter, 1), Encoding.GetEncoding("gb2312"));

                //string pageStr = CommonFun.GetValue(content, "<span class=\"pageOp\">共", "页");

                //int page = 0;

                //if (string.IsNullOrEmpty(pageStr))
                //{
                //    brandFilter = GetBrandFilter(content, "其他");
                //    content = request.HttpGet(string.Format(sBrandUrl, itemName, brandFilter, 1), Encoding.GetEncoding("gb2312"));
                //    pageStr = CommonFun.GetValue(content, "<span class=\"pageOp\">共", "页");
                //}

                //bool is_brand = true;
                //if (string.IsNullOrEmpty(pageStr))
                //{
                //    content = request.HttpGet(string.Format(urlPage, itemName, 1), Encoding.GetEncoding("gb2312"));
                //    pageStr = CommonFun.GetValue(content, "<span class=\"pageOp\">共", "页");
                //    is_brand = false;
                //}

                //if (string.IsNullOrEmpty(pageStr))
                //{
                //    return null;
                //}


                //page = Convert.ToInt32(pageStr);
                //page = page > 10 ? 10 : page;
                //for (int i = 1; i <= page; i++)
                //{
                //    string qUrl = is_brand ? string.Format(sBrandUrl, itemName, brandFilter, i) : string.Format(urlPage, itemName, i);

                //    content = request.HttpGet(qUrl, Encoding.GetEncoding("gb2312"));

                //    List<string> urls = isUseOldFun ? GetOnePageSeachItemUrls(content) : GetOnePageSeachItemUrls(content, toItem.ItemName);

                //    RequestInfo info = new RequestInfo();
                //    info.Urls = urls;
                //    info.RequestParams.Add("requestType", "get");
                //    info.RequestParams.Add("encoding", "gb2312");

                //    RequestUrls(info);

                //    ItemInfo resItem = null;

                //    foreach (string itemInfoStr in info.Contentes)
                //    {
                //        //string itemInfoStr = request.HttpGet(url, Encoding.GetEncoding("gb2312"));

                //        string sellNameInfo = CommonFun.GetValue(itemInfoStr, "<div class=\"right_property\">", "</div>");

                //        sellNameInfo = CommonFun.GetValue(itemInfoStr, "<h3>", "</h3>");

                //        if (!string.IsNullOrEmpty(sellNameInfo) && sellNameInfo != name)
                //        {
                //            //int count = 50;

                //            //if (toItem.Use != titleName)
                //            //{
                //            string itemId = CommonFun.GetValue(itemInfoStr, "itemid=\"", "\"");
                //            string pno = CommonFun.GetValue(itemInfoStr, "item_pno = '", "'");
                //            string provinceId = "20";
                //            string postData = string.Format("itemid={0}&pno={1}&provinceId={2}", itemId, pno, provinceId);
                //            string countInfo = request.HttpGet(cUrl + "?" + postData);
                //            //string countInfo = request.HttpPost(cUrl, postData);
                //            countInfo = CommonFun.GetValue(countInfo, "\\[\"", "\"");
                //            string[] arrCountInfo = countInfo.Split('_');
                //            int count = Convert.ToInt32(arrCountInfo[2]);
                //            //}

                //            if (count > 0)
                //            {
                //                if (isSpc || count > minStock)
                //                //if (toItem.Use != titleName && count > 10)
                //                {
                //                    ItemInfo item = GetOneItem(itemInfoStr);

                //                    string mformat = toItem.Format.Replace('s', '粒');
                //                    mformat = mformat.Replace('S', '粒');
                //                    mformat = mformat.Replace("毫克", "mg");
                //                    mformat = mformat.Replace("x", "*");
                //                    mformat = mformat.Replace("×", "*");

                //                    string pformat = item.Format.Replace('s', '粒');
                //                    pformat = pformat.Replace('S', '粒');
                //                    pformat = pformat.Replace("毫克", "mg");
                //                    pformat = pformat.Replace("x", "*");
                //                    pformat = pformat.Replace("×", "*");

                //                    string mItemNam = toItem.ItemName.Replace('s', '粒');
                //                    mItemNam = mItemNam.Replace('S', '粒');
                //                    mItemNam = mItemNam.Replace("毫克", "mg");
                //                    mItemNam = mItemNam.Replace("x", "*");
                //                    mItemNam = mItemNam.Replace("×", "*");

                //                    string pItemNam = item.ItemName.Replace('s', '粒');
                //                    pItemNam = pItemNam.Replace('S', '粒');
                //                    pItemNam = pItemNam.Replace("毫克", "mg");
                //                    pItemNam = pItemNam.Replace("x", "*");
                //                    pItemNam = pItemNam.Replace("×", "*");

                //                    if (isUseName)
                //                    {
                //                        if (CommonFun.GetNum(item.ID) == CommonFun.GetNum(toItem.ID) && mformat == pformat && item.Created == toItem.Created && mItemNam == pItemNam)
                //                        {
                //                            if (resItem == null || (resItem.ShopPrice > item.ShopPrice && item.ShopPrice > 0))
                //                            {
                //                                resItem = item;
                //                            }
                //                        }
                //                    }
                //                    else
                //                    {
                //                        if (resItem == null || (resItem.ShopPrice > item.ShopPrice && item.ShopPrice > 0))
                //                        {
                //                            string toItemName = toItem.ItemName.Replace(" ", "");
                //                            toItemName = toItemName.Replace('s', '粒');
                //                            toItemName = toItemName.Replace('S', '粒');
                //                            toItemName = toItemName.Replace("毫克", "mg");
                //                            toItemName = toItemName.Replace("x", "*");
                //                            toItemName = toItemName.Replace("×", "*");

                //                            string gItemName = item.ItemName.Replace(" ", "");
                //                            gItemName = gItemName.Replace('s', '粒');
                //                            gItemName = gItemName.Replace('S', '粒');
                //                            gItemName = gItemName.Replace("毫克", "mg");
                //                            gItemName = gItemName.Replace("x", "*");
                //                            gItemName = gItemName.Replace("×", "*");

                //                            if (toItemName == gItemName)
                //                            {
                //                                resItem = item;
                //                            }
                //                        }
                //                    }
                //                }
                //            }
                //        }
                //    }

                //    if (resItem != null)
                //    {
                //        return resItem;
                //    }
                //}
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return null;
        }

        public List<string> GetOnePageSeachItemUrls(string content, string itemName)
        {
            List<string> urls = new List<string>();

            content = CommonFun.GetValue(content, " <ul id=\"itemSearchList\" class=\"itemSearchList\">", "</ul>");

            MatchCollection iMs = CommonFun.GetValues(content, "<li", "</li>");

            foreach (Match m in iMs)
            {
                MatchCollection pMs = CommonFun.GetValues(m.Value, "<p class=\"titleBox\">", " </p>");

                MatchCollection nMs = CommonFun.GetValues(m.Value, "<span class=\"list_lable_business\"></span>", "</a>");

                if (nMs.Count > 0 && !string.IsNullOrEmpty(nMs[0].Value))
                {
                    //string[] nArray = nMs[0].Value.Trim().Split(' ');
                    //string[] mArray = brandName.Split(' ');
                    //if (nArray[0] == mArray[0])
                    //{
                    //string sItemName = GetFormat(nMs[0].Value.Trim());
                    //string rItemName = GetFormat(itemName);

                    //if (sItemName.Contains(rItemName) || rItemName.Contains(sItemName))
                    //{
                    urls.Add("http:" + CommonFun.GetValue(pMs[0].Value, "\" href=\"", "\""));
                    //}

                    //}

                }
            }

            return urls;
        }

        public List<string> GetOnePageSeachItemUrls(string content)
        {
            List<string> urls = new List<string>();

            content = CommonFun.GetValue(content, " <ul id=\"itemSearchList\" class=\"itemSearchList\">", "</ul>");

            MatchCollection iMs = CommonFun.GetValues(content, "<li", "</li>");

            foreach (Match m in iMs)
            {
                MatchCollection pMs = CommonFun.GetValues(m.Value, "<p class=\"titleBox\">", " </p>");

                //MatchCollection nMs = CommonFun.GetValues(m.Value, "<span class=\"list_lable_business\"></span>", "</a>");

                //if (nMs.Count > 0 && !string.IsNullOrEmpty(nMs[0].Value))
                //{
                //    string[] nArray = nMs[0].Value.Trim().Split(' ');
                //    string[] mArray = brandName.Split(' ');
                //    if (nArray[0] == mArray[0])
                //    {
                urls.Add("http:" + CommonFun.GetValue(pMs[0].Value, "\" href=\"", "\""));
                //}

                //}
            }

            return urls;
        }

        public void GetWinItem()
        {
            int totalCount = ShopAllItems.Count, curCount = 0;

            //对比数据
            ReadPlatFormWebPageValue flatform = new ReadPlatFormWebPageValue();

            foreach (KeyValuePair<string, BaseItemInfo> info in ShopAllItems)
            {
                try
                {
                    Console.WriteLine("{0},totalCount:{1}, curCount:{2}", DateTime.Now.ToString(), totalCount, ++curCount);

                    if ((DateTime.Now - startTime).TotalMinutes > 30)
                    {
                        flatform.Login();
                        startTime = DateTime.Now;
                    }

                    BaseItemInfo item = info.Value;

                    string key = item.Name + item.Format + item.Created;

                    SeachedItemFromPF(item.ID, flatform, 10);

                    if (seachItemList.ContainsKey(item.ID))
                    {
                        List<BaseItemInfo> compareItems = seachItemList[item.ID];
                        bool isExist = false;
                        foreach (BaseItemInfo compareItem in compareItems)
                        {
                            if (CommonFun.IsSameFormat(item.Format, compareItem.Format, item.Name, compareItem.Name))
                            {
                                isExist = true;
                                if (ComparePrice(compareItem, item))
                                {
                                    //上架
                                }
                            }
                        }

                        if (!isExist)
                        {
                            CommonFun.WriteCSV("YiYao/NoFormat" + ticks + ".csv", item);
                        }
                    }
                    else
                    {
                        CommonFun.WriteCSV("YiYao/NotExist" + ticks + ".csv", item);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
        }

        public void Test()
        {
            //DataTable data = CommonFun.ReadXLS("yy/test.xlsx");

            //for (int row = 0; row < data.Rows.Count; row++)
            //{
            //    try
            //    {
            //        BaseItemInfo item = new BaseItemInfo();
            //        item.ID = data.Rows[row]["批准文号"].ToString();
            //        item.Name = (string)data.Rows[row]["通用名称"].ToString();
            //        item.ItemName = data.Columns.Contains("商品名称") ? data.Rows[row]["商品名称"].ToString() : "";
            //        item.Created = (string)data.Rows[row]["生产厂家"].ToString();
            //        item.Format = (string)data.Rows[row]["包装规格"].ToString();
            //        string priceStr = (string)data.Rows[row]["平台售价（最低价格）"].ToString();
            //        item.ShopPrice = string.IsNullOrEmpty(priceStr) ? 9999 : Convert.ToDecimal(priceStr);
            //        item.PlatformPrice = item.ShopPrice;
            //        item.Type = data.Columns.Contains("剂型") ? (string)data.Rows[row]["剂型"].ToString() : "";
            //        item.Inventory = (string)data.Rows[row]["库存"].ToString();
            //        item.SellType = (string)data.Rows[row]["出售方式（零或整）"].ToString();

            //        string key = item.Name + item.Format + item.Created;
            //    }
            //    catch (Exception ex)
            //    {
            //        Console.WriteLine(ex);
            //    }
            //}
            //request.HttpGet("http://www.111.com.cn/product/51026872.html");
            Login();
            //ItemInfo tItem = new ItemInfo();
            //万乐 乌苯美司片 30mg*12片
            ItemInfo item = new ItemInfo();
            item.Name = "盐酸雷尼替丁胶囊";
            item.Format = "0.15gx20粒/盒";
            item.BrandName = "芊克";
            item.ItemName = "芊克 盐酸地尔硫卓控释胶囊 90mg*6粒";
            //tItem.ID = "	国药准字H20030905";
            //tItem.Created = "北京银谷世纪药业有限公司";
            //tItem.Name = "鲑鱼降钙素喷鼻剂";
            //tItem.Format = "20ug*28喷";
            //tItem.BrandName = "金尔力";
            //tItem.ItemName = "金尔力 鲑鱼降钙素喷鼻剂 20ug*28喷";
            //tItem.Type = "1234,4567";
            //CommonFun.WriteCSV("yy/test.csv", tItem);
            //for (int i = 0; i < 2; i++)
            //{
            //    GetYiYaoMinPriceItem(tItem, false, i == 1);
            //}
            UpNewItem(item);
        }

        public void SetItmeInfo(ItemInfo item, string content)
        {
            try
            {
                if (string.IsNullOrEmpty(content))
                {
                    return;
                }
                string meunInfo = CommonFun.GetValue(content, "<div class=\"detailnav\">", "</div>");
                MatchCollection mMs = CommonFun.GetValues(meunInfo, "html\\\">", "</a>");
                item.Menu1 = mMs[0].Value;
                item.Menu2 = mMs[1].Value;
                item.Menu3 = mMs[2].Value;

                string otherInfo = CommonFun.GetValue(content, "<div class=\"goods_intro\">", "</div>");

                MatchCollection td3Ms = CommonFun.GetValues(otherInfo, "<td colspan=\"3\">", "</td>");
                item.ItemName = td3Ms[0].Value;

                string[] nameInfo = item.ItemName.Split(' ');

                if (nameInfo.Length == 3)
                {
                    item.BrandName = nameInfo[0];
                    item.Name = nameInfo[1];
                    item.Format = nameInfo[2];
                }
                else if (nameInfo.Length == 4)
                {
                    item.BrandName = nameInfo[1];
                    item.Name = nameInfo[2];
                    item.Format = nameInfo[3];
                }
                else
                {
                    Console.WriteLine(item.ItemName);
                }

                MatchCollection tdMs = CommonFun.GetValues(otherInfo, "<td>", "</td>");
                item.ID = tdMs[4].Value.IndexOf('(') > 0 ? tdMs[4].Value.Substring(0, tdMs[4].Value.IndexOf('(')) : "";
                item.ID = item.ID.Trim();

                item.Format = tdMs[1].Value;
                item.Weight = string.IsNullOrEmpty(tdMs[2].Value) || tdMs[2].Value.Contains("E") ? 0 : Convert.ToDecimal(tdMs[2].Value.Substring(0, tdMs[2].Value.IndexOf('k')));
                item.Created = tdMs[3].Value;

                string id = CommonFun.GetValue(content, "var item_id = '", "'");

                string priceInfo = request.HttpGet(string.Format("http://www.111.com.cn/items/getComboInfo.action?id={0}&type=ZSP&_=1515903723882", id));

                //[{"id":2264968,"itemId":50658819,"productName":"【3盒装】","mainId":845124,"recommendPrice":1245.0,"originalPrice":1245.0,"materialtype":"TCP","details":[{"areaItemId":845124,"comboId":0,"detailPriority":1000,"detailCount":3,"detailType":1,"detailStatus":1,"detailPrice":415.0,"minusPrice":0.0}]},{"id":2333985,"itemId":50701935,"productName":"【多送20片避孕套+20ml润滑液】","mainId":845124,"recommendPrice":450.8,"originalPrice":440.8,"materialtype":"TCP","details":[{"areaItemId":845124,"comboId":0,"detailPriority":1000,"detailCount":1,"detailType":1,"detailStatus":1,"detailPrice":415.0,"minusPrice":0.0},
                //{"areaItemId":869780,"comboId":0,"detailPriority":999,"detailCount":2,"detailType":0,"detailStatus":1,"detailPrice":2.9,"minusPrice":5.0},
                //{"areaItemId":1108596,"comboId":0,"detailPriority":998,"detailCount":4,"detailType":0,"detailStatus":1,"detailPrice":5.0,"minusPrice":0.0}]},
                //{"id":2428496,"itemId":50766560,"productName":"防早泄，搭配必利劲效果更佳","mainId":845124,"recommendPrice":643.0,"originalPrice":635.0,"materialtype":"TCP","details":[{"areaItemId":845124,"comboId":0,"detailPriority":1000,"detailCount":1,"detailType":1,"detailStatus":1,"detailPrice":415.0,"minusPrice":0.0},{"areaItemId":874608,"comboId":0,"detailPriority":999,"detailCount":1,"detailType":0,"detailStatus":1,"detailPrice":220.0,"minusPrice":8.0}]}
                //,{"id":845124,"itemId":974905,"productName":"希爱力 他达拉非片 20mg*4片","mainId":0,"recommendPrice":495.0,"originalPrice":415.0,"materialtype":"ZSP","details":null}]
                MatchCollection idMs = CommonFun.GetValues(priceInfo, "\"id\":", ",");

                MatchCollection opMs = CommonFun.GetValues(priceInfo, "originalPrice\":", ",");

                MatchCollection rpMs = CommonFun.GetValues(priceInfo, "recommendPrice\":", ",");

                for (int i = 0; i < idMs.Count; i++)
                {
                    if (id == idMs[i].Value)
                    {
                        decimal price = 0;

                        string originalPrice = opMs[i].Value;

                        if (string.IsNullOrEmpty(originalPrice))
                        {
                            string recommendPrice = rpMs[i].Value;

                            price = string.IsNullOrEmpty(recommendPrice) ? 0 : Convert.ToDecimal(recommendPrice);
                        }
                        else
                        {
                            price = Convert.ToDecimal(originalPrice);
                        }

                        item.PlatformPrice = price;
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                SetItmeInfo(item, content);
            }
        }
        public override void ReadAllItem()
        {
            string getOtherFormatUrl = "http://www.111.com.cn/interfaces/series.action";
            string postStr = "itemid={0}&pno={1}&provinceId=20";
            List<string> name_list = new List<string>();
            foreach (string itemUrl in AllItemUrl)
            {
                try
                {
                    string content = request.HttpGet(itemUrl, Encoding.GetEncoding("gb2312"));

                    BaseItemInfo item = GetOneItem(content);

                    AddItemNew(item, "YiYao/YiYao" + ticks + "_" + menuIndex + ".csv");

                    if (!name_list.Contains(item.Name))
                    {
                        name_list.Add(item.Name);
                        CommonFun.WriteCSV("YiYao/YiYao" + ticks + "_names" + ".csv", item);
                    }
                    //{"series_product":[{"stock":"1261","status":"8","item_id":"974905","attribute_id_3":"20mg*4片","attribute":"20mg*4片","image":"https://p4.maiyaole.com/img/item/201709/28/65_20170928135453559.jpg","product_no":"0009749057"},{"stock":"4288","status":"8","item_id":"987612","attribute_id_3":"20mg*1片","attribute":"20mg*1片","image":"https://p3.maiyaole.com/img/item/201709/28/65_20170928135130841.jpg","product_no":"0009876124"},{"stock":"2173","status":"8","item_id":"6477888","attribute_id_3":"20mg*8片","attribute":"20mg*8片","image":"https://p3.maiyaole.com/img/item/201709/28/65_20170928135308415.jpg","product_no":"0064778887"},{"stock":"7850","status":"8","item_id":"50006546","attribute_id_3":"5mg*28片","attribute":"5mg*28片","image":"https://p3.maiyaole.com/img/item/201709/28/65_20170928134910614.jpg","product_no":"1598970255"}],"stocks":["0009749057_3_1261","0009876124_3_4288","0064778887_3_2173","1598970255_3_7850"],"series_info":[{"规格":[{"value":"20mg*4片","priority":1,"image":null,"isMain":"0"},{"value":"20mg*1片","priority":2,"image":null,"isMain":"0"},{"value":"20mg*8片","priority":3,"image":null,"isMain":"0"},{"value":"5mg*28片","priority":4,"image":null,"isMain":"0"}],"show_pic":"0","attribute_id":"3","attribute_name":"规格"}]}
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }

            foreach (BaseItemInfo item in ShopAllItems.Values)
            {
                CommonFun.WriteCSV("YiYao/YiYao" + ticks + ".csv", item);
            }
        }
        public override bool ComparePrice(BaseItemInfo platformItem, BaseItemInfo info)
        {
            if (info.ShopPrice > 0 && info.ShopPrice * (decimal)0.6 >= platformItem.ShopSelaPrice)
            {
                info.PlatformPrice = platformItem.ShopSelaPrice;
                info.Type = platformItem.Type;
                info.ViewCount = platformItem.ViewCount;

                if (!string.IsNullOrEmpty(platformItem.ViewCount) && Convert.ToInt32(platformItem.ViewCount) > 3000)
                {
                    CommonFun.WriteCSV("YiYao/3000-40以上" + ticks + ".csv", info);
                }
                else if (!string.IsNullOrEmpty(platformItem.ViewCount) && Convert.ToInt32(platformItem.ViewCount) > 2000)
                {
                    CommonFun.WriteCSV("YiYao/2000-40以上" + ticks + ".csv", info);
                }
                else if (!string.IsNullOrEmpty(platformItem.ViewCount) && Convert.ToInt32(platformItem.ViewCount) > 1000)
                {
                    CommonFun.WriteCSV("YiYao/1000-40以上" + ticks + ".csv", info);
                }
                else if (!string.IsNullOrEmpty(platformItem.ViewCount) && Convert.ToInt32(platformItem.ViewCount) > 500)
                {
                    CommonFun.WriteCSV("YiYao/500-40以上" + ticks + ".csv", info);
                }
                else
                {
                    CommonFun.WriteCSV("YiYao/40以上" + ticks + ".csv", info);
                }
                return true;
            }
            return false;
        }

        public override void ReadAllMenuURL()
        {
            string url = "http://www.111.com.cn";

            string content = request.HttpGet(url);


            MatchCollection ms = CommonFun.GetValues(content, "<li class=\"stitle\">", "data-sjtj=");

            if (menuIndex == -1)
            {
                foreach (Match m in ms)
                {
                    GetOneTypeMenuURL(m.Value);
                }
            }
            else
            {
                GetOneTypeMenuURL(ms[menuIndex].Value);
            }

            ReadAllItemUrl();
        }

        private void GetOneTypeMenuURL(string menuInfo)
        {
            string content = request.HttpGet(CommonFun.GetValue(menuInfo, "href=\"", "\""));

            content = CommonFun.GetValue(content, "<div class=\"itemChooseBox\">", "<div class=\"info_box mt\">");

            MatchCollection ddMs = CommonFun.GetValues(content, "<li>", "</li>");

            foreach (Match m in ddMs)
            {
                MatchCollection urlMs = CommonFun.GetValues(m.Value, "href=\"", "\"");

                foreach (Match uM in urlMs)
                {
                    if (!AllMenuUrl.Contains(uM.Value))
                    {
                        AllMenuUrl.Add(uM.Value);
                    }
                }
            }
        }

        /// <summary>
        /// 同步药房物品
        /// </summary>
        public void SyncYFItems()
        {
            try
            {
                //获取药房物品在售列表
                ReadPlatFormWebPageValue yf = new ReadPlatFormWebPageValue();
                yf.Login(2);
                Dictionary<string, BaseItemInfo> yfItems = yf.GetSellingItems();

                //获取一号网在售列表
                Login();
                Dictionary<string, BaseItemInfo> yyItems = GetSellingItems();

                //比对上下架并同步库存
                foreach (BaseItemInfo yfItem in yfItems.Values)
                {
                    bool isInSellingList = false;
                    foreach (BaseItemInfo yyItem in yyItems.Values)
                    {
                        if (CommonFun.IsSameFormat(yfItem.Format, yyItem.Format, yfItem.Name, yfItem.Name))
                        {
                            //同步库存
                            isInSellingList = true;
                            break;
                        }
                    }

                    if (!isInSellingList)
                    {
                        //上架
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public void ReadAllItemUrl()
        {
            int total = AllMenuUrl.Count;
            int cur = 0;
            foreach (string menuUrl in AllMenuUrl)
            {
                cur++;
                string content = request.HttpGet(menuUrl);

                string pageStr = CommonFun.GetValue(content, "<input type=\"hidden\" id=\"totalPageId\" value=\"", "\"");

                Console.WriteLine("Total:{0}, Cur:{1}, Page:{3} URL:{2}", total, cur, menuUrl, pageStr);

                if (!string.IsNullOrEmpty(pageStr))
                {
                    int page = Convert.ToInt32(pageStr);

                    for (int i = 1; i <= page; i++)
                    {
                        content = request.HttpGet(menuUrl + "-" + i + ".html");

                        content = CommonFun.GetValue(content, "\"itemSearchList\">", "class=\"turnPageBottom\">");
                        MatchCollection ms = CommonFun.GetValues(content, "<input type=\"hidden\"", "\"product_pic pro_img\">");

                        foreach (Match m in ms)
                        {
                            string itemUrl = CommonFun.GetValue(m.Value, "<a href=\"", "\"");
                            if (!AllItemUrl.Contains(itemUrl))
                            {
                                AllItemUrl.Add(itemUrl);
                            }
                        }
                    }
                }
            }
            ReadAllItem();
        }

        public void UpdateBaseInfo()
        {

        }

        /// <summary>
        /// 更新价格和库存
        /// </summary>
        /// <param name="popItemId"></param>
        /// <param name="originalPrice"></param>
        /// <param name="recommendPrice"></param>
        public void UpdatePirceAndQuantity(string popItemId, string originalPrice, string recommendPrice, string quantity, string oldOriginalPrice = null)
        {
            if (!string.IsNullOrEmpty(originalPrice))
            {
                string url = "http://popadmin.111.com.cn/admin/itemlist/updateOriginalPrice.action?itemCondition.originalPrice={0}&itemCondition.popItemId={1}";
                string content = request.HttpGet(string.Format(url, originalPrice, popItemId));

                //限价逻辑
                if (content.Contains("false"))
                {
                    BaseItemInfo item = new BaseItemInfo();
                    item.ID = popItemId;
                    item.ShopPrice = Convert.ToDecimal(originalPrice);
                    item.Remark = content;

                    string limitPriceStr = CommonFun.GetValue(content, "price\":", ",");
                    if (!string.IsNullOrEmpty(limitPriceStr) && limitPriceStr.Trim() != oldOriginalPrice.Trim())
                    {
                        item.ViewCount = request.HttpGet(string.Format(url, limitPriceStr, popItemId));
                    }

                    CommonFun.WriteCSV(fileName + "updateFailed" + ticks + ".csv", item);
                }
            }

            if (!string.IsNullOrEmpty(recommendPrice))
            {
                string url = string.Format("http://popadmin.111.com.cn/admin/itemlist/updateRecommendPrice.action?itemCondition.recommendPrice={0}&itemCondition.popItemId={1}", recommendPrice, popItemId);
                request.HttpGet(url);
            }

            if (!string.IsNullOrEmpty(quantity))
            {
                string url = string.Format("http://popadmin.111.com.cn/admin/itemlist/updateQuantity.action?itemCondition.quantity={0}&itemCondition.popItemId={1}", quantity, popItemId);
                request.HttpGet(url);
            }
        }

        /// <summary>
        /// 发布新品
        /// </summary>
        public bool UpNewItem(BaseItemInfo item)
        {
            try
            {
                //1、查找基本信息
                string sUrl = "http://popadmin.111.com.cn/admin/item/getItems.action";

                string sDataPostStr = string.Format("skuName={0}", CommonFun.GetUrlEncode(item.Name));
                string itemsContent = request.HttpPost(sUrl, sDataPostStr);

                itemsContent = itemsContent.Replace("attrs:{", "\"");
                itemsContent = itemsContent.Replace("\"}", "\"");
                MatchCollection ms = CommonFun.GetValues(itemsContent, "{", "}");

                string id = null;
                string productNO = null;
                string catalogId = null;
                string catalogName = null;
                string firstCatalogId = null;
                string firstCategoryName = null;
                string secondCategoryName = null;
                string productName = null;
                string approvalnum = null;
                string norms = null;
                string brandName = null;
                string manufacturer = null;
                string barCode = null;
                string weight = null;
                string prescription = null;
                string itemId = null;
                foreach (Match m in ms)
                {
                    string format = CommonFun.GetValue(m.Value, "\"norms\":\"", "\"");
                    approvalnum = CommonFun.GetValue(m.Value, "\"approvalnum\":\"", "\"");
                    if (approvalnum.Trim() == item.ID && CommonFun.IsSameFormat(format, item.Format))
                    {
                        itemId = CommonFun.GetValue(m.Value, "\"id\":", ",");
                        id = CommonFun.GetValue(m.Value, "\"skuId\":", ",");
                        productNO = CommonFun.GetValue(m.Value, "\"productNo\":\"", "\"");
                        catalogId = CommonFun.GetValue(m.Value, "\"catalogId\":", ",");
                        catalogName = CommonFun.GetValue(m.Value, "\"catalogName\":\"", "\"");
                        firstCatalogId = CommonFun.GetValue(m.Value, "\"firstCatalogId\":", ",");
                        firstCategoryName = CommonFun.GetValue(m.Value, "\"firstCatalogName\":\"", "\"");
                        secondCategoryName = CommonFun.GetValue(m.Value, "\"secondCatalogName\":\"", "\"");
                        productName = CommonFun.GetValue(m.Value, "\"productName\":\"", "\"");
                        approvalnum = CommonFun.GetValue(m.Value, "\"approvalnum\":\"", "\"");
                        norms = CommonFun.GetValue(m.Value, "\"norms\":\"", "\"");
                        brandName = CommonFun.GetValue(m.Value, "\"brandName\":\"", "\"");
                        manufacturer = CommonFun.GetValue(m.Value, "\"manufacturer\":\"", "\"");
                        barCode = CommonFun.GetValue(m.Value, "\"barcode\":\"", "\"");
                        weight = CommonFun.GetValue(m.Value, "\"weight\":", ",");
                        prescription = CommonFun.GetValue(m.Value, "\"prescription\":", ",");
                        break;
                    }
                }

                if (!string.IsNullOrEmpty(id))
                {
                    //2、选择发布 商品id
                    string pUrl = "http://popadmin.111.com.cn/admin/item/checkSkuId.action";
                    string pDataStr = string.Format("skuId={0}", id);
                    string pContent = request.HttpPost(pUrl, pDataStr);

                    if (pContent == "1")
                    {
                        Console.WriteLine("The item is up now id:{0} format:{1} created:{2}", item.ID, item.Format, item.Created);
                        return false;
                    }

                    //3、保存前检测  
                    //string cUrl = "http://popadmin.111.com.cn/admin/item/checkNewItemStandard.action";
                    //string cDataStr = string.Format("approvalnum={1}&norms={2}&skuName={0}", HttpUtility.UrlEncode(productName, Encoding.GetEncoding("utf-8")).ToUpper(), CommonFun.GetUrlEncode(approvalnum), CommonFun.GetUrlEncode(norms, false));
                    //string cContent = request.HttpPost(cUrl, cDataStr);

                    //string itemId = CommonFun.GetValue(cContent, "\"itemId\":", ",");
                    //if (string.IsNullOrEmpty(itemId))
                    //{
                    //    itemId = CommonFun.GetValue(cContent, "\"itemId\":", "}");
                    //}
                    //4、保存
                    string saveUrl = "http://popadmin.111.com.cn/admin/item/saveItemBaseInfo.action?pageType=itempublish_old";
                    string sDataStr = string.Format("tempProduct.popItemId=&tempProduct.itemId=&tempProduct.skuId={0}&tempProduct.productNo={1}&venderService=&errorMsg=&tempProduct.catalogId={2}&tempProduct.secondCategoryId=&tempProduct.firstCategoryId =&brandId=&itemId={3}&tempProduct.catalogName={4}&brandname=&fistCatalogId={5}&tempProduct.firstCategoryName={6}&tempProduct.secondCategoryName={7}&tempProduct.outerItemId=null&tempProduct.outerSkuId=null&tempProduct.productName={8}&tempProduct.approvalnum={9}&tempProduct.norms={10}&tempProduct.brandName={11}&tempProduct.manufacturer={12}&tempProduct.weight={14}&tempProduct.barCode={13}&tempProduct.isHaiTao=0&tempProduct.prescription={15}",
                        id, productNO, catalogId, itemId, HttpUtility.UrlEncode(catalogName, Encoding.GetEncoding("utf-8")), firstCatalogId, HttpUtility.UrlEncode(firstCategoryName, Encoding.GetEncoding("utf-8")), HttpUtility.UrlEncode(secondCategoryName, Encoding.GetEncoding("utf-8")), HttpUtility.UrlEncode(productName, Encoding.GetEncoding("utf-8")), HttpUtility.UrlEncode(approvalnum, Encoding.GetEncoding("utf-8")), HttpUtility.UrlEncode(norms, Encoding.GetEncoding("utf-8")), HttpUtility.UrlEncode(brandName, Encoding.GetEncoding("utf-8")), HttpUtility.UrlEncode(manufacturer, Encoding.GetEncoding("utf-8")), barCode, weight, prescription);
                    string uri = "";
                    Dictionary<string, string> heads = new Dictionary<string, string>();
                    heads.Add("Referer", "http://popadmin.111.com.cn/admin/item/queryItemByPop.action");
                    heads.Add("Content-Type", "application/x-www-form-urlencoded");
                    heads.Add("User-Agent", "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/70.0.3538.77 Safari/537.36");
                    string content = request.HttpPost(saveUrl, sDataStr, ref uri);
                    //string content = "";
                    string getPopItemIdUrl = "http://popadmin.111.com.cn/admin/itemlist/showItem.action";
                    string postData = string.Format("venderService=0&tagInx=0&itemCondition.pageIndex=1&itemCondition.searchType=1&itemCondition.productName={0}&itemCondition.productNos=&itemCondition.catalogName=&itemCondition.catalogId=&itemCondition.catalogLevel=&itemCondition.inshopCatalogName=&itemCondition.inshopCatalogId=&itemCondition.inshopCatalogLevel=&itemCondition.checkStatus=&itemCondition.brandCheckStatus=&itemCondition.pageSize=10", CommonFun.GetUrlEncode(productName));
                    content = request.HttpPost(getPopItemIdUrl, postData);
                    content = CommonFun.GetValue(content, "<td class=\"noBorLeft\">", "</td>");
                    string popItemId = CommonFun.GetValue(content, "value=\"", "\""); //uri.Split('?')[1];
                    //5、获取新发布商品信息
                    string qUrl = string.Format("http://popadmin.111.com.cn/admin/item/queryItemByPop.action?popItemId={0}&tagInx=0&brandCheckStatus=1", popItemId);

                    string qContent = request.HttpGet(qUrl);
                    qContent = CommonFun.GetValue(qContent, "<form name=\"baseInfoForm\" id=\"baseInfoForm\" method=\"post\">", "</form>");
                    string brandId = CommonFun.GetValue(qContent, "\"brandId\" value=\"", "\"");
                    string venderId = CommonFun.GetValue(qContent, "venderId\" value=\"", "\"");
                    string tagInx = CommonFun.GetValue(qContent, "tagInx\" value=\"", "\"");
                    string brandCheckStatus = CommonFun.GetValue(qContent, "brandCheckStatus\" value=\"", "\"");
                    //是否无理由退货
                    string isEdit = CommonFun.GetValue(qContent, "isEdit\" value=\"", "\"");
                    //店铺分类
                    string inShopCataIds = "22207";
                    //市场价
                    string recommendPrice = "103";
                    //销售价
                    string originalPrice = "100";
                    //库存
                    string quantity = "20";
                    string storeSchemeId = "3681";
                    string frightTemplateId = "2258";

                    //6、填写价格和分类
                    //popItemId = CommonFun.GetValue(popItemId, "popItemId=", "&");
                    string wUrl = "http://popadmin.111.com.cn/admin/item/saveItemBaseInfo.action?pageType=itempublish_old&isOld=true";
                    string wData = string.Format("tempProduct.popItemId={0}&tempProduct.itemId={1}&tempProduct.skuId={2}&tempProduct.productNo=&venderService=0&errorMsg=save_suc&firstCategoryId=&catalogId={3}&brandId={4}&tempProduct.venderId={5}&tagInx={6}&tempProduct.brandCheckStatus={7}&tempProduct.brandName={8}&isEdit=false&tempProduct.catalogName={9}&tempProduct.catalogId={10}&tempProduct.brandId={11}&itemId={12}&tempProduct.productName={13}&tempProduct.productSubTitle=&tempProduct.barCode=&tempProduct.approvalnum=&tempProduct.approvalnum={14}&tempProduct.norms={15}&tempProduct.exChangedDay=0&tempProduct.haiTaoAddress=0&tempProduct.haiTaoCountry=&tempProduct.isHaiTao=0&tempProduct.inshopCataId=&inShopCataIds={16}&tempProduct.recommendPrice={17}&tempProduct.originalPrice={18}&tempProduct.weight={19}&tempProduct.quantity={20}&tempProduct.extProductNo=&tempProduct.storeSchemeId={21}&tempProduct.frightTemplateId={22}&ImgId=&btocPictureId=&pictureId=&imgPic=&imgtxt=&imgtxthid=&ImgId=&btocPictureId=&pictureId=&imgPic=&imgtxt=&imgtxthid=&ImgId=&btocPictureId=&pictureId=&imgPic=&imgtxt=&imgtxthid=&ImgId=&btocPictureId=&pictureId=&imgPic=&imgtxt=&imgtxthid=&ImgId=&btocPictureId=&pictureId=&imgPic=&imgtxt=&imgtxthid=&ImgId=&btocPictureId=&pictureId=&imgPic=&imgtxt=&imgtxthid=&descModel.itemId={23}"
                        , popItemId, itemId, id, catalogId, brandId, venderId, tagInx, brandCheckStatus, brandName, catalogName, catalogId, brandId, itemId, productName, approvalnum, norms, inShopCataIds, recommendPrice, originalPrice, weight, quantity, storeSchemeId, frightTemplateId, itemId);
                    content = request.HttpPost(wUrl, wData);
                    return true;
                    //7、获取新发布商品信息http://popadmin.111.com.cn/admin/item/queryItemByPop.action?popItemId=889041&errorMsg=save_suc
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            return false;
        }
    }
}
