using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtherCAT_Master.Core
{
    public class HomingMethods
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public Dictionary<sbyte, string> methodDict;

        public HomingMethods()
        {
            methodDict = new Dictionary<sbyte, string>() {
                { 1,   "Method 1: Homing on negative limit switch with index pulse" },
                { 2,   "Method 2: Homing on positive limit switch with index pulse" },
                { 3,   "Method 3: Homing on positive home switch and index pulse (negative stop)" },
                { 4,   "Method 4: Homing on positive home switch and index pulse (positive stop)" },
                { 5,   "Method 5: Homing on negative home switch and index pulse (positive stop)" },
                { 6,   "Method 6: Homing on negative home switch and index pulse (negative stop)" },
                { 7,   "Method 7: Homing on home switch and index pulse - positive inital motion (Version 1)" },
                { 8,   "Method 8: Homing on home switch and index pulse - positive inital motion (Version 2)" },
                { 9,   "Method 9: Homing on home switch and index pulse - positive inital motion (Version 3)" },
                { 10,  "Method 10: Homing on home switch and index pulse - positive inital motion (Version 4)" },
                { 11,  "Method 11: Homing on home switch and index pulse - negative inital motion (Version 1)" },
                { 12,  "Method 12: Homing on home switch and index pulse - negative inital motion (Version 2)" },
                { 13,  "Method 13: Homing on home switch and index pulse - negative inital motion (Version 3)" },
                { 14,  "Method 14: Homing on home switch and index pulse - negative inital motion (Version 4)" },
                { 17,  "Method 17: Homing on negative limit switch" },
                { 18,  "Method 18: Homing on positive limit switch" },
                { 19,  "Method 19: Homing on positive home switch (negative stop)" },
                { 20,  "Method 20: Homing on positive home switch (positive stop)" },
                { 21,  "Method 21: Homing on negative home switch (positive stop)" },
                { 22,  "Method 22: Homing on negative home switch (negative stop)" },
                { 23,  "Method 23: Homing on home switch - positive inital motion (Version 1)" },
                { 24,  "Method 24: Homing on home switch - positive inital motion (Version 2)" },
                { 25,  "Method 25: Homing on home switch - positive inital motion (Version 3)" },
                { 26,  "Method 26: Homing on home switch - positive inital motion (Version 4)" },
                { 27,  "Method 27: Homing on home switch - negative inital motion (Version 1)" },
                { 28,  "Method 28: Homing on home switch - negative inital motion (Version 2)" },
                { 29,  "Method 29: Homing on home switch - negative inital motion (Version 3)" },
                { 30,  "Method 30: Homing on home switch - negative inital motion (Version 4)" },
                { 33,  "Method 33: Homing on index pulse (negative direction)" },
                { 34,  "Method 34: Homing on index pulse (positive direction)" },
                { 37,  "Method 37: Homing on current position" },
                { -17, "Method -17: Block move negative" },
                { -18, "Method -18: Block move positive" },
            };
        }

        public async void SetOperModeHM(PCS drive, sbyte home_method, int home_offset, uint index_pulse, uint fast_speed, uint slow_speed, uint home_acceleration)
        {
            drive.SetOperMode(PCS.OM_HM);

            drive.COMM.AsyncWrite(drive.SlaveNumber, 0x607c, 0x00, home_offset); //Home offset
            drive.COMM.AsyncWrite(drive.SlaveNumber, 0x2700, 0x04, index_pulse); //Index pulse
            drive.COMM.AsyncWrite(drive.SlaveNumber, 0x6099, 0x01, fast_speed); //Homing speed fast
            drive.COMM.AsyncWrite(drive.SlaveNumber, 0x6099, 0x02, slow_speed); //Homing speed slow
            drive.COMM.AsyncWrite(drive.SlaveNumber, 0x609A, 0x00, home_acceleration); //Homing Acceleration

            drive.COMM.AsyncWrite(drive.SlaveNumber, 0x6098, 0x00, home_method); //Homing Method

            drive.SetControlWord(drive.CurrentCW, 0x00);
            await Task.Delay(10);
            drive.SetControlWord(drive.CurrentCW, 0x10);
        }

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
