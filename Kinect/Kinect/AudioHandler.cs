using Kinect.Model;
using Kinect.Properties;
using KinectCoordinateMapping;
using Microsoft.Kinect;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Xml.Linq;
using System.Xml.Serialization;
using static System.Net.Mime.MediaTypeNames;

namespace Kinect.Audio
{
    // These are the different types of verbal cues and comments we provide,
    // structured into groups so we can easily call a cue at random from within a group
    // in order to get variable responses for particular comment needs
    public enum AudioType
    {
        // Tries to lure passerbies to come stretch/encourage spectators to engage
        DrawIn,
        // When NAO first begins its turn as leader
        LeadInitial,
        // Comments/encouragement while leading the movement (i.e. “you’re doing great”)
        LeadComment,
        // When we switch moves (as opposed to when we first start leading, which is the lead initial)
        TryNew,
        // I.e. “let’s try that again”; “one more time”
        TryRepeat,
        // When NAO first begins its turn as follower, or when we get a new follower
        FollowInitial,
        // Comments/encouragement while NAO is following the participant
        FollowComment,
        // Non-joke filler comments when things are silent(more generic or asking participant questions)
        SilenceFill,
        // All the more memorable joke/puns(must be good follow up to “wanna hear a joke?”)
        Joke,
        // Thanking participants/come back next time/etc
        Outtro,
        // Trivia Question!
        trivia,
    }

    // Basically, set up in case we want to change what voice we're running, though right now we only have masculine audio
    public enum VoiceType
    {
        Matt,
        Joanna
    }

    // This handles all verbal cues, so handles initializing and setting up individual groups, as well as specific individual audio cues
    public class AudioHandler : IEnumerable<AudioGroup>, IEnumerable
    {
        private readonly AudioGroup[] _audioGroupData;

        private VoiceType _voiceType;
        public VoiceType SelectedVoice
        {
            get { return this._voiceType; }
            set
            {
                this._voiceType = value;
                for (int i = 0;i < this._audioGroupData.Length;i++) 
                { this._audioGroupData[i].SelectedVoice = this._voiceType; }
            }
        }

        private string _lastAudio = "";

        public event EventHandler<RobotMessageEventArgs> SendMessageReady;
        private void SendAudio(string clipName, bool async=true)
        {
            this._lastAudio = clipName;
            NaoMsgPacket packet = new NaoMsgPacket()
            {
                Topic = "Audio",
                AudioString = clipName,
                AudioAsync = async
            };
            this.SendMessageReady?.Invoke(this, new RobotMessageEventArgs() { MessagePacket = packet });
        }

        public void RepeatAudio()
        {
            this.SendAudio(this._lastAudio);
        }

        // Basically, if one of our audio groups wants to play an audio, it goes here, then this sends it out properly to the data manager
        private void SendAudio_DataReady(object sender, RobotMessageEventArgs e)
        {
            this._lastAudio = e.MessagePacket.AudioString;
            this.SendMessageReady?.Invoke(this, e);
        }

        public AudioHandler(VoiceType startingVoiceType) 
        { 
            this._voiceType=startingVoiceType;
            //var audioTypes = Enum.GetValues(typeof(AudioType));
            //_audioGroupData = new AudioGroup[audioTypes.Length];
            var audiogr = ConfigurationManager.GetSection("AudioSettings/AudioGroups") as List<AudioTypeProperties>;
            Debug.WriteLine(ConfigurationManager.AppSettings.Count);
            // Generate out audio groups from the audio groups config setting stuff
            _audioGroupData = new AudioGroup[AudioSettings.audioSettings.AudioGroups.Count];
            for (int i = 0; i < this._audioGroupData.Length; i++)
            {
                _audioGroupData[i] = new AudioGroup(this._voiceType, AudioSettings.audioSettings.AudioGroups[i]);
                _audioGroupData[i].SendMessageReady += this.SendAudio_DataReady;
            }
        }


        public AudioGroup this[AudioType audioType]
        {
            get
            {
                return _audioGroupData[(int)audioType];
            }
            set
            {
                if (value.audioType != audioType)
                {
                    throw new ArgumentException();
                }

                _audioGroupData[(int)audioType] = value;
            }
        }


        public void SayWannaHearAJoke()
        {
            //NaoMsgPacket packet = new NaoMsgPacket();
            //packet.Topic = "Audio";
            //packet.AudioString = "wanna_hear_a_joke_" + this._voiceType.ToString() + ".mp3";
            this.SendAudio("wanna_hear_a_joke_" + this._voiceType.ToString() + ".mp3");
            //DataTransfer.sendMessage(packet);
        }
        public void SayIntro()
        {
            //NaoMsgPacket packet = new NaoMsgPacket();
            //packet.Topic = "Audio";
            //packet.AudioString = "intro_" + this._voiceType.ToString() + ".mp3";
            //DataTransfer.sendMessage(packet);
            this.SendAudio("intro_" + this._voiceType.ToString() + ".mp3");
        }

        public void SayHello()
        {
            //NaoMsgPacket packet = new NaoMsgPacket();
            //packet.Topic = "Audio";
            //packet.AudioString = "hello_" + this._voiceType.ToString() + ".mp3";
            //DataTransfer.sendMessage(packet);
            this.SendAudio("hello_" + this._voiceType.ToString() + ".mp3");
        }

        public void SayYes()
        {
            //NaoMsgPacket packet = new NaoMsgPacket();
            //packet.Topic = "Audio";
            //packet.AudioString = "yes_" + this._voiceType.ToString() + ".mp3";
            //DataTransfer.sendMessage(packet);
            this.SendAudio("yes_" + this._voiceType.ToString() + ".mp3");
        }

        public void SayNo()
        {
            //NaoMsgPacket packet = new NaoMsgPacket();
            //packet.Topic = "Audio";
            //packet.AudioString = "no_" + this._voiceType.ToString() + ".mp3";
            //DataTransfer.sendMessage(packet);
            this.SendAudio("no_" + this._voiceType.ToString() + ".mp3");
        }

        public void SayLaugh()
        {
            //NaoMsgPacket packet = new NaoMsgPacket();
            //packet.Topic = "Audio";
            //packet.AudioString = "no_" + this._voiceType.ToString() + ".mp3";
            //DataTransfer.sendMessage(packet);
            this.SendAudio("laugh_" + this._voiceType.ToString() + ".mp3");
        }

        //
        // Summary:
        //     This method is used to enumerate the list of audio groups.
        //
        // Returns:
        //     The related enumerator.
        public IEnumerator GetEnumerator()
        {
            return _audioGroupData.GetEnumerator();
        }

        //
        // Summary:
        //     This method is used to enumerate the list of audio groups.
        //
        // Returns:
        //     The related enumerator.
        IEnumerator<AudioGroup> IEnumerable<AudioGroup>.GetEnumerator()
        {
            return ((IEnumerable<AudioGroup>)_audioGroupData).GetEnumerator();
        }

    }

    // This handles making sure we randomly grab an audio cue, and can make sure we go through all cues before repeating one
    public class AudioGroup : CycleQueue
    {
        public AudioType audioType { get; private set; }
        // this is the last part of the audio file name, which includes the string version of the specific voice
        private string _nameEnd;
        private VoiceType _voiceType;
        public VoiceType SelectedVoice
        { get { return this._voiceType; }
            set
            {
                this._voiceType = value;
                this._nameEnd = "_" + this.SelectedVoice.ToString() + ".mp3";
            }
        }

        private int numClips;
        public int NumClips {
            get { return this.numClips; }
            set
            {
                this.numClips = value;
                this.BuildAudioList();
            }
        }

        // Should we play this audio even while doing movement commands? or should it block doing other actions until it finishes?
        public bool PlayAsync { get; private set; }

        // Is this a call response audio type that has a set of initial and follow up audio linked?
        public bool IsCallResponse { get; private set; }


        // If we've started a call, save it, so we play the right response
        private string _currentCall = "";
        // Tracking if we're currently in an audio call and need to play the response next
        private bool _needToRespond = false;
        public bool IsResponseNext { get { return this._needToRespond; } }


        private Random rng;

        public AudioGroup(AudioType audioType, VoiceType voiceType, int numClips, bool playAsync = true, bool callResponse = false) 
        {
            this.audioType = audioType;
            this.numClips = numClips;
            this._voiceType = voiceType;
            this.PlayAsync = playAsync;
            this.IsCallResponse = callResponse;
            this.rng = new Random();
            this.BuildAudioList();
        }

        public AudioGroup(VoiceType voiceType, AudioTypeProperties audioTypeProperties)
        {
            this._voiceType = voiceType;
            this.audioType = audioTypeProperties.Type;
            this.numClips = audioTypeProperties.ClipCount;
            this.PlayAsync = audioTypeProperties.IsAudioAsync;
            this.IsCallResponse = audioTypeProperties.IsCallResponse;
            this.rng = new Random();
            this.BuildAudioList();
        }

        public event EventHandler<RobotMessageEventArgs> SendMessageReady;
        private void SendAudio(string clipName)
        {
            NaoMsgPacket packet = new NaoMsgPacket()
            {
                Topic = "Audio",
                AudioString = clipName,
                AudioAsync = this.PlayAsync
            };
            this.SendMessageReady?.Invoke(this, new RobotMessageEventArgs() { MessagePacket = packet });
        }

        private void BuildAudioList()
        {
            var nameStarter = this.audioType.ToString() + "_";
            this._nameEnd = "_" + this.SelectedVoice.ToString() + ".mp3";
            this.itemList = new List<string>();
            for (int i = 0; i < this.numClips; i++)
            {
                this.itemList.Add(nameStarter + i.ToString());
            }
            this.ResetQueue();
        }

        
        public void PlayAudio()
        {
            if(!this.IsCallResponse)
            {
                this._currentCall = this.GetNewItem();
                this.SendAudio(this._currentCall + this._nameEnd);
                return;
            }
            if (this._needToRespond)
            {
                this._needToRespond = false;
                this.SendAudio(this._currentCall + "_answer" + this._nameEnd);
            }
            else
            {
                this._currentCall = this.GetNewItem();
                this.SendAudio(this._currentCall + "_question" + this._nameEnd);
                this._needToRespond = true;
            }
        }

        /// <summary>
        /// Play an audio in this category with a probability given by probability (from 0.0 - 1.0)
        /// Default is 0.5 (aka, 50%), and returns true if it did play audio
        /// If the audio group is call-response style, it will just not play anything
        /// </summary>
        /// <param name="probability"></param>
        /// <returns>True if played audio</returns>
        public bool PlayAudioMaybe(double probability = 0.5)
        {
            if(this.IsCallResponse) { return false; }

            var check = this.rng.NextDouble();
            if (check < probability) {
                this._currentCall = this.GetNewItem();
                this.SendAudio(this._currentCall + this._nameEnd);
                return true;
            }
            return false;
        }
    }
}
