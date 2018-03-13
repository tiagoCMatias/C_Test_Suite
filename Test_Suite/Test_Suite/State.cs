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
    abstract class State
    {
        public int state_number;
        public abstract void Handle(MDB_BOARD board);
        public abstract void GoToNextState(MDB_BOARD board, bool state);
    }

    class Init_State : State
    {
        
        public override void GoToNextState(MDB_BOARD board, bool state)
        {
            state_number = 0;
            if(state)
                board.State = new Led_State();
            else
                board.State = new ErrorState();
        }

        public override void Handle(MDB_BOARD board)
        {
            board.test_ongoing = true;
            GoToNextState(board, true);
        }
    }

    class Null_state : State
    {

        public override void GoToNextState(MDB_BOARD board, bool state)
        {

        }

        public override void Handle(MDB_BOARD board)
        {
        }
    }

    class ErrorState : State
    {
        public override void GoToNextState(MDB_BOARD board, bool state)
        {
            throw new NotImplementedException();
        }

        public override void Handle(MDB_BOARD board)
        {
            Thread.Sleep(1000);
            board.UpdateMessage = "Error detected during testing";
            Thread.Sleep(1000);
            Debug.WriteLine("ERROR");
            board.error_state = true;
            board.test_ongoing = false;
            if (board.NucleoPort.IsOpen)
                board.CloseSerialPort();

            board.State = new SQL_Update();
        }
    }
    
    class FinishState : State
    {
        public override void GoToNextState(MDB_BOARD board, bool state)
        {
            throw new NotImplementedException();
        }

        public override void Handle(MDB_BOARD board)
        {
            Debug.WriteLine("Finish Handle");
        }
    }
}
