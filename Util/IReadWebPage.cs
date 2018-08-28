using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GetWebPageDate.Util
{
    public interface IReadWebPage
    {
        void ReadAllMenuURL(string url, string data, string cookie);

        void ReadAllItem();

    }
}
