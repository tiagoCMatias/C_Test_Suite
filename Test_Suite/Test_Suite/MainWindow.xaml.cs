using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO.Ports;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Test_Suite
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        private class STM
        {
            public const int INIT = 0;
            public const int LEDS = 1;
            public const int RELAY = 2;
            public const int RS232 = 3;
            public const int DEVICE_CURRENT = 4;
            public const int ERROR = 99;
            public const int UNDER_TEST = 50;
            public const int FINISH = 5;

        }

        private DispatcherTimer timer;
        private Stopwatch stopWatch;
        private int STM_Board_Test;
        private double time_elapsed;
        private bool check_connection = false;
        private string observations;
        private int progress_bar_increment = 10;

        public class Test_list_item
        {
            public string Name { get; set; }
            public string Path { get; set; }
        }
        ObservableCollection<Test_list_item> items = new ObservableCollection<Test_list_item>();

        //MainWindow Listbox reset
        private void ResetListBox()
        {
            Dispatcher.Invoke(new Action(() =>
            {
                test_list.Items.Clear();
                test_list.Items.Insert(0, new Test_list_item() { Name = "USB PORT B", Path = null, });
                test_list.Items.Insert(1, new Test_list_item() { Name = "LEDS", Path = null, });
                test_list.Items.Insert(2, new Test_list_item() { Name = "RELAY", Path = null, });
                test_list.Items.Insert(3, new Test_list_item() { Name = "Serial Port", Path = null, });
                test_list.Items.Insert(4, new Test_list_item() { Name = "USB PORT A", Path = null, });
                test_list.Items.Insert(5, new Test_list_item() { Name = "DEVICE CURRENT", Path = null, });
            }), DispatcherPriority.Normal);
        }
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
            Dispatcher.Invoke(new Action(() =>
            {
                time_elapsed = TimeSpan.Parse(stopWatch.Elapsed.ToString()).TotalSeconds;
                test_time_lbl.Text = TimeSpan.FromSeconds(stopWatch.Elapsed.TotalSeconds).ToString(@"hh\:mm\:ss");
            }), DispatcherPriority.Normal);

            if(check_connection == false && time_elapsed > 15)
            {
                time_elapsed = 0;
                check_connection = true;
                observations = "Cant communicate";
                UpdateTextEvolution("ERROR");
                STM_Board_Test = STM.ERROR;
                Process[] runingProcess = Process.GetProcesses();
                for (int i = 0; i < runingProcess.Length; i++)
                {
                    // compare equivalent process by their name
                    if (runingProcess[i].ProcessName == "mdb_test")
                    {
                        // kill  running process
                        runingProcess[i].Kill();
                    }

                }
                UpdateList(0, "USB PORT B", false);

                Debug.WriteLine("Passed 15secs and no communication");
            }

        }

       
        //begin script
        public string[] Start_test_with_arguments(string arguments)
        {

            List<String> lines = new List<String>();
            
            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "D:\\Trabalho\\CCode\\MDB_USB_MasterSlave\\C_Test_Suite\\Test_Suite\\Test_Suite\\Files\\mdb_test.exe",
                    //FileName = Path.GetFullPath(@"..\\Files\\mdb_test.exe"),
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            string line;
            proc.Start();
            while (!proc.StandardOutput.EndOfStream)
            {
                line = proc.StandardOutput.ReadLine();
                if (line.Contains("Error while"))
                {
                    proc.Kill();
                    //Process.Start("cmd", "taskkill /im mdb_test.exe /f");
                    UpdateTextEvolution("Si fudeu");
                    STM_Board_Test = STM.ERROR;
                    break;
                }
                else
                {
                    UpdateList(0, "USB PORT B", true);
                    lines.Add(line);
                }
            }

            String[] stringArray = lines.ToArray();

            return stringArray;
        }


        public void Test_RS232()
        {
            check_connection = false;
            bool passed = true;
            string[] lines = Start_test_with_arguments("--com USB0");

            for(int i = 0; i < lines.Length; i++)
            {
                if(lines[i].Contains("FAILED"))
                {
                    if (lines[i - 1].Contains("SERIAL"))
                        observations = "Serial failed";
                    else if(lines[i - 1].Contains("[M <]checking MDB S -> M"))
                        observations = "MDB Slave to Master Failed";
                    else if (lines[i - 1].Contains("[M <]checking MDB S -> M"))
                        observations = "MDB Master to Slave Failed";

                    STM_Board_Test = STM.ERROR;
                    passed = false;
                }
            }

            if(passed)
            {
                check_connection = true;
                time_elapsed = 0;
                UpdateList(3, "RS232", true);
                UpdateProgressBar(progress_bar_increment);
                UpdateTextEvolution("RS232 Test Passed");
                STM_Board_Test++;
            }
            else
            {
                check_connection = true;
                time_elapsed = 0;
                UpdateList(3, "RS232", false);
                observations = "RS232 Test Failed";
                UpdateProgressBar(200);
                UpdateTextEvolution("RS232 Test Failed");
            }
        }

        public void Test_Relay()
        {
            check_connection = false;
            string[] lines = Start_test_with_arguments("--relay");

            if (lines.Length >= 9 &&
                lines[0].Contains("[M >]M,TEST_REL") &&
               lines[1].Contains("[M <]m,ACK") &&
               lines[2].Contains("[M <]checking RELAY...") &&
               lines[3].Contains("[M <]ON") &&
               lines[4].Contains("[M <]OFF") &&
               lines[5].Contains("[M <]ON") &&
               lines[6].Contains("[M <]OFF") &&
               lines[7].Contains("[M <]-------------------------") &&
               lines[8].Contains("- closing mdb connection"))
            {
                check_connection = true;
                time_elapsed = 0;
                UpdateList(2, "RELAY", true);
                UpdateProgressBar(progress_bar_increment);
                UpdateTextEvolution("Relay Test Passed");
                STM_Board_Test = STM.RS232;
            }
            else
            {
                check_connection = true;
                time_elapsed = 0;
                UpdateList(2, "RELAY", false);
                UpdateProgressBar(200);
                observations = "Relay Fail";
                Debug.WriteLine("Relay Fail");
                STM_Board_Test = STM.ERROR;
            }
        }

        public void Test_leds()
        {
            check_connection = false;

            MessageBoxResult response = MessageBoxResult.No;
            Dispatcher.Invoke(new Action(() =>
            {
                response = MessageBox.Show("Starting Leds Test\n Are you ready?", "Question", MessageBoxButton.YesNo, MessageBoxImage.Question);
            }), DispatcherPriority.Background);
            if (response == MessageBoxResult.Yes)
            {
                UpdateTextEvolution("Test Leds Started");
                string[] lines = Start_test_with_arguments("--leds");

                if (lines.Length >= 10 &&
                    lines[0].Contains("[M >]M,TEST_LED") &&
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
                    check_connection = true;
                    time_elapsed = 0;

                    Dispatcher.Invoke(new Action(() =>
                    {
                        response = MessageBox.Show("Leds ok?", "Question", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    }), DispatcherPriority.Background);
                    if (response == MessageBoxResult.Yes)
                    {
                        UpdateList(1, "LEDS", true);
                        UpdateProgressBar(progress_bar_increment);
                        UpdateTextEvolution("Leds Test Passed");
                        STM_Board_Test = STM.RELAY;
                    }
                    else
                    {
                        UpdateList(1, "LEDS", false);
                        UpdateProgressBar(200);
                        observations = "Leds Fail";
                        UpdateTextEvolution("Leds Test Failed by user");
                        STM_Board_Test = STM.ERROR;
                    }
                }
                else
                {
                    check_connection = true;
                    time_elapsed = 0;
                    UpdateList(1, "LEDS", false);
                    UpdateProgressBar(200);
                    observations = "Leds Fail";

                    Debug.WriteLine("Leds Fail");
                    STM_Board_Test = STM.ERROR;
                }
            }
            else
            {
                UpdateList(1, "LEDS", false);
                UpdateProgressBar(200);
                UpdateTextEvolution("Leds Test Canceled");
                STM_Board_Test = STM.ERROR;
            }
        }

        private void Test_Device_Current()
        {
            Debug.WriteLine("Nothing to do");
        }

        private void Init_process()
        {
            UpdateTextEvolution("----------------------------");
            UpdateTextEvolution("New Test Started");
            Dispatcher.Invoke(new Action(() =>
            {
                StartTimer();
                timer.Start();
                time_elapsed = 0;
                test_time_lbl.Visibility = Visibility.Visible;
                STM_Board_Test = STM.LEDS;
            }), DispatcherPriority.Normal);
        }

        public void Start()
        {
            STM_Board_Test = STM.INIT;
            ResetProgressBar();
            ResetListBox();
            
            while (STM_Board_Test != STM.FINISH)
            {
                //Debug.WriteLine("State:" + STM_Board_Test);
                switch (STM_Board_Test)
                {
                    case STM.INIT:
                        Init_process();
                        break;
                    case STM.LEDS:
                        Test_leds();
                        break;
                    case STM.RELAY:
                        Test_Relay();
                        break;
                    case STM.RS232:
                        Test_RS232();
                        break;
                    case STM.DEVICE_CURRENT:
                        Test_Device_Current();
                        break;
                    case STM.ERROR:
                        Dispatcher.Invoke(new Action(() =>
                        {
                            start_test_button.IsEnabled = true;
                        }), DispatcherPriority.Normal);
                        STM_Board_Test = STM.FINISH;
                        Debug.WriteLine("Error");
                        break;
                    case STM.FINISH:
                        start_test_button.IsEnabled = true;
                        Debug.WriteLine("Finish");
                        break;
                    case STM.UNDER_TEST:
                        break;
                    default:
                        break;
                }
            }
            
        }

        public bool IsDigitsOnly(string str)
        {
            int count = 0;
            foreach (char c in str)
            {
                count++;
                if (c < '0' || c > '9')
                    return false;
            }
            if (count == 10)
                return true;
            else
                return false;
        }

        public MainWindow()
        {
            STM_Board_Test = 0;

            InitializeComponent();

        }

        /*** Interface Interactions ****/
        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            /*configurationWindow configurationWindow = new configurationWindow();
            configurationWindow.ShowDialog();
            Fetch_DB_Credentials();

            test_evolution_txtbox.Text = "Configuration File" + Environment.NewLine +
                "Server: " + server + Environment.NewLine +
                "Database: " + database + Environment.NewLine +
                "Uid:" + uid + Environment.NewLine +
                "Password " + password;*/
        }

        private void Help_button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                String fileName = @"Files\\linulus-test-system.pdf";
                Process process = new Process();
                process.StartInfo.FileName = fileName;
                process.Start();
                process.WaitForExit();
            }
            catch
            {
                MessageBox.Show("Can't open PDF document" + Environment.NewLine + "Please make sure you have a PDF Viewer", "PDF Viewer", MessageBoxButton.OK, MessageBoxImage.Information);
            }

        }

        /*** Progress Bar  ***/
        private void UpdateProgressBar(int progress_bar_inc)
        {
            if(progress_bar_inc > 100)
            {
                Dispatcher.Invoke(new Action(() =>
                {
                    progress_bar.BeginAnimation(ProgressBar.ValueProperty, null);
                    progress_bar.Background = Brushes.Red;
                }), DispatcherPriority.Background);
            }
            else
            {
                Duration duration = new Duration(TimeSpan.FromSeconds(1));
                Dispatcher.Invoke(new Action(() =>
                {
                    DoubleAnimation doubleanimation = new DoubleAnimation(progress_bar.Value + progress_bar_inc, duration);
                    progress_bar.BeginAnimation(ProgressBar.ValueProperty, doubleanimation);
                }), DispatcherPriority.Background);
            }
            
        }
        private void ResetProgressBar()
        {
            Dispatcher.Invoke(new Action(() =>
            {
                progress_bar.Background = Brushes.Lavender;
                progress_bar.BeginAnimation(ProgressBar.ValueProperty, null);
            }), DispatcherPriority.Background);
            
        }

        /*** Update Text Evolution***/
        private void UpdateTextEvolution(string text)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                test_evolution_txtbox.Text += text + Environment.NewLine;
            }), DispatcherPriority.Background);
        }


        /** Closing Definitions **/
        private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Thread.Sleep(100);
        }

        private void UpdateList(int index_list, string text_to_update, bool test_status)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                test_list.Items.RemoveAt(index_list);
                test_list.Items.Insert(index_list, new Test_list_item() { Name = text_to_update, Path = (test_status) ? @"../images/pass.png" : @"../images/fail.png" });
            }), DispatcherPriority.Normal);
        }

        private void Start_test_button_Click(object sender, RoutedEventArgs e)
        {
            //Get Serial Number
            start_test_button.IsEnabled = false;
            Serial_Input_Window serialWindow = new Serial_Input_Window();

            serialWindow.ShowDialog();
            String SerialNumber = serialWindow.serialNumber;

            if (SerialNumber.Contains("Cancel"))
            {
                start_test_button.IsEnabled = true;
                return;
            }
            if (IsDigitsOnly(SerialNumber) == false)
            {
                start_test_button.IsEnabled = true;
                return;
            }

            ResetProgressBar();
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

        private void SerialCom_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            
            string[] ports = SerialPort.GetPortNames();
            SerialCom.Items.Clear();
            //Console.WriteLine("The following serial ports were found:");

            // Display each port name to the console.
            foreach (string port in ports)
            {
                //Console.WriteLine(port);
                SerialCom.Items.Add(port);
            }
            //SerialCom.SelectedIndex = 0;
        }
    }
}
