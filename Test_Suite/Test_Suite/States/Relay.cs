using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Test_Suite
{
    class RelayState : State
    {
        public override void GoToNextState(MDB_BOARD board, bool state)
        {
            state_number = 2;

            board.test_result[state_number] = state ? 1 : 0;
            board.UpdateList(state_number, state);

            if (state)
            {
                board.UpdateMessage = "Relay Test Passed";
                board.State = new RS232();
            }
            else
            {
                board.BoardErrorDescription = "Relay Test Failed";
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
                    board.NucleoMessage = "";
                    Thread.Sleep(1000);
                    board.SendToNucleo("4");
                    Thread.Sleep(1000);
                    List<String> listStrLineElements = board.NucleoMessage.Split(';').ToList();
                    Debug.WriteLine("RELAY PASSED");
                    foreach (var item in listStrLineElements)
                    {
                        var number = int.Parse(Regex.Match(item, "\\d+").Value);

                        if (number > 2 || number <= 0)
                        {
                            Debug.WriteLine("ops");
                        }
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
