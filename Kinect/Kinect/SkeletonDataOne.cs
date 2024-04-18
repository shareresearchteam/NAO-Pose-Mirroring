#if KINECT2
using Kinect.Model;
using Microsoft.Kinect;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows;
using System.Windows.Media.Imaging;
using KinectCoordinateMapping;

namespace Kinect
{
    public class SkeletonDataOne : SkeletonBase
    {
        /// <summary>
        /// Active Kinect sensor
        /// </summary>
        public KinectSensor kinectSensor = null;

        private readonly int bytesPerPixel = (PixelFormats.Bgr32.BitsPerPixel + 7) / 8;

        /// <summary>
        /// The size in bytes of the _kinectColorBitmap back buffer
        /// </summary>
        private uint bitmapBackBufferSize = 0;

        /// <summary>
        /// Intermediate storage for the color to depth mapping
        /// </summary>
        private DepthSpacePoint[] colorMappedToDepthPoints = null;

        /// <summary>
        /// Coordinate mapper to map one type of point to another
        /// </summary>
        private CoordinateMapper coordinateMapper = null;

        private MultiSourceFrameReader multiFrameSourceReader = null;

        /// <summary>
        /// Reader for body frames
        /// </summary>
        private BodyFrameReader bodyFrameReader = null;


        /// <summary>
        /// Reader for color frames
        /// </summary>
        private ColorFrameReader colorFrameReader = null;

        /// <summary>
        /// Bitmap to display
        /// </summary>
        public WriteableBitmap colorBitmap = null;

        /// <summary>
        /// Array for the bodies
        /// </summary>
        private Body[] bodies = null;

        /// <summary>
        /// definition of bones
        /// </summary>
        private List<Tuple<JointType, JointType>> bones;

        /// <summary>
        /// Width of display (depth space)
        /// </summary>
        private int displayWidth;

        /// <summary>
        /// Height of display (depth space)
        /// </summary>
        private int displayHeight;

        /// <summary>
        /// Constant for clamping Z values of camera space points from being negative
        /// </summary>
        private const float InferredZPositionClamp = 0.1f;


        //public List<KeyFrame> SkeletonDataList = new List<KeyFrame>();

        //dictionary voor het tekenen van de bones (zal joint name en zijn position bevatten)
        public Dictionary<string, Point> dictionary = new Dictionary<string, Point>();

        public List<ulong> trackedSkels = new List<ulong>();
        private ulong trackedSkeleton = 0;
        private bool haveSkeleton = false;
        public override string TrackedSkeleton
        {
        get { return this.trackedSkeleton.ToString(); }
        set
        {
            if (ulong.TryParse(value, out ulong j))
            {
                this.trackedSkeleton = j;
                base.OnTrackedSkeletonUpdated(new EventArgs());
            }
         }
        }


        public SkeletonDataOne()
        {

        }

        private void ColorSkeletonData_TrackedSkeletonUpdated(object sender, EventArgs e)
        {
            if (this.trackedSkeleton == 0) { return; }
            if (this.kinectSensor != null && this.bodyFrameReader != null && this.kinectSensor.IsAvailable)
            {
                this.ResetJointList();
            }
        }

        private void ResetJointList()
        {
            // TODO: resetting for if we want to record motions
            return;
        }


        // TODO: let's rework this, so it actually grabs the left most skeleton to start,
        // And then you go right or left depending on button presses, and it gets the one that positionally
        // is more right or left
        public override void CycleSkeleton(bool right = true)
        {

            Console.WriteLine(string.Join(", " ,trackedSkels));
            

            
            if (this.kinectSensor != null && this.kinectSensor.IsAvailable)
            {

                // find all the current skeletons
                //var viableSkels = this.trackedSkels.Count;
                var viableSkels = this.bodies.Where(s => s.LeanTrackingState == TrackingState.Inferred || s.IsTracked);
                if (viableSkels.Count() < 1) { return; }
                var sortedSkels = viableSkels.OrderBy(s => s.Joints[JointType.SpineShoulder].Position.X).ToList();
                int currentIndex = 0;
                // Try and get the index of the one we're currently tracking... 
                try
                {
                    //currentIndex = trackedSkels.FindIndex(s => s == this.trackedSkeleton);
                    currentIndex = sortedSkels.FindIndex(s => s.TrackingId == this.trackedSkeleton);
                }
                // If that fails cause our tracking id is wrong then try and get it from the skeleton tracked state
                catch
                {
                    try
                    {
                        currentIndex = sortedSkels.FindIndex(s => s.IsTracked);
                    }
                    // If none of them are tracked then just return
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
                // base.OnTrackedSkeletonUpdated(new EventArgs());
            }


        }

        public override void InitializeSensorAndSkeleton()
        {

            // handling with a single sensor lol
            // We're gonna need to tweak this if we want to have multiple sensors in use.
            // TODO: adjust this so we can actually have multiple sensors
            /*
            foreach (var potentialSensor in KinectSensor.KinectSensors)
            {
                if (potentialSensor.Status == KinectStatus.Connected)
                {
                    this.Sensor = potentialSensor;
                    break;
                }
            }
            */

            this.kinectSensor = KinectSensor.GetDefault();

            if (this.kinectSensor != null)
            {
                // one sensor is currently supported
                //this.kinectSensor = KinectSensor.GetDefault();

                //this.multiFrameSourceReader = this.kinectSensor.OpenMultiSourceFrameReader(FrameSourceTypes.Depth | FrameSourceTypes.Color | FrameSourceTypes.BodyIndex | FrameSourceTypes.Body);

                // open the reader for the color frames
                this.colorFrameReader = this.kinectSensor.ColorFrameSource.OpenReader();

                // wire handler for frame arrival
                this.colorFrameReader.FrameArrived += this.Sensor_ColorFrameReady;

                // create the colorFrameDescription from the ColorFrameSource using Bgra format
                FrameDescription colorFrameDescription = this.kinectSensor.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Bgra);

                // create the bitmap to display
                this.colorBitmap = new WriteableBitmap(colorFrameDescription.Width, colorFrameDescription.Height, 96.0, 96.0, PixelFormats.Bgr32, null);

                this.bitmapBackBufferSize = (uint)((this.colorBitmap.BackBufferStride * (this.colorBitmap.PixelHeight - 1)) + (this.colorBitmap.PixelWidth * this.bytesPerPixel));

                // get the coordinate mapper
                this.coordinateMapper = this.kinectSensor.CoordinateMapper;

                // get the depth (display) extents
                FrameDescription frameDescription = this.kinectSensor.DepthFrameSource.FrameDescription;


                // get size of joint space
                this.displayWidth = frameDescription.Width;
                this.displayHeight = frameDescription.Height;

                this.colorMappedToDepthPoints = new DepthSpacePoint[colorFrameDescription.Width * colorFrameDescription.Height];

                // open the reader for the body frames
                this.bodyFrameReader = this.kinectSensor.BodyFrameSource.OpenReader();

                // a bone defined as a line between two joints
                this.bones = new List<Tuple<JointType, JointType>>();

                // Torso
                this.bones.Add(new Tuple<JointType, JointType>(JointType.Head, JointType.Neck));
                this.bones.Add(new Tuple<JointType, JointType>(JointType.Neck, JointType.SpineShoulder));
                this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.SpineMid));
                this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineMid, JointType.SpineBase));
                this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderRight));
                this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderLeft));
                this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipRight));
                this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipLeft));

                // Right Arm
                this.bones.Add(new Tuple<JointType, JointType>(JointType.ShoulderRight, JointType.ElbowRight));
                this.bones.Add(new Tuple<JointType, JointType>(JointType.ElbowRight, JointType.WristRight));
                this.bones.Add(new Tuple<JointType, JointType>(JointType.WristRight, JointType.HandRight));
                this.bones.Add(new Tuple<JointType, JointType>(JointType.HandRight, JointType.HandTipRight));
                this.bones.Add(new Tuple<JointType, JointType>(JointType.WristRight, JointType.ThumbRight));

                // Left Arm
                this.bones.Add(new Tuple<JointType, JointType>(JointType.ShoulderLeft, JointType.ElbowLeft));
                this.bones.Add(new Tuple<JointType, JointType>(JointType.ElbowLeft, JointType.WristLeft));
                this.bones.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.HandLeft));
                this.bones.Add(new Tuple<JointType, JointType>(JointType.HandLeft, JointType.HandTipLeft));
                this.bones.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.ThumbLeft));

                // Right Leg
                //this.bones.Add(new Tuple<JointType, JointType>(JointType.HipRight, JointType.KneeRight));
                //this.bones.Add(new Tuple<JointType, JointType>(JointType.KneeRight, JointType.AnkleRight));
                //this.bones.Add(new Tuple<JointType, JointType>(JointType.AnkleRight, JointType.FootRight));

                // Left Leg
                //this.bones.Add(new Tuple<JointType, JointType>(JointType.HipLeft, JointType.KneeLeft));
                //this.bones.Add(new Tuple<JointType, JointType>(JointType.KneeLeft, JointType.AnkleLeft));
                //this.bones.Add(new Tuple<JointType, JointType>(JointType.AnkleLeft, JointType.FootLeft));

                // set IsAvailableChanged event notifier
                this.kinectSensor.IsAvailableChanged += this.Sensor_IsAvailableChanged;

                // open the sensor
                this.kinectSensor.Open();
            }
            //this.multiFrameSourceReader.MultiSourceFrameArrived += this.Reader_MultiSourceFrameArrived;

            if (this.bodyFrameReader != null)
            {
                this.bodyFrameReader.FrameArrived += this.Reader_FrameArrived;
            }
        }

        public void AddViewWindow(System.Windows.Controls.Canvas canvas, System.Windows.Controls.Image image)
        {
            this.imgList.Add(image);
            this.canvasList.Add(canvas);
        }

        private void ClearCanvases()
        {
            foreach (Canvas canvas in this.canvasList)
            {
                canvas.Children.Clear();
            }
        }

        /// <summary>
        /// Handles the event which the sensor becomes unavailable (E.g. paused, closed, unplugged).
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Sensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
            // on failure, set the status text
            if (this.kinectSensor.IsAvailable) { Console.WriteLine("Kinect sensor available for frames."); }
            else { 
                Console.WriteLine("Kinect sensor not reachable.");
                //this.StopSensorAndSkeleton();
            }
        }

        private void Sensor_ColorFrameReady(object sender, ColorFrameArrivedEventArgs e)
        {
            // ColorFrame is IDisposable
            using (ColorFrame colorFrame = e.FrameReference.AcquireFrame())
            {
                if (colorFrame != null)
                {
                    FrameDescription colorFrameDescription = colorFrame.FrameDescription;

                    using (KinectBuffer colorBuffer = colorFrame.LockRawImageBuffer())
                    {
                        //this.colorBitmap.Lock();

                        //// verify data and write the new color frame data to the display bitmap
                        //if ((colorFrameDescription.Width == this.colorBitmap.PixelWidth) && (colorFrameDescription.Height == this.colorBitmap.PixelHeight))
                        //{
                        //    colorFrame.CopyConvertedFrameDataToIntPtr(
                        //        this.colorBitmap.BackBuffer,
                        //        (uint)(colorFrameDescription.Width * colorFrameDescription.Height * 4),
                        //        ColorImageFormat.Bgra);

                        //    this.colorBitmap.AddDirtyRect(new Int32Rect(0, 0, this.colorBitmap.PixelWidth, this.colorBitmap.PixelHeight));
                        //}

                        //this.colorBitmap.Unlock();
                        //this.colorBitmap = colorFrame.ToBitmap();

                        foreach (System.Windows.Controls.Image img in this.imgList)
                        {
                            img.Source = colorFrame.ToBitmap(); //this.colorBitmap;
                        }
                    }
                }
            }
            
        }

        public override void StopSensorAndSkeleton()
        {
            if (this.colorFrameReader != null)
            {
                // ColorFrameReder is IDisposable
                this.colorFrameReader.Dispose();
                this.colorFrameReader = null;
            }

            if (this.bodyFrameReader != null)
            {
                // BodyFrameReader is IDisposable
                this.bodyFrameReader.Dispose();
                this.bodyFrameReader = null;
            }

            if (this.multiFrameSourceReader != null)
            {
                this.multiFrameSourceReader.Dispose();
                this.multiFrameSourceReader = null;
            }

            if (this.kinectSensor != null)
            {
                this.kinectSensor.Close();
                this.kinectSensor = null;
            }
        }

        private void Reader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            int depthWidth = 0;
            int depthHeight = 0;

            DepthFrame depthFrame = null;
            ColorFrame colorFrame = null;
            BodyIndexFrame bodyIndexFrame = null;
            BodyFrame bodyFrame = null;

            bool isBitmapLocked = false;

            MultiSourceFrame multiSourceFrame = e.FrameReference.AcquireFrame();

            // If the Frame has expired by the time we process this event, return.
            if (multiSourceFrame == null)
            {
                return;
            }

            // We use a try/finally to ensure that we clean up before we exit the function.  
            // This includes calling Dispose on any Frame objects that we may have and unlocking the _kinectColorBitmap back buffer.
            try
            {
                depthFrame = multiSourceFrame.DepthFrameReference.AcquireFrame();
                colorFrame = multiSourceFrame.ColorFrameReference.AcquireFrame();
                bodyIndexFrame = multiSourceFrame.BodyIndexFrameReference.AcquireFrame();

                // If any frame has expired by the time we process this event, return.
                // The "finally" statement will Dispose any that are not null.
                if ((depthFrame == null) || (colorFrame == null) || (bodyIndexFrame == null))
                {
                    return;
                }

                FrameDescription colorFrameDescription = colorFrame.FrameDescription;

                using (KinectBuffer colorBuffer = colorFrame.LockRawImageBuffer())
                {
                    this.colorBitmap.Lock();

                    // verify data and write the new color frame data to the display bitmap
                    if ((colorFrameDescription.Width == this.colorBitmap.PixelWidth) && (colorFrameDescription.Height == this.colorBitmap.PixelHeight))
                    {
                        colorFrame.CopyConvertedFrameDataToIntPtr(
                            this.colorBitmap.BackBuffer,
                            (uint)(colorFrameDescription.Width * colorFrameDescription.Height * 4),
                            ColorImageFormat.Bgra);

                        this.colorBitmap.AddDirtyRect(new Int32Rect(0, 0, this.colorBitmap.PixelWidth, this.colorBitmap.PixelHeight));
                    }

                    this.colorBitmap.Unlock();

                    foreach (System.Windows.Controls.Image img in this.imgList)
                    {
                        img.Source = this.colorBitmap;
                    }
                }

                bodyFrame = multiSourceFrame.BodyFrameReference.AcquireFrame();
                if (bodyFrame != null)
                {
                    //this.RenderBodyFrame(bodyFrame);
                }

                

                //// Process Depth
                //FrameDescription depthFrameDescription = depthFrame.FrameDescription;

                //depthWidth = depthFrameDescription.Width;
                //depthHeight = depthFrameDescription.Height;

                //// Access the depth frame data directly via LockImageBuffer to avoid making a copy
                //using (KinectBuffer depthFrameData = depthFrame.LockImageBuffer())
                //{
                //    this.coordinateMapper.MapColorFrameToDepthSpaceUsingIntPtr(
                //        depthFrameData.UnderlyingBuffer,
                //        depthFrameData.Size,
                //        this.colorMappedToDepthPoints);
                //}

                //// We're done with the DepthFrame 
                //depthFrame.Dispose();
                //depthFrame = null;

                //// Process Color

                //// Lock the _kinectColorBitmap for writing
                //this.colorBitmap.Lock();
                //isBitmapLocked = true;

                //colorFrame.CopyConvertedFrameDataToIntPtr(this.colorBitmap.BackBuffer, this.bitmapBackBufferSize, ColorImageFormat.Bgra);

                //// We're done with the ColorFrame 
                //colorFrame.Dispose();
                //colorFrame = null;

                //// We'll access the body index data directly to avoid a copy
                //using (KinectBuffer bodyIndexData = bodyIndexFrame.LockImageBuffer())
                //{
                //    unsafe
                //    {
                //        byte* bodyIndexDataPointer = (byte*)bodyIndexData.UnderlyingBuffer;

                //        int colorMappedToDepthPointCount = this.colorMappedToDepthPoints.Length;

                //        fixed (DepthSpacePoint* colorMappedToDepthPointsPointer = this.colorMappedToDepthPoints)
                //        {
                //            // Treat the color data as 4-byte pixels
                //            uint* bitmapPixelsPointer = (uint*)this.colorBitmap.BackBuffer;

                //            //// Loop over each row and column of the color image
                //            //// Zero out any pixels that don't correspond to a body index
                //            for (int colorIndex = 0; colorIndex < colorMappedToDepthPointCount; ++colorIndex)
                //            {
                //                float colorMappedToDepthX = colorMappedToDepthPointsPointer[colorIndex].X;
                //                float colorMappedToDepthY = colorMappedToDepthPointsPointer[colorIndex].Y;

                //                // The sentinel value is -inf, -inf, meaning that no depth pixel corresponds to this color pixel.
                //                if (!float.IsNegativeInfinity(colorMappedToDepthX) &&
                //                    !float.IsNegativeInfinity(colorMappedToDepthY))
                //                {
                //                    // Make sure the depth pixel maps to a valid point in color space
                //                    int depthX = (int)(colorMappedToDepthX + 0.5f);
                //                    int depthY = (int)(colorMappedToDepthY + 0.5f);

                //                    // If the point is not valid, there is no body index there.
                //                    if ((depthX >= 0) && (depthX < depthWidth) && (depthY >= 0) && (depthY < depthHeight))
                //                    {
                //                        int depthIndex = (depthY * depthWidth) + depthX;

                //                        // If we are tracking a body for the current pixel, do not zero out the pixel
                //                        if (bodyIndexDataPointer[depthIndex] != 0xff)
                //                        {
                //                            continue;
                //                        }
                //                    }
                //                }

                //                bitmapPixelsPointer[colorIndex] = 0;
                //            }
                //        }

                //        this.colorBitmap.AddDirtyRect(new Int32Rect(0, 0, this.colorBitmap.PixelWidth, this.colorBitmap.PixelHeight));
                //    }
                //}
            }
            finally
            {
                if (isBitmapLocked)
                {
                    this.colorBitmap.Unlock();
                }

                if (depthFrame != null)
                {
                    depthFrame.Dispose();
                }

                if (colorFrame != null)
                {
                    colorFrame.Dispose();
                }

                if (bodyIndexFrame != null)
                {
                    bodyIndexFrame.Dispose();
                }
            }
        }


        /// <summary>
        /// Handles the body frame data arriving from the sensor
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        
        private void Reader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            bool dataReceived = false;

            using (BodyFrame bodyFrame = e.FrameReference.AcquireFrame())
            {
                if (bodyFrame != null)
                {
                    if (this.bodies == null)
                    {
                        this.bodies = new Body[bodyFrame.BodyCount];
                    }

                    // The first time GetAndRefreshBodyData is called, Kinect will allocate each Body in the array.
                    // As long as those body objects are not disposed and not set to null in the array,
                    // those body objects will be re-used.
                    bodyFrame.GetAndRefreshBodyData(this.bodies);
                    dataReceived = true;
                }
            }
        
        /*
        private void RenderBodyFrame(BodyFrame bodyFrame)
        {
            bool dataReceived = false;

            //Body[] bodies = null;
            if (bodyFrame != null)
            {
                if (this.bodies == null)
                {
                    this.bodies = new Body[bodyFrame.BodyCount];
                }

                // The first time GetAndRefreshBodyData is called, Kinect will allocate each Body in the array.
                // As long as those body objects are not disposed and not set to null in the array,
                // those body objects will be re-used.
                bodyFrame.GetAndRefreshBodyData(this.bodies);
                bodyFrame.Dispose();
                dataReceived = true;
            }
        */

            if (dataReceived)
            {
                //canvas.Children.Clear();
                ClearCanvases();
                this.trackedSkels = new List<ulong>();
                foreach (Body skel in this.bodies)
                {
                    if (skel.TrackingId == this.trackedSkeleton && !skel.IsTracked)
                    {
                        this.haveSkeleton = false;
                    }
                    if (skel.LeanTrackingState != TrackingState.NotTracked)
                    {
                        trackedSkels.Add(skel.TrackingId);
                    }
                    if (skel.IsTracked)
                    {
                        if (!this.haveSkeleton)
                        {
                            this.TrackedSkeleton = skel.TrackingId.ToString();
                            this.haveSkeleton = true;
                            //TrackedSkeletonUpdated?.Invoke(this, new EventArgs());
                        }

                        if (skel.TrackingId == this.trackedSkeleton)
                        {
                            base.OnJointAnglesReady(new JointAnglesEventArgs() { SkelJoints = skel.Joints });

                            // Draw the full skeleton if it's who we're tracking
                            //this.DrawClippedEdges(body, dc);

                            IReadOnlyDictionary<JointType, Joint> joints = skel.Joints;

                            // convert the joint points to depth (display) space
                            Dictionary<JointType, Point> jointPoints = new Dictionary<JointType, Point>();

                            foreach (JointType jointType in joints.Keys)
                            {
                                // sometimes the depth(Z) of an inferred joint may show as negative
                                // clamp down to 0.1f to prevent coordinatemapper from returning (-Infinity, -Infinity)
                                CameraSpacePoint position = joints[jointType].Position;
                                if (position.Z < 0)
                                {
                                    position.Z = InferredZPositionClamp;
                                }
                                //this.coordinateMapper.
                                //this.coordinateMapper.
                                //DepthSpacePoint depthSpacePoint = this.coordinateMapper.MapCameraPointToDepthSpace(position);
                                ColorSpacePoint colorSpacePoint = this.coordinateMapper.MapCameraPointToColorSpace(position);
                                // 2D coordinaten in pixels
                                Point point = new Point()
                                {
                                    X = colorSpacePoint.X,
                                    Y = colorSpacePoint.Y
                                };
                                if (jointType == JointType.SpineShoulder)
                                {
                                    DrawText(canvasList[0], point, skel.TrackingId.ToString());
                                }
                                foreach (Canvas canvas in this.canvasList)
                                {
                                    DrawEllipse(canvas, point);
                                }
                                jointPoints[jointType] = new Point(position.X, position.Y);
                            }

                            // Draw the bones
                            foreach (var bone in this.bones)
                            {
                                this.DrawBone(joints, bone);
                            }
                        }
                        else
                        {
                            // sometimes the depth(Z) of an inferred joint may show as negative
                            // clamp down to 0.1f to prevent coordinatemapper from returning (-Infinity, -Infinity)
                            CameraSpacePoint position = skel.Joints[JointType.SpineShoulder].Position;
                            if (position.Z < 0)
                            {
                                position.Z = InferredZPositionClamp;
                            }

                            //DepthSpacePoint depthSpacePoint = this.coordinateMapper.MapCameraPointToDepthSpace(position);
                            //ColorSpacePoint depthSpacePoint = this.coordinateMapper.MapCameraPointToColorSpace(position);
                            ColorSpacePoint colorSpacePoint = this.coordinateMapper.MapCameraPointToColorSpace(position);
                            // 2D coordinaten in pixels
                            Point point = new Point()
                            {
                                X = colorSpacePoint.X,
                                Y = colorSpacePoint.Y
                            };
                            //Otherwise just draw the center point?
                            DrawText(canvasList[0], point, skel.TrackingId.ToString());
                            DrawEllipse(canvasList[0], point);
                        }

                    }
                    else if (skel.LeanTrackingState == TrackingState.Inferred)
                    {
                        //not quite sure how to implement this without the position only concept...
                        // sometimes the depth(Z) of an inferred joint may show as negative
                        // clamp down to 0.1f to prevent coordinatemapper from returning (-Infinity, -Infinity)
                        CameraSpacePoint position = skel.Joints[JointType.SpineShoulder].Position;
                        if (position.Z < 0)
                        {
                            position.Z = InferredZPositionClamp;
                        }

                        //DepthSpacePoint depthSpacePoint = this.coordinateMapper.MapCameraPointToDepthSpace(position);
                        ColorSpacePoint colorSpacePoint = this.coordinateMapper.MapCameraPointToColorSpace(position);
                        // 2D coordinaten in pixels
                        Point point = new Point()
                        {
                            X = colorSpacePoint.X,
                            Y = colorSpacePoint.Y
                        };
                        //Otherwise just draw the center point?
                        DrawText(canvasList[0], point, skel.TrackingId.ToString());
                        DrawEllipse(canvasList[0], point);
                    }
                }
            }
        }

        private void DrawBone(IReadOnlyDictionary<JointType, Joint> joints, Tuple<JointType, JointType> bone)
        {
            Joint joint1 = joints[bone.Item1];
            Joint joint2 = joints[bone.Item2];

            // If we can't find either of these joints, exit
            if (joint1.TrackingState == TrackingState.NotTracked ||
                joint2.TrackingState == TrackingState.NotTracked)
            {
                return;
            }

            foreach (Canvas canvas in this.canvasList)
            {
                DrawLine(canvas, new Point() { X = joint1.Position.X, Y = joint1.Position.Y },
                new Point() { X = joint2.Position.X, Y = joint2.Position.Y });
            }
        }

    }
}
#endif