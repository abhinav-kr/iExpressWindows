using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Media.Imaging;
using TETCSharpClient;
using TETCSharpClient.Data;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace iExpress
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ScrollPage : Page, IGazeListener
    {
        private const float DPI_DEFAULT = 96f; // default system DIP setting
        private const double SPEED_BOOST = 20.0;
        private const double ACTIVE_SCROLL_AREA = 0.25; // 25% top and bottom
        private const int MAX_IMAGE_WIDTH = 1600;
        //private readonly double dpiScale;
        //private Matrix transfrm;
        private double scrollLevel;
        private bool canScroll;
        enum Direction { Up = -1, Down = 1 }

        public ScrollPage()
        {
            this.InitializeComponent();

            WebImage.Source = new BitmapImage(new Uri("ms-appx:///Assets/newyorktimes.jpg"));
            //WebImageScroll.ScrollToVerticalOffset(200);
            //WebImageScroll.ChangeView(0, 2000, 1);

            //GazeManager.Instance.Activate(GazeManager.ApiVersion.VERSION_1_0, GazeManager.ClientMode.Push);
            GazeManager.Instance.AddGazeListener(this);
            this.DispatcherTimerSetup();
        }

        DispatcherTimer dispatcherTimer;
        public void DispatcherTimerSetup()
        {
            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += ScrollTimerTick;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 30);
            dispatcherTimer.Start();
        }

        private void ScrollTimerTick(object sender, object e)//object sender, EventArgs e)
        {
            if (!canScroll) return;
            WebImageScroll.ScrollToVerticalOffset(WebImageScroll.VerticalOffset - scrollLevel);
            //WebImageScroll.ChangeView(0, WebImageScroll.VerticalOffset - scrollLevel, 1);
            canScroll = false;
        }

        public void OnGazeUpdate(GazeData gazeData)
        {
            var x = (int)Math.Round(gazeData.SmoothedCoordinates.X, 0);
            var y = (int)Math.Round(gazeData.SmoothedCoordinates.Y, 0);
            if (x == 0 & y == 0) return;
            // Invoke thread
            //Dispatcher.BeginInvoke(new Action(() => UpdateUI(x, y)));
            UpdateUI(x, y);
        }

        private void UpdateUI(int x, int y)
        {
            DoScroll(x, y);
        }

        private void DoScroll(int x, int y)
        {
            //WebImage.Focus();

            // Scrolling based on distance to either top or bottom
            var h = this.ActualHeight; //Screen.PrimaryScreen.Bounds.Height;
            var xExp = 0.0;
            var newVar = h * ACTIVE_SCROLL_AREA;
            var doScroll = false;
            var direction = Direction.Up;
            if (y > h - newVar)
            {
                direction = Direction.Up;
                doScroll = true;
                xExp = 1 - ((h - y) / newVar);
            }
            else if (y < newVar)
            {
                direction = Direction.Down;
                doScroll = true;
                xExp = 1 - (y / newVar);
            }
            if (!doScroll) return;

            // Sigmoid function multiplied with the scroll direction and a constant SPEED_BOOST
            scrollLevel = (1 / (1 + Math.Exp((-10 * xExp) + 6))) * ((int)direction * SPEED_BOOST);
            canScroll = true;
        }
    }


}
