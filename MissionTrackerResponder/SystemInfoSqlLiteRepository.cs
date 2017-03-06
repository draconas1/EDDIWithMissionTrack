using EddiDataProviderService;
using EddiMissionTrackerResponder;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;

namespace EddiDataProviderService
{
    public class SystemInfoSqlLiteRepository
    {
        public static string DbFile
        {
            get { return Constants.DATA_DIR + @"\StarSystemInfo.sqlite"; }
        }

        public static SQLiteConnection SimpleDbConnection()
        {
            return new SQLiteConnection("Data Source=" + DbFile);
        }

        private const string TRUNCATE_SYSTEMS_SQL = "TRUNCATE TABLE systems";
        private const string TRUNCATE_STATIONS_SQL = "TRUNCATE TABLE stations";

        private const string CREATE_JOIN_TABLE_SQL = @"
                    CREATE TEMPORARY TABLE EDSM_IDS(edsm_id BIGINT)";


        private const string CREATE_SYSTEMS_TABLE_SQL = @"
                    CREATE TABLE IF NOT EXISTS systems(
                     id BIGINT NOT NULL
                     ,edsm_id BIGINT
                     ,name TEXT NOT NULL
                     ,x int NOT NULL
                     ,y int NOT NULL
                     ,z int NOT NULL)";
        private const string CREATE_STATION_TABLE_SQL = @"
                    CREATE TABLE IF NOT EXISTS stations(
                     id BIGINT NOT NULL
                     ,name TEXT NOT NULL
                     ,system_id BIGINT NOT NULL
                     ,distance_to_star INT NOT NULL)";
        private const string INSERT_SYSTEM_SQL = @"
                    INSERT INTO systems(
                     id
                     ,edsm_id
                     ,name
                     ,x
                     ,y
                     ,z)
                    VALUES(@id, @edsm_id, @name, @x, @y, @z)";
     
        private const string INSERT_STATION_SQL = @"
                    INSERT INTO stations (id, name, system_id, distance_to_star) 
                    VALUES (@id, @name, @system_id, @distance)";


        private const string FIND_PROBLEM_STATIONS_SQL = @"
                    SELECT st.name as station_name, st.distance_to_star, ss.name as system_name
                    FROM systems ss INNER JOIN stations st ON ss.id = st.system_id
                    WHERE ss.edsm_id IN ({0})
                    AND st.distance_to_star > @distance";


        private static SystemInfoSqlLiteRepository instance;

        private SystemInfoSqlLiteRepository()
        {
            CreateDatabase();
        }

        private static readonly object instanceLock = new object();
        public static SystemInfoSqlLiteRepository Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (instanceLock)
                    {
                        if (instance == null)
                        {
                            Logging.Debug("No SystemInfoSqlLiteRepository instance: creating one");
                            instance = new SystemInfoSqlLiteRepository();
                        }
                    }
                }
                return instance;
            }
        }

        public void ClearDatabase()
        {
            using (var con = SimpleDbConnection())
            {
                con.Open();

                using (var cmd = new SQLiteCommand(TRUNCATE_SYSTEMS_SQL, con))
                {
                    Logging.Debug("deleting systems repository");
                    cmd.ExecuteNonQuery();
                }
                using (var cmd = new SQLiteCommand(TRUNCATE_STATIONS_SQL, con))
                {
                    Logging.Debug("deleting station repository");
                    cmd.ExecuteNonQuery();
                }

                con.Close();
            }
            Logging.Debug("cleared star system repository");
        }

        private static void CreateDatabase()
        {
            using (var con = SimpleDbConnection())
            {
                con.Open();

                using (var cmd = new SQLiteCommand(CREATE_SYSTEMS_TABLE_SQL, con))
                {
                    Logging.Debug("Creating systems repository");
                    cmd.ExecuteNonQuery();
                }
                using (var cmd = new SQLiteCommand(CREATE_STATION_TABLE_SQL, con))
                {
                    Logging.Debug("Creating station repository");
                    cmd.ExecuteNonQuery();
                }

                con.Close();
            }
            Logging.Debug("Created mission & cargo repository");
        }

        public void insertSystems(string jsonFilePath)
        {
            using (var file = new StreamReader(jsonFilePath))
            { 
                using (var con = SimpleDbConnection())
                {
                    con.Open();
                    using (var transaction = con.BeginTransaction())
                    {
                        using (var cmd = new SQLiteCommand(con))
                        {
                            cmd.CommandText = INSERT_SYSTEM_SQL;
                            cmd.Prepare();
                            string line;
                            while ((line = file.ReadLine()) != null)
                            {
                                IDictionary<string, object> data = Deserializtion.DeserializeData(line);
                                cmd.Parameters.AddWithValue("@id", data["id"]);
                                cmd.Parameters.AddWithValue("@edsm_id", data["edsm_id"]);
                                cmd.Parameters.AddWithValue("@name", data["name"]);
                                cmd.Parameters.AddWithValue("@x", data["x"]);
                                cmd.Parameters.AddWithValue("@y", data["y"]);
                                cmd.Parameters.AddWithValue("@z", data["z"]);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        transaction.Commit();
                    }
                    con.Close();
                }
                file.Close();
            }
        }

        public void insertStations(string jsonFilePath)
        {
            using (var file = new StreamReader(jsonFilePath))
            {
                using (var con = SimpleDbConnection())
                {
                    con.Open();
                    using (var transaction = con.BeginTransaction())
                    {
                        using (var cmd = new SQLiteCommand(con))
                        {
                            cmd.CommandText = INSERT_STATION_SQL;
                            cmd.Prepare();
                            string line;
                            while ((line = file.ReadLine()) != null)
                            {
                                IDictionary<string, object> data = Deserializtion.DeserializeData(line);
                                cmd.Parameters.AddWithValue("@id", data["id"]);
                                cmd.Parameters.AddWithValue("@system_id", data["system_id"]);
                                cmd.Parameters.AddWithValue("@name", data["name"]);
                                var distance = data["distance_to_star"];
                                if (distance == null)
                                {
                                    distance = 1000;
                                }
                                cmd.Parameters.AddWithValue("@distance", distance);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        transaction.Commit();
                    }
                    con.Close();
                }
                file.Close();
            }
        }

        public IList<ProblemStation> getProblemStations(IList<long> edsmIds, int distanceToBeAProblem)
        {
            IList<ProblemStation> result = new List<ProblemStation>();
            try
            {
                using (var con = SimpleDbConnection())
                {
                    con.Open();
                    var ids = string.Join(",", edsmIds.ToArray());
                    
                    using (var cmd = new SQLiteCommand(con))
                    {
                        cmd.CommandText = string.Format(FIND_PROBLEM_STATIONS_SQL, ids);
                        cmd.Prepare();
                        cmd.Parameters.AddWithValue("@distance", distanceToBeAProblem);
                        using (SQLiteDataReader rdr = cmd.ExecuteReader())
                        {
                            while (rdr.Read())
                            {
                                string stationName = rdr.GetString(0);
                                int distance = rdr.GetInt32(1);
                                string systemName = rdr.GetString(2);
                                result.Add(new ProblemStation(stationName, systemName, distance));
                            }
                        }
                    }
                    con.Close();
                }
            }
            catch (Exception ex)
            {
                Logging.Warn("Problem obtaining data: " + ex);
            }
            return result;
        }
       
    }
}
