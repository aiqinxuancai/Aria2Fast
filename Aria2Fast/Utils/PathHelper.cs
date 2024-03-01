using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Aria2Fast.Utils
{
    internal class PathHelper
    {
        public static string RemoveInvalidChars(string input)
        {
            var invalidFileNameChars = Path.GetInvalidFileNameChars();
            var invalidPathChars = Path.GetInvalidPathChars();

            foreach (var c in invalidFileNameChars.Union(invalidPathChars).Distinct())
            {
                input = input.Replace(c.ToString(), "");
            }

            return input;
        }

        public static void WriteResourceToFile(string resourceName, string outputFilePath)
        {
            // 获取调用者的程序集, 你可能需要根据实际情况获取其他程序集
            Assembly assembly = Assembly.GetExecutingAssembly();

            // 使用资源的全名打开流，resourcesName 通常以 “命名空间.文件名” 的形式存在
            using (Stream resourceStream = assembly.GetManifestResourceStream(resourceName))
            {
                if (resourceStream == null)
                {
                    throw new ArgumentException("无法找到指定的资源。", nameof(resourceName));
                }

                // 创建文件流用于将资源写出到文件
                using (FileStream outputFileStream = new FileStream(outputFilePath, FileMode.Create))
                {
                    resourceStream.CopyTo(outputFileStream);
                }
            }
        }
    }
}
