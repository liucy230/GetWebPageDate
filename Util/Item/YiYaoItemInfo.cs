using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GetWebPageDate.Util.Item
{
    public class YiYaoItemInfo : BaseItemInfo
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
        /// 品牌
        /// </summary>
        public string Brand { get; set; }

        /// <summary>
        /// 海外购
        /// </summary>
        public string IsAbroad { get; set; }

        /// <summary>
        /// 副标题
        /// </summary>
        public string TagTitle { get; set; }

        /// <summary>
        /// 条形码
        /// </summary>
        public string NUM { get; set; }

        /// <summary>
        /// 外部商品编码
        /// </summary>
        public string OtherNUM { get; set; }

        /// <summary>
        /// 处方药
        /// </summary>
        public string IsOCT { get; set; }

        /// <summary>
        /// 多规格
        /// </summary>
        public string MoveFormat { get; set; }


        public override string GetLogHeadLine()
        {
            return "商品名称,规格,一级分类,二级分类,三级分类,品牌,海外购,生产厂家,毛重,副标题,条形码,外部商品编码,批准文号,处方药,多规格";
        }

        public override string[] GetLogStrArr()
        {
            return new[]
                {
                     "" + ItemName,
                     "" + Format,
                     "" + Menu1,
                     "" + Menu2,
                     "" + Menu3,
                     "" + Brand,
                     "" + IsAbroad,
                     "" + Created,
                     "" + Weight,
                     "" + TagTitle,
                     "" + NUM,
                     "" + OtherNUM,
                     "" + ID,
                     "" + IsOCT,
                     "" + MoveFormat,
                  };
        }
    }
}
