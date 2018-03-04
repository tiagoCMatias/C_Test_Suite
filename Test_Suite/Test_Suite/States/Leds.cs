using System.Diagnostics;
using System.Windows;

namespace Test_Suite
{
    class Led_State : State
    {

        public override void GoToNextState(MDB_BOARD board, bool state)
        {
            state_number = 1;
            //board.LastTestNumber = state_number;
            //board.UpdateMessageResult = state ? "Leds Test Passed" : "Leds Test Failed";
            //board.LastTestResult = state;

            if (state)
                board.State = new RelayState();
            else
                board.State = new ErrorState();
        }

        public override void Handle(MDB_BOARD board)
        {
            Debug.WriteLine("Leds Handle");
            MessageBoxResult response = MessageBoxResult.No;
            response = MessageBox.Show("Starting Leds Test\n Are you ready?", "Question", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (response == MessageBoxResult.Yes)
            {
                string[] lines = board.Start_script("--leds");
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
                    response = MessageBox.Show("Leds ok?", "Question", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (response == MessageBoxResult.Yes)
                    {
                        Debug.WriteLine("Nice");
                        GoToNextState(board, true);

                    }
                    else
                    {
                        Debug.WriteLine("Fail");
                        GoToNextState(board, false);                        
                    }
                }
                else
                {
                    Debug.WriteLine("Leds Fail");
                    GoToNextState(board, false);                    
                }
            }
            else
            {
                GoToNextState(board, false);
                Debug.WriteLine("Leds Fail");
            }
        }
    }
}
