using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EddiDataProviderService
{
    interface IMissionRepository
    {
        void saveMission(long id, string json);
        void deleteMission(long id);
        void deleteAllMissions();
        void setCargo(string name, int quantity);
        void deleteCargo(string name);
        void deleteAllCargo();
        IEnumerable<string> loadAllMissions();
        IDictionary<string, int> loadAllCargo();

    }
}
