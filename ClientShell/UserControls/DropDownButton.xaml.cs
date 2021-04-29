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
    /// Логика взаимодействия для DropDownButton.xaml
    /// </summary>
    public partial class DropDownButton : UserControl
    {

        public static readonly DependencyProperty ButtonContentProperty =
            DependencyProperty.Register(nameof(ButtonContent), typeof(ContentControl), typeof(DropDownButton));
        public static readonly DependencyProperty DropDownContentProperty =
            DependencyProperty.Register(nameof(DropDownContent), typeof(ContentControl), typeof(DropDownButton));

        public ContentControl ButtonContent { get => (ContentControl)GetValue(ButtonContentProperty); set => SetValue(ButtonContentProperty,value); }
        public ContentControl DropDownContent { get => (ContentControl)GetValue(DropDownContentProperty); set => SetValue(DropDownContentProperty, value); }

        public DropDownButton()
        {
            InitializeComponent();
        }

        private void ButtonClick(object sender, RoutedEventArgs e)
        {
            if(!popup.IsOpen)
                ShowPopup();
        }

        private void ShowPopup()
        {
            popup.IsOpen = true;
        }

        private void HidePopup()
        {
            popup.IsOpen = false;
        }
    }
}
