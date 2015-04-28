using Parse;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
        //
namespace iExpress
{
    public class ButtonEventArgs : EventArgs
    {
        public string Data { get; set; }
        public ButtonEventArgs(string data)
        {
            Data = data;
        }
    }

    class ButtonHandler
    {
        private Button button = null;
        private double init_x = 0.0;
        private double init_y = 0.0;
        private double out_x = 0.0;
        private double out_y = 0.0;
        private bool entered_button = false;
        private bool exited_button = true;
        private bool hover_button = false;
        private String name;
        private int counter;
        private int running_counter;
        private int internal_counter = 12;
        private String userName;
        private String content;

        public EventHandler<ButtonEventArgs> buttonPressDetected;
        public EventHandler<ButtonEventArgs> homeAutomationControl;

        public ButtonHandler(Button button)
        {
            this.button = button;
            Button but = button;

            var trans = but.TransformToVisual(null);
            var point = trans.TransformPoint(new Windows.Foundation.Point());

            init_x = point.X;
            init_y = point.Y;
            out_x = init_x + button.Width;
            out_y = init_y + button.Height;

            entered_button = false;
            name = button.Name.ToString();
            content = button.Content.ToString();

        }

        public ButtonHandler(Button button, bool correction)
        {
            if (correction)
            {
                this.button = button;
                Button but = button;

                var trans = but.TransformToVisual(null);
                var point = trans.TransformPoint(new Windows.Foundation.Point());

                init_x = point.X + 572;
                init_y = point.Y + 10;
                out_x = init_x + button.Width;
                out_y = init_y + button.Height;

                entered_button = false;
                name = button.Name.ToString();
                content = button.Content.ToString();
            }
        }

        public async void entered(int x, int y)
        {
            //With some analysis we came up with this padding 

            //x = x - 180;
            //y = y - 350;
            x = x  - 175;
            y = y  - 300;

            if (init_x <= x && x <= out_x && init_y <= y && y <= out_y)
            {
                //Debug.WriteLine("Init_X " + init_x + "  Init_Y " + init_y);
                //Debug.WriteLine("OUT_X " + out_x + "  OUT_Y " + out_y);
                //Debug.WriteLine("X  " + x + "     Y= " + y);
                
                if (entered_button == false && exited_button == true)
                {
                    //counter = 6;
                    //Abhi - Testing if CountDown Testing Works 
                    await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                        () =>
                        {
                            if (ApplicationData.Current.RoamingSettings.Values.ContainsKey("CountDown"))
                            {
                                counter = (int)ApplicationData.Current.RoamingSettings.Values["CountDown"];
                                counter++;
                            }
                            else
                            {
                                counter = 4;
                            }


                            running_counter = 0;
                            entered_button = true;
                            hover_button = true;
                            exited_button = false;
                        });
                }

                else if (entered_button == true && hover_button == true)
                {
                    await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                        () =>
                        {
                            running_counter++;
                            if (running_counter == internal_counter)
                            {
                                running_counter = 0;
                                counter--;
                                String location = "ms-appx:///Assets/" + counter + ".png";

                                this.button.Background = new ImageBrush { ImageSource = new BitmapImage(new Uri(location)) };

                            }
                        });

                    entered_button = true;
                    hover_button = true;
                    exited_button = false;

                    if (counter == 1)
                    {
                        await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                        () =>
                        {
                            if (this.button.Name == "b21" || this.button.Name == "b22" || this.button.Name == "b23" || this.button.Name == "b24" || this.button.Name == "b25" || this.button.Name == "b26" || this.button.Name == "b27" || this.button.Name == "b28" || this.button.Name == "b29" || this.button.Name == "b30" || this.button.Name == "b31" || this.button.Name == "b32")
                            {
                                ButtonEventArgs arg = new ButtonEventArgs(this.button.Name);
                                homeAutomationControl(this.button, arg);
                            }

                            if(this.button.Name == "b7")
                            {
                                ButtonEventArgs arg = new ButtonEventArgs("home automation");
                                buttonPressDetected(this, arg);
                            }
                            
                            if(this.button.Name == "b8") 
                            {
                                ButtonEventArgs arg = new ButtonEventArgs("read");
                                buttonPressDetected(this, arg);
                            }
                            if (this.button.Name == "b9")
                            {
                                ButtonEventArgs arg = new ButtonEventArgs("media");
                                buttonPressDetected(this, arg);
                            }

                            this.button.Background = new ImageBrush { ImageSource = new BitmapImage(new Uri("ms-appx:///Assets/Sent.png")) };

                            if(this.button.Name == "b4")
                            {
                                this.button.Background = new ImageBrush { ImageSource = new BitmapImage(new Uri("ms-appx:///Assets/Like.png")) };
                            }

                            if (this.button.Name == "b5")
                            {
                                this.button.Background = new ImageBrush { ImageSource = new BitmapImage(new Uri("ms-appx:///Assets/Dislike.png")) };
                            }

                            if (ApplicationData.Current.RoamingSettings.Values.ContainsKey("UserName"))
                                userName = (String)ApplicationData.Current.RoamingSettings.Values["UserName"];
                            else
                                userName = "Patient";

                            if (hover_button == true)
                            {
                                String message = this.content;
                                ParsePush push = new ParsePush();
                                push.Channels = new List<String> { "global" };
                                IDictionary<string, object> dic = new Dictionary<string, object>();
                                
                                //Abhishek: Changes for Hard Notification
                                if(name.Equals("b1")) dic.Add("sound", "emergency.caf");
                                else dic.Add("sound", ".");
                                dic.Add("alert", userName + ": " + message);
                                push.Data = dic;
                                push.SendAsync();


                                ParseObject internal_tweets = new ParseObject("TweetsInternal");
                                internal_tweets["content"] = message;
                                internal_tweets["sender"] = userName;
                                internal_tweets.SaveAsync();
                            }
                        });

                        hover_button = false;
                    }

                }

            }
            else
            {

                if (entered_button == true && exited_button == false)
                {
                    await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                       () =>
                       {
                           this.button.Background = null;
                       });

                    exited_button = true;
                    hover_button = false;
                    entered_button = false;
                }

            }

        }
    }
}
