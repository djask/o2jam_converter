using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using O2JamUtils;

namespace O2JamDebug
{
    class Program
    {

        public static bool show_help { get; set; } = false;
        public static string input { get; set; } = null;
        public static string output { get; set; } = null;
        public static Boolean zipOSZ { get; set; } = false;
        public static Boolean use_ffmpeg { get; set; } = false;

        static void Main(string[] args)
        {
            var p = new NDesk.Options.OptionSet() {
                { "i|input=", "the input directory",
                    v => input = v },
                { "o|output=", "output beatmaps folder",
                    v => output = v },
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

            if (Path.GetExtension(input) != ".ojn")
            {
                Console.WriteLine("The file you specified doesn't seem to be an ojn file");
            }
            else
            {
                Console.Write($"Processing file {input}... ");
                OsuBeatmap map = new OsuBeatmap();
                String outDir = map.BeatmapDump(input, output, use_ffmpeg);
                if (zipOSZ) Helpers.ZipDir(outDir, ".osz");
                Console.Write("Done");
            }
        }
    }
}
