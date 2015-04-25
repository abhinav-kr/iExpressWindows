using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Parse;
using iExpress.Common;
using Windows.Storage;
using Windows.UI.ApplicationSettings;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI;
using Windows.UI.Xaml.Media.Imaging;
using System.Diagnostics;
using TETCSharpClient;
using TETCSharpClient.Data;
using Windows.UI.Popups;
using Windows.UI.Core;
using Windows.ApplicationModel.Core;
using Windows.System.Threading;
using Windows.Networking.PushNotifications;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace iExpress
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class HomeAutomationPage : Page, IGazeListener, IConnectionStateListener, ITrackerStateListener
    {
        #region initialized variables

        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();

        private Boolean entered;
        private Boolean exited;
        private int counter;
        private int internal_counter = 36;
        private int running_counter;
        private String UserName;
        private List<ButtonHandler> buttons = null;
        private int runningTemp;
        private static String dbTemp;
        public static Dictionary<string, string> tagIds;

        // This will be used when the eye tribe is disconnected
        private ThreadPoolTimer PeriodicTimer = null;
        private int timer_duration = 10000;

        #endregion

        #region defined classes

        //private class TempUpdate
        //{
        //    public string new_temp { get; set; }
        //    public string curr_temp { get; set; }
        //}


        //private class SwitchUpdate
        //{
        //    public string switch_to_change { get; set; }
        //    public string new_switch_status { get; set; }
        //}

        private class ComponentUpdate
        {
            public string tag_id { get; set; }
            public string required_value { get; set; }
        }

        private class DBResponse
        {
            public int id { get; set; }
            public string user_name { get; set; }
            public string tag_id { get; set; }
            public string signal_type { get; set; }
            public string current_value { get; set; }
            public string required_value { get; set; }
            public string mode { get; set; }
        }

        /// <summary>
        /// This can be changed to a strongly typed view model.
        /// </summary>
        public ObservableDictionary DefaultViewModel
        {
            get { return this.defaultViewModel; }
        }

        /// <summary>
        /// NavigationHelper is used on each page to aid in navigation and 
        /// process lifetime management
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }

        #endregion

        public HomeAutomationPage()
        {

            //GazeManager.Instance.Activate(GazeManager.ApiVersion.VERSION_1_0, GazeManager.ClientMode.Push);
            GazeManager.Instance.AddGazeListener(this);

            // Add listener if EyeTribe Server is closed
            GazeManager.Instance.AddConnectionStateListener(this);
            GazeManager.Instance.AddTrackerStateListener(this);

            if (!GazeManager.Instance.IsActivated)
            {
                Debug.WriteLine("IsActivated not ");
                //errorMessage("Eye Tribe Is Not Active.");
            }


            this.InitializeComponent();

            buttons = new List<ButtonHandler>();
            buttons.Add(new ButtonHandler(this.b1));
            buttons.Add(new ButtonHandler(this.b2));
            buttons.Add(new ButtonHandler(this.b3));
            buttons.Add(new ButtonHandler(this.b4));
            buttons.Add(new ButtonHandler(this.b5));
            buttons.Add(new ButtonHandler(this.b6));
            buttons.Add(new ButtonHandler(this.b7));
            buttons.Add(new ButtonHandler(this.b8));
            buttons.Add(new ButtonHandler(this.b9));
            buttons.Add(new ButtonHandler(this.b10));
            buttons.Add(new ButtonHandler(this.b11));

            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += navigationHelper_LoadState;
            this.navigationHelper.SaveState += navigationHelper_SaveState;

            /* Initialize dictionary for switch & thermostat tag IDs */
            tagIds = new Dictionary<string, string>();
            tagIds.Add("1", "115914");
            tagIds.Add("2", "115498");
            tagIds.Add("3", "115341");
            tagIds.Add("4", "1155B6");
            tagIds.Add("temp", "11B264");

            /* Initialize temperature variables and display */
            runningTemp = 0;
            runningTemp = loadCurrTemp();
            currTemp.Content = runningTemp.ToString();
        }

        private int loadCurrTemp()
        {
            dbTemp = "0";

            // Get current temp with API call
            string tagID = "";
            tagIds.TryGetValue("temp", out tagID);
            DBResponse responseObject = GetRequest(tagID);

            // Set the current temperature to the server's current_value
            dbTemp = convertTempH2I(responseObject.current_value);

            // Parse Temp to int
            int tempAsInt = 0;
            Boolean success = int.TryParse(dbTemp, out tempAsInt);
            if (!success) 
            {
                tempAsInt = 0;
                Debug.WriteLine("Could not parse temperature from database. Value received = {0}", dbTemp);
                currTemp.Content = "Err";
            }
            else if (!tempInRange(tempAsInt))
            {
                tempAsInt = 0;
                Debug.WriteLine("Temperature from database out of range.", dbTemp);
                currTemp.Content = "Err";
            }

            return tempAsInt;
        }

        private Boolean tempInRange(int temp)
        {
            if (temp < 5 || temp > 90)
            {
                return false;
            }
            return true;
        }

        /* Convert the hex string from the database response 
         * with the formula: 
         * hex string => int value => int/2 => int/2 string
         */
        private string convertTempH2I(string hexString)
        {
            // Convert directly from a hex string to an int
            Int32 tempVar = Int32.Parse(hexString, System.Globalization.NumberStyles.HexNumber);

            return (tempVar/2).ToString();
        }

        /* Convert the hex string from the database response 
         * with the formula: 
         * int string => int value => int*2 => convert int*2 to 2-digit hex string
         */
        private string convertTempI2H(string intString)
        {
            // Parse intString to int
            int intVal;
            Boolean success = int.TryParse(intString, out intVal);
            if (!success)
            {
                intVal = 0;
                Debug.WriteLine("Parsing error in convertTempI2H");
            }

            Int32 tempVar = (Int32)(intVal * 2);
            
            // Convert to a 2-digit hex string
            return tempVar.ToString("x2");
        }

        #region unused code

        /// <summary>
        /// Populates the page with content passed during navigation. Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="sender">
        /// The source of the event; typically <see cref="NavigationHelper"/>
        /// </param>
        /// <param name="e">Event data that provides both the navigation parameter passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested and
        /// a dictionary of state preserved by this page during an earlier
        /// session. The state will be null the first time a page is visited.</param>
        private void navigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
        }

        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache.  Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="sender">The source of the event; typically <see cref="NavigationHelper"/></param>
        /// <param name="e">Event data that provides an empty dictionary to be populated with
        /// serializable state.</param>
        private void navigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
        }

        #region NavigationHelper registration

        /// The methods provided in this section are simply used to allow
        /// NavigationHelper to respond to the page's navigation methods.
        /// 
        /// Page specific logic should be placed in event handlers for the  
        /// <see cref="GridCS.Common.NavigationHelper.LoadState"/>
        /// and <see cref="GridCS.Common.NavigationHelper.SaveState"/>.
        /// The navigation parameter is available in the LoadState method 
        /// in addition to page state preserved during an earlier session.

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            navigationHelper.OnNavigatedFrom(e);
        }

        #endregion

        #endregion
        private void mouseEntered(object sender, PointerRoutedEventArgs e)
        {

            Windows.UI.Xaml.Controls.Button but = (sender as Windows.UI.Xaml.Controls.Button);
            String message = but.Content.ToString();


            Debug.WriteLine(sender.GetHashCode() + "Detected the entering of the button");


            entered = true;
            exited = false;

            if (ApplicationData.Current.RoamingSettings.Values.ContainsKey("CountDown"))
            {
                counter = (int)ApplicationData.Current.RoamingSettings.Values["CountDown"];
                counter++;
            }
            else if (message == "Increase Temp" || message == "Decrease Temp")
            {
                counter = 3;
            }
            else
            {
                counter = 6;
            }

            running_counter = 0;
        }

        private void mouseExited(object sender, PointerRoutedEventArgs e)
        {
            Debug.WriteLine(sender.GetHashCode() + "Detected the exiting of the button");
            exited = true;
            entered = false;

            (sender as Windows.UI.Xaml.Controls.Button).Background = null;
        }

        private void mousedMoved(object sender, PointerRoutedEventArgs e)
        {
            if (entered == true && exited == false)
            {
                running_counter++;
                if (running_counter == internal_counter)
                {
                    running_counter = 0;
                    counter--;
                    String location = "ms-appx:///Assets/" + counter + ".png";
                    (sender as Windows.UI.Xaml.Controls.Button).Background = new ImageBrush { ImageSource = new BitmapImage(new Uri(location)) };


                }

                if (counter == 1)
                {

                    if (ApplicationData.Current.RoamingSettings.Values.ContainsKey("UserName"))
                        UserName = (String)ApplicationData.Current.RoamingSettings.Values["UserName"];
                    else
                        UserName = "Patient";

                    Windows.UI.Xaml.Controls.Button but = (sender as Windows.UI.Xaml.Controls.Button);
                    String message = but.Content.ToString();

                    Debug.WriteLine("Trigger execution!!!!!!!!");
                    but.Background = new ImageBrush { ImageSource = new BitmapImage(new Uri("ms-appx:///Assets/Sent.png")) };

                    if (message == "Increase Temp")
                    {
                        runningTemp++;
                        String displayTemp = runningTemp.ToString();

                        targetTemp.Content = displayTemp;
                    }
                    else if (message == "Decrease Temp")
                    {
                        runningTemp--;
                        String displayTemp = runningTemp.ToString();

                        targetTemp.Content = displayTemp;
                    }
                    else if (message == "Update Temp")
                    {
                        String displayTemp = runningTemp.ToString();

                        string tagId = "";
                        tagIds.TryGetValue("temp", out tagId);
                        ComponentUpdate tempUpdate = new ComponentUpdate
                        {
                            tag_id = tagId,
                            required_value = convertTempI2H(displayTemp)
                        };

                        // Send request with this data as the POST body
                        bool successCode = PutRequest(tempUpdate);

                        if (successCode)
                        {
                            // Update the current temperature box on page to match displayTemp
                            currTemp.Content = displayTemp;
                        }
                        else
                        {
                            Debug.WriteLine("Server error occurred while trying to update temperature.");
                            currTemp.Content = "Err";
                        }
                    }
                    else                     
                    {
                        //THIS IS A SWITCH ON/OFF BUTTON

                        // Index of the switch number in the button's Content field
                        int numIndex = 7;
                        string switchNum = message.Substring(numIndex,1);
                        string tagId = "";

                        //message.Substring takes the switch number from the button
                        // 1 corresponds to increase
                        // 0 corresponds to decrease
                        if (message.Contains("ON"))
                        {
                            // Get tagID from dictionary
                            tagIds.TryGetValue(switchNum, out tagId);

                            ComponentUpdate switchUpdate = new ComponentUpdate
                            {
                                tag_id = tagId,
                                required_value = "1"
                            };

                            // Send request with this data as the POST body
                            PutRequest(switchUpdate);

                        }
                        else if (message.Contains("OFF"))
                        {
                            // Get tagID from dictionary
                            tagIds.TryGetValue(switchNum, out tagId);

                            ComponentUpdate switchUpdate = new ComponentUpdate
                            {
                                tag_id = tagId,
                                required_value = "0"
                            };

                            // Send request with this data as the POST body
                            PutRequest(switchUpdate); 
                        }
                    }

                    entered = false;
                    exited = true;
                }
            }
        }


        private static bool PutRequest(ComponentUpdate updateObject)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri("http://128.2.83.208:8001/");
                client.DefaultRequestHeaders.Accept.Clear();

                // First make a get request to obtain the current state of the server for this component
                DBResponse responseObject = GetRequest(updateObject.tag_id);

                // Now set the required value to your updated required_value
                responseObject.required_value = updateObject.required_value;

                // Serialize this to JSON
                string jsonObject = JsonConvert.SerializeObject(responseObject);
                Debug.WriteLine("This is the response object I retrieved:");
                Debug.WriteLine(jsonObject);

                // Convert jsonObject to HttpContent for the request
                HttpContent content = new StringContent(jsonObject);
                content.Headers.ContentType = new MediaTypeWithQualityHeaderValue("application/json");

                // Make a PUT request to the REST server and return response
                // .Result forces this async call to wait until response terminates before it returns
                HttpResponseMessage response = client.PutAsync("api/v1/homeautomation/ha_user", content).Result;

                if (!response.IsSuccessStatusCode)
                {
                    Debug.WriteLine("Unsuccessful PUT request!!!!");
                    return false;
                }
            }
            return true;
        }

        
        private static DBResponse GetRequest(String tagId)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri("http://128.2.83.208:8001/");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Makes a GET request to the REST server and returns response body as string
                // .Result forces this async call to wait until response terminates before it returns
                string response = client.GetStringAsync("api/v1/homeautomation/ha_user").Result;

                if (response != "")
                {
                    // Deserialize the server response into a list of response objects
                    Debug.WriteLine(response);
                    List<DBResponse> dbResponseList = JsonConvert.DeserializeObject<List<DBResponse>>(response);
                    Debug.WriteLine("Got the response objects. Printing them now...");

                    Debug.WriteLine("My tagId is {0}", tagId);

                    foreach (DBResponse resp in dbResponseList)
                    {
                        Debug.WriteLine(resp.tag_id);

                        if (resp.tag_id == tagId)
                        {
                            return resp;
                        }
                    }
                }
                return new DBResponse();
            }
        }

        
        #region backing functions

        public void OnGazeUpdate(GazeData gazeData)
        {
            // start or stop tracking lost animation
            if ((gazeData.State & GazeData.STATE_TRACKING_GAZE) == 0 &&
                (gazeData.State & GazeData.STATE_TRACKING_PRESENCE) == 0) return;
            var x = (int)Math.Round(gazeData.SmoothedCoordinates.X, 0);
            var y = (int)Math.Round(gazeData.SmoothedCoordinates.Y, 0);
            //var gX = Smooth ? gazeData.SmoothedCoordinates.X : gazeData.RawCoordinates.X;
            //var gY = Smooth ? gazeData.SmoothedCoordinates.Y : gazeData.RawCoordinates.Y;
            //var screenX = (int)Math.Round(x + gX, 0);
            //var screenY = (int)Math.Round(y + gY, 0);
            // Debug.WriteLine("OnGazeUpdate       " + x + "    " + y);

            // return in case of 0,0 
            if (x == 0 && y == 0) return;

            determine_Button(x, y);

        }

        private void determine_Button(int x, int y)
        {
            if (buttons != null)
            {
                for (int i = 0; i < buttons.Count(); i++)
                {
                    buttons.ElementAt(i).entered(x, y);
                }
            }
        }

        private async void errorMessage(String value)
        {
            try
            {
                MessageDialog msgDialog = new MessageDialog(value, "Error");
                await msgDialog.ShowAsync();
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
            }
        }


        // server connection check
        public void OnConnectionStateChanged(bool isConnected)
        {
            if (!GazeManager.Instance.IsActivated)
            {
                //GazeManager.Instance.Deactivate();
                Debug.WriteLine("Deactivate");

                errorMessage("Gaze have been disconnected.");
            }
        }


        public void OnScreenStatesChanged(int screenIndex, int screenResolutionWidth, int screenResolutionHeight, float screenPhysicalWidth, float screenPhysicalHeight)
        {
            // do not need this
        }

        public void OnTrackerStateChanged(GazeManager.TrackerState trackerState)
        {
            switch (trackerState)
            {
                case GazeManager.TrackerState.TRACKER_CONNECTED:
                    Debug.WriteLine("TRACKER_CONNECTED");
                    sysMessage("Eye Tracker has been connected.");
                    if (PeriodicTimer != null)
                    {
                        PeriodicTimer.Cancel();

                        PeriodicTimer = null;
                    }
                    break;
                case GazeManager.TrackerState.TRACKER_CONNECTED_NOUSB3:
                    Debug.WriteLine("TRACKER_CONNECTED_NOUSB3");
                    break;
                case GazeManager.TrackerState.TRACKER_CONNECTED_BADFW:
                    Debug.WriteLine("TRACKER_CONNECTED_BADFW");
                    break;
                case GazeManager.TrackerState.TRACKER_NOT_CONNECTED:
                    Debug.WriteLine("TRACKER_NOT_CONNECTED");
                    sysMessage("Eye Tracker has been disconnected.");
                    PeriodicTimer = ThreadPoolTimer.CreatePeriodicTimer(TimerElapsedHandler,
                                        TimeSpan.FromMilliseconds(timer_duration));
                    break;
                case GazeManager.TrackerState.TRACKER_CONNECTED_NOSTREAM:
                    Debug.WriteLine("TRACKER_CONNECTED_NOSTREAM");
                    break;
            }
        }

        private void TimerElapsedHandler(ThreadPoolTimer timer)
        {
            Debug.WriteLine("TimerElapsedHandler");
            sysMessage("Eye Tracker has been disconnected.");
        }

        private async void sysMessage(String msg)
        {

            if (ApplicationData.Current.RoamingSettings.Values.ContainsKey("UserName"))
                UserName = (String)ApplicationData.Current.RoamingSettings.Values["UserName"];
            else
                UserName = "Patient";

            String message = msg;
            ParsePush push = new ParsePush();
            push.Channels = new List<String> { "testing" };
            IDictionary<string, object> dic = new Dictionary<string, object>();
            dic.Add("sound", ".");
            dic.Add("alert", message);
            push.Data = dic;
            await push.SendAsync();


            ParseObject internal_tweets = new ParseObject("TweetsInternal");
            internal_tweets["content"] = message;
            internal_tweets["sender"] = UserName;
            await internal_tweets.SaveAsync();
        }

        private async void updateNotification(object sender, PushNotificationReceivedEventArgs e)
        {
            Debug.WriteLine("Debug :: This is from the ToastNotificationReceived    ");

            var query = from comment in ParseObject.GetQuery("TweetsInternal")
                                    .Limit(2)
                        orderby comment.CreatedAt descending
                        select comment;

            IEnumerable<ParseObject> comments = await query.FindAsync();


            bool top = true;
            string forNotification1 = "";
            string forNotification2 = "";

            foreach (ParseObject p in comments)
            {
                if (top)
                {
                    string format = "hh:mm tt";
                    var dt = p.CreatedAt;
                    forNotification1 = dt.Value.ToLocalTime().ToString(format) + " " + p.Get<string>("sender") + ": " + p.Get<string>("content");
                    top = !top;
                }
                else
                {
                    string format = "hh:mm tt";
                    var dt = p.CreatedAt;
                    forNotification2 = dt.Value.ToLocalTime().ToString(format) + " " + p.Get<string>("sender") + ": " + p.Get<string>("content");
                }
            }
            //await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
            //() =>
            //{
            //    this.notification2.Text = forNotification2;
            //    this.notification1.Text = forNotification1;
            //});

        }
        #endregion
    }
}
