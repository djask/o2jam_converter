using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace O2JamUtils
{
    public class FMODSystem
    {
        public FMOD.System FmodSys;
        public List<OJMDump.FMODSample> Samples { get; set; }
        public bool Stream = false;

        public FMODSystem(bool play = true)
        {
            FmodSys = InitFMOD(play: play);
        }

        public FMOD.System InitFMOD(bool play = false)
        {
            FMOD.RESULT result;


            char[] name = new char[256];

            /* 
                Create a System object and initialize. 
            */
            result = FMOD.Factory.System_Create(out FMOD.System system);

            system.getNumDrivers(out int numdrivers);
            if (numdrivers == 0)
            {
                system.setOutput(FMOD.OUTPUTTYPE.NOSOUND);
            }

            if (!play)
            {
                result = system.setOutput(FMOD.OUTPUTTYPE.WAVWRITER_NRT);
            }

            IntPtr out_name;
            if (!play)
            {
                out_name = Marshal.StringToHGlobalUni("r.wav");
            }
            else
            {
                out_name = IntPtr.Zero;
            }

            result = system.init(100, FMOD.INITFLAGS.NORMAL, IntPtr.Zero);
            Marshal.FreeHGlobal(out_name);
            return system;
        }

        public void ReleaseSystem()
        {
            FmodSys.close();
            FmodSys.release();
        }

        public void PlaySample(int refID)
        {
            OJMDump.FMODSample found = Samples.Where(sample => sample.RefID == refID).FirstOrDefault();
            FmodSys.playSound(found.Data, null, false, out FMOD.Channel channel);
        }

        public void LoadSamples(string path, bool stream)
        {
            Samples = OJMDump.ExtractSamples(path, FmodSys, stream);
        }
    }
}
