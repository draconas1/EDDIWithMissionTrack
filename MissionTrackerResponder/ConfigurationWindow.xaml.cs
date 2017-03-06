using Eddi;
using EddiDataProviderService;
using EddiEvents;
using EddiJournalMonitor;
using Microsoft.Win32;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Utilities;

namespace EddiMissionTrackerResponder
{
    /// <summary>
    /// Interaction logic for ConfigurationWindow.xaml
    /// </summary>
    public partial class ConfigurationWindow : UserControl
    {
        public MissionTrackerResponder TestHack { get; set; }

        public MissionTrackerViewModel Data { get; private set; }

        private MissionTrackerWindow mWindow;

        public ConfigurationWindow(MissionTrackerViewModel viewModel)
        {
            Data = viewModel;
            DataContext = Data;
                        
            InitializeComponent();
            MissionTrackerConfiguration config = MissionTrackerConfiguration.FromFile();
            MaxStarDistanceBox.Text = config.maxDistanceFromCurrent;
            MinStationDistance.Text = config.problemDistanceFromStar;
        }

        private void testButtonClick(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                string line;
                System.IO.StreamReader file = new System.IO.StreamReader(openFileDialog.FileName);
                while ((line = file.ReadLine()) != null)
                {
                    var journalEvent = JournalMonitor.ParseJournalEntry(line);
                    TestHack.Handle(journalEvent);
                }
            }
        }

        private void showMainWindow(object sender, EventArgs e)
        {
            mWindow = new MissionTrackerWindow(Data);
            mWindow.Show();
        }


        private void loadStationData(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                SystemInfoSqlLiteRepository.Instance.insertStations(openFileDialog.FileName);
            }
        }

        private void loadSystemData(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                SystemInfoSqlLiteRepository.Instance.insertSystems(openFileDialog.FileName);
            }
        }

        private void configChanged(object sender, TextChangedEventArgs e)
        {
            MissionTrackerConfiguration config = MissionTrackerConfiguration.FromFile();
            if (!string.IsNullOrEmpty(MaxStarDistanceBox.Text))
            {
                config.maxDistanceFromCurrent = MaxStarDistanceBox.Text;
            }
            if (!string.IsNullOrEmpty(MinStationDistance.Text))
            {
                config.problemDistanceFromStar = MinStationDistance.Text;
            }
            config.ToFile();
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void clearProblemStationClick(object sender, RoutedEventArgs e)
        {
            SystemInfoSqlLiteRepository.Instance.ClearDatabase();
        }
    }
}
