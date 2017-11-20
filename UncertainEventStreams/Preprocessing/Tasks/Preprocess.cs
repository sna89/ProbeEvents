using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            //STEP 1:
            //Get all (or relavant) journey pattern details ([Journey Pattern ID],[Stop Index],[Stop ID])
            //ordered by [Journey Pattern ID] (ASC) from JourneyPatterns table
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
              [Journey Pattern ID] in (select distinct top 1 [Journey Pattern ID] from [ProbeEvents].[dbo].[EventLog] order by [Journey Pattern ID])
              order by [Journey Pattern ID],[Stop Index]
              ";
            var helper = new StoreHelper(); 
            List<string> journeyList = new List<string>();
            var journeyPatternsDT = helper.FillJourneyPatternsDT(journeyPatternsQuery, journeyList);
            Console.WriteLine("{0} rows processed", journeyPatternsDT.Rows.Count);
            var eventLogDT = helper.FillEventLogDT(journeyList,true);
            Console.WriteLine("{0} rows processed", eventLogDT.Rows.Count);
            var EventLogProcessed = helper.CreateEventLogProcessedDT(journeyPatternsDT, eventLogDT);












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
