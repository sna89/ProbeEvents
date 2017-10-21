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
    }
}
