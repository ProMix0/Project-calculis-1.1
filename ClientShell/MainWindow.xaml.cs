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
            AbstractConnection connection =  new RsaDecorator(new TcpConnection());
            //AbstractConnection connection = new TcpConnection();
            connection.SetEndPoint("127.0.0.1", 8888);
            connection.Connect();
            //output.Text = Encoding.UTF8.GetString(connection.GetMessage().Result);
            try
            {
                button.Click += async (object sender, RoutedEventArgs e) =>
                 {
                     if (connection.IsActive)
                     {
                         connection.Send(Encoding.UTF8.GetBytes(input.Text));
                         Task<byte[]> task = connection.GetMessageAsync();
                         await task;
                         output.Text = Encoding.UTF8.GetString(task.Result);
                     }
                 };
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
