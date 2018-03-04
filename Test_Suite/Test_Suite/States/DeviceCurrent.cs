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
    class DeviceCurrent : State
    {
        public override void Handle(MDB_BOARD board)
        {
            
            //throw new NotImplementedException();
            if (board.NucleoPort == null)
                GoToNextState(board, false);
            board.SendToNucleo("1");
            Thread.Sleep(100);
            if(board.NucleoMessage != null)
            {
                board.BoardCurrent = Regex.Match(board.NucleoMessage, @"\d+").Value;
                Debug.WriteLine(board.BoardCurrent + "mA");
            }
            if (board.BoardCurrent != null)
            {

                GoToNextState(board, true);
            }
                
        }

        public override void GoToNextState(MDB_BOARD board, bool state)
        {
            state_number = 4;

            //board.LastTestNumber = state_number;
            //board.LastTestResult = state;
            //board.UpdateMessageResult = state ? "Device Current Passed : " + board.BoardCurrent + "mA" : "Failed to Read Device Current";

            if(board.MDB_MOD)
            {
                board.State = new SQL_Update();
            }
            else if (state)
            {
                while (board.MDB_MOD == false)
                    board.SetMDBSupply();
                board.State = new Led_State();
            }
            else
                board.State = new ErrorState();
        }
    }
}
