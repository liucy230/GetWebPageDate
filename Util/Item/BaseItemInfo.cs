using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GetWebPageDate.Util.Item
{
    public class BaseItemInfo
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
        /// 商品名称
        /// </summary>
        public string ItemName { get; set; }

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
        /// 商城折后售价
        /// </summary>
        public decimal ShopSelaPrice { get; set; }

        /// <summary>
        /// 折扣
        /// </summary>
        public decimal Sela { get; set; }

        /// <summary>
        /// 最高返利
        /// </summary>
        public decimal ReturnPrice { get; set; }

        /// <summary>
        /// 剂型
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// 重量（克）
        /// </summary>
        public decimal Weight { get; set; }

        /// <summary>
        /// 库存
        /// </summary>
        public string Inventory { get; set; }

        /// <summary>
        /// 最近浏览
        /// </summary>
        public string ViewCount { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        public string Remark { get; set; }

        public virtual string GetLogHeadLine()
        {
            return "批准文号,通用名称,商品名称,出售方式（零或整）,生产厂家,包装规格,商城售价(最低价格),平台售价（最低价格）,商城折后售价,折扣, 返利, 剂型,重量（克）,库存,最近浏览, 备注";
        }

        public virtual string[] GetLogStrArr()
        {
            return new[]
            {
                "" + ID,
                "" + Name,
                "" + ItemName,
                "" + SellType,
                "" + Created,
                "" + Format,
                "" + ShopPrice,
                "" + PlatformPrice,
                "" + ShopSelaPrice,
                "" + Sela,
                "" + ReturnPrice,
                "" + Type,
                "" + Weight,
                "" + Inventory,
                "" + ViewCount,
                "" + Remark,
            };
        }

    }
}
