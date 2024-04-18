using Kinect.Audio;
using Kinect.Model;
using KinectCoordinateMapping;
using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace Kinect
{
    public enum RunMode
    {
        Deactivated = -1,
        Awake = 0,
        Leading = 1,
        Following = 2,
    }
    public class BehaviorManager
    {
        public AudioHandler AudioCollection { get; set; }
        public ActionHandler ActionManager { get; set; }

        //timer
        private Timer timer = new Timer();

        private JointAnglesEventArgs jointAnglesList = null;

        // which run mode we're in
        // -1 - go to sleep
        // 0 - wake up
        // 1 - leader
        // 2 - follower
        private RunMode runMode = RunMode.Deactivated;
        public RunMode RunMode
        {
            get { return this.runMode; }
            set
            {
                switch(value)
                {
                    case RunMode.Leading:
                        if (this.timer.Enabled == true) { this.timer.Dispose(); }
                        if (this.runMode != RunMode.Leading)
                        {
                            this.AudioCollection[AudioType.LeadInitial].PlayAudio();
                            this.runMode = RunMode.Leading;
                        }
                        // If we don't play a try new audio, then maybe play a lead comment
                        else if (!this.AudioCollection[AudioType.TryNew].PlayAudioMaybe())
                        {
                            this.AudioCollection[AudioType.LeadComment].PlayAudioMaybe(0.3);
                        }
                        this.ActionManager.StartNewLead();
                        break;
                    case RunMode.Following:
                        if (this.runMode != RunMode.Following)
                        {
                            this.AudioCollection[AudioType.FollowInitial].PlayAudio();
                            this.runMode = RunMode.Following;
                        }
                        if (this.timer.Enabled == true) { this.timer.Dispose(); }
                        this.InitTimer();
                        this.UpdateMode();
                        break;
                    case RunMode.Awake:
                        if (this.timer.Enabled == true) { this.timer.Dispose(); }
                        this.runMode = RunMode.Awake;
                        this.UpdateMode();
                        //this.DataTransferManager.newMode(this.runMode);
                        break;
                    default:
                        if (this.timer.Enabled == true) { this.timer.Dispose(); }
                        this.runMode = RunMode.Deactivated;
                        this.UpdateMode();
                        //this.DataTransferManager.newMode(this.runMode);
                        break;
                }
            }
        }

        public BehaviorManager()
        {
            this.AudioCollection = new AudioHandler(VoiceType.Matt);
            this.ActionManager = new ActionHandler(Constants.LeadActions);
            this.AudioCollection.SendMessageReady += this.SendMessage_DataReady;
            this.ActionManager.SendMessageReady += this.SendMessage_DataReady;
            // Use this to let NAO find the IP address of the computer
            //_ = DataTransfer.DiscoverMeProcessAsync();
        }

        ~BehaviorManager()
        {
            StopTimer();
        }

        public event EventHandler<RobotMessageEventArgs> SendMessageReady;
        private void UpdateMode()
        {
            NaoMsgPacket packet = new NaoMsgPacket()
            {
                Topic = "Mode",
                Mode = (int)this.runMode,
            };
            this.SendMessageReady?.Invoke(this, new RobotMessageEventArgs() { MessagePacket = packet });
        }

        private void SendMessage_DataReady(object sender, RobotMessageEventArgs e)
        {
            this.SendMessageReady?.Invoke(this, e);
        }

        public string RepeatLeadAction()
        {
            this.AudioCollection[AudioType.TryRepeat].PlayAudioMaybe();
            //string currentAct = this.drawSkeleton.RepeatLead();
            return this.ActionManager.RepeatLead();
        }

        //timer start
        private void InitTimer(int packetTime = 300)
        {
            Debug.WriteLine("-----Timer Started------");
            this.timer = new Timer(packetTime);
            this.timer.Elapsed += FollowTimer_Elapsed;
            this.timer.Enabled = true;
            this.timer.AutoReset = true;
        }

        //timer stop
        private void StopTimer()
        {
            if (this.timer.Enabled == true) { this.timer.Dispose(); }
            Debug.WriteLine("-----Timer Stopped-----");
        }

        //timer for sending joints, so we don't overload the system
        private void FollowTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            // TODO: When sending joints, structure them with joint name list ahead of time so we don't need to order them on NAO

            // If we're not pose following, we don't care about the timer
            if (this.runMode != RunMode.Following || this.jointAnglesList == null)
            {
                return;
            }
            //List<JointAngle> jointAngles = this.jointAnglesList.JointAngles;
            JointList jointAngles = this.jointAnglesList.JointAngles;
            if (jointAngles == null) { return; }

            this.jointAnglesList = null;

            NaoMsgPacket packet = new NaoMsgPacket()
            {
                Topic = "Joints",
                Mode = (int)this.runMode,
                AngleList = jointAngles
            };
            this.SendMessageReady?.Invoke(this, new RobotMessageEventArgs() { MessagePacket = packet });
            //this.logEntries.Add("Sent skeleton to python");
            Debug.WriteLine("Sent skeleton to python");

        }

        public void JointAngle_DataReady(object sender, JointAnglesEventArgs e)
        {
            this.jointAnglesList = e;
        }

    }
    public class ActionHandler : CycleQueue
    {
        private string lastAction = "";
        public string LastAction { get { return lastAction; } }
        private string _customBehaviorsID;

        
        public ActionHandler(List<string> item_list)
        {
            var behaviorSettings = (NameValueCollection)ConfigurationManager.GetSection("BehaviorSettings");
            this._customBehaviorsID = behaviorSettings.Get("CustomBehaviorsID");
            this.itemList = item_list.ToList();
            this.ResetQueue();
        }

        public event EventHandler<RobotMessageEventArgs> SendMessageReady;
        private void SendAction(string action)
        {
            NaoMsgPacket packet = new NaoMsgPacket()
            {
                Topic = "Mode",
                Mode = (int)RunMode.Leading,
                BehaviorName = this._customBehaviorsID + action
            };
            this.SendMessageReady?.Invoke(this, new RobotMessageEventArgs() { MessagePacket = packet });
        }

        public string RepeatLead()
        {
            if(this.lastAction != "" && this.itemList.Contains(this.lastAction))
            {
                this.SendAction(this.lastAction);
                //this.DataTransferManager.runBehavior(this.lastAction);
                return this.lastAction;
            }
            return "None";
        }

        public string StartNewLead()
        {
            this.lastAction = this.GetNewItem();
            this.SendAction(this.lastAction);
            return this.lastAction;
        }

        public void Salute()
        {
            this.SendAction("Salute");
        }

        public void Wave()
        {
            this.SendAction("Wave");
        }
        public void LookRight()
        {
            this.SendAction("LookRight");
            //DataTransfer.runBehavior("LookRight");
        }
        public void LookLeft()
        {
            this.SendAction("LookLeft");
            //DataTransfer.runBehavior("LookLeft");
        }
        public void HeadNod()
        {
            this.SendAction("HeadNod");
            //DataTransfer.runBehavior("HeadNod");
        }
        public void HeadShake()
        {
            this.SendAction("HeadShake");
            //DataTransfer.runBehavior("HeadShake");
        }

        public void GazeRight()
        {
            this.SendAction("GazeRight");
        }

        public void GazeSlightRight()
        {
            this.SendAction("GazeSlightRight");
        }

        public void GazeLeft()
        {
            this.SendAction("GazeLeft");
        }

        public void GazeSlightLeft()
        {
            this.SendAction("GazeSlightLeft");
        }


    }
}
