using System.Windows.Controls;
using CRM_lourd.ViewModels;

namespace CRM_lourd.Views
{
    public partial class DashboardView : UserControl
    {
        public DashboardView()
        {
            InitializeComponent();

            var vm = new DashboardViewModel();
            this.DataContext = vm;
        }
    }
}
