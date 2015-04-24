using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Diagnostics;
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
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;

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
        private const double ACTIVE_SCROLL_AREA =1; // 25% top and bottom
        private const int MAX_IMAGE_WIDTH = 1600;
        //private readonly double dpiScale;
        //private Matrix transfrm;
        private double scrollLevel;
        private bool canScroll;
        private readonly DispatcherTimer scrollTimer;

        enum Direction { Up = -1, Down = 1, Middle = 0 }

        private static double slevel = 0;
        private static Direction curState;

        public ScrollPage()
        {
            this.InitializeComponent();

            WebImage.Source = new BitmapImage(new Uri("ms-appx:///Assets/newyorktimes.jpg"));
           // WebImageScroll.ScrollToVerticalOffset(2000);

            scrollTimer = new DispatcherTimer();
            scrollTimer.Interval = new TimeSpan(0, 0, 0, 0, 30);
            scrollTimer.Tick += ScrollTimerTick;
            scrollTimer.Start();


            //WebImageScroll.ChangeView(0, 2000, 1);
            this.testScroll();

            Debug.WriteLine("this.ActualHeight : "+this.ActualHeight + "this.Height: " +this.Height);

            //GazeManager.Instance.Activate(GazeManager.ApiVersion.VERSION_1_0, GazeManager.ClientMode.Push);
            GazeManager.Instance.AddGazeListener(this);
        }

        public async void testScroll()
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                WebImageScroll.ScrollToVerticalOffset(2000.0d);
                //WebImageScroll.ChangeView(0.0f, 200.0f, 1.0f);
            });
        }

        private async void ScrollTimerTick(object sender, object e)
        {

            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                // if (!canScroll) return;
                //WebImageScroll.ScrollToVerticalOffset(WebImageScroll.VerticalOffset - scrollLevel);
                //Debug.WriteLine("Is it even getting called?");

                double delta;
                switch(curState) {
                    case Direction.Up:
                        delta = -3;
                        break;
                    case Direction.Middle:
                    default:
                        delta = 0;
                        break;
                    case Direction.Down:
                        delta = 3;
                        break;
                }
                slevel += delta;
                if (delta > WebImageScroll.ScrollableHeight)
                {
                    delta = WebImageScroll.ScrollableHeight;
                }
                else if (delta < 0)
                {
                    delta = 0;
                }
                WebImageScroll.ChangeView(0, slevel, 0.4f);
                canScroll = false;
            });
        }

        public async void OnGazeUpdate(GazeData gazeData)
        {
            var x = (int)Math.Round(gazeData.SmoothedCoordinates.X, 0);
            var y = (int)Math.Round(gazeData.SmoothedCoordinates.Y, 0);
            if (x == 0 & y == 0) return;
            // Invoke thread
            //Dispatcher.BeginInvoke(new Action(() => UpdateUI(x, y)));

            if (y < 500)
            {
                curState = Direction.Up;
            }
            else if (y > 1500)
            {
                curState = Direction.Down;
            }
            else
            {
                curState = Direction.Middle;
            }

            /*
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                UpdateUI(x, y);
            });*/
        }

        private void UpdateUI(int x, int y)
        {
              DoScroll(x, y);
        }

        private void DoScroll(int x, int y)
        {
            //WebImage.Focus();
            Debug.WriteLine("Gaze x: "+x + "   Gaze y: "+y);
            // Scrolling based on distance to either top or bottom
            var h = this.Frame.ActualHeight;// this.ActualHeight; //Screen.PrimaryScreen.Bounds.Height;

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
            Debug.WriteLine("scrollLevel" + scrollLevel);
            canScroll = true;
        }
    }




}
