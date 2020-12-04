//using ClientLibrary;
using CommonLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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

namespace ClientShell
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            AbstractConnection connection = new TcpConnection(); //new RsaDecorator(new TcpConnection(),false);
            connection.SetEndPoint("127.0.0.1", 8888);
            connection.ReceivingState = ReceivingState.Method;
            connection.Connect();
            while (connection.GetReceivedCount() == 0) Thread.Sleep(100);
            output.Text = Encoding.UTF8.GetString(connection.GetReceived().ToArray());
            //try
            //{
            //    button.Click += (object sender, RoutedEventArgs e) =>
            //    {
            //        if (connection.IsActive())
            //        {
            //            connection.Send(Encoding.UTF8.GetBytes(input.Text));
            //            while (connection.GetReceivedCount() == 0) Thread.Sleep(100);
            //            output.Text = Encoding.UTF8.GetString(connection.GetReceived().ToArray());
            //        }
            //    };
            //}
            //catch (Exception ex)
            //{
            //    MessageBox.Show(ex.Message);
            //}
        }
    }
}
