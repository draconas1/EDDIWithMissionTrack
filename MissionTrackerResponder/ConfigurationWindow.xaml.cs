using Eddi;
using EddiEvents;
using EddiJournalMonitor;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
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
            mWindow = new MissionTrackerWindow(viewModel);
            mWindow.Hide();
        }

        private void testButtonClick(object sender, EventArgs e)
        {
            string line;
            System.IO.StreamReader file = new System.IO.StreamReader("D:\\Users\\Draconas\\Saved Games\\Frontier Developments\\Elite Dangerous\\Journal.170128103049.01.log");
            while ((line = file.ReadLine()) != null)
            {
                var journalEvent = JournalMonitor.ParseJournalEntry(line);
                TestHack.Handle(journalEvent);
            }
        }

        private void clearStore(object sender, EventArgs e)
        {
            mWindow.Show();
        }
    }
}
