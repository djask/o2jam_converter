using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace O2JamUtils
{
    public class OJNData
    {
        public int SongID { get; }
        private readonly char[] signature = new char[4];
        private readonly float encode_version;
        public int Genre { get; }
        public float BPM { get; }
        public short[] level = new short[4];
        private readonly int[] event_count = new int[4];
        private readonly int[] note_count = new int[4];
        private readonly int[] measure_count = new int[4];
        private readonly int[] package_count = new int[4];
        private readonly short old_encode_version;
        private readonly short old_songid;
        private readonly char[] old_genre = new char[20];
        private readonly int bmp_size;
        private readonly int old_file_version;
        public string Title { get; }
        public string Artist { get; }
        public string Noter { get; }
        public string OJMFile { get;  }
        private readonly int cover_size;
        public readonly int[] time = new int[3];
        private readonly int[] note_offset = new int[3];
        public int CoverOffset { get; }

        public NoteUtils.Chart EXChart { get; set; } = null;
        public NoteUtils.Chart NXChart { get; set; } = null;
        public NoteUtils.Chart HXChart { get; set; } = null;

        public class DiffInfo
        {
            public int EventCount { get; set; }
            public int NoteCount { get; set;  }
            public int MeasureCount { get; set; }
            public int PackageCount { get; set; }
            public int Time { get; set; }
            public int Start { get; set; }
            public int End { get; set; }
        }

        //grab diff information
        public DiffInfo GetDiffHeaders(int diff)
        {
            DiffInfo ret = new DiffInfo
            {
                EventCount = event_count[diff],
                NoteCount = note_count[diff],
                MeasureCount = measure_count[diff],
                PackageCount = package_count[diff],
                Time = time[diff],
                Start = note_offset[diff]
            };
            if (diff < 2) ret.End = note_offset[diff + 1];
            else ret.End = CoverOffset;
            return ret;
        }

        private MemoryMappedFile ojn_file;

        //populate our class variables
        public OJNData(String path)
        {
            ojn_file = Helpers.MemFile(path);

            //312 bytes long i think
            MemoryMappedViewAccessor buf = ojn_file.CreateViewAccessor(0, 312, MemoryMappedFileAccess.Read);
            long offset = 0;
            SongID = buf.ReadInt32(offset); offset += 4;

            byte[] unk_raw1 = new byte[4];
            buf.ReadArray(offset, unk_raw1, 0, 4); offset += 4;
            signature = Encoding.UTF8.GetString(unk_raw1).ToCharArray();
            encode_version = buf.ReadSingle(offset); offset += 4;
            Genre = buf.ReadInt32(offset); offset += 4;
            BPM = buf.ReadSingle(offset); offset += 4;

            //some level stuff
            buf.ReadArray(offset, level, 0, 4); offset += 8;
            buf.ReadArray(offset, event_count, 0, 3); offset += 12;
            buf.ReadArray(offset, note_count, 0, 3); offset += 12;
            buf.ReadArray(offset, measure_count, 0, 3); offset += 12;
            buf.ReadArray(offset, package_count, 0, 3); offset += 12;

            //skip the old variables
            offset += 24;

            bmp_size = buf.ReadInt32(offset); offset += 4;

            //skip old file version
            offset += 4;

            //title and stuff, just assuming it's utf8 for now, but will
            //have to deal with gbk and big5
            byte[] raw_title = new byte[64];
            byte[] raw_artist = new byte[32];
            byte[] raw_noter = new byte[32];
            buf.ReadArray(offset, raw_title, 0, 64); offset += 64;
            buf.ReadArray(offset, raw_artist, 0, 32); offset += 32; 
            buf.ReadArray(offset, raw_noter, 0, 32); offset += 32;

            raw_title = raw_title.Where(i => i != 0).ToArray();
            raw_artist = raw_artist.Where(i => i != 0).ToArray();
            raw_noter = raw_noter.Where(i => i != 0).ToArray();

            Title = Encoding.GetEncoding(936).GetString(raw_title);
            Artist = Encoding.GetEncoding(936).GetString(raw_artist);
            Noter = Encoding.GetEncoding(936).GetString(raw_noter);

            byte[] raw_file = new byte[32];
            buf.ReadArray(offset, raw_file, 0, 32); offset += 32;
            raw_file = raw_file.Where(i => i != 0).ToArray();

            //assuming file names should be in unicode...
            OJMFile = Encoding.UTF8.GetString(raw_file);

            cover_size = buf.ReadInt32(offset); offset += 4;
            buf.ReadArray(offset, time, 0, 3); offset += 12;
            buf.ReadArray(offset, note_offset, 0, 3); offset += 12;
            CoverOffset = buf.ReadInt32(offset);

            HXChart = NoteUtils.ReadOJNPackage(ojn_file, this, 2);
            if (level[1] < level[2]) NXChart = NoteUtils.ReadOJNPackage(ojn_file, this, 1);
            if (level[0] < level[2] || level[0] < level[1]) EXChart = NoteUtils.ReadOJNPackage(ojn_file, this, 0);
        }

        public void DumpImage(String out_dir)
        {
            if (cover_size == 0) return;
            using (MemoryMappedViewAccessor buf = ojn_file.CreateViewAccessor(CoverOffset, cover_size, MemoryMappedFileAccess.Read))
            {
                //jpeg image dump
                String filename = "bg.jpg";
                String path = Path.Combine(out_dir, filename);
                using (BinaryWriter writer = new BinaryWriter(File.Open(path, FileMode.Create)))
                {
                    byte[] tmp = new byte[cover_size];
                    buf.ReadArray(0, tmp, 0, cover_size);
                    writer.Write(tmp);
                }
            }
        }
    };
}
