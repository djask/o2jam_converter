using System;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace O2JamUtils
{
    public class OJNNoteUtils
    {
        public class Chart
        {
            //chart has timings, note events
            public List<NoteEvent> Notes { get; set; } = new List<NoteEvent>();

            //timings list
            public List<BPMChange> Timings { get; set; } = new List<BPMChange>();
        }

        public class NoteHeader
        {
            //which "bar" the package is on
            public int Measure { get; set; }

            //channel meaning
            //0 	measure fraction
            //1 	BPM change
            //2 	note on 1st lane
            //3 	note on 2nd lane
            //4 	note on 3rd lane
            //5 	note on 4th lane(middle button)
            //6 	note on 5th lane
            //7 	note on 6th lane
            //8 	note on 7th lane
            //9~22 	auto-play samples(?)
            public short Channel { get; set; }

            //the number of events
            public short Events { get; set; }
        }

        public class NoteEvent
        {
            public int Channel { get; set; }

            public short Value { get; set; }
            public float Volume { get; set; }
            public float Pan { get; set; }
            public byte NoteType { get; set; }

            //measure pos is event index / no_events
            public float MeasureStart { get; set; }

            //duration if it's a long note
            public float MeasureEnd { get; set; }
        }

        public class BPMChange
        {
            public float NewBPM { get; set; }
            public float MeasureStart { get; set; }
        }

        public static Chart ReadOJNPackage(MemoryMappedFile ojn_file, OJNData header, int diff)
        {
            long pos = 0;
            //an array of packages
            Chart chart = new Chart();

            //get the initial bpm timing (not sure if needed yet)...
            BPMChange start_bpm = new BPMChange
            {
                NewBPM = header.BPM,
                MeasureStart = 0
            };
            chart.Timings.Add(start_bpm);

            //some variables to keep track of stuff
            //start of long holds
            NoteEvent[] start_holds = new NoteEvent[7];

            OJNData.DiffInfo diff_headers = header.GetDiffHeaders(diff);

            int N = diff_headers.PackageCount;

            int sz = diff_headers.End - diff_headers.Start;

            using (var buf = ojn_file.CreateViewAccessor(diff_headers.Start, sz, MemoryMappedFileAccess.Read))
            {
                for(int i = 0; i < N; i++)
                {
                    int measure = buf.ReadInt32(pos); pos += 4;
                    short channel = buf.ReadInt16(pos); pos += 2;
                    short events = buf.ReadInt16(pos); pos += 2;

                    //hope that this doesnt occur
                    if (channel == 0)
                    {
                        pos += events * 4;
                    }

                    //get bpm change timing
                    else if (channel == 1)
                    {
                        for(int j = 0; j < events; j++)
                        {
                            float time = measure;
                            //PLEASE DO NOT ADD ONE TO J, IT BREAKS THE PROCESS
                            time += (j / ((float)events));

                            float val = buf.ReadSingle(pos); pos += 4;
                            if (val == 0) continue;

                            BPMChange timing = new BPMChange
                            {
                                NewBPM = val,
                                MeasureStart = time
                            };
                            chart.Timings.Add(timing);

                        }
                    }
                    //populate note events
                    else
                    {
                        for(int j = 0; j < events; j++)
                        {
                            short val = buf.ReadInt16(pos); pos += 2;
                            //skip empty events (but also add the offset)
                            if(val == 0) { pos += 2;  continue;  }
                            val--;

                            //new noteevent object
                            NoteEvent note_event = new NoteEvent
                            {
                                Value = val
                            };

                            //the measure time (e.g. 2.5 is measure 2, halfway throught the bar)
                            //logically with 8 events, the 4th one should be the middle right?
                            //but adding one to j breaks the program, and I don't know why
                            float time = measure;
                            time += (j / ((float)events));
                            note_event.MeasureStart = time;

                            //get the two half chars
                            byte raw_byte = buf.ReadByte(pos); pos++;

                            //split the 2nd byte into 2 nybbles
                            note_event.Volume = ((raw_byte >> 4) & 0x0F) / 16f;
                            note_event.Pan = (raw_byte  & 0x0F);
                            if (note_event.Pan == 0) note_event.Pan = 8;
                            note_event.Pan -= 8;
                            note_event.Pan /= 8f;

                            //get the note_type
                            note_event.NoteType = buf.ReadByte(pos); pos++;
                            if (note_event.NoteType % 8 > 3) note_event.Value += 1000;
                            note_event.NoteType %= 4;


                            //start of a long note, we record the measure time
                            if (channel < 9 && note_event.NoteType == 2 && start_holds[channel - 2] == null)
                            {
                                start_holds[channel - 2] = note_event;
                                continue;
                            }

                            //the end of a long note, we can put this into the note list
                            else if (channel < 9 && note_event.NoteType == 3 && start_holds[channel - 2] != null)
                            {
                                note_event.MeasureEnd = time;
                                note_event.MeasureStart = start_holds[channel - 2].MeasureStart;
                                start_holds[channel - 2] = null;
                            }

                            //normal note procedure
                            //not a long note so no need for end measure
                            else if (note_event.NoteType == 0)
                                note_event.MeasureEnd = -1;

                            //add to chart object
                            note_event.Channel = channel;
                            chart.Notes.Add(note_event);
                            
                        }
                    }
                }
            }
            return chart;
        }
    }
}
