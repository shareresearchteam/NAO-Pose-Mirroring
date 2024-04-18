using Kinect.Audio;
using Kinect.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace KinectCoordinateMapping
{
    /// <summary>
    /// Holds some commonly used Kinect constant values.
    /// </summary>
    public static class Constants
    {
        #region Constants

        /// <summary>
        /// Kinect DPI.
        /// </summary>
        public static readonly double DPI = 96.0;

        /// <summary>
        /// Default format.
        /// </summary>
        public static readonly PixelFormat FORMAT = PixelFormats.Bgr32;

        /// <summary>
        /// Bytes per pixel.
        /// </summary>
        public static readonly int BYTES_PER_PIXEL = (FORMAT.BitsPerPixel + 7) / 8;


        public static readonly List<string> JointNames = new List<string>() {
            "ShoulderLeft", "ElbowLeft", "WristLeft",
            "ShoulderRight", "ElbowRight", "WristRight",
            "HipLeft", "KneeLeft",
            "HipRight", "KneeRight",
            "Neck"
        };

        // List all onboard behaviors for lead actions
        public static readonly List<string> LeadActions = new List<string>()
        {
            //"leadactions-326f51/",
            "WavingArms",
            "HandJive",
            //"DiscoSit",
            "StretchSky",
            "PushPull",
            "TArms",
            "Macarena",
            "KarateKid",
            "DiagonalArms",
            "HeadBang",
            "HandsOutFlip",
            //"Salute",
            //"Wave",
        };

        #endregion
    }
}