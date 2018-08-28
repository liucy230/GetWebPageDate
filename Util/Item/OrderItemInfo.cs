using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GetWebPageDate.Util.Item
{
    public class OrderItemInfo : BaseItemInfo
    {
        /// <summary>
        /// 下单时间
        /// </summary>
        public string GetOrderTime { get; set; }

        /// <summary>
        /// 电话号码
        /// </summary>
        public string PhoneNO { get; set; }

        /// <summary>
        /// 收件地址
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        /// 单价（元）
        /// </summary>
        public string Price { get; set; }
        /// <summary>
        /// 数量
        /// </summary>
        public string Count { get; set; }

        /// <summary>
        /// 姓名
        /// </summary>
        public string UserName { get; set; }

        public override string GetLogHeadLine()
        {
            return "下单时间,姓名,电话,地址,商品,生产厂家,批准文号,包装规格,单价（元）,数量";
        }

        public override string[] GetLogStrArr()
        {
            return new[]
            {
                "" + GetOrderTime,
                "" + UserName,
                "" + PhoneNO,
                "" + Address,
                "" + Name,
                "" + Created,
                "" + ID,
                "" + Format,
                "" + Price,
                "" + Count
            };
        }
    }
}
