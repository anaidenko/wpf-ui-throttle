using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using System.Threading;
using MVVM.Core.Optimization;

namespace Example
{
    public class Registry
    {
        public static readonly string[] Companies = new[] { "Apple", "Apple-Killer", "Facebook", "Google", "Microsoft" };
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Dependency Properties

        public static readonly DependencyProperty UIThrottleEnabledProperty = DependencyProperty.Register("UIThrottleEnabled", typeof(bool), typeof(MainWindow), new PropertyMetadata(true));
        public static readonly DependencyProperty CompanyFilterProperty = DependencyProperty.Register("CompanyFilter", typeof(string), typeof(MainWindow), new PropertyMetadata(new PropertyChangedCallback(CompanyFilterPropertyChanged)));
        public static readonly DependencyProperty FilteredCompaniesProperty = DependencyProperty.Register("FilteredCompanies", typeof(ObservableCollection<string>), typeof(MainWindow), new PropertyMetadata(new ObservableCollection<string>(Registry.Companies)));

        #endregion

        private readonly UIThrottle _filterChangedThrottle = new UIThrottle(500);

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            tbCompanyFilter.GotFocus += new RoutedEventHandler(tbCompanyFilter_GotFocus);
        }

        #region Properties

        public bool UIThrottleEnabled
        {
            get { return (bool)GetValue(UIThrottleEnabledProperty); }
            set { SetValue(UIThrottleEnabledProperty, value); }
        }

        public string CompanyFilter
        {
            get { return (string)GetValue(CompanyFilterProperty); }
            set { SetValue(CompanyFilterProperty, value); }
        }

        public ObservableCollection<string> FilteredCompanies
        {
            get { return (ObservableCollection<string>)GetValue(FilteredCompaniesProperty); }
            set { SetValue(FilteredCompaniesProperty, value); }
        }

        #endregion

        private void tbCompanyFilter_GotFocus(object sender, RoutedEventArgs e)
        {
            CompanyFilter = CompanyFilter ?? string.Empty;
        }

        private static void CompanyFilterPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var vm = d as MainWindow;
            Action filterCompany = () => vm.FilterCompanies(e.NewValue as string);

            if (vm.UIThrottleEnabled)
            {
                vm._filterChangedThrottle.Handle(filterCompany);
            }
            else
            {
                filterCompany();
            }
        }

        private void FilterCompanies(string companyNameFilter)
        {
            // Simulate long operation (e.g. calling external WCF service)...
            Thread.Sleep(300);

            var filteredCompanies = Registry.Companies.Where(x => x.Contains(companyNameFilter)).ToArray();
            FilteredCompanies = new ObservableCollection<string>(filteredCompanies);
        }
    }
}
