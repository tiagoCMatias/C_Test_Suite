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
            board.State = new FinishState();
        }

        public override void Handle(MDB_BOARD board)
        {
            board.UpdateMessage = "Saving into database";
            try
            {
                if (board.NucleoPort != null)
                    board.CloseSerialPort();

                board.InsertTestResult();

                GoToNextState(board, true);
            }
            catch (Exception e)
            {
                GoToNextState(board, false);
                Debug.WriteLine("Database Error - Exception: " + e.Message);
            }
            
        }
    }
}
