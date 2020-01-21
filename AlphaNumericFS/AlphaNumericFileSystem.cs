using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using FuseSharp;
using Mono.Unix.Native;
using NeoSmart.Unicode;

namespace AlphaNumericFS
{
    public class AlphaNumericFileSystem : FileSystem
    {
        const string AlphaPath = "alpha";
        const string NumericPath = "numeric";
        const string UnicodePath = "unicode";

        private static string AlphaContent = "abcdefghijklmnopqrstuvwxyz\n".Repeat(5);
        private static string NumericContent = "0123456789\n".Repeat(5);

        const int DummyFileDescriptor = 0x2aaaaaaa;

        private HashSet<string> Paths = new HashSet<string>(new string[] { "/", $"/{AlphaPath}", $"/{NumericPath}", $"/{UnicodePath}" });

        public override Errno OnGetPathStatus(string path, out Stat stat)
        {
            Trace.WriteLine($"OnGetPathStatus {path}");

            stat = new Stat();

            long timeNow = 0;
            Syscall.time(out timeNow);

            stat.st_uid = Syscall.getuid();
            stat.st_gid = Syscall.getgid();
            stat.st_atime = timeNow;
            stat.st_mtime = timeNow;

            if (path.StartsWith($"/{UnicodePath}/"))
            {
                var codepoint = path.Replace($"/{UnicodePath}/", "");
                var size = GetUnicodeEndpointDataSize(codepoint);

                stat.st_mode = FilePermissions.S_IFREG | (FilePermissions)Convert.ToInt32("0644", 8);
                stat.st_nlink = 1;
                stat.st_size = size;
            }
            else if (Paths.Contains(path))
            {
                stat.st_mode = FilePermissions.S_IFDIR | (FilePermissions)Convert.ToInt32("0755", 8);
                stat.st_nlink = 2;
            }
            else if (path.StartsWith(UnicodePath))
            {
                stat.st_mode = FilePermissions.S_IFREG | (FilePermissions)Convert.ToInt32("0644", 8);
                stat.st_nlink = 1;
                stat.st_size = 2;
            }
            else if (path.EndsWith("/data"))
            {
                stat.st_mode = FilePermissions.S_IFREG | (FilePermissions)Convert.ToInt32("0644", 8);
                stat.st_nlink = 1;

                if (path.Contains(AlphaPath))
                {
                    stat.st_size = AlphaContent.Length;
                }
                else if (path.Contains(NumericPath))
                {
                    stat.st_size = NumericContent.Length;
                }
                else
                {
                    stat.st_size = 0;
                }
            }
            else
            {
                return Errno.ENOENT;
            }

            return 0;
        }

        public override Errno OnReadDirectory(
            string directory,
            PathInfo info,
            out IEnumerable<DirectoryEntry> paths)
        {
            // Trace.WriteLine($"OnReadDirectory {directory}");

            if (!Paths.Contains(directory))
            {
                paths = null;
                return Errno.ENOENT;
            }

            List<DirectoryEntry> entries = new List<DirectoryEntry>();
            entries.Add(new DirectoryEntry("."));
            entries.Add(new DirectoryEntry(".."));

            if (directory == "/")
            {
                entries.Add(new DirectoryEntry(AlphaPath));
                entries.Add(new DirectoryEntry(NumericPath));
                entries.Add(new DirectoryEntry(UnicodePath));
            }
            else if (directory != UnicodePath)
            {
                entries.Add(new DirectoryEntry("data"));
            }

            paths = entries;

            return 0;
        }

        public override Errno OnOpenHandle(string file, PathInfo info)
        {
            Trace.WriteLine($"OnOpenHandle {file} Flags={info.OpenFlags}");

            info.Handle = new IntPtr(DummyFileDescriptor);
            return 0;
        }

        public override Errno OnReadHandle(
            string file,
            PathInfo info,
            byte[] buf,
            long offset,
            out int bytesRead)
        {
            Trace.WriteLine($"OnReadHandle {file} Flags={info.OpenFlags}");

            string content = null;

            if (file.StartsWith($"/{UnicodePath}"))
            {
                var codepointString = file.Replace("/unicode/", "");
                var cp = new Codepoint(codepointString);
                var codeUnit = cp.AsString() + "\n";
                bytesRead = CopyBuf(codeUnit, buf, codeUnit.Length);
                return 0;
            }
            else if (file.StartsWith($"/{AlphaPath}"))
            {
                content = AlphaContent;
            }
            else if (file.StartsWith($"/{NumericPath}"))
            {
                content = NumericContent;
            }
            else
            {
                bytesRead = -1;
                return 0;
            }

            int toBeReadCount = buf.Length;

            bytesRead = CopyBuf(content, buf, toBeReadCount, (int)offset);

            return 0;
        }

        private unsafe int CopyBuf(string src, byte[] dest, int length, int offset = 0)
        {
            if (offset + length > src.Length)
            {
                length = src.Length - (int)offset;
            }

            fixed (byte* pBuf = dest)
            {
                var charBuf = Encoding.UTF8.GetBytes(src.Substring((int)offset, length));
                for (int i = 0; i < charBuf.Length; i++)
                {
                    pBuf[i] = charBuf[i];
                }

                return charBuf.Length;
            }
        }

        private int GetUnicodeEndpointDataSize(string codepoint)
        {
            var cp = new Codepoint(codepoint);
            var charBuf = Encoding.UTF8.GetBytes(cp.AsString() + "\n");
            return charBuf.Length;
        }
    }

    public static class StringExtensions
    {
        public static string Repeat(this string s, int n)
            => new StringBuilder(s.Length * n).Insert(0, s, n).ToString();
    }
}