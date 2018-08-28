using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GetWebPageDate.Util
{
    public interface IReadWebPage
    {

        void Login();

        void ReadAllMenuURL();

        void ReadAllItem();

    }
}
