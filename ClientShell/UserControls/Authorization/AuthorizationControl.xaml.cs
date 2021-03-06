﻿using System;
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
    /// Логика взаимодействия для AuthorizationControl.xaml
    /// </summary>
    public partial class AuthorizationControl : UserControl
    {
        private LogInControl logInControl;
        private RegisterControl registerControl;

        public AuthorizationControl()
        {
            InitializeComponent();
            logInControl = new LogInControl(this);
            registerControl = new RegisterControl(this);

            GoToLogIn();
        }

        public void GoToLogIn()
        {
            border.Child = logInControl;
        }

        public void GoToRegister()
        {
            border.Child = registerControl;
        }
    }
}
