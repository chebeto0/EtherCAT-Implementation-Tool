using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
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

namespace EtherCAT_Master.Core.Controls
{
    /// <summary>
    /// Interaction logic for ScopeUserControl.xaml
    /// </summary>
    public partial class ScopeUserControl : UserControl
    {
        public ScopeUserControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Planned to open a second plot. Not implementes yet
        /// </summary>
        private void OnOptionsClick(object sender, ItemClickEventArgs e)
        {
            hamburger1.SelectedOptionsIndex = -1;
        }
    }
}
