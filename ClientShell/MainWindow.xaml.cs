﻿using Client_library;
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
            try
            {
                ConnectionToServer connection = new ConnectionToServer();
                connection.Connect("10.0.0.10", 8888);
                button.Click += (object sender, RoutedEventArgs e) =>
                {
                    if (!connection.Disposed)
                    {
                        connection.Send(Encoding.UTF8.GetBytes(input.Text));
                        output.Text = Encoding.UTF8.GetString(connection.Receive());
                    }
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
