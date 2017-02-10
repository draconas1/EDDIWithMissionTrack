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
                mViewModel.Missions.Add(newMissionEvent);
            }
            else if (theEvent is MissionCompletedEvent)
            {
                MissionCompletedEvent missionEndedEvent = (MissionCompletedEvent)theEvent;
                mViewModel.removeMission(missionEndedEvent.missionid);
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
            else if (theEvent is DockedEvent)
            {
                var dockedEvent = (DockedEvent)theEvent;

                foreach(var mr in mViewModel.MissionRequirements)
                {
                    mr.AtCurrentStation = null;
                }

                Thread updateThread = new Thread(() => updateStationInfo(dockedEvent));
                updateThread.IsBackground = true;
                updateThread.Start();
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


        private void updateStationInfo(DockedEvent dockedEvent)
        {
            if (CompanionAppService.Instance == null && CompanionAppService.Instance.CurrentState != CompanionAppService.State.READY)
            {
                Logging.Debug("Cannot refresh profile when companion app service is not active");
                return;
            }

            int maxTries = 16;

            while (mRunning && maxTries > 0)
            {
                try
                {
                    if (EDDI.Instance.CurrentStation.commodities != null)
                    {
                        var lookup = mViewModel.MissionRequirements.ToDictionary(x => x.Name, x => x);

                        foreach (Commodity commodity in EDDI.Instance.CurrentStation.commodities)
                        {
                            if (lookup.ContainsKey(commodity.name))
                            {
                                if (commodity.sellprice.HasValue && commodity.stock.HasValue)
                                {
                                    lookup[commodity.name].AtCurrentStation = true;
                                }
                            }
                        }
                        break;
                    }
                    else
                    { 
                        // profile hasn't updated yet.  Wait.  shorter wait than profile as to be useful i need to get info to the user quickly.
                        Thread.Sleep(5000);
                    }
                }
                catch (Exception ex)
                {
                    Logging.Error("Exception getting updated profile info", ex);
                }
                finally
                {
                    maxTries--;
                }
            }

            if (maxTries == 0)
            {
                Logging.Info("Maximum attempts reached; giving up on updating profile");
            }
        }
    }
}
