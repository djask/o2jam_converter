using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace O2JamUtils
{
    public class OsuBeatmap
    {
        private enum Diff
        {
            EX,
            NX,
            HX
        };

        private enum Sig
        {
            M30,
            OJM
        };

        //timings for bpm changes plus their ms markings for osu format
        private class MSTiming
        {
            public float MsMarking { get; set; }
            public float MsPerMeasure { get; set; }
            public float MeasureStart { get; set; }
        }

        private class OsuNote
        {
            public bool LN { get; set; }
            public int Channel { get; set; }
            public float MsStart { get; set; }
            public float MsEnd = -1;
            public int RefID { get; set; }
        }

        private class SampleTiming
        {
            public float MeasureStart { get; set; }
        }

        private class TimingComparer : IComparer<MSTiming>
        {
            public int Compare(MSTiming x, MSTiming y)
            {
                return x.MeasureStart.CompareTo(y.MeasureStart);
            }
        }

        private Boolean virtual_mode;

        private List<MSTiming> OsuTimings { get; set; } = new List<MSTiming>();
        private List<OsuNote> OsuNotes { get; set; } = new List<OsuNote>();
        private List<OsuNote> OsuSamples { get; set; } = new List<OsuNote>();

        //dumps file contents and returns the new directory with the contents
        public string BeatmapDump(string ojn_path, string out_dir, bool ffmpeg)
        {
            //read ojn headers
            OJNData ojnHeader = new OJNData(ojn_path);

            //output folder for beatmap
            string folderName = $"{ojnHeader.SongID} {ojnHeader.Artist} - {ojnHeader.Title}";
            folderName = Helpers.GetSafeFilename(folderName);
            string outFolder = Path.Combine(out_dir, folderName);
            DirectoryInfo directory = Directory.CreateDirectory(outFolder);

            //get the ojm path, we assume it is in the same directory
            DirectoryInfo o2jFolder = Directory.GetParent(ojn_path);
            string ojmPath = Path.Combine(o2jFolder.FullName, $"{Path.GetFileNameWithoutExtension(ojn_path)}.ojm");

            NotePackage.Chart chart = ojnHeader.DumpHXPackage();
            int no_samples = chart.Notes.GroupBy(x => x.Value).Count();
            virtual_mode = no_samples > 10 ? true : false;

            //dump image
            ojnHeader.DumpImage(outFolder);

            //write HX
            CreateDiff(outFolder, ojnHeader, Diff.HX, ffmpeg);

            //write NX
            if (ojnHeader.level[1] < ojnHeader.level[2])
                CreateDiff(outFolder, ojnHeader, Diff.NX, ffmpeg);

            //write EX
            if (ojnHeader.level[0] < ojnHeader.level[1] || ojnHeader.level[0] < ojnHeader.level[2])
                CreateDiff(outFolder, ojnHeader, Diff.EX, ffmpeg);

            if (!virtual_mode)
            {
                FMODSystem fmod_sys = new FMODSystem(false);
                fmod_sys.LoadSamples(ojmPath, false);

                RenderToFile(fmod_sys, ojnHeader.time[2]);
                fmod_sys.ReleaseSystem();
                if (ffmpeg)
                {
                    try
                    {
                        var p = new Process();
                        p.StartInfo = new ProcessStartInfo("ffmpeg.exe", $"-i fmodoutput.wav -f mp3 audio.mp3")
                        {
                            UseShellExecute = false
                        };

                        p.Start();
                        p.WaitForExit();

                        File.Move("audio.mp3", Path.Combine(outFolder, "audio.mp3"));
                    }
                    catch (Win32Exception)
                    {
                        Console.WriteLine("You specified ffmpeg but no ffmpeg was found, aborting...");
                        System.Environment.Exit(1);
                    }
                }
                else
                {
                    File.Move("fmodoutput.wav", Path.Combine(outFolder, "output.wav"));
                }
            }
            else
            {
                OJMDump.DumpSamples(ojmPath, outFolder);
            }

            return outFolder;
        }

        private void CreateDiff(string path, OJNData ojn_header, Diff diff, bool ffmpeg)
        {
            NotePackage.Chart chart;

            string diffname = null;
            string diffex = null;

            switch (diff)
            {
                default:
                case Diff.EX:
                    chart = ojn_header.DumpEXPackage();
                    diffex = $"_EX_LVL{ojn_header.level[0]}";
                    break;
                case Diff.NX:
                    chart = ojn_header.DumpNXPackage();
                    diffex = $"_NX_LVL{ojn_header.level[1]}";
                    break;
                case Diff.HX:
                    chart = ojn_header.DumpHXPackage();
                    diffex = $"_HX_LVL{ojn_header.level[2]}";
                    break;
            }
            diffname = $"{ojn_header.Title}{diffex}.osu";
            diffname = Helpers.GetSafeFilename(diffname);
            diffname = Path.Combine(path, diffname);

            string audio_filename = ffmpeg ? "audio.mp3" : "output.wav";
            string audio_file = virtual_mode ? "virtual" : audio_filename;

            string[] general =
            {
                "[General]",
                $"AudioFilename: {audio_file}",
                "AudioLeadIn: 0",
                "PreviewTime: 6969",
                "Countdown: 0",
                "SampleSet: Soft",
                "StackLeniency: 0.7",
                "Mode: 3",
                "LetterboxInBreaks: 0\n\n",
            };


            string[] metadata =
            {
                "[Metadata]",
                $"Title:{ojn_header.Title}",
                $"TitleUnicode:{ojn_header.Title}",
                $"Artist:{ojn_header.Artist}",
                $"ArtistUnicode:{ojn_header.Artist}",
                $"Creator:{ojn_header.Noter}",
                $"Version:7k{diffex}",
                "Source:o2jam",
                $"Tags:o2jam {ojn_header.OJMFile}",
                $"BeatmapID:{ojn_header.SongID}",
                $"BeatmapSetID:{ojn_header.SongID}",
                "\n"
            };

            string[] difficulty =
            {
                "[Difficulty]",
                "HPDrainRate:5",
                "CircleSize:7",
                "OverallDifficulty:5",
                "ApproachRate:5",
                "SliderMultiplier:1.4",
                "SliderTickRate:1",
                "\n"
            };

            string[] events =
{
                "[Events]",
                "0,0,\"bg.jpg\"",
                "\n"
            };

            //clear the globals
            OsuTimings.Clear();
            OsuSamples.Clear();
            OsuNotes.Clear();

            //populate the osu lists
            GenTimings(chart.Timings);
            GenNotes(chart.Notes);

            //as per osu specification
            int[] column_map = new int[7] { 36, 109, 182, 255, 328, 401, 474 };
            using (StreamWriter w = new StreamWriter(File.Open(diffname, FileMode.Create)))
            {
                w.Write("osu file format v14\n\n");
                foreach (var l in general) w.WriteLine(l);
                foreach (var l in metadata) w.WriteLine(l);
                foreach (var l in difficulty) w.WriteLine(l);
                foreach (var l in events) w.WriteLine(l);

                if (virtual_mode)
                {
                    foreach (var hit in OsuSamples)
                    {
                        w.Write($"5,{(int)hit.MsStart},0,\"{hit.RefID}.ogg\",100\n");
                    }
                }

                w.WriteLine("\n\n[TimingPoints]");
                foreach (var timing in OsuTimings)
                {
                    float bpm = 240 / timing.MsPerMeasure * 1000;
                    float ms_per_beat = 60000 / bpm;
                    if (ms_per_beat < 0.01) ms_per_beat = 0.01f;
                    string line = $"{(int)timing.MsMarking},{ms_per_beat},4,2,2,100,1,0\n";
                    w.Write(line);
                }

                w.WriteLine("\n\n[HitObjects]");


                foreach (var note in OsuNotes)
                {
                    string line;
                    int vol = 0;
                    string filename = "";
                    if (virtual_mode)
                    {
                        vol = 100;
                        filename = $"{note.RefID}.ogg";
                    }
                    if (note.LN)
                    {
                        line = $"{column_map[note.Channel]},0,{(int)note.MsStart},128,0,{(int)note.MsEnd}:0:0:0:{vol}:{filename}\n";
                    }
                    else
                    {
                        line = $"{column_map[note.Channel]},0,{(int)note.MsStart},1,0,0:0:0:{vol}:{filename}\n";
                    }
                    w.Write(line);
                }
            }
        }

        private List<OsuNote> GenSampleSequence(FMODSystem fmod_sys)
        {
            List<OsuNote> sample_times = new List<OsuNote>();
            foreach (var sample in OsuSamples)
            {
                if (fmod_sys.Samples.Any(n => n.RefID == sample.RefID))
                {
                    sample_times.Add(sample);
                }
            }
            foreach (var note in OsuNotes)
            {
                if (fmod_sys.Samples.Any(n => n.RefID == note.RefID))
                {
                    sample_times.Add(note);
                }
            }
            return sample_times;
        }

        private void PlaySong(FMODSystem fmod_sys)
        {
            List<OsuNote> sample_times = GenSampleSequence(fmod_sys);

            sample_times = sample_times.OrderBy(o => o.MsStart).ToList();
            long time = Helpers.HighResolutionDateTime.timenow;
            foreach (var toplay in sample_times)
            {
                long elapsed_ms = Helpers.HighResolutionDateTime.timenow - time;
                while (elapsed_ms < toplay.MsStart)
                {
                    elapsed_ms = Helpers.HighResolutionDateTime.timenow - time;
                }
                //Console.WriteLine(elapsed_ms);
                fmod_sys.PlaySample(toplay.RefID);
            }
        }

        private void RenderToFile(FMODSystem fmod_sys, float duration)
        {
            List<OsuNote> sample_times = GenSampleSequence(fmod_sys);

            sample_times = sample_times.OrderBy(o => o.MsStart).ToList();
            float elapsed = 0.0f;
            float rate = 1024.0f / 48000.0f * 1000.0f;
            int index = 0;
            OsuNote next_id = sample_times[index];

            while (elapsed < duration * 1000 + 1000)
            {
                if (next_id != null && elapsed > next_id.MsStart)
                {
                    fmod_sys.PlaySample(next_id.RefID);
                    index++;
                    if (index < sample_times.Count()) next_id = sample_times[index];
                    else next_id = null;
                }
                elapsed += rate;
                fmod_sys.FmodSys.update();
            }
        }

        private void GenTimings(List<NotePackage.BPMChange> timings)
        {
            //elapsed milliseconds
            NotePackage.BPMChange prev = null;
            float milliseconds = 0.0f;

            foreach (var timing in timings)
            {
                // Console.WriteLine($"!timing change {timing.val} on {timing.measure_start}");
                MSTiming ms_time = new MSTiming();

                if (prev == null)
                {
                    prev = timing;
                    ms_time.MeasureStart = timing.MeasureStart;
                    ms_time.MsMarking = 0;
                    ms_time.MsPerMeasure = 240 / timing.NewBPM * 1000;
                    OsuTimings.Add(ms_time);
                    continue;
                }
                float ms_per_measure = 240 / prev.NewBPM * 1000;
                float delta = timing.MeasureStart - prev.MeasureStart;

                milliseconds += delta * ms_per_measure;
                prev = timing;
                float prev_ms = OsuTimings[OsuTimings.Count() - 1].MsMarking;
                if (milliseconds - prev_ms < 1) OsuTimings[OsuTimings.Count() - 1].MsMarking--;

                ms_time.MeasureStart = timing.MeasureStart;
                ms_time.MsMarking = milliseconds;
                ms_time.MsPerMeasure = 240 / timing.NewBPM * 1000;
                OsuTimings.Add(ms_time);
            }
        }

        private void GenNotes(List<NotePackage.NoteEvent> notes)
        {
            foreach (var note in notes)
            {
                //Console.WriteLine($"channel {note.channel} start {note.measure_start} end {note.measure_end}");
                //grab the last bpm change
                MSTiming last_bpm = OsuTimings[0];

                //incase the end of a LN is after a bpm change
                MSTiming next_bpm = null;

                int index = OsuTimings.BinarySearch(new MSTiming { MeasureStart = note.MeasureStart }, new TimingComparer());
                if (index < 0) index = ~index - 1;
                last_bpm = OsuTimings[index];

                //check for bpm changes throughout a long note
                if (note.NoteType == 3)
                {
                    index = OsuTimings.BinarySearch(new MSTiming { MeasureStart = note.MeasureEnd }, new TimingComparer());
                    if (index < 0) index = ~index - 1;
                    next_bpm = OsuTimings[index];
                }

                OsuNote osu_note = new OsuNote
                {
                    Channel = note.Channel
                };

                //difference between our note and the start of the last bpm
                float start_delta = note.MeasureStart - last_bpm.MeasureStart;

                //grab the ms base for the start of the bpm change and add 
                float start_offset = last_bpm.MsPerMeasure * start_delta;
                float hit_start = last_bpm.MsMarking + start_offset;

                osu_note.MsStart = hit_start;

                //grab the offset for the end of a long note (if needed)
                if (note.NoteType == 3)
                {
                    float end_delta = note.MeasureEnd - next_bpm.MeasureStart;
                    float end_offset = next_bpm.MsPerMeasure * end_delta;
                    float hit_end = next_bpm.MsMarking + end_offset;
                    osu_note.LN = true;
                    osu_note.MsEnd = hit_end;
                }

                    if (note.NoteType == 4) note.Value += 1000;
                    osu_note.RefID = note.Value;

                if (osu_note.Channel < 9)
                {
                    osu_note.Channel -= 2;
                    OsuNotes.Add(osu_note);
                }
                else OsuSamples.Add(osu_note);
            }
        }
    }
}
