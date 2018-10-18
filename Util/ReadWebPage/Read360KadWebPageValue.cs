using GetWebPageDate.Http;
using GetWebPageDate.Util.Item;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GetWebPageDate.Util
{
    /// <summary>
    /// 360康
    /// </summary>
    public class Read360KadWebPageValue : BaseReadWebPage
    {
        public override void ReadAllMenuURL()
        {
            string url = "http://www.360kad.com/dymhh/allclass.shtml";

            string content = request.HttpGet(url);

            int startIndex = content.IndexOf("clearfix ksBoxs");

            content = CommonFun.GetValue(content, "<ul class=\"clearfix ksBoxs\" ", "</ul>");

            MatchCollection ms = CommonFun.GetValues(content, "<div class=\"right\">", "</div>");

            List<string> menuUrl = new List<string>();

            foreach (Match m in ms)
            {
                MatchCollection msUrl = CommonFun.GetValues(m.Value, "href=\"", "\"");

                foreach (Match mUrl in msUrl)
                {
                    if (!menuUrl.Contains(mUrl.Value))
                    {
                        menuUrl.Add(mUrl.Value);
                    }
                }
            }

            AllMenuUrl = menuUrl;
        }

        public override void ReadAllItem()
        {
            int totalCount = AllMenuUrl.Count;
            int curCount = 0;

            foreach (string menuUrl in AllMenuUrl)
            {
                try
                {
                    if (!menuUrl.Contains(".shtml"))
                    {
                        bool isAspx = menuUrl.Contains(".aspx");

                        string content = request.HttpGet(menuUrl);

                        GetItemUrl(content);

                        int pageCount = GetTotalPage(content, isAspx);

                        for (int i = 2; i <= pageCount; i++)
                        {
                            string pageUrl = "";

                            if (isAspx)
                            {
                                pageUrl = menuUrl + "page=" + i;
                            }
                            else
                            {
                                pageUrl = menuUrl + "&pageIndex=" + i;
                            }

                            content = request.HttpGet(pageUrl);

                            GetItemUrl(content);
                        }
                    }
                    else
                    {
                        if (!AllItemUrl.Contains(menuUrl))
                        {
                            AllItemUrl.Add(menuUrl);
                        }

                    }

                    Console.WriteLine("Menu TotalCount:{0}, CurCount:{1}", totalCount, ++curCount);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("error:{0}, menuUrl:{1}", ex.ToString(), menuUrl);
                }
            }

            int totalItemUrlCount = AllItemUrl.Count;
            int curItmeUrlCount = 0;
            foreach (string itemUrl in AllItemUrl)
            {
                try
                {
                    ReadOneItem(itemUrl);

                    Console.WriteLine("Item TotalCount:{0}, CurCount:{1}", totalItemUrlCount, ++curItmeUrlCount);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("error:{0}, menuUrl:{1}", ex.ToString(), itemUrl);
                }
            }
        }

        private void GetItemUrl(string content)
        {
            string productContent = CommonFun.GetValue(content, "<ul class=\"Productlist\">", "</ul>");

            MatchCollection msItem = CommonFun.GetValues(productContent, "<p class=\"t\">", "</p>");

            foreach (Match m in msItem)
            {
                string url = CommonFun.GetValue(m.Value, "href=\"", "\"");

                if (!AllItemUrl.Contains(url))
                {
                    AllItemUrl.Add(url);
                }
            }
        }

        private int GetTotalPage(string content, bool isAspx)
        {
            if (isAspx)
            {
                string str = CommonFun.GetValue(content, "<strong>", "</strong>");

                return string.IsNullOrEmpty(str) ? 0 : Convert.ToInt32(str);
            }
            else
            {
                string pageContent = CommonFun.GetValue(content, "YPagebox", "</span>");

                MatchCollection ms = CommonFun.GetValues(pageContent, ">", "</a>");

                return ms.Count - 2;
            }
        }

        public void ReadOneItem(string url)
        {
            try
            {
                string content = request.HttpGet(url);

                int startIndex = content.IndexOf("<div class=\"prem-dtl-infomation\">");

                int endIndex = content.IndexOf("<div class=\"inf-r-wxhb\">");

                string mainStr = content.Substring(startIndex, endIndex - startIndex);

                MatchCollection ms = CommonFun.GetValues(mainStr, "<div class=\"dtl-inf-r\">", "</div>");

                if (!string.IsNullOrEmpty(ms[0].Value) && !ms[0].Value.Contains("注射"))
                {
                    ItemInfo info = new ItemInfo();

                    info.Name = ms[0].Value;

                    string idStr = ms[1].Value;

                    info.ID = string.IsNullOrEmpty(idStr) ? "" : idStr.Substring(0, idStr.IndexOf('\n'));

                    info.Created = ms[2].Value;

                    info.Format = CommonFun.GetValue(content, "规格：", "<");

                    info.Type = CommonFun.GetValue(content, "剂型：", "<");

                    info.ItemName = CommonFun.GetValue(content, "商品名称：", "<");

                    info.BrandName = info.ItemName.Split(' ')[0];

                    info.DrugType = GetDrugType(content);

                    string priceStr = CommonFun.GetValue(content, "salePrice : ", ",");

                    info.ShopPrice = string.IsNullOrEmpty(priceStr) ? 0 : Convert.ToDecimal(priceStr);

                    string key = info.ID + "{" + info.Format + "}";

                    SetMenuInfo(content, info);

                    SetMainInfo(content, info);

                    SetPicture(content, info);

                    if (ShopAllItems.ContainsKey(key))
                    {
                        if (info.ShopPrice != 0 && ShopAllItems[key].ShopPrice > info.ShopPrice)
                        {
                            ShopAllItems[key] = info;
                        }
                    }
                    else
                    {
                        ShopAllItems.Add(key, info);
                    }

                    // CommonFun.WriteCSV("360Kad.csv", info);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex + string.Format(url));
            }
        }

        private void SetMenuInfo(string content, ItemInfo info)
        {
            string menuStr = CommonFun.GetValue(content, "wrap-dtl-nav", "</div>");

            MatchCollection menuMs = CommonFun.GetValues(menuStr, "\">", "</a>");

            info.Menu1 = menuMs[1].Value;

            info.Menu2 = menuMs[2].Value;

            info.Menu3 = menuMs[3].Value;
        }

        private void SetPicture(string content, ItemInfo info)
        {
            string prictureUrlStr = CommonFun.GetValue(content, "<div class=\"minPicScrolldiv\">", "</div>");

            MatchCollection ms = CommonFun.GetValues(prictureUrlStr, "src=\"", "\"");

            info.PicturePath = @"Picture/" + info.Name.Replace('.', '_').Replace('*', '_').Replace(':', '_').Replace(" ", "") + "/" + info.Format.Replace('.', '_').Replace('*', '_').Replace(':', '_').Replace(" ", "") + "/";

            int pictureCount = 0;

            //foreach (Match url in ms)
            //{
            //    Image image = request.HttpGetPicture(url.Value);

            //    CommonFun.SavePicture(image, info.PicturePath + pictureCount++ + ".jpg");
            //}


            info.PicturePath = string.Format("=HYPERLINK(\"{0}\")", info.PicturePath);
        }

        private void SetMainInfo(string content, ItemInfo info)
        {
            string mainInfoStr = CommonFun.GetValue(content, "<ul class=\"instructions-ul\">", "</ul>");

            int index = mainInfoStr.IndexOf("见【");

            List<string> replacelist = new List<string>();

            int i = 0;

            while (index > 0)
            {
                string tempStr = mainInfoStr.Substring(index, mainInfoStr.Length - index);

                int end = tempStr.IndexOf("】");

                string replaceStr = mainInfoStr.Substring(index, end + 1);

                replacelist.Add(replaceStr);

                mainInfoStr = mainInfoStr.Replace(replaceStr, string.Format("{{0}}", i++));

                index = mainInfoStr.IndexOf("见【");
            }

            MatchCollection titles = CommonFun.GetValues(mainInfoStr, "【", "】");

            MatchCollection ms = CommonFun.GetValues(mainInfoStr, "】", "【");

            List<string> values = new List<string>();

            foreach (Match m in ms)
            {
                values.Add(m.Value);
            }
            int startIndex = mainInfoStr.LastIndexOf("】");
            string lastValue = mainInfoStr.Substring(startIndex + 1, mainInfoStr.Length - startIndex - 1);
            int endIndex = lastValue.IndexOf('<');
            lastValue = lastValue.Substring(0, endIndex);
            values.Add(lastValue);
            for (int j = 0; j < replacelist.Count; j++)
            {
                for (int m = 0; m < values.Count; m++)
                {
                    string value = string.Format("{{0}}", j);

                    if (values[m].Contains(value))
                    {
                        values[m] = values[m].Replace(value, replacelist[j]);
                        continue;
                    }
                }
            }

            if (ms.Count > 1)
            {
                int mIndex = GetIndex("成", titles);
                if (mIndex != -1)
                {
                    info.Basis = RemoveTag(values[mIndex]);
                }

                mIndex = GetIndex("性", titles);
                if (mIndex != -1)
                {
                    info.Character = RemoveTag(values[mIndex]);
                }

                mIndex = GetIndex("治", titles);
                mIndex = mIndex == -1 ? mIndex = GetIndex("适", titles) : mIndex;
                if (mIndex != -1)
                {
                    info.Function = RemoveTag(values[mIndex]);
                }

                mIndex = GetIndex("法", titles);
                if (mIndex != -1)
                {
                    info.Use = RemoveTag(values[mIndex]);
                }

                mIndex = GetIndex("良", titles);
                if (mIndex != -1)
                {
                    info.AdverseReaction = RemoveTag(values[mIndex]);
                }

                mIndex = GetIndex("禁", titles);
                if (mIndex != -1)
                {
                    info.Contraindication = RemoveTag(values[mIndex]);
                }

                mIndex = GetIndex("意", titles);
                if (mIndex != -1)
                {
                    info.NoticMatters = RemoveTag(values[mIndex]);
                }

                mIndex = GetIndex("贮", titles);
                if (mIndex != -1)
                {
                    info.SaveType = RemoveTag(values[mIndex]);
                }
            }
        }

        private int GetIndex(string title, MatchCollection ms)
        {
            for (int i = 0; i < ms.Count; i++)
            {
                if (ms[i].Value.Contains(title))
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// 移除标记
        /// </summary>
        /// <param name="old"></param>
        /// <returns></returns>
        private string RemoveTag(string old)
        {
            return old.Replace("<p>", "").Replace("</p>", "").Replace("<br />", "").Replace("</strong>", "").Replace("<strong>", "");
        }

        private string GetDrugType(string content)
        {
            if (content.Contains("本品为乙类非处方药品"))
            {
                return "乙类OTC";
            }
            else if (content.Contains("本品为甲类非处方药品"))
            {
                return "甲类OTC";
            }
            else if ((content.Contains("本品为处方药品")))
            {
                return "处方药";
            }
            else
            {
                return "其他";
            }
        }

        public override void Login()
        {
            throw new NotImplementedException();
        }

        public override void Start()
        {
            throw new NotImplementedException();
        }
    }


}
