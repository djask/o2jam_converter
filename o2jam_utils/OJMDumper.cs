using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

/* a c# implementation of the ojm dumper based on information from open2jam */

namespace O2JamUtils
{
    public class OJMDump
    {
        /** the xor mask used in the M30 format */
        private readonly static byte[] mask_nami = new byte[] { 0x6E, 0x61, 0x6D, 0x69 }; // nami
        private readonly static byte[] mask_0412 = new byte[] { 0x30, 0x34, 0x31, 0x32 }; // 0412


        /** the M30 signature, "M30\0" in little endian */
        private const int M30_SIGNATURE = 0x0030334D;

        /** the OMC signature, "OMC\0" in little endian */
        private const int OMC_SIGNATURE = 0x00434D4F;

        /** the OJM signature, "OJM\0" in little endian */
        private const int OJM_SIGNATURE = 0x004D4A4F;

        //????????? from the notetool
        private static readonly byte[] REARRANGE_TABLE = new byte[]{
            0x10, 0x0E, 0x02, 0x09, 0x04, 0x00, 0x07, 0x01,
            0x06, 0x08, 0x0F, 0x0A, 0x05, 0x0C, 0x03, 0x0D,
            0x0B, 0x07, 0x02, 0x0A, 0x0B, 0x03, 0x05, 0x0D,
            0x08, 0x04, 0x00, 0x0C, 0x06, 0x0F, 0x0E, 0x10,
            0x01, 0x09, 0x0C, 0x0D, 0x03, 0x00, 0x06, 0x09,
            0x0A, 0x01, 0x07, 0x08, 0x10, 0x02, 0x0B, 0x0E,
            0x04, 0x0F, 0x05, 0x08, 0x03, 0x04, 0x0D, 0x06,
            0x05, 0x0B, 0x10, 0x02, 0x0C, 0x07, 0x09, 0x0A,
            0x0F, 0x0E, 0x00, 0x01, 0x0F, 0x02, 0x0C, 0x0D,
            0x00, 0x04, 0x01, 0x05, 0x07, 0x03, 0x09, 0x10,
            0x06, 0x0B, 0x0A, 0x08, 0x0E, 0x00, 0x04, 0x0B,
            0x10, 0x0F, 0x0D, 0x0C, 0x06, 0x05, 0x07, 0x01,
            0x02, 0x03, 0x08, 0x09, 0x0A, 0x0E, 0x03, 0x10,
            0x08, 0x07, 0x06, 0x09, 0x0E, 0x0D, 0x00, 0x0A,
            0x0B, 0x04, 0x05, 0x0C, 0x02, 0x01, 0x0F, 0x04,
            0x0E, 0x10, 0x0F, 0x05, 0x08, 0x07, 0x0B, 0x00,
            0x01, 0x06, 0x02, 0x0C, 0x09, 0x03, 0x0A, 0x0D,
            0x06, 0x0D, 0x0E, 0x07, 0x10, 0x0A, 0x0B, 0x00,
            0x01, 0x0C, 0x0F, 0x02, 0x03, 0x08, 0x09, 0x04,
            0x05, 0x0A, 0x0C, 0x00, 0x08, 0x09, 0x0D, 0x03,
            0x04, 0x05, 0x10, 0x0E, 0x0F, 0x01, 0x02, 0x0B,
            0x06, 0x07, 0x05, 0x06, 0x0C, 0x04, 0x0D, 0x0F,
            0x07, 0x0E, 0x08, 0x01, 0x09, 0x02, 0x10, 0x0A,
            0x0B, 0x00, 0x03, 0x0B, 0x0F, 0x04, 0x0E, 0x03,
            0x01, 0x00, 0x02, 0x0D, 0x0C, 0x06, 0x07, 0x05,
            0x10, 0x09, 0x08, 0x0A, 0x03, 0x02, 0x01, 0x00,
            0x04, 0x0C, 0x0D, 0x0B, 0x10, 0x05, 0x06, 0x0F,
            0x0E, 0x07, 0x09, 0x0A, 0x08, 0x09, 0x0A, 0x00,
            0x07, 0x08, 0x06, 0x10, 0x03, 0x04, 0x01, 0x02,
            0x05, 0x0B, 0x0E, 0x0F, 0x0D, 0x0C, 0x0A, 0x06,
            0x09, 0x0C, 0x0B, 0x10, 0x07, 0x08, 0x00, 0x0F,
            0x03, 0x01, 0x02, 0x05, 0x0D, 0x0E, 0x04, 0x0D,
            0x00, 0x01, 0x0E, 0x02, 0x03, 0x08, 0x0B, 0x07,
            0x0C, 0x09, 0x05, 0x0A, 0x0F, 0x04, 0x06, 0x10,
            0x01, 0x0E, 0x02, 0x03, 0x0D, 0x0B, 0x07, 0x00,
            0x08, 0x0C, 0x09, 0x06, 0x0F, 0x10, 0x05, 0x0A,
            0x04, 0x00
        };

        public class FMODSample
        {
            public ushort RefID { get; set; }
            public uint FileSize { get; set; }
            public FMOD.Sound Data { get; set; }
            public byte[] BinData { get; set; }
            public Boolean AsStream { get; set; }
        }

        public static String GetType(string path)
        {
            byte[] signature = new byte[3];
            using (MemoryMappedFile f = Helpers.MemFile(path))
            using (MemoryMappedViewStream vs = f.CreateViewStream(0, 0, MemoryMappedFileAccess.Read))
            {
                vs.Read(signature, 0, signature.Length);
            }
            return Encoding.Default.GetString(signature);
        }

        public static List<FMODSample> ExtractSamples(string path, FMOD.System fmod_sys, Boolean stream)
        {
            int signature;
            MemoryMappedFile f = Helpers.MemFile(path);

            using (MemoryMappedViewStream vs = f.CreateViewStream(0L, 0L, MemoryMappedFileAccess.Read))
            using (BinaryReader reader = new BinaryReader(vs))
                signature = reader.ReadInt32();

            List<FMODSample> samples = new List<FMODSample>();

            switch (signature)
            {
                case M30_SIGNATURE:
                    ParseM30(f, fmod_sys:fmod_sys, samples:samples, stream:stream);
                    break;
                case OMC_SIGNATURE:
                case OJM_SIGNATURE:
                    ParseOMC(f, fmod_sys: fmod_sys, samples: samples, stream: stream);
                    break;
            }

            return samples;
        }

        public static void DumpSamples(string path, string outpath)
        {
            int signature;
            MemoryMappedFile f = Helpers.MemFile(path);

            using (MemoryMappedViewStream vs = f.CreateViewStream(0L, 0L, MemoryMappedFileAccess.Read))
            using (BinaryReader reader = new BinaryReader(vs))
                signature = reader.ReadInt32();

            switch (signature)
            {
                case M30_SIGNATURE:
                    ParseM30(f, outpath:outpath);
                    break;
                case OMC_SIGNATURE:
                case OJM_SIGNATURE:
                    ParseOMC(f, outpath:outpath);
                    break;
            }
        }

        private static void ParseM30(MemoryMappedFile f, FMOD.System fmod_sys = null, List<FMODSample> samples = null, Boolean stream = false, string outpath = null)
        {
            MemoryMappedViewStream buf = f.CreateViewStream(4, 24, MemoryMappedFileAccess.Read);
            BinaryReader reader = new BinaryReader(buf);

            // header
            int file_format_version = reader.ReadInt32();
            int encryption_flag = reader.ReadInt32();
            int sample_count = reader.ReadInt32();
            int sample_offset = reader.ReadInt32();
            int payload_size = reader.ReadInt32();
            int padding = reader.ReadInt32();

            Boolean zero_start = false;

            using (buf = f.CreateViewStream(28, 0, MemoryMappedFileAccess.Read))
            using (reader = new BinaryReader(buf))
            {

                for (int i = 0; i < sample_count; i++)
                {
                    FMOD.CREATESOUNDEXINFO exinfo = new FMOD.CREATESOUNDEXINFO();

                    long remaining_bytes = buf.Capacity - buf.Position;
                    if (remaining_bytes < 52)
                    {
                        throw new System.ArgumentException("OJM Header sample size is incorrect");
                    }

                    byte[] sample_name = reader.ReadBytes(32);
                    uint sample_size = reader.ReadUInt32();

                    short codec_code = reader.ReadInt16();
                    short codec_code2 = reader.ReadInt16();

                    int music_flag = reader.ReadInt32();
                    ushort note_ref = reader.ReadUInt16();
                    short unk_zero = reader.ReadInt16();
                    int pcm_samples = reader.ReadInt32();

                    byte[] sample_data = reader.ReadBytes((int)sample_size);

                    switch (encryption_flag)
                    {
                        case 0: break; //Let it pass
                        case 16: M30_xor(sample_data, mask_nami); break;
                        case 32: M30_xor(sample_data, mask_0412); break;
                        default: break;
                    }

                    if (note_ref == 0) zero_start = true;

                    //normal note
                    if(zero_start)note_ref += 1;

                    //background note
                    if (codec_code == 0)
                    {
                        note_ref += 1000;
                    }

                    //unknown sound
                    else if (codec_code != 5)
                    {
                        Console.WriteLine("not recognized sample type");
                    }

                    String filename = $"{note_ref}.ogg";

                    if (fmod_sys != null)
                    {
                        FMODSample sample = new FMODSample();
                        sample.RefID = note_ref += 1;
                        sample.FileSize = sample_size;
                        sample.BinData = sample_data;
                        FMOD.Sound fmod_sound;

                        exinfo.length = sample.FileSize;
                        exinfo.cbsize = Marshal.SizeOf(exinfo);
                        FMOD.MODE mode = FMOD.MODE.OPENMEMORY;
                        if (stream) mode |= FMOD.MODE.CREATESTREAM;
                        else mode |= FMOD.MODE.CREATESAMPLE;

                        var result = fmod_sys.createSound(sample.BinData, mode, ref exinfo, out fmod_sound);
                        sample.Data = fmod_sound;
                        sample.AsStream = stream;
                        samples.Add(sample);
                    }
                    else if (outpath != null)
                    {
                        using (BinaryWriter writer = new BinaryWriter(File.Open(Path.Combine(outpath,filename), FileMode.Create)))
                        {
                            writer.Write(sample_data);
                        }
                    }
                }
            }
        }

        private static void M30_xor(byte[] array, byte[] mask)
        {
            for (int i = 0; i + 3 < array.Length; i += 4)
            {
                array[i + 0] ^= mask[0];
                array[i + 1] ^= mask[1];
                array[i + 2] ^= mask[2];
                array[i + 3] ^= mask[3];
            }
        }

        private static void ParseOMC(MemoryMappedFile f, FMOD.System fmod_sys = null,List<FMODSample> samples = null, Boolean stream = false, string outpath = null)
        {
            FMOD.MODE mode = FMOD.MODE.DEFAULT;
            FMOD.CREATESOUNDEXINFO exinfo = new FMOD.CREATESOUNDEXINFO();

            if (fmod_sys != null)
            {
                mode = FMOD.MODE.OPENMEMORY;
                if (stream) mode |= FMOD.MODE.CREATESTREAM;
                else mode |= FMOD.MODE.CREATESAMPLE;
            }

            int file_offset = 0;
            short wav_count, ogg_count;
            int wav_start, ogg_start, filesize;
            using (MemoryMappedViewAccessor buf = f.CreateViewAccessor(4, 16, MemoryMappedFileAccess.Read))
            {
                // header
                wav_count = buf.ReadInt16(file_offset); file_offset += 2; //wav count
                ogg_count = buf.ReadInt16(file_offset); file_offset += 2; //ogg count
                wav_start = buf.ReadInt32(file_offset); file_offset += 4;
                ogg_start = buf.ReadInt32(file_offset); file_offset += 4;
                filesize = buf.ReadInt32(file_offset); file_offset += 4;
            }

            file_offset = 20;
            int sample_id = 1;

            acc_keybyte = 0xFF;
            acc_counter = 0;

            //read wav data first this doesn't really work yet
            while (file_offset < ogg_start)
            {
                //some package variables
                byte[] sample_name = new byte[32];
                short audio_format, num_channels, block_align, bits_per_sample;
                int sample_rate, bit_rate, unk_data, chunk_size;

                using (MemoryMappedViewAccessor buf = f.CreateViewAccessor(file_offset, 56, MemoryMappedFileAccess.Read))
                {
                    long pos = 0;
                    //WAV DATA HEADERS
                    //name of sample
                    buf.ReadArray(pos, sample_name, 0, 32); pos += 32;

                    //wav metadata
                    audio_format = buf.ReadInt16(pos); pos += 2;
                    num_channels = buf.ReadInt16(pos); pos += 2;
                    sample_rate = buf.ReadInt32(pos); pos += 4;
                    bit_rate = buf.ReadInt32(pos); pos += 4;
                    block_align = buf.ReadInt16(pos); pos += 2;
                    bits_per_sample = buf.ReadInt16(pos); pos += 2;
                    unk_data = buf.ReadInt32(pos); pos += 4;

                    //sample size
                    chunk_size = buf.ReadInt32(pos); pos += 4;

                    //0 size wav, go to next
                    if (chunk_size == 0)
                    {
                        sample_id++;
                        continue;
                    }
                }

                file_offset += chunk_size;
                byte[] wav_data = new byte[chunk_size];

                using (var buf = f.CreateViewAccessor(file_offset, chunk_size, MemoryMappedFileAccess.Read))
                {
                    //basically get remaining data
                    buf.ReadArray(0, wav_data, 0, chunk_size);
                }
                wav_data = Rearrange(wav_data);
                wav_data = OMC_xor(wav_data);

                //write the filename can't be bothered finding out the encoding
                byte[] bindata = new byte[chunk_size + 36];
                using (var bstream = new MemoryStream(bindata))
                using (BinaryWriter writer = new BinaryWriter(bstream))
                {
                    writer.Write("RIFF");
                    writer.Write(chunk_size + 36);
                    writer.Write("WAVE");
                    writer.Write("fmt");
                    writer.Write(0x10);
                    writer.Write(audio_format);
                    writer.Write(num_channels);
                    writer.Write(sample_rate);
                    writer.Write(bit_rate);
                    writer.Write(block_align);
                    writer.Write(bits_per_sample);
                    writer.Write("data");
                    writer.Write(chunk_size);
                    writer.Write(wav_data);
                }

                string filename = $"{sample_id}.wav";

                if (fmod_sys != null)
                {
                    FMODSample sample = new FMODSample();
                    sample.FileSize = (uint)filesize;
                    sample.AsStream = stream;
                    sample.BinData = bindata;

                    exinfo.cbsize = Marshal.SizeOf(exinfo);
                    exinfo.length = sample.FileSize;
                    FMOD.Sound fmod_sound;
                    FMOD.RESULT result = fmod_sys.createSound(sample.BinData, mode, ref exinfo, out fmod_sound);
                    sample.Data = fmod_sound;
                    samples.Add(sample);
                } 
                else if(outpath != null)
                {
                    using (BinaryWriter writer = new BinaryWriter(File.Open(Path.Combine(outpath, filename), FileMode.Create)))
                    {
                        writer.Write(bindata);
                    }
                }
            }
            file_offset = ogg_start;
            sample_id = 1001; //ogg uses 1000+
            byte[] tmp_buffer = new byte[1024];
            while (file_offset < filesize) //read ogg data
            {
                byte[] sample_name;
                int sample_size;
                using (var buf = f.CreateViewStream(file_offset, 36, MemoryMappedFileAccess.Read))
                using(BinaryReader reader = new BinaryReader(buf))
                {
                    sample_name = reader.ReadBytes(32);
                    sample_size = reader.ReadInt32();
                }

                file_offset += 36;


                if (sample_size == 0)
                {
                    sample_id++;
                    continue;
                }

                if (fmod_sys != null)
                {

                    FMODSample sample = new FMODSample();

                    sample.BinData = new byte[sample_size];
                    sample.AsStream = stream;
                    sample.FileSize = (uint)sample_size;
                    sample.RefID = (ushort)sample_id;

                    using (var buf = f.CreateViewStream(file_offset, sample_size, MemoryMappedFileAccess.Read))
                    using (BinaryReader reader = new BinaryReader(buf))
                    {
                        //write the filename
                        sample_name = sample_name.Where(i => i != 0).ToArray();
                        using (var bstream = new MemoryStream(sample.BinData))
                        using (BinaryWriter writer = new BinaryWriter(bstream))
                        {
                            while (reader.BaseStream.Position != reader.BaseStream.Length)
                            {
                                tmp_buffer = reader.ReadBytes(1024);
                                writer.Write(tmp_buffer);
                            }
                        }
                    }

                    exinfo.length = (uint)sample_size;
                    exinfo.cbsize = Marshal.SizeOf(exinfo);

                    FMOD.Sound fmod_sound;
                    var result = fmod_sys.createSound(sample.BinData, mode, ref exinfo, out fmod_sound);
                    sample.Data = fmod_sound;
                    samples.Add(sample);
                }
                else if(outpath != null)
                {
                    string filename = Path.Combine(outpath, $"{sample_id}.ogg");
                    using (var buf = f.CreateViewStream(file_offset, sample_size, MemoryMappedFileAccess.Read))
                    using (BinaryReader reader = new BinaryReader(buf))
                    {
                        //write the filename
                        sample_name = sample_name.Where(i => i != 0).ToArray();
                        using (BinaryWriter writer = new BinaryWriter(File.Open(filename, FileMode.Create)))
                        {
                            while (reader.BaseStream.Position != reader.BaseStream.Length)
                            {
                                tmp_buffer = reader.ReadBytes(1024);
                                writer.Write(tmp_buffer);
                            }
                        }
                    }

                }

                file_offset += sample_size;
                sample_id++;
            }
        }

        /** some weird encryption */
        private static int acc_keybyte = 0xFF;
        private static int acc_counter = 0;
        private static byte[] OMC_xor(byte[] buf)
        {
            int temp;
            byte this_byte;
            for (int i = 0; i < buf.Length; i++)
            {
                temp = this_byte = buf[i];

                if (((acc_keybyte << acc_counter) & 0x80) != 0)
                {
                    this_byte = (byte)~this_byte;
                }

                buf[i] = this_byte;
                acc_counter++;
                if (acc_counter > 7)
                {
                    acc_counter = 0;
                    acc_keybyte = temp;
                }
            }
            return buf;
        }

        private static byte[] Rearrange(byte[] encoded_data)
        {
            int len = encoded_data.Length;
            int key = ((len % 17) << 4) + (len % 17);

            int block_size = len / 17;

            byte[] raw_data = new byte[len];
            System.Array.Copy(encoded_data, raw_data, 0);

            for (int i = 0; i < 17; i++)
            {
                //i think this works properly
                System.Array.ConstrainedCopy(encoded_data, i, raw_data, REARRANGE_TABLE[key], block_size);
                key++;
            }
            return raw_data;
        }
    }
}
