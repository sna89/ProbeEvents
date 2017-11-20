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
        private List<string> EVENT_TYPES = new List<string>() { "0 - Start", "1 - NotActive", "2 - Resume", "3 - Active", "4 - Suspend", "5 - End" };
        enum EventType {
            Start,
            NotAcive,
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

        public DataTable FillJourneyPatternsDT(string query, List<string> JourneyList)
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("Journey Pattern ID", typeof(string));
            dt.Columns.Add("Stop Index", typeof(string));
            dt.Columns.Add("Stop ID", typeof(string));
            using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["LogConnection"].ConnectionString))
            {
                //Open connection
                conn.Open();

                SqlCommand cmd = new SqlCommand(query, conn);
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
            return dt;
        }

        public DataTable FillEventLogDT(List<String> journeyList, bool test)
        {
            DataTable dt = new DataTable();
            var FileImport = new FileImport();
            var columns = FileImport.GetEventLogColumns();
            foreach (var col in columns)
            {
                dt.Columns.Add(col.Key, col.Value);
            }
            string sqlQuery = @"select * from [ProbeEvents].[dbo].[EventLog]";
            using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["LogConnection"].ConnectionString))
            {
                conn.Open();
                if (test)
                {
                    sqlQuery = "select * from[ProbeEvents].[dbo].[EventLog] where [Journey Pattern ID] = @journeyListID order by [Vehicle Journey ID],[Timestamp]";
                }
                using (SqlCommand command = new SqlCommand(sqlQuery, conn))
                {
                    //string journeyListParameter = MakeJourneyListAsParameter(journeyList);
                    SqlParameter param = new SqlParameter();
                    param.ParameterName = "@journeyListID";
                    param.Value = journeyList[0];
                    command.Parameters.Add(param);
                    SqlDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        var row = dt.NewRow();
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
                        dt.Rows.Add(row);
                    }
                }
            }
            return dt;
        }

        public DataTable CreateEventLogProcessedDT(DataTable journeyPatternsDT,DataTable eventLogDT)
        {
            //Step 1 - Create new dt to store processed events (ProcessedEventLog)
            DataTable eventLogProcessed = new DataTable();
            eventLogProcessed.Columns.Add("Vehicle Journey ID", typeof(Int32));
            eventLogProcessed.Columns.Add("Journey Pattern ID", typeof(string));
            eventLogProcessed.Columns.Add("Stop ID", typeof(Int32));
            eventLogProcessed.Columns.Add("Timestamp", typeof(DateTime));
            eventLogProcessed.Columns.Add("EventType", typeof(string));
            eventLogProcessed.Columns.Add("Station Log Index", typeof(Int32));
            eventLogProcessed.Columns.Add("JourneyIndex", typeof(Int32));

            //Step 2 - Create help table which store only relavant data about Journey Pattern ID at a time. For example: 00010001

            string journeyPatternID = journeyPatternsDT.Rows[0][0].ToString();
            DataTable journeyPatternEventLogDT = eventLogDT.AsEnumerable().Where(x => x.Field<string>("Journey Pattern ID") == journeyPatternID).CopyToDataTable();
            DataTable journeyPatternJourneyPatternsDT = journeyPatternsDT.AsEnumerable().Where(x => x.Field<string>("Journey Pattern ID") == journeyPatternID).CopyToDataTable();
            int i = 0, j = 0;

            while(j < journeyPatternEventLogDT.Rows.Count)
            {
                var journeyPatternRow = journeyPatternsDT.Rows[i];
                if (journeyPatternID != journeyPatternRow[0].ToString())
                {
                    journeyPatternID = journeyPatternRow[0].ToString();
                    journeyPatternEventLogDT = eventLogDT.AsEnumerable().Where(x => x.Field<string>("Journey Pattern ID") == journeyPatternID).CopyToDataTable();
                    journeyPatternJourneyPatternsDT = journeyPatternsDT.AsEnumerable().Where(x => x.Field<string>("Journey Pattern ID") == journeyPatternID).CopyToDataTable();

                }
                //int stopIndex = Convert.ToInt32(row[1]);
                //int stopID = Convert.ToInt32(journeyPatternRow[2]);

                //Step 3 - use help table from step 3 and store only relavant date about Vehicle Journey ID. For example: 14841
                //Meaning - This new help table will store date for specific journey. In this example (00010001,14841)

                var eventLogRow = journeyPatternEventLogDT.Rows[j];
                int vehicleJourneyID = Convert.ToInt32(eventLogRow[5]);
                var JourneyEventLogDT = journeyPatternEventLogDT.AsEnumerable().Where(x => x.Field<int>("Vehicle Journey ID") == vehicleJourneyID).CopyToDataTable();

                //step 4  - Compare event log help table with Journey Pattern help table and Add processed data to ProcessedEventLog datatable.
                var row = eventLogProcessed.NewRow();
                DateTime emptyTimeStamp = new DateTime();
                EventType eventType;

                AddEvent(eventLogProcessed, row, vehicleJourneyID, journeyPatternID, -1, emptyTimeStamp,EventType.Start , -1, -1);

                int i1 = 0, i2 = 0;
                int journeyIndex = -1;
                int stationLogIndex = 0;
                
                int eventLogStopID;
                int atStop;
                bool resumed = false;
                bool active = false;
                bool visited = false;

                while (i1 < journeyPatternJourneyPatternsDT.Rows.Count && i2 < JourneyEventLogDT.Rows.Count)
                {
                    int journeyPatternStopID = Convert.ToInt32(journeyPatternJourneyPatternsDT.Rows[i1][2]);

                    eventLogStopID = Convert.ToInt32(JourneyEventLogDT.Rows[i2][13]);

                    if (journeyPatternStopID == eventLogStopID)
                    {
                            journeyIndex++;
                            if (!visited)
                            {
                                stationLogIndex = 0;
                                eventType = EventType.NotAcive;
                                row = eventLogProcessed.NewRow();

                                AddEvent(eventLogProcessed, row, vehicleJourneyID, journeyPatternID, journeyPatternStopID, emptyTimeStamp, eventType, stationLogIndex, journeyIndex);

                                journeyIndex++;
                                stationLogIndex++;

                                eventType = EventType.Resume;
                                row = eventLogProcessed.NewRow();

                                AddEvent(eventLogProcessed, row, vehicleJourneyID, journeyPatternID, journeyPatternStopID, emptyTimeStamp, eventType, stationLogIndex, journeyIndex);
                 
                                journeyIndex++;
                                stationLogIndex++;
                                resumed = true;
                                active = false;
                            }
                            atStop = Convert.ToInt32(JourneyEventLogDT.Rows[i2][14]);
                            if (atStop == 0)
                            {
                                eventType = EventType.Active;
                                row = eventLogProcessed.NewRow();
                                DateTime timestamp = (DateTime)(JourneyEventLogDT.Rows[i2][0]);

                                AddEvent(eventLogProcessed, row, vehicleJourneyID, journeyPatternID, journeyPatternStopID, timestamp, eventType, stationLogIndex, journeyIndex);

                                resumed = false;
                                active = true;
                            }
                            else if (atStop == 1)
                            {
                                if (resumed && !active)
                                {
                                    eventType = EventType.Active;
                                    row = eventLogProcessed.NewRow();

                                    AddEvent(eventLogProcessed, row, vehicleJourneyID, journeyPatternID, journeyPatternStopID, emptyTimeStamp, eventType, stationLogIndex, journeyIndex);

                                    stationLogIndex++;
                                    journeyIndex++;
                                }
                                eventType = EventType.Suspend;
                                row = eventLogProcessed.NewRow();
                                DateTime timestamp = (DateTime)(JourneyEventLogDT.Rows[i2][0]);
                                
                                AddEvent(eventLogProcessed, row, vehicleJourneyID, journeyPatternID, journeyPatternStopID, timestamp, eventType, stationLogIndex, journeyIndex);

                                resumed = false;
                                active = false;
                            }
                            stationLogIndex++;
                            i2++;
                            visited = true;
                        }
                        else
                        {
                            if (active)
                            {
                                journeyIndex++;
                                eventType = EventType.Suspend;
                                row = eventLogProcessed.NewRow();

                                AddEvent(eventLogProcessed, row, vehicleJourneyID, journeyPatternID, journeyPatternStopID, emptyTimeStamp, eventType, stationLogIndex, journeyIndex);
                                resumed = false;
                                active = false;
                            }
                            stationLogIndex = 0;
                            i1++;
                            visited = false;
                        }

                    }
                atStop = Convert.ToInt32(JourneyEventLogDT.Rows[i2-1][14]);
                eventLogStopID = Convert.ToInt32(JourneyEventLogDT.Rows[i2 - 1][13]);
                if ((i2 == JourneyEventLogDT.Rows.Count) && (atStop == 0))
                {
                    eventType = EventType.Suspend;
                    row = eventLogProcessed.NewRow();
                    AddEvent(eventLogProcessed, row, vehicleJourneyID, journeyPatternID, eventLogStopID, emptyTimeStamp, eventType, stationLogIndex, journeyIndex);

                }
                row = eventLogProcessed.NewRow();
                eventType = EventType.End;

                AddEvent(eventLogProcessed, row, vehicleJourneyID, journeyPatternID, -1, emptyTimeStamp, eventType, -1, -1);

                j += i2;
            }
            return eventLogProcessed;
        }

        private void AddEvent(DataTable eventLogProcessed, DataRow row, int vehicleJourneyID, string journeyPatternID, int stopID, DateTime timestamp,EventType eventype
            ,int journeyIndex,int stationLogIndex)
        {
            row[0] = vehicleJourneyID;
            row[1] = journeyPatternID;
            if (stopID != -1)
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
            if (journeyIndex != -1)
            {
                row[5] = journeyIndex; // Stop ID
            }
            else
            {
                row[5] = DBNull.Value;
            }
            if (stationLogIndex != -1)
            {
                row[6] = stationLogIndex; // Stop ID
            }
            else
            {
                row[6] = DBNull.Value;
            }
            eventLogProcessed.Rows.Add(row);
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

