using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Test_Suite
{
    class RelayState : State
    {
        public override void GoToNextState(MDB_BOARD board, bool state)
        {
            state_number = board.MDB_MOD ? 7 : 2;

            board.test_result[2] = state ? 1 : 0;
            board.UpdateList(state_number, state);

            if (state)
            {
                board.UpdateMessage = "Relay Test Passed";
                board.State = new RS232();
            }
            else
            {
                board.BoardErrorDescription = board.MDB_MOD ? "Relay Test Failed - Second Test" : "Relay Test Failed - First Test";
                board.UpdateMessage = "Relay Test Failed";
                board.State = new ErrorState();
            }
                
        }

        public override void Handle(MDB_BOARD board)
        {
            //Debug.WriteLine("Relay Handle");
            try
            {
                board.NucleoMessage = "";

                board.SendToNucleo("3");

                Thread.Sleep(1000);

                if (board.NucleoMessage == null)
                {
                    Debug.WriteLine("No Reset ACK... Failed State");
                    GoToNextState(board, false);
                    return;
                }

                if (!board.NucleoMessage.Contains("Reset"))
                {
                    Debug.WriteLine("No Reset ACK... Failed State");
                    GoToNextState(board, false);
                    return;
                }

                string[] lines = board.Start_script("--relay");

                if (lines.Length >= 5)
                {
                    board.NucleoMessage = "";
                    Thread.Sleep(1000);
                    board.SendToNucleo("4");
                    Thread.Sleep(1000);
                    List<String> listStrLineElements = board.NucleoMessage.Split(';').ToList();
                    Debug.WriteLine("RELAY PASSED");
                    foreach (var item in listStrLineElements)
                    {
                        var number = int.Parse(Regex.Match(item, "\\d+").Value);
                        /*
                        if (number > 2 || number <= 0)
                        {
                            MessageBox.Show("Relay Count Fail", "Test Fail", MessageBoxButton.OK, MessageBoxImage.Error);
                            //Debug.WriteLine("RELAY FAIL");
                            GoToNextState(board, false);
                            return;
                        }*/
                        Debug.WriteLine(item.ToString());
                    }

                    //Debug.WriteLine(listStrLineElements);
                    GoToNextState(board, true);

                }
                else
                {
                    Debug.WriteLine("RELAY FAIL");
                    GoToNextState(board, false);
                }
            }
            catch(Exception e)
            {
                Debug.WriteLine("RELAY FAIL - Exception: " + e.Message);
                GoToNextState(board, false);                
            }            
        }
    }
}
