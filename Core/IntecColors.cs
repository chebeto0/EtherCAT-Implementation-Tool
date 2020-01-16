using System.Collections.Generic;
using System.Windows.Media;

namespace EtherCAT_Master.Core
{
    
    static class IntecColors
    {

        //Official Intec colors
        public static readonly Color light_grey = new Color() { A=255, R=143, G=158, B=165 };
        public static readonly Color dark_grey = new Color() { A=255, R=27, G=47, B=53 };
        public static readonly Color blue = new Color() { A=255, R=70, G=149, B=188 };
        public static readonly Color dark_blue = new Color() { A=255, R=0, G=70, B=114 };
        public static readonly Color white = new Color() { A=255, R=255, G=255, B=255 };//""
        public static readonly Color bg_grey = new Color() { A=255, R=0xf4, G=0xf6, B=0xf7 };//""

        //Not official Intec colors
        public static readonly Color green = new Color() { A=255, R=46, G=204, B=113 };
        public static readonly Color darkish_green = new Color() { A=255, R=30, G=180, B=80 };
        public static readonly Color light_red = new Color() { A=255, R=231, G=76, B=60 };
        public static readonly Color dark_red = new Color() { A=255, R=192, G=57, B=43 };
        public static readonly Color orange = new Color() { A=255, R=230, G=126, B=34 };
        public static readonly Color yellow = new Color() { A=255, R=241, G=196, B=15 };

        // List for Plot Colors
        public static readonly List<Color> plotColors = new List<Color> { dark_blue, dark_red, darkish_green };

    }
}
