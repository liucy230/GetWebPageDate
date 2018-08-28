using GetWebPageDate.Util.ReadWebPage;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GetWebPageDate.Util.SyncStock
{
    public class SyncItemStock
    {
        private ReadTKWebPageValue tk = new ReadTKWebPageValue();

        private YiYaoWebRead yy = new YiYaoWebRead();

        private T315ReadWebPage tof = new T315ReadWebPage();

        private EightHundredF ehf = new EightHundredF();

        private Dictionary<string, BaseItemInfo> unUpdate = new Dictionary<string, BaseItemInfo>();

        private Dictionary<string, Dictionary<string, int>> lastStocks = new Dictionary<string, Dictionary<string, int>>();
        public void Start()
        {
            try
            {
                //1、读取固定
                GetFixedItem();

                tk.Login();
                yy.Login();
                tof.Login();

                do
                {
                    double ticks = DateTime.Now.Ticks;
                    int curCount = 0;
                    int totalCount = unUpdate.Count;
                    try
                    {
                        foreach (KeyValuePair<string, BaseItemInfo> info in unUpdate)
                        {
                            try
                            {
                                BaseItemInfo item = info.Value;

                                // 首次同步其他三个平台
                                bool isFirstUpdate = string.IsNullOrEmpty(item.SellType);

                                //2、取平台库存
                                Dictionary<string, BaseItemInfo> tkItems = tk.GetSellingItem(info.Value.ID);
                                Dictionary<string, BaseItemInfo> tofItems = tof.GetSellingItem(info.Value.ID, false, false);


                                if (isFirstUpdate)
                                {
                                    foreach (BaseItemInfo tkItem in tkItems.Values)
                                    {
                                        if (CommonFun.IsSameFormat(item.Format, tkItem.Format, item.Name, tkItem.Name))
                                        {
                                            if (tkItem.Inventory != item.Inventory)
                                            {
                                                tkItem.Inventory = item.Inventory;
                                                tkItem.ShopPrice = tkItem.PlatformPrice;
                                                tkItem.Type = "201";
                                                tk.UpdatePrice(tkItem, tkItem.Type);
                                            }


                                            break;
                                        }
                                    }

                                    int count = 0;
                                    bool isFind = false;
                                    do
                                    {
                                        foreach (BaseItemInfo tofItem in tofItems.Values)
                                        {
                                            bool isSame = false;
                                            if (count > 0)
                                            {
                                                isSame = CommonFun.IsSameFormat(tofItem.Format, item.Format, tofItem.Name, item.Name);
                                            }
                                            else
                                            {
                                                isSame = tofItem.Type.Trim() == item.Type.Trim();
                                            }
                                            if (isSame)
                                            {
                                                if (tofItem.Inventory != item.Inventory)
                                                {
                                                    tofItem.Inventory = item.Inventory;
                                                    tof.UpdateItemInfo(tofItem.ItemName, tofItem.PlatformPrice, "0", item.Type, tofItem.Inventory, tofItem);
                                                }
                                                isFind = true;
                                                break;
                                            }
                                        }
                                    } while (++count < 2 && !isFind);

                                    yy.UpdateStock(item.Type, item.Inventory);

                                    ehf.SyncStock(item);

                                    CommonFun.WriteCSV("Stock/Init" + ticks + ".csv", item);
                                }
                                else
                                {
                                    //item.SellType = "2018-06-16 12:00:00";
                                    //以固定表为上次同步值
                                    int baseCount = Convert.ToInt32(item.Inventory);
                                    int nextCount = baseCount;
                                    int tkCount = GetItemCount(item, tkItems, true);
                                    int tofCount = GetItemCount(item, tofItems, false);
                                    int ehfCount = ehf.GetStock(item.Type);
                                    int yyCount = int.MaxValue;
                                    Dictionary<string, BaseItemInfo> yyItems = yy.GetSellingItem(item.Type, DateTime.ParseExact(item.SellType, "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.CurrentCulture));
                                    foreach (BaseItemInfo yyItem in yyItems.Values)
                                    {
                                        yyCount = Convert.ToInt32(yyItem.Inventory);
                                    }

                                    tkCount = tkCount == int.MaxValue ? tkCount : baseCount - tkCount;
                                    yyCount = yyCount == int.MaxValue ? yyCount : baseCount - yyCount;
                                    tofCount = tofCount == int.MaxValue ? tofCount : baseCount - tofCount;
                                    ehfCount = ehfCount == int.MaxValue ? ehfCount : baseCount - ehfCount;

                                    if (tkCount < 0 || yyCount < 0 || tofCount < 0 || ehfCount < 0)
                                    {
                                        if (tkCount < 0)
                                        {
                                            nextCount = baseCount - tkCount;
                                        }
                                        else if (yyCount < 0)
                                        {
                                            nextCount = baseCount - yyCount;
                                        }
                                        else if (tofCount < 0)
                                        {
                                            nextCount = baseCount - tofCount;
                                        }
                                        else if (ehfCount < 0)
                                        {
                                            nextCount = baseCount - nextCount;
                                        }
                                    }
                                    else
                                    {
                                        nextCount = tkCount == int.MaxValue ? nextCount : nextCount - tkCount;
                                        nextCount = yyCount == int.MaxValue ? nextCount : nextCount - yyCount;
                                        nextCount = tofCount == int.MaxValue ? nextCount : nextCount - tofCount;
                                        nextCount = ehfCount == int.MaxValue ? nextCount : nextCount - ehfCount;
                                    }

                                    Console.WriteLine("TotalCount:{0} CurCount:{1} baseCount:{2} nextCount:{6} tkCount:{3} tofCount:{4} yyCount:{5}", totalCount, ++curCount, baseCount, tkCount, tofCount, yyCount, nextCount);

                                    if (nextCount != int.MaxValue && nextCount >= 0)
                                    {
                                        if (nextCount != baseCount)
                                        {
                                            item.Inventory = nextCount.ToString();
                                            foreach (BaseItemInfo tkItem in tkItems.Values)
                                            {
                                                if (CommonFun.IsSameFormat(item.Format, tkItem.Format, item.Name, tkItem.Name))
                                                {
                                                    if (tkItem.Inventory != item.Inventory)
                                                    {
                                                        tkItem.Inventory = item.Inventory;
                                                        tkItem.ShopPrice = tkItem.PlatformPrice;
                                                        tkItem.Type = "201";
                                                        tk.UpdatePrice(tkItem, tkItem.Type);
                                                    }

                                                    break;
                                                }
                                            }
                                            int count = 0;
                                            bool isFind = false;
                                            do
                                            {
                                                foreach (BaseItemInfo tofItem in tofItems.Values)
                                                {
                                                    bool isSame = false;
                                                    if (count > 0)
                                                    {
                                                        isSame = CommonFun.IsSameFormat(tofItem.Format, item.Format, tofItem.Name, item.Name);
                                                    }
                                                    else
                                                    {
                                                        isSame = tofItem.Type.Trim() == item.Type.Trim();
                                                    }
                                                    if (isSame)
                                                    {
                                                        if (tofItem.Inventory != item.Inventory)
                                                        {
                                                            tofItem.Inventory = item.Inventory;
                                                            tof.UpdateItemInfo(tofItem.ItemName, tofItem.PlatformPrice, tofItem.SellType, tofItem.Type, tofItem.Inventory, tofItem);
                                                        }
                                                        isFind = true;
                                                        break;
                                                    }
                                                }
                                            } while (++count < 2 && !isFind);

                                            yy.UpdateStock(item.Type, item.Inventory);

                                            ehf.SyncStock(item);
                                        }
                                    }
                                    else
                                    {
                                        Console.WriteLine("The stock is null id:{0}, name:{1}, format{2}, NO:{3}", item.ID, item.Name, item.Format, item.Type);
                                        CommonFun.WriteCSV("Stock/StockNull" + ticks + ".csv", item);
                                        continue;
                                    }
                                }

                                item.SellType = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                                CommonFun.UpdateXLS("KTUnUpdate.xlsx", new string[] { "出售方式（零或整）", "库存" }, new string[] { item.SellType, item.Inventory }, "剂型", item.Type);
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
                } while (true);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private int GetItemCount(BaseItemInfo item, Dictionary<string, BaseItemInfo> items, bool isTK)
        {
            try
            {
                int count = 0;
                do
                {
                    foreach (BaseItemInfo sItem in items.Values)
                    {
                        if (isTK)
                        {
                            if (CommonFun.IsSameFormat(item.Format, sItem.Format, item.Name, sItem.Name))
                            {
                                return Convert.ToInt32(sItem.Inventory);
                            }
                        }
                        else
                        {
                            bool isSame = false;
                            if (count > 0)
                            {
                                isSame = CommonFun.IsSameFormat(item.Format, sItem.Format, item.Name, sItem.Name);
                            }
                            else
                            {
                                isSame = item.Type.Trim() == sItem.Type.Trim();
                            }
                            if (isSame)
                            {
                                return Convert.ToInt32(sItem.Inventory);
                            }
                        }
                    }
                } while (++count < 2 && !isTK);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }


            return int.MaxValue;
        }

        /// <summary>
        /// 获取固定表
        /// </summary>

        private void GetFixedItem()
        {
            try
            {
                DataTable data = CommonFun.ReadXLS("KTUnUpdate.xlsx");

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
                        item.Type = (string)data.Rows[row]["剂型"].ToString();
                        item.Inventory = (string)data.Rows[row]["库存"].ToString();
                        item.SellType = (string)data.Rows[row]["出售方式（零或整）"].ToString();

                        //item.Name = (string)data.Rows[row]["通用名称"].ToString();

                        string key = item.Name + item.Format + item.Created;
                        if (!unUpdate.ContainsKey(key))
                        {
                            unUpdate.Add(key, item);
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
        /// 获取平台库存
        /// </summary>
        /// <param name="item"></param>
        /// <param name="platform"></param>
        /// <returns></returns>
        private Dictionary<string, int> GetPlatformStock(BaseItemInfo item)
        {
            Dictionary<string, int> stocks = new Dictionary<string, int>();
            try
            {
                //1、获取tk的库存
                tk.GetSellingItem(item.ID);
                //2、获取1号网的库存
                string yyUrl = "";
                //3、获取315的库存
                string tofUrl = "https://www.315jiage.cn/memberViewPrices.aspx?act=ajax&cmd=get&p=1&w=certification&k=%E5%9B%BD%E8%8D%AF%E5%87%86%E5%AD%97Z15021169&minStock=&maxStock=&hot=0&rnd=334376";
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            return stocks;
        }



        /// <summary>
        /// 更新平台库存
        /// </summary>
        /// <param name="item"></param>
        /// <param name="stock"></param>
        /// <param name="platform"></param>
        /// <returns></returns>
        private bool UpdatePlatformStock(BaseItemInfo item, int stock, string platform)
        {
            try
            {

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            return true;
        }
    }
}
