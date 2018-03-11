using MahApps.Metro.Controls;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
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
        string board_state;

        string server;
        string database;
        string uid;
        string password;
        private DispatcherTimer timer;
        private Stopwatch stopWatch;
        private double time_elapsed;
        private int progress_bar_increment = 10;
        private int error_progress_bar = 200;

        public MainWindow()
        {
            InitializeComponent();
            ListSerialPort();
            Fetch_DB_Credentials();
        }

        private void Start_test_button_Click(object sender, RoutedEventArgs e)
        {

            if (!CheckIfFormComplete())
            {
                MessageBox.Show("Please select all itens correctly", "Wrong Fields", MessageBoxButton.OKCancel, MessageBoxImage.Exclamation);
                SetStartButtonState(true);
                return;
            }
            MDB_Board = new MDB_BOARD(new Init_State())
            {
                BoardOperator = operator_txtbox.Text,
                BoardWorkstation = combo_workStation.Text,
                SerialNumber = GetSerialNumber(),
                BoardErrorDescription = "No Errors"
            };
            MDB_Board.ConnectToMysql(server, database, uid, password);

            //Check for a Valid DB Connection - Error Exit Program
            if (!UpdateDBConnectionBox())
            {
                MessageBox.Show("Error Connecting to Database", "Check Database configuration", MessageBoxButton.OKCancel, MessageBoxImage.Exclamation);
                SetStartButtonState(true);
                return;
            }
            //Check for a valid Serial Number or a Repeated One - Error Exit Program
            if (String.IsNullOrEmpty(MDB_Board.SerialNumber) || MDB_Board.CheckRepeatedTest())
            {
                MessageBox.Show("Error In Serial Number", "Repeated Serial Number", MessageBoxButton.OKCancel, MessageBoxImage.Exclamation);
                SetStartButtonState(true);
                return;
            }
            //So far so good...
            ResetProgressBar();
            //Assign Serial Communication Ports
            MDB_Board.RS232Port = SerialCom.SelectedItem.ToString();
            MDB_Board.NucleoSerialCommunication(CurrentPort.SelectedItem.ToString());
            //Validate Serial Ports - Error Exit Program
            if (!MDB_Board.CorrectPortConfig())
            {
                MDB_Board.CloseSerialPort();
                MessageBox.Show("Wrong Port Selection", "Wrong Fields", MessageBoxButton.OKCancel, MessageBoxImage.Exclamation);
                SetStartButtonState(true);
                return;
            }
            //Set Correct Power Supply
            MDB_Board.SetMDBSupply();
            //Everything is Ok
            //Start Testing with scripts in a Thread
            try
            {
                MDB_Board.InitializeList();
                test_list.ItemsSource = MDB_Board.list_itens;
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
            if (string.IsNullOrWhiteSpace(operator_txtbox.Text) || combo_workStation.SelectedItem == null || SerialCom.SelectedItem == null || CurrentPort.SelectedItem == null || SerialCom.SelectedItem == CurrentPort.SelectedItem)
                return false;
            else
                return true;
        }
  
        //Application Timer
        public void StartTimer()
        {
            timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(0.01)
            };
            timer.Tick += DispatcherTimerTick_;
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
            }));
            Thread.Sleep(1);

            if (board_state != MDB_Board.State.ToString())
            {
                MDB_Board.BoardTime = DateTime.Now + " - " + TimeSpan.FromSeconds(stopWatch.Elapsed.TotalSeconds).ToString(@"hh\:mm\:ss");
                board_state = MDB_Board.State.ToString();
                UpdateTextEvolution(MDB_Board.UpdateMessage);
                test_list.ItemsSource = null;
                test_list.ItemsSource = MDB_Board.list_itens;
                if (!board_state.Contains("SQL") && !board_state.Contains("Error") && !board_state.Contains("Finish"))
                    UpdateProgressBar(progress_bar_increment);
                if(MDB_Board.BoardTestStatus == 1)
                {   
                    UpdateProgressBar(100);
                }
            }
            if(board_state.Contains("Error"))
            {
                UpdateProgressBar(error_progress_bar);
            }
            if (board_state.Contains("Finish"))
            {
                SetStartButtonState(true);
                if (MDB_Board.DB_Connection != null && MDB_Board.DB_Connection.State == ConnectionState.Open)
                    MDB_Board.DB_Connection.Close();
                timer.Stop();
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
            if(progress_bar_inc == error_progress_bar)
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
            try
            {
                if (MDB_Board.NucleoPort != null)
                    MDB_Board.NucleoPort.Close();
                if (MDB_Board.DB_Connection != null && MDB_Board.DB_Connection.State == ConnectionState.Open)
                    MDB_Board.DB_Connection.Close();
            }
            catch (Exception exc)
            {
                Debug.WriteLine("Erro Closing " + exc.Message);
            }
            
            Thread.Sleep(1000);
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

        private bool UpdateDBConnectionBox()
        {
            if (MDB_Board.DB_Connection != null && MDB_Board.DB_Connection.State == ConnectionState.Closed)
                MDB_Board.ConnectToMysql(server, database, uid, password);
            if (MDB_Board.DB_Connection != null && MDB_Board.DB_Connection.State == ConnectionState.Open)
            {
                
                Dispatcher.Invoke(new Action(() =>
                {
                    db_status_txtbox.Background = new SolidColorBrush(Color.FromArgb(0xCC, 0x11, 0x9E, 0xDA)); //#CC119EDA
                    db_status_txtbox.Foreground = Brushes.White;
                }), DispatcherPriority.Background);
                return true;
            }
            else
            {
                Dispatcher.Invoke(new Action(() =>
                {
                    db_status_txtbox.Background = Brushes.Red;
                }), DispatcherPriority.Background);
                return false;
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

                UpdateTextEvolution(Environment.NewLine + "Configuration File" + Environment.NewLine +
                    "Server: " + server + Environment.NewLine +
                    "Database: " + database + Environment.NewLine +
                    "Uid:" + uid + Environment.NewLine +
                    "Password " + password);

                reader.Close();

                
            }
            catch(Exception e)
            {
                Debug.WriteLine("Exception: " +e.Message);
                MessageBox.Show("Failed to load configuration file");
            }

        }
    }
}
