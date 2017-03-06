using Eddi;
using EddiCompanionAppService;
using EddiDataDefinitions;
using EddiEvents;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using Utilities;

namespace EddiMissionTrackerResponder
{
    public class MissionTrackerResponder : EDDIResponder
    {
        private MissionTrackerViewModel mViewModel;
        private bool mRunning;

        public MissionTrackerResponder()
        {
            Logging.Info("Initialised " + ResponderName() + " " + ResponderVersion());
        }

        public UserControl ConfigurationTabItem()
        {
            return new ConfigurationWindow(mViewModel) { TestHack = this };
        }

        public string ResponderName()
        {
            return "Mission Tracker";
        }

        public string ResponderVersion()
        {
            return "1.0.0";
        }

        public string ResponderDescription()
        {
            return "Tracks your trade missions";
        }

        public void Handle(Event theEvent)
        {
            if (EDDI.Instance.inCQC)
            {
                // We don't do anything whilst in CQC
                return;
            }

            if (theEvent is MissionAcceptedEvent)
            {
                MissionAcceptedEvent newMissionEvent = (MissionAcceptedEvent)theEvent;
                if (newMissionEvent.missionid.HasValue)
                {
                    mViewModel.addMission(newMissionEvent);
                }
            }
            else if (theEvent is MissionCompletedEvent)
            {
                MissionCompletedEvent missionEndedEvent = (MissionCompletedEvent)theEvent;
                if (missionEndedEvent.missionid.HasValue)
                {
                    mViewModel.removeMission(missionEndedEvent.missionid);
                }
            }
            else if (theEvent is MissionAbandonedEvent)
            {
                MissionAbandonedEvent missionEndedEvent = (MissionAbandonedEvent)theEvent;
                mViewModel.removeMission(missionEndedEvent.missionid);
            }

            else if (theEvent is CommodityPurchasedEvent)
            {
                var commodityEvent = (CommodityPurchasedEvent)theEvent;
                mViewModel.addCargo(commodityEvent.commodity, commodityEvent.amount);
            }
            else if (theEvent is CommoditySoldEvent)
            {
                var commodityEvent = (CommoditySoldEvent)theEvent;
                mViewModel.removeCargo(commodityEvent.commodity, commodityEvent.amount);
            }
            else if (theEvent is CommodityRefinedEvent)
            {
                var commodityEvent = (CommodityRefinedEvent)theEvent;
                mViewModel.addCargo(commodityEvent.commodity, 1);
            }
            else if (theEvent is CommodityCollectedEvent)
            {
                var commodityEvent = (CommodityCollectedEvent)theEvent;
                mViewModel.addCargo(commodityEvent.commodity, 1);
            }
            else if (theEvent is CommodityEjectedEvent)
            {
                var commodityEvent = (CommodityEjectedEvent)theEvent;
                mViewModel.removeCargo(commodityEvent.commodity, commodityEvent.amount);
            }
            else if (theEvent is MarketInformationUpdatedEvent)
            {
                var dockedEvent = (MarketInformationUpdatedEvent)theEvent;

                foreach(var mr in mViewModel.MissionRequirements)
                {
                    mr.AtCurrentStation = null;
                }

                updateStationInfo(dockedEvent);
            }
        }
   
        public void Reload()
        {
            mViewModel = new MissionTrackerViewModel();
        }

        public bool Start()
        {
            Reload();
            mRunning = true;
            return mViewModel != null;
        }

        public void Stop()
        {
            mViewModel = null;
            mRunning = false;
        }


        private void updateStationInfo(MarketInformationUpdatedEvent dockedEvent)
        {
            if (EDDI.Instance.CurrentStation.commodities != null)
            {
                var lookup = mViewModel.MissionRequirements.ToDictionary(x => x.Name, x => x);

                foreach (Commodity commodity in EDDI.Instance.CurrentStation.commodities)
                {
                    if (lookup.ContainsKey(commodity.name))
                    {
                        if (commodity.buyprice.HasValue && commodity.buyprice> 0 && commodity.stock.HasValue && commodity.stock > 0)
                        {
                            lookup[commodity.name].AtCurrentStation = true;
                        }
                    }
                }
            }
        }
    }
}
