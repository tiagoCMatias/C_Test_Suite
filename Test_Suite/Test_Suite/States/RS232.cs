using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Test_Suite
{
    class RS232 : State
    {
        

        public override void GoToNextState(MDB_BOARD board, bool state)
        {
            state_number = board.MDB_MOD ? 8 : 3;

            board.test_result[3] = state ? 1 : 0;
            board.UpdateList(state_number, state);

            if (state)
            {
                board.UpdateMessage = "RS232 Test Passed";
                board.State = new DeviceCurrent();
            }
            else
            {
                MessageBox.Show("RS232 Test Fail", "Test Fail", MessageBoxButton.OK, MessageBoxImage.Error);
                //Debug.WriteLine("RELAY FAIL");
               
                board.BoardErrorDescription = board.MDB_MOD ? "RS232 Test Failed - Second Test" : "RS232 Test Failed - First Test";
                board.UpdateMessage = "RS232 Test Failed";
                board.State = new ErrorState();
            }
                
        }

        public override void Handle(MDB_BOARD board)
        {
            try
            {
                string[] lines = board.Start_script("--com " + board.RS232Port);

                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Contains("FAILED"))
                    {
                        if (lines[i - 1].Contains("SERIAL") || lines[i - 1].Contains("[M <]checking MDB S -> M") || (lines[i - 1].Contains("[M <]checking MDB S -> M")))
                        {
                            Debug.WriteLine("Serial failed");
                            GoToNextState(board, false);
                            return;
                        }
                    }
                }
                Thread.Sleep(100);
                GoToNextState(board, true);
                //Debug.WriteLine("RS232 Sucess");
            }
            catch (Exception e)
            {
                GoToNextState(board, false);
                Debug.WriteLine("RS232 Fail - Exception: " + e.Message);
            }            
        }
    }
}
