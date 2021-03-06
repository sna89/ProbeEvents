﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UncertainEventStreams.Entities;
using UncertainEventStreams.Events;
using UncertainEventStreams.Events.IntervalBased;
using UncertainEventStreams.Inference;

namespace UncertainEventStreams.Preprocessing
{
    public class EventLogStore
    {
        private StoreHelper _helper;

        public EventLogStore()
        {
            _helper = new StoreHelper();
        }

        #region Public methods

        public Dictionary<int, List<BusDataItem>> GetLog(string journeyPatternId, int vehicleJourneyId)
        {
            var logs = _helper.GetReader<BusDataItem>("SELECT * FROM EventLog WHERE [Journey Pattern ID]=@JourneyPatternId AND [Vehicle Journey ID]=@VehicleJourneyId AND [Stop ID] IS NOT NULL",
                r => new BusDataItem()
                {
                    AtStop = (bool)r["At Stop"],
                    JourneyPatternId = (string)r["Journey Pattern Id"],
                    VehicleJourneyId = (int)r["Vehicle Journey ID"],
                    LineId = (int)r["Line ID"],
                    StopId = (int)r["Stop ID"],
                    Timestamp = (DateTime)r["Timestamp"]
                },
                new Dictionary<string, object>()
                {
                    { "@JourneyPatternId", journeyPatternId},
                    { "@VehicleJourneyId", vehicleJourneyId}
                }
                );

            return logs.GroupBy(x=>x.StopId).ToDictionary(x=>x.Key, y=>y.ToList());
        }

        public IEnumerable<Tuple<string, int>> GetUniqueJourneys()
        {
            var journey = _helper.GetReader<Tuple<string, int>>("SELECT DISTINCT [Journey Pattern ID], [Vehicle Journey ID] FROM EventLog",
                r => new Tuple<string, int>(GetValue<string>(r, "Journey Pattern ID"), (int)r["Vehicle Journey ID"]));

            return journey;
        }

        public SegmentedIntervalEvent GetProcessedLog(JourneyKey key, PointEvent.PointType type)
        {
            char[] trim = new char[] { '-', '1', '2', '3', '4', '5' };
            var sql = @"SELECT [Vehicle Journey ID], [Journey Pattern ID], [Stop ID], [Timestamp], EventType, [Station Log Index], CAST(JourneyIndex AS INT) AS JourneyIndex
                FROM EventLogProcessed WHERE [Journey Pattern ID]=@JourneyPatternId AND [Vehicle Journey ID]=@VehicleJourneyId AND [EventType] <> '1-NotActive'";
            var logs = _helper.GetReader<PointEvent>(sql,
                x =>
                {
                    return new PointEvent((EventType)Enum.Parse(typeof(EventType), ((string)x["EventType"]).Trim(trim), true),
                                          GetValue<DateTime?>(x, "Timestamp"),
                                          GetValue<int>(x, "JourneyIndex"), type)
                    {
                        IntervalId = GetValue<int>(x, "Stop ID")
                    };
                }
                ,
                new Dictionary<string, object>()
                {
                    { "@JourneyPatternId", key.JourneyPatternId},
                    { "@VehicleJourneyId", key.VehicleJourneyId}
                });

            var intervalEvents = logs
                .GroupBy(x => x.IntervalId)
                .OrderBy(x=>x.Min(y=>y.EventIndex))
                .Select((x, index)=> new BasicIntervalEvent(x, x.Key, index));

            var journeyEvent = new SegmentedIntervalEvent(intervalEvents, key.ToString());

            return journeyEvent;
        }
        public IEnumerable<ComparisonInfo> JourneysToCompare()
        {
            List<Tuple<JourneyKey, List<int>>> journeyWithStops = new List<Tuple<JourneyKey, List<int>>>() { };

            var sqlQuery = @"select DISTINCT [Journey Pattern ID],[Vehicle Journey ID],[Stop ID] from [ProbeEvents].[dbo].[EventLogProcessed]
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
                    JourneyKey journey = new JourneyKey(journeyPatternID, vehicleJourneyID);
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
                    var tuple = new Tuple<JourneyKey, List<int>>(journey, stopList);
                    journeyWithStops.Add(tuple);
                }
            }
            var journiesToCompare = new List<ComparisonInfo>();
            List<JourneyKey> checkedJournies = new List<JourneyKey>();
            foreach(var journeyWithStop1 in journeyWithStops)
            {
                var journey1 = journeyWithStop1.Item1;
                foreach (var journeyWithStop2 in journeyWithStops)
                {
                    var journey2 = journeyWithStop2.Item1;
                    if (!journey1.IsEqual(journey2) && !checkedJournies.Contains(journey1) && !checkedJournies.Contains(journey2))
                    {
                        var stopListJourney1 = journeyWithStop1.Item2;
                        var stopListJourney2 = journeyWithStop2.Item2;
                        var mutualStopList = stopListJourney1.Intersect(stopListJourney2).ToList();
                        foreach (var stop in mutualStopList)
                        { 
                            var compInfo = new ComparisonInfo(new JourneyKey(journey1.JourneyPatternId, journey1.VehicleJourneyId),
                            new JourneyKey(journey2.JourneyPatternId, journey2.VehicleJourneyId), stop);
                            journiesToCompare.Add(compInfo);
                        }
                    }
                }
                checkedJournies.Add(journey1);
            }
            return journiesToCompare;
            //yield return new ComparisonInfo(new JourneyKey("00411001", 13065), new JourneyKey("00010001", 14858), 52);
        }

        public IEnumerable<ComparisonInfo> OLDJourneysToCompare()
        {
            var sql = "select * from dbo.OverlappingStops ";
            var sql2 = @"SELECT *
FROM (
SELECT *,
KnowledgeLevel = CASE WHEN [FirstKnownRatio X SecondKnownRatio] < 0.3 THEN 'LOW' WHEN [FirstKnownRatio X SecondKnownRatio] < 0.6 THEN 'MEDIUM' ELSE 'GOOD' END,
[OverlapMeasure] = CASE WHEN MaxOverlapInSeconds  > 1000 THEN 'NOT RELIABLE' WHEN MaxOverlapInSeconds  < 30 THEN 'LOW' ELSE 'MEDIUM' END
FROM (
SELECT *,CAST([First Vehicle Journey ID] AS VARCHAR) + ':'+ [First Journey Pattern ID] + '-' +
       CAST([Second Vehicle Journey ID] AS VARCHAR) + ':' + [Second Journey Pattern ID] PairwiseJourneyKey,
       FirstKnownRatio =  CAST([FirstTotalKnownEvents] AS FLOAT) /[FirstTotalEvents], 
          SecondKnownRatio =  CAST([SecondTotalKnownEvents] AS FLOAT) /[SecondTotalEvents], 
          (CAST([FirstTotalKnownEvents] AS FLOAT) /[FirstTotalEvents]) * (CAST([SecondTotalKnownEvents] AS FLOAT) /[SecondTotalEvents]) [FirstKnownRatio X SecondKnownRatio]
FROM dbo.OverlappingJourneyStops
)A) B
WHERE KnowledgeLevel in ('GOOD') and OverlapMeasure in('MEDIUM','LOW')";

            yield return new ComparisonInfo(new JourneyKey("00411001", 13065), new JourneyKey("00010001", 14858), 52);

            var journey = _helper.GetReader<ComparisonInfo>(sql,
                r => new ComparisonInfo(new JourneyKey(GetValue<string>(r, "First Journey Pattern ID"), (int)r["First Vehicle Journey ID"]),
                     new JourneyKey(GetValue<string>(r, "Second Journey Pattern ID"), (int)r["Second Vehicle Journey ID"]), (int)r["Stop ID"]));

            //return journey;
        }

        public void WriteResult(JourneyKey firstJourney, JourneyKey secondJourney, int stopId, RunResult result)
        {
            var cmd = @"
DELETE FROM ProbeEvents.dbo.JourneyOverlapResults
WHERE [Stop ID] = @StopID AND[First Vehicle Journey ID] = @FirstVehicleJourneyID AND[First Journey Pattern ID] = @FirstJourneyPatternID AND[Second Vehicle Journey ID] = @SecondVehicleJourneyID AND[Second Journey Pattern ID] = @SecondJourneyPatternID

 INSERT INTO ProbeEvents.dbo.JourneyOverlapResults
([Stop ID], [First Vehicle Journey ID], [First Journey Pattern ID], [Second Vehicle Journey ID], [Second Journey Pattern ID], NumberOfMissingEvents, Probability, [RunTimeDuration(ms)],[Pruned]) 
VALUES(@StopID, @FirstVehicleJourneyID, @FirstJourneyPatternID, @SecondVehicleJourneyID, @SecondJourneyPatternID, @NumberOfMissingEvents, @Probability, @RunTime, @Pruned)";

            var parameters = new Dictionary<string, object>()
            {
                { "@StopID", stopId },
                { "@FirstVehicleJourneyID", firstJourney.VehicleJourneyId },
                { "@FirstJourneyPatternID", firstJourney.JourneyPatternId },
                { "@SecondVehicleJourneyID", secondJourney.VehicleJourneyId },
                { "@SecondJourneyPatternID", secondJourney.JourneyPatternId },
                { "@NumberOfMissingEvents", result.MissingEvents },
                { "@Probability", result.Probability },
                { "@RunTime", result.Runtime.TotalMilliseconds },
                { "@Pruned", result.Pruned }
            };


            _helper.ExecuteNonQuery(cmd, parameters);
        }

        #endregion

        #region Private methods

        private T GetValue<T>(IDataRecord record, string name)
        {
            var val = record[name];
            if (val == DBNull.Value)
            {
                return default(T);
            }
            return (T)val;
        }

        #endregion
    }
}
