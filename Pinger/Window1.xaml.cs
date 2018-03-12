using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
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


namespace Pinger
{
    public partial class MainWindow : Window
    {
        Window2 window2 = new Window2();

        public MainWindow()
        {
            InitializeComponent();
        }



        // ============================================================================================== Main Button Trigger
        // ============================================================================================== 
        // ============================================================================================== 

        IPStatus status = IPStatus.Unknown;
        int ttl = 0;
        long rtt = 0;
        int bitesize = 0;
        int seq = 0;

        private delegate void ThreadDelegate();
        IPAddress addr = IPAddress.Parse("127.0.0.1");
        IPHostEntry hostEntry;
        string devicetype = "dg";
        bool pingcomplete;
        bool singlecomplete = false;
        public bool cancelrequested = false;
        string rateslidervalue = "1";
        long roundtriptime;
        int rateping = 1;
        int pingtimes;
        int lastseq = 0;
        int senttotal = 0;
        string addressinputvar;
        string box1 = "";
        byte[] icmpdata = new byte[1];
        int currentoperationindex = 0;
        int ri1rttmax;
        int ri1rttmin;
        double ri1rttaverage;
        int receivedtotal;
        int losttotal;
        string storenumber;
        int pingindex = 0;
        bool isValidIp;
        List<int> rtts = new List<int>();
        int resttime = 1000;
        string outs = "";
        int val = 10;
        Ping pingSender = new Ping();
        
        bool meswarn = false;

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            PingButton.IsEnabled = false;
            pingcomplete = false;
            lastseq = 0;
            cancelrequested = false;
            storenumber = "";
            window2.LogBox2.AppendText("\n=======" + Convert.ToString(DateTime.Now) + "=======\n");
            progressbar.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadDelegate(new Action(() => UpdateProgressbar(0))));
            CancelButton.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadDelegate(EnableCancelButton));
            Outputfield.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadDelegate(clearoutbox));
            StatisticsBox.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadDelegate(UpdateBox0));
            ParseInput();
            ThreadDelegate pinger = new ThreadDelegate(Repeaticmp);
            pinger.BeginInvoke(null, null);
            //ThreadDelegate parser = new ThreadDelegate(ParseInput);
            //parser.BeginInvoke(null, null);

        }


        private void Repeaticmp()
        {
            progressbar.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadDelegate(new Action(() => UpdateProgressbar(10))));
            receivedtotal = 0;
            senttotal = 0;
            losttotal = 0;
            for (int i = 1; i <= pingtimes; i++)
            {
                singlecomplete = false;
                AnimateprogressbarAsync();
                Task taskA = Task.Factory.StartNew(() => Sendicmp(addr));
                taskA.Wait();
                singlecomplete = true;
                try
                {
                    ri1rttmax = rtts.Max();
                    ri1rttmin = rtts.Min();
                    ri1rttaverage = Convert.ToInt16(rtts.Average());
                }
                catch (Exception e)
                {
                    MessageBox.Show(Convert.ToString(e));
                    MessageBox.Show("Failed to accurately calculate averages for statistics, probably because no replies were received.");
                    ri1rttaverage = 0;
                    ri1rttmax = 0;
                    ri1rttmin = 0;
                    continue;
                }
                if (rateping < 24)
                {
                    if (i != pingtimes)
                    {
                        Task taskB = Task.Factory.StartNew(() => UpdateUserInterface(bitesize, ttl));
                        taskB.Wait();
                    }
                }
                else
                {
                    if (i % (rateping/24) == 0)
                    {
                        if (i != pingtimes)
                        {
                            Task taskD = Task.Factory.StartNew(() => UpdateUserInterface(bitesize, ttl));
                            taskD.Wait();
                        }
                    }
                }
                    if (i < pingtimes)
                    {
                        Thread.Sleep(resttime);
                    }
                if (cancelrequested == true)
                {
                    break;
                }

            }
            Task taskE = Task.Factory.StartNew(() => UpdateUserInterface(bitesize, ttl));
            taskE.Wait();
            progressbar.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadDelegate(new Action(() => UpdateProgressbar(0))));
            pingcomplete = true;
            window2.LogBox2.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadDelegate(new Action(() => UpdateLogBox("\n" + box1 + "\n"))));
            window2.LogBox2.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadDelegate(window2.LogBox2.ScrollToEnd));
            Outputfield.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadDelegate(UpdateUserInterfacePingComplete));
            window2.LogBox2.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadDelegate(new Action(() => UpdateLogBox("Ping complete\n"))));
            CancelButton.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadDelegate(DisableCancelButton));
            PingButton.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadDelegate(EnablePingButton));
        }



        private void Sendicmp(IPAddress addresstoping)
        {
            
            status = IPStatus.Unknown;
            rtt = 0;
            bitesize = 0;
            ttl = 0;
            seq = 0;
            PingReply reply = null;
            
            try
            {
                Task updatetask = Task.Factory.StartNew(() => reply = pingSender.Send(addresstoping, 5000, icmpdata));
                updatetask.Wait();
                senttotal++;
            }
            catch (PingException e)
            {
                MessageBox.Show("Exception thrown while sending ping " + Convert.ToString(e));
                status = IPStatus.Unknown;
                return;
            }
            status = reply.Status;
            rtt = reply.RoundtripTime;
            bitesize = reply.Buffer.Length;
            rtts.Add(Convert.ToInt16(reply.RoundtripTime));
            try
            {
                ttl = (reply.Options.Ttl);
            }
            catch (NullReferenceException e)
            {
                //MessageBox.Show("Null ref exception while setting ttl " + Convert.ToString(e));
                ttl = 0;
            }
            if (status == IPStatus.Success)
            {
                receivedtotal++;
            }
            else
            {
                losttotal++;
                return;
            }
            //MessageBox.Show("Status after ping " + Convert.ToString(status));
            return;
        }
        

        //.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadDelegate(new Action(() => )));
        private void UpdateUserInterface(int bitesize, int ttl)
        {
            Outputfield.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadDelegate(Outputfield.ScrollToEnd));
            //MessageBox.Show("status in update is " + Convert.ToString(status));
            if (cancelrequested == true)
            {
                return;
            }
            if (status == IPStatus.Success)
            {
                Outputfield.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadDelegate(new Action(() => Outputfield.AppendText(senttotal + " Reply from " + addr + ": bytes=" + bitesize + " time=" +
            rtt + "ms TTL=" + ttl + "\n"))));
                window2.LogBox2.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadDelegate(new Action(() => window2.LogBox2.AppendText(senttotal + " Reply from " + addr + ": bytes=" + bitesize + " time=" +
            rtt + "ms TTL=" + ttl + "\n"))));
            if (storenumber == "")
                {
                    StatisticsBox.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadDelegate(new Action(() => box1 = StatisticsBox.Text = "Ping statistics for " + addr + ":\n    Packets: Sent = " + senttotal + ", Received = " + receivedtotal +
                                         ", Lost = " + losttotal + "(" + (Convert.ToInt16(((Convert.ToDouble(losttotal) / (Convert.ToDouble(senttotal)) * 100.0)))) + "% loss),\n"
                                         + "Approximate rount trip times in milli-seconds:\n"
                                         + "    Minimum = " + ri1rttmin + "ms, Maximum = " + ri1rttmax + "ms, Average = " + ri1rttaverage + "ms\n")));

                    window2.LogBox2.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadDelegate(window2.LogBox2.ScrollToEnd));
            }
            if (storenumber != "")
                {
                    StatisticsBox.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadDelegate(new Action(() => box1 = StatisticsBox.Text = "Ping statistics for " + addr + "(" + storenumber + "):\n    Packets: Sent = " + senttotal + ", Received = " + receivedtotal +
                                         ", Lost = " + losttotal + "(" + (Convert.ToInt16(((Convert.ToDouble(losttotal) / (Convert.ToDouble(senttotal)) * 100.0)))) + "% loss),\n"
                                         + "Approximate rount trip times in milli-seconds:\n"
                                         + "    Minimum = " + ri1rttmin + "ms, Maximum = " + ri1rttmax + "ms, Average = " + ri1rttaverage + "ms\n")));
                    window2.LogBox2.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadDelegate(window2.LogBox2.ScrollToEnd));
                }
            }
            else
            {
                switch (status)
                {
                    case IPStatus.TimedOut:
                        outs = (senttotal + " Request timed out.\n");
                        break;
                    case IPStatus.BadDestination:
                        outs = (senttotal + " Bad destination\n");
                        break;
                    case IPStatus.DestinationHostUnreachable:
                        outs = (senttotal + " Destination host unreachable.\n");
                        break;
                    case IPStatus.DestinationNetworkUnreachable:
                        outs = (senttotal + " Destination network unreachable.\n");
                        break;
                    case IPStatus.DestinationUnreachable:
                        outs = (senttotal + " Destination unreachable.\n");
                        break;
                    case IPStatus.HardwareError:
                        outs = (senttotal + " Hardware failure.\n");
                        break;
                    case IPStatus.NoResources:
                        outs = (senttotal + " Insufficient resources to complete request.\n");
                        break;
                    case IPStatus.PacketTooBig:
                        outs = (senttotal + " Packet sent was too big.\n");
                        break;
                    case IPStatus.ParameterProblem:
                        outs = (senttotal + " Parameters are invalid.\n");
                        break;
                    case IPStatus.SourceQuench:
                        outs = (senttotal + " Reported source quench.\n");
                        break;
                    case IPStatus.TimeExceeded:
                        outs = (senttotal + " TTL expired in transit.\n");
                        break;
                    case IPStatus.TtlExpired:
                        outs = (senttotal + " TTL expired in transit.\n");
                        break;
                    case IPStatus.TtlReassemblyTimeExceeded:
                        outs = (senttotal + " TTL expired in transit.\n");
                        break;
                    case IPStatus.Unknown:
                        outs = (senttotal + " Unknown Failure.\n");
                        break;
                    case IPStatus.UnrecognizedNextHeader:
                        outs = (senttotal + " Unrecognized next header.\n");
                        break;
                }
                Outputfield.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadDelegate(new Action(() => Outputfield.AppendText(outs))));
                window2.LogBox2.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadDelegate(new Action(() => window2.LogBox2.AppendText(outs))));
                window2.LogBox2.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadDelegate(window2.LogBox2.ScrollToEnd));
            }
        }




        
        private async Task AnimateprogressbarAsync()
        {
            val = 10;
            while (val < 100)
            {
                if (singlecomplete == true)
                {
                    progressbar.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadDelegate(new Action(() => UpdateProgressbar(0))));
                    return;
                }
                if (cancelrequested == false)
                {
                        progressbar.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadDelegate(new Action(() => UpdateProgressbar(val))));
                        await Task.Delay(TimeSpan.FromSeconds(0.112));
                        val = val + 2;
                }
                else
                {
                    return;
                }
            }
        }










        //        ==============================
        //        ==============================
        //        ==============================
        //        ==============================
        //        ==============================
        //        ==============================
        //        ==============================
        //        ==============================
        //        ==============================
        //===============================Functions that are under control======================
        //        ==============================
        //        ==============================
        //        ==============================
        //        ==============================
        //        ==============================
        //        ==============================
        //        ==============================
        //        ==============================





        private void ParseInput()
        {
            Outputfield.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadDelegate(ResolvingHostUpdate));
            progressbar.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadDelegate(new Action(() => UpdateProgressbar(25))));
            isValidIp = IPAddress.TryParse(addressinputvar, out addr);
            if (addressinputvar != Convert.ToString(addr))
            {
                storenumber = addressinputvar.PadLeft(5, '0');
                storenumber = devicetype + storenumber;
                try
                {
                    hostEntry = Dns.GetHostEntry(storenumber);
                }
                catch (System.Net.Sockets.SocketException)
                {
                    cancelrequested = true;
                    CancelButton.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadDelegate(DisableCancelButton));
                    PingButton.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadDelegate(EnablePingButton));
                    Outputfield.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadDelegate(UpdateOutputFailed));
                    Outputfield.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadDelegate(UpdateUserInterfacePingComplete));
                    progressbar.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadDelegate(new Action(() => UpdateProgressbar(0))));
                    return;
                }
                addr = hostEntry.AddressList[0];
                progressbar.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadDelegate(new Action(() => UpdateProgressbar(50))));
            }
            else
            {
                addr = IPAddress.Parse(addressinputvar);
            }
            progressbar.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadDelegate(new Action(() => UpdateProgressbar(0))));
            Outputfield.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadDelegate(clearoutbox));
        }


        private void Address_TextChanged(object sender, TextChangedEventArgs e)
        {
            addressinputvar = addressin.Text;
        }


        private void Number_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (numberofpings.Text == "0")
            {
                numberofpings.Text = "1";
            }
            if (numberofpings.Text == "")
            {
                numberofpings.Text = "1";
            }
            pingtimes = Convert.ToInt32(numberofpings.Text);
            if (pingtimes > 50000)
            {
                pingtimes = 50000;
                numberofpings.Text = "50000";
            }
        }


        private void UpdateOutputFailed()
        {
            Outputfield.Text = "Could not resolve hostname\n";
        }


        private void UpdateAddressSubstring()
        {
            addressin.Text = addressin.Text.Substring(0, 5);
        }


        private void ResolvingHostUpdate()
        {
            Outputfield.Text = "Resolving hostname...\n";
        }



        private void UpdateLogBox(string inp)
        {
            window2.LogBox2.AppendText(inp);
        }


        private void UpdateBox0()
        {
            StatisticsBox.Text = "";
        }


        private void UpdateUserInterfacePingComplete()
        {
            Outputfield.AppendText("Ping complete\n");
            Outputfield.ScrollToEnd();
        }


        private void UpdateUserInterfacePingCancelled()
        {
            Outputfield.AppendText("cancelling...\n");
        }




        private void UpdateProgressbar(int prog)
        {
            progressbar.Value = prog;
        }


        private void clearoutbox()
        {
            Outputfield.Text = "";
        }



        private void TextBox_TextChanged_1(object sender, TextChangedEventArgs e)
        {

        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            switch (Convert.ToString(DeviceSelector.SelectedItem))
            {
                case "System.Windows.Controls.ComboBoxItem: Router":
                    devicetype = "dg";
                    break;
                case "System.Windows.Controls.ComboBoxItem: Switch US":
                    devicetype = "ussw010";
                    break;
                case "System.Windows.Controls.ComboBoxItem: Workstation":
                    devicetype = "mws";
                    break;
                case "System.Windows.Controls.ComboBoxItem: Switch CA":
                    devicetype = "casw010";
                    break;
            };
        }





        private void progressbar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {

        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {

            //rateping = Convert.ToInt16((Math.Pow((1 / (Math.Sqrt(1 - ((rateslider.Value + 0.001) / 10) * 2.5))), 10)));
            rateping = Convert.ToInt16(1 + (Math.Pow(rateslider.Value, 3)));
            if (rateping > 1000)
            {
                rateping = 1000;
            }
            if (rateping > 20)
            {
                if (meswarn == false)
                {
                    MessageBox.Show("Are you sure you want to ping at " + rateping + " pings/second?\nNetwork stability at the receiving site may be compromised.");
                    meswarn = true;
                }
                }
            resttime = Convert.ToInt16((1000 / rateping));
            pingratelabel.Content = "Ping Rate: " + rateping + "/second";
        }



        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            Application.Current.Shutdown();
        }


        private void EnablePingButton()
        {
            PingButton.IsEnabled = true;
        }



        private void EnableCancelButton()
        {
            CancelButton.IsEnabled = true;
        }

        private void DisablePingButton()
        {
            PingButton.IsEnabled = false;
        }


        private void DisableCancelButton()
        {
            CancelButton.IsEnabled = false;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            cancelrequested = true;
            CancelButton.IsEnabled = false;
            Outputfield.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadDelegate(UpdateUserInterfacePingCancelled));
            progressbar.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadDelegate(new Action(() => UpdateProgressbar(0))));
        }


        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            window2.Show();
        }


        private void TextBox_TextChanged_2(object sender, TextChangedEventArgs e)
        {
            if (Convert.ToInt32(PingSize.Text) > 65000)
            {
                PingSize.Text = "65000";
            }
            if (Convert.ToInt32(PingSize.Text) < 1)
            {
                PingSize.Text = "1";
            }
            icmpdata = new byte[Convert.ToInt32(PingSize.Text)];
            for (int i = 0; i < icmpdata.Length; i++)
            {
                icmpdata[i] = 0x61;
            }
        }















    }
}
