using GetWebPageDate.Http;
using GetWebPageDate.Util.Item;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace GetWebPageDate.Util
{
    public class BaseReadWebPage : IReadWebPage
    {
        protected long ticks = DateTime.Now.Ticks;

        protected static Random random = new Random((int)DateTime.Now.Ticks);

        /// <summary>
        /// 左氧   阿昔洛韦 头孢  霉素   磷酸奥司他韦  甲硝唑  阿莫  克拉 泛昔洛韦   氟康唑   注射   沙星 伊曲康唑
        /// </summary>
        protected List<string> unloadList = new List<string>() { "左氧", "阿昔洛韦", "头孢", "霉素", "磷酸奥司他韦", "甲硝唑", "阿莫", "克拉", "泛昔洛韦", "氟康唑", "注射", "沙星", "伊曲康唑", "米诺环", "利巴韦林", "环素", "联磺甲氧苄啶片" };

        /// <summary>
        /// 国药号黑名单
        /// </summary>
        protected List<string> idBlackList = new List<string>();

        /// <summary>
        /// 商家黑名单
        /// </summary>
        protected List<string> storeBlacklist = new List<string>();

        /// <summary>
        /// 厂家黑名单
        /// </summary>
        protected List<string> createdBlackList = new List<string>();

        /// <summary>
        /// 改价标记
        /// </summary>
        protected List<string> typeList = new List<string>();

        /// <summary>
        /// 最低库存改价标准
        /// </summary>
        protected List<string> minStockList = new List<string>();

        /// <summary>
        /// 获取在售列表
        /// </summary>
        protected Dictionary<string, BaseItemInfo> sellItems = new Dictionary<string, BaseItemInfo>();

        /// <summary>
        /// 获取下架列表
        /// </summary>
        protected Dictionary<string, BaseItemInfo> downItems = new Dictionary<string, BaseItemInfo>();

        /// <summary>
        /// 以搜索的物品
        /// </summary>
        protected Dictionary<string, string> seachedItemID = new Dictionary<string, string>();

        /// <summary>
        /// 药房平台搜索数据
        /// </summary>
        protected Dictionary<string, BaseItemInfo> seachPlatItems = new Dictionary<string, BaseItemInfo>();

        /// <summary>
        /// 药房平台所有列表
        /// </summary>
        protected Dictionary<string, List<BaseItemInfo>> seachItemList = new Dictionary<string, List<BaseItemInfo>>();

        /// <summary>
        /// 固定表
        /// </summary>
        protected Dictionary<string, BaseItemInfo> unUpdate = new Dictionary<string, BaseItemInfo>();

        /// <summary>
        /// 不更新价格表
        /// </summary>
        protected Dictionary<string, BaseItemInfo> unUpdatePrice = new Dictionary<string, BaseItemInfo>();

        protected DateTime startTime = DateTime.MinValue;

        protected string dateStr = DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss");

        protected string fileExtendName = ".csv";
        /// <summary>
        /// 所有菜单URL
        /// </summary>
        private List<string> allMenuUrl = new List<string>();

        public List<string> AllMenuUrl
        {
            get { return allMenuUrl; }
            set { allMenuUrl = value; }
        }

        /// <summary>
        /// 所有商品URL
        /// </summary>
        private List<string> allItemUrl = new List<string>();

        public List<string> AllItemUrl
        {
            get { return allItemUrl; }
            set { allItemUrl = value; }
        }

        /// <summary>
        /// 商品列表
        /// </summary>
        private Dictionary<string, BaseItemInfo> shopAllItems = new Dictionary<string, BaseItemInfo>();

        public Dictionary<string, BaseItemInfo> ShopAllItems
        {
            get { return shopAllItems; }
            set { shopAllItems = value; }
        }

        protected HttpRequest request = new HttpRequest();

        protected string url;

        protected string userName;

        protected string password;

        protected decimal lPrice;

        protected int minStock;

        protected int clickingRate;

        protected int minDownRate;

        public BaseReadWebPage()
        {
            unloadList = GetConfigList("nameKey");

            idBlackList = GetConfigList("idKey");

            storeBlacklist = GetConfigList("storeKey");

            createdBlackList = GetConfigList("createdKey");

            typeList = GetConfigList("typeKey");

            minStockList = GetConfigList("minStockKey", true);

            string lPriceStr = ConfigurationManager.AppSettings["lPriceKey"];

            lPrice = string.IsNullOrEmpty(lPriceStr) ? (decimal)0 : Convert.ToDecimal(lPriceStr);

            string minStockStr = ConfigurationManager.AppSettings["stockKey"];

            minStock = string.IsNullOrEmpty(minStockStr) ? 10 : Convert.ToInt32(minStockStr);

            string clickingRateStr = ConfigurationManager.AppSettings["clickingRateKey"];

            clickingRate = string.IsNullOrEmpty(clickingRateStr) ? 100 : Convert.ToInt32(clickingRateStr);

             string minDownRateStr = ConfigurationManager.AppSettings["minDownRateKey"];

             minDownRate = string.IsNullOrEmpty(minDownRateStr) ? 100 : Convert.ToInt32(minDownRateStr);

            unUpdate = ReadXlsItems("KTUnUpdate.xlsx");

            unUpdatePrice = ReadXlsItems("UnUpdatePrice.xlsx");

            ThreadPool.SetMaxThreads(10, 10);
        }



        protected Dictionary<string, BaseItemInfo> ReadXlsItems(string fileName)
        {
            Dictionary<string, BaseItemInfo> items = new Dictionary<string, BaseItemInfo>();
            try
            {
                DataTable data = CommonFun.ReadXLS(fileName);

                for (int row = 0; row < data.Rows.Count; row++)
                {
                    try
                    {
                        BaseItemInfo item = new BaseItemInfo();
                        item.ID = data.Rows[row]["批准文号"].ToString();
                        item.Name = (string)data.Rows[row]["通用名称"].ToString();
                        item.ItemName = data.Columns.Contains("商品名称") ? data.Rows[row]["商品名称"].ToString() : "";
                        item.Created = (string)data.Rows[row]["生产厂家"].ToString();
                        item.Format = (string)data.Rows[row]["包装规格"].ToString();
                        string priceStr = (string)data.Rows[row]["平台售价（最低价格）"].ToString();
                        item.ShopPrice = string.IsNullOrEmpty(priceStr) ? 9999 : Convert.ToDecimal(priceStr);
                        item.PlatformPrice = item.ShopPrice;
                        item.Type = data.Columns.Contains("剂型") ? (string)data.Rows[row]["剂型"].ToString() : "";
                        item.Inventory = (string)data.Rows[row]["库存"].ToString();
                        item.SellType = (string)data.Rows[row]["出售方式（零或整）"].ToString();

                        string key = item.Name + item.Format + item.Created;
                        if (!items.ContainsKey(key))
                        {
                            items.Add(key, item);
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

            return items;
        }

        protected void UpdateAppConfig(string key, string value)
        {
            try
            {
                Configuration cnf = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

                cnf.AppSettings.Settings[key].Value = value;

                cnf.Save();

                ConfigurationManager.RefreshSection("appSettings");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public HttpRequest Request { get { return request; } }

        protected List<string> GetConfigList(string configName, bool canRe = false)
        {
            List<string> list = new List<string>();
            try
            {
                string configInfo = ConfigurationManager.AppSettings[configName];
                if (!string.IsNullOrEmpty(configInfo))
                {
                    string[] items = configInfo.Split(',');
                    foreach (string item in items)
                    {
                        string name = item.Trim();
                        if (!list.Contains(name) || canRe)
                        {
                            list.Add(name);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            return list;
        }

        /// <summary>
        /// 是否需要加载
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        protected bool CanLoad(string name, string id)
        {
            foreach (string idBlack in idBlackList)
            {
                if (id.Contains(idBlack))
                {
                    return false;
                }
            }

            foreach (string nuload in unloadList)
            {
                if (name.Contains(nuload))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 是否需要改价
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        protected bool CanUpDatePrice(string key)
        {
            if (!unUpdatePrice.ContainsKey(key))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 是否在商家黑名单中
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        protected bool IsBlacklistStore(string name)
        {
            name = name.Trim();

            foreach (string storeBlack in storeBlacklist)
            {
                if (name.Contains(storeBlack))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 是否在列表中
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        protected bool IsInTypeList(string type)
        {
            return typeList.Contains(type);
        }

        public void ReadAllItemUrl(string fileName)
        {
            string content = CommonFun.ReadCSV(fileName);

            if (string.IsNullOrEmpty(content))
            {
                string[] lines = content.Split('\r');

                foreach (string line in lines)
                {
                    allItemUrl.Add(line);
                }
            }
            else
            {
                Console.WriteLine("ReadAllItemUrl error" + fileName);
            }
        }

        protected decimal GetNoProfitItemPrice(decimal compaceItemPrice)
        {
            if (compaceItemPrice > 100)
            {
                compaceItemPrice *= (decimal)1.4;
            }
            else if (compaceItemPrice > 50)
            {
                compaceItemPrice *= (decimal)1.8;
            }
            else if (compaceItemPrice > 15)
            {
                compaceItemPrice *= (decimal)2;
            }
            else if (compaceItemPrice > 5)
            {
                compaceItemPrice *= (decimal)3;
            }
            else
            {
                compaceItemPrice *= (decimal)4;
            }

            return Math.Round(compaceItemPrice, 2);
        }

        public void SeachedItemFromPF(string id, ReadPlatFormWebPageValue flatform, int inventoryMin = 5)
        {
            //查找该商品
            if (!seachedItemID.ContainsKey(id))
            {
                seachedItemID.Add(id, id);

                List<BaseItemInfo> item_list = flatform.SeachInfoByID(id, inventoryMin);

                Dictionary<string, BaseItemInfo> minPricItems = new Dictionary<string, BaseItemInfo>();


                foreach (BaseItemInfo sItem in item_list)
                {
                    string sItemKey = sItem.ID + sItem.Format + sItem.Created;

                    if (!seachPlatItems.ContainsKey(sItemKey))
                    {
                        seachPlatItems.Add(sItemKey, sItem);
                        minPricItems.Add(sItemKey, sItem);
                    }
                    else if (seachPlatItems[sItemKey].ShopSelaPrice > sItem.ShopSelaPrice)
                    {
                        seachPlatItems[sItemKey] = sItem;
                        minPricItems[sItemKey] = sItem;
                    }
                }

                seachItemList.Add(id, minPricItems.Values.ToList());
            }
        }

        public void ReadBaseItemInfo(string fileName, bool isBaseItemInfo)
        {
            //DataTable data = CommonFun.ReadXLS(fileName);
            string content = CommonFun.ReadCSV(fileName);

            if (!string.IsNullOrEmpty(content))
            {
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
                            string newLine = line;

                            MatchCollection ms = CommonFun.GetValues(newLine, "\"\"\"", "\"\"\"");

                            for (int i = 0; i < ms.Count; i++)
                            {
                                if (!string.IsNullOrEmpty(ms[i].Value))
                                {
                                    newLine = newLine.Replace(ms[i].Value, string.Format("{{0}}", i));
                                }
                            }

                            string[] infoStr = newLine.Split(',');

                            for (int i = 0; i < infoStr.Length; i++)
                            {
                                for (int j = 0; j < ms.Count; j++)
                                {
                                    infoStr[i] = infoStr[i].Replace(string.Format("{{0}}", j), ms[j].Value);
                                }
                            }

                            if (infoStr.Length > 1)
                            {
                                BaseItemInfo info = isBaseItemInfo ? new BaseItemInfo() : new ItemInfo();

                                string[] menuName = info.GetLogHeadLine().Split(',');

                                SetBaseItemInfo(info, infoStr, menu);

                                if (!isBaseItemInfo)
                                {
                                    SetItemInfo(info as ItemInfo, infoStr, menu);
                                }

                                string key = info.Name + info.Format + info.Created;

                                if (!shopAllItems.ContainsKey(key))
                                {
                                    shopAllItems.Add(key, info);
                                }
                                else if (shopAllItems[key].ShopPrice > info.ShopPrice)
                                {
                                    shopAllItems[key] = info;
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
            else
            {
                Console.WriteLine("ReadBaseItemInfo error" + fileName);
            }
        }

        private void SetItemInfo(ItemInfo info, string[] infoStr, string[] menu)
        {
            string[] menuName = info.GetLogHeadLine().Split(',');

            foreach (string name in menuName)
            {
                int index = GetMenuIndex(name, menu);

                if (index != -1)
                {
                    if (name == "一级菜单")
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
        }

        private void SetBaseItemInfo(BaseItemInfo info, string[] infoStr, string[] menu)
        {
            try
            {
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
                        else if (name == "生产厂家" || name == "生产企业")
                        {
                            info.Created = infoStr[index];
                        }
                        else if (name == "包装规格")
                        {
                            info.Format = infoStr[index];
                        }
                        else if (name == "商城售价(最低价格)")
                        {
                            string p = infoStr[index];
                            try
                            {
                                info.ShopPrice = string.IsNullOrEmpty(p) ? 0 : Convert.ToDecimal(p);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex + p);
                            }
                        }
                        else if (name == "平台售价（最低价格）")
                        {
                            string p = infoStr[index];
                            info.PlatformPrice = string.IsNullOrEmpty(p) ? 0 : Convert.ToDecimal(p);
                        }
                        else if (name == "商城折后售价")
                        {
                            string p = infoStr[index];
                            info.ShopSelaPrice = string.IsNullOrEmpty(p) ? 0 : Convert.ToDecimal(p);

                        }
                        else if (name == "折扣")
                        {
                            string p = infoStr[index];
                            info.Sela = string.IsNullOrEmpty(p) ? 0 : Convert.ToDecimal(p);
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
                                info.Weight = string.IsNullOrEmpty(p) ? 0 : Convert.ToInt32(p);
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

                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
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

        public void WriteItemUrl(string url, string fileName)
        {
            CommonFun.WriteUrlCSV(fileName, url);
        }

        public void WriteErrorUrl(string url, string fileName)
        {
            CommonFun.WriteUrlCSV(fileName, url);
        }

        public void WriteItem(BaseItemInfo item, string fileName)
        {
            CommonFun.WriteCSV(fileName, item);
        }

        public void AddItemNew(BaseItemInfo item, string fileName, bool isShop = true)
        {
            string format = CommonFun.FormatStr(item.Format, item.Name);
            string key = item.Created + format + item.Name;

            if (!ShopAllItems.ContainsKey(key))
            {
                ShopAllItems.Add(key, item);

            }
            else
            {
                if (isShop)
                {
                    if (shopAllItems[key].PlatformPrice > item.PlatformPrice)
                    {
                        ShopAllItems[key] = item;
                    }
                }
                else
                {
                    if (shopAllItems[key].ShopPrice > item.ShopPrice)
                    {
                        ShopAllItems[key] = item;
                    }
                }
            }
            WriteItem(item, fileName);
        }

        public void AddItme(BaseItemInfo item, string fileName, bool isShop = true)
        {
            string key = item.Created + item.Format + item.Name;

            if (!ShopAllItems.ContainsKey(key))
            {
                ShopAllItems.Add(key, item);
                WriteItem(item, fileName);
            }
            else
            {
                if (isShop)
                {
                    if (shopAllItems[key].PlatformPrice > item.PlatformPrice)
                    {
                        ShopAllItems[key] = item;
                        WriteItem(item, fileName);
                    }
                }
                else
                {
                    if (shopAllItems[key].ShopPrice > item.ShopPrice)
                    {
                        ShopAllItems[key] = item;
                        WriteItem(item, fileName);
                    }
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
        public void Compare(string pFileName, string sFileName, string createDir, bool isload = false)
        {
            ReadPlatFormWebPageValue flatform = new ReadPlatFormWebPageValue();
            BaseReadWebPage platform = new BaseReadWebPage();
            BaseReadWebPage shop = new BaseReadWebPage();
            if (!string.IsNullOrEmpty(pFileName))
            {
                platform.ReadBaseItemInfo(pFileName, true);
            }


            shop.ReadBaseItemInfo(sFileName, true);

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

                if (!isEixst && isload)
                {
                    List<BaseItemInfo> seachItems = flatform.SeachInfoByID(item.ID);

                    Dictionary<string, BaseItemInfo> saveInfos = new Dictionary<string, BaseItemInfo>();

                    foreach (BaseItemInfo info in seachItems)
                    {
                        string key = info.Name + info.Format + info.Created;

                        if (!platform.ShopAllItems.ContainsKey(key))
                        {
                            if (item.ID == info.ID || (itemName == info.Name && item.Created == info.Created))
                            {
                                if (CommonFun.IsSameFormat(info.Format, item.Format, info.Name, item.Name))
                                {
                                    isEixst = true;
                                    ComparePrice(info, item);
                                }
                            }

                            platform.ShopAllItems.Add(key, info);
                            saveInfos.Add(key, info);
                        }
                        else if (platform.ShopAllItems[key].ShopSelaPrice > info.ShopSelaPrice)
                        {
                            if (item.ID == info.ID || (itemName == info.Name && item.Created == info.Created))
                            {
                                if (CommonFun.IsSameFormat(info.Format, item.Format, info.Name, item.Name))
                                {
                                    isEixst = true;
                                    ComparePrice(info, item);
                                }
                            }

                            platform.ShopAllItems[key] = info;
                            saveInfos[key] = info;
                        }
                    }

                    foreach (BaseItemInfo saveInfo in saveInfos.Values)
                    {
                        string name = string.IsNullOrEmpty(pFileName) ? "Platform/Platform" + ticks + ".csv" : pFileName;
                        CommonFun.WriteCSV(name, saveInfo);
                    }
                }

                if (!isEixst)
                {
                    CommonFun.WriteCSV(createDir + "/NotIsEixst" + ticks + ".csv", item);
                }
            }
        }

        public void RequestUrls(RequestInfo info)
        {
            try
            {
                string type = info.GetParam("requestType");
                DateTime start = DateTime.Now;
                if (type == "post")
                {
                    if (info.PostDatas.Count == 0)
                    {
                        info.IsFinshed = true;
                    }

                    for (int i = 0; i < info.PostDatas.Count; i++)
                    {
                        ThreadPool.QueueUserWorkItem(RequestPostUrl, info);

                        if (i > 0 && i % 4 == 0)
                        {
                            Thread.Sleep(1000);
                        }
                    }
                }
                else
                {
                    if (info.Urls.Count == 0)
                    {
                        info.IsFinshed = true;
                    }

                    for (int i = 0; i < info.Urls.Count; i++)
                    {
                        ThreadPool.QueueUserWorkItem(RequestGetUrl, info);

                        if (i > 0 && i % 4 == 0)
                        {
                            Thread.Sleep(1000);
                        }
                    }
                }

                start = DateTime.Now;
                while (!info.IsFinshed)
                {
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public void RequestGetUrl(object state)
        {
            try
            {
                RequestInfo info = (RequestInfo)state;
                int index = info.GetIndex();

                Console.WriteLine("request get index:{0} total:{2}, url:{1}", index, info.Urls[index], info.Urls.Count);

                string postData = info.GetParam("postData");

                string encodingStr = info.GetParam("encoding");

                Encoding encoding = null;
                if (!string.IsNullOrEmpty(encodingStr))
                {
                    encoding = Encoding.GetEncoding(encodingStr);
                }

                string content = request.HttpGet(info.Urls[index], postData, encoding, info.IsUseUserAgent);

                lock (info.Contentes)
                {
                    info.Contentes.Add(content);

                    if (info.Contentes.Count == info.Urls.Count)
                    {
                        info.IsFinshed = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public void RequestPostUrl(object state)
        {
            try
            {
                RequestInfo info = (RequestInfo)state;
                int index = info.GetIndex();

                Console.WriteLine("request post index:{0}, url:{1}", index, info.PostDatas[index]);

                //string postDataStr = info.GetParam("postData");

                string encodingStr = info.GetParam("encoding");

                string referfer = info.GetParam("referfer");

                string accept = info.GetParam("accept");

                Encoding encoding = null;

                if (!string.IsNullOrEmpty(encodingStr))
                {
                    encoding = Encoding.GetEncoding(encodingStr);
                }

                string content = request.HttpPost(info.Urls[0], info.PostDatas[index], referfer, accept, encoding);

                lock (info.Contentes)
                {
                    info.Contentes.Add(content);

                    if (info.Contentes.Count == info.PostDatas.Count)
                    {
                        info.IsFinshed = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public virtual bool ComparePrice(BaseItemInfo platformItem, BaseItemInfo info)
        {
            return false;
        }

        public virtual void Login()
        {

        }

        public virtual void ReadAllMenuURL()
        {

        }

        public virtual void ReadAllItemURL()
        {

        }

        public virtual void ReadAllItem()
        {

        }

        public virtual void Start()
        {
            ReadAllMenuURL();

            ReadAllItemURL();

            ReadAllItem();
        }
    }
}
