using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KinectCoordinateMapping
{
    public class Angle3
    {
        public double roll { get; set; }
        public double pitch { get; set; }
        public double yaw { get; set; }

        public double rollDegree { get { return roll * 180.0 / Math.PI; } }
        public double pitchDegree { get { return pitch * 180.0 / Math.PI; } }
        public double yawDegree { get { return yaw * 180.0 / Math.PI; } }

        public override string ToString()
        {
            return "roll: " + roll.ToString() + "\npitch: " + pitch.ToString() + "\nyaw: " + yaw.ToString();
        }
    }
}
