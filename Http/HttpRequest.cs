﻿using GetWebPageDate.Util;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GetWebPageDate.Http
{
    public class HttpRequest
    {
        private const int timeOut = 6;

        private CookieContainer cookieContainer;

        private string cookie = "ASP.NET_SessionId=kamm45hqgxdwpw5jcbxbhkwo; loginCookie=loginKey=ee36bJu8GIYjT03Nighq8A==&loginKeyMi=iUSyl9FWQnL77cKt+HVhlepcJ";

        public CookieContainer CookieContainer
        {
            get { return cookieContainer; }
            set { cookieContainer = value; }
        }

        public string Cookie
        {
            get { return cookie; }
            set { cookie = value; }
        }

        private static bool CheckValidationResult(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
        {
            return true; //总是接受
        }

        public string HttpPost(string url, string postDataStr, Dictionary<string, string> heads = null)
        {
            return HttpPost(url, postDataStr, null, null, null, heads);
        }

        public string HttpPost(string url, string postDataStr, ref string reUri, Dictionary<string, string> heads = null)
        {
            return HttpPost(url, postDataStr, null, null, null, ref reUri, heads);
        }

        public string HttpPost(string url, string postDataStr, string referfer, string accept, Encoding encoding, Dictionary<string, string> heads = null)
        {
            string reUri = "";
            return HttpPost(url, postDataStr, referfer, accept, encoding, ref reUri, heads);
        }

        public string HttpPost(string url, string postDataStr, string referfer, string accept, Encoding encoding, ref string reUri, Dictionary<string, string> heads)
        {
            int runCount = 0;
            do
            {
                try
                {
                    HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
                    if (url.StartsWith("https", StringComparison.OrdinalIgnoreCase))
                    {
                        ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(CheckValidationResult);
                        request.ProtocolVersion = HttpVersion.Version11;
                        // 这里设置了协议类型。
                        ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;// SecurityProtocolType.Tls1.2; 
                        request.KeepAlive = false;
                        ServicePointManager.CheckCertificateRevocationList = true;
                        ServicePointManager.DefaultConnectionLimit = 100;
                        ServicePointManager.Expect100Continue = false;
                    }

                    request.Method = "POST";
                    request.KeepAlive = true;
                    request.Referer = null;
                    request.AllowAutoRedirect = false;
                    request.UserAgent = "Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 5.2; .NET CLR 1.1.4322; .NET CLR 2.0.50727)";
                    request.Accept = "*/*";
                    //request.Host = "admin.tkyfw.com";
                    request.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";

                    if (!string.IsNullOrEmpty(accept))
                    {
                        request.Accept = accept;
                    }

                    if (!string.IsNullOrEmpty(referfer))
                    {
                        request.Referer = referfer;
                    }

                    //request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/51.0.2704.79 Safari/537.36 Edge/14.14393";

                    if (cookieContainer != null)
                    {
                        request.CookieContainer = cookieContainer;
                    }
                    else
                    {
                        request.Headers.Add("Cookie", cookie);
                    }

                    AddHeads(heads, request);

                    if (!string.IsNullOrEmpty(postDataStr))
                    {
                        byte[] postData = Encoding.UTF8.GetBytes(postDataStr);

                        request.ContentLength = postData.Length;

                        using (Stream outputStream = request.GetRequestStream())
                        {
                            outputStream.Write(postData, 0, postData.Length);
                        }
                    }
                    string content;
                    using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                    {
                        StreamReader reader;

                        Stream responseStream = response.GetResponseStream();

                        reader = new System.IO.StreamReader(responseStream, encoding == null ? Encoding.UTF8 : encoding);

                        content = reader.ReadToEnd();
                        reUri = response.ResponseUri.AbsoluteUri;
                        responseStream.Dispose();
                        response.Dispose();
                        request.Abort();
                    }


                    return content;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    Console.WriteLine("Url:{0}, postDataStr:{1}, referfer:{2}, accept:{3}, reUri:{4}", url, postDataStr, string.IsNullOrEmpty(referfer) ? "null" : referfer, string.IsNullOrEmpty(accept) ? "" : accept, string.IsNullOrEmpty(reUri) ? "null" : reUri);
                    int sleepTime = 5 * 1000;
                    if (ex.ToString().Contains("无法连接"))
                    {
                        sleepTime = 30 * 1000;
                    }
                    Thread.Sleep(sleepTime);
                    if (ex.ToString().Contains("404") || runCount++ > 3)
                    {
                        return null;
                    }
                }
            } while (true);
        }

        public string GetLogin(string url, string dataStr, Encoding encoding = null)
        {
            try
            {
                if (!string.IsNullOrEmpty(dataStr))
                {
                    url += "?" + dataStr;
                }
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                CookieContainer cookies = new CookieContainer();

                request.ContentType = "text/html;charset=UTF-8";
                request.Method = "GET";
                request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/59.0.3071.115 Safari/537.36";
                request.KeepAlive = true;
                request.Accept = "*/*";
                request.Referer = "http://www.hyey.cn/Store/index.htm";
                request.CookieContainer = cookies;
                //request.Headers.Add("Accept-Encoding", "gzip, deflate");
                //request.Headers.Add("Accept-Language", "zh-CN,zh;q=0.8");
                //request.Headers.Add("Accept-Charset", "GBK,utf-8;q=0.7,*;q=0.3");
                //request.Headers.Add("Host", "www.hyey.cn");
                //request.CookieContainer = myCookieContainer;
                //            request.Headers.Add("Cookie", "	__jsluid=461a853c4fabb30826b6e52419570ef1"
                //+ "UM_distinctid=1671133d9f429-07f92019831a7a-5c11301c-1fa400-1671133d9f56"
                //+ "HistoryMedicine=9154300|6231990|3825429|3954845|11906090|3985362|4037712"
                //+ "real_ip=219.137.142.51"
                //+ "Hm_lvt_e5f454eb1aa8e839f8845470af4667eb=1546673024"
                //+ "hotkeywords=%E8%A1%A5%E8%A1%80%23%231%23%23wwwsearch%23%23%24%23%23search.html%23%23keyword%3D%25e8%25a1%25a5%25e8%25a1%2580%40%40999%23%230%23%23other_https%3A%2F%2Fwww.yaofangwang.com%2Fsearch%2F13791.html%40%40%E7%89%87%E4%BB%94%E7%99%80%23%230%23%23other_https%3A%2F%2Fwww.yaofangwang.com%2Fsearch%2F39735.html%40%40%E5%A6%88%E5%AF%8C%E9%9A%86%23%230%23%23wwwsearch%23%23%24%23%23search.html%23%23keyword%3D%25e5%25a6%2588%25e5%25af%258c%25e9%259a%2586%40%40%E9%98%BF%E8%83%B6%23%231%23%23other_https%3A%2F%2Fwww.yaofangwang.com%2Fsearch%2F11442.html%40%40%E9%87%91%E6%88%88%23%230%23%23other_https%3A%2F%2Fwww.yaofangwang.com%2Fsearch%2F30642.html%40%40%E6%B1%A4%E8%87%A3%E5%80%8D%E5%81%A5%23%230%23%23other_https%3A%2F%2Fwww.yaofangwang.com%2Fsearch%2F50493.html"
                //+ "__RequestVerificationToken=Z_ElEcjuJBuCSy5ZIUdQZPQ92GYVe-XsFmlRsGRwgXxIlslu8OC6dGAzJ5iEt7X4v4eMHCLmd__DYWFqHixzk_Zpoc8cGlWJkMBwtCqDxzs1"
                //+ "ASP.NET_SessionId=dbqivtdhkd3n2o1frxyb4f3t"
                //+ "historysearch="
                //+ "logined_username=BSK777"
                //+ "CNZZDATA1261831897=1407370989-1546668388-https%253A%252F%252Fwww.yaofangwang.com%252F%7C1546679190"
                //+ "Hm_lpvt_e5f454eb1aa8e839f8845470af4667eb=1546681415");

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream myResponseStream = response.GetResponseStream();

                StreamReader myStreamReader = new StreamReader(myResponseStream, encoding == null ? Encoding.GetEncoding("utf-8") : encoding);
                string retString = myStreamReader.ReadToEnd();
                cookies.Add(response.Cookies);
                myStreamReader.Close();
                myResponseStream.Close();

                CookieContainer = cookies;

                return retString;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return null;
        }

        private void AddHeads(Dictionary<string, string> heads, HttpWebRequest request)
        {
            if (heads != null)
            {
                foreach (KeyValuePair<string, string> head in heads)
                {
                    if (head.Key == "Date")
                    {
                        request.Date = Convert.ToDateTime(head.Value);
                    }
                    else if (head.Key == "Content-Type")
                    {
                        request.ContentType = head.Value;
                    }
                    else if (head.Key == "User-Agent")
                    {
                        request.UserAgent = head.Value;
                    }
                    else if (head.Key == "Accept")
                    {
                        request.Accept = head.Value;
                    }
                    else
                    {
                        request.Headers.Add(head.Key, head.Value);
                    }
                }
            }
        }

        public string Login(string url, string postDataStr, Dictionary<string, string> heads = null)
        {
            try
            {
                GetWebPageDate.Http.HttpRequest http = new GetWebPageDate.Http.HttpRequest();

                CookieContainer cookies = new CookieContainer();

                byte[] postData = Encoding.UTF8.GetBytes(postDataStr);

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                //ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(CheckValidationResult);
                //request.ProtocolVersion = HttpVersion.Version11;
                // 这里设置了协议类型。
                //ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;// SecurityProtocolType.Tls1.2; 
                // request.Headers.Remove(HttpRequestHeader.Connection);
                request.Headers.Add(HttpRequestHeader.KeepAlive, "TRUE");
                request.Method = "POST";
                request.KeepAlive = true;
                request.Timeout = 1000000;
                // request.Connection = "keep-alive";
                request.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
                request.Referer = "https://reg.yaofangwang.com/login.aspx";
                request.ContentLength = postData.Length;
                //request.UserAgent = "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)";
                request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/63.0.3239.84 Safari/537.36";
                request.CookieContainer = cookies;

                AddHeads(heads, request);

                //                __jsluid=461a853c4fabb30826b6e52419570ef1
                //    UM_distinctid=1671133d9f429-07f92019831a7a-5c11301c-1fa400-1671133d9f56
                //    HistoryMedicine=9154300|6231990|3825429|3954845|11906090|3985362|4037712
                //    real_ip=219.137.142.51
                //    Hm_lvt_e5f454eb1aa8e839f8845470af4667eb=1546673024
                //    hotkeywords=%E8%A1%A5%E8%A1%80%23%231%23%23wwwsearch%23%23%24%23%23search.html%23%23keyword%3D%25e8%25a1%25a5%25e8%25a1%2580%40%40999%23%230%23%23other_https%3A%2F%2Fwww.yaofangwang.com%2Fsearch%2F13791.html%40%40%E7%89%87%E4%BB%94%E7%99%80%23%230%23%23other_https%3A%2F%2Fwww.yaofangwang.com%2Fsearch%2F39735.html%40%40%E5%A6%88%E5%AF%8C%E9%9A%86%23%230%23%23wwwsearch%23%23%24%23%23search.html%23%23keyword%3D%25e5%25a6%2588%25e5%25af%258c%25e9%259a%2586%40%40%E9%98%BF%E8%83%B6%23%231%23%23other_https%3A%2F%2Fwww.yaofangwang.com%2Fsearch%2F11442.html%40%40%E9%87%91%E6%88%88%23%230%23%23other_https%3A%2F%2Fwww.yaofangwang.com%2Fsearch%2F30642.html%40%40%E6%B1%A4%E8%87%A3%E5%80%8D%E5%81%A5%23%230%23%23other_https%3A%2F%2Fwww.yaofangwang.com%2Fsearch%2F50493.html
                //    __RequestVerificationToken=Z_ElEcjuJBuCSy5ZIUdQZPQ92GYVe-XsFmlRsGRwgXxIlslu8OC6dGAzJ5iEt7X4v4eMHCLmd__DYWFqHixzk_Zpoc8cGlWJkMBwtCqDxzs1
                //    ASP.NET_SessionId=dbqivtdhkd3n2o1frxyb4f3t
                //    historysearch=
                //    logined_username=BSK777
                //    CNZZDATA1261831897=1407370989-1546668388-https%253A%252F%252Fwww.yaofangwang.com%252F%7C1546679190
                //    Hm_lpvt_e5f454eb1aa8e839f8845470af4667eb=1546681425
                //                Response sent 94 bytes of Cookie data:
                //    Set-Cookie: real_ip=219.137.142.51; domain=.yaofangwang.com; expires=Sat, 05-Jan-2019 10:43:43 GMT; path=/

                //Response sent 99 bytes of Cookie data:
                //    Set-Cookie: ASP.NET_SessionId=dbqivtdhkd3n2o1frxyb4f3t; domain=/; expires=Fri, 04-Jan-2019 09:43:43 GMT; path=/

                //This response did not contain a P3P Header.

                //Validate P3P Policies at: http://www.w3.org/P3P/validator.html
                //Learn more at: http://fiddler2.com/r/?p3pinfo
                Stream myRequestStream = request.GetRequestStream();
                myRequestStream.Write(postData, 0, postData.Length);
                myRequestStream.Close();

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                Stream myResponseStream = response.GetResponseStream();
                //cookies.Add(response.Cookies);
                StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.GetEncoding("utf-8"));
                string retString = myStreamReader.ReadToEnd();

                myStreamReader.Close();
                myResponseStream.Close();
                cookieContainer = cookies;

                return retString;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return null;
        }

        public string HttpGet(string url, string postDataStr = null, Encoding encoding = null, bool isUseUserAgent = false)
        {
            url += (string.IsNullOrEmpty(postDataStr) ? "" : "?") + postDataStr;

            return HttpGet(url, encoding, isUseUserAgent);
        }

        public string HttpGet(string url, Encoding encoding, bool isUseUserAgent = false, Dictionary<string, string> heads = null)
        {
            int runCount = 0;
            do
            {
                try
                {
                    if (string.IsNullOrEmpty(url))
                    {
                        return "";
                    }

                    if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!url.StartsWith("://", StringComparison.OrdinalIgnoreCase))
                        {
                            if (url.StartsWith("//", StringComparison.OrdinalIgnoreCase))
                            {
                                url = "http:" + url;
                            }
                            else
                            {
                                url = "http://" + url;
                            }
                        }
                        else
                        {
                            url = "http" + url;
                        }
                    }

                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

                    request.ContentType = "text/html;charset=UTF-8";
                    request.Method = "GET";

                    if (isUseUserAgent)
                    {
                        request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/59.0.3071.115 Safari/537.36";
                    }

                    request.KeepAlive = true;
                    //request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8";
                    request.Referer = "http://www.hyey.cn/Store/index.htm";
                    //request.Headers.Add("Accept-Encoding", "gzip, deflate");
                    //request.Headers.Add("Accept-Language", "zh-CN,zh;q=0.8");
                    //request.Headers.Add("Accept-Charset", "GBK,utf-8;q=0.7,*;q=0.3");
                    if (cookieContainer != null)
                    {
                        request.CookieContainer = cookieContainer;
                    }
                    else
                    {
                        request.Headers.Add("Cookie", cookie);
                    }

                    AddHeads(heads, request);

                    //request.Headers.Add("Host", "www.hyey.cn");
                    //request.CookieContainer = myCookieContainer;


                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                    Stream myResponseStream = response.GetResponseStream();

                    StreamReader myStreamReader = new StreamReader(myResponseStream, encoding == null ? Encoding.GetEncoding("utf-8") : encoding);
                    string retString = myStreamReader.ReadToEnd();
                    myStreamReader.Close();
                    myResponseStream.Close();

                    return retString;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error:{0}, Url:{1}", ex.ToString(), url);
                    int sleepTime = 10 * 1000;
                    if (ex.ToString().Contains("无法连接"))
                    {
                        sleepTime = 30 * 1000;
                    }
                    Thread.Sleep(sleepTime);
                    
                    if (ex.ToString().Contains("404") || runCount++ > 3)
                    {
                        return "";
                    }
                }
            } while (true);

        }

        public Image HttpGetPicture(string url)
        {

            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

                request.ContentType = "image/jpeg";
                request.Method = "GET";
                //request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/59.0.3071.115 Safari/537.36";
                request.KeepAlive = true;
                //request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8";
                request.Referer = "http://www.hyey.cn/Store/index.htm";
                //request.Headers.Add("Accept-Encoding", "gzip, deflate");
                //request.Headers.Add("Accept-Language", "zh-CN,zh;q=0.8");
                //request.Headers.Add("Accept-Charset", "GBK,utf-8;q=0.7,*;q=0.3");
                if (cookieContainer != null)
                {
                    request.CookieContainer = cookieContainer;
                }
                else
                {
                    request.Headers.Add("Cookie", cookie);
                }
                //request.Headers.Add("Host", "www.hyey.cn");
                //request.CookieContainer = myCookieContainer;


                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream myResponseStream = response.GetResponseStream();


                Image image = Image.FromStream(myResponseStream);

                myResponseStream.Close();

                return image;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Thread.Sleep(5 * 1000);
                return HttpGetPicture(url);
            }
        }
               
    }
}
