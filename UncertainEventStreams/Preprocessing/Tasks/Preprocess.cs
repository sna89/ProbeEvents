using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UncertainEventStreams.Entities;
using UncertainEventStreams.Events;

namespace UncertainEventStreams.Preprocessing.Tasks
{
    public class PreprocessTask : AbstractTask
    {
        private EventLogStore _log;
        private JourneyPatternsStore _journeys;

        public override string Name { get { return "Preprocess"; } }

        public PreprocessTask()
        {
            _log = new EventLogStore();
            _journeys = new JourneyPatternsStore();
        }

        protected override void RunSpecific()
        {
            var helper = new StoreHelper(); 
            List<string> journeyPatternList = new List<string>();
            var journeyPatternsDT = helper.FillJourneyPatternsDT(journeyPatternList);
            Console.WriteLine("{0} rows processed in journeyPatternsDT", journeyPatternsDT.Rows.Count);

            var eventLogDT = helper.CreateEventLog();
            
            foreach (var journeyPattern in journeyPatternList)
            {
                helper.AddJourenyToEventLogDT(eventLogDT, journeyPattern);
            }
            Console.WriteLine("{0} rows processed in eventLogDT", eventLogDT.Rows.Count);

            List<JourneyKey> journeyList = new List<JourneyKey>();
            FillJourneyList(journeyList);

            var EventLogProcessedDT = helper.CreateEventLogProcessedDT();
            foreach (var journeyPattern in journeyPatternList)
            {
                helper.FillEventLogProcessedDT(journeyPatternsDT, eventLogDT, EventLogProcessedDT, journeyPattern);
            }
            Console.WriteLine("{0} rows processed in EventLogProcessedDT", EventLogProcessedDT.Rows.Count);


            var destinationTableName = "EventLogProcessed";
            
            using (SqlBulkCopy bulkCopy = new SqlBulkCopy(ConfigurationManager.ConnectionStrings["LogConnection"].ConnectionString))
            {
                bulkCopy.DestinationTableName = destinationTableName;
                bulkCopy.BatchSize = 50;
                bulkCopy.WriteToServer(EventLogProcessedDT);
            }

            helper.DeleteJourniesWithMissingStops(EventLogProcessedDT, journeyList);

            //foreach (var journey in _log.GetUniqueJourneys())
            //{
            //    try
            //    {
            //        WriteEvents(journey.Item1, journey.Item2);
            //    }
            //    catch (Exception ex)
            //    {
            //        Console.WriteLine("Failed to process, JourneyPatternId: {0}, VehicleJourneyId: {1}, ex: {2}",
            //            journey.Item1, journey.Item2, ex);
            //    }

            //}
        }


        private void FillJourneyList(List<JourneyKey> journeyList)
        {
            var sqlQuery = @"select distinct [Journey Pattern ID],[Vehicle Journey ID]
from [ProbeEvents].[dbo].[EventLog]
order by [Journey Pattern ID],[Vehicle Journey ID]";

            using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["LogConnection"].ConnectionString))
            {
                conn.Open();
                using (SqlCommand command = new SqlCommand(sqlQuery, conn))
                {
                    SqlDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        var journeyPatternId = Convert.ToString(reader.GetValue(0));
                        var vehicleJourneyId = Convert.ToInt32(reader.GetValue(1));
                        JourneyKey journey = new JourneyKey(journeyPatternId, vehicleJourneyId);
                        journeyList.Add(journey);
                    }
                }
            }
        }

    private void WriteEvents(string journeyPatternId, int vehicleJourneyId)
        {
            #region SQL

            var sql = @"
--DELETE FROM dbo.EventLogProcessed WHERE [Journey Pattern ID]=@JourneyPatternId AND  [Vehicle Journey ID]=@VehicleJourneyId;

WITH DS as
(
SELECT [Vehicle Journey ID],A.[Journey Pattern ID], A.[Stop ID], A.[Timestamp], CASE WHEN [At Stop]=0 THEN '3-Active' ELSE '1-NotActive' END AS [EventType], 
[Station Log Index] = row_number() over (partition by A.[Journey Pattern ID], [Vehicle Journey ID], [Stop ID] order by [Timestamp] asc)
FROM EventLog A 
WHERE A.[Journey Pattern ID]=@JourneyPatternId AND  [Vehicle Journey ID]=@VehicleJourneyId
), BOUND AS
(

SELECT [Vehicle Journey ID], [Journey Pattern ID], [Stop ID], NULL AS Timestamp,'2-Resume' AS [EventType], MAX([Station Log Index]) + 1 AS [Station Log Index]--,MAX([Event Index]) + 0.4 as [Event Index]
FROM DS A
WHERE [Stop ID] NOT IN(SELECT TOP 1 [Stop ID] FROM JourneyPatterns WHERE [Id]=A.[Journey Pattern ID] ORDER BY [Stop Index] DESC)
GROUP BY [Vehicle Journey ID], [Journey Pattern ID], [Stop ID]
UNION ALL
SELECT [Vehicle Journey ID], [Journey Pattern ID], [Stop ID], NULL AS Timestamp, '4-Suspend' AS [EventType], MIN([Station Log Index]) -1 AS [Station Log Index]--,MIN([Event Index]) - 0.4 as [Event Index]
FROM DS A
WHERE [Stop ID] NOT IN(SELECT TOP 1 [Stop ID] FROM JourneyPatterns WHERE [Id]=A.[Journey Pattern ID] ORDER BY [Stop Index] DESC)
GROUP BY [Vehicle Journey ID], [Journey Pattern ID], [Stop ID]
),
EX AS(
SELECT *
FROM BOUND
UNION ALL 
SELECT * 
FROM DS

UNION ALL 
SELECT @VehicleJourneyId AS [Vehicle Journey ID], A.[Id],  A.[Stop ID], NULL AS Timestamp, '2-Resume' AS [EventType],[Station Log Index] = 1
FROM JourneyPatterns A 
WHERE A.[Id]=@JourneyPatternId AND A.[Stop ID] NOT IN (
SELECT DISTINCT [Stop ID]
FROM EventLog
WHERE [Journey Pattern ID]=@JourneyPatternId AND  [Vehicle Journey ID]=@VehicleJourneyId)
UNION ALL
SELECT @VehicleJourneyId AS [Vehicle Journey ID], A.[Id],  A.[Stop ID], NULL AS Timestamp, '4-Suspend' AS [EventType],[Station Log Index] = 2
FROM JourneyPatterns A 
WHERE A.[Id]=@JourneyPatternId AND A.[Stop ID] NOT IN (
SELECT DISTINCT [Stop ID]
FROM EventLog
WHERE [Journey Pattern ID]=@JourneyPatternId AND  [Vehicle Journey ID]=@VehicleJourneyId)
)

INSERT INTO dbo.EventLogProcessed
SELECT A.*, JourneyIndex = row_number() over (order by B.[Stop Index],[EventType])
FROM EX A LEFT JOIN JourneyPatterns B ON A.[Journey Pattern ID]=B.[Id] AND A.[Stop ID] = B.[Stop ID]
ORDER BY B.[Stop Index],[EventType]";
            #endregion

            var helper = new StoreHelper();
            var parameters = new Dictionary<string, object>();
            parameters.Add("@VehicleJourneyId", vehicleJourneyId);
            parameters.Add("@JourneyPatternId", journeyPatternId);
            var rows = helper.ExecuteNonQuery(sql, parameters);

            Console.WriteLine("{0} rows processed", rows);
        }
    }
}
