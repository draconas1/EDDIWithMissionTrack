using Eddi;
using EddiDataProviderService;
using EddiStarMapService;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace EddiMissionTrackerResponder
{
    class ProblematicDestinationCalculator
    {
        private static CultureInfo EN_US_CULTURE = new CultureInfo("en-US");
        private readonly SystemInfoSqlLiteRepository mRepository;
        private readonly MissionTrackerViewModel mViewModel;
        private readonly StarMapConfiguration mStarMapConfig;
        private string mBaseUrl;
        private readonly int maxDistanceFromCurrent;
        private readonly int minStationDistance;

        public ProblematicDestinationCalculator(MissionTrackerViewModel viewModel)
        {
            mViewModel = viewModel;
            mRepository = SystemInfoSqlLiteRepository.Instance;
            mStarMapConfig = StarMapConfiguration.FromFile();
            mBaseUrl = "http://www.edsm.net/";
            var config = MissionTrackerConfiguration.FromFile();
            maxDistanceFromCurrent = int.Parse(config.maxDistanceFromCurrent);
            minStationDistance = int.Parse(config.problemDistanceFromStar);
        }
        
        public void calculate()
        {
            var currentSystem = EDDI.Instance.CurrentStarSystem;
            var client = new RestClient(mBaseUrl);
            var request = new RestRequest("api-v1/cube-systems");
            request.AddParameter("apiKey", mStarMapConfig.apiKey);
            request.AddParameter("commanderName", mStarMapConfig.commanderName);

            var encodedName = WebUtility.UrlEncode(currentSystem.name);
            //request.AddParameter("systemName", encodedName);

            request.AddParameter("x", getCoOrd(currentSystem.x));
            request.AddParameter("y", getCoOrd(currentSystem.y));
            request.AddParameter("z", getCoOrd(currentSystem.z));
            request.AddParameter("showId", 1);
            request.AddParameter("size", maxDistanceFromCurrent * 2);

            var nearbySystems = client.Execute<List<StarMapSquareAnswer>>(request);
            List<StarMapSquareAnswer> commentResponse = nearbySystems.Data;

            IList<ProblemStation> problems = mRepository.getProblemStations(commentResponse.Select(x => x.id).ToList(), minStationDistance);
            mViewModel.ProblemStations.Clear();
            foreach(ProblemStation problem in problems)
            {
                mViewModel.ProblemStations.Add(problem);
            }
        }

        private string getCoOrd(object cord)
        {
            return ((decimal)cord).ToString("0.000", EN_US_CULTURE);
        }


        class StarMapSquareAnswer
        {
            public string name { get; set; }
            public long id { get; set; }

        }
    }
}
