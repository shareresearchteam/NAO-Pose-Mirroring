using Kinect.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Kinect;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows;

namespace Kinect
{
    public abstract class SkeletonBase
    {
        /// <summary>
        /// Active Kinect sensor
        /// </summary>
        public KinectSensor kinectSensor = null;

        // Event to raise whenever we change skeleton 
        public event EventHandler TrackedSkeletonUpdated;
        //The event-invoking method that derived classes can override.
        protected virtual void OnTrackedSkeletonUpdated(EventArgs e)
        {
            // Safely raise the event for all subscribers
            TrackedSkeletonUpdated?.Invoke(this, e);
        }

        // Event to raise whenever joint data is ready for sending
        public event EventHandler<JointAnglesEventArgs> JointAnglesReady;
        //The event-invoking method that derived classes can override.
        protected virtual void OnJointAnglesReady(JointAnglesEventArgs e)
        {
            // Safely raise the event for all subscribers
            JointAnglesReady?.Invoke(this, e);
        }

        // Camera frame UI image list
        protected List<System.Windows.Controls.Image> imgList = new List<System.Windows.Controls.Image>();
        // Draw Canvas UI elements list for skeleton drawing
        protected List<System.Windows.Controls.Canvas> canvasList = new List<System.Windows.Controls.Canvas>();


        public abstract string TrackedSkeleton { get; set; }


        public abstract void InitializeSensorAndSkeleton();

        public void AddViewWindow(System.Windows.Controls.Canvas canvas, System.Windows.Controls.Image image)
        {
            this.imgList.Add(image);
            this.canvasList.Add(canvas);
        }

        public abstract void StopSensorAndSkeleton();

        /// <summary>
        /// This should take our current trackedSkeleton ID, and  check where in the frame it is positioned.
        /// If right is true then we get the skeleton to the right of our current skeleton. 
        /// If right is false, we get the skeleton to the left of our current skeleton, from the viewer's perspective.
        /// We set that to our new trackedSkeleton and then invoke that the tracked skeleton has been updated. 
        /// </summary>
        /// <param name="isRight"></param>
        public abstract void CycleSkeleton(bool isRight = true);

        protected void DrawEllipse(Canvas canvas, Point point)
        {
            if (point.X >= canvas.ActualWidth || point.X < 0 || point.Y >= canvas.ActualHeight || point.Y < 0) { return; }
            Ellipse ellipse = new Ellipse
            {
                Fill = Brushes.Red,
                Width = 10,
                Height = 10
            };

            Canvas.SetLeft(ellipse, point.X - ellipse.Width / 2);
            Canvas.SetTop(ellipse, point.Y - ellipse.Height / 2);

            canvas.Children.Add(ellipse);
        }

        protected void DrawText(Canvas canvas, Point point, string text)
        {
            if (point.X >= canvas.ActualWidth || point.X < 0 || point.Y >= canvas.ActualHeight || point.Y < 0) { return; }
            TextBlock textBlock = new TextBlock
            {
                Text = text,
                Foreground = Brushes.Red,
                Background = Brushes.Black,
                FontSize = 24,
            };

            Canvas.SetLeft(textBlock, point.X);
            Canvas.SetTop(textBlock, point.Y);

            canvas.Children.Add(textBlock);
        }

        protected void DrawLine(Canvas canvas, Point point1, Point point2)
        {
            if (point1.X >= canvas.ActualWidth || point1.X < 0 || point1.Y >= canvas.ActualHeight || point1.Y < 0) { return; }
            if (point2.X >= canvas.ActualWidth || point2.X < 0 || point2.Y >= canvas.ActualHeight || point2.Y < 0) { return; }
            Line line = new Line();
            line.Stroke = Brushes.Green;
            line.StrokeThickness = 2;

            line.X1 = point1.X;
            line.Y1 = point1.Y;
            line.X2 = point2.X;
            line.Y2 = point2.Y;

            canvas.Children.Add(line);
        }
    }
}
