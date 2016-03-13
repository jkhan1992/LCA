using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace BDLC_LCA
{
    class Program : Control_API
    {
        public static int console_top = 0;
        private static bool first_start = true;
        private static int count1 = 0;
        private static int count2 = 0;
        private static int count3 = 0; //for Set_Defended_Peak_to_Demand function
                
        static void Main(string[] args)
        {
            Console.Title = "Load Control Application - BDLC";
            Console.WriteLine("Initializing Program...");
            Check_Database_Existence();
            Console.WriteLine("Initialization Completed at " + DateTime.Now);

            Console.WriteLine("Loading Demand_Defending from databast if exists");
            Get_Defended_Peak();

            Console.WriteLine("Database path = " + Databasepath);

            Console.WriteLine("Getting Device List...");
            Get_Device_List();

            Console.WriteLine("Type \"set defended_peak= [value]\" to change defeneded peak");
            Thread read_Strings = new Thread(() =>
            {
                int O = 0;
                while (0 < 100)
                {
                    string[] read_string = Console.ReadLine().Split(' ');
                    if (read_string[0].ToLower() == "set")
                    {
                        if (read_string[1].ToLower().Contains("defend") || read_string[1].ToLower().Contains("peak"))
                        {
                            double s_0 = S_0;
                            Double.TryParse(read_string[2], out s_0);
                            if (s_0 > 0 && s_0 != S_0)
                            {
                                Console.WriteLine("Defended Peak has been changed from " + S_0 + " kVA to " + s_0 +" kVA");
                                S_0 = s_0;                                                                
                            }
                        }
                        else
                        {
                            Console.WriteLine("Invalid Input");
                        }
                    }
                    else if (read_string[0].ToLower() == "reset")
                    {
                        if (read_string[1].ToLower().Contains("defend") || read_string[1].ToLower().Contains("peak"))
                        {
                            Console.WriteLine("Defended Peak has been changed from " + S_0 + " kVA to " + default_S_0 + " kVA");
                            S_0 = default_S_0;
                        }
                        else
                        {
                            Console.WriteLine("Invalid Input");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Invalid Input");
                    }
                    Thread.Sleep(10);
                    O = 0;
                }
            });
            read_Strings.Start();
            Console.WriteLine("\n\n");
            console_top = Console.CursorTop;
            
            Console.Write("Waiting for next 0 seconds to synchronize\nSeconds Left = ");
            int top = Console.CursorTop;
            int left = Console.CursorLeft;

            while (DateTime.Now.Second != 0)
            {                
                Console.SetCursorPosition(left, top);
                Console.Write("                                   ");
                Console.SetCursorPosition(left, top);
                Console.Write((60 - DateTime.Now.Second) + " seconds");
                Thread.Sleep(1000);
            }
            Thread main_thread = new Thread(() =>
            {
                Run();
            });
            main_thread.Start();
            BECDemand.Start_BEC_Demand_Logging_Thread();
        }
        
        public static void Run()
        {
            int i = 0;
            int check = 0;
            while (i < 100)
            {
                DateTime now_ = DateTime.Now;
                TimeSpan time = new TimeSpan(now_.Ticks);
                double running_Remainder = (double)time.TotalSeconds % 60;

                if (running_Remainder < 15)
                {
                    check++;
                    if (check == 1)
                    {
                        Clear_Console(console_top);
                        Console.SetCursorPosition(0, console_top);

                        Set_Remaining_Minutes(DateTime.Now); //Set value of minutes remaining until next interval
                        double latest_demand = 0;
                        try
                        {
                            BECDemand.LoadBECData(); //load demand to BECDemand.Demandstorage
                            latest_demand = BECDemand.Demandstorage.Last();

                            if ((now_ - BECDemand.BEC_Demand_latest_Date).Minutes < 15) //Check if Demand was updated recently
                            {
                                Set_Defended_Peak_to_Demand(BECDemand.BEC_Demand_latest_Date, latest_demand);
                            }

                            Add_to_Demand_History(latest_demand, BECDemand.BEC_Demand_latest_Date); //add latest demand value to demand history
                            Console.WriteLine("Peak to defend is set to: " + S_0.ToString("####0.00 kVA"));
                            Console.WriteLine("DateTime = " + BECDemand.BEC_Demand_latest_Date + "\nDemand value = " + latest_demand.ToString("####0.00 kVA"));

                        }
                        catch (Exception es)
                        {
                            Console.WriteLine("Failed to load demand, Error: " + es);
                        }

                        double predicted_Ongoing = ((current_slope * Remaining_minutes) + latest_demand); //predicted peek for next interval based on ongoing ROC
                        double predicted_Average = 0.0;

                        try
                        {
                            Console.WriteLine(Remaining_minutes + " minutes are remaining until next interval");

                            predicted_Ongoing = ((current_slope * Remaining_minutes) + latest_demand); //predicted peek for next interval based on ongoing ROC
                            predicted_Average = -1; //predicted peek for next interval based on average ROC
                            Console.WriteLine("\nOngoing Rate of change of Demand [kVA per minute] = " + current_slope.ToString("####0.00"));
                            Console.WriteLine("With current Ongoing [kVA per minute], at the end of this " + Demand_interval + " minutes interval,  Peak will be " + predicted_Ongoing.ToString("####0.00 kVA"));
                            if (Slope_History.Count > 0)
                            {
                                predicted_Average = ((Slope_History.Average() * Remaining_minutes) + latest_demand);

                                Console.WriteLine("\nAverage Rate of change of Demand [kVA per minute] = " + Slope_History.Average().ToString("####0.00"));
                                Console.WriteLine("With current Average [kVA per minute], at the end of this " + Demand_interval + " minutes interval,  Peak will be " + predicted_Average.ToString("####0.00 kVA"));
                            }
                        }
                        catch (Exception es)
                        {
                            Console.WriteLine("Failed to update predicted values, Error: " + es);
                        }

                        DateTime date_now = DateTime.Now;


                        try
                        {
                            if (predicted_Average >= 0)
                            {
                                InsertDemandLog(date_now, BECDemand.BEC_Demand_latest_Date, S_0, latest_demand, Demand_interval, current_slope, Slope_History.Average(), predicted_Ongoing, predicted_Average);
                            }
                            else
                            {
                                InsertDemandLog(date_now, BECDemand.BEC_Demand_latest_Date, S_0, latest_demand, Demand_interval, current_slope, predicted_Ongoing);
                            }
                        }
                        catch (Exception es)
                        {
                            Console.WriteLine("Failed to write Demand Logs to Database, Error: " + es);
                        }
                        try
                        {
                            SendLogQueries(SystemLogs_DB);

                        }
                        catch (Exception es)
                        {
                            Console.WriteLine("Failed to write error Logs to Database, Error: " + es);
                        }
                        finally
                        {
                            SystemLogs_DB.Clear();
                        }

                        try
                        {
                            if ((date_now.Minute + LC_mins_before_interval) % Demand_interval == 0 || date_now.Minute == 0) //at the end of interval
                            {
                                count2++;
                                if (count2 == 1)
                                {
                                    if ((date_now - BECDemand.BEC_Demand_latest_Date).Minutes < 15) //Check if Demand was updated recently
                                    {
                                        double average = BECDemand.Demandstorage.Average();
                                        BECDemand.Demandstorage.Clear();
                                        Console.WriteLine("DateTime = " + BECDemand.BEC_Demand_latest_Date + "\nAverage Demand value = " + average.ToString("####0.00"));
                                        
                                        if (average + Defence_Range >= S_0)
                                        {
                                            Console.WriteLine("Demand + Defence range = " + average + Defence_Range);
                                            Full_Control_ON(average);
                                            if (average > S_0)
                                            {
                                                S_0 = average;
                                            }
                                            if (first_start)
                                            {
                                                control_started_at = DateTime.Now; //update control start time
                                                control_removed_at = control_started_at.AddMinutes(1); //update control remove time
                                            }
                                            if (control_removed_at > control_started_at) //if control was removed earlier
                                            {
                                                control_started_at = DateTime.Now; //update control start time
                                            }
                                        }
                                        else if (average + (Defence_Range * 2) <= S_0)
                                        {
                                            Console.WriteLine("Demand + Defence range = " + average + Defence_Range);
                                            Control_OFF(average);
                                            if (first_start)
                                            {
                                                control_removed_at = DateTime.Now; //update control remove time
                                                control_started_at = control_removed_at.AddMinutes(1); //update control start time
                                            }
                                            if (control_started_at > control_removed_at)
                                            {
                                                control_removed_at = DateTime.Now;//update control remove time
                                            }
                                            if ((DateTime.Now - control_removed_at).TotalMinutes > control_timeout_mins) //apply control if it has been disabled for previous "control_timeout_mins" minutes
                                            {
                                                can_apply_control = true;
                                            }
                                        }
                                        first_start = false;

                                        /*
                                            Defended - 50kVA = BEC demand -> apply load
                                            S0, S (BEC demand at the end of interval)

                                            if S + 50 >= Defended -> apply full load control
                                            if S + 100 <= Defended -> remove control
                                        */
                                    }
                                    else
                                    {
                                        Console.WriteLine(date_now + ". BEC demand is not being updated. Please check server collecting data for BEC");
                                        SystemLogs_DB.Add(date_now + ". BEC demand is not being updated. Please check server collecting data for BEC");
                                    }
                                    Slope_History.Clear();
                                    Clear_Demand_History();
                                }
                            }
                            else
                            {
                                count2 = 0;
                            }
                        }
                        catch (Exception es)
                        {
                            Console.WriteLine("Failed to update Control status, Error: " + es);
                        }

                        Console.WriteLine("Control was started at : " + control_started_at);
                        Console.WriteLine("Control was removed at : " + control_removed_at);
                        if (full_Control_ON)
                        {
                            Console.WriteLine(DateTime.Now + ": Full Control is ON.");
                        }
                        else if (control_OFF)
                        {
                            Console.WriteLine(DateTime.Now + ": Control is OFF.");
                        }
                    }
                }
                else
                {
                    check = 0;
                }
                Thread.Sleep(1000);
                i = 1;
            }
        }

        private static void Clear_Console(int Console_position)
        {
            int last_line = Console.CursorTop;
            for (int i = last_line; i >= Console_position; i--)
            {
                Console.SetCursorPosition(0, i);
                Console.Write(new string(' ', Console.WindowWidth));
            }
        }

        private static void Set_Remaining_Minutes(DateTime date)
        {
            int minutes_now = date.Minute;
            int intervals = 60 / Demand_interval;

            int which_interval_is_it = 0;
            for(int i = 1; i <= intervals; i++)
            {
                if((i * Demand_interval) - minutes_now > 0 && (minutes_now - (Demand_interval * (i -1))) >= 0)
                {
                    which_interval_is_it = i;
                }
            }            
            Remaining_minutes = (which_interval_is_it * Demand_interval) - minutes_now;
        }

        private static void Set_Defended_Peak_to_Demand(DateTime date, double S)
        {
            if ((int)date.DayOfWeek > 0 && (int)date.DayOfWeek < 6) //only weekdays
            {
                if (date.Hour == 8 && date.Minute == 00)
                {
                    count3++;
                    if (count3 == 1)
                    {
                        S_0 = S;
                        Console.WriteLine("Defended Peak is being set to the value of Demand");
                    }
                }
                else
                {
                    count3 = 0;
                }
            }
        }

    }
}
