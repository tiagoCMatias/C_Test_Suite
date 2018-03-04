using MahApps.Metro.Controls;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Text.RegularExpressions;
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


        MDB_BOARD MDB_Board;
        string TABLE_NAME = "mdb_usb_ms";
        string server;
        string database;
        string uid;
        string password;
        private DispatcherTimer timer;
        private Stopwatch stopWatch;
        private double time_elapsed;
        private string observations;
        private int progress_bar_increment = 10;

        public string UpdateMyBox
        {
            get { return test_evolution_txtbox.Text; }
            set { test_evolution_txtbox.Text = value; }
        }

        public class Test_list_item
        {
            public string Name { get; set; }
            public string Path { get; set; }
        }
        ObservableCollection<Test_list_item> items = new ObservableCollection<Test_list_item>();


        public MainWindow()
        {
            InitializeComponent();
            ListSerialPort();
            MDB_Board = new MDB_BOARD(new Init_State());
            Fetch_DB_Credentials();
        }
        
        private void Start_test_button_Click(object sender, RoutedEventArgs e)
        {
            if(!CheckIfFormComplete())
            {
                MessageBox.Show("Please select all itens correctly", "Wrong Fields", MessageBoxButton.OKCancel, MessageBoxImage.Exclamation);
                SetStartButtonState(true);
                return;
            }

            MDB_Board.BoardOperator = operator_txtbox.Text;
            MDB_Board.SerialNumber = GetSerialNumber();


            if (String.IsNullOrEmpty(MDB_Board.SerialNumber) || MDB_Board.CheckRepeatedTest())
            {
                MessageBox.Show("Error In Serial Number", "Repeated Serial Number", MessageBoxButton.OKCancel, MessageBoxImage.Exclamation);
                SetStartButtonState(true);
                return;
            }

            ResetProgressBar();
            MDB_Board.RS232Port = SerialCom.SelectedItem.ToString();
            MDB_Board.NucleoSerialCommunication(CurrentPort.SelectedItem.ToString());
            if (!MDB_Board.CorrectPortConfig())
            {
                
                //Debug.WriteLine("Porta: "+ MDB_Board.SerialPort);
                MDB_Board.CloseSerialPort();
                MessageBox.Show("Wrong Port Selection", "Wrong Fields", MessageBoxButton.OKCancel, MessageBoxImage.Exclamation);
                SetStartButtonState(true);
                return;
            }
            MDB_Board.SetUSBSupply();
            try
            {
                ResetListBox();
                UpdateTextEvolution(Environment.NewLine + "---------------------------");
                StartTimer();
                Thread Test_Thread = new Thread(() => MDB_Board.StartTesting());
                Test_Thread.Start();
            }
            catch
            {
                SetStartButtonState(true);
                MessageBox.Show("Can't communicate with board" + Environment.NewLine + "Please check cable connection", "No communication with board", MessageBoxButton.OK, MessageBoxImage.Error);
                timer.Stop();
            }
            
        }

        private string GetSerialNumber()
        {
            //Get Serial Number
            SetStartButtonState(false);
            Serial_Input_Window serialWindow = new Serial_Input_Window();

            serialWindow.ShowDialog();
            String SerialNumber = serialWindow.serialNumber;

            if (SerialNumber.Contains("Cancel"))
            {
                SetStartButtonState(true);
                return null;
            }
            if (IsDigitsOnly(SerialNumber) == false)
            {
                SetStartButtonState(true);
                return null;
            }
            //MDB_BOARD.SerialNumber = this.SerialNumber;
            return SerialNumber;
        }

        private void SetStartButtonState(bool state)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                start_test_button.IsEnabled = state;
            }), DispatcherPriority.Normal);
        }

        private bool CheckIfFormComplete()
        {
            if (string.IsNullOrWhiteSpace(operator_txtbox.Text) || combo_workStation.SelectedItem == null || SerialCom.SelectedItem == null || CurrentPort.SelectedItem == null)
                return false;
            else
                return true;
        }

        private void UpdateState(Test_list_item estado_teste, bool state, int index)
        {
            if(state)
            {
                UpdateProgressBar(progress_bar_increment);
                //fazer get do name da test
                UpdateTextEvolution(estado_teste.Name + " Passed");
                UpdateList(index, estado_teste.Name, true);
            }
            else
            {
                UpdateProgressBar(200);
                //fazer get do name da test
                UpdateTextEvolution(estado_teste.Name + " Failed");
                UpdateList(index, estado_teste.Name, false);
            }
        }

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
                test_list.Items.Insert(4, new Test_list_item() { Name = "DEVICE CURRENT", Path = null, });
                test_list.Items.Insert(5, new Test_list_item() { Name = "USB PORT A", Path = null, });
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
                /*
                if(mdb_state != MDB_Board.State.ToString())
                {
                    mdb_state = MDB_Board.State.ToString();
                    Debug.WriteLine(mdb_state);
                    //UpdateTextEvolution(MDB_Board.State.message);
                    //UpdateL(MDB_Board.LastTestNumber, MDB_Board.LastTestResult);
                    //if (MDB_Board.LastTestResult && !mdb_state.Contains("FinishState"))
                    //    UpdateProgressBar(progress_bar_increment);
                    //else
                    //    UpdateProgressBar(200);
                    if(mdb_state.Contains("ErrorState"))
                    {
                        UpdateProgressBar(200);
                    }
                    else if(!mdb_state.Contains("FinishState"))
                    {
                        UpdateProgressBar(progress_bar_increment);
                    }
                }
                if (mdb_state.Contains("FinishState")|| mdb_state.Contains("ErrorState"))
                    SetStartButtonState(true);
                    */
                time_elapsed = TimeSpan.Parse(stopWatch.Elapsed.ToString()).TotalSeconds;
                test_time_lbl.Text = TimeSpan.FromSeconds(stopWatch.Elapsed.TotalSeconds).ToString(@"hh\:mm\:ss");
            }), DispatcherPriority.Normal);
            /*
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
                    if (runingProcess[i].ProcessName == "mdb_test")
                    {
                        runingProcess[i].Kill();
                    }

                }
                UpdateList(0, "USB PORT B", false);

                Debug.WriteLine("Passed 15secs and no communication");
            }
            */
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

        /*** Interface Interactions ****/
        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            ConfigurationWindow configurationWindow = new ConfigurationWindow();
            configurationWindow.ShowDialog();
            Fetch_DB_Credentials();
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
        public void UpdateProgressBar(int progress_bar_inc)
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

        public void ResetProgressBar()
        {
            Dispatcher.Invoke(new Action(() =>
            {
                progress_bar.Background = Brushes.Lavender;
                progress_bar.BeginAnimation(ProgressBar.ValueProperty, null);
            }), DispatcherPriority.Background);
            
        }

        /*** Update Text Evolution***/
        public void UpdateTextEvolution(string text)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                test_evolution_txtbox.Text += text + Environment.NewLine;
            }), DispatcherPriority.Background);
        }

        /** Closing Definitions **/
        private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if(MDB_Board.NucleoPort.IsOpen)
                MDB_Board.NucleoPort.Close();
            Thread.Sleep(100);
        }

        public void UpdateList(int index_list, string text_to_update, bool test_status)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                if (test_list.Items.Count == 0)
                    return;
                test_list.Items.RemoveAt(index_list);
                test_list.Items.Insert(index_list, new Test_list_item() { Name = text_to_update, Path = (test_status) ? @"../images/pass.png" : @"../images/fail.png" });
            }), DispatcherPriority.Normal);
        }

        public void UpdateL(int index_list, bool test_status)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                if (test_list.Items.Count == 0)
                    return;
                test_list.Items.RemoveAt(index_list);
                string text_to_update = null;
                switch (index_list)
                {
                    case 0: text_to_update = "USB PORT B"; break;
                    case 1: text_to_update = "LEDS"; break;
                    case 2: text_to_update = "RELAY"; break;
                    case 3: text_to_update = "Serial Port"; break;
                    case 4: text_to_update = "DEVICE CURRENT"; break;
                    case 5: text_to_update = "USB PORT A"; break;
                }
                test_list.Items.Insert(index_list, new Test_list_item() { Name = text_to_update, Path = (test_status) ? @"../images/pass.png" : @"../images/fail.png" });
            }), DispatcherPriority.Normal);
        }

        private void ListSerialPort()
        {
            string[] ports = SerialPort.GetPortNames();
            SerialCom.Items.Clear();
            CurrentPort.Items.Clear();
            foreach (string port in ports) {
                SerialCom.Items.Add(port);
                CurrentPort.Items.Add(port);
            }
        }

        private void UpdateDBConnectionTxtBox(bool state)
        {
            if(state)
            {
                Dispatcher.Invoke(new Action(() =>
                {
                    db_status_txtbox.Background = new SolidColorBrush(Color.FromArgb(0xCC, 0x11, 0x9E, 0xDA)); //#CC119EDA
                    db_status_txtbox.Foreground = Brushes.White;
                }), DispatcherPriority.Background);
            }
            else
            {
                Dispatcher.Invoke(new Action(() =>
                {
                    db_status_txtbox.Background = Brushes.Red;
                }), DispatcherPriority.Background);
            }
        }

        
        //Fetch database credentials
        private void Fetch_DB_Credentials()
        {
            try
            {

                StreamReader reader = new StreamReader(@"Files/config.txt");
                TextReader tr = reader;
                string fileContents = tr.ReadToEnd();
                //Debug.WriteLine(fileContents);

                string[] fileLines = fileContents.Split(';');

                server = fileLines[0].Split('=', ';')[1];
                server = Regex.Replace(server, @"\s+", " ");

                database = fileLines[1].Split('=', ';')[1];
                database = Regex.Replace(database, @"\s+", " ");

                uid = fileLines[2].Split('=', ';')[1];
                uid = Regex.Replace(uid, @"\s+", " ");

                password = fileLines[3].Split('=', ';')[1];
                password = Regex.Replace(password, @"\s+", " ");

                UpdateTextEvolution("Configuration File" + Environment.NewLine +
                    "Server: " + server + Environment.NewLine +
                    "Database: " + database + Environment.NewLine +
                    "Uid:" + uid + Environment.NewLine +
                    "Password " + password);

                reader.Close();

                MDB_Board.ConnectToMysql(server, database, uid, password);
            }
            catch(Exception e)
            {
                Debug.WriteLine("Exception: " +e.Message);
                MessageBox.Show("Failed to load configuration file");
            }

        }
    }
}
