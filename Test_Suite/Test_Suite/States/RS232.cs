using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Test_Suite
{
    class RS232 : State
    {
        

        public override void GoToNextState(MDB_BOARD board, bool state)
        {
            state_number = 3;
            ///board.LastTestNumber = state_number;
            //board.UpdateMessageResult = state ? "RS232 Passed" : "RS232 Test Failed";
            //board.LastTestResult = state;

            if (state)
                board.State = new DeviceCurrent();
            else
                board.State = new ErrorState();
        }

        public override void Handle(MDB_BOARD board)
        {
            Debug.WriteLine("RS232 State " + board.RS232Port);

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
            Debug.WriteLine("RS232 Sucess");
            return;
        }
    }
}
