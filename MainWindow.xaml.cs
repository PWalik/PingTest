using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;
using System.Net.NetworkInformation;

namespace PingTest
{
    public partial class MainWindow : Window
    { 
        string IP; //IP can be changed always by editing the ping edit text box - but it's only updated if we are currently not pinging anything
        bool isCounting; //are we currently counting the seconds and doing the pinging operations
        bool isPinging; //are we currently in the middle of a ping
        string messages; //messages in the ping info window
        int time = 7; //how many seconds do we want to ping for. Could be a float for greater range of time options, but its better looking on the ui texts as full seconds
        int timeout = 5000; //how many miliseconds do we have to wait for a ping
        float count = 0; //how many steps did we take during the waiting period
        Timer timer; //timer ticking every 100 ms - used for invoking the update of the progress bar and sending of pings

        public MainWindow() //main window constructor
        {
            InitializeComponent();
            InitTimer();
            IP = PINGEditBox.Text;
            PingText.DataContext = messages;
            UpdatePingTimeMessage();
        }

        void InitTimer() //initialize the timer, add the tick event
        {
            timer = new Timer();
            timer.Interval = 100;
            timer.Elapsed += (o, e) => {
                Application.Current.Dispatcher.Invoke(() => MessagesTimerTick());
            };
        }


        private void MessagesTimerTick()
        {
            count++; //each time we tick we increase the count and update the progress bar.
            ProgressBar.Value++;
            if (count % 10 == 0 && !isPinging) //every 10th tick (so 100ms * 10 = 1s) we ping the server (if we are not currently pinging)
                Ping();

            if(count > time*10) //since each tick is 1/10th of a second, after we tick for time*10 ticks its time to stop the timer - we stop everything and give control to the user
            {
                timer.Stop();
                ProgressBar.Value = 0;
                count = 0;
                isCounting = false;
                PINGEditBox.IsEnabled = true;
                StartPingButton.IsEnabled = true;
                messages += "PING test ended.\n";
                UpdatePingText();
            }
        }

        void Ping() //start the ping by disabling the button and ip text field, and calling the async method PingHost
        {
            isPinging = true;
            PINGEditBox.IsEnabled = false;
            StartPingButton.IsEnabled = false;
            PingHost(this, count, IP, timeout);
        }

    

        static async void PingHost(MainWindow main, float count, string address, int timeout)
        {
            Ping ping = new Ping();
            Stopwatch watch = new Stopwatch(); //stopwatch is used to check how many ms passed in case of a failure (since we can't get it from reply.RountripTime)
            watch.Start();
            try //we try to ping the server 
            {
                PingReply reply = await ping.SendPingAsync(address, timeout);
                if (reply != null && main.isCounting) //only print if we are still counting - avoids a bug where the count would stop, but a ping would still print after
                {
                    main.AddNewPingMessage((int)count, reply.Status == IPStatus.Success ? true : false, address, (int)reply.RoundtripTime);

                }
            }
            catch { if (main.isCounting) main.AddNewPingMessage((int)count, false, address, (int)watch.ElapsedMilliseconds); } //this happens if we dont succeed in pinging the server

            main.StopPing();
        }

        public void StopPing()
        {
            isPinging = false;
        }

        private void Start_Ping(object sender, RoutedEventArgs e) //this starts the pinging process - timers, disabling the controls, etc
        {
            if (isCounting)
                return;

            messages = "";
            AddNewPingMessage(0, true, "", 0);
            isCounting = true;
            ProgressBar.Value = 0;
            ProgressBar.Maximum = time * 10;
            timer.Start();
        }

        private void PINGEditChanged(object sender, TextChangedEventArgs e) //what happens when you edit the PINGEditBox
        {
            if (isCounting)
                PINGEditBox.Text = IP;
            else IP = PINGEditBox.Text;
        }


        void AddNewPingMessage(int count, bool isSuccess, string address, int responseTime) //Add a new ping messages to the messages string, then update the UI text
        {
            if (count == 0)
                messages += "Odpowiedzi PING:\n";
            else
                messages += "PING status: " + (isSuccess ? "Success " : "Failure ") + "IP: " + address + " time = " + responseTime + " ms\n";
            UpdatePingText();
        }

        void UpdatePingTimeMessage() //update the button text at the start of the program (so it displays the correct time)
        {
            StartPingButton.Content = "Wysyłaj zapytania PING przez " + time + " sek";
        }

        void UpdatePingText() //update the UI text block for ping info
        {
            PingText.Text = messages;
        }

    }
}
