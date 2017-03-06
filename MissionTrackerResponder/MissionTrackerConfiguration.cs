using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;

namespace EddiMissionTrackerResponder
{
    public class MissionTrackerConfiguration
    {
        [JsonProperty("apiKey")]
        public string maxDistanceFromCurrent { get; set; }
        [JsonProperty("commanderName")]
        public string problemDistanceFromStar { get; set; }

        [JsonIgnore]
        private string dataPath;

        public MissionTrackerConfiguration()
        {
            maxDistanceFromCurrent = "35";
            problemDistanceFromStar = "1500";
        }

        /// <summary>
        /// Obtain credentials from a file.  If the file name is not supplied the the default
        /// path of Constants.Data_DIR\edsm.json is used
        /// </summary>
        public static MissionTrackerConfiguration FromFile(string filename = null)
        {
            if (filename == null)
            {
                filename = Constants.DATA_DIR + @"\missionTracker.json";
            }

            MissionTrackerConfiguration credentials = new MissionTrackerConfiguration();
            try
            {
                string credentialsData = File.ReadAllText(filename);
                credentials = JsonConvert.DeserializeObject<MissionTrackerConfiguration>(credentialsData);
            }
            catch (Exception ex)
            {
                Logging.Debug("Failed to read mission tracker information", ex);
            }

            credentials.dataPath = filename;
            return credentials;
        }

        /// <summary>
        /// Clear the information held by credentials.
        /// </summary>
        public void Clear()
        {
            maxDistanceFromCurrent = null;
            problemDistanceFromStar = null;
        }

        /// <summary>
        /// Obtain credentials to a file.  If the filename is not supplied then the path used
        /// when reading in the credentials will be used, or the default path of 
        /// Constants.Data_DIR\credentials.json will be used
        /// </summary>
        public void ToFile(string filename = null)
        {
            if (filename == null)
            {
                filename = dataPath;
            }
            if (filename == null)
            {
                filename = Constants.DATA_DIR + @"\missionTracker.json";
            }

            string json = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(filename, json);
        }
    }
}
