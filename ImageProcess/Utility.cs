using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace ImageProcess
{
    public class Utility
    {
        public static void ForeachFile(string srcDir, string dstDir, System.Action<string, string> action)
        {
            if(dstDir.Contains('\\'))
                dstDir = dstDir.Replace('\\','/');
            if (!dstDir.EndsWith("/"))
                dstDir += '/';

            DirectoryInfo dir = new DirectoryInfo(srcDir);
            FileInfo[] files = dir.GetFiles("*.*", SearchOption.TopDirectoryOnly);
            for (int i = 0; i < files.Length; ++i)
            {
                FileInfo file = files[i];
                action(file.FullName, dstDir + file.Name);
            }
        }
    }
}
