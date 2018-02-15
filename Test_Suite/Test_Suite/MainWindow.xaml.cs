using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace Test_Suite
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        static class STM
        {
            public const int INIT = 0;
            public const int LEDS = 1;
            public const int RELAY = 2;
            public const int RS232 = 3;
            public const int USB_TYPE_A = 4;
            public const int USB_TYPE_B = 5;
            public const int DEVICE_CURRENT = 6;
            public const int ERROR = 99;
            public const int FINISH = 7;
        }

        private DispatcherTimer timer;
        private Stopwatch stopWatch;
        private int STM_Board_Test;
        //Application Timer
        public void StartTimer()
        {
            timer = new DispatcherTimer();
            timer.Tick += DispatcherTimerTick_;
            timer.Interval = new TimeSpan(0, 0, 0, 0, 1);
            stopWatch = new Stopwatch();
            stopWatch.Start();
            timer.Start();
        }

        //Timer Handler
        private void DispatcherTimerTick_(object sender, EventArgs e)
        {
            /*Dispatcher.Invoke(new Action(() =>
            {
                test_time_lbl.Text = TimeSpan.FromSeconds(stopWatch.Elapsed.TotalSeconds).ToString(@"hh\:mm\:ss");
            }), DispatcherPriority.Normal);*/
        }

        public string[] Start_test_with_arguments(string arguments)
        {
            List<String> lines = new List<String>();
            
            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "D:\\Trabalho\\CCode\\MDB_USB_MasterSlave\\MDB_Test_Suite\\MDB_Test_Suite\\Files\\mdb_test.exe",
                    //FileName = "../Files/mdb_test.exe",
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };

            proc.Start();
            while (!proc.StandardOutput.EndOfStream)
            {
                lines.Add(proc.StandardOutput.ReadLine());
            }

            String[] stringArray = lines.ToArray();

            return stringArray;
        }


        public void Test_RS232()
        {
            string[] lines = Start_test_with_arguments("--com USB0");

            if (lines[0].Contains("[M >]M,TEST_LED") &&
               lines[1].Contains("[M <]m,ACK") &&
               lines[2].Contains("[M <]checking LEDs...") &&
               lines[3].Contains("[M <]1") &&
               lines[4].Contains("[M <]2") &&
               lines[5].Contains("[M <]3") &&
               lines[6].Contains("[M <]4") &&
               lines[7].Contains("[M <]all") &&
               lines[8].Contains("[M <]-------------------------") &&
               lines[9].Contains("- closing mdb connection"))
            {
                Debug.WriteLine("RS232 Sucess");
                STM_Board_Test++;
            }
            else
            {
                Debug.WriteLine("RS232 Fail");
                STM_Board_Test = STM.ERROR;
            }
        }

        public void Test_Relay()
        {
            string[] lines = Start_test_with_arguments("--relay");

            if (lines[0].Contains("[M >]M,TEST_LED") &&
               lines[1].Contains("[M <]m,ACK") &&
               lines[2].Contains("[M <]checking LEDs...") &&
               lines[3].Contains("[M <]1") &&
               lines[4].Contains("[M <]2") &&
               lines[5].Contains("[M <]3") &&
               lines[6].Contains("[M <]4") &&
               lines[7].Contains("[M <]all") &&
               lines[8].Contains("[M <]-------------------------") &&
               lines[9].Contains("- closing mdb connection"))
            {
                Debug.WriteLine("Relay Sucess");
                STM_Board_Test++;
            }
            else
            {
                Debug.WriteLine("Relay Fail");
                STM_Board_Test = STM.ERROR;
            }
        }

        public void Test_leds()
        {
            string[] lines = Start_test_with_arguments("--leds");

            if (lines[0].Contains("[M >]M,TEST_LED") &&
               lines[1].Contains("[M <]m,ACK") &&
               lines[2].Contains("[M <]checking LEDs...") &&
               lines[3].Contains("[M <]1") &&
               lines[4].Contains("[M <]2") &&
               lines[5].Contains("[M <]3") &&
               lines[6].Contains("[M <]4") &&
               lines[7].Contains("[M <]all") &&
               lines[8].Contains("[M <]-------------------------") &&
               lines[9].Contains("- closing mdb connection"))
            {
                Debug.WriteLine("Sucess");
                STM_Board_Test++;
            }
            else
            {
                Debug.WriteLine("Leds Fail");
                STM_Board_Test = STM.ERROR;
            }

        }

        public void Start()
        {
            while (STM_Board_Test != STM.ERROR || STM_Board_Test != STM.FINISH)
            {
                switch (STM_Board_Test)
                {
                    case STM.INIT:
                        STM_Board_Test++;
                        break;
                    case STM.LEDS:
                        Test_leds();
                        break;
                    case STM.RELAY:
                        break;
                    case STM.RS232:
                        break;
                    case STM.USB_TYPE_A:
                        break;
                    case STM.USB_TYPE_B:
                        break;
                    case STM.DEVICE_CURRENT:
                        break;
                    case STM.ERROR:
                        break;
                    case STM.FINISH:
                        break;

                    default:
                        break;
                }
            }
            
        }

        public MainWindow()
        {
            STM_Board_Test = 0;

            InitializeComponent();

            try
            {
                Thread cThread = new Thread(Start);
                cThread.Start();

            }
            catch
            {
                MessageBox.Show("Can't communicate with board" + Environment.NewLine + "Please check cable connection", "No communication with board", MessageBoxButton.OK, MessageBoxImage.Error);
                timer.Stop();
            }
        }
    }
}
