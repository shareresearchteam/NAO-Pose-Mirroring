using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Kinect.Audio;
using Kinect.Model;
using KinectCoordinateMapping;
using Microsoft.Kinect;

namespace Kinect
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public SkeletonBase drawSkeleton { get; set; }
        //public ViewerWindow viewWindow { get; set; }

        public BehaviorManager BehaviorManager { get; set; }

        public DataTransferManager DataTransferManager { get; set; }

        public System.Collections.ObjectModel.ObservableCollection<string> logEntries { get; set; }
        public MainWindow()
        {
            InitializeComponent();
            // Giving feedback on connection stuff
            this.logEntries = new ObservableCollection<string>();
            this.DataTransferManager = new DataTransferManager();



            // Our back end data handling for the skeleton posing, if we're using a 
            // v2 kinect, we'll use the SkeletonDatav2 version of the SkeletonBase abstract class
            // Otherwise we'll use the SkeletonDataV1 version
#if KINECT2
            this.drawSkeleton = new SkeletonDataOne();
#else
            this.drawSkeleton = new SkeletonData360();
#endif

            this.BehaviorManager = new BehaviorManager();
            this.BehaviorManager.SendMessageReady += this.DataTransferManager.sendMessage_DataReady;

            // Additional display window
            //this.viewWindow = new ViewerWindow(this.imgCamera, this.CnvSkeleton);
            //this.viewWindow.Show();
            //this.CnvSkeleton.SourceUpdated += CnvSkeleton_SourceUpdated;
            //this.imgCamera.SourceUpdated += ImgCamera_SourceUpdated;
            this.drawSkeleton.TrackedSkeletonUpdated += DrawSkeleton_TrackedSkeletonUpdated;
            this.drawSkeleton.JointAnglesReady += this.BehaviorManager.JointAngle_DataReady;

            // Setting up combo box lists
            //this.comboActions.ItemsSource = KinectCoordinateMapping.Constants.LeadActions;
            //this.comboActions.SelectedIndex = 0;
            //this.comboAudio.ItemsSource = KinectCoordinateMapping.Constants.AudioFiles;
            //this.comboAudio.SelectedIndex = 0;

            // Setting up logs
            this.logEntries.Add("Ready to Connect");
            this.listLogs.ItemsSource = logEntries;

            // Setting up tracked skeleton and button texts
            this.txtSkel1.Text = this.drawSkeleton.TrackedSkeleton;
            this.btnLast.Content = char.ConvertFromUtf32(0x2190);
            this.btnNext.Content = char.ConvertFromUtf32(0x2192);

            // Setting up all our buttons and controls enable/disable state
            this.btnStart.IsEnabled = false;
            this.btnStop.IsEnabled = false;
            this.btnMattAudio.IsEnabled = false;
            this.btnRepeatLead.IsEnabled = false;
            this.panelFollowControls.IsEnabled = false;
            this.panelFollowControls.Visibility = Visibility.Collapsed;
            this.panelMovementControls.IsEnabled = false;
            this.panelMovementControls.Visibility = Visibility.Collapsed;

            // The bits that aren't ready yet
            this.btnJoannaAudio.IsEnabled = false;
            this.btnWave.IsEnabled = false;
            this.btnRight.IsEnabled = false;
            this.btnLeft.IsEnabled = false;

            _ = this.ConnectToNao();
        }

        private async Task ConnectToNao()
        {
            await this.DataTransferManager.DiscoverMeProcessAsync();
            //this.DataTransferManager.robotIPAddress = System.Net.IPAddress.Parse("127.0.0.1");
            // Now that we have an ip address, we can actually start the system
            this.btnStart.IsEnabled = true;
        }

        private void DrawSkeleton_TrackedSkeletonUpdated(object sender, EventArgs e)
        {
            this.txtSkel1.Text = this.drawSkeleton.TrackedSkeleton;
            if (this.BehaviorManager.RunMode == RunMode.Following)
            {
                this.DataTransferManager.logMessage("Tracking new skeleton: " + this.drawSkeleton.TrackedSkeleton);
            }
        }

        private void ImgCamera_SourceUpdated(object sender, DataTransferEventArgs e)
        {
            //throw new NotImplementedException();
            //this.viewWindow.UpdateImg(this.imgCamera);
        }

        private void CnvSkeleton_SourceUpdated(object sender, DataTransferEventArgs e)
        {
            //throw new NotImplementedException();
            //this.viewWindow.UpdateCanvas(this.CnvSkeleton);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.drawSkeleton.AddViewWindow(CnvSkeleton, imgCamera);
            //this.drawSkeleton.AddViewWindow(this.viewWindow.CnvSkeleton, this.viewWindow.imgCamera);
            this.drawSkeleton.InitializeSensorAndSkeleton();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.drawSkeleton.StopSensorAndSkeleton();
            //this.viewWindow.Close();
        }

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            this.logEntries.Add("Waking NAO");
            this.BehaviorManager.RunMode = RunMode.Awake;
            this.panelMovementControls.Visibility = Visibility.Visible;
            this.btnStart.IsEnabled = false;
            this.btnStop.IsEnabled = true;
            this.panelMovementControls.IsEnabled = true;
        }

        private void btnRandomLead_Click(object sender, RoutedEventArgs e)
        {
            this.BehaviorManager.RunMode = RunMode.Leading;
            this.panelFollowControls.IsEnabled = false;
            this.panelFollowControls.Visibility = Visibility.Collapsed;
            this.btnRepeatLead.IsEnabled = true;
            this.logEntries.Add("Start lead act " + this.BehaviorManager.ActionManager.LastAction);
        }

        private void btnRepeatLead_Click(object sender, RoutedEventArgs e)
        {
            this.panelFollowControls.IsEnabled = false;
            this.panelFollowControls.Visibility = Visibility.Collapsed;
            string currentAct = this.BehaviorManager.RepeatLeadAction();
            this.logEntries.Add("Start lead act " + currentAct);
        }

        private void btnFollow_Click(object sender, RoutedEventArgs e)
        {
            this.BehaviorManager.RunMode = RunMode.Following;
            this.logEntries.Add("Following new person");
            this.panelFollowControls.IsEnabled = true;
            this.panelFollowControls.Visibility = Visibility.Visible;
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            this.BehaviorManager.RunMode = RunMode.Deactivated;
            this.btnStart.IsEnabled = true;
            this.btnStop.IsEnabled = true;
            this.panelMovementControls.Visibility = Visibility.Collapsed;
            this.panelMovementControls.IsEnabled = false;
            this.logEntries.Add("Sleeping NAO");
        }

        // TODO: make the audio be selecting a type, and then it plays one from that type, for simplicity
        private void btnMattAudio_Click(object sender, RoutedEventArgs e)
        {
            this.btnMattAudio.IsEnabled = false;
            this.btnJoannaAudio.IsEnabled = true;
            this.BehaviorManager.AudioCollection.SelectedVoice = Audio.VoiceType.Matt;
            //this.comboAudio.ItemsSource = KinectCoordinateMapping.Constants.AudioFiles;
            //this.comboAudio.SelectedIndex = 0;
        }
        private void btnJoannaAudio_Click(object sender, RoutedEventArgs e)
        {
            //this.btnMattAudio.IsEnabled = true;
            //this.btnJoannaAudio.IsEnabled = false;
            //this.BehaviorManager.AudioCollection.SelectedVoice = Audio.VoiceType.Joanna;
            //this.comboAudio.ItemsSource = KinectCoordinateMapping.Constants.AudioFilesJoanna;
            //this.comboAudio.SelectedIndex = 0;
        }
        private void btnPlayAudio_Click(object sender, RoutedEventArgs e)
        {
            NaoMsgPacket packet = new NaoMsgPacket();
            packet.Topic = "Audio";
            //packet.AudioString = this.comboAudio.SelectedItem.ToString();
            this.DataTransferManager.sendMessage(packet);
            //this.drawSkeleton.PlayAudio(this.comboAudio.SelectedItem.ToString());
            this.logEntries.Add("Playing Audio");
        }

        private void btnSayAudio_Click(object sender, RoutedEventArgs e)
        {
            NaoMsgPacket packet = new NaoMsgPacket();
            packet.Topic = "Say";
            packet.AudioString = this.textBoxSay.Text;
            this.DataTransferManager.sendMessage(packet);
            //this.drawSkeleton.SayAudio(this.textBoxSay.Text);
            this.logEntries.Add("Say text: " + this.textBoxSay.Text);
            this.textBoxSay.Text = "";
        }


        private void btnSkel1Update_Click(object sender, RoutedEventArgs e)
        {
            this.drawSkeleton.TrackedSkeleton = this.txtSkel1.Text;
        }

        private void btnLast_Click(object sender, RoutedEventArgs e)
        {
            this.drawSkeleton.CycleSkeleton(false);
            this.logEntries.Add("Tracking skeleton: " + this.drawSkeleton.TrackedSkeleton);
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            this.drawSkeleton.CycleSkeleton(true);
            this.logEntries.Add("Tracking skeleton: " + this.drawSkeleton.TrackedSkeleton);
        }

        private void btnSalute_Click(object sender, RoutedEventArgs e)
        {
            this.BehaviorManager.ActionManager.Salute();
        }

        private void btnWave_Click(object sender, RoutedEventArgs e)
        {
            this.BehaviorManager.ActionManager.Wave();
        }

        private void btnRight_Click(object sender, RoutedEventArgs e)
        {
            this.BehaviorManager.ActionManager.LookRight();
        }

        private void btnLeft_Click(object sender, RoutedEventArgs e)
        {
            this.BehaviorManager.ActionManager.LookLeft();
        }

        private void btnNod_Click(object sender, RoutedEventArgs e)
        {
            this.BehaviorManager.ActionManager.HeadNod();
        }

        private void btnShake_Click(object sender, RoutedEventArgs e)
        {
            this.BehaviorManager.ActionManager.HeadShake();
        }

        private void btnViewLeft_Click(object sender, RoutedEventArgs e)
        {
            this.BehaviorManager.ActionManager.GazeLeft();
        }

        private void btnViewSlightLeft_Click(object sender, RoutedEventArgs e)
        {
            this.BehaviorManager.ActionManager.GazeSlightLeft();
        }

        private void btnViewMiddle_Click(object sender, RoutedEventArgs e)
        {
        }

        private void btnViewSlightRight_Click(object sender, RoutedEventArgs e)
        {
            this.BehaviorManager.ActionManager.GazeSlightRight();
        }

        private void btnViewRight_Click(object sender, RoutedEventArgs e)
        {
            this.BehaviorManager.ActionManager.GazeRight();
        }


        // Audio

        private void btnHello_Click(object sender, RoutedEventArgs e)
        {
            this.BehaviorManager.AudioCollection.SayHello();
        }

        private void btnYes_Click(object sender, RoutedEventArgs e)
        {
            this.BehaviorManager.AudioCollection.SayYes();
        }

        private void btnNo_Click(object sender, RoutedEventArgs e)
        {
            this.BehaviorManager.AudioCollection.SayNo();
        }

        private void btnIntro_Click(object sender, RoutedEventArgs e)
        {
            this.BehaviorManager.AudioCollection.SayIntro();
        }

        private void btnDrawIn_Click(object sender, RoutedEventArgs e)
        {
            this.BehaviorManager.AudioCollection[AudioType.DrawIn].PlayAudio();
        }

        private void btnJokePrimer_Click(object sender, RoutedEventArgs e)
        {
            this.BehaviorManager.AudioCollection.SayWannaHearAJoke();
        }

        private void btnJoke_Click(object sender, RoutedEventArgs e)
        {
            this.BehaviorManager.AudioCollection[AudioType.Joke].PlayAudio();
            //this.logEntries.Add("Playing Joke");
        }

        private void btnSilenceFill_Click(object sender, RoutedEventArgs e)
        {
            this.BehaviorManager.AudioCollection[AudioType.SilenceFill].PlayAudio();
        }

        private void btnOuttro_Click(object sender, RoutedEventArgs e)
        {
            this.BehaviorManager.AudioCollection[AudioType.Outtro].PlayAudio();
        }

        private void btnFollowInitial_Click(object sender, RoutedEventArgs e)
        {
            this.BehaviorManager.AudioCollection[AudioType.FollowInitial].PlayAudio();
        }

        private void btnFollowComment_Click(object sender, RoutedEventArgs e)
        {
            this.BehaviorManager.AudioCollection[AudioType.FollowComment].PlayAudio();
        }

        private void btnLeadComment_Click(object sender, RoutedEventArgs e)
        {
            this.BehaviorManager.AudioCollection[AudioType.LeadComment].PlayAudio();
        }


        private void btnRepeatAudio_Click(object sender, RoutedEventArgs e)
        {
            this.BehaviorManager.AudioCollection.RepeatAudio();
        }

        private void btnLaugh_Click(object sender, RoutedEventArgs e)
        {
            this.BehaviorManager.AudioCollection.SayLaugh();
        }

        private void btnStopFollow_Click(object sender, RoutedEventArgs e)
        {
            this.BehaviorManager.RunMode = RunMode.Awake;
        }

    }
}
