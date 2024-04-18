using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Kinect.Model
{
    public class RobotMessageEventArgs : EventArgs
    {
        public NaoMsgPacket MessagePacket { get; set; }

    }
    public class NaoMsgPacket
    {
        /// <summary>
        /// What type of data is in the packet?
        /// Joints == Has jointlist angles
        /// Mode == We're changing system mode
        /// Audio == We want to play an audio recording
        /// Say == We want to say some text
        /// </summary>
        [JsonProperty("topic")]
        public String Topic { get; set; }

        /// <summary>
        /// What mode are we changing too?
        /// 0 == Start a new action as leader
        /// 1 == Begin following a new person
        /// </summary>
        [JsonProperty("mode")]
        public int Mode { get; set; }

        [JsonProperty("behavior")]
        public string BehaviorName { get; set; }

        /// <summary>
        /// If we want to log a message in the nao log!
        /// </summary>
        [JsonProperty("logmsg")]
        public string LogMessage { get; set; }

        /// <summary>
        /// The joint angles to set the robot angles too
        /// This is where we do all our joint math in c# land
        /// Only set during following phase
        /// </summary>
        [JsonProperty("anglelist")]
        public JointList AngleList { get; set; }

        /// <summary>
        /// Run an audio file on the robot
        /// Using the audio name provided
        /// </summary>
        [JsonProperty("audiostring")]
        public string AudioString { get; set; }

        /// <summary>
        /// Should the audio be played asynchronously?
        /// </summary>
        [JsonProperty("audioasync")]
        public bool AudioAsync { get; set; }
    }
}
