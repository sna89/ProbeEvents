using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

namespace UncertainEventStreams.Preprocessing
{
    public class StoreHelper
    {
        private const int NUM_OF_EVENTS = 4;
        private const int NO_DATA = -1;
        private const int JOURENT_PATTERN_STOP_COLUMN = 2;
        private const int EVENT_LOG_STOP_COLUMN = 13;
        private const int AT_STOP_COLUMN = 14;
        private const int TIMESTAMP_COLUMN = 0;

        private List<string> EVENT_TYPES = new List<string>() { "0 - Start", "1 - NotActive", "2 - Resume", "3 - Active", "4 - Suspend", "5 - End" };
        enum EventType {
            Start,
            NotActive,
            Resume,
            Active,
            Suspend,
            End
        };
        public StoreHelper()
        {

        }

        public IEnumerable<T> GetReader<T>(string cmd, Func<IDataRecord, T> extract)
        {
            return GetReader<T>(cmd, extract, new Dictionary<string, object>());
        }


        public IEnumerable<T> GetReader<T>(string cmd, Func<IDataRecord, T> extract, Dictionary<string, object> parameters)
        {
            using (var connection = new SqlConnection(ConfigurationManager.ConnectionStrings["LogConnection"].ConnectionString))
            {
                //Open connection
                connection.Open();

                // build command
                var command = connection.CreateCommand();
                command.CommandText = cmd;
                command.CommandTimeout = 10000;
                foreach (var param in parameters)
                {
                    command.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                }

                //execute & transform
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        yield return extract(reader);
                    }
                }
            }
        }

        public int ExecuteNonQuery(string cmd)
        {
            return ExecuteNonQuery(cmd, new Dictionary<string, object>());
        }

        public int ExecuteNonQuery(string cmd, Dictionary<string, object> parameters)
        {
            using (var connection = new SqlConnection(ConfigurationManager.ConnectionStrings["LogConnection"].ConnectionString))
            {
                //Open connection
                connection.Open();

                // build command
                var command = connection.CreateCommand();
                command.CommandText = cmd;
                foreach (var param in parameters)
                {
                    command.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                }

                return command.ExecuteNonQuery();
            }
        }

        public DataTable FillJourneyPatternsDT(List<string> JourneyList)
        {
            #region query
            //STEP 1:
            //Get all (or relavant) journey pattern details ([Journey Pattern ID],[Stop Index],[Stop ID])
            //ordered by [Journey Pattern ID],[Stop Index] (ASC) from JourneyPatterns table

            var journeyPatternsQuery = @"
            WITH DS as
            (
            SELECT DISTINCT CASE WHEN LEN([Journey Pattern ID]) = 5 THEN (select CONCAT('000',[Journey Pattern ID]))
	               WHEN LEN([Journey Pattern ID]) = 6 THEN (select CONCAT('00',[Journey Pattern ID]))
	               WHEN LEN([Journey Pattern ID]) = 7 THEN (select CONCAT('0',[Journey Pattern ID]))
	               ELSE [Journey Pattern ID]
	               END AS [Journey Pattern ID]
	               ,[Stop Index]
                  ,[A]
                  ,[Stop ID]
                  ,[B]
                  ,[C]
                  ,[D]
            FROM [ProbeEvents].[dbo].[JourneyPatterns]
            )
            SELECT DISTINCT [Journey Pattern ID],[Stop Index],[Stop ID]
              FROM DS
              where 1 = 1 and 
              [Journey Pattern ID] in (select distinct [Journey Pattern ID] from [ProbeEvents].[dbo].[EventLog])
              order by [Journey Pattern ID],[Stop Index]
              ";
            #endregion

            #region Create Datatable
            DataTable dt = new DataTable();
            dt.Columns.Add("Journey Pattern ID", typeof(string));
            dt.Columns.Add("Stop Index", typeof(string));
            dt.Columns.Add("Stop ID", typeof(string));
            #endregion

            #region execute query
                        using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["LogConnection"].ConnectionString))
                        {
                            //Open connection
                            conn.Open();

                            SqlCommand cmd = new SqlCommand(journeyPatternsQuery, conn);
                            SqlDataReader reader = cmd.ExecuteReader();
                            while (reader.Read())
                            {
                                var row = dt.NewRow();
                                row[0] = reader.GetString(0);
                                if (!JourneyList.Contains(row[0]))
                                {
                                    string Journey = row[0].ToString();
                                    JourneyList.Add(Journey);
                                }
                                row[1] = reader.GetInt32(1);
                                row[2] = reader.GetInt32(2);
                                dt.Rows.Add(row);
                            }

                            reader.Close();
                            conn.Close();
                        }
                        #endregion

            return dt;
        }

        public DataTable CreateEventLog()
        {
            DataTable dt = new DataTable();
            var FileImport = new FileImport();
            var columns = FileImport.GetEventLogColumns();
            foreach (var col in columns)
            {
                dt.Columns.Add(col.Key, col.Value);
            }
            return dt;
        }

        public void AddJourenyToEventLogDT(DataTable eventLogDT, String journey)
        {

            #region sql

            var sqlQuery = @"select * from [ProbeEvents].[dbo].[EventLog] where [Journey Pattern ID] = @journeyListID 
            and timestamp >= '2014-09-14 10:00:00.000' and timestamp <= '2014-09-14 12:00:00.000'
            order by [Vehicle Journey ID],[Timestamp]";
            
                
            #endregion
            using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["LogConnection"].ConnectionString))
            {
                conn.Open();
                using (SqlCommand command = new SqlCommand(sqlQuery, conn))
                {
                    //string journeyListParameter = MakeJourneyListAsParameter(journeyList);
                    SqlParameter param = new SqlParameter();
                    param.ParameterName = "@journeyListID";
                    param.Value = journey;
                    command.Parameters.Add(param);
                    SqlDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        var row = eventLogDT.NewRow();
                        row[0] = reader.GetValue(0);
                        row[1] = reader.GetValue(1);
                        row[2] = reader.GetValue(2);
                        row[3] = reader.GetValue(3);
                        row[4] = reader.GetValue(4);
                        row[5] = reader.GetValue(5);
                        row[6] = reader.GetValue(6);
                        row[7] = reader.GetValue(7);
                        row[8] = reader.GetValue(8);
                        row[9] = reader.GetValue(9);
                        row[10] = reader.GetValue(10);
                        row[11] = reader.GetValue(11);
                        row[12] = reader.GetValue(12);
                        row[13] = reader.GetValue(13);
                        row[14] = reader.GetValue(14);
                        eventLogDT.Rows.Add(row);
                    }
                    reader.Close();
                }
            }
        }

        public DataTable CreateEventLogProcessedDT()
        {
            //Step 1 - Create new dt to store processed events (ProcessedEventLog)

            #region CreateTable
            DataTable eventLogProcessedDT = new DataTable();
            eventLogProcessedDT.Columns.Add("Vehicle Journey ID", typeof(Int32));
            eventLogProcessedDT.Columns.Add("Journey Pattern ID", typeof(string));
            eventLogProcessedDT.Columns.Add("Stop ID", typeof(Int32));
            eventLogProcessedDT.Columns.Add("Timestamp", typeof(DateTime));
            eventLogProcessedDT.Columns.Add("EventType", typeof(string));
            eventLogProcessedDT.Columns.Add("Station Log Index", typeof(Int32));
            eventLogProcessedDT.Columns.Add("JourneyIndex", typeof(Int32));
            #endregion
            return eventLogProcessedDT;
        }

        public void FillEventLogProcessedDT(DataTable journeyPatternsDT, DataTable eventLogDT,DataTable eventLogProcessedDT, string journeyPatternID) {

            
            try
            {

                DataTable journeyPatternEventLogDT = eventLogDT.AsEnumerable().Where(x => x.Field<string>("Journey Pattern ID") == journeyPatternID).CopyToDataTable();
                DataTable journeyPatternJourneyPatternsDT = journeyPatternsDT.AsEnumerable().Where(x => x.Field<string>("Journey Pattern ID") == journeyPatternID).CopyToDataTable();
           
                int eventLogCounter = 0;
                List<int> vehicleJourneyList = new List<int>();

                while(eventLogCounter < journeyPatternEventLogDT.Rows.Count)
                {
                    //var journeyPatternRow = journeyPatternsDT.Rows[i];
                    //if (journeyPatternID != journeyPatternRow[0].ToString())
                    //{
                    //    journeyPatternID = journeyPatternRow[0].ToString();
                    //    journeyPatternEventLogDT = eventLogDT.AsEnumerable().Where(x => x.Field<string>("Journey Pattern ID") == journeyPatternID).CopyToDataTable();
                    //    journeyPatternJourneyPatternsDT = journeyPatternsDT.AsEnumerable().Where(x => x.Field<string>("Journey Pattern ID") == journeyPatternID).CopyToDataTable();

                    //}
                    //int stopIndex = Convert.ToInt32(row[1]);
                    //int stopID = Convert.ToInt32(journeyPatternRow[2]);

                    //Step 3 - use help table from step 3 and store only relavant date about Vehicle Journey ID. For example: 14841
                    //Meaning - This new help table will store date for specific journey. In this example (00010001,14841)

                    var eventLogRow = journeyPatternEventLogDT.Rows[eventLogCounter];
                    int vehicleJourneyID = Convert.ToInt32(eventLogRow[5]);
                    if (!vehicleJourneyList.Contains(vehicleJourneyID))
                    {
                        var journeyEventLogDT = journeyPatternEventLogDT.AsEnumerable().Where(x => x.Field<int>("Vehicle Journey ID") == vehicleJourneyID).CopyToDataTable();
                        eventLogCounter = ProcessJourney(eventLogCounter, journeyPatternID, vehicleJourneyID, journeyPatternJourneyPatternsDT, journeyEventLogDT, eventLogProcessedDT);
                        //Console.WriteLine("Rows were processed for journey: {0}",  journeyPatternID);
                        vehicleJourneyList.Add(vehicleJourneyID);
                    }
                    else
                    {
                        eventLogCounter++;
                    }
                
                }
            }
            catch
            {
                Console.WriteLine("No rows were processed for journey: {0}", journeyPatternID);
                return;
            }
        }

        private int ProcessJourney(int eventLogCounter, string journeyPatternID,int vehicleJourneyID,DataTable journeyPatternJourneyPatternsDT, DataTable JourneyEventLogDT, DataTable eventLogProcessed)
        {

            DateTime unknownTimeStamp = new DateTime();
            int journeyIndex = 0;


            int JourneyPatternsCounter = 0;
            int JourneyEventLogCounter = 0;
            int stationLogIndex = 0;
            int eventLogStopID;
            int atStop = -1;
            bool resumed = false;
            bool active = false;
            bool visited = false;
            bool firstStation = true;

            int journeyPatternStopID = Convert.ToInt32(journeyPatternJourneyPatternsDT.Rows[JourneyPatternsCounter][JOURENT_PATTERN_STOP_COLUMN]);

            List<int> visitedStops = new List<int>();

            while (JourneyPatternsCounter < journeyPatternJourneyPatternsDT.Rows.Count && JourneyEventLogCounter < JourneyEventLogDT.Rows.Count)
            {
                journeyPatternStopID = Convert.ToInt32(journeyPatternJourneyPatternsDT.Rows[JourneyPatternsCounter][JOURENT_PATTERN_STOP_COLUMN]);

                eventLogStopID = Convert.ToInt32(JourneyEventLogDT.Rows[JourneyEventLogCounter][EVENT_LOG_STOP_COLUMN]);

                if (!visitedStops.Contains(eventLogStopID))
                {
                    if (firstStation)
                    {
                        AddEvent(eventLogProcessed, vehicleJourneyID, journeyPatternID, journeyPatternStopID, unknownTimeStamp, EventType.Start, stationLogIndex, journeyIndex);
                        journeyIndex++;
                        stationLogIndex++;
                    }
                    if (journeyPatternStopID != eventLogStopID)
                    {
                        
                        if (visited)
                        {
                            if (atStop == 0)
                            {
                                //Last event of stopID in event log was Active and Suspend event is needed to be added as well.

                                AddEvent(eventLogProcessed, vehicleJourneyID, journeyPatternID, journeyPatternStopID, unknownTimeStamp, EventType.Suspend, stationLogIndex, journeyIndex);
                                journeyIndex++;
                            }
                            resumed = false;
                            active = false;
                            visited = false;
                            JourneyPatternsCounter++;
                        }
                        else
                        {
                            if (firstStation)
                            {
                                firstStation = false;
                            }
                            else
                            {
                                stationLogIndex = 0;
                            }
                            AddEvent(eventLogProcessed, vehicleJourneyID, journeyPatternID, journeyPatternStopID, unknownTimeStamp, EventType.NotActive, stationLogIndex, journeyIndex);
                            journeyIndex++;
                            stationLogIndex++;
                            AddEvent(eventLogProcessed, vehicleJourneyID, journeyPatternID, journeyPatternStopID, unknownTimeStamp, EventType.Resume, stationLogIndex, journeyIndex);
                            journeyIndex++;
                            stationLogIndex++;
                            AddEvent(eventLogProcessed, vehicleJourneyID, journeyPatternID, journeyPatternStopID, unknownTimeStamp, EventType.Active, stationLogIndex, journeyIndex);
                            journeyIndex++;
                            stationLogIndex++;
                            AddEvent(eventLogProcessed, vehicleJourneyID, journeyPatternID, journeyPatternStopID, unknownTimeStamp, EventType.Suspend, stationLogIndex, journeyIndex);
                            journeyIndex++;
                            stationLogIndex++;

                            stationLogIndex = 0;
                            JourneyPatternsCounter++;
                            resumed = false;
                            active = false;
                            visited = false;
                        }
                    }
                    else
                    {
                        if (!visited)
                        {
                            if (firstStation)
                            {
                                firstStation = false;
                            }
                            else
                            {
                                stationLogIndex = 0;
                            }
                            AddEvent(eventLogProcessed, vehicleJourneyID, journeyPatternID, journeyPatternStopID, unknownTimeStamp, EventType.NotActive, stationLogIndex, journeyIndex);
                            journeyIndex++;
                            stationLogIndex++;
                            AddEvent(eventLogProcessed, vehicleJourneyID, journeyPatternID, journeyPatternStopID, unknownTimeStamp, EventType.Resume, stationLogIndex, journeyIndex);
                            journeyIndex++;
                            stationLogIndex++;
                            resumed = true;
                        }

                        atStop = Convert.ToInt32(JourneyEventLogDT.Rows[JourneyEventLogCounter][AT_STOP_COLUMN]);

                        if (atStop == 0)
                        {
                            DateTime timestamp = (DateTime)(JourneyEventLogDT.Rows[JourneyEventLogCounter][TIMESTAMP_COLUMN]);

                            AddEvent(eventLogProcessed, vehicleJourneyID, journeyPatternID, journeyPatternStopID, timestamp, EventType.Active, stationLogIndex, journeyIndex);

                            resumed = false;
                            active = true;
                        }
                        else if (atStop == 1)
                        {
                            if (resumed && !active)
                            {
                                //If station in event log get only this value (at stop = 1) then we don't know when it was active.
                                AddEvent(eventLogProcessed, vehicleJourneyID, journeyPatternID, journeyPatternStopID, unknownTimeStamp, EventType.Active, stationLogIndex, journeyIndex);

                                stationLogIndex++;
                                journeyIndex++;
                            }
                            DateTime timestamp = (DateTime)(JourneyEventLogDT.Rows[JourneyEventLogCounter][TIMESTAMP_COLUMN]);

                            AddEvent(eventLogProcessed, vehicleJourneyID, journeyPatternID, journeyPatternStopID, timestamp, EventType.Suspend, stationLogIndex, journeyIndex);

                            resumed = false;
                            active = false;
                        }
                        stationLogIndex++;
                        journeyIndex++;
                        visited = true;
                        if ((JourneyEventLogCounter + 1 < JourneyEventLogDT.Rows.Count) &&
                        (eventLogStopID != Convert.ToInt32(JourneyEventLogDT.Rows[JourneyEventLogCounter + 1][EVENT_LOG_STOP_COLUMN])))
                        {
                            visitedStops.Add(eventLogStopID);
                        }
                        JourneyEventLogCounter++;
                    }
                }
                else //!visitedStops.Contains(eventLogStopID)
                {
                    JourneyEventLogCounter++;
                    visited = true;
                }

            }//End While

            atStop = Convert.ToInt32(JourneyEventLogDT.Rows[JourneyEventLogCounter - 1][AT_STOP_COLUMN]);
            eventLogStopID = Convert.ToInt32(JourneyEventLogDT.Rows[JourneyEventLogCounter - 1][EVENT_LOG_STOP_COLUMN]);
            if ((JourneyEventLogCounter == JourneyEventLogDT.Rows.Count) && (atStop == 0))
            {
                AddEvent(eventLogProcessed, vehicleJourneyID, journeyPatternID, eventLogStopID, unknownTimeStamp, EventType.Suspend, stationLogIndex, journeyIndex);
                journeyIndex++;
                stationLogIndex++;
            }
            
            while (JourneyPatternsCounter < journeyPatternJourneyPatternsDT.Rows.Count)
            {
                //In case when there is no more station in eventlog and there is more in jp
                if (!visited)
                {
                    stationLogIndex = 0;
                    journeyPatternStopID = Convert.ToInt32(journeyPatternJourneyPatternsDT.Rows[JourneyPatternsCounter][JOURENT_PATTERN_STOP_COLUMN]);
                    AddEvent(eventLogProcessed, vehicleJourneyID, journeyPatternID, journeyPatternStopID, unknownTimeStamp, EventType.NotActive, stationLogIndex, journeyIndex);
                    journeyIndex++;
                    stationLogIndex++;
                    AddEvent(eventLogProcessed, vehicleJourneyID, journeyPatternID, journeyPatternStopID, unknownTimeStamp, EventType.Resume, stationLogIndex, journeyIndex);
                    journeyIndex++;
                    stationLogIndex++;
                    AddEvent(eventLogProcessed, vehicleJourneyID, journeyPatternID, journeyPatternStopID, unknownTimeStamp, EventType.Active, stationLogIndex, journeyIndex);
                    journeyIndex++;
                    stationLogIndex++;
                    AddEvent(eventLogProcessed, vehicleJourneyID, journeyPatternID, journeyPatternStopID, unknownTimeStamp, EventType.Suspend, stationLogIndex, journeyIndex);
                    journeyIndex++;
                    stationLogIndex++;
                }
                visited = false;
                JourneyPatternsCounter++;
            }

            AddEvent(eventLogProcessed, vehicleJourneyID, journeyPatternID, journeyPatternStopID, unknownTimeStamp, EventType.End, stationLogIndex, journeyIndex);

            eventLogCounter += JourneyEventLogCounter;
            return eventLogCounter;
        }


        private void AddEvent(DataTable eventLogProcessed, int vehicleJourneyID, string journeyPatternID, int stopID, DateTime timestamp,EventType eventype
            ,int journeyIndex,int stationLogIndex)
        {
            var row = eventLogProcessed.NewRow();

            row[0] = vehicleJourneyID;
            row[1] = journeyPatternID;
            if (stopID != NO_DATA)
            {
                row[2] = stopID; // Stop ID
            }
            else
            {
                row[2] = DBNull.Value;
            }
            if (timestamp != new DateTime())
            {
                row[3] = timestamp;
            }
            else
            {
                row[3] = DBNull.Value;
            }
            row[4] = eventype;
            if (journeyIndex != NO_DATA)
            {
                row[5] = journeyIndex; // Stop ID
            }
            else
            {
                row[5] = DBNull.Value;
            }
            if (stationLogIndex != NO_DATA)
            {
                row[6] = stationLogIndex; // Stop ID
            }
            else
            {
                row[6] = DBNull.Value;
            }
            eventLogProcessed.Rows.Add(row);
        }

        public List<Tuple<string, int, List<int>>> GetJourneyWithStop()
        {
            List<Tuple<string, int, List<int>>> journeyWithStops = new List<Tuple<string, int, List<int>>>() { };

            var sqlQuery = @"select DISTINCT [Journey Pattern ID],[Vehicle Journey ID],[Stop ID] from [ProbeEvents].[dbo].[EventLog]
            order by [Journey Pattern ID],[Vehicle Journey ID]";

            using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["LogConnection"].ConnectionString))
            {
                //Open connection
                conn.Open();

                SqlCommand cmd = new SqlCommand(sqlQuery, conn);
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    string journeyPatternID = reader.GetString(0);
                    int vehicleJourneyID = reader.GetInt32(1);
                    List<int> stopList = new List<int>() { };
                    int stopID = reader.GetInt32(2);
                    stopList.Add(stopID);

                    while (reader.Read())
                    {
                        if ((journeyPatternID == reader.GetString(0)) && (vehicleJourneyID == reader.GetInt32(1)))
                        {
                            stopID = reader.GetInt32(2);
                            stopList.Add(stopID);
                        }
                        else
                        {
                            break;
                        }
                    }
                    var tuple = new Tuple<string, int, List<int>>(journeyPatternID, vehicleJourneyID, stopList);
                    journeyWithStops.Add(tuple);
                }
            }
                    return journeyWithStops;
        }
        private string MakeJourneyListAsParameter(List<string> journeyList)
        {
            string journeyListParameter = "('";
            bool flag = true;
            foreach (var journey in journeyList)
            {
                if (!flag)
                {
                    journeyListParameter = journeyListParameter + "', '" + journey;
                }
                else
                {
                    journeyListParameter = journeyListParameter + journey;
                    flag = false;
                }
            }
            journeyListParameter = journeyListParameter + "')";
            return journeyListParameter;
        }

    }
}

