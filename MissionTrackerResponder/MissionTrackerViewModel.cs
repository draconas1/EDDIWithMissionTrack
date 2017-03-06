using EddiDataProviderService;
using EddiEvents;
using EddiJournalMonitor;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;

namespace EddiMissionTrackerResponder
{
    public class MissionTrackerViewModel : INotifyPropertyChanged
    {
        private static object sMissionsLock = new object();
        private static object sCargoLock = new object();
        private static object sMissionCargoLock = new object();
        private static object sProblemStationsLock = new object();
        

        public ObservableCollection<MissionAcceptedEvent> Missions { get; private set; }

        public ObservableCollection<Cargo> Cargo { get; private set; }

        public ObservableCollection<ProblemStation> ProblemStations { get; private set; }

        public ObservableCollection<MissionCargo> MissionRequirements { get; private set; }

        public RelayCommand DeleteSelectedMission { get; private set; }

        public RelayCommand CalculateProblematicStations { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged;

        private IMissionRepository mRepository;

        private NotifyCollectionChangedEventHandler mDatabaseHandler;

        private MissionAcceptedEvent mSelectedMission;

        public MissionTrackerViewModel()
        {
            Missions = new ObservableCollection<MissionAcceptedEvent>();
            Cargo = new ObservableCollection<Cargo>();
            MissionRequirements = new ObservableCollection<MissionCargo>();
            ProblemStations = new ObservableCollection<ProblemStation>();

            BindingOperations.EnableCollectionSynchronization(Missions, sMissionsLock);
            BindingOperations.EnableCollectionSynchronization(Cargo, sCargoLock);
            BindingOperations.EnableCollectionSynchronization(MissionRequirements, sMissionCargoLock);
            BindingOperations.EnableCollectionSynchronization(ProblemStations, sProblemStationsLock);

            Missions.CollectionChanged += new NotifyCollectionChangedEventHandler(calculateMissionRequirements);
            mRepository = MissionSqlLiteRepository.Instance;
            foreach(var cargo in mRepository.loadAllCargo().AsEnumerable())
            {
                Cargo.Add(new Cargo(cargo.Key, cargo.Value));
            }
            foreach(var json in mRepository.loadAllMissions())
            {
                var journalEvent = (MissionAcceptedEvent)JournalMonitor.ParseJournalEntry(json);
                Missions.Add(journalEvent);
            }
            mDatabaseHandler = new NotifyCollectionChangedEventHandler(updateDatabase);
            Missions.CollectionChanged += mDatabaseHandler;

            DeleteSelectedMission = new RelayCommand(deleteMission, () => SelectedMission != null);
            CalculateProblematicStations = new RelayCommand(() => new ProblematicDestinationCalculator(this).calculate());
        }


        public MissionAcceptedEvent SelectedMission
        {
            get
            {
                return mSelectedMission;
            }
            set
            {
                if (mSelectedMission != value)
                {
                    mSelectedMission = value;
                    DeleteSelectedMission.RaiseCanExecuteChanged();
                }
            }
        }

        private void deleteMission()
        {
            if (SelectedMission == null)
            {
                return;
            }
            mRepository.deleteMission(SelectedMission.missionid.Value);
            Missions.Remove(SelectedMission);
        }



        private void updateDatabase(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach(var item in e.NewItems.Cast<MissionAcceptedEvent>())
                {
                    mRepository.saveMission(item.missionid.Value, item.raw);
                }
            }
            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (var item in e.OldItems.Cast<MissionAcceptedEvent>())
                {
                    mRepository.deleteMission(item.missionid.Value);
                }
            }
        }

        private void calculateMissionRequirements(object sender, NotifyCollectionChangedEventArgs e)
        {
            var cargoLookup = Cargo.ToDictionary(cargo => cargo.Name, cargo => cargo.Quantity);
            MissionRequirements.Clear();
            var stuff = Missions.Where(mission => mission.amount.HasValue)
                .Where(mission => !mission.name.Contains("Deliver"))
                .GroupBy(mission => mission.commodity)
                .Select(group => new
                {
                    Commodity = group.Key,
                    Quantity = group.Sum(mission => mission.amount.Value)
                });

            foreach(var thing in stuff)
            {
                int inCargoBay = 0;
                cargoLookup.TryGetValue(thing.Commodity, out inCargoBay);
                MissionRequirements.Add(new MissionCargo(thing.Commodity, inCargoBay) { RequiredQuantity = thing.Quantity });
            }
        }

        public void addCargo(string name, int value)
        {
            var foundCargo = Cargo.SingleOrDefault(cargo => cargo.Name == name);
            if (foundCargo != null)
            {
                foundCargo.Quantity += value;
                mRepository.setCargo(name, foundCargo.Quantity);
            }
            else
            {
                Cargo.Add(new Cargo(name, value));
                mRepository.setCargo(name, value);
            }
        }

        public void removeCargo(string name, int value)
        {
            var foundCargo = Cargo.SingleOrDefault(cargo => cargo.Name == name);
            if (foundCargo != null)
            {
                foundCargo.Quantity -= value;
                if (foundCargo.Quantity <= 0)
                {
                    Cargo.Remove(foundCargo);
                    mRepository.deleteCargo(name);
                }
                else
                {
                    mRepository.setCargo(name, foundCargo.Quantity);
                }
            }
        }

        public void addMission(MissionAcceptedEvent newMission)
        {
            Missions.Add(newMission);
        }

        public void removeMission(long? missionId)
        {
            var foundMission = Missions.SingleOrDefault(mission => mission.missionid == missionId);
            if (foundMission != null)
            {
                Missions.Remove(foundMission);
            }
        }

        public void Clear()
        {
            Missions.CollectionChanged -= mDatabaseHandler;
            mRepository.deleteAllCargo();
            mRepository.deleteAllMissions();
            Missions.Clear();
            Cargo.Clear();
            MissionRequirements.Clear();
            Missions.CollectionChanged += mDatabaseHandler;
        }

        // This method is called by the Set accessor of each property.
        // The CallerMemberName attribute that is applied to the optional propertyName
        // parameter causes the property name of the caller to be substituted as an argument.
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class Cargo : IComparable<Cargo>, INotifyPropertyChanged
    {
        public string Name { get; }

        private int mQuantity;

        public Cargo(string name, int quantity)
        {
            Name = name;
            mQuantity = quantity;
        }

        public int Quantity
        {
            get
            {
                return mQuantity;
            }
            set
            {
                if (value != mQuantity)
                {
                    mQuantity = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public int CompareTo(Cargo other)
        {
            return Name.CompareTo(other.Name);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        // This method is called by the Set accessor of each property.
        // The CallerMemberName attribute that is applied to the optional propertyName
        // parameter causes the property name of the caller to be substituted as an argument.
        protected void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class MissionCargo : Cargo
    {
        private int mRequiredQuantity;
        private bool? mAtCurrentStation;

        public int RequiredQuantity
        {
            get
            {
                return mRequiredQuantity;
            }
            set
            {
                if (value != mRequiredQuantity)
                {
                    mRequiredQuantity = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public bool? AtCurrentStation
        {
            get
            {
                return mAtCurrentStation;
            }
            set
            {
                if (value != mAtCurrentStation)
                {
                    mAtCurrentStation = value;
                    NotifyPropertyChanged();
                }
            }
        }


        public MissionCargo(string name, int quantity) : base(name, quantity)
        {
        }
    }

    public class ProblemStation
    {
        public string Name { get; private set; }
        public string SystemName { get; private set; }
        public int Distance { get; private set; }

        public ProblemStation(string name, string systemName, int distance)
        {
            Name = name;
            SystemName = systemName;
            Distance = distance;
        }
    }
}
