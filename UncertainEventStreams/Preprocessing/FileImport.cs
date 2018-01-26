using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LumenWorks.Framework.IO.Csv;
using System.Data.SqlClient;
using System.Data.Odbc;
using System.Data;
using System.Configuration;
using System.Data.OleDb;
using System.Globalization;

namespace UncertainEventStreams.Preprocessing
{
    public class FileImport
    {
        private const string DATA_TABLE_NAME = "EventLog";
        private const string DEFAULT_PATH = @"C:\Users\noamarbe\Desktop\YossiProject\siri";
        private const string EXTRACTED_PATH = @"C:\Users\noamarbe\Desktop\YossiProject\extracted\";

        public void Load(string path = DEFAULT_PATH, int numFiles = int.MaxValue)
        {
            var di = new DirectoryInfo(EXTRACTED_PATH);
            foreach (FileInfo csvFile in di.GetFiles("*.csv").OrderBy(x => x.FullName).Take(numFiles))
            {
                //System.IO.File.Move(fileToDecompress.FullName, fileToDecompress.FullName.Replace("siri.", "siri_"));
                ImportCSV(csvFile.FullName);

                Console.WriteLine("Finished: {0}", csvFile.FullName);
            }
        }

        private void ImportCSV(string filePath, bool isFirstRowHeader = false)
        {
            #region Obtain file reader

            //string header = isFirstRowHeader ? "Yes" : "No";

            string pathOnly = Path.GetDirectoryName(filePath);
            string fileName = Path.GetFileName(filePath);

            //string sql = @"SELECT top 10000 * FROM [" + fileName + "]";

            DataTable dt = new DataTable();
            IEnumerable<KeyValuePair<string, Type>> Columns;
            if (fileName == "Journey_Pattern.csv")
            {
                Columns = GetJuorneyPatternsColumns();
            }
            else
            {
                Columns = GetEventLogColumns();
            }
            foreach (var col in Columns)
            {
                var colName = col.Key;
                var colType = col.Value;
                dt.Columns.Add(colName, colType);
            }

            dt = FillDataTable(dt, fileName, isFirstRowHeader);

            #endregion

            #region Process data to new data reader
            DataTable dtProcessed = ObtainTableCopy(DATA_TABLE_NAME);
            if (!(fileName == "Journey_Pattern.csv"))
            {

                using (var reader = dt.CreateDataReader())
                {
                    while (reader.Read())
                    {
                        var row = dtProcessed.NewRow();
                        row["Timestamp"] = DateTimeOffset.FromUnixTimeMilliseconds(Convert.ToInt64(reader["Timestamp"]) / 1000).DateTime;
                        row["Line ID"] = reader["Line ID"];
                        row["Direction"] = reader["Direction"];
                        row["Journey Pattern ID"] = (reader["Journey Pattern ID"] as string) == "null" ? null : reader["Journey Pattern ID"];
                        row["Timeframe"] = reader["Timeframe"];
                        row["Vehicle Journey ID"] = reader["Vehicle Journey ID"];
                        row["Operator"] = reader["Operator"];
                        row["Congestion"] = reader["Congestion"];
                        row["Lon WGS84"] = reader["Lon WGS84"];
                        row["Lat WGS84"] = reader["Lat WGS84"];
                        row["Delay"] = reader["Delay"];
                        row["Block ID"] = reader["Block ID"];
                        row["Vehicle ID"] = reader["Vehicle ID"];

                        var stopId = reader["Stop ID"].ToString();

                        if (string.IsNullOrEmpty(stopId) || stopId == "null")
                        {
                            row["Stop ID"] = DBNull.Value;
                        }
                        else
                        {
                            row["Stop ID"] = int.Parse(stopId);
                        }

                        var atStop = (string)reader["At Stop"].ToString();
                        row["At Stop"] = atStop == "0" ? false : true;

                        dtProcessed.Rows.Add(row);
                    }
                }
            }
            else
            {
                dtProcessed = dt;
            }
            #endregion

            #region Bulk insert processed data
            string DataTableName;
            if (!(fileName == "Journey_Pattern.csv")) { 
                DataTableName = DATA_TABLE_NAME;
            }
            else
            {
                DataTableName = "JourneyPatterns";
            }
            using (SqlBulkCopy bulkCopy = new SqlBulkCopy(ConfigurationManager.ConnectionStrings["LogConnection"].ConnectionString))
            {
                bulkCopy.DestinationTableName = DataTableName;
                bulkCopy.BatchSize = 50;
                bulkCopy.WriteToServer(dtProcessed);
            }
            #endregion
        }


        public IEnumerable<KeyValuePair<string, Type>> GetEventLogColumns()
        {
            IEnumerable<KeyValuePair<string, Type>> SOURCE_COLUMNS = new[]
            {
                new KeyValuePair<string, Type>("Timestamp", typeof(DateTime)),
                new KeyValuePair<string, Type>("Line ID", typeof(String)),
                new KeyValuePair<string, Type>("Direction", typeof(int)),
                new KeyValuePair<string, Type>("Journey Pattern ID", typeof(String)),
                new KeyValuePair<string, Type>("Timeframe", typeof(string)),
                new KeyValuePair<string, Type>("Vehicle Journey ID", typeof(int)),
                new KeyValuePair<string, Type>("Operator", typeof(String)),
                new KeyValuePair<string, Type>("Congestion", typeof(int)),
                new KeyValuePair<string, Type>("Lon WGS84", typeof(decimal)),
                new KeyValuePair<string, Type>("Lat WGS84", typeof(decimal)),
                new KeyValuePair<string, Type>("Delay", typeof(int)),
                new KeyValuePair<string, Type>("Block ID", typeof(int)),
                new KeyValuePair<string, Type>("Vehicle ID", typeof(int)),
                new KeyValuePair<string, Type>("Stop ID", typeof(String)),
                new KeyValuePair<string, Type>("At Stop", typeof(Int32))
            };
            return SOURCE_COLUMNS;
        }

        public IEnumerable<KeyValuePair<string, Type>> GetJuorneyPatternsColumns()
        {
            IEnumerable<KeyValuePair<string, Type>> SOURCE_COLUMNS = new[]
            {
                new KeyValuePair<string, Type>("Journey Pattern ID", typeof(String)),
                new KeyValuePair<string, Type>("Stop Index", typeof(Int32)),
                new KeyValuePair<string, Type>("A", typeof(Int32)),
                new KeyValuePair<string, Type>("Stop ID", typeof(Int32)),
                new KeyValuePair<string, Type>("B", typeof(Int32)),
                new KeyValuePair<string, Type>("C", typeof(Int32))
            };
            return SOURCE_COLUMNS;
    }

        public DataTable FillDataTable(DataTable dt,string fileName,bool isFirstRowHeader)
        {
            using (CsvReader csv = new CsvReader(new StreamReader(EXTRACTED_PATH + fileName), isFirstRowHeader))
            {
                
                while (csv.ReadNextRecord())
                {
                    DataRow row = dt.NewRow();
                    for (int i = 0; i < dt.Columns.Count; i++)
                    {
                        try
                        {
                            row[i] = csv[i];
                        }
                        catch
                        {
                            row[i] = DBNull.Value;
                        }
                    }
                    dt.Rows.Add(row);
                }
            }
            return dt;
        }
        public IEnumerable<string> ObtainColumns(string tableName)
        {
            using (var connection = new SqlConnection(ConfigurationManager.ConnectionStrings["LogConnection"].ConnectionString))
            {
                connection.Open();

                var command = connection.CreateCommand();
                command.CommandText = string.Format("SELECT [name] FROM sys.columns WHERE object_id=object_id('{0}')", tableName);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        yield return (string)reader["name"];
                    }
                }
            }
        }

        public DataTable ObtainTableCopy(string tableName)
        {
            using (SqlConnection sqlConn = new SqlConnection(ConfigurationManager.ConnectionStrings["LogConnection"].ConnectionString))
            using (SqlCommand cmd = new SqlCommand(string.Format("SELECT TOP 0 * FROM {0}", tableName), sqlConn))
            {
                sqlConn.Open();
                DataTable dt = new DataTable();
                dt.Load(cmd.ExecuteReader());
                return dt.Clone();
            }
        }
    }
}
