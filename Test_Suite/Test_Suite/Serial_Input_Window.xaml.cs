using MahApps.Metro.Controls;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace Test_Suite
{
    /// <summary>
    /// Interaction logic for Serial_Input_Window.xaml
    /// </summary>
    public partial class Serial_Input_Window : MetroWindow
    {
        public string serialNumber = "Cancel";
        public Serial_Input_Window()
        {
            InitializeComponent();
            this.Topmost = true;
            Activate();
            serial_number_txtbox.Focus();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            serialNumber = serial_number_txtbox.Text;
            this.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            serialNumber = "Cancel";
            this.Close();
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }
    }
}
