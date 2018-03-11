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
            //board.LastTestNumber = state_number;
            //board.UpdateMessageResult = state ? "Board Initialized" : "Failed to Initialize board";
            //board.LastTestResult = state;

            

            state_number = 0;
            if(state)
                board.State = new Led_State();
            else
                board.State = new ErrorState();
        }

        public override void Handle(MDB_BOARD board)
        {
            Debug.WriteLine("Init Handle");

           /* if (board.NucleoPort != null)
                board.CloseSerialPort();
            */
            GoToNextState(board, true);
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
            if(board.NucleoPort.IsOpen)
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
            //context.State = new RelayState();
        }
    }
}
