using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Threading;

namespace Test_Suite
{
    class MDB_BOARD : Board_Class
    {
        private State _state;

        public SerialPort NucleoPort { get; set; }
        public string RS232Port { get; set; }
        public bool MDB_MOD { get; set; }
        public string NucleoMessage { get; set; }
        private MySqlConnection db_con;
        public string SqlTable = "mdb_usb_ms";


        public MDB_BOARD(State state)
        {
            this.State = state;
        }

        public List<State_result> StateTestResult { get; set; }

        public State State
        {
            get { return _state; }
            set { _state = value; }
        }


        public override void StartTesting()
        {
            while (_state.GetType().Name != "FinishState")
                State.Handle(this);
        }

        public void NucleoSerialCommunication(string comPort)
        {
            try
            {
                if (NucleoPort == null)
                {
                    NucleoPort = new SerialPort(comPort, 9600, Parity.None, 8, StopBits.One);
                    NucleoPort.DataReceived += new SerialDataReceivedEventHandler(ReceiveFromNucleo);

                    NucleoPort.Open();
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("Exception: " + e.Message);
            }
        }

        public bool CorrectPortConfig()
        {
            //consumo_corrent = string.Empty;
            SendToNucleo("1");

            Thread.Sleep(150);

            if (string.IsNullOrEmpty(NucleoMessage))
                return false;
            else
                return true;
        }

        public void SendToNucleo(string data)
        {
            try
            {
                NucleoPort.WriteLine(data);
            }
            catch (Exception e)
            {
                Debug.WriteLine("Exception: " + e.Message);
            }
        }

        public void ReceiveFromNucleo(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                SerialPort sp = (SerialPort)sender;
                string indata = sp.ReadLine();
                this.NucleoMessage = indata;
                Debug.WriteLine(indata);
            }
            catch (Exception excep)
            {
                Debug.WriteLine("Exception:" + excep.Message);
            }

            //ValidateMessage(indata);
        }

        public void CloseSerialPort()
        {
            if (NucleoPort != null)
            {
                NucleoPort.Close();
                NucleoPort = null;
            }
        }

        public void SetMDBSupply()
        {
            if (NucleoPort != null && NucleoPort.IsOpen)
            {
                SendToNucleo("2");
                Thread.Sleep(100);

                if (NucleoMessage.Contains("PA_5 + PA_7 On"))
                {
                    MDB_MOD = true;
                }
                else
                {
                    MDB_MOD = false;
                }

            }
        }

        public void SetUSBSupply()
        {
            if (NucleoPort != null && NucleoPort.IsOpen)
            {
                SendToNucleo("6");
                Thread.Sleep(100);

                if (NucleoMessage.Contains("PA_5 + PA_7 Off"))
                {
                    MDB_MOD = true;
                }
                else
                {
                    MDB_MOD = false;
                }

            }
        }
        
        public override bool ConnectToMysql(string server, string database, string uid, string password)
        {
            try
            {
                string connectionString = "SERVER=" + server + ";" + "DATABASE=" +
                database + ";" + "Uid=" + uid + ";" + "PASSWORD=" + password + ";";
                //Connection
                db_con = new MySqlConnection(connectionString);
                db_con.Open();

                Thread.Sleep(100);

                return true;
            }
            catch (Exception e)
            {
                Debug.WriteLine("Exception: " + e.Message);

                return false;
            }

        }

        public override bool CheckRepeatedTest()
        {
            try
            {
                if (db_con != null && db_con.State == ConnectionState.Open)
                {
                    MySqlCommand command = db_con.CreateCommand();
                    command.CommandText = "SELECT * FROM " + SqlTable + " WHERE sn=" + SerialNumber + " AND test_flag=1;";
                    MySqlDataReader reader = command.ExecuteReader();

                    if (reader.Read() && reader["sn"].ToString().Contains(SerialNumber))
                    {
                        db_con.Close();
                        return true;
                    }
                    db_con.Close();
                    return false;
                }
                else
                    return false;

            }

            catch (Exception e)
            {
                Debug.WriteLine("Exception: " + e.Message);
                return false;
            }
        }

        public override void InsertTestResult()
        {
            try
            {
                var usb_b = StateTestResult[0];
                var usb_a = StateTestResult[1];
                var relay = StateTestResult[2];
                var test_current = StateTestResult[3];
                var leds = StateTestResult[4];
                var rs232 = StateTestResult[5];
                var observations = "Something";
                if (db_con != null && db_con.State == ConnectionState.Open)
                {
                    MySqlCommand command = db_con.CreateCommand();
                    command.CommandText = "INSERT INTO " + SqlTable + " ( `operator`, `sn`, `workstation`, `test_time`, `test_flag`, `usb_type_b`, " +
                        "`usb_type_a`, `relay`, `rs232`, `device_current`, `test_error`, `test_leds`) VALUES (" +
                        "'" + BoardOperator + "', '" + SerialNumber + "', '" + BoardWorkstation + "', '" + BoardTime + "', '" + BoardTestStatus + "', '" + usb_a +
                        "' , '" + usb_b + "', '" + relay + "', '" + rs232 + "', '" + test_current + "', '" + observations + "', '" + leds + "');";

                    command.ExecuteNonQuery();

                    db_con.Close();

                    Debug.WriteLine(command.CommandText);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("Exception: " + e.Message);
            }
        }

    }

    public class State_result
    {
        public string Name { get; set; }
        public bool Result { get; set; }
    }
}
