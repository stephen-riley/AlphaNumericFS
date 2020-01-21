using System;
using System.Diagnostics;
using System.IO;
using FuseSharp;

namespace AlphaNumericFS
{
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.Error.WriteLine("Usage: AlphaNumericFS <mount point>");
                return -1;
            }

            AppDomain.CurrentDomain.UnhandledException +=
                new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            string mountPoint;
            if (!Directory.Exists(mountPoint = Path.GetFullPath(args[0])))
                Directory.CreateDirectory(mountPoint);

            Console.WriteLine("Mount point:{0}", mountPoint);

            string[] actualArgs = { "-s", "-f", mountPoint };

            int status = -1;

            using (FileSystem fs = new AlphaNumericFileSystem())
            using (FileSystemHandler fsh = new FileSystemHandler(fs, actualArgs, OperationsFlags.All))
            {
                status = fsh.Start();
            }

            Console.WriteLine(status);
            return status;
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Console.WriteLine((e.ExceptionObject as Exception).Message);
            Debug.WriteLine((e.ExceptionObject as Exception).Message);
        }
    }
}