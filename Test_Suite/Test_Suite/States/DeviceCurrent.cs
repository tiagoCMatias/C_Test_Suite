﻿using System;
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
    class DeviceCurrent : State
    {
        string device_current = null;
        string usb_volt = null;
        public override void Handle(MDB_BOARD board)
        {
            //throw new NotImplementedException();
            if (board.NucleoPort == null)
                GoToNextState(board, false);
            else
            {
                board.NucleoMessage = "";
                Thread.Sleep(1500);
                board.SendToNucleo("1");
                Thread.Sleep(1500);
                if (board.NucleoMessage != null)
                {
                    device_current = board.NucleoMessage;
                    board.BoardCurrent = Regex.Match(board.NucleoMessage, @"\d+").Value;
                    Debug.WriteLine(board.BoardCurrent + "mA");
                }

                board.NucleoMessage = "";
                Thread.Sleep(1500);
                board.SendToNucleo("5");
                Thread.Sleep(1500);
                if (board.NucleoMessage != null)
                {
                    usb_volt = board.NucleoMessage;
                    board.BoardUSBVolt = Regex.Match(board.NucleoMessage, @"\d+").Value;
                    Debug.WriteLine(usb_volt + "mV");
                }
                try
                {
                    if (board.BoardCurrent != null && int.Parse(board.BoardUSBVolt) > 0)
                    {
                        GoToNextState(board, true);
                    }
                    else
                    {
                        GoToNextState(board, false);
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Exception: " +e.Message);
                    GoToNextState(board, false);
                }
                
            }


        }

        public override void GoToNextState(MDB_BOARD board, bool state)
        {
            state_number = 4;
            board.test_result[4] = state ? 1 : 0;
            board.UpdateList(state_number, state);

            if (board.MDB_MOD)
            {
                board.test_result[5] = state ? 1 : 0;
                board.BoardTestStatus = state ? 1 : 0;

                if(usb_volt != null)
                    board.UpdateList(9, state);

                board.State = new SQL_Update();
            }
            else if (state)
            {
                board.SetMDBSupply();
                
                board.UpdateMessage = "Device Current: " + device_current + "mA";
                Thread.Sleep(500);
                board.State = new Led_State();
            }
            else
            {
                MessageBox.Show("Device Fail"+Environment.NewLine+ "Device Current: " + device_current + "mA"+Environment.NewLine+ "USB -" + usb_volt + "mV", "Test Fail", MessageBoxButton.OK, MessageBoxImage.Error);
                board.BoardErrorDescription = (board.MDB_MOD) ? "Failed to Read Device Current - Second Test" : "Failed to Read Device Current - First Test";
                board.UpdateMessage = "Failed to Read Device Current";
                board.State = new ErrorState();
            }

        }
    }
}
