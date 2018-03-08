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
    

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Window2 window2 = new Window2();

        public class PINGREPL
        {
            public PINGREPL(IPStatus Status, int Ttl, long Rtt, int Bitesize, int Seq)
            {
                Status = IPStatus.Unknown;
                Ttl = 0;
                Rtt = 0;
                Bitesize = 0;
                Seq = 0;

            }
            public IPStatus Status { get; set; }
            public int Ttl { get; set; }
            public long Rtt { get; set; }
            public int Bitesize { get; set; }
            public int Seq { get; set; }

        }

        public MainWindow()
        {
            InitializeComponent();
        }

        private delegate void ThreadDelegate();

        // ============================================================================================== Main Button Trigger
        // ============================================================================================== 
        // ============================================================================================== 
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            window2.LogBox2.AppendText("\n======="+ Convert.ToString(DateTime.Now)+ "=======\n");
            cancelled = false;
            storenumber = "";
            progressbar.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadDelegate(new Action(() => UpdateProgressbar(0))));
            PingButton.IsEnabled = false;
            CancelButton.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadDelegate(EnableCancelButton));
            pingcomplete = false;
            box4 = box3;
            box3 = box2;
            box2 = box1;
            currentoperationindex++;
            Outputfield.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadDelegate(clearoutbox));
            StatisticsBox.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadDelegate(UpdateBox0));
            //StatisticsBox_Copy.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadDelegate(UpdateBox1));
            //StatisticsBox_Copy1.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadDelegate(UpdateBox2));
            //StatisticsBox_Copy2.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadDelegate(UpdateBox3));
            ThreadDelegate parser = new ThreadDelegate(ParseInput);
            parser.BeginInvoke(null, null);
        }
        // ============================================================================================== /Main Button Trigger
        // ==============================================================================================
        // ==============================================================================================
        // ======================================= Variable Declarations
        IPAddress addr = IPAddress.Parse("127.0.0.1");
        IPHostEntry hostEntry;
        string devicetype = "dg";
        bool pingcomplete;
        bool singlecomplete = false;
        public bool cancelled = false;
        string rateslidervalue = "1";
        long roundtriptime;
        int ttl;
        double rateping = 1;
        int pingtimes;
        int lastseq = 0;
        int currentseq = 0;
        string addressinputvar;
        string box1 = "";
        string box2 = "";
        string box3 = "";
        string box4 = "";
        byte[] icmpdata = new byte[1];
        int currentoperationindex = 0;
        
        //public string data = new String('a', 5);
        //System.Byte[] icmpdata = Encoding.ASCII.GetBytes(data);

        PINGREPL replyvalues = new PINGREPL(IPStatus.Unknown, 0, 0, 0, 0);

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
            pingtimes = Convert.ToInt16(numberofpings.Text);
            if (pingtimes > 500)
            {
                pingtimes = 500;
                numberofpings.Text = "500";
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


        int ri1rttmax;
        int ri1rttmin;
        double ri1rttaverage;
        int receivedtotal;
        int senttotal;
        int losttotal;
        string storenumber;
        int pingindex = 0;

        private void ParseInput()
        {
            lastseq = 0;
            Outputfield.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadDelegate(ResolvingHostUpdate));
            ThreadDelegate progressbarthread = new ThreadDelegate(Animateprogressbar);
            progressbarthread.BeginInvoke(null, null);
            bool isValidIp = IPAddress.TryParse(addressinputvar, out addr);
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
                    Outputfield.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadDelegate(UpdateOutputFailed));
                    cancelled = true;
                    CancelButton.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadDelegate(DisableCancelButton));
                    PingButton.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadDelegate(EnablePingButton));
                    Outputfield.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadDelegate(UpdateUserInterfacePingComplete));
                    progressbar.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadDelegate(new Action(() => UpdateProgressbar(0))));
                    return;

                }
                addr = hostEntry.AddressList[0];
            }
            else
            {
                addr = IPAddress.Parse(addressinputvar);
            }
            progressbar.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadDelegate(new Action(() => UpdateProgressbar(0))));
            Outputfield.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadDelegate(clearoutbox));
            pingindex++;
            ThreadDelegate pinger = new ThreadDelegate(Repeaticmp);
            pinger.BeginInvoke(null, null);
        }


        private void clearoutbox()
        {
            Outputfield.Text = "";
        }

        private void Repeaticmp()
        {
            int thisinstance = pingindex;
            List<int> rtts = new List<int>();
            receivedtotal = 0;
            receivedtotal = 0;
            senttotal = 0;
            losttotal = 0;
            
            for (int i = 1; i <= pingtimes; i++)
            {
                if (thisinstance != pingindex)
                {
                    return;
                }
                if (cancelled == true)
                {
                    break;
                }
                singlecomplete = false;
                progressbar.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadDelegate(new Action(() => UpdateProgressbar(10))));
                ThreadDelegate progressbarthread = new ThreadDelegate(Animateprogressbar);
                progressbarthread.BeginInvoke(null, null);
                senttotal++;
                replyvalues = Sendicmp(addr, i);
                singlecomplete = true;
                if (replyvalues.Bitesize != 0)
                {
                    rtts.Add(Convert.ToInt16(replyvalues.Rtt));
                }
                try
                {
                    ri1rttmax = rtts.Max();
                    ri1rttmin = rtts.Min();
                    ri1rttaverage = Convert.ToInt16(rtts.Average());
                }
                catch (Exception)
                {
                }
                if (cancelled != true)
                {
                    Outputfield.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadDelegate(UpdateUserInterface));
                    progressbar.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadDelegate(new Action(() => UpdateProgressbar(0))));
                }

                if (cancelled != true) {
                    if (i < pingtimes)

                {
                    Thread.Sleep(1000 / (Convert.ToInt16(rateping)));
                }
                }

            }
            progressbar.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadDelegate(new Action(() => UpdateProgressbar(0))));
            pingcomplete = true;
            window2.LogBox2.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadDelegate(new Action(() => UpdateLogBox("\n" + box1 + "\n"))));
            Outputfield.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadDelegate(UpdateUserInterfacePingComplete));
            window2.LogBox2.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadDelegate(new Action(() => UpdateLogBox("Ping complete\n"))));
            CancelButton.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadDelegate(DisableCancelButton));
            PingButton.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadDelegate(EnablePingButton));

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

        string outs = "";
        private void UpdateUserInterface()
        {
            
            currentseq = senttotal;
            Outputfield.ScrollToEnd();
            if (lastseq != currentseq)
                {
                
                if (replyvalues.Status == IPStatus.Success)
                {
                    if (replyvalues.Bitesize != 0)
                        {
                            
                            progressbar.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadDelegate(new Action(() => UpdateProgressbar(0))));
                            Outputfield.AppendText(currentseq + " Reply from " + addr + ": bytes=" + replyvalues.Bitesize + " time=" +
                            replyvalues.Rtt + "ms TTL=" + replyvalues.Ttl + "\n");
                            window2.LogBox2.AppendText(currentseq + " Reply from " + addr + ": bytes=" + replyvalues.Bitesize + " time=" +
                            replyvalues.Rtt + "ms TTL=" + replyvalues.Ttl + "\n");
                            if (storenumber == "")
                            {
                                box1 = StatisticsBox.Text = "Ping statistics for " + addr + ":\n    Packets: Sent = " + senttotal + ", Received = " + receivedtotal +
                                                         ", Lost = " + losttotal + "(" + (Convert.ToInt16(((Convert.ToDouble(losttotal) / (Convert.ToDouble(senttotal)) * 100.0)))) + "% loss),\n"
                                                         + "Approximate rount trip times in milli-seconds:\n"
                                                         + "    Minimum = " + ri1rttmin + "ms, Maximum = " + ri1rttmax + "ms, Average = " + ri1rttaverage + "ms\n";
                                window2.LogBox2.ScrollToEnd();
                            }
                            if (storenumber != "")
                            {
                                box1 = StatisticsBox.Text = "Ping statistics for " + addr + "(" + storenumber + "):\n    Packets: Sent = " + senttotal + ", Received = " + receivedtotal +
                                                         ", Lost = " + losttotal + "(" + (Convert.ToInt16(((Convert.ToDouble(losttotal) / (Convert.ToDouble(senttotal)) * 100.0)))) + "% loss),\n"
                                                         + "Approximate rount trip times in milli-seconds:\n"
                                                         + "    Minimum = " + ri1rttmin + "ms, Maximum = " + ri1rttmax + "ms, Average = " + ri1rttaverage + "ms\n";
                                window2.LogBox2.ScrollToEnd();
                            }
                        }
                    }

                    else
                    {
                        if (cancelled == false)
                        {
                            progressbar.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadDelegate(new Action(() => UpdateProgressbar(0))));
                            switch (replyvalues.Status)
                                
                            {
                                case IPStatus.TimedOut:
                                    outs = (currentseq + " Request timed out.\n");
                                    break;
                                case IPStatus.BadDestination:
                                    outs = (currentseq + " Bad destination\n");
                                    break;
                                case IPStatus.DestinationHostUnreachable:
                                    outs = (currentseq + " Destination host unreachable.\n");
                                    break;
                                case IPStatus.DestinationNetworkUnreachable:
                                    outs = (currentseq + " Destination network unreachable.\n");
                                    break;
                                case IPStatus.DestinationUnreachable:
                                    outs = (currentseq + " Destination unreachable.\n");
                                    break;
                                case IPStatus.HardwareError:
                                    outs = (currentseq + " Hardware failure.\n");
                                    break;
                                case IPStatus.NoResources:
                                    outs = (currentseq + " Insufficient resources to complete request.\n");
                                    break;
                                case IPStatus.PacketTooBig:
                                    outs = (currentseq + " Packet sent was too big.\n");
                                    break;
                                case IPStatus.ParameterProblem:
                                    outs = (currentseq + " Parameters are invalid.\n");
                                    break;
                                case IPStatus.SourceQuench:
                                    outs = (currentseq + " Reported source quench.\n");
                                    break;
                                case IPStatus.TimeExceeded:
                                    outs = (currentseq + " TTL expired in transit.\n");
                                    break;
                                case IPStatus.TtlExpired:
                                    outs = (currentseq + " TTL expired in transit.\n");
                                    break;
                                case IPStatus.TtlReassemblyTimeExceeded:
                                    outs = (currentseq + " TTL expired in transit.\n");
                                    break;
                                case IPStatus.Unknown:
                                    outs = (currentseq + " Unknown Failure.\n");
                                    break;
                                case IPStatus.UnrecognizedNextHeader:
                                    outs = (currentseq + " Unrecognized next header.\n");
                                    break;
                                    ;
                            };
                            losttotal++;
                            currentseq = lastseq;
                            Outputfield.AppendText(outs);
                            window2.LogBox2.AppendText(outs);
                            window2.LogBox2.ScrollToEnd();
                        }
                    }
                }
            currentseq = lastseq;



        }





        private PINGREPL Sendicmp(IPAddress addresstoping, int sequenceno)
        {

            replyvalues.Rtt = 0;
            replyvalues.Bitesize = 0;
            Ping pingSender = new Ping();
            PingReply reply = null;
            try
            {
                reply = pingSender.Send(addresstoping, 5000, icmpdata);
            }
            catch (PingException)
            {
                replyvalues.Status = IPStatus.Unknown;
                return replyvalues;
            }
            if (reply == null)
            {
                replyvalues.Status = IPStatus.Unknown;
                return replyvalues;
            }
            if (reply.Status == IPStatus.Success)
            {
                if (reply.Buffer.Length != 0)
                {
                    replyvalues.Status = reply.Status;
                    progressbar.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadDelegate(new Action(() => UpdateProgressbar(100))));
                    replyvalues.Rtt = (reply.RoundtripTime);
                    try
                    {
                        replyvalues.Ttl = (reply.Options.Ttl);
                    }
                    catch (NullReferenceException)
                    {
                        replyvalues.Ttl = 0;
                    }
                    replyvalues.Bitesize = (reply.Buffer.Length);
                    replyvalues.Seq = (sequenceno);
                    receivedtotal++;
                    return replyvalues;
                }
                else
                {
                    replyvalues.Status = IPStatus.Unknown;
                    progressbar.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadDelegate(new Action(() => UpdateProgressbar(100))));
                    replyvalues.Rtt = 0;
                    replyvalues.Ttl = 0;
                    replyvalues.Bitesize = 0;
                    replyvalues.Seq = (sequenceno);
//lost total is incremented in the switch statement.
                    return replyvalues;
                }
            }
            else
            {
                progressbar.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadDelegate(new Action(() => UpdateProgressbar(100))));
                replyvalues.Status = reply.Status;
                replyvalues.Rtt = 0;
                replyvalues.Ttl = 0;
                replyvalues.Bitesize = 0;
                replyvalues.Seq = (sequenceno);
                return replyvalues;
            }
        }
            
        

        private void UpdateProgressbar(int prog)
        {
            progressbar.Value = prog;
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            double x = rateslider.Value/10;
            rateping = Convert.ToInt16((Math.Pow((2 / (Math.Sqrt(4 - x*2.5))), 10)));
            if (rateping > 100)
            {
                rateping = 100;
            }
            pingratelabel.Content = "Ping Rate: " +  rateping + "/second";
            //((Convert.ToInt16(rateslider.Value * 9)+1))

        }


        private void TextBox_TextChanged_1(object sender, TextChangedEventArgs e)
        {

        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string selecteditem = Convert.ToString(DeviceSelector.SelectedItem);
            switch (selecteditem)
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

        private void Animateprogressbar()
        {
            int val = 10;
            while (val < 100)
            {
                if (singlecomplete == false)
                {
                    if (cancelled == false)
                    {
                        progressbar.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadDelegate(new Action(() => UpdateProgressbar(val))));
                        Thread.Sleep(112);
                        val=val+2;
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
            }
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

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
            cancelled = true;
            CancelButton.IsEnabled = false;
            Outputfield.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadDelegate(UpdateUserInterfacePingCancelled));
            progressbar.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadDelegate(new Action(() => UpdateProgressbar(0))));
            PingButton.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadDelegate(EnablePingButton));
            
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

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            window2.Show();
        }
    }
}
