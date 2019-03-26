using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GetWebPageDate.Util.Item
{
    public class YFGreenOrderInfo : BaseItemInfo
    {
        /// <summary>
        /// 订单号
        /// </summary>
        public string OrderNO {get;set;}

        /// <summary>
        /// 订购时间
        /// </summary>
        public string CreateOrderTime { get; set; }

        /// <summary>
        /// 订单总额
        /// </summary>
        public string TotalPrice { get; set; }

        /// <summary>
        /// 地址
        /// </summary>
        public string ReceiverAddress { get; set; }

        /// <summary>
        /// 收件人
        /// </summary>
        public string ReceiverName { get; set; }

        /// <summary>
        /// 电话
        /// </summary>
        public string ReceiverPhoneNumber { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        public string Remark { get; set; }


        public override string GetLogHeadLine()
        {
            return "订单编号,订购时间,名称,国药准字,规格,价格,数量,订单总额,收件人,地址,电话,备注";
        }

        public override string[] GetLogStrArr()
        {
            return new[]
            {
                "" + OrderNO,
                "" + CreateOrderTime,
                "" + Name,
                "" + ID,
                "" + Format,
                "" + ShopPrice,
                "" + Inventory,
                "" + TotalPrice,
                "" + ReceiverName,
                "" + ReceiverAddress,
                "" + ReceiverPhoneNumber,
                "" + Remark,
            };
        }
    }
}
