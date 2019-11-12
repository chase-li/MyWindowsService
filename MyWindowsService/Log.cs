using System;
using System.Collections.Generic;
using System.IO;
using System.Web;

namespace WindowsService
{
    public class Log
    {
        static private string logpathlog = AppDomain.CurrentDomain.BaseDirectory + "log.txt";

        static public void writelog(string classname)
        {
            string path = logpathlog;
            if (!File.Exists(path))
            {
                // Create a file to write to.
                using (File.Create(path)) { }
            }

            FileInfo fileinfo = new FileInfo(path);
            if (fileinfo.Length > 1024 * 1024 * 2)
            {
                File.Move(path, AppDomain.CurrentDomain.BaseDirectory + DateTime.Now.ToString("yyyyMMddHHmmss") + "log.txt");

                if (!File.Exists(path))
                {
                    using (File.Create(path)) { }
                }

            }

            using (StreamWriter sw = File.AppendText(path))
            {
                sw.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "：" + "\t\n");
                sw.WriteLine(classname + "\t\n");
                sw.WriteLine("------------------------------------------------------------------------" + "\t\n");
                sw.Close();
            }

        }

        static public void SetException(Exception e)
        {
            writelog(e.ToString());
        }

        static public void SetString(string log)
        {
            writelog(log);
        }

    }
}