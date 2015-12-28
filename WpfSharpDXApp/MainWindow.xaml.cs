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
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace WpfSharpDXApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static Cube app = new Cube();

        // Magnifier Image Settings
        private const double MagImageScale = 1.25; // Scale of image to magnified ellipse
        private const double MagImageOffset = 0.12; // Offset of magnified ellipse within image

        // Unit conversion
        private const float DegreesToRadians = (float)Math.PI / 180;

        // State Management
        private bool magnify = true;
        TimeSpan lastRender;
        bool lastVisible;

        // Magnifier Settings (filled by default slider vlaues)
        private double magSize;
        private double magScale;

        public MainWindow()
        {
            InitializeComponent();
            this.host.Loaded += new RoutedEventHandler(this.Host_Loaded);
            this.host.SizeChanged += new SizeChangedEventHandler(this.Host_SizeChanged);
        }

        private static bool Init()
        {
            app.InitDevice();
            return true;
        }

        private static void Cleanup()
        {
            app.Dispose();
        }

        private static int Render(IntPtr resourcePointer, bool isNewSurface)
        {
            app.Render(resourcePointer, isNewSurface);
            return 0;
        }

        private static int SetCameraRadius(float radius)
        {
            app.GetCamera().Radius = radius;
            return 0;
        }

        private static int SetCameraTheta(float theta)
        {
            app.GetCamera().Theta = theta;
            return 0;
        }

        private static int SetCameraPhi(float phi)
        {
            app.GetCamera().Phi = phi;
            return 0;
        }

        #region Callbacks
        private void Host_Loaded(object sender, RoutedEventArgs e)
        {
            Init();
            this.InitializeRendering();

            // Setup the Magnifier Size
            MagEllipse.Height = this.magSize;
            MagEllipse.Width = this.magSize;
            Scale.Value = this.magScale;

            // Add mouse over event
            host.MouseMove += this.MagElement_MouseMove;
            ImageHost.MouseMove += this.MagElement_MouseMove;
            MagEllipse.MouseMove += this.MagElement_MouseMove;
            MagImage.MouseMove += this.MagElement_MouseMove;

            host.MouseLeave += this.MagElement_MouseLeave;
            MagEllipse.MouseLeave += this.MagElement_MouseLeave;
            ImageHost.MouseLeave += this.MagElement_MouseLeave;
            MagImage.MouseLeave += this.MagElement_MouseLeave;

            MagBox.Checked += this.MagBox_Checked;
            MagBox.Unchecked += this.MagBox_Unchecked;
        }

        private void Host_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            double dpiScale = 1.0; // default value for 96 dpi

            // determine DPI
            // (as of .NET 4.6.1, this returns the DPI of the primary monitor, if you have several different DPIs)
            var hwndTarget = PresentationSource.FromVisual(this).CompositionTarget as HwndTarget;
            if (hwndTarget != null)
            {
                dpiScale = hwndTarget.TransformToDevice.M11;
            }

            int surfWidth = (int)(host.ActualWidth < 0 ? 0 : Math.Ceiling(host.ActualWidth * dpiScale));
            int surfHeight = (int)(host.ActualHeight < 0 ? 0 : Math.Ceiling(host.ActualHeight * dpiScale));

            // Notify the D3D11Image of the pixel size desired for the DirectX rendering.
            // The D3DRendering component will determine the size of the new surface it is given, at that point.
            InteropImage.SetPixelSize(surfWidth, surfHeight);

            // Stop rendering if the D3DImage isn't visible - currently just if width or height is 0
            // TODO: more optimizations possible (scrolled off screen, etc...)
            bool isVisible = (surfWidth != 0 && surfHeight != 0);
            if (lastVisible != isVisible)
            {
                lastVisible = isVisible;
                if (lastVisible)
                {
                    CompositionTarget.Rendering += CompositionTarget_Rendering;
                }
                else
                {
                    CompositionTarget.Rendering -= CompositionTarget_Rendering;
                }
            }
        }

        private void Scale_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            this.magScale = e.NewValue;
        }

        private void Size_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            this.magSize = e.NewValue;

            // Setup the Magnifier Size
            this.MagEllipse.Height = this.magSize;
            this.MagEllipse.Width = this.magSize;
        }

        private void MagBox_Checked(object sender, RoutedEventArgs e)
        {
            this.magnify = true;

            MagCurserToggle1.Cursor = System.Windows.Input.Cursors.None;
            MagCurserToggle2.Cursor = System.Windows.Input.Cursors.None;
            host.Cursor = System.Windows.Input.Cursors.None;
        }

        private void MagBox_Unchecked(object sender, RoutedEventArgs e)
        {
            this.magnify = false;

            MagCurserToggle1.Cursor = System.Windows.Input.Cursors.Arrow;
            MagCurserToggle2.Cursor = System.Windows.Input.Cursors.Arrow;
            host.Cursor = System.Windows.Input.Cursors.Arrow;
        }

        private void MagElement_MouseMove(object sender, MouseEventArgs e)
        {
            if (this.magnify)
            {
                Point point = Mouse.GetPosition(host);

                if (!(point.X < 0 || point.Y < 0 || point.X > host.ActualWidth || point.Y > host.ActualHeight))
                {
                    // Draw the Magnified ellipse on top of image
                    System.Windows.Controls.Canvas.SetTop(this.MagEllipse, point.Y - (this.magSize / 2));
                    System.Windows.Controls.Canvas.SetLeft(this.MagEllipse, point.X - (this.magSize / 2));

                    // Set the magnifier image on top of magnified ellipse 
                    System.Windows.Controls.Canvas.SetTop(this.MagImage, point.Y - (this.magSize * (.5 + MagImageOffset)));
                    System.Windows.Controls.Canvas.SetLeft(this.MagImage, point.X - (this.magSize * (.5 + MagImageOffset)));
                    MagImage.Width = this.magSize * MagImageScale;

                    MagEllipse.Visibility = System.Windows.Visibility.Visible;
                    MagImage.Visibility = System.Windows.Visibility.Visible;

                    double magViewboxSize = this.magSize / this.magScale;
                    MagBrush.Viewbox = new Rect(point.X - (.5 * magViewboxSize), point.Y - (.5 * magViewboxSize), magViewboxSize, magViewboxSize);
                }
                else
                {
                    MagEllipse.Visibility = Visibility.Hidden;
                    MagImage.Visibility = Visibility.Hidden;
                }
            }
        }

        private void MagElement_MouseLeave(object sender, MouseEventArgs e)
        {
            MagEllipse.Visibility = Visibility.Hidden;
            MagImage.Visibility = Visibility.Hidden;
        }

        private void Radius_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            SetCameraRadius((float)e.NewValue);
        }

        private void Theta_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            SetCameraTheta((float)e.NewValue * DegreesToRadians);
        }

        private void Phi_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            SetCameraPhi((float)e.NewValue * DegreesToRadians);
        }
        #endregion Callbacks

        #region Helpers
        private void InitializeRendering()
        {
            InteropImage.WindowOwner = (new System.Windows.Interop.WindowInteropHelper(this)).Handle;
            InteropImage.OnRender = this.DoRender;

            // Set up camera
            SetCameraRadius((float)RadiusSlider.Value);
            SetCameraPhi((float)PhiSlider.Value * DegreesToRadians);
            SetCameraTheta((float)ThetaSlider.Value * DegreesToRadians);

            // Start rendering now!
            InteropImage.RequestRender();
        }

        void CompositionTarget_Rendering(object sender, EventArgs e)
        {
            RenderingEventArgs args = (RenderingEventArgs)e;

            // It's possible for Rendering to call back twice in the same frame 
            // so only render when we haven't already rendered in this frame.
            if (this.lastRender != args.RenderingTime)
            {
                InteropImage.RequestRender();
                this.lastRender = args.RenderingTime;
            }
        }

        private void UninitializeRendering()
        {
            Cleanup();

            CompositionTarget.Rendering -= this.CompositionTarget_Rendering;
        }
        #endregion Helpers

        private void DoRender(IntPtr surface, bool isNewSurface)
        {
            Render(surface, isNewSurface);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.UninitializeRendering();

            host.MouseMove -= this.MagElement_MouseMove;
            ImageHost.MouseMove -= this.MagElement_MouseMove;
            MagEllipse.MouseMove -= this.MagElement_MouseMove;
            MagImage.MouseMove -= this.MagElement_MouseMove;

            host.MouseLeave -= this.MagElement_MouseLeave;
            MagEllipse.MouseLeave -= this.MagElement_MouseLeave;
            ImageHost.MouseLeave -= this.MagElement_MouseLeave;
            MagImage.MouseLeave -= this.MagElement_MouseLeave;

            MagBox.Checked -= this.MagBox_Checked;
            MagBox.Unchecked -= this.MagBox_Unchecked;
        }
    }
}
