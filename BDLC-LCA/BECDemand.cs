﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Data;

namespace BDLC_LCA
{
    class BECDemand : Database_Functions
    {
        public class BECDemand_Value
        {
            double Value;
            DateTime date;

            public BECDemand_Value()
            {

            }

            public BECDemand_Value(double Value, DateTime date)
            {
                this.Value = Value;
                this.date = date;
            }
            public double _value
            {
                get { return Value; }
                set { Value = value; }
            }

            public DateTime _date
            {
                get { return date; }
                set { date = value; }
            }
        }

        private static bool Connected;
        private static string BECdbconnection = "Data Source=199.83.136.42,8080;Initial Catalog=ION_Data;Persist Security Info=True;User ID=AMG;Password=amg#energy1;Connection Timeout=30;";
        public static List<double> Demandstorage = new List<double>();
        public static DateTime BEC_Demand_latest_Date = DateTime.Now;

        public static List<string> SystemLogs_BECDemand = new List<string>();

        public static List<string> LoadBECData()
        {
            List<string> received = new List<string>();
            using (SqlConnection sql_con1 = new SqlConnection(BECdbconnection))
            {
                
                sql_con1.Open();
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
                        SqlCommand sql_cmd = new SqlCommand();
                        sql_cmd.Connection = sql_con1;
                        sql_cmd.CommandText = ReadBECDemand();
                        using (SqlDataReader rdr = sql_cmd.ExecuteReader())
                        {
                            if (rdr.HasRows)
                            {
                                while (rdr.Read())
                                {
                                    DateTime date;
                                    double value = 0.0;

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
                                        SystemLogs_BECDemand.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss") + received1);
                                    }
                                    else
                                    {
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
                                        SystemLogs_BECDemand.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss") + received1);
                                        date = rdr.GetDateTime(2);
                                        if (date.Ticks > 0)
                                            BEC_Demand_latest_Date = date;
                                        value = Convert.ToDouble(rdr.GetValue(3));
                                        if (value > 0)
                                        {
                                            Demandstorage.Add(value);
                                            received.Add(received1);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                SystemLogs_BECDemand.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss") + "No data available to read!!");
                            }
                        }
                    }
                    catch (Exception es)
                    {
                        SystemLogs_BECDemand.Add(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss") + "Failed to read data!!,Error: " + es);
                    }
                    finally
                    {
                        sql_con1.Close();

                    }

                }
            }
            return received;

        } //load result from the query into a list of strings

        /*      double value = 0.0;
                int deviceid = 00;
                if (Demandstorage.Count > 0)
                value = Demandstorage.Average();
        */       

        public static string ReadBECDemand()
        {
            int offset = -1 * (DateTime.UtcNow - DateTime.Now).Hours;
            string Demand = "SELECT TOP 2 [SourceName],[SourceNamespace],DATEADD(HOUR," + offset + ", [TimestampUTC]),[Value] ";
            Demand += "FROM [ION_Data].[dbo].[vDataLogChannelValue] WHERE SourceName = 'IONMeter.BEC' and Quantity Like ";
            Demand += "'apparent power sli%' ORDER BY TimestampUTC DESC";
            return Demand;
        }

        public static List<string> GetSystemLogs()
        {
            return SystemLogs_BECDemand;
        }
    }
}
