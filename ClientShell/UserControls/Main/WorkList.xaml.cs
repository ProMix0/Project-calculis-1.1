using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace ClientShell
{
    /// <summary>
    /// Логика взаимодействия для WorkList.xaml
    /// </summary>
    public partial class WorkList : UserControl
    {
        public static DependencyProperty SelectedProperty = DependencyProperty.Register(nameof(Selected), typeof(Control), typeof(WorkList));


        public Control Selected { get; }
        public ObservableCollection<WorkInfo> WorkItems { get; } = new ObservableCollection<WorkInfo>();

        public WorkList()
        {
            InitializeComponent();
        }
    }
}
