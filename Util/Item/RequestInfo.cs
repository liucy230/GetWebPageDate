using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GetWebPageDate.Util.Item
{
    public class RequestInfo
    {
        private List<string> contentes = new List<string>();

        /// <summary>
        /// 请求的所有类容
        /// </summary>
        public List<string> Contentes
        {
            get { return contentes; }
            set { contentes = value; }
        }

        private int requestCount = -1;

        /// <summary>
        /// 请求的次数
        /// </summary>
        public int RequestCount
        {
            get { return requestCount; }
            set { requestCount = value; }
        }

        private Dictionary<string, string> requestParams = new Dictionary<string, string>();

        /// <summary>
        /// 请求的参数
        /// </summary>
        public Dictionary<string, string> RequestParams
        {
            get { return requestParams; }
            set { requestParams = value; }
        }

        private List<string> urls = new List<string>();

        /// <summary>
        /// 请求的url
        /// </summary>
        public List<string> Urls
        {
            get { return urls; }
            set { urls = value; }
        }

        private List<string> postDatas;

        public List<string> PostDatas
        {
            get { return postDatas; }
            set { postDatas = value; }
        }

        public int GetIndex()
        {
            return Interlocked.Increment(ref requestCount);
        }


        public string GetParam(string name)
        {
            if (requestParams.ContainsKey(name))
            {
                return requestParams[name];
            }

            return null;
        }

        private bool isFinshed;
        public bool IsFinshed
        {
            get { return isFinshed; }
            set { isFinshed = value; }
        }

        private bool isUseUserAgent;

        public bool IsUseUserAgent
        {
            get { return isUseUserAgent; }
            set { isUseUserAgent = value; }
        }
    }
}
