using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect;
using System.Windows;

namespace Kinect.Model
{
    

    public class JointAngle
    {
        [JsonProperty("anglename")]
        public String AngleName { get; set; }
        [JsonProperty("value")]
        public double Value { get; set; }
    }

    public class JointList
    {
        [JsonProperty("jointnames")]
        public List<string> JointNames { get; set; }
        [JsonProperty("jointangles")]
        public List<double> JointAngles { get; set; }
        [JsonProperty("jointtimes")]
        public List<double> JointTimes { get {
                return Enumerable.Repeat(0.4, JointAngles.Count).ToList();
            }
        }

        public static JointList FromJointAngleList(List<JointAngle> jointAngles)
        {
            if(jointAngles.Count == 0) return null;

            return new JointList()
            {
                JointNames = jointAngles.Select(x => x.AngleName).ToList(),
                JointAngles = jointAngles.Select(x => x.Value).ToList()
            };
        }

        public void AddJointAngleRange(List<JointAngle> jointAngles)
        {
            if (jointAngles.Count == 0) return;

            this.JointNames.AddRange(jointAngles.Select(x => x.AngleName).ToList());
            this.JointAngles.AddRange(jointAngles.Select(x => x.Value).ToList());
        }

        public bool IsEmptyNull()
        {
            return this.JointNames == null || this.JointNames.Count <= 0;
        }
    }

    public class JointAnglesEventArgs : EventArgs
    {
#if KINECT2
        public IReadOnlyDictionary<JointType,Joint> SkelJoints { get; set; }
#else
        public JointCollection SkelJoints { get; set; }
#endif

        public JointList JointAngles { get
            {
                //List<JointAngle> jointAngles = new List<JointAngle>();
 
                // TODO: Neck/Head/hand?

                // Right Arm - NAO Left Arm
                //jointAngles.AddRange(JointConversions.GetArmAngles(this.SkelJoints[JointType.ShoulderRight], this.SkelJoints[JointType.ElbowRight], this.SkelJoints[JointType.WristRight], true));
                JointList jointAngles = JointList.FromJointAngleList(JointConversions.GetArmAngles(this.SkelJoints[JointType.ShoulderRight], this.SkelJoints[JointType.ElbowRight], this.SkelJoints[JointType.WristRight], true));
                //Console.WriteLine("Right Shoulder Pitch: " + SkelOrient[JointType.ShoulderLeft].Orientation.Pitch().ToString());
                //Console.WriteLine("Right Shoulder Roll: " + SkelOrient[JointType.ShoulderLeft].Orientation.Roll().ToString());
                //Console.WriteLine("Right Shoulder Yaw: " + SkelOrient[JointType.ShoulderLeft].Orientation.Yaw().ToString());
                // Left Arm - NAO Right Arm

                //jointAngles.AddRange(JointConversions.GetArmAngles(this.SkelJoints[JointType.ShoulderLeft], this.SkelJoints[JointType.ElbowLeft], this.SkelJoints[JointType.WristLeft], false));
                if (jointAngles != null) jointAngles.AddJointAngleRange(JointConversions.GetArmAngles(this.SkelJoints[JointType.ShoulderLeft], this.SkelJoints[JointType.ElbowLeft], this.SkelJoints[JointType.WristLeft], false));
                else jointAngles = JointList.FromJointAngleList(JointConversions.GetArmAngles(this.SkelJoints[JointType.ShoulderLeft], this.SkelJoints[JointType.ElbowLeft], this.SkelJoints[JointType.WristLeft], false));

                return jointAngles;
            } }
    }
}
