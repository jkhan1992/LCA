using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

namespace BDLC_LCA
{    
    class Control_API : Database_Functions
    {
        public static readonly double default_S_0 = 5300.0; //Peak to defend
        public static double S_0 = default_S_0; //Peak to defend is set on runtime
        public static readonly double Defence_Range = 50.0;
        private static List<Control_Logs> Control_logs_List = new List<Control_Logs>(); //List for containing logs
        private static List<Devices> R1_Status_List = new List<Devices>(); //contains information of devices and relay status to be written
        public static List<BECDemand.BECDemand_Value> Demand_History = new List<BECDemand.BECDemand_Value>(); //for keeping track of slope
        public static List<double> Slope_History = new List<double>();
        public static bool full_Control_ON = false;
        public static bool control_OFF = true;
        public static readonly int Demand_interval = Convert.ToInt32(ConfigurationManager.AppSettings[1]); //interval in minutes
        public static int Remaining_minutes;
        public static double Expected_peak; //Peak expected at the end of every interval
        public static double current_slope;
        public static DateTime control_removed_at;
        public static DateTime control_started_at;
        
        public static int control_timeout_mins = Convert.ToInt32(ConfigurationManager.AppSettings[3]);
        public static int LC_mins_before_interval = Convert.ToInt32(ConfigurationManager.AppSettings[2]);
        public static bool can_apply_control = true;

        private class Control_Logs
        {
            DateTime Control_Time;
            string Control_Status;
            string Control_Type;
            double S; //Demand Value
            double S0; //Peak to defend

            private Control_Logs()
            {
                
            }
            private Control_Logs(DateTime control_Time, string control_Status, string control_Type)
            {
                this.Control_Time = control_Time;
                this.Control_Status = control_Status;
                this.Control_Type = control_Type;
            }
            private Control_Logs(DateTime control_Time, string control_Status, string control_Type, double S, double S0)
            {
                this.Control_Time = control_Time;
                this.Control_Status = control_Status;
                this.Control_Type = control_Type;
                this.S = S;
                this.S0 = S0;
            }

            private DateTime _Control_Time {
                get { return Control_Time; }
                set { Control_Time = value; }
            }
            private string _Control_Status
            {
                get { return Control_Status; }
                set { Control_Status = value; }
            }
            private string _Control_Type
            {
                get { return Control_Type; }
                set { Control_Type = value; }
            }

            private double _S
            {
                get { return S; }
                set { S = value; }
            }
            private double _S0
            {
                get { return S0; }
                set { S0 = value; }
            }

        }

        private class Devices
        {
            string Device_Name;
            int Device_id;
            int R1_status;

            public Devices()
            {

            }
            public Devices(int Device_id, string Device_Name, int R1_status)
            {
                this.Device_id = Device_id;
                this.R1_status = R1_status;
                this.Device_Name = Device_Name;
            }

            
            public string _Device_Name
            {
                get { return Device_Name; }
                set { Device_Name = value; }
            }

            public int _Device_id
            {
                get { return Device_id; }
                set { Device_id = value; }
            }

            public int _R1_status
            {
                get { return R1_status; }
                set { R1_status = value; }
            }

        }

        public static void Get_Device_List()
        {
            List<string> from_Database = LoadData(RelayStatusdbconnection, "SELECT DeviceID, DeviceName, R1 FROM Status");
            try
            {
                if (from_Database.Count > 0)
                {
                    foreach (string str in from_Database)
                    {
                        string[] strs = str.Split(',');
                        try
                        {
                            Console.WriteLine(strs[0] + "\t"+ strs[1]);
                            R1_Status_List.Add(new Devices(Convert.ToInt32(strs[0]), strs[1], Convert.ToInt32(strs[2])));
                        }
                        catch(Exception)
                        {

                        }
                    }
                }
            }            
            catch(Exception es)
            {
                SystemLogs_DB.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss") + "Failed to get Device List,Error: " + es);
            }
}

        public static double Get_Defended_Peak()
        {
            double peak = Control_API.S_0;
            string read = "Select Demand_Defending From Control_Logs Order By rowid DESC Limit 1";
            List<string> thisstring = LoadData(ControlLogsdbconnection, read).ToList();
            if (thisstring.Count > 0)
            {
                double test = 0.0;
                foreach (string str in thisstring)
                {
                    if (Double.TryParse(str, out test))
                    {
                        if (test > peak)
                        {
                            peak = test;                                                      
                        }
                    }
                }
            }
            if(peak > S_0)
            {
                S_0 = peak;
                Console.WriteLine("Defended Peak from database is set to = " + S_0 + " kVA");
            }
            else
            {
                Console.WriteLine("Defended Peak is set to = " + S_0 +" kVA by default");
            }
            return peak;
        }

        public static void Full_Control_ON(double S)
        {
            //foreach device send query
            try
            {
                full_Control_ON = true;
                control_OFF = false;
                if (R1_Status_List.Count > 0)
                {
                    List<string> update_queries = new List<string>();
                    foreach (Devices device in R1_Status_List)
                    {
                        if (device._Device_id == 12 || device._Device_id == 20 || device._Device_id == 21)
                        {
                            update_queries.Add(UpdateStatus(device._Device_id, 1, 0));
                        }
                        else
                        {
                            update_queries.Add(UpdateStatus(device._Device_id, 1, 1));
                        }
                    }
                    bool success = false;
                    DateTime date_now = DateTime.Now;
                    while (!success && (DateTime.Now - date_now).Seconds < 30)
                    {
                        success = SendStatusUpdateQueries(RelayStatusdbconnection, update_queries); //Send queries
                    }
                    update_queries.Clear();
                    
                    InsertControlLog(1, DateTime.Now, "Full Control", "ON", S);
                }                
            }
            catch(Exception es)
            {
                SystemLogs_DB.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss") + "Failed to apply Full Control,Error: " + es);
            }
        }

        public static void Control_OFF(double S)
        {
            //foreach device send query
            try
            {
                control_OFF = true;
                full_Control_ON = false;
                if (R1_Status_List.Count > 0)
                {
                    List<string> update_queries = new List<string>();
                    foreach (Devices device in R1_Status_List)
                    {
                        if (device._Device_id == 12 || device._Device_id == 20 || device._Device_id == 21)
                        {
                            update_queries.Add(UpdateStatus(device._Device_id, 1, 1));
                        }
                        else
                        {
                            update_queries.Add(UpdateStatus(device._Device_id, 1, 0));
                        }
                    }
                    bool success = false;
                    DateTime date_now = DateTime.Now;
                    while (!success && (DateTime.Now - date_now).Seconds < 30)
                    {
                        success = SendStatusUpdateQueries(RelayStatusdbconnection, update_queries);
                    }
                    update_queries.Clear();                    
                    InsertControlLog(2, DateTime.Now, "Full Control", "OFF", S);
                }
            }
            catch (Exception es)
            {
                SystemLogs_DB.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss") + "Failed to turn Control OFF,Error: " + es);
            }
        }

        public static void Add_to_Demand_History(double Value, DateTime date)
        {
            List<BECDemand.BECDemand_Value> Demand_History_Copy = Demand_History.ToList();
            try
            {
                if (Demand_History_Copy.Count > 0)
                {
                    if (!Demand_History_Copy.Exists(x => x._date == date) && date.Ticks > 0)
                    {
                        Demand_History.Add(new BECDemand.BECDemand_Value(Value,date));
                       // Demand_History_2.Add(new BECDemand.BECDemand_Value(Value, date));
                        current_slope = Get_Demand_Slope();
                    }
                }
                else
                {
                    Demand_History.Add(new BECDemand.BECDemand_Value(Value, date));
                   // Demand_History_2.Add(new BECDemand.BECDemand_Value(Value, date));
                    current_slope = Get_Demand_Slope();
                }
            }
            catch(Exception es)
            {
                SystemLogs_DB.Add(DateTime.Now + " Error while Adding data point to history, Error: " + es);
            }
            Demand_History_Copy.Clear();
        }

        public static void Clear_Demand_History()
        {
            if (Demand_History.Count > 1)
            {
                BECDemand.BECDemand_Value previous_value = Demand_History.ElementAt(Demand_History.Count - 2);
                BECDemand.BECDemand_Value this_value = Demand_History.Last();
                Demand_History.Clear();
                Demand_History.Add(previous_value);
                Demand_History.Add(this_value);
            }
            else if (Demand_History.Count > 0 && Demand_History.Count <= 1)
            {
                BECDemand.BECDemand_Value this_value = Demand_History.Last();
                Demand_History.Clear();
                Demand_History.Add(this_value);
            }
        }

        public static double Get_Demand_Slope()
        {
            double slope = 0.0;
            List<BECDemand.BECDemand_Value> Demand_History_Copy = Demand_History.ToList();
            try
            {
                if (Demand_History_Copy.Count > 1)
                {
                    int current_index = Demand_History_Copy.Count - 1;
                    int previous_index = current_index - 1;
                    DateTime current_date = Demand_History_Copy[current_index]._date;
                    DateTime previous_date = Demand_History_Copy[previous_index]._date;
                    double current_value = Demand_History_Copy[current_index]._value;
                    double previous_value = Demand_History_Copy[previous_index]._value;

                    slope = (current_value - previous_value) / (double)(current_date - previous_date).Minutes; //kVA per minute   
                    Slope_History.Add(slope);
                    Console.WriteLine("\nROC History Count = " + Slope_History.Count + "\nCurrent ROC = " + slope +"\nROC Average = " + Slope_History.Average() + "\n");
                    //ROC = Rate Of Change
                }
            }
            catch (Exception es)
            {
                SystemLogs_DB.Add(DateTime.Now + " Error while retrieving demand slope, Error: " + es);
            }
            Demand_History_Copy.Clear();
            return slope;
        }
    }
}
