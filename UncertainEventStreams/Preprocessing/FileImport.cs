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
        private const string EXTRACTED_PATH = @"C:\Users\noamarbe\Desktop\YossiProject\extracted";

        public void Load(string path = DEFAULT_PATH, int numFiles = int.MaxValue)
        {
            var di = new DirectoryInfo(EXTRACTED_PATH);
            foreach (FileInfo csvFile in di.GetFiles("*.csv").OrderBy(x=>x.FullName).Take(numFiles))
            {
                //System.IO.File.Move(fileToDecompress.FullName, fileToDecompress.FullName.Replace("siri.", "siri_"));
                ImportCSV(csvFile.FullName);

                Console.WriteLine("Finished: {0}", csvFile.FullName);
            }
        }

        private void ImportCSV(string filePath, bool isFirstRowHeader = false)
        {
            #region Obtain file reader

            string header = isFirstRowHeader ? "Yes" : "No";

            string pathOnly = Path.GetDirectoryName(filePath);
            string fileName = Path.GetFileName(filePath);

            string sql = @"SELECT * FROM [" + fileName + "]";

            DataTable dt = new DataTable();

            using (OleDbConnection connection = new OleDbConnection(
                      @"Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + pathOnly +
                      ";Extended Properties=\"Text;HDR=" + header + "\""))
            using (OleDbCommand command = new OleDbCommand(sql, connection))
            using (OleDbDataAdapter adapter = new OleDbDataAdapter(command))
            {

                dt.Locale = CultureInfo.CurrentCulture;
                adapter.Fill(dt);
            }

            var cols = ObtainColumns(DATA_TABLE_NAME).ToArray();
            for (int i = 0; i < dt.Columns.Count; i++)
            {
                dt.Columns[i].ColumnName = cols[i];
            }

            #endregion

            #region Process data to new data reader
            DataTable dtProcessed = ObtainTableCopy(DATA_TABLE_NAME);

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

                    if(string.IsNullOrEmpty(stopId) || stopId == "null")
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

            #endregion

            #region Bulk insert processed data

            using (SqlBulkCopy bulkCopy = new SqlBulkCopy(ConfigurationManager.ConnectionStrings["LogConnection"].ConnectionString))
            {
                bulkCopy.DestinationTableName = DATA_TABLE_NAME;
                bulkCopy.BatchSize = 50;
                bulkCopy.WriteToServer(dtProcessed);
            }

            #endregion
        }

        public IEnumerable<string> ObtainColumns(string tableName)
        {
            using (var connection = new SqlConnection(ConfigurationManager.ConnectionStrings["LogConnection"].ConnectionString))
            {
                connection.Open();

                var command = connection.CreateCommand();
                command.CommandText =string.Format("SELECT [name] FROM sys.columns WHERE object_id=object_id('{0}')", tableName);

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
