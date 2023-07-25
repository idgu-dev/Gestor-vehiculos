// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using Microsoft.Data.Sqlite;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace Vehicle_manager
{
    public sealed partial class AddPage : Page
    {
        private string cs;
        public MainWindow mainWindow { get; set; }
        public AddPage()
        {
            this.InitializeComponent();
        }


        private void show_error_dialog(string message)
        {
            ContentDialog dialog = new ContentDialog();
            dialog.XamlRoot = this.XamlRoot;
            dialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
            dialog.Title = "Error creando el nuevo vehiculo";
            dialog.Content = message;
            dialog.CloseButtonText = "Ok";
            dialog.DefaultButton = ContentDialogButton.Close;
            var result = dialog.ShowAsync();
        }


        private string get_icon_name()
        {
            if (button_car_icon.IsChecked == true)
            {
                return "racecar";
            }
            if (button_moto_icon.IsChecked == true)
            {
                return "moto";
            }
            if (button_truck_icon.IsChecked == true)
            {
                return "truck";
            }
            return "racecar";
        }

        private void buton_vehicle_add_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(textbox_matricula.Text) || string.IsNullOrEmpty(richeditbox_vehicle_marca.Text)
                || string.IsNullOrEmpty(textbox_modelo.Text) || string.IsNullOrEmpty(textbox_km.Text)
                || string.IsNullOrEmpty(textbox_bastidor.Text) || string.IsNullOrEmpty(textbox_ano.Text))
            {
                return;
            }

            try
            {
                var con = new SqliteConnection(cs);
                con.Open();
                var command = con.CreateCommand();
                command.CommandText = @"
                        INSERT INTO Vehiculos
                                Values ($matricula, $marca, $modelo, $km, $bastidor, $fabricacion, $icon, '' );
                    ";
                command.Parameters.AddWithValue("$matricula", textbox_matricula.Text);
                command.Parameters.AddWithValue("$marca", richeditbox_vehicle_marca.Text);
                command.Parameters.AddWithValue("$modelo", textbox_modelo.Text);
                command.Parameters.AddWithValue("$km", textbox_km.Text);
                command.Parameters.AddWithValue("$bastidor", textbox_bastidor.Text);
                command.Parameters.AddWithValue("$fabricacion", textbox_ano.Text);
                command.Parameters.AddWithValue("$icon", get_icon_name());
                command.ExecuteNonQuery();

                string name = richeditbox_vehicle_marca.Text + " " + textbox_modelo.Text + " " + textbox_matricula.Text;
                mainWindow.load_new_vehicle(name, get_icon_name());
            }
            catch (Exception ex)
            {
                if (ex.Message.ToLower().Contains("unique"))
                {
                    show_error_dialog("Ya existe un vehículo con esta matrícula.");
                }
                else
                {
                    show_error_dialog(ex.Message);
                }
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
        }

         protected override void OnNavigatedTo(NavigationEventArgs e)
         {
            mainWindow = (MainWindow)e.Parameter;
            cs=mainWindow.cs;
            base.OnNavigatedTo(e);
        }

        private void buton_import_Click(object sender, RoutedEventArgs e)
        {
            mainWindow.import_old_tables();
        }
    }
}
