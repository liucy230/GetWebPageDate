using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GetWebPageDate.Util.Item
{
    public class ItemInfo : BaseItemInfo
    {
        /// <summary>
        /// 一级菜单
        /// </summary>
        public string Menu1 { get; set; }

        /// <summary>
        /// 二级菜单
        /// </summary>
        public string Menu2 { get; set; }

        /// <summary>
        /// 三级菜单
        /// </summary>
        public string Menu3 { get; set; }

        /// <summary>
        /// 品牌名
        /// </summary>
        public string BrandName { get; set; }

        /// <summary>
        /// 药品属性
        /// </summary>
        public string DrugProtery { get; set; }

        /// <summary>
        /// 药品分类
        /// </summary>
        public string DrugType { get; set; }

        /// <summary>
        /// 功能主治
        /// </summary>
        public string Function { get; set; }

        /// <summary>
        /// 储藏方法
        /// </summary>
        public string SaveType { get; set; }

        /// <summary>
        /// 主要成分
        /// </summary>
        public string Basis { get; set; }

        /// <summary>
        /// 性状
        /// </summary>
        public string Character { get; set; }

        /// <summary>
        /// 用法用量
        /// </summary>
        public string Use { get; set; }

        /// <summary>
        /// 不良反应
        /// </summary>
        public string AdverseReaction { get; set; }

        /// <summary>
        /// 禁忌症
        /// </summary>
        public string Contraindication { get; set; }

        /// <summary>
        /// 注意事项
        /// </summary>
        public string NoticMatters { get; set; }

        /// <summary>
        /// 图片路径
        /// </summary>
        public string PicturePath { get; set; }

        public override string GetLogHeadLine()
        {
            return "批准文号,一级菜单,二级菜单,三级菜单,商品名称,通用名称,出售方式（零或整）,生产厂家,包装规格,品牌名称,剂型,药品分类,药品属性,商城售价(最低价格),平台售价（最低价格）,商城折后售价,折扣,返利,功能主治,储藏方法,主要成分,性状,用法用量,不良反应,禁忌症,注意事项,图片路径,重量（克）,库存,最近浏览, 备注";
        }

        public override string[] GetLogStrArr()
        {
            return new[]
                {
                     "" + ID,
                     "" + Menu1,
                     "" + Menu2,
                     "" + Menu3,
                     "" + ItemName,
                     "" + Name,
                     "" + SellType,
                     "" + Created,
                     "" + Format,
                     "" + BrandName,
                     "" + Type,
                     "" + DrugType,
                     "" + DrugProtery,
                     "" + ShopPrice,
                     "" + PlatformPrice,
                     "" + ShopSelaPrice,
                     "" + Sela,
                     "" + ReturnPrice,
                     "" + Function,
                     "" + SaveType,
                     "" + Basis,
                     "" + Character,
                     "" + Use,
                     "" + AdverseReaction,
                     "" + Contraindication,
                     "" + NoticMatters,
                     "" + PicturePath,
                     "" + Weight,
                     "" + Inventory,
                     "" + ViewCount,
                     "" + Remark,
                };
        }
    }
}
