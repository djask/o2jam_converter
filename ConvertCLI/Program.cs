using O2JamUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace O2JamDebug
{
    class Program
    {

        public static bool show_help { get; set; } = false;
        public static string input { get; set; } = null;
        public static string output { get; set; } = null;
        public static bool zipOSZ { get; set; } = false;
        public static bool use_ffmpeg { get; set; } = false;
        public static bool fmod_flag { get; set; } = false;

        public enum Mode
        {
            Convert,
            OJNLog
        }

        public static void ConvertFile(string input)
        {
            FileAttributes attr = File.GetAttributes(input);

            if (Path.GetExtension(input) != ".ojn")
            {
                Console.WriteLine("The file you specified doesn't seem to be an ojn file");
            }
            else
            {
                Console.Write($"Processing file {input}... ");
                OsuBeatmap map = new OsuBeatmap(input, fmod_flag, use_ffmpeg);
                String outDir = map.BeatmapDump(output);
                if (outDir != null && zipOSZ) Helpers.ZipDir(outDir, ".osz");
                Console.WriteLine("Done");
            }
        }

        static void Main(string[] args)
        {
            if (args.Length == 0) show_help = true;

            Mode mode = Mode.Convert;
            var p = new NDesk.Options.OptionSet() {
                { "d|dump-ojn-list=", "generate a file of songs in directory",
                    v=> {if (v != null) mode = Mode.OJNLog; } },
                { "i|input=", "the input directory",
                    v => input = v },
                { "o|output=", "output beatmaps folder",
                    v => output = v },
                { "k|keysound.", "use fmod to render",
                    v=> {if (v != null) fmod_flag = true; } },
                { "f|useffmpeg", "use ffmpeg to encode mp3",
                    v=> {if (v != null) use_ffmpeg = true; } },
                { "z|ziposz.", "zip the contents at the end",
                    v=> {if (v != null) zipOSZ = true; } },
                { "h|help",  "show this message and exit",
                    v => show_help = v != null },
                };

            List<string> extra;
            try
            {
                extra = p.Parse(args);
            }
            catch (NDesk.Options.OptionException e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("Try `O2JamDebug --help' for more information.");
                return;
            }

            if (show_help)
            {
                p.WriteOptionDescriptions(Console.Out);
                return;
            }

            if (input != null)
            {
            }

            switch (mode)
            {
                case Mode.Convert:
                    try
                    {
                        System.IO.Directory.CreateDirectory(output);
                    }
                    catch
                    {
                        Console.WriteLine("Error creating or finding output directory, please check your arguments");
                        Console.ReadKey();
                        return;
                    }
                    FileAttributes attr = File.GetAttributes(input);
                    if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                    {
                        Console.WriteLine("Directory Detected...");
                        DirectoryInfo d = new DirectoryInfo(input);
                        FileInfo[] Files = d.GetFiles("*.ojn");
                        foreach (var file in Files)
                        {
                            try
                            {
                                ConvertFile(file.FullName);
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine($"Error, unhandled {e.Message}");
                                continue;
                            }
                        }
                    }
                    else
                    {
                        ConvertFile(input);
                    }
                    break;
                case Mode.OJNLog:
                    throw new System.NotImplementedException();
                default:
                    break;
            }
        }
    }
}
