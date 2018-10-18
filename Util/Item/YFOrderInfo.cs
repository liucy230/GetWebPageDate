using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GetWebPageDate.Util.Item
{
    public class YFOrderInfo : BaseItemInfo
    {
        /// <summary>
        /// 发件人姓名
        /// </summary>
        public string SenderName { get; set; }

        /// <summary>
        /// 发件人地址
        /// </summary>
        public string SenderAddress { get; set; }

        /// <summary>
        /// 发件人公司名称
        /// </summary>
        public string SenderCompany { get; set; }

        /// <summary>
        /// 发件人电话
        /// </summary>
        public string SenderPhoneNumber { get; set; }

        /// <summary>
        /// 发件人邮编
        /// </summary>
        public string SenderZipCode { get; set; }

        /// <summary>
        /// 收件人姓名
        /// </summary>
        public string ReceiverName { get; set; }

        /// <summary>
        /// 收件人地址
        /// </summary>
        public string ReceiverAddress { get; set; }

        /// <summary>
        /// 收件人公司名称
        /// </summary>
        public string ReceiverCompany { get; set; }

        /// <summary>
        /// 收件人电话
        /// </summary>
        public string ReceiverPhoneNumber { get; set; }

        /// <summary>
        /// 收件人邮编
        /// </summary>
        public string ReceiverZipCode { get; set; }

        /// <summary>
        /// 买家ID昵称
        /// </summary>
        public string BuyerId { get; set; }

        /// <summary>
        /// 快递公司
        /// </summary>
        public string ExpressName { get; set; }

        /// <summary>
        /// 快递模板
        /// </summary>
        public string ExpressTemplate { get; set; }

        /// <summary>
        /// 快递单号
        /// </summary>
        public string ExpressNO { get; set; }

        /// <summary>
        /// 打印时间
        /// </summary>
        public string PrintTime { get; set; }

        public override string GetLogHeadLine()
        {
            return "发件人姓名,发件人地址,发件人公司名称,发件人电话,发件人邮编,收件人姓名,收件人地址,收件人公司名称,收件人电话,收件人邮编,买家ID昵称,商品名称,快递公司,快递模板,快递单号,打印时间,我的备注";
        }

        public override string[] GetLogStrArr()
        {
            return new[]
            {
                "" + SenderName,
                "" + SenderAddress,
                "" + SenderCompany,
                "" + SenderPhoneNumber,
                "" + SenderZipCode,
                "" + ReceiverName,
                "" + ReceiverAddress,
                "" + ReceiverCompany,
                "" + ReceiverPhoneNumber,
                "" + ReceiverZipCode,
                "" + BuyerId,
                "" + ItemName,
                "" + ExpressName,
                "" + ExpressTemplate,
                "" + ExpressNO,
                "" + PrintTime,
                "" + Remark,
            };
        }
    }
}
