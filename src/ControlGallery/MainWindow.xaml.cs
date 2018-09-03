﻿using Celestial.UIToolkit.Controls;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ControlGallery
{

    public partial class MainWindow : Window
    {

        public MainWindow()
        {
            InitializeComponent();

            var lv = new ListView();
            var o = new object();
        }

        private void NavView_BackRequested(object sender, System.EventArgs e)
        {
            MessageBox.Show("Back requested!");
        }

        private void ChangeMenuItemButton_Click(object sender, RoutedEventArgs e)
        {
            NavView.MenuItems.Add("New Item. It's a string!");
        }

        private void ClearMenuItemsButton_Click(object sender, RoutedEventArgs e)
        {
            NavView.MenuItems.Clear();
        }

        private void ChangeMenuItemsSource_Click(object sender, RoutedEventArgs e)
        {
            var controlSource = new ObservableCollection<object>()
            {
            };

            NavView.MenuItemsSource = controlSource;

            controlSource.Add(new Label() { Content = "Hello" });
            controlSource.Add(new Button() { Content = "From" });
            controlSource.Add(new TextBox() { Text = "The" });
            controlSource.Add("ItemsSource");
        }

        private void ClearMenuItemsSource_Click(object sender, RoutedEventArgs e)
        {
            NavView.MenuItemsSource = null;
        }

        private void NavView_ItemInvoked(object sender, NavigationViewItemEventArgs e)
        {
            Debug.WriteLine(e.ToString());
        }

        private void NavView_ItemInvoked(object sender, object e)
        {

        }
        
    }

}
