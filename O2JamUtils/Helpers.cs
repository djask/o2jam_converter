using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace O2JamUtils
{
    public class Helpers
    {
        public static class HighResolutionDateTime
        {
            public static bool IsAvailable { get; private set; }

            [DllImport("Kernel32.dll", CallingConvention = CallingConvention.Winapi)]
            private static extern void GetSystemTimePreciseAsFileTime(out long filetime);

            public static DateTime UtcNow
            {
                get
                {
                    if (!IsAvailable)
                    {
                        throw new InvalidOperationException(
                            "High resolution clock isn't available.");
                    }

                    long filetime;
                    GetSystemTimePreciseAsFileTime(out filetime);

                    return DateTime.FromFileTimeUtc(filetime);
                }
            }

            public static long timenow
            {
                get
                {
                    if (!IsAvailable)
                    {
                        throw new InvalidOperationException(
                            "High resolution clock isn't available.");
                    }

                    long filetime;
                    GetSystemTimePreciseAsFileTime(out filetime);

                    return filetime / 10000;
                }
            }

            static HighResolutionDateTime()
            {
                try
                {
                    long filetime;
                    GetSystemTimePreciseAsFileTime(out filetime);
                    IsAvailable = true;
                }
                catch (EntryPointNotFoundException)
                {
                    // Not running Windows 8 or higher.
                    IsAvailable = false;
                }
            }
        }
        //safer memory mapping i guess...
        public static MemoryMappedFile MemFile(string path)
        {
            return MemoryMappedFile.CreateFromFile(
                      File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read),
                      //not mapping to a name
                      null,
                      //use the file's actual size
                      0L,
                      //read only access
                      MemoryMappedFileAccess.Read,
                      //not configuring security
                      null,
                      //adjust as needed
                      HandleInheritability.None,
                      //close the previously passed in stream when done
                      false);

        }

        public static string GetSafeFilename(string filename)
        {
            return string.Join("_", filename.Split(Path.GetInvalidFileNameChars()));
        }

        public static void ZipDir(String path, String ext)
        {
            //get the ojm path, we assume it is in the same directory
            string beatmapName = Path.GetFileName(path) + ext;
            DirectoryInfo beatmapParentFolder = Directory.GetParent(path);
            string outputName = Path.Combine(beatmapParentFolder.FullName, beatmapName);
            if (File.Exists(outputName)) File.Delete(outputName);
            ZipFile.CreateFromDirectory(path, outputName);
            Directory.Delete(path, true);
        }
    }
}
