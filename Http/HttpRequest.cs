using GetWebPageDate.Util;
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
                            else
                            {
                                request.Headers.Add(head.Key, head.Value);
                            }
                        }
                    }

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
                    if (ex.ToString().Contains("404") || ex.ToString().Contains("指定的值含有无效的控制字符") || ex.ToString().Contains("(500)") || ex.ToString().Contains("无法处理从 HTTP/HTTPS 协议到其他不同协议的重定向"))
                    {
                        return null;
                    }
                }
            } while (true);
        }

        public void GetLogin(string url, string dataStr, Encoding encoding = null)
        {
            try
            {
                url += "?" + dataStr;
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


                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream myResponseStream = response.GetResponseStream();

                StreamReader myStreamReader = new StreamReader(myResponseStream, encoding == null ? Encoding.GetEncoding("utf-8") : encoding);
                string retString = myStreamReader.ReadToEnd();
                cookies.Add(response.Cookies);
                myStreamReader.Close();
                myResponseStream.Close();

                CookieContainer = cookies;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public void Login(string url, string postDataStr)
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
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public string HttpGet(string url, string postDataStr = null, Encoding encoding = null, bool isUseUserAgent = false)
        {
            url += (string.IsNullOrEmpty(postDataStr) ? "" : "?") + postDataStr;

            return HttpGet(url, encoding, isUseUserAgent);
        }

        public string HttpGet(string url, Encoding encoding, bool isUseUserAgent = false, Dictionary<string, string> heads = null)
        {
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

                    if (heads != null)
                    {
                        foreach (KeyValuePair<string, string> head in heads)
                        {
                            if (head.Key == "Date")
                            {
                                request.Date = Convert.ToDateTime(head.Value);
                            }
                            else if (head.Key == "Referer")
                            {
                                request.Referer = head.Value;
                            }
                            else
                            {
                                request.Headers.Add(head.Key, head.Value);
                            }
                        }
                    }

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
                    if (ex.ToString().Contains("404"))
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

        public string HttpGetPlatform(string url)
        {
            do
            {
                try
                {
                    // string platformCookie = "__jsluid=e6b4a4a0ada8311a314c61296436218d; UM_distinctid=15d3f14bef16b9-0deb7c5e344e91-333f5902-1fa400-15d3f14bef2e60; ASP.NET_SessionId=3zela4n0kxujjtk4t4bzy3rv; isContact=0; HistoryMedicine=4618674; hotkeywords=%E6%8B%9C%E5%94%90%E8%8B%B9%23%231%23%23med%23%23%24%23%23medicine-188444.html%23%23%24%40%40%E8%A1%A5%E8%A1%80%23%231%23%23wwwsearch%23%23%24%23%23search.html%23%23keyword%3D%25e8%25a1%25a5%25e8%25a1%2580%40%40999%23%230%23%23other_http%3A%2F%2Fwww.yaofangwang.com%2Fsearch%2F13791.html%40%40%E7%89%87%E4%BB%94%E7%99%80%23%231%23%23other_http%3A%2F%2Fwww.yaofangwang.com%2Fsearch%2F39735.html%40%40%E5%A6%88%E5%AF%8C%E9%9A%86%23%230%23%23wwwsearch%23%23%24%23%23search.html%23%23keyword%3D%25e5%25a6%2588%25e5%25af%258c%25e9%259a%2586%40%40%E9%98%BF%E8%83%B6%23%231%23%23other_http%3A%2F%2Fwww.yaofangwang.com%2Fsearch%2F11442.html%40%40%E9%87%91%E6%88%88%23%230%23%23other_http%3A%2F%2Fwww.yaofangwang.com%2Fsearch%2F30642.html%40%40%E6%B1%A4%E8%87%A3%E5%80%8D%E5%81%A5%23%230%23%23other_http%3A%2F%2Fwww.yaofangwang.com%2Fsearch%2F50493.html; bottomApp_close=1; cartcount=0; subcatalogflag=1; historysearch=%E8%BE%BE%E5%96%9C%20%E9%93%9D%E7%A2%B3%E9%85%B8%E9%95%81%E7%89%87%20-%20%E6%8B%9C%E8%80%B3%E5%8C%BB%E8%8D%AF%7C%7C%E5%90%97%E4%B8%81%E5%95%89%20%E5%A4%9A%E6%BD%98%E7%AB%8B%E9%85%AE%E7%89%87%20-%20%E8%A5%BF%E5%AE%89%E6%9D%A8%E6%A3%AE%7C%7C999%20%E4%B8%89%E4%B9%9D%E8%83%83%E6%B3%B0%E9%A2%97%E7%B2%92%20-%20%E5%8D%8E%E6%B6%A6%E4%B8%89%E4%B9%9D%7C%7C%E8%83%83%E8%82%A0%E7%94%A8%E8%8D%AF%7C%7C%E4%B8%AD%E8%A5%BF%E8%8D%AF%E5%93%81%7C%7CZ43020350%7C%7CZ43020350%2050*50%7C%7C; topnavflag=1; CNZZDATA1261831897=1674121359-1499998668-%7C1500036473; Hm_lvt_e5f454eb1aa8e839f8845470af4667eb=1500001911; Hm_lpvt_e5f454eb1aa8e839f8845470af4667eb=1500040568";

                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

                    request.ContentType = "text/html;charset=UTF-8";
                    request.Method = "GET";
                    request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/59.0.3071.115 Safari/537.36";
                    request.KeepAlive = true;
                    //request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8";
                    //request.Referer = "http://www.yaofangwang.com";
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

                    StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.GetEncoding("utf-8"));
                    string retString = myStreamReader.ReadToEnd();
                    myStreamReader.Close();
                    myResponseStream.Close();

                    return retString;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("url:{0}, error:{1}", url, ex);
                    int sleepTime = 5 * 1000;
                    if (ex.ToString().Contains("无法连接"))
                    {
                        sleepTime = 30 * 1000;
                    }
                    Thread.Sleep(sleepTime);
                    if (ex.ToString().Contains("404") || ex.ToString().Contains("500"))
                    {
                        return "";
                    }
                }
            } while (true);
        }
    }
}
