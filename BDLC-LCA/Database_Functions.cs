using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SQLite;
using System.Threading;
using System.Data;
using System.IO;
using System.Configuration;

namespace BDLC_LCA
{
    class Database_Functions
    { 
        private SQLiteDataAdapter DB;
        private static bool Connected;
        public static List<string> SystemLogs_DB = new List<string>(1000);
        
        public static readonly string LiveDatadb = "LiveData.db";
        public static readonly string RelayStatusdb = "RelayStatus.db";
        public static readonly string ControlLogsdb = "ControlLogs.db";
        public static readonly string ErrorLogsdb = "LCAErrorLogs";
        public static readonly string Databasepath = ConfigurationManager.AppSettings[0].ToString();
        public static readonly string ControlLogsdbconnection = @"Data Source =" + Databasepath + "\\" + ControlLogsdb + ";Version=3;New=false;Compress=true;";
        public static readonly string RelayStatusdbconnection = @"Data Source =" + Databasepath + "\\" + RelayStatusdb + ";Version=3;New=false;Compress=true;";
        public static readonly string LiveDatadbconnection = @"Data Source =" + Databasepath + "\\" + LiveDatadb + ";Version=3;New=false;Compress=true;";
        public static string Errordbconnection;

        public static List<string> GetSystemLogs()
        {
            return SystemLogs_DB;
        }

        public static void checkConnection(string db)
        {
            using (SQLiteConnection sql_con = new SQLiteConnection(db))
            {
                try
                {
                    sql_con.Open();
                }
                catch (Exception es)
                {
                    SystemLogs_DB.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss") + "Failed to open database connection,Error: " + es);
                }
                if (sql_con.State == ConnectionState.Open)
                {
                    Connected = true;
                    SystemLogs_DB.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss") + "Database " + db + " Connection successful!");
                    sql_con.Close();
                }
                else
                {
                    Connected = false;
                    SystemLogs_DB.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss") + "Database " + db + " Connection failed!");
                }
            }
        }
        
        public static bool SendStatusUpdateQueries(string db, List<string> status_this)
        {
            List<string> newList = status_this.ToList();
            bool success = false;
            using (SQLiteConnection sql_con = new SQLiteConnection(db))
            {
                try
                {
                    sql_con.Open();
                }
                catch (Exception es)
                {
                    SystemLogs_DB.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss") + "Failed to open database connection,Error: " + es);
                }
                if (sql_con.State == ConnectionState.Open)
                {
                    Connected = true;
                }
                else
                {
                    Connected = false;
                }
                if (Connected)
                {
                    try
                    {
                        SQLiteCommand sql_cmd1 = sql_con.CreateCommand();
                        sql_cmd1.CommandText = "Begin Transaction;";
                        sql_cmd1.ExecuteNonQuery();

                        foreach (string thisstring in newList)
                        {
                            SQLiteCommand sql_cmd = sql_con.CreateCommand();
                            sql_cmd.CommandText = thisstring;
                            sql_cmd.ExecuteNonQuery();
                        }
                        newList.Clear();

                        SQLiteCommand sql_cmd2 = sql_con.CreateCommand();
                        sql_cmd2.CommandText = "Commit;";
                        sql_cmd2.ExecuteNonQuery();

                        success = true;
                    }
                    catch (Exception es)
                    {
                        SystemLogs_DB.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss") + "Failed to send update status query,Error: " + es);
                    }
                }
            }
            return success;
        }

        public static void SendRelayStatusQueries(string db, List<string> Relaystatus)
        {
            using (SQLiteConnection sql_con = new SQLiteConnection(db))
            {
                try
                {
                    sql_con.Open();
                }
                catch (Exception es)
                {
                    SystemLogs_DB.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss") + "Failed to open database connection,Error: " + es);
                }
                if (sql_con.State == ConnectionState.Open)
                {
                    Connected = true;
                }
                else
                {
                    Connected = false;
                }
                if (Connected)
                {
                    try
                    {
                        SQLiteCommand sql_cmd1 = sql_con.CreateCommand();
                        sql_cmd1.CommandText = "Begin Transaction;";
                        sql_cmd1.ExecuteNonQuery();

                        foreach (string str in Relaystatus)
                        {
                            SQLiteCommand sql_cmd = sql_con.CreateCommand();
                            sql_cmd.CommandText = str;
                            sql_cmd.ExecuteNonQuery();
                        }

                        SQLiteCommand sql_cmd2 = sql_con.CreateCommand();
                        sql_cmd2.CommandText = "Commit;";
                        sql_cmd2.ExecuteNonQuery();
                    }
                    catch (Exception es)
                    {
                        SystemLogs_DB.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss") + "Failed to send relay status queries,Error: " + es);
                    }
                }
            }
        }

        public static void SendLogQueries(List<string> SystemLogs11)
        {
            string db = Databasepath + "\\SystemLogs";
            Check_ErrorandSystem_directories(db);
            string path = db + "\\" + DateTime.Now.Year + "\\" + DateTime.Now.ToString("MMM");
            path += "\\" + ControlLogsdb + "_" + DateTime.Now.ToString("yyyy-MM-dd");

            try
            {
                if (!File.Exists(path + ".db"))
                {
                    Create_ControlLogsdb();
                }
            }
            catch (Exception es)
            {
                SystemLogs_DB.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss") + "Failed to check existence of file,Error: " + es);
            }

            db = @"Data Source =" + path + ".db;Version=3;New=false;Compress=true;";
            using (SQLiteConnection sql_con = new SQLiteConnection(db))
            {
                try
                {
                    sql_con.Open();
                }
                catch (Exception es)
                {
                    SystemLogs_DB.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss") + "Failed to open database connection,Error: " + es);
                }
                if (sql_con.State == ConnectionState.Open)
                {
                    Connected = true;
                }
                else
                {
                    Connected = false;
                }
                if (Connected)
                {
                    try
                    {
                        SQLiteCommand sql_cmd1 = sql_con.CreateCommand();
                        sql_cmd1.CommandText = "Begin Transaction;";
                        sql_cmd1.ExecuteNonQuery();

                        foreach (string cmd in SystemLogs11)
                        {
                            SQLiteCommand sql_cmd = sql_con.CreateCommand();
                            if (cmd.Contains(",Error") || cmd.Contains(", Error"))
                            {
                                sql_cmd.CommandText = "INSERT INTO ErrorLogs VALUES(" + 0 + ",\"" + DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss") + "\",\"" + cmd + "\");";
                            }
                            else
                            {
                                sql_cmd.CommandText = "INSERT INTO SystemLogs VALUES(" + 0 + ",\"" + DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss") + "\",\"" + cmd + "\");";
                            }
                            sql_cmd.ExecuteNonQuery();
                        }

                        SQLiteCommand sql_cmd2 = sql_con.CreateCommand();
                        sql_cmd2.CommandText = "Commit;";
                        sql_cmd2.ExecuteNonQuery();
                    }
                    catch (Exception es)
                    {
                        SystemLogs_DB.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss") + "Failed to send Error or System message queries,Error: " + es);
                    }
                }
            }
        }

        public static bool SendQueries(string db, List<string> DataLogs)
        {
            bool success = false;
            using (SQLiteConnection sql_con = new SQLiteConnection(db))
            {
                try
                {
                    sql_con.Open();
                }
                catch (Exception es)
                {
                    SystemLogs_DB.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss") + "Failed to open database connection,Error: " + es);
                }
                if (sql_con.State == ConnectionState.Open)
                {
                    try
                    {
                        SQLiteCommand sql_cmd1 = sql_con.CreateCommand();
                        sql_cmd1.CommandText = "Begin Transaction;";
                        sql_cmd1.ExecuteNonQuery();

                        foreach (string cmd in DataLogs)
                        {
                            SQLiteCommand sql_cmd = sql_con.CreateCommand();
                            sql_cmd.CommandText = cmd;
                            sql_cmd.ExecuteNonQuery();
                        }

                        SQLiteCommand sql_cmd2 = sql_con.CreateCommand();
                        sql_cmd2.CommandText = "Commit;";
                        sql_cmd2.ExecuteNonQuery();
                        success = true;
                    }
                    catch (Exception es)
                    {
                        SystemLogs_DB.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss") + "Failed to send Data Log queries,Error: " + es);
                    }
                }
            }
            return success;
        }

        public static bool SendQuery(string db, string cmd)
        {
            bool success = false;
            using (SQLiteConnection sql_con = new SQLiteConnection(db))
            {
                try
                {
                    sql_con.Open();
                }
                catch (Exception es)
                {
                    SystemLogs_DB.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss") + "Failed to open database connection,Error: " + es);
                }
                if (sql_con.State == ConnectionState.Open)
                {
                    Connected = true;
                }
                else
                {
                    Connected = false;
                }
                if (Connected)
                {
                    try
                    {
                        SQLiteCommand sql_cmd = sql_con.CreateCommand();
                        sql_cmd.CommandText = cmd;
                        sql_cmd.ExecuteNonQuery();
                        success = true;
                    }
                    catch (Exception es)
                    {
                        SystemLogs_DB.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss") + "Failed to send query,Error: " + es);
                    }
                }
            }
            return success;
        }

        public static bool SendEncryptedQuery(string db, string cmd)
        {
            bool success = false;
            using (SQLiteConnection sql_con = new SQLiteConnection(db))
            {
                try
                {
                    sql_con.Open();
                    sql_con.ChangePassword("Jawad");
                }
                catch (Exception es)
                {
                    SystemLogs_DB.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss") + "Failed to open database connection,Error: " + es);
                }
                if (sql_con.State == ConnectionState.Open)
                {
                    Connected = true;
                }
                else
                {
                    Connected = false;
                }
                if (Connected)
                {
                    try
                    {
                        SQLiteCommand sql_cmd = sql_con.CreateCommand();
                        sql_cmd.CommandText = cmd;
                        sql_cmd.ExecuteNonQuery();
                        success = true;
                    }
                    catch (Exception es)
                    {
                        SystemLogs_DB.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss") + "Failed to send query,Error: " + es);
                    }
                }
            }
            return success;
        }

        public static bool SendErrorQueries(List<string> DataLogs)
        {
            bool success = false;
            string db = Databasepath + "\\ErrorLogs";
            Check_ErrorandSystem_directories(db);
            string path = db + "\\" + DateTime.Now.Year + "\\" + DateTime.Now.ToString("MMM");
            path += "\\" + ErrorLogsdb + "_" + DateTime.Now.ToString("yyyy-MM-dd");

            try
            {
                if (!File.Exists(path + ".db"))
                {
                    Create_ErrorLogsdb();
                }
            }
            catch (Exception es)
            {
                SystemLogs_DB.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss") + "Failed to check existence of file,Error: " + es);
            }

            db = @"Data Source =" + path + ".db;Version=3;New=false;Compress=true;";
            using (SQLiteConnection sql_con = new SQLiteConnection(db))
            {
                try
                {
                    sql_con.Open();
                }
                catch (Exception es)
                {
                    SystemLogs_DB.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss") + "Failed to open database connection,Error: " + es);
                }
                if (sql_con.State == ConnectionState.Open)
                {
                    Connected = true;
                }
                else
                {
                    Connected = false;
                }
                if (Connected)
                {
                    try
                    {
                        SQLiteCommand sql_cmd1 = sql_con.CreateCommand();
                        sql_cmd1.CommandText = "Begin Transaction;";
                        sql_cmd1.ExecuteNonQuery();

                        foreach (string cmd in DataLogs)
                        {
                            SQLiteCommand sql_cmd = sql_con.CreateCommand();
                            sql_cmd.CommandText = LogError(cmd);
                            sql_cmd.ExecuteNonQuery();
                        }

                        SQLiteCommand sql_cmd2 = sql_con.CreateCommand();
                        sql_cmd2.CommandText = "Commit;";
                        sql_cmd2.ExecuteNonQuery();
                        success = true;
                    }
                    catch (Exception es)
                    {
                        SystemLogs_DB.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss") + "Failed to send query,Error: " + es);
                    }
                }
            }
            return success;
        }

        public static void SendErrorQuery(string cmd)
        {
            string db = Databasepath + "\\ErrorLogs";
            Check_ErrorandSystem_directories(db);
            string path = db + "\\" + DateTime.Now.Year + "\\" + DateTime.Now.ToString("MMM");
            path += "\\" + ErrorLogsdb + "_" + DateTime.Now.ToString("yyyy-MM-dd");

            try
            {
                if (!File.Exists(path + ".db"))
                {
                    Create_ErrorLogsdb();
                }
            }
            catch (Exception es)
            {
                SystemLogs_DB.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss") + "Failed to check existence of file,Error: " + es);
            }

            db = @"Data Source =" + path + ".db;Version=3;New=false;Compress=true;";
            using (SQLiteConnection sql_con = new SQLiteConnection(db))
            {
                try
                {
                    sql_con.Open();
                }
                catch (Exception es)
                {
                    SystemLogs_DB.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss") + "Failed to open database connection,Error: " + es);
                }
                if (sql_con.State == ConnectionState.Open)
                {
                    Connected = true;
                }
                else
                {
                    Connected = false;
                }
                if (Connected)
                {
                    try
                    {
                        SQLiteCommand sql_cmd = sql_con.CreateCommand();
                        sql_cmd.CommandText = cmd;
                        sql_cmd.ExecuteNonQuery();
                    }
                    catch (Exception es)
                    {
                        SystemLogs_DB.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss") + "Failed to send query,Error: " + es);
                    }
                }
            }
        }

        public static List<string> LoadData(string db, string cmd)
        {
            List<string> received = new List<string>();
            bool success = false;
            int timeout = 10; // timeout in seconds
            DateTime starttime = DateTime.Now;

            while (!success && (DateTime.Now - starttime).TotalSeconds < timeout)
            {
                using (SQLiteConnection sql_con1 = new SQLiteConnection(db))
                {
                    try
                    {
                        sql_con1.Open();
                    }
                    catch (Exception es)
                    {
                        SystemLogs_DB.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss") + "Failed to open database connection,Error: " + es);
                    }
                    if (sql_con1.State == ConnectionState.Open)
                    {
                        Connected = true;
                    }
                    else
                    {
                        Connected = false;
                    }
                    string received1 = "";
                    ushort currentrow = 0;

                    if (Connected)
                    {
                        try
                        {
                            SQLiteCommand sql_cmd = sql_con1.CreateCommand();
                            sql_cmd.CommandText = cmd;
                            using (SQLiteDataReader rdr = sql_cmd.ExecuteReader())
                            {
                                if (rdr.HasRows)
                                {
                                    while (rdr.Read())
                                    {
                                        if (currentrow == 0)
                                        {
                                            for (int n = 0; n < rdr.FieldCount; n++)
                                            {
                                                if (n == rdr.FieldCount - 1)
                                                {
                                                    received1 += rdr.GetName(n);
                                                }
                                                else
                                                {
                                                    received1 += rdr.GetName(n) + ",";
                                                }
                                            }
                                            received.Add(received1);
                                            currentrow = 1;
                                        }
                                        received1 = "";
                                        for (int n = 0; n < rdr.FieldCount; n++)
                                        {

                                            if (n == rdr.FieldCount - 1)
                                            {
                                                received1 += rdr.GetValue(n);
                                            }
                                            else
                                            {
                                                received1 += rdr.GetValue(n) + ",";
                                            }
                                        }
                                        received.Add(received1);
                                    }
                                }
                                else
                                {
                                    SystemLogs_DB.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss") + "No data available to read!!");
                                }
                            }
                            success = true;
                        }
                        catch (Exception es)
                        {
                            SystemLogs_DB.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss") + "Failed to read data,Error: " + es);
                        }

                    }
                }
            }
            return received;
        }

        public static int GetStatus(string db, int id, int relayid)
        {
            int relay_value = 0;
            using (SQLiteConnection sql_con1 = new SQLiteConnection(db))
            {
                try
                {
                    sql_con1.Open();
                }
                catch (Exception es)
                {
                    SystemLogs_DB.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss") + "Failed to open database connection,Error: " + es);
                }
                if (sql_con1.State == ConnectionState.Open)
                {
                    Connected = true;
                }
                else
                {
                    Connected = false;
                }
                ushort currentrow = 0;

                if (Connected)
                {
                    try
                    {
                        SQLiteCommand sql_cmd = sql_con1.CreateCommand();
                        sql_cmd.CommandText = "Select R" + relayid + " from Status Where DeviceID = " + id;
                        using (SQLiteDataReader rdr = sql_cmd.ExecuteReader())
                        {
                            if (rdr.HasRows)
                            {
                                while (rdr.Read())
                                {
                                    if (currentrow == 0)
                                    {
                                        relay_value = rdr.GetInt32(0);
                                    }
                                    currentrow++;
                                }
                                currentrow = 0;
                            }
                            else
                            {
                                SystemLogs_DB.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss") + "No data available to read!!");
                            }
                        }
                    }
                    catch (Exception es)
                    {
                        SystemLogs_DB.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss") + "Failed to read relay status for tide control,Error: " + es);
                    }

                }
            }
            return relay_value;
        }

        public static string LogError(string error)
        {
            int id = 0;
            string Error = "INSERT INTO ErrorLogs VALUES (";
            Error += id + ", \"" + DateTime.Now + "\", \"" + error + "\");";
            return Error;
        }

        public static string LogSystem(string error)
        {
            int id = 0;
            string Error = "INSERT INTO SystemLogs VALUES (";
            Error += id + ", '" + DateTime.Now + "', '" + error + "');";
            return Error;
        }

        

        public static string ReadData(string tablename, string[] parameters)
        {
            string readdata = "SELECT ";
            for (int n = 0; n < parameters.Length; n++)
            {
                if (n < (parameters.Length - 1))
                {
                    readdata += parameters[n] + ",";
                }
                else
                {
                    readdata += parameters[n];
                }
            }
            readdata += " FROM " + tablename + ";";
            return readdata;
        }

        public static string ReadData(string tablename, string[] parameters, string whereExpression)
        {
            string readdata = "SELECT ";
            for (int n = 0; n < parameters.Length; n++)
            {
                if (n < (parameters.Length - 1))
                {
                    readdata += parameters[n] + ",";
                }
                else
                {
                    readdata += parameters[n];
                }
            }
            readdata += " FROM " + tablename + " WHERE " + whereExpression + ";";
            return readdata;
        }

        public static string ReadData(string tablename)
        {
            string readdata = "SELECT *";
            readdata += " FROM " + tablename + ";";
            return readdata;
        }

        public static string ReadData(string tablename, string whereExpression)
        {
            string readdata = "SELECT *";
            readdata += " FROM " + tablename + " WHERE " + whereExpression + ";";
            return readdata;
        }

        public static string UpdateStatus(int id, int relay, int status)
        {
            string query = "UPDATE Status Set R" + relay + " = " + status + " Where DeviceID = " + id + ";";
            return query;
        }

        public static void InsertControlLog(int id, DateTime date, string type, string status, double demand)
        {
            string insert = "INSERT INTO Control_Logs VALUES(" + id + ",\"" + date.ToString("yyyy-MM-ddTHH:mm:ss") + "\",\"" + type + "\",\"" + status;
            insert += "\"," + Control_API.S_0 + "," +  demand + ");";
            bool success = false;
            DateTime date_now = DateTime.Now;
            while (!success && (DateTime.Now - date_now).Seconds < 30)
            {
                success = SendQuery(ControlLogsdbconnection, insert);
            }
        }

        public static void InsertDemandLog(DateTime date, DateTime date_demand, double defending, double demand, double interval, double Ongoing_ROC, double Average_ROC, double Predicted_Ongoing, double Predicted_Average )
        {
            string insert = "INSERT INTO Demand_Logs VALUES(\"" +  date.ToString("yyyy-MM-ddTHH:mm:ss") + "\",\"" + date_demand.ToString("yyyy-MM-ddTHH:mm:ss") + "\",";
            insert += interval + "," + Control_API.S_0 + "," + demand + "," + Ongoing_ROC + "," + Average_ROC + "," + Predicted_Ongoing + "," + Predicted_Average + ");";
            bool success = false;
            DateTime date_now = DateTime.Now;
            while (!success && (DateTime.Now - date_now).Seconds < 30)
            {
                success = SendQuery(ControlLogsdbconnection, insert);
            }
        }

        public static void InsertDemandLog(DateTime date, DateTime date_demand, double defending, double demand, double interval, double Ongoing_ROC, double Predicted_Ongoing)
        {
            string insert = "INSERT INTO Demand_Logs VALUES(\"" + date.ToString("yyyy-MM-ddTHH:mm:ss") + "\",\"" + date_demand.ToString("yyyy-MM-ddTHH:mm:ss") + "\",";
            insert += interval + "," + Control_API.S_0 + "," + demand + "," + Ongoing_ROC + ",Null," + Predicted_Ongoing + ",Null);";
            bool success = false;
            DateTime date_now = DateTime.Now;
            while (!success && (DateTime.Now - date_now).Seconds < 30)
            {
                success = SendQuery(ControlLogsdbconnection, insert);
            }
        }
        
        public static void Delete_Older_Logs(int number, string connectionstring)
        {
            DateTime deleteDate = DateTime.Now.AddDays(-number);
            string date = deleteDate.ToString("yyyy-MM-dd");
            string delete = "DELETE FROM SystemLogs WHERE datetime(\"DateTime\") < datetime(\"" + deleteDate + "\");";
            SendQuery(connectionstring, delete);
            delete.Replace("SystemLogs", "ErrorLogs");
            SendQuery(connectionstring, delete);
        }

        public static string Vacuum()
        {
            return "Vacuum;";
        }

        public static void Create_AllDatabases()
        {
            try
            {
                Create_ControlLogsdb();
                Create_ErrorLogsdb();
            }
            catch (Exception es)
            {
                SystemLogs_DB.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss" + "Error while creating databases,Error: " + es.ToString()));
            }
        }

        public static void Create_ControlLogsdb()
        {
            string path = Databasepath + "\\" + ControlLogsdb;

            SQLiteConnection.CreateFile(path);

            string Datadb_table1 = @"CREATE TABLE `Control_Logs` (
	                                `id`	INTEGER,
	                                `DateTime`	TEXT,
	                                `Control_Type`	TEXT,
	                                `Control_Status`	TEXT,
	                                `Demand_Defending`	REAL,
	                                `Demand_Actual`	REAL);";

            string Datadb_table2 = @"CREATE TABLE `Demand_Logs` (
	                                `DateTime_Now`	TEXT,
	                                `DateTime_of_Demand`	TEXT,
	                                `Demand_Interval`	REAL,
	                                `Demand_Defending_S0`	REAL,
	                                `Demand_S`	REAL,
	                                `Ongoing_Rate_Of_Change`	REAL,
	                                `Average_Rate_Of_Change`	REAL,
	                                `Predicted_Demand_Ongoing_Rate`	REAL,
	                                `Predicted_Demand_Average_Rate`	REAL);";
            try
            {
                SendQuery(ControlLogsdbconnection, Datadb_table1);
                SendQuery(ControlLogsdbconnection, Datadb_table2);
            }
            catch (Exception es)
            {
                SystemLogs_DB.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss" + "Error while creating Tables for " + ControlLogsdb + ",Error: " + es.ToString()));
            }
        }        

        public static void Create_ErrorLogsdb()
        {
            string path = Databasepath + "\\ErrorLogs";

            Check_ErrorandSystem_directories(path);

            path += "\\" + DateTime.Now.Year + "\\" + DateTime.Now.ToString("MMM") + "\\" + ErrorLogsdb + "_" + DateTime.Now.ToString("yyyy-MM-dd");

            SQLiteConnection.CreateFile(path + ".db");
            string connection = @"Data Source =" + path + ".db;Version=3;New=false;Compress=true;";
            Errordbconnection = connection;
            string Datadb_table = @"CREATE TABLE `ErrorLogs` (
	                                `id`	INTEGER,
	                                `DateTime`	TEXT,
	                                `Error`	TEXT);";
            try
            {
                SendQuery(connection, Datadb_table);
            }
            catch (Exception es)
            {
                SystemLogs_DB.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss" + "Error while creating Tables for " + ErrorLogsdb + ",Error: " + es.ToString()));
            }
        }

        public static void Check_Database_Existence()
        {
            Thread[] theseThreads = new Thread[2];

            //Check if Sqlite Browser is running.
            Process[] processes = Process.GetProcesses(); ;
            foreach (Process process in processes)
            {
                if (process.ProcessName.Contains("sqlitebrowser"))
                {
                    process.Kill();
                }
            }

            if (!Directory.Exists(Databasepath))
            {
                SystemLogs_DB.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss") + "Creating directory for Databases");
                Directory.CreateDirectory(Databasepath);
                SystemLogs_DB.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss") + "Directory successfully created");

                SystemLogs_DB.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss") + "Creating database files!");
                Create_AllDatabases();
                SystemLogs_DB.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss") + "All database files created successfully!");
            }
            else
            {
                if (!Directory.Exists(Databasepath + "\\ErrorLogs\\"))
                {
                    SystemLogs_DB.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss") + "Creating directory for ErrorLogs Databases");
                    Directory.CreateDirectory(Databasepath + "\\ErrorLogs");
                    SystemLogs_DB.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss") + "Directory successfully created");
                }                

                try
                {
                    if (!File.Exists(Databasepath +"\\"+ ControlLogsdb))
                    {
                        Thread createdb = new Thread(() =>
                        {
                            SystemLogs_DB.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss") + "\nCreating ControlLogs.db database file");
                            Create_ControlLogsdb();
                            SystemLogs_DB.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss") + "ControlLogs.db creation complete\n");
                        });
                        theseThreads[0] = createdb;
                    }
                }
                catch (Exception es)
                {
                    SystemLogs_DB.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss") + " Failed to create" + Databasepath + "\\SystemLogs\\" + DateTime.Now.Year + "\\" + DateTime.Now.ToString("MMM") + "\\" + ControlLogsdb + DateTime.Now.ToString("yyyy-MM-dd") + ".db" + ",Error: " + es);
                }

                try
                {
                    if (!File.Exists(Databasepath + "\\ErrorLogs\\" + DateTime.Now.Year + "\\" + DateTime.Now.ToString("MMM") + "\\" + ErrorLogsdb + "_" + DateTime.Now.ToString("yyyy-MM-dd") + ".db"))
                    {
                        Thread createdb = new Thread(() =>
                        {
                            SystemLogs_DB.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss") + "\nCreating SystemLogs.db database file");
                            Create_ErrorLogsdb();
                            SystemLogs_DB.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss") + "SystemLogs.db creation complete\n");
                        });
                        theseThreads[1] = createdb;
                    }
                }
                catch (Exception es)
                {
                    SystemLogs_DB.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss") + " Failed to create" + Databasepath + "\\ErrorLogs\\" + DateTime.Now.Year + "\\" + DateTime.Now.ToString("MMM") + "\\" + ControlLogsdb + DateTime.Now.ToString("yyyy-MM-dd") + ".db" + ",Error: " + es);
                }

                foreach (Thread thr in theseThreads)
                {
                    if (thr != null)
                        thr.Start();
                }
                for (int i = 0; i < 2; i++)
                {
                    if (theseThreads[i] != null)
                    {
                        if (theseThreads[i].ThreadState == System.Threading.ThreadState.Running)
                            theseThreads[i].Join();
                    }
                }
            }
        }        

        private static void Check_ErrorandSystem_directories(string path)
        {
            if (!Directory.Exists(path + "\\" + DateTime.Now.Year))
            {
                Directory.CreateDirectory(path + "\\" + DateTime.Now.Year);
            }

            if (!Directory.Exists(path + "\\" + DateTime.Now.Year + "\\" + DateTime.Now.ToString("MMM")))
            {
                Directory.CreateDirectory(path + "\\" + DateTime.Now.Year + "\\" + DateTime.Now.ToString("MMM"));
            }
        }
    }
}
