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
using System.Diagnostics;
using TETCSharpClient;
using TETCSharpClient.Data;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace iExpress
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MediaPage : Page, IGazeListener
    {
        public MediaPage()
        {
            this.InitializeComponent();
            GazeManager.Instance.AddGazeListener(this);
           // mediaControl.Source =  new Uri("msappx:///Assets/Floyd1.mp3");
            //SetLocalMedia();
            mediaControl.Play();
        }

        async private void SetLocalMedia()
        {
            var openPicker = new Windows.Storage.Pickers.FileOpenPicker();

            openPicker.FileTypeFilter.Add(".wmv");
            openPicker.FileTypeFilter.Add(".mp4");
            openPicker.FileTypeFilter.Add(".wma");
            openPicker.FileTypeFilter.Add(".mp3");

            var file = await openPicker.PickSingleFileAsync();

            var stream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read);

            // mediaControl is a MediaElement defined in XAML
            if (null != file)
            {
                mediaControl.SetSource(stream, file.ContentType);

                mediaControl.Play();
            }
        }

        public void OnGazeUpdate(GazeData gazeData)
        {
            var x = (int)Math.Round(gazeData.SmoothedCoordinates.X, 0);
            var y = (int)Math.Round(gazeData.SmoothedCoordinates.Y, 0);
            if (x == 0 & y == 0) return;
            // Invoke thread
            //Dispatcher.BeginInvoke(new Action(() => UpdateUI(x, y)));
           
            if (y < 10)
            {
                mediaControl.Play();
            }
            else if (y > this.ActualHeight - 10)
            {
                mediaControl.Pause();
            }
        }


    }

   
}
