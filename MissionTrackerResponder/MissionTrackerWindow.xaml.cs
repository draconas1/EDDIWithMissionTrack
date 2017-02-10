using EddiEvents;
using System;
using System.Collections.Generic;
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

namespace EddiMissionTrackerResponder
{
    /// <summary>
    /// Interaction logic for MissionTrackerWindow.xaml
    /// </summary>
    public partial class MissionTrackerWindow : Window
    {
        public MissionTrackerViewModel Data { get; private set; }


        public MissionTrackerWindow(MissionTrackerViewModel viewModel)
        {
            Data = viewModel;
            DataContext = Data;

            InitializeComponent();
        }

        private void clearStore(object sender, EventArgs e)
        {
            Data.Clear();
        }
    }
}
