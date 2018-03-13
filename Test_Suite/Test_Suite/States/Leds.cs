using System;
using System.Diagnostics;
using System.Windows;

namespace Test_Suite
{
    class Led_State : State
    {

        public override void GoToNextState(MDB_BOARD board, bool state)
        {
            state_number = board.MDB_MOD ? 6 : 1;
            board.UpdateList(state_number - 1, state);
            board.UpdateList(state_number, state);


            board.test_result[state_number - 1] = state ? 1 : 0;
            board.test_result[1] = state ? 1 : 0;

            if (state)
            {
                board.UpdateMessage = "Leds Test Passed";
                board.State = new RelayState();
            }
            else
            {
                MessageBox.Show("Leds Test Fail", "Test Fail", MessageBoxButton.OK, MessageBoxImage.Error);
                //Debug.WriteLine("RELAY FAIL");
                board.BoardErrorDescription = board.MDB_MOD ? "Leds Test Failed - Second Test" : "Leds Test Failed - First Test";
                board.UpdateMessage = "Leds Test Failed";
                board.State = new ErrorState();
            }
        }

        public override void Handle(MDB_BOARD board)
        {
            Debug.WriteLine("Leds Handle");
            MessageBoxResult response = MessageBoxResult.No;
            response = MessageBox.Show("Starting Leds Test\n Are you ready?", "Question", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (response == MessageBoxResult.Yes)
            {
                try
                {
                    string[] lines = board.Start_script("--leds");
                    if (lines.Length >= 5)
                    {
                        response = MessageBox.Show("Leds ok?", "Question", MessageBoxButton.YesNo, MessageBoxImage.Question);
                        if (response == MessageBoxResult.Yes)
                        {
                            //Debug.WriteLine("Nice");
                            GoToNextState(board, true);

                        }
                        else
                        {
                            //Debug.WriteLine("Fail");
                            GoToNextState(board, false);
                        }
                    }
                    else
                    {
                        Debug.WriteLine("Leds Fail");
                        GoToNextState(board, false);
                    }
                }
                catch(Exception e)
                {
                    GoToNextState(board, false);
                    Debug.WriteLine("Leds Fail - Exception: " + e.Message);
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
