using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GetWebPageDate.Util.Item
{

    public class YFOrderWriteInfo
    {
        private List<BaseItemInfo> totalItem = new List<BaseItemInfo>();
        /// <summary>
        /// 整条订单信息
        /// </summary>
        public BaseItemInfo BaseItem { get; set; }

        /// <summary>
        /// 分裂订单信息
        /// </summary>
        public List<BaseItemInfo> TotalItem { get { return totalItem; } }
    }
}
