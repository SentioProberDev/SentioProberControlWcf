using System.Collections.Specialized;
using System.Windows;

using Sentio.WcfTest.ViewModel;

namespace Sentio.WcfTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            ((INotifyCollectionChanged)ListBox.Items).CollectionChanged += ListView_CollectionChanged;
        }

        private void ListView_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                // scroll the new item into view   
                ListBox.ScrollIntoView(e.NewItems[0]);
            }
        }

        private void MainWindow_OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var mvm = DataContext as MainViewModel;
            mvm?.TryAutoConnect();
        }
    }
}
