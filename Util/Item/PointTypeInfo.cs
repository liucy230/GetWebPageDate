using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GetWebPageDate.Util.Item
{
    /// <summary>
    /// “.”后缀编码信息
    /// </summary>
    public class PointTypeInfo
    {
        /// <summary>
        /// 编码
        /// </summary>
        public List<string> Type { get; set; }

        /// <summary>
        /// 点击率
        /// </summary>
        public int ClickCount { get; set; }

        /// <summary>
        /// 有效价格库存
        /// </summary>
        public int GetPriceStock { get; set; }

        /// <summary>
        /// 改价最小库存值
        /// </summary>
        public int LowerStock { get; set; }

        /// <summary>
        /// 大于最低库存量降价值
        /// </summary>
        public decimal MTLowerPrice { get; set; }

        /// <summary>
        /// 小于最低库存量降价值
        /// </summary>
        public decimal LTLowerPrice { get; set; }

        /// <summary>
        /// 最大降价幅度
        /// </summary>
        public int MaxDownRate { get; set; }
    }
}
