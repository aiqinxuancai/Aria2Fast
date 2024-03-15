using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aria2Fast.Service
{
    /// <summary>
    /// 扩展常用下载路径的保存
    /// </summary>
    public partial class AppConfig
    {

        public void InitLocalPath(string dir)
        {
            var dict = ConfigData.AddTaskSavePathList;

            dict[ConfigData.Aria2RpcAuto] = new List<string>
            {
                dir,
                Path.Combine(dir, "movies"),
                Path.Combine(dir, "tvshows")
            };

            dict = ConfigData.AddSubscriptionSavePathList;
            dict[ConfigData.Aria2RpcAuto] = new List<string>
            {
                dir,
                Path.Combine(dir, "movies"),
                Path.Combine(dir, "tvshows")
            };

            Save();

   
        }


        public List<string> GetDownloadPathWithAddTask()
        {
            var dict = ConfigData.AddTaskSavePathList;

            if (dict.ContainsKey(ConfigData.Aria2RpcAuto))
            {
                return dict[ConfigData.Aria2RpcAuto];
            }
            //返回默认的值并写入

            dict[ConfigData.Aria2RpcAuto] = new List<string>
            {
                "/downloads" ,
                "/downloads/movies",
                "/downloads/tvshows"
            };

            Save();

            return dict[ConfigData.Aria2RpcAuto];
        }

        public List<string> GetDownloadPathWithAddSubscription()
        {
            var dict = ConfigData.AddSubscriptionSavePathList;

            if (dict.ContainsKey(ConfigData.Aria2RpcAuto))
            {
                return dict[ConfigData.Aria2RpcAuto];
            }
            //返回默认的值并写入

            dict[ConfigData.Aria2RpcAuto] = new List<string>
            {
                "/downloads" ,
                "/downloads/movies",
                "/downloads/tvshows"
            };

            Save();

            return dict[ConfigData.Aria2RpcAuto];
        }

        public void SaveDownloadPathWithAddTask(string path)
        {
            var dict = ConfigData.AddTaskSavePathList;

            if (!dict.ContainsKey(ConfigData.Aria2RpcAuto))
            {
                dict[ConfigData.Aria2RpcAuto] = new List<string>();
            }
            var list = dict[ConfigData.Aria2RpcAuto];

            var hasValue = list.Any(a => a == path);
            if (hasValue)
            {
                list.Remove(path);
            }
            if (list.Count > 8)
            {
                list.RemoveAt(list.Count - 1);
            }

            list.Insert(0, path); 
            Save();
        }

        public void SaveDownloadPathWithAddSubscription(string path)
        {
            var dict = ConfigData.AddSubscriptionSavePathList;

            if (!dict.ContainsKey(ConfigData.Aria2RpcAuto))
            {
                dict[ConfigData.Aria2RpcAuto] = new List<string>();
            }
            var list = dict[ConfigData.Aria2RpcAuto];

            var hasValue = list.Any(a => a == path);
            if (hasValue)
            {
                list.Remove(path);
            }
            if (list.Count > 8)
            {
                list.RemoveAt(list.Count - 1);
            }

            list.Insert(0, path);
            Save();
        }
    }
}
