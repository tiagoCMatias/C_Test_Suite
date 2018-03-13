using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Test_Suite
{
    /// <summary>
    /// Interaction logic for ConfigurationServer_Window.xaml
    /// </summary>
    public partial class ConfigurationWindow : MetroWindow
    {
        public string config_file_path = "/Files/config.txt";

        public ConfigurationWindow()
        {
            InitializeComponent();

            this.Topmost = true;
            Activate();

            FillFields();
        }

        private void FillFields()
        {
            try
            {
                StreamReader reader = new StreamReader(@"Files/config.txt");
                TextReader tr = reader;
                string fileContents = tr.ReadToEnd();
                //Debug.WriteLine(fileContents);

                string[] fileLines = fileContents.Split(';');

                fileLines[0] = Regex.Replace(fileLines[0], @"\s+", "");
                server_ip_txtbox.Text = fileLines[0].Split('=', ';')[1];

                fileLines[1] = Regex.Replace(fileLines[1], @"\s+", "");
                server_database_txtbox.Text = fileLines[1].Split('=', ';')[1];

                fileLines[2] = Regex.Replace(fileLines[2], @"\s+", "");
                server_username_txtbox.Text = fileLines[2].Split('=', ';')[1];

                fileLines[3] = Regex.Replace(fileLines[3], @"\s+", "");
                server_password_txtbox.Text = fileLines[3].Split('=', ';')[1];
                reader.Close();
            }
            catch
            {
                MessageBox.Show("Failed to load configuration file");
            }
        }

        private void Save_configuration_file_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(server_ip_txtbox.Text) ||
                string.IsNullOrWhiteSpace(server_username_txtbox.Text) ||
                string.IsNullOrWhiteSpace(server_password_txtbox.Text) ||
                string.IsNullOrWhiteSpace(server_database_txtbox.Text))
            {
                // Message box
                MessageBox.Show("Please fill all the textbox");
            }
            else
            {
                try
                {
                    //var assembly = Assembly.GetExecutingAssembly();
                    //var resourceName = "Redundix.Files.config.txt";
                    //var textStreamWriter = new StreamWriter(Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName));
                    /*
                    FileInfo fInfo = new FileInfo(@"Files/config.txt");
                    FileSecurity fSecurity = fInfo.GetAccessControl();
                    fSecurity.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null), FileSystemRights.Modify, InheritanceFlags.ContainerInherit, PropagationFlags.None, AccessControlType.Allow));
                    fInfo.SetAccessControl(fSecurity);
                    */

                    using (var writer = new StreamWriter(@"Files/config.txt"))
                    {
                        // writer.WriteLine(String.Empty);
                        writer.WriteLine("Server = " + Regex.Replace(server_ip_txtbox.Text, @"\s+", " ") + ";");
                        writer.WriteLine("Database = " + Regex.Replace(server_database_txtbox.Text, @"\s+", " ") + ";");
                        writer.WriteLine("Uid = " + Regex.Replace(server_username_txtbox.Text, @"\s+", " ") + ";");
                        writer.WriteLine("Password = " + Regex.Replace(server_password_txtbox.Text, @"\s+", " ") + ";");
                    }

                    //MessageBox.Show("Configurations Saved");
                    AutoClosingMessageBox.Show("Configurations Saved", "Configuration File", 1000);

                }
                catch (Exception ex)
                {
                    MessageBox.Show("A handled exception just occurred: " + ex.Message, "Exception Sample", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                configurationServer_Window.Close();
                FillFields();
            }
        }

        private void Cancel_btn_Click(object sender, RoutedEventArgs e)
        {
            configurationServer_Window.Close();
        }

        private void Ccode_server_btn_Click(object sender, RoutedEventArgs e)
        {
            server_ip_txtbox.Text = "sql.ccode.pt";
            server_database_txtbox.Text = "dev_team";
            server_username_txtbox.Text = "dev_team";
            server_password_txtbox.Text = "supereasy";
        }

        private void Hfa_server_btn_Click(object sender, RoutedEventArgs e)
        {
            server_ip_txtbox.Text = "10.0.1.7";
            server_database_txtbox.Text = "kibix";
            server_username_txtbox.Text = "kibix";
            server_password_txtbox.Text = "1234";
        }
    }
    public class AutoClosingMessageBox
    {
        System.Threading.Timer _timeoutTimer;
        string _caption;
        AutoClosingMessageBox(string text, string caption, int timeout)
        {
            _caption = caption;
            _timeoutTimer = new System.Threading.Timer(OnTimerElapsed,
                null, timeout, System.Threading.Timeout.Infinite);
            using (_timeoutTimer)
                MessageBox.Show(text, caption);
        }
        public static void Show(string text, string caption, int timeout)
        {
            new AutoClosingMessageBox(text, caption, timeout);
        }
        void OnTimerElapsed(object state)
        {
            IntPtr mbWnd = FindWindow("#32770", _caption); // lpClassName is #32770 for MessageBox
            if (mbWnd != IntPtr.Zero)
                SendMessage(mbWnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
            _timeoutTimer.Dispose();
        }
        const int WM_CLOSE = 0x0010;
        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);
    }

}

