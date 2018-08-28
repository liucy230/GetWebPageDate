using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace GetWebPageDate.Util.ReadWebPage
{
    /// <summary>
    /// 八百方
    /// </summary>
    public class EightHundredF : BaseReadWebPage
    {
        private string host = "http://sm.800pharm.com/shop";

        private string shopCode = "100879";

        private string signKey;

        private string adminUrl = "http://www.800pharm.com/shop/erp/syncApi.html";

        private Dictionary<string, BaseItemInfo> allItems = new Dictionary<string, BaseItemInfo>();

        private string fileName = "ehf";

        private string selfStoreName = "百寿康大药房";

        private Dictionary<string, string> heads = new Dictionary<string, string>();

        private string token;

        private string loginUrl = "http://sm.800pharm.com/shop/login/";

        private string cookie;

        public override void Login()
        {
            request.Cookie = cookie;

            GetToken();
        }

        private void GetToken()
        {
            string url = "http://pms.800pharm.com/merchant/manage?from_supervisor_url=0";

            string content = request.HttpGet(url);

            token = CommonFun.GetValue(content, "<meta name=\"csrf-token\" value=\"", "\"");
        }

        public EightHundredF()
        {
            heads.Add("User-Agent", "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/67.0.3396.87 Safari/537.36");
            heads.Add("X-Requested-With", "XMLHttpRequest");
            signKey = ConfigurationManager.AppSettings["signKey"];
        }

        public void Test()
        {

            Dictionary<string, string> info = new Dictionary<string, string>();
            info.Add("storage", "30");
            info.Add("wareId", "1478999");

            //List<Dictionary<string, string>> list = new List<Dictionary<string, string>>();
            //list.Add(info);
            //UpdateStock(list);

            //http://pms.800pharm.com/merchant/release/search-standard-sku
            //GetALLItem();
            Login();
            Dictionary<string, BaseItemInfo> items = GetSellingItems();

            foreach (BaseItemInfo item in items.Values)
            {
                UpNewItem(item);
                //UpdateItemInfo(item.ViewCount, null, item.ShopPrice.ToString(), null);

                //DownItem(item);
            }
            GetDownItems();
            GetAuditItems();
            //SeachNewItemInfo("国药准字Z20055346");
            //Dictionary<string, string> heads = new Dictionary<string, string>();
            //heads.Add("User-Agent", "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/67.0.3396.87 Safari/537.36");
            ////SeachNewItemInfo("国药准字H31021537");
            //string postData = "page=1&records=10&_token=sYCMzEqpHi1ykGMTiyqIn4NVBxw2JEidkv4lUylN";
            //string url = "http://pms.800pharm.com/merchant/manage";
            //string content = request.HttpPost(url, postData, "http://pms.800pharm.com/merchant/manage?from_supervisor_url=0", "", null, heads);
        }

        public void CheckLogin()
        {
            Login();

            while (string.IsNullOrEmpty(token))
            {
                Console.WriteLine("{0} {1}", DateTime.Now, "please relogin..................................");

                cookie = Console.ReadLine();

                Login();
            }
        }

        public override void Start()
        {
            Stream stream = Console.OpenStandardInput();
            Console.SetIn(new StreamReader(stream, Encoding.Default, false, 5000));

            //tk在售列表
            ReadTKWebPageValue tk = new ReadTKWebPageValue();
            tk.Login();
            Dictionary<string, BaseItemInfo> tkSItems = tk.GetSellingItem();

            CheckLogin();

            //在售列表
            Dictionary<string, BaseItemInfo> sItems = GetSellingItems();

            //下架列表
            Dictionary<string, BaseItemInfo> dItems = GetDownItems();

            //仓库列表
            Dictionary<string, BaseItemInfo> aItems = GetAuditItems();

            int curCount = 0;

            foreach (BaseItemInfo ehfSItem in sItems.Values)
            {
                CheckLogin();

                Console.WriteLine("{0} Running down totalCount:{1} curCount:{2}", DateTime.Now, sItems.Count, ++curCount);

                bool isInTKSellingList = false;

                foreach (BaseItemInfo tkSItem in tkSItems.Values)
                {
                    if (ehfSItem.ID == tkSItem.ID && CommonFun.IsSameFormat(ehfSItem.Format, tkSItem.Format, ehfSItem.Name, tkSItem.Name))
                    {
                        isInTKSellingList = true;
                        break;
                    }
                }

                if (!isInTKSellingList)
                {
                    DownItem(ehfSItem);
                }
            }

            curCount = 0;

            foreach (BaseItemInfo tkSItem in tkSItems.Values)
            {
                bool isInSellList = false;

                CheckLogin();

                Console.WriteLine("{0} Running up totalCount:{1} curCount:{2}", DateTime.Now, tkSItems.Count, ++curCount);

                foreach (BaseItemInfo sItem in sItems.Values)
                {
                    if (tkSItem.ID == sItem.ID && CommonFun.IsSameFormat(tkSItem.Format, sItem.Format, tkSItem.Name, sItem.Name))
                    {
                        isInSellList = true;
                        break;
                    }
                }

                if (!isInSellList)
                {
                    string downListKey = null;
                    foreach (KeyValuePair<string, BaseItemInfo> item in dItems)
                    {
                        BaseItemInfo dItem = item.Value;
                        if (tkSItem.ID == dItem.ID && CommonFun.IsSameFormat(tkSItem.Format, dItem.Format, tkSItem.Name, dItem.Name))
                        {
                            downListKey = item.Key;
                            break;
                        }
                    }

                    if (!string.IsNullOrEmpty(downListKey))
                    {
                        //重新上架
                        if (ReUpItem(dItems[downListKey]))
                        {
                            sItems.Add(downListKey, dItems[downListKey]);
                            dItems.Remove(downListKey);
                        }

                    }
                    else
                    {
                        //新品
                        string aListKey = null;
                        foreach (KeyValuePair<string, BaseItemInfo> item in aItems)
                        {
                            BaseItemInfo aItem = item.Value;
                            if (tkSItem.ID == aItem.ID && CommonFun.IsSameFormat(tkSItem.Format, aItem.Format, tkSItem.Name, aItem.Name))
                            {
                                aListKey = item.Key;
                                break;
                            }
                        }

                        if (string.IsNullOrEmpty(aListKey))
                        {
                            tkSItem.ShopPrice = tkSItem.PlatformPrice;
                            UpNewItem(tkSItem);
                        }
                    }
                }
            }

        }

        public void GetAllItem()
        {
            try
            {
                string param = string.Format("shopcode={0}&signKey={1}&type=8&verify=1", shopCode, signKey);

                string sign = CommonFun.MD5Str(param).ToLower();

                param = string.Format("shopcode={0}&sign={1}&type=8&verify=1", shopCode, sign);

                string content = request.HttpGet(adminUrl, param);

                allItems = GetItems(content);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public BaseItemInfo GetItemByProductCode(string productCode)
        {
            try
            {
                string[] ids = productCode.Split(',');

                foreach (string id in ids)
                {
                    if (allItems.ContainsKey(id))
                    {
                        return allItems[id];
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return null;
        }


        private Dictionary<string, BaseItemInfo> GetItems(string content)
        {
            Dictionary<string, BaseItemInfo> items = new Dictionary<string, BaseItemInfo>();
            try
            {
                JObject job = (JObject)JsonConvert.DeserializeObject(content);

                string productInfoStr = job["productInfoList"].ToString();

                JArray pJob = (JArray)JsonConvert.DeserializeObject(productInfoStr);

                for (int i = 0; i < pJob.Count; i++)
                {
                    BaseItemInfo item = new BaseItemInfo();
                    item.ID = pJob[i]["pzwh"].ToString();
                    item.Name = pJob[i]["wareName"].ToString();
                    item.Format = pJob[i]["spec"].ToString();
                    item.Created = pJob[i]["company"].ToString();
                    item.ItemName = pJob[i]["wareId"].ToString();
                    item.ViewCount = pJob[i]["productcode"].ToString();
                    item.Inventory = pJob[i]["storage"].ToString();

                    if (!string.IsNullOrEmpty(item.ViewCount))
                    {
                        items.Add(item.ViewCount, item);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return items;
        }

        private List<BaseItemInfo> GetItemList(string content)
        {
            List<BaseItemInfo> items = new List<BaseItemInfo>();
            try
            {
                if (!string.IsNullOrEmpty(content))
                {
                    Object obj = JsonConvert.DeserializeObject(content);

                    if (obj != null)
                    {
                        JObject job = (JObject)obj;
                        string dataStr = job["data"] != null ? job["data"].ToString() : "";

                        if (!string.IsNullOrEmpty(dataStr))
                        {
                            Object dObj = JsonConvert.DeserializeObject(dataStr);

                            if (dObj != null)
                            {
                                JObject dJob = (JObject)dObj;

                                string rowsStr = dJob["rows"] != null ? dJob["rows"].ToString() : "";
                                if (!string.IsNullOrEmpty(rowsStr))
                                {
                                    JArray rJob = (JArray)JsonConvert.DeserializeObject(rowsStr);

                                    foreach (JObject r in rJob)
                                    {
                                        BaseItemInfo item = new BaseItemInfo();

                                        item.ID = r["approval_number"] != null ? r["approval_number"].ToString() : "";
                                        item.ItemName = r["self_define_name"] != null ? r["self_define_name"].ToString() : "";
                                        item.Name = r["name"] != null ? r["name"].ToString() : "";
                                        item.Format = r["combined_spec"] != null ? r["combined_spec"].ToString() : "";
                                        item.Created = r["full_name"] != null ? r["full_name"].ToString() : "";
                                        item.ViewCount = r["id"] != null ? r["id"].ToString() : "";
                                        item.Type = r["product_number"] != null ? r["product_number"].ToString() : "";
                                        item.ShopPrice = r["shop_price"] != null ? Convert.ToDecimal(r["shop_price"].ToString()) : -1;

                                        items.Add(item);
                                    }
                                }

                            }

                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return items;
        }

        private BaseItemInfo GetItem(string content)
        {
            try
            {
                if (!string.IsNullOrEmpty(content))
                {
                    Object obj = JsonConvert.DeserializeObject(content);

                    if (obj != null)
                    {
                        JObject job = (JObject)obj;

                        string productInfoStr = job["productInfoMap"] != null ? job["productInfoMap"].ToString() : "";
                        if (!string.IsNullOrEmpty(productInfoStr))
                        {
                            JObject pJob = (JObject)JsonConvert.DeserializeObject(productInfoStr);

                            BaseItemInfo item = new BaseItemInfo();
                            //item.ID = pJob["pzwh"].ToString();
                            item.Name = pJob["wareName"].ToString();
                            item.Format = pJob["spec"].ToString();
                            //item.Created = pJob["company"].ToString();
                            item.ItemName = pJob["wareId"].ToString();
                            item.ViewCount = pJob["productcode"].ToString();
                            item.Inventory = pJob["storage"].ToString();

                            return item;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Content:{0}, Error:{1}", content, ex);
            }

            return null;
        }
        public void SyncStock(BaseItemInfo item)
        {
            try
            {
                string[] ids = item.Type.Split(',');

                foreach (string id in ids)
                {
                    BaseItemInfo newItme = SeachItem(id);

                    if (newItme != null && newItme.Inventory != item.Inventory)
                    {
                        Dictionary<string, string> info = new Dictionary<string, string>();
                        info.Add("storage", item.Inventory);
                        info.Add("wareId", newItme.ItemName);
                        List<Dictionary<string, string>> listInfo = new List<Dictionary<string, string>>();
                        listInfo.Add(info);
                        if (UpdateStock(listInfo))
                        {
                            newItme.Inventory = item.Inventory;
                            CommonFun.WriteCSV(fileName + "/Update" + ticks + ".csv", newItme);
                        }
                        else
                        {
                            CommonFun.WriteCSV(fileName + "/UpdateFail" + ticks + ".csv", item);
                        }
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public int GetStock(string prodcutIds)
        {
            try
            {
                string[] ids = prodcutIds.Split(',');

                foreach (string id in ids)
                {
                    BaseItemInfo item = SeachItem(id);

                    if (item != null)
                    {
                        return Convert.ToInt32(item.Inventory);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return int.MaxValue;
        }

        public BaseItemInfo SeachItem(string productCode)
        {
            try
            {
                string param = string.Format("productcode={0}&shopcode={1}&signKey={2}&type=2", productCode, shopCode, signKey);

                string sign = CommonFun.MD5Str(param).ToLower();

                param = string.Format("productcode={0}&shopcode={1}&sign={2}&type=2", productCode, shopCode, sign);

                string content = request.HttpGet(adminUrl, param);

                return GetItem(content);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            return null;
        }
        public bool UpdateStock(List<Dictionary<string, string>> infoList)
        {
            try
            {
                string param = string.Format("shopcode={0}&signKey={1}&type=9", shopCode, signKey);

                string sign = CommonFun.MD5Str(param).ToLower();

                string shopInfoList = JsonConvert.SerializeObject(infoList);

                param = string.Format("shopcode={0}&sign={1}&type=9&shopInfoList={2}", shopCode, sign, shopInfoList);

                string content = request.HttpGet(adminUrl, param);

                JObject job = (JObject)JsonConvert.DeserializeObject(content);

                if (job["code"].ToString() == "1")
                {
                    return true;
                }
                else
                {
                    Console.WriteLine("UpdateStock failed {0}", job.ToString());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return false;
        }

        /// <summary>
        /// 修改商品信息
        /// </summary>
        /// <param name="type"></param>
        /// <param name="price"></param>
        /// <param name="stock"></param>
        /// <returns></returns>
        public bool UpdateItemInfo(string id, string type, string price, string stock)
        {
            try
            {
                string url = "http://pms.800pharm.com/merchant/manage/quick-update";

                string content = request.HttpGet(url + "-page/" + id);

                content = CommonFun.GetValue(content, "<div class=\"page-content\">", "确认提交");

                MatchCollection ms = CommonFun.GetValues(content, "<div class=\"clearfix\">", "</div>");

                string product_number = CommonFun.GetValue(ms[0].Value, "value=\"", "\"");

                MatchCollection mMs = CommonFun.GetValues(ms[1].Value, "<option", "</option>");
                string shelf_one_id = CommonFun.GetValue(mMs[1].Value, "value=\"", "\"");
                string shelf_two_id = CommonFun.GetValue(mMs[2].Value, "value=\"", "\"");
                string shelf_id = CommonFun.GetValue(mMs[3].Value, "value=\"", "\"");

                string shop_price = CommonFun.GetValue(ms[2].Value, "value=\"", "\"");
                string mobile_price = CommonFun.GetValue(ms[3].Value, "value=\"", "\"");
                string market_price = CommonFun.GetValue(ms[4].Value, "value=\"", "\"");
                string storage = CommonFun.GetValue(ms[5].Value, "value=\"", "\"");
                string weight = CommonFun.GetValue(ms[6].Value, "value=\"", "\"");

                product_number = string.IsNullOrEmpty(type) ? product_number : type;
                shop_price = string.IsNullOrEmpty(price) ? shop_price : price;
                market_price = string.IsNullOrEmpty(price) ? market_price : price;
                storage = string.IsNullOrEmpty(stock) ? storage : price;

                string postData = string.Format("_token={0}&id%5B%5D={1}&product_number={2}&shelf_one_id={3}&shelf_two_id={4}&shelf_id={5}&shop_price={6}&mobile_price={7}&market_price={8}&storage={9}&weight={10}", token, id, product_number, shelf_one_id, shelf_two_id, shelf_id, shop_price, mobile_price, market_price, storage, weight);

                content = request.HttpPost(url, postData, heads);

                if (!string.IsNullOrEmpty(content))
                {
                    Object obj = JsonConvert.DeserializeObject(content);

                    if (obj != null)
                    {
                        JObject job = (JObject)obj;
                        if (job["code"].ToString() == "0")
                        {
                            return true;
                        }
                        else
                        {
                            Console.WriteLine("UpdateItemInfo:{0}", obj.ToString());
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

        /// <summary>
        /// 下架物品
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool DownItem(BaseItemInfo item)
        {
            try
            {
                string url = "http://pms.800pharm.com/merchant/manage/down-shelf";

                string postdata = string.Format("_token={0}&id%5B%5D={1}", token, item.ViewCount);

                string content = request.HttpPost(url, postdata, heads);

                if (!string.IsNullOrEmpty(content))
                {
                    Object obj = JsonConvert.DeserializeObject(content);

                    if (obj != null)
                    {
                        JObject job = (JObject)obj;
                        if (job["code"].ToString() == "0")
                        {

                            return true;
                        }
                        else
                        {
                            Console.WriteLine("DownItem:{0}", job.ToString());
                        }
                    }
                }

                CommonFun.WriteCSV(fileName + "/down" + ticks + fileExtendName, item);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return false;
        }

        /// <summary>
        /// 重新上架物品
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool ReUpItem(BaseItemInfo item)
        {
            try
            {
                CommonFun.WriteCSV(fileName + "/reUp" + ticks + fileExtendName, item);

                return UpdateItemInfo(item.ViewCount, null, null, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return false;
        }

        /// <summary>
        /// 上架新物品
        /// </summary>
        /// <returns></returns>
        public bool UpNewItem(BaseItemInfo item)
        {
            try
            {
                //item.Inventory = random.Next(30, 40).ToString();
                //item.Weight = 20;

                CommonFun.WriteCSV(fileName + "/new" + ticks + fileExtendName, item);
                //1、通过id查询
                string content = SeachNewItemInfo(item.ID);

                JObject sJob = JsonConvert.DeserializeObject<JObject>(content);
                if (sJob == null)
                {
                    return false;
                }

                JObject dJob = JsonConvert.DeserializeObject<JObject>(sJob["data"].ToString());

                if (dJob == null)
                {
                    return false;
                }

                JArray rJob = JsonConvert.DeserializeObject<JArray>(dJob["rows"].ToString());

                string id = null;
                int is_drug = 1;
                foreach (JObject row in rJob)
                {
                    if (CommonFun.IsSameFormat(item.Format, row["specification_name"].ToString(), item.Name, item.Name))
                    {
                        id = row["id"].ToString();
                        is_drug = row["product_attribute_name"].ToString() == "非药" ? 0 : 1;
                        break;
                    }
                }

                if (string.IsNullOrEmpty(id))
                {
                    return false;
                }

                ////2、查询商品详情
                content = SeachItemDetail(id);

                ////3、提交详细信息
                if (Submit(content, id))
                {
                    //4、获取商品信息
                    string url = string.Format("http://pms.800pharm.com/merchant/release/{0}/release-drug-standard-sku", id);

                    content = request.HttpGet(url);

                    if (!string.IsNullOrEmpty(content))
                    {

                        string jItem = CommonFun.GetValue(content, "var storageALLData =", "defaultSkuEditors =");
                        jItem = jItem.Trim();
                        jItem = jItem.Substring(0, jItem.Length - 2);

                        JObject obj = JsonConvert.DeserializeObject<JObject>(jItem);

                        if (obj != null)
                        {
                            string standard_sku = obj["standard_sku"] == null ? "" : obj["standard_sku"].ToString();

                            if (!string.IsNullOrEmpty(standard_sku))
                            {
                                JObject jObj = JsonConvert.DeserializeObject<JObject>(standard_sku);

                                string standard_sku_id = jObj["standard_sku_id"] == null ? "" : jObj["standard_sku_id"].ToString();
                                string standard_product_id = jObj["standard_product_id"] == null ? "" : jObj["standard_product_id"].ToString();
                                string base_info_id = jObj["base_info_id"] == null ? "" : jObj["base_info_id"].ToString();
                                string specification_name = jObj["specification_name"] == null ? "" : jObj["specification_name"].ToString();


                                string spec_arr = obj["spec_arr"] == null ? "" : obj["spec_arr"].ToString();

                                if (!string.IsNullOrEmpty(spec_arr))
                                {
                                    JArray aObj = JsonConvert.DeserializeObject<JArray>(spec_arr);

                                    foreach (JObject sJObj in aObj)
                                    {
                                        string s_standard_sku_id = sJObj["standard_sku_id"] == null ? "" : sJObj["standard_sku_id"].ToString();
                                        if (s_standard_sku_id == id)
                                        {
                                            string spec_value_map = sJObj["spec_value_map"] == null ? "" : sJObj["spec_value_map"].ToString();
                                            string postData = string.Format("is_drug={5}&_token={0}&sku_item%5B0%5D%5Bstandard_sku_id%5D={1}&sku_item%5B0%5D%5Bstandard_product_id%5D={2}&sku_item%5B0%5D%5Bspecification_name%5D={3}&sku_item%5B0%5D%5Bspec_value_map%5D={4}&", token, standard_sku_id, standard_product_id, CommonFun.GetUrlEncode(specification_name, false), spec_value_map, is_drug);

                                            string spec_value = sJObj["spec_value"] == null ? "" : sJObj["spec_value"].ToString();
                                            string images = sJObj["images"] == null ? "" : sJObj["images"].ToString();
                                            string unit = sJObj["unit"] == null ? "" : sJObj["unit"].ToString();
                                            string description = sJObj["description"] == null ? "" : sJObj["description"].ToString();

                                            string spec_id;
                                            string spec_value_id;

                                            if (!string.IsNullOrEmpty(spec_value))
                                            {
                                                JArray sValueObj = JsonConvert.DeserializeObject<JArray>(spec_value);

                                                if (sValueObj != null)
                                                {

                                                    spec_id = sValueObj[0]["spec_id"] == null ? "" : sValueObj[0]["spec_id"].ToString();
                                                    spec_value_id = sValueObj[0]["spec_value_id"] == null ? "" : sValueObj[0]["spec_value_id"].ToString();

                                                    string spcPostData = string.Format("sku_item%5B0%5D%5Bspec_value%5D%5B0%5D%5Bspec_id%5D={0}&sku_item%5B0%5D%5Bspec_value%5D%5B0%5D%5Bspec_value_id%5D={1}&", spec_id, spec_value_id);

                                                    postData += spcPostData;
                                                }
                                            }

                                            string imagesPostData = "";
                                            if (!string.IsNullOrEmpty(images))
                                            {
                                                JArray iObjs = JsonConvert.DeserializeObject<JArray>(images);

                                                for (int i = 0; i < iObjs.Count; i++)
                                                {
                                                    string iId = iObjs[i]["id"] == null ? "" : iObjs[i]["id"].ToString();
                                                    string iUrl = iObjs[i]["url"] == null ? "" : CommonFun.GetUrlEncode(iObjs[i]["url"].ToString(), false);

                                                    imagesPostData += string.Format("sku_item%5B0%5D%5Bimages%5D%5B%5D={0}&sku_item%5B0%5D%5Bimages_boxs%5D%5B{1}%5D%5Bid%5D={2}&sku_item%5B0%5D%5Bimages_boxs%5D%5B{3}%5D%5Burl%5D={4}&", iId, i, iId, i, iUrl);
                                                }
                                            }

                                            postData += imagesPostData;


                                            string self_define_spec = "";
                                            string self_define_name = item.Name;   //商品名称
                                            string title = "";
                                            string shop_price = item.ShopPrice.ToString();
                                            string mobile_price = "";
                                            string market_price = item.ShopPrice.ToString();
                                            string storage = item.Inventory;
                                            string product_number = item.Type;
                                            string weight = item.Weight == 0 ? random.Next(10, 30).ToString() : item.Weight.ToString();
                                            string bar_code = sJObj["bar_code"] == null ? "" : sJObj["bar_code"].ToString();
                                            string shelf_id = "48418";
                                            // name  shopPrice marketPrice storge
                                            string selfPostData = string.Format("sku_item%5B0%5D%5Bdescription%5D={7}&sku_item%5B0%5D%5Bself_define_spec%5D=&sku_item%5B0%5D%5Bself_define_name%5D={0}&sku_item%5B0%5D%5Btitle%5D=&sku_item%5B0%5D%5Bshop_price%5D={1}&sku_item%5B0%5D%5Bmobile_price%5D=&sku_item%5B0%5D%5Bmarket_price%5D={2}&sku_item%5B0%5D%5Bstorage%5D={3}&sku_item%5B0%5D%5Bproduct_number%5D={8}&sku_item%5B0%5D%5Bweight%5D={4}&sku_item%5B0%5D%5Bbar_code%5D={9}&sku_item%5B0%5D%5Bunit%5D={5}&shelf_id={6}&is_cross_border=0", CommonFun.GetUrlEncode(self_define_name), shop_price, market_price, storage, weight, CommonFun.GetUrlEncode(unit), shelf_id, CommonFun.GetUrlEncode(description, false), product_number, bar_code);

                                            postData += selfPostData;
                                            //5、提交商品详细信息
                                            string sUrl = "http://pms.800pharm.com/merchant/release/release-standardized-product-sku";

                                            //postData = string.Format("is_drug=1&_token={0}&sku_item%5B0%5D%5Bstandard_sku_id%5D=234690&sku_item%5B0%5D%5Bstandard_product_id%5D=158494&sku_item%5B0%5D%5Bspecification_name%5D=6g*10%E8%A2%8B%2F%E7%9B%92&sku_item%5B0%5D%5Bspec_value_map%5D=65269&sku_item%5B0%5D%5Bspec_value%5D%5B0%5D%5Bspec_id%5D=1&sku_item%5B0%5D%5Bspec_value%5D%5B0%5D%5Bspec_value_id%5D=65269&sku_item%5B0%5D%5Bimages_boxs%5D%5B0%5D%5Bid%5D=822087&sku_item%5B0%5D%5Bimages_boxs%5D%5B0%5D%5Burl%5D=http%3A%2F%2Fimg.800pharm.com%2Fimages%2F20140422%2F20140422161619_108.jpg&sku_item%5B0%5D%5Bimages_boxs%5D%5B1%5D%5Bid%5D=822088&sku_item%5B0%5D%5Bimages_boxs%5D%5B1%5D%5Burl%5D=http%3A%2F%2Fimg.800pharm.com%2Fimages%2F20140422%2F20140422161623_687.jpg&sku_item%5B0%5D%5Bimages_boxs%5D%5B2%5D%5Bid%5D=822089&sku_item%5B0%5D%5Bimages_boxs%5D%5B2%5D%5Burl%5D=http%3A%2F%2Fimg.800pharm.com%2Fimages%2F20140422%2F20140422161645_818.jpg&sku_item%5B0%5D%5Bimages_boxs%5D%5B3%5D%5Bid%5D=822090&sku_item%5B0%5D%5Bimages_boxs%5D%5B3%5D%5Burl%5D=http%3A%2F%2Fimg.800pharm.com%2Fimages%2F20140422%2F20140422161630_791.jpg&sku_item%5B0%5D%5Bimages_boxs%5D%5B4%5D%5Bid%5D=822091&sku_item%5B0%5D%5Bimages_boxs%5D%5B4%5D%5Burl%5D=http%3A%2F%2Fimg.800pharm.com%2Fimages%2F20140422%2F20140422161648_347.jpg&sku_item%5B0%5D%5Bimages%5D%5B%5D=822087&sku_item%5B0%5D%5Bimages%5D%5B%5D=822088&sku_item%5B0%5D%5Bimages%5D%5B%5D=822089&sku_item%5B0%5D%5Bimages%5D%5B%5D=822090&sku_item%5B0%5D%5Bimages%5D%5B%5D=822091&sku_item%5B0%5D%5Bdescription%5D=%3Cp%3E%3Cbr%3E%3C%2Fp%3E&sku_item%5B0%5D%5Bself_define_spec%5D=&sku_item%5B0%5D%5Bself_define_name%5D=123&sku_item%5B0%5D%5Btitle%5D=&sku_item%5B0%5D%5Bshop_price%5D=79.00&sku_item%5B0%5D%5Bmobile_price%5D=&sku_item%5B0%5D%5Bmarket_price%5D=79.00&sku_item%5B0%5D%5Bstorage%5D=1&sku_item%5B0%5D%5Bproduct_number%5D=&sku_item%5B0%5D%5Bweight%5D=20&sku_item%5B0%5D%5Bbar_code%5D=&sku_item%5B0%5D%5Bunit%5D=%E7%9B%92&shelf_id=48418", token);

                                            string result = request.HttpPost(sUrl, postData, heads);

                                            if (!string.IsNullOrEmpty(result))
                                            {
                                                Thread.Sleep(random.Next(20, 30) * 1000);
                                                JObject rObj = JsonConvert.DeserializeObject<JObject>(result);

                                                if (rObj != null && rObj["code"].ToString() == "0")
                                                {
                                                    CommonFun.WriteCSV(fileName + "/upNew" + ticks + fileExtendName, item);
                                                    return true;
                                                }
                                                else
                                                {
                                                    item.Remark = rObj.ToString();
                                                    CommonFun.WriteCSV(fileName + "/upNewFailed" + ticks + fileExtendName, item);
                                                    Console.WriteLine("UpNew:{0}", rObj.ToString());
                                                    return false;
                                                }
                                            }
                                            //string postData = "is_drug=1&_token={0}&sku_item%5B0%5D%5Bstandard_sku_id%5D={1}&sku_item%5B0%5D%5Bstandard_product_id%5D={2}&sku_item%5B0%5D%5Bspecification_name%5D={3}&sku_item%5B0%5D%5Bspec_value_map%5D={4}&sku_item%5B0%5D%5Bspec_value%5D%5B0%5D%5Bspec_id%5D={5}&sku_item%5B0%5D%5Bspec_value%5D%5B0%5D%5Bspec_value_id%5D={6}&images_boxs={7}&sku_item%5B0%5D%5Bimages_boxs%5D%5B0%5D%5Burl%5D={8}&sku_item%5B0%5D%5Bimages_boxs%5D%5B1%5D%5Bid%5D={9}&sku_item%5B0%5D%5Bimages_boxs%5D%5B1%5D%5Burl%5D={10}&sku_item%5B0%5D%5Bimages_boxs%5D%5B2%5D%5Bid%5D={11}&sku_item%5B0%5D%5Bimages_boxs%5D%5B2%5D%5Burl%5D={12}&sku_item%5B0%5D%5Bimages_boxs%5D%5B3%5D%5Bid%5D={12}&sku_item%5B0%5D%5Bimages_boxs%5D%5B3%5D%5Burl%5D={13}&sku_item%5B0%5D%5Bimages%5D%5B%5D={14}&sku_item%5B0%5D%5Bimages%5D%5B%5D={15}&sku_item%5B0%5D%5Bimages%5D%5B%5D={16}&sku_item%5B0%5D%5Bimages%5D%5B%5D={17}&sku_item%5B0%5D%5Bdescription%5D={17}&sku_item%5B0%5D%5Bself_define_spec%5D=&sku_item%5B0%5D%5Bself_define_name%5D={18}&sku_item%5B0%5D%5Btitle%5D=&sku_item%5B0%5D%5Bshop_price%5D={19}&sku_item%5B0%5D%5Bmobile_price%5D=&sku_item%5B0%5D%5Bmarket_price%5D={20}&sku_item%5B0%5D%5Bstorage%5D={21}&sku_item%5B0%5D%5Bproduct_number%5D=&sku_item%5B0%5D%5Bweight%5D={22}&sku_item%5B0%5D%5Bbar_code%5D=&sku_item%5B0%5D%5Bunit%5D={23}&shelf_id={24}";
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("id:{0} format:{1} error:{2}", item.ID, item.Format, ex);
            }

            return false;
        }

        private string SeachMenuList()
        {
            string url = "http://pms.800pharm.com/merchant/common/search-catalog-list";

            string postData = string.Format("_token={0}&parent_id=0");

            string content = request.HttpPost(url, postData, heads);

            return content;
        }

        public bool Submit(string content, string id)
        {
            try
            {
                if (!string.IsNullOrEmpty(content))
                {
                    JObject dJ = JsonConvert.DeserializeObject<JObject>(content);

                    if (dJ != null)
                    {
                        JObject dataJ = JsonConvert.DeserializeObject<JObject>(dJ["data"].ToString());

                        JObject dDataJ = JsonConvert.DeserializeObject<JObject>(dataJ["data"].ToString());

                        JObject baseInfoJ = JsonConvert.DeserializeObject<JObject>(dDataJ["base_info"].ToString());

                        JObject instructionJ = JsonConvert.DeserializeObject<JObject>(dDataJ["base_drug_instruction"].ToString());

                        string chemical_name = string.IsNullOrEmpty(baseInfoJ["chemical_name"].ToString()) ? "" : CommonFun.GetUrlEncode(baseInfoJ["chemical_name"].ToString());
                        string english_name = string.IsNullOrEmpty(baseInfoJ["english_name"].ToString()) ? "" : CommonFun.GetUrlEncode(baseInfoJ["english_name"].ToString());
                        string product_attribute = baseInfoJ["product_attribute"].ToString();
                        string expiry_date = string.IsNullOrEmpty(instructionJ["expiry_date"].ToString()) ? "" : CommonFun.GetNum(instructionJ["expiry_date"].ToString());
                        string medical_insurance_class = string.IsNullOrEmpty(instructionJ["medical_insurance_class"].ToString()) ? "" : CommonFun.GetUrlEncode(instructionJ["medical_insurance_class"].ToString());
                        string major_component = string.IsNullOrEmpty(instructionJ["major_component"].ToString()) ? "" : CommonFun.GetUrlEncode(instructionJ["major_component"].ToString());
                        string character = string.IsNullOrEmpty(instructionJ["character"].ToString()) ? "" : CommonFun.GetUrlEncode(instructionJ["character"].ToString());
                        string functional_indications = string.IsNullOrEmpty(instructionJ["functional_indications"].ToString()) ? "" : CommonFun.GetUrlEncode(instructionJ["functional_indications"].ToString());
                        string adverse_reactions = string.IsNullOrEmpty(instructionJ["adverse_reactions"].ToString()) ? "" : CommonFun.GetUrlEncode(instructionJ["adverse_reactions"].ToString());
                        string avoid_notices = string.IsNullOrEmpty(instructionJ["avoid_notices"].ToString()) ? "" : CommonFun.GetUrlEncode(instructionJ["avoid_notices"].ToString());
                        string attention = string.IsNullOrEmpty(instructionJ["attention"].ToString()) ? "" : CommonFun.GetUrlEncode(instructionJ["attention"].ToString());
                        string drug_interactions = string.IsNullOrEmpty(instructionJ["drug_interactions"].ToString()) ? "" : CommonFun.GetUrlEncode(instructionJ["drug_interactions"].ToString());
                        string effects = string.IsNullOrEmpty(instructionJ["effects"].ToString()) ? "" : CommonFun.GetUrlEncode(instructionJ["effects"].ToString());
                        string pharmacology = string.IsNullOrEmpty(instructionJ["pharmacology"].ToString()) ? "" : CommonFun.GetUrlEncode(instructionJ["pharmacology"].ToString());
                        string storage = string.IsNullOrEmpty(instructionJ["storage"].ToString()) ? "" : CommonFun.GetUrlEncode(instructionJ["storage"].ToString());
                        string applicable_people = string.IsNullOrEmpty(instructionJ["applicable_people"].ToString()) ? "" : CommonFun.GetUrlEncode(instructionJ["applicable_people"].ToString());


                        string url = "http://pms.800pharm.com/merchant/release/choose-standard-sku-to-release";

                        string postdata = string.Format("_token={0}&standard_sku%5Bid%5D={1}&base_info%5Bchemical_name%5D={2}&base_info%5Benglish_name%5D={3}&base_drug_instruction%5Bexpiry_date%5D={4}&base_drug_instruction%5Bmedical_insurance_class%5D={5}&base_drug_instruction%5Bmajor_component%5D={6}&base_drug_instruction%5Bcharacter%5D={7}&base_drug_instruction%5Bfunctional_indications%5D={8}&base_drug_instruction%5Badverse_reactions%5D={9}&base_drug_instruction%5Bavoid_notices%5D={10}&base_drug_instruction%5Battention%5D={11}&base_drug_instruction%5Bdrug_interactions%5D={12}&base_drug_instruction%5Beffects%5D={13}&base_drug_instruction%5Bpharmacology%5D={14}&base_drug_instruction%5Bstorage%5D={15}&base_drug_instruction%5Bapplicable_people%5D={16}&base_info%5Bproduct_attribute%5D={17}",
                            token, id, chemical_name, english_name, expiry_date, medical_insurance_class, major_component, character, functional_indications, adverse_reactions, avoid_notices, attention, drug_interactions, effects, pharmacology, storage, applicable_people, product_attribute);

                        string result = request.HttpPost(url, postdata);

                        if (!string.IsNullOrEmpty(result))
                        {
                            JObject rJ = JsonConvert.DeserializeObject<JObject>(result);

                            if (rJ != null && rJ["code"].ToString() == "0")
                            {
                                return true;
                            }
                            else
                            {
                                Console.WriteLine("Sumbit result：{0}", rJ.ToString());
                            }
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

        public string SeachItemDetail(string id)
        {
            try
            {
                string url = string.Format("http://pms.800pharm.com/merchant/release/{0}/choose-standard-sku-detail", id);

                string postData = string.Format("_token={0}&standard_sku_id={1}", token, id);

                string content = request.HttpPost(url, postData, heads);

                return content;

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return null;
        }

        private string SeachNewItemInfo(string id)
        {
            try
            {
                string url = "http://pms.800pharm.com/merchant/release/search-standard-sku";

                string postData = string.Format("_token={1}&approval_number={0}&name=&common_name=&manufacturer_name=&brand_name=&specification_name=", CommonFun.GetUrlEncode(id), token);

                string content = request.HttpPost(url, postData, null, "application/json, text/javascript, */*; q=0.01", null, heads);

                return content;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return null;
        }

        private Dictionary<string, BaseItemInfo> GetItemsByUrl(string url)
        {
            Dictionary<string, BaseItemInfo> items = new Dictionary<string, BaseItemInfo>();
            int records = 100;

            int page = 1;
            int totalPage = 0;

            do
            {
                try
                {
                    string postData = string.Format("page={0}&records={1}&_token={2}", page, records, token);

                    string content = request.HttpPost(url, postData, heads);

                    if (totalPage == 0)
                    {
                        string pageStr = CommonFun.GetValue(content, "total\":", ",");

                        totalPage = (int)Math.Ceiling(Convert.ToDouble(pageStr) / records);
                    }

                    Console.WriteLine("{0} Get page item totalPage:{1} curPage:{2}", DateTime.Now, totalPage, page);

                    List<BaseItemInfo> pItems = GetItemList(content);

                    foreach (BaseItemInfo item in pItems)
                    {
                        string key = item.Name + item.Format + item.Created;

                        if (!items.ContainsKey(key))
                        {
                            items.Add(key, item);
                        }
                        else
                        {
                            Console.WriteLine("Have same key:{0}", key);
                        }
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            } while (++page <= totalPage);


            return items;
        }

        /// <summary>
        /// 在售列表
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, BaseItemInfo> GetSellingItems()
        {
            string url = "http://pms.800pharm.com/merchant/manage";

            return GetItemsByUrl(url);
        }

        /// <summary>
        /// 下架列表
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, BaseItemInfo> GetDownItems()
        {
            string url = "http://pms.800pharm.com/merchant/down-shelf";

            //get info

            //post detailInfo 

            string postSelfData = "page=1&records=10&_token=3XJDiFH0lCJ8sUz8F3lfTDaOGHKWlsM5Sogoyult";

            string postPlatformData = "page=1&records=10&type=platform&_token=3XJDiFH0lCJ8sUz8F3lfTDaOGHKWlsM5Sogoyult";

            return GetItemsByUrl(url);
        }

        /// <summary>
        /// 审核列表
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, BaseItemInfo> GetAuditItems()
        {
            string url = "http://pms.800pharm.com/merchant/audit";

            string postData = "page=1&records=10&_token=3XJDiFH0lCJ8sUz8F3lfTDaOGHKWlsM5Sogoyult";

            return GetItemsByUrl(url);
        }

        public decimal GetMinPrice(string keyword, string format)
        {
            try
            {
                string url = string.Format("http://www.800pharm.com/shop/search.html?keyword={0}", keyword);


                string content = request.HttpGet(url);

                MatchCollection ms = CommonFun.GetValues(content, "class=\"goNextBtn\" href=", "</a>");

                foreach (Match m in ms)
                {
                    bool isOlny = m.Value.Contains("查看商品详情");
                    string infoUrl = CommonFun.GetValue(m.Value, "\"", "\"");

                    if (!isOlny)
                    {
                        int page = 1;
                        int totalPage = 0;
                        bool isSame = false;
                        do
                        {
                            infoUrl += string.Format("?sortType=1&p={0}", page);

                            string infoContent = request.HttpGet(infoUrl);
                            if (totalPage == 0)
                            {
                                string pageStr = CommonFun.GetValue(infoContent, "<div class=\"pages\">", "</div> ");

                                pageStr = CommonFun.GetValue(pageStr, "下一页", "末页");

                                pageStr = CommonFun.GetValue(pageStr, "p=", "'");

                                totalPage = Convert.ToInt32(pageStr);

                                string sFormat = CommonFun.GetValue(infoContent, "<p class=\"prod_standard\">", "</p>");
                                isSame = CommonFun.IsSameFormat(format, sFormat, keyword, keyword);
                            }
                            infoContent = CommonFun.GetValue(infoContent, "<ul class=\"prod_list\">", "</ul>");

                            if (isSame)
                            {
                                MatchCollection iMs = CommonFun.GetValues(infoContent, "<li>", "</li>");

                                foreach (Match iM in iMs)
                                {
                                    if (iM.Value.Contains("<input type=\"button\""))
                                    {
                                        string storeName = CommonFun.GetValue(iM.Value, "<h4>", "</h4>");
                                        storeName = CommonFun.GetValue(storeName, "title=\"", "\"");

                                        if (storeName != selfStoreName)
                                        {
                                            string priceStr = CommonFun.GetValue(iM.Value, "<b>￥</b>", "</p>");
                                            return Convert.ToDecimal(priceStr);
                                        }
                                    }
                                }
                            }
                        } while (++page <= totalPage && isSame);
                    }
                    else
                    {
                        string infoContent = request.HttpGet(infoUrl);

                        string sformat = CommonFun.GetValue(infoContent, "<div class=\"b_select\">", "</div>");

                        sformat = CommonFun.GetValue(sformat, "<span>", "</span>");

                        if (CommonFun.IsSameFormat(sformat, format, keyword, keyword))
                        {
                            string priceStr = CommonFun.GetValue(infoContent, "<em>￥</em>", "</span>");

                            return Convert.ToDecimal(priceStr);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return decimal.MaxValue;
        }


        public override void ReadAllMenuURL()
        {
            string url = "http://www.800pharm.com/product/drug_class_list.html";

            string content = request.HttpGet(url);

            MatchCollection ms = CommonFun.GetValues(content, "<div class='classify_text2'>", "</div>");

            foreach (Match m in ms)
            {
                MatchCollection uMs = CommonFun.GetValues(m.Value, "<dd>", "</dd>");

                foreach (Match uM in uMs)
                {
                    MatchCollection iUMs = CommonFun.GetValues(uM.Value, "<a href='", "'");

                    foreach (Match iUM in iUMs)
                    {
                        if (!AllMenuUrl.Contains(iUM.Value))
                        {
                            AllMenuUrl.Add(iUM.Value);
                        }
                    }

                }
            }
        }

        public override void ReadAllItemURL()
        {
            foreach (string url in AllMenuUrl)
            {
                try
                {
                    string content = request.HttpGet(url);
                    string pageStr = CommonFun.GetValue(content, "<span class='page_text'>1/", "<");
                    if (!string.IsNullOrEmpty(pageStr))
                    {
                        int page = Convert.ToInt32(pageStr);

                        for (int i = 1; i <= page; i++)
                        {
                            content = request.HttpGet(url + "&p=" + i);

                            MatchCollection ms = CommonFun.GetValues(content, "<dt class=\"pic\">", "</dt>");

                            foreach (Match m in ms)
                            {
                                string iUrl = CommonFun.GetValue(m.Value, "<a href='", "'");
                                if (!AllItemUrl.Contains(iUrl))
                                {
                                    AllItemUrl.Add(iUrl);
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
        }

        public override void ReadAllItem()
        {
            foreach (string url in AllItemUrl)
            {
                try
                {
                    if (!string.IsNullOrEmpty(url))
                    {
                        string content = request.HttpGet(url);
                        BaseItemInfo item;
                        if (url.Contains("groupId"))
                        {
                            item = GetItemInfo(content);
                        }
                        else
                        {
                            item = GetItemInfoOther(content);
                        }
                        AddItemNew(item, "EightHundredF/EHF" + ticks + ".csv");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }

            }
        }

        private BaseItemInfo GetItemInfoOther(string content)
        {
            BaseItemInfo item = new BaseItemInfo();
            try
            {
                string itemInfoStr = CommonFun.GetValue(content, "<li><span class=\"fontGrey2\">", "<li class=\"clearfix kuaidi\">");

                MatchCollection ms = CommonFun.GetValues(itemInfoStr, "<li", "</li>");

                int add_vule = 0;

                if (!ms[2].Value.Contains("可选规格"))
                {
                    add_vule = 1;
                }

                MatchCollection fMs = CommonFun.GetValues(ms[2 + add_vule].Value, "<div id=", "</div>");
                string cur_item = CommonFun.GetValue(ms[2 + add_vule].Value, "{", ";");
                cur_item = CommonFun.GetNum(cur_item);
                cur_item = cur_item.Replace(".", " ");
                foreach (Match fm in fMs)
                {
                    string id = CommonFun.GetValue(fm.Value, "\"v_", "\"");
                    id = CommonFun.GetNum(id);
                    if (Convert.ToInt32(id) == Convert.ToInt32(cur_item))
                    {
                        item.Format = CommonFun.GetValue(fm.Value, "<span>", "</span>");
                        break;
                    }
                }

                item.Name = CommonFun.GetValue(ms[4 + add_vule].Value, "<span class=\"fontGrey1\">", "</span>");
                item.Name = CommonFun.GetValue(item.Name, ">", "<");
                item.Name = item.Name.Trim();
                string createdStr = CommonFun.GetValue(content, "生产厂商：</td>", "</tr>");
                item.Created = CommonFun.GetValue(createdStr, "<td>", "</td>");
                item.ID = CommonFun.GetValue(ms[5 + add_vule].Value, "<a", "<em>|</em>");
                item.ID = CommonFun.GetValue(item.ID, ">", "</a>");
                item.ID = item.ID.Trim();
                string priceStr = CommonFun.GetValue(itemInfoStr, "<em>￥</em>", "</span>");
                item.ShopPrice = string.IsNullOrEmpty(priceStr) ? 0 : Convert.ToDecimal(priceStr);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return item;
        }

        private BaseItemInfo GetItemInfo(string content)
        {
            BaseItemInfo item = new BaseItemInfo();
            try
            {
                item.Format = CommonFun.GetValue(content, "<p class=\"prod_standard\">", "</p>");
                string itemInfoStr = CommonFun.GetValue(content, "<div class=\"prod_box\">", "<ul class=\"prod_tabs\" id=\"prod_tabs\">");

                MatchCollection ms = CommonFun.GetValues(itemInfoStr, "<dd", "</dd>");
                item.Name = CommonFun.GetValue(ms[0].Value, "\">", "</a>");
                item.Created = CommonFun.GetValue(ms[1].Value, ">", " ");
                item.ID = CommonFun.GetValue(ms[2].Value, ">", "&");
                string priceStr = CommonFun.GetValue(itemInfoStr, "<b>￥</b>", "~");
                priceStr = priceStr.Trim();
                item.ShopPrice = string.IsNullOrEmpty(priceStr) ? 0 : Convert.ToDecimal(priceStr);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return item;
        }

        public void Compare()
        {
            ReadBaseItemInfo("EightHundredF/EHF636579559061038001.csv", true);

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
                            CommonFun.WriteCSV("EightHundredF/NoFormat" + ticks + ".csv", item);
                        }
                    }
                    else
                    {
                        CommonFun.WriteCSV("EightHundredF/NotExist" + ticks + ".csv", item);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
        }

        public override bool ComparePrice(BaseItemInfo platformItem, BaseItemInfo info)
        {
            if (info.ShopPrice > 0)
            {
                decimal rate = 0.3m;
                string name = "25以下";
                if (info.ShopPrice > 60)
                {
                    rate = 0.7m;
                    name = "60以上";
                }
                else if (info.ShopPrice > 40)
                {
                    rate = 0.5m;
                    name = "40-60";
                }
                else if (info.ShopPrice > 25)
                {
                    rate = 0.4m;
                    name = "25-40";
                }

                if (info.ShopPrice * rate >= platformItem.ShopSelaPrice)
                {
                    info.PlatformPrice = platformItem.ShopSelaPrice;
                    info.Type = platformItem.Type;
                    info.ViewCount = platformItem.ViewCount;

                    if (!string.IsNullOrEmpty(platformItem.ViewCount) && Convert.ToInt32(platformItem.ViewCount) > 3000)
                    {
                        CommonFun.WriteCSV("EightHundredF/3000-" + name + ticks + ".csv", info);
                    }
                    else if (!string.IsNullOrEmpty(platformItem.ViewCount) && Convert.ToInt32(platformItem.ViewCount) > 2000)
                    {
                        CommonFun.WriteCSV("EightHundredF/2000-" + name + ticks + ".csv", info);
                    }
                    else if (!string.IsNullOrEmpty(platformItem.ViewCount) && Convert.ToInt32(platformItem.ViewCount) > 1000)
                    {
                        CommonFun.WriteCSV("EightHundredF/1000-" + name + ticks + ".csv", info);
                    }
                    else if (!string.IsNullOrEmpty(platformItem.ViewCount) && Convert.ToInt32(platformItem.ViewCount) > 500)
                    {
                        CommonFun.WriteCSV("EightHundredF/500-" + name + ticks + ".csv", info);
                    }
                    else
                    {
                        CommonFun.WriteCSV("EightHundredF/40以上" + name + ticks + ".csv", info);
                    }
                    return true;
                }
            }
            return false;
        }
    }
}
