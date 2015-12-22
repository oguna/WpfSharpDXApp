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
        private ID3D11App myApp;
        private DispatcherTimer timer;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_OnLoaded;
            timer = new DispatcherTimer(DispatcherPriority.Normal);
            timer.Interval = TimeSpan.FromSeconds(1.0 / 60);
            timer.Tick += (sender, args) => { InteropImage.RequestRender(); };
        }

        private void DoRender(IntPtr surface, bool isNewSurface)
        {
            Render(myApp, surface, isNewSurface);
        }

        private int Render(ID3D11App app, IntPtr resourcePointer, bool isNewSurface)
        {
            app.Render(resourcePointer, isNewSurface);
            return 0;
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            InteropImage.WindowOwner = (new System.Windows.Interop.WindowInteropHelper(this)).Handle;
            InteropImage.OnRender = this.DoRender;
            myApp = new MiniCube();
            myApp.InitDevice();
            InteropImage.SetPixelSize(800, 600);
            InteropImage.RequestRender();
            timer.Start();
        }
    }
}
