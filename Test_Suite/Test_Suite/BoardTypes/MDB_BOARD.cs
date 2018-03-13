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
        private string relayOn = "RELAY ON";
        private string relayOff = "RELAY OFF";
        private string connectionString;

        public SerialPort NucleoPort { get; set; }
        public string RS232Port { get; set; }
        public bool MDB_MOD { get; set; }
        public string NucleoMessage { get; set; }
        public MySqlConnection DB_Connection { get; set; }
        public string SqlTable = "mdb_usb_ms";
        public string UpdateMessage { get; set; }
        public int[] test_result = new int[10];
        public bool error_state { get; set; }
        public bool test_ongoing { get; set; }

        public List<Test_list_item> list_itens = new List<Test_list_item>();

        public class Test_list_item
        {
            public string Name { get; set; }
            public string Path { get; set; }
        }


        public MDB_BOARD(State state)
        {
            this.State = state;
        }

        public State State
        {
            get { return _state; }
            set { _state = value; }
        }

        public void InitializeList()
        {
            list_itens.Clear();
            list_itens.Insert(0, new Test_list_item() { Name = "USB PORT B", Path = null, });
            list_itens.Insert(1, new Test_list_item() { Name = "LEDS 1", Path = null, });
            list_itens.Insert(2, new Test_list_item() { Name = "RELAY 1", Path = null, });
            list_itens.Insert(3, new Test_list_item() { Name = "Serial Port 1", Path = null, });
            list_itens.Insert(4, new Test_list_item() { Name = "DEVICE CURRENT 1", Path = null, });
            list_itens.Insert(5, new Test_list_item() { Name = "USB PORT A", Path = null, });
            list_itens.Insert(6, new Test_list_item() { Name = "LEDS 2", Path = null, });
            list_itens.Insert(7, new Test_list_item() { Name = "RELAY 2", Path = null, });
            list_itens.Insert(8, new Test_list_item() { Name = "Serial Port 2", Path = null, });
            list_itens.Insert(9, new Test_list_item() { Name = "DEVICE CURRENT 2", Path = null, });
        }

        public void UpdateList(int index_list, bool test_status)
        {
            if (list_itens.Count > 0)
                list_itens.Remove(list_itens[index_list]);
            string text_to_update = null;
            switch (index_list)
            {
                case 0: text_to_update = "USB PORT B"; break;
                case 1: text_to_update = "LEDS 1"; break;
                case 2: text_to_update = "RELAY 1"; break;
                case 3: text_to_update = "Serial Port 1"; break;
                case 4: text_to_update = "DEVICE CURRENT 1"; break;
                case 5: text_to_update = "USB PORT A"; break;
                case 6: text_to_update = "LEDS 2"; break;
                case 7: text_to_update = "RELAY 2"; break;
                case 8: text_to_update = "Serial Port 2"; break;
                case 9: text_to_update = "DEVICE CURRENT 2"; break;
            }
            list_itens.Insert(index_list, new Test_list_item() { Name = text_to_update, Path = (test_status) ? @"../images/pass.png" : @"../images/fail.png" });
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
                if (NucleoPort == null || !NucleoPort.IsOpen)
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
                NucleoMessage = indata;
                Debug.WriteLine(indata);
            }
            catch (Exception excep)
            {
                Debug.WriteLine("Exception:" + excep.Message);
            }
        }

        public void CloseSerialPort()
        {
            if (NucleoPort != null)
            {
                NucleoPort.Close();
                NucleoPort = null;
            }
        }

        public bool SetUSBSupply()
        {
            if (NucleoPort != null && NucleoPort.IsOpen)
            {
                //turn off relay send "2"
                NucleoMessage = "";
                Thread.Sleep(3000);
                SendToNucleo("2");
                Thread.Sleep(1000);

                if (NucleoMessage.Contains(relayOff))
                {
                    Debug.WriteLine("MDB_MOD false");
                    MDB_MOD = false;
                    Thread.Sleep(100);
                    return true;
                }
                else
                {
                    Debug.WriteLine("MDB_MOD true - " + NucleoMessage);
                    MDB_MOD = true;
                    Thread.Sleep(100);
                    return false;
                }

            }
            return false;
        }

        public bool SetMDBSupply()
        {
            if (NucleoPort != null && NucleoPort.IsOpen)
            {
                //turn on relay send "6"
                NucleoMessage = "";
                Thread.Sleep(1500);
                SendToNucleo("6");

                Thread.Sleep(1000);

                if (NucleoMessage.Contains(relayOn))
                {
                    Debug.WriteLine("MDB_MOD true");
                    MDB_MOD = true;
                    Thread.Sleep(100);
                    return true;
                }
                else
                {
                    Debug.WriteLine("MDB - false");
                    MDB_MOD = false;
                    Thread.Sleep(100);
                    return false;
                }
            }
            return false;
        }
        
        public override bool ConnectToMysql(string server, string database, string uid, string password)
        {
            try
            {
                connectionString = "SERVER=" + server + ";" + "DATABASE=" +
                database + ";" + "Uid=" + uid + ";" + "PASSWORD=" + password + ";";
                //Connection
                DB_Connection = new MySqlConnection(connectionString);
                DB_Connection.Open();

                Thread.Sleep(100);


                //DB_Connection.Close();

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
                if (DB_Connection != null && DB_Connection.State == ConnectionState.Open)
                {
                    MySqlCommand command = DB_Connection.CreateCommand();
                    command.CommandText = "SELECT * FROM " + SqlTable + " WHERE sn=" + SerialNumber + " AND test_flag=1;";
                    MySqlDataReader reader = command.ExecuteReader();

                    if (reader.Read() && reader["sn"].ToString().Contains(SerialNumber))
                    {
                        DB_Connection.Close();
                        return true;
                    }
                    DB_Connection.Close();
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
                var usb_b = test_result[0];
                var usb_a = test_result[5];
                var relay = test_result[2];
                var test_current = test_result[4];
                var leds = test_result[1];
                var rs232 = test_result[3];

                DB_Connection = new MySqlConnection(connectionString);
                DB_Connection.Open();

                MySqlCommand command = DB_Connection.CreateCommand();
                command.CommandText = "INSERT INTO " + SqlTable + " ( `operator`, `sn`, `workstation`, `test_time`, `test_flag`, `usb_type_b`, " +
                    "`usb_type_a`, `relay`, `rs232`, `device_current`, `test_error`, `test_leds`, `device_read_current`, `device_usb_volt`) VALUES (" +
                    "'" + BoardOperator + "', '" + SerialNumber + "', '" + BoardWorkstation + "', '" + BoardTime + "', '" + BoardTestStatus + "', '" + usb_a +
                    "' , '" + usb_b + "', '" + relay + "', '" + rs232 + "', '" + test_current + "', '" + BoardErrorDescription + "', '" + leds + "', '"+ BoardCurrent  +"', '"+ BoardUSBVolt +"');";

                Debug.WriteLine(command.CommandText);
                    
                command.ExecuteNonQuery();

                //DB_Connection.Close();

            }
            catch (Exception e)
            {
                Debug.WriteLine("Exception: " + e.Message);
            }
        }

    }
}
