using EddiDataProviderService;
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
    class MissionSqlLiteRepository : SqLiteBaseRepository, IMissionRepository
    {
        private const string CREATE_MISSION_TABLE_SQL = @"
                    CREATE TABLE IF NOT EXISTS missions(
                     id BIGINT NOT NULL
                     ,event_json TEXT NOT NULL)";
        private const string CREATE_CARGO_TABLE_SQL = @"
                    CREATE TABLE IF NOT EXISTS cargo(
                     name TEXT NOT NULL
                     ,quantity INT NOT NULL)";
        private const string INSERT_MISSION_SQL = @"
                    INSERT INTO missions(
                     id
                     ,event_json)
                    VALUES(@id, @json)";
        private const string DELETE_MISSION_SQL = @"
                    DELETE FROM missions
                    WHERE id = @id";

        private const string DELETE_ALL_MISSION_SQL = @"
                    DELETE FROM missions";

        private const string INSERT_CARGO_SQL = @"
                    INSERT OR REPLACE INTO cargo (name, quantity) 
                    VALUES (@name, @quantity)";

        private const string DELETE_CARGO_SQL = @"
                    DELETE FROM cargo
                    WHERE name = @name";

        private const string DELETE_ALL_CARGO_SQL = @"
                    DELETE FROM cargo";

        private const string SELECT_ALL_MISSIONS = @"
                    SELECT event_json 
                    FROM missions";

        private const string SELECT_ALL_CARGO = @"
                    SELECT name, quantity 
                    FROM cargo";

        private static MissionSqlLiteRepository instance;

        private MissionSqlLiteRepository()
        {
            CreateDatabase();
        }

        private static readonly object instanceLock = new object();
        public static MissionSqlLiteRepository Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (instanceLock)
                    {
                        if (instance == null)
                        {
                            Logging.Debug("No MissionSqLiteRepository instance: creating one");
                            instance = new MissionSqlLiteRepository();
                        }
                    }
                }
                return instance;
            }
        }

        private static void CreateDatabase()
        {
            using (var con = SimpleDbConnection())
            {
                con.Open();

                using (var cmd = new SQLiteCommand(CREATE_MISSION_TABLE_SQL, con))
                {
                    Logging.Debug("Creating mission repository");
                    cmd.ExecuteNonQuery();
                }
                using (var cmd = new SQLiteCommand(CREATE_CARGO_TABLE_SQL, con))
                {
                    Logging.Debug("Creating cargo repository");
                    cmd.ExecuteNonQuery();
                }

                con.Close();
            }
            Logging.Debug("Created mission & cargo repository");
        }

        public void deleteAllCargo()
        {
            using (var con = SimpleDbConnection())
            {
                con.Open();

                using (var cmd = new SQLiteCommand(con))
                {
                    cmd.CommandText = DELETE_ALL_CARGO_SQL;
                    cmd.Prepare();
                    cmd.ExecuteNonQuery();
                }
                con.Close();
            }
        }

        public void deleteAllMissions()
        {
            using (var con = SimpleDbConnection())
            {
                con.Open();

                using (var cmd = new SQLiteCommand(con))
                {
                    cmd.CommandText = DELETE_ALL_MISSION_SQL;
                    cmd.Prepare();
                    cmd.ExecuteNonQuery();
                }
                con.Close();
            }
        }

        public void deleteCargo(string name)
        {
            using (var con = SimpleDbConnection())
            {
                con.Open();

                using (var cmd = new SQLiteCommand(con))
                {
                    cmd.CommandText = DELETE_CARGO_SQL;
                    cmd.Prepare();
                    cmd.Parameters.AddWithValue("@name", name);
                    cmd.ExecuteNonQuery();
                }
                con.Close();
            }
        }

        public void deleteMission(long id)
        {
            using (var con = SimpleDbConnection())
            {
                con.Open();

                using (var cmd = new SQLiteCommand(con))
                {
                    cmd.CommandText = DELETE_MISSION_SQL;
                    cmd.Prepare();
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }
                con.Close();
            }
        }

        public void saveMission(long id, string json)
        {
            using (var con = SimpleDbConnection())
            {
                con.Open();

                using (var cmd = new SQLiteCommand(con))
                {
                    cmd.CommandText = INSERT_MISSION_SQL;
                    cmd.Prepare();
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.Parameters.AddWithValue("@json", json);
                    cmd.ExecuteNonQuery();
                }
                con.Close();
            }
        }

        public void setCargo(string name, int quantity)
        {
            using (var con = SimpleDbConnection())
            {
                con.Open();

                using (var cmd = new SQLiteCommand(con))
                {
                    cmd.CommandText = INSERT_CARGO_SQL;
                    cmd.Prepare();
                    cmd.Parameters.AddWithValue("@name", name);
                    cmd.Parameters.AddWithValue("@quantity", quantity);
                    cmd.ExecuteNonQuery();
                }
                con.Close();
            }
        }

        public IEnumerable<string> loadAllMissions()
        {
            if (!File.Exists(DbFile)) return null;

            IList<string> result = new List<string>();
            try
            {
                using (var con = SimpleDbConnection())
                {
                    con.Open();
                    using (var cmd = new SQLiteCommand(con))
                    {
                        cmd.CommandText = SELECT_ALL_MISSIONS;
                        cmd.Prepare();
                        using (SQLiteDataReader rdr = cmd.ExecuteReader())
                        {
                            while (rdr.Read())
                            {
                                string missionJson = rdr.GetString(0);
                                result.Add(missionJson);
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

        public IDictionary<string, int> loadAllCargo()
        {
            if (!File.Exists(DbFile)) return null;

            IDictionary<string, int> result = new Dictionary<string, int>();
            try
            {
                using (var con = SimpleDbConnection())
                {
                    con.Open();
                    using (var cmd = new SQLiteCommand(con))
                    {
                        cmd.CommandText = SELECT_ALL_CARGO;
                        cmd.Prepare();
                        using (SQLiteDataReader rdr = cmd.ExecuteReader())
                        {
                            while (rdr.Read())
                            {
                                string cargoName = rdr.GetString(0);
                                int cargoQuantity = rdr.GetInt32(1);
                                result.Add(cargoName, cargoQuantity);
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
