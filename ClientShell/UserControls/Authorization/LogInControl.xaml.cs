using System;
using System.Collections.Generic;
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
    /// Логика взаимодействия для LogInControl.xaml
    /// </summary>
    public partial class LogInControl : UserControl
    {
        private AuthorizationControl authorizationControl;

        public LogInControl(AuthorizationControl authorizationControl)
        {
            InitializeComponent();
            this.authorizationControl = authorizationControl;
        }

        private void GoToRegister(object sender, RoutedEventArgs e)
        {
            authorizationControl.GoToRegister();
        }
    }
}
