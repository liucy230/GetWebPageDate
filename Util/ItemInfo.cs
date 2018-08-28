using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GetWebPageDate.Util
{
    public class ItemInfo
    {
        /// <summary>
        /// 批准文号
        /// </summary>
        public string ID { get; set; }

        /// <summary>
        /// 通用名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 出售方式（零或整）
        /// </summary>
        public string SellType { get; set; }
        /// <summary>
        /// 生产厂家
        /// </summary>
        public string Created { get; set; }

        /// <summary>
        /// 包装规格
        /// </summary>
        public string Format { get; set; }

        /// <summary>
        /// 商城售价(最低价格)
        /// </summary>
        public decimal ShopPrice { get; set; }

        /// <summary>
        /// 平台售价（最低价格）
        /// </summary>
        public decimal PlatformPrice { get; set; }

        /// <summary>
        /// 剂型
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// 重量（克）
        /// </summary>
        public int Weight { get; set; }

        /// <summary>
        /// 库存
        /// </summary>
        public string Inventory { get; set; }

        /// <summary>
        /// 最近浏览
        /// </summary>
        public string ViewCount { get; set; }

        public string LogHeadLine { get; set; }

        public ItemInfo()
        {
            LogHeadLine = "批准文号,通用名称,出售方式（零或整）,生产厂家,包装规格,商城售价(最低价格),平台售价（最低价格）,剂型,重量（克）,库存,最近浏览";
        }

        public string[] getLogStrArr()
        {
            return new[]
                {
                     ID,
                    "" + Name,
                    "" + SellType,
                    "" + Created,
                    "" + Format,
                    "" + ShopPrice,
                    "" + PlatformPrice,
                    "" + Type,
                    "" + Weight,
                    "" + Inventory,
                    "" + ViewCount,
                };
        }
    }
}
