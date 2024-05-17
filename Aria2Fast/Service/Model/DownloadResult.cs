using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aria2Fast.Service.Model
{
    public class Aria2FastDownloadResult
    {
        //返回值非0或网络请求有异常
        public bool IsSuccessed { get; set; }

        public string ErrorMessage { get; set; }

        public string Gid { get; set; }


        public string InfoHash { get; set; }
        
    }
}
