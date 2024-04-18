#if !KINECT2
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Kinect.Model;
using KinectCoordinateMapping;
using Microsoft.Kinect;
using Newtonsoft.Json;
using static System.Net.Mime.MediaTypeNames;
using Kinect.Audio;
using Newtonsoft.Json.Linq;

namespace Kinect
{

    public class SkeletonData360 : SkeletonBase
    {
        //property voor kinectsensor 
        public KinectSensor Sensor;

        //array van skeletons 
        public Skeleton[] Skeletons;


        // image voor colorstream
        System.Windows.Controls.Image image;

        //canvas om skelet op te tekenen
        System.Windows.Controls.Canvas canvas;


        //public List<KeyFrame> SkeletonDataList = new List<KeyFrame>();

        public List<int> trackedSkels = new List<int>();
        private int trackedSkeleton = -1;
        public override string TrackedSkeleton
        {
            get { return this.trackedSkeleton.ToString(); }
            set {
                if(Int32.TryParse(value, out int j))
                {
                    this.trackedSkeleton = j;
                    base.OnTrackedSkeletonUpdated(new EventArgs());
                }
            }
        }

        
        //public System.Collections.ObjectModel.ObservableCollection<string> logEntries;

        //dictionary voor het tekenen van de bones (zal joint name en zijn position bevatten)
        public Dictionary<string, Point> dictionary = new Dictionary<string, Point>();


        public SkeletonData360()
        {

        }

        private void ColorSkeletonData_TrackedSkeletonUpdated(object sender, EventArgs e)
        {
            if (this.trackedSkeleton == -1) { return; }
            if (this.Sensor != null && this.Sensor.SkeletonStream != null)
            {
                if (!this.Sensor.SkeletonStream.AppChoosesSkeletons)
                {
                    this.Sensor.SkeletonStream.AppChoosesSkeletons = true;
                }
                this.Sensor.SkeletonStream.ChooseSkeletons(this.trackedSkeleton);

                
            }
        }


        // TODO: let's rework this, so it actually grabs the left most skeleton to start,
        // And then you go right or left depending on button presses, and it gets the one that positionally
        // is more right or left
        public override void CycleSkeleton(bool right = true)
        {
            if (this.Sensor != null && this.Sensor.SkeletonStream != null)
            {
                if (!this.Sensor.SkeletonStream.AppChoosesSkeletons)
                {
                    this.Sensor.SkeletonStream.AppChoosesSkeletons = true;
                }

                var viableSkels = this.Skeletons.Where(s => s.TrackingState != SkeletonTrackingState.NotTracked);
                if (viableSkels.Count() < 1) { return; }
                var sortedSkels = viableSkels.OrderBy(s => s.Position.X).ToList();
                int currentIndex = 0;
                // Try and get the index of the one we're currently tracking... 
                try
                {
                    currentIndex = sortedSkels.FindIndex(s => s.TrackingId == this.trackedSkeleton);
                }
                // If that fails cause our tracking id is wrong then try and get it from the skeleton tracked state
                catch
                {
                    try
                    {
                        currentIndex = sortedSkels.FindIndex(s => s.TrackingState == SkeletonTrackingState.Tracked);
                    }
                    // If none of them are tracked, then just return
                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
                        return;
                    }
                }
                if (right)
                {
                    currentIndex = currentIndex + 1;
                    if (currentIndex >= sortedSkels.Count) { currentIndex = 0; }
                }
                else
                {
                    currentIndex = currentIndex - 1;
                    if (currentIndex < 0) { currentIndex = sortedSkels.Count - 1; }
                }
                this.trackedSkeleton = sortedSkels[currentIndex].TrackingId;
                base.OnTrackedSkeletonUpdated(new EventArgs());
            }
        }

        public override void InitializeSensorAndSkeleton()
        {

            // handling with a single sensor lol
            // We're gonna need to tweak this if we want to have multiple sensors in use.
            // TODO: adjust this so we can actually have multiple sensors
            foreach (var potentialSensor in KinectSensor.KinectSensors)
            {
                if (potentialSensor.Status == KinectStatus.Connected)
                {
                    this.Sensor = potentialSensor;
                    break;
                }
            }


            if (this.Sensor != null)
            {
                this.Sensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Seated;
                TransformSmoothParameters smoothingParam = new TransformSmoothParameters();
                {
                    smoothingParam.Smoothing = 0.5f;
                    smoothingParam.Correction = 0.5f;
                    smoothingParam.Prediction = 0.5f;
                    smoothingParam.JitterRadius = 0.05f;
                    smoothingParam.MaxDeviationRadius = 0.04f;
                };

                //kleur aanzetten
                this.Sensor.ColorStream.Enable();

                // dit start skeletontracking
                this.Sensor.SkeletonStream.Enable(smoothingParam);
                this.Skeletons = new Skeleton[this.Sensor.SkeletonStream.FrameSkeletonArrayLength];


                //event op colorstream
                Sensor.ColorFrameReady += Sensor_ColorFrameReady;

                //event op skeletonstream
                Sensor.SkeletonFrameReady += Sensor_SkeletonFrameReady;

                this.TrackedSkeletonUpdated += ColorSkeletonData_TrackedSkeletonUpdated;

                //If the sensor doesn't start, then set it to null 
                try
                {
                    this.Sensor.Start();
                }
                catch (IOException)
                {
                    this.Sensor = null;
                }

            }
        }

        private void ClearCanvases()
        {
            foreach (Canvas canvas in this.canvasList)
            {
                canvas.Children.Clear();
            }
        }

        private void Sensor_ColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            using (var frame = e.OpenColorImageFrame())
            {
                if (frame != null)
                {
                    foreach(System.Windows.Controls.Image img in this.imgList)
                    {
                        img.Source = frame.ToBitmap();
                    }
                }
            }
        }

        public override void StopSensorAndSkeleton()
        {
            if (this.Sensor != null)
            {
                this.Sensor.Stop();
            }
        }

        // Event handling for whenever we get new skeleton frame data
        private void Sensor_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame()) //skelet openen
            {                
                if (skeletonFrame != null) // checken of een frame beschikbaar is 
                {

                    //canvas.Children.Clear();
                    ClearCanvases();

                    //this.Skeletons = new Skeleton[skeletonFrame.SkeletonArrayLength];
                    skeletonFrame.CopySkeletonDataTo(this.Skeletons); // skelet informatie van het frame bemachtigen
                    this.trackedSkels = new List<int>();
                    foreach (Skeleton skel in Skeletons)
                    {
                        if (skel.TrackingState != SkeletonTrackingState.NotTracked)
                        {
                            trackedSkels.Add(skel.TrackingId);
                        }
                        if (skel.TrackingState == SkeletonTrackingState.Tracked)
                        {
                            if (this.trackedSkeleton == -1) { 
                                this.trackedSkeleton = skel.TrackingId;
                                base.OnTrackedSkeletonUpdated(new EventArgs());
                            }
                            
                            if (this.trackedSkeleton == skel.TrackingId) // && this.runMode == RunMode.Following)
                            {
                                /*
                                // Right Arm - NAO Left Arm
                                rShoulderPitch.ComputeAverage(JointConversions.AngleShoulderPitch(skel.Joints[JointType.ShoulderRight], skel.Joints[JointType.ElbowRight], true));
                                rShoulderRoll.ComputeAverage(JointConversions.AngleShoulderRoll(skel.Joints[JointType.ShoulderRight], skel.Joints[JointType.ElbowRight], true));
                                rElbowRoll.ComputeAverage(JointConversions.AngleElbowRoll(skel.Joints[JointType.ShoulderRight], skel.Joints[JointType.ElbowRight], skel.Joints[JointType.WristRight], true));
                                rElbowYaw.ComputeAverage(JointConversions.AngleElbowYaw(skel.Joints[JointType.ShoulderRight], skel.Joints[JointType.ElbowRight], skel.Joints[JointType.WristRight], true));

                                // Left Arm - NAO Right Arm
                                lShoulderPitch.ComputeAverage(JointConversions.AngleShoulderPitch(skel.Joints[JointType.ShoulderLeft], skel.Joints[JointType.ElbowLeft], false));
                                lShoulderRoll.ComputeAverage(JointConversions.AngleShoulderRoll(skel.Joints[JointType.ShoulderLeft], skel.Joints[JointType.ElbowLeft], false));
                                lElbowRoll.ComputeAverage(JointConversions.AngleElbowRoll(skel.Joints[JointType.ShoulderLeft], skel.Joints[JointType.ElbowLeft], skel.Joints[JointType.WristLeft], false));
                                lElbowYaw.ComputeAverage(JointConversions.AngleElbowYaw(skel.Joints[JointType.ShoulderLeft], skel.Joints[JointType.ElbowLeft], skel.Joints[JointType.WristLeft], false));
                                */
                                base.OnJointAnglesReady(new JointAnglesEventArgs() { SkelJoints = skel.Joints });
                            }
                            
                            /*
                            Debug.WriteLine("Original Method:");
                            Debug.WriteLine("roll: " + JointConversions.AngleShoulderRoll(skel.Joints[JointType.ShoulderRight], skel.Joints[JointType.ElbowRight], false).ToString());
                            Debug.WriteLine("pitch: " + JointConversions.AngleShoulderPitch(skel.Joints[JointType.ShoulderRight], skel.Joints[JointType.ElbowRight], false).ToString());

                            Debug.WriteLine("Absolute Orientation: ");
                            Debug.WriteLine(skel.BoneOrientations[JointType.ShoulderRight].AbsoluteRotation.Quaternion.toEulerianAngle());
                            Debug.WriteLine("Hierarchical Orientation: ");
                            Debug.WriteLine(skel.BoneOrientations[JointType.ShoulderRight].HierarchicalRotation.Quaternion.toEulerianAngle());
                            */
                            Dictionary<string, Point> dictionary = new Dictionary<string, Point>();
                            foreach (Joint joint in skel.Joints)
                            {
                                //3D coordinaten in meter
                                SkeletonPoint skeletonPoint = joint.Position;

                                // 2D coordinaten in pixels
                                Point point = new Point();


                                // Skelet naar color mapping
                                ColorImagePoint colorPoint = Sensor.CoordinateMapper.MapSkeletonPointToColorPoint(skeletonPoint, ColorImageFormat.RgbResolution640x480Fps30);

                                point.X = colorPoint.X;
                                point.Y = colorPoint.Y;

                                string type = joint.JointType.ToString();
                                Point point2 = point;


                                dictionary.Add(type, point2);

                                if(joint.JointType == JointType.ShoulderCenter)
                                {
                                    DrawText(canvasList[0], point, skel.TrackingId.ToString());
                                }
                                foreach (Canvas canvas in this.canvasList)
                                {
                                    DrawEllipse(canvas, point);

                                }

                            }

                            DrawBones(dictionary);

                        }
                        else if(skel.TrackingState == SkeletonTrackingState.PositionOnly)
                        {
                            //this.Sensor.SkeletonStream.DrawSkeletonPosition(skel.Position);
                            //3D coordinaten in meter
                            SkeletonPoint skeletonPoint = skel.Position;

                            // 2D coordinaten in pixels
                            Point point = new Point();


                            // Skelet naar color mapping
                            ColorImagePoint colorPoint = Sensor.CoordinateMapper.MapSkeletonPointToColorPoint(skeletonPoint, ColorImageFormat.RgbResolution640x480Fps30);

                            point.X = colorPoint.X;
                            point.Y = colorPoint.Y;

                            //string type = joint.JointType.ToString();
                            Point point2 = point;

                            DrawText(canvasList[0], point, skel.TrackingId.ToString());
                            DrawEllipse(canvasList[0], point);
                        }
                    }

                }
            }
        }


        public void WriteToFile(string filename)
        {
            string file = filename + ".txt";
            int index = 1;

            string path = System.IO.Path.Combine(Environment.CurrentDirectory, file);
            if (!File.Exists(path))
            {
                File.Create(path).Close();
                //string json = JsonConvert.SerializeObject(SkeletonDataList);
                //System.IO.File.WriteAllText(path, json);

            }

            //SkeletonDataList.Clear();
            index = 0;
        }

        // tekent de bones tussen de verschillende joints adhv x,y van joint1 & joint2
        public void DrawBones(Dictionary<string, Point> dictionary)
        {
            foreach (Canvas canvas in this.canvasList)
            {
                DrawLine(canvas, dictionary["Head"], dictionary["ShoulderCenter"]);
                DrawLine(canvas, dictionary["ShoulderCenter"], dictionary["ShoulderLeft"]);
                DrawLine(canvas, dictionary["ShoulderCenter"], dictionary["ShoulderRight"]);
                DrawLine(canvas, dictionary["ShoulderLeft"], dictionary["ElbowLeft"]);
                DrawLine(canvas, dictionary["ElbowLeft"], dictionary["WristLeft"]);
                DrawLine(canvas, dictionary["WristLeft"], dictionary["HandLeft"]);
                DrawLine(canvas, dictionary["ShoulderRight"], dictionary["ElbowRight"]);
                DrawLine(canvas, dictionary["ElbowRight"], dictionary["WristRight"]);
                DrawLine(canvas, dictionary["WristRight"], dictionary["HandRight"]);
                if (this.Sensor.SkeletonStream.TrackingMode != SkeletonTrackingMode.Seated)
                {
                    DrawLine(canvas, dictionary["ShoulderCenter"], dictionary["Spine"]);
                    DrawLine(canvas, dictionary["Spine"], dictionary["HipCenter"]);
                    DrawLine(canvas, dictionary["HipCenter"], dictionary["HipLeft"]);
                    DrawLine(canvas, dictionary["HipCenter"], dictionary["HipRight"]);
                    DrawLine(canvas, dictionary["HipLeft"], dictionary["KneeLeft"]);
                    DrawLine(canvas, dictionary["KneeLeft"], dictionary["AnkleLeft"]);
                    DrawLine(canvas, dictionary["AnkleLeft"], dictionary["FootLeft"]);
                    DrawLine(canvas, dictionary["HipRight"], dictionary["KneeRight"]);
                    DrawLine(canvas, dictionary["KneeRight"], dictionary["AnkleRight"]);
                    DrawLine(canvas, dictionary["AnkleRight"], dictionary["FootRight"]);
                }
            }

            dictionary.Clear();

        }

    }
}
#endif