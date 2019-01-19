using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GetWebPageDate.Util.Item
{
    public class SalesItemInfo : BaseItemInfo
    {
        /// <summary>
        /// 销售量
        /// </summary>
        public string SalesVolume;

        /// <summary>
        /// 销售额
        /// </summary>
        public string SalesAmount;

        /// <summary>
        /// 销售排名
        /// </summary>
        public string SalesRanking;

        public override string GetLogHeadLine()
        {
            return "排名(销量),商品名称,包装规格,剂型,商品销量,销售额￥";
        }

        public override string[] GetLogStrArr()
        {
            return new[]
            {
                "" + SalesRanking,
                "" + ItemName,
                "" + Format,
                "" + Type,
                "" + SalesVolume,
                "" + SalesAmount,
            };
        }
    }
}
