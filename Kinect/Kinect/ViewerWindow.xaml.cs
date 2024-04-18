using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Kinect
{
    /// <summary>
    /// Interaction logic for ViewerWindow.xaml
    /// </summary>
    public partial class ViewerWindow : Window
    {
        public SkeletonBase drawSkeleton { get; set; }

        public ViewerWindow(Image imgCamera, Canvas CnvSkeleton)
        {
            InitializeComponent();
            //this.drawSkeleton = drawSkeleton;
            this.imgCamera.Source = imgCamera.Source;
            foreach (UIElement child in CnvSkeleton.Children)
            {
                this.CnvSkeleton.Children.Add(child);
            }
        }

        public void ShowFace(int faceNum)
        {
            // Create the image element.
            //Image simpleImage = new Image();
            //simpleImage.Width = 200;
            //simpleImage.Margin = new Thickness(5);

            // Create source.
            BitmapImage bi = new BitmapImage();
            // BitmapImage.UriSource must be in a BeginInit/EndInit block.
            bi.BeginInit();
            bi.UriSource = new Uri(@"/Assets/robo_face_smile.png", UriKind.RelativeOrAbsolute);
            bi.EndInit();
            // Set the image source.
            //simpleImage.Source = bi;
            this.imgFace.Source = bi;
            this.imgFace.Visibility = Visibility.Visible;
        }

        public void HideFace()
        {
            this.imgFace.Visibility = Visibility.Hidden;
        }

        public void UpdateImg(Image imgCam)
        {
            this.imgCamera.Source = imgCam.Source;
        }

        public void UpdateCanvas(Canvas canvas)
        {
            foreach (UIElement child in CnvSkeleton.Children)
            {
                this.CnvSkeleton.Children.Add(child);
            }
        }
    }
}
