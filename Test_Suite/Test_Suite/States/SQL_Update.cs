using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test_Suite
{
    class SQL_Update : State
    {
        public override void GoToNextState(MDB_BOARD board, bool state)
        {
            //board.LastTestNumber = state_number;
           // board.UpdateMessageResult = state ? "Updating Database" : "Updating Database";
           // board.LastTestResult = state;

            board.State = new FinishState();
        }

        public override void Handle(MDB_BOARD board)
        {
            if (board.NucleoPort != null)
                board.CloseSerialPort();

            Debug.WriteLine("Update DB");

            

            GoToNextState(board, true);
        }
    }
}
