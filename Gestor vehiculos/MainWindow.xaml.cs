// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Windows.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.Data.Sqlite;
using Microsoft.UI.Composition.SystemBackdrops;
using Windows.Storage.Pickers;
using Windows.Storage;
using System.IO;
using Microsoft.UI.Windowing;
using WinRT.Interop;
using Microsoft.UI;
using Gestor_vehiculos;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using System.Diagnostics;

namespace Vehicle_manager
{
    public sealed partial class MainWindow : Window
    {
        public string cs = "Data Source=";
        public string working_dir;
        private string actual_page = "";
        private string first_name = "";


        public MainWindow()
        {
            this.InitializeComponent();
            ExtendsContentIntoTitleBar = true;
            SystemBackdrop = new MicaBackdrop() 
                        { Kind = MicaKind.BaseAlt };
            
            load_working_dir();
            cs += Path.Join(working_dir, "database.db");
            CreateTableIfNotExists();
        }

        private async void  load_working_dir()
        {
            Windows.Storage.ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            working_dir = (string)localSettings.Values["working_dir"];
            if(working_dir != null)
            {
                return;
            }

            FolderPicker openPicker = new FolderPicker();
            openPicker.ViewMode = PickerViewMode.Thumbnail;
            openPicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            openPicker.FileTypeFilter.Add("*");
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hwnd);
            StorageFolder folder = await openPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                working_dir = folder.Path;
            }
            else
            {
                Close();
            }
            localSettings.Values["working_dir"] = working_dir;
            if(cs.Length<24)
            {
                cs = "Data Source=" + Path.Join(working_dir, "database.db");
            }
            NavView_Loaded(null, null);
        }


        public void load_new_vehicle(string name, string icon_name)
        {
            NavView.MenuItems.Add(new NavigationViewItem
            {
                Content = name,
                Tag = "Vehicle_manager.Vehicle",
                Icon = get_icon(icon_name),
            });
            NavView.SelectedItem = NavView.MenuItems[NavView.MenuItems.Count - 1];
            NavView_Navigate(typeof(Vehicle), new EntranceNavigationTransitionInfo(), name);
        }

        public void delete_vehicle(string matricula)
        {
            NavView.MenuItems.Clear();
            NavView_Loaded(null, null);
        }

        
        private BitmapIcon get_icon(string icon)
        {
            return new BitmapIcon() { UriSource = new Uri("ms-appx:///Assets/" + icon + ".png") };
        }


        public void NavView_Loaded(object sender, RoutedEventArgs e)
        {
            first_name = "";
            FontIcon icon = new FontIcon();
            icon.Glyph = "\uE964";

            using (var connection = new SqliteConnection(cs))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText =
                @"
                    SELECT Marca, Modelo, Matricula, Icon 
                    FROM Vehiculos
                ";
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var name = reader.GetString(0);
                        name += " ";
                        name += reader.GetString(1);
                        name += " ";
                        name += reader.GetString(2);
                        if (string.IsNullOrWhiteSpace(first_name))
                        {
                            first_name = name;
                        }

                        NavView.MenuItems.Add(new NavigationViewItem
                        {
                            Content = name,
                            Tag = "Vehicle_manager.Vehicle",
                            Icon = get_icon(reader.GetString(3))
                        });
                    }
                }
            }

            if (NavView.MenuItems.Count == 0)
            {
                NavView.SelectedItem = NavView.FooterMenuItems[0];
                NavView_Navigate(typeof(AddPage), new EntranceNavigationTransitionInfo(), first_name);
            }
            else
            {
                NavView.SelectedItem = NavView.MenuItems[0];
                NavView_Navigate(typeof(Vehicle), new EntranceNavigationTransitionInfo(), first_name);
            }
        }

        private void NavView_ItemInvoked(NavigationView sender,
                                         NavigationViewItemInvokedEventArgs args)
        {
            if( (args.InvokedItemContainer is null))
            {
                return;
            }
            if (args.InvokedItemContainer.Tag != null)
            {
                if (args.InvokedItemContainer.Tag.Equals("Vehicle_manager.Sync"))
                {
                    sync_other_db();
                    return;
                }

                Type navPageType = Type.GetType(args.InvokedItemContainer.Tag.ToString());
                string new_vehicle_selected = args.InvokedItemContainer.Content.ToString();
                if (!new_vehicle_selected.Equals(actual_page))
                {
                    actual_page = new_vehicle_selected;
                    NavView_Navigate(navPageType, args.RecommendedNavigationTransitionInfo, args.InvokedItemContainer.Content.ToString());
                }
            }
        }

        private async void sync_other_db()
        {
            try { 
                var con = new SqliteConnection(cs);
                con.Open();
                var command = con.CreateCommand();
                command.CommandText =
                @"
                    SELECT Marca, Modelo, Matricula 
                    FROM Vehiculos
                ";
                List<string> actual_vehicles = new List<string>();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var name = reader.GetString(0);
                        name += "||";
                        name += reader.GetString(1);
                        name += "||";
                        name += reader.GetString(2);
                        actual_vehicles.Add(name);
                    }
                }

                FileOpenPicker openPicker = new FileOpenPicker();
                openPicker.ViewMode = PickerViewMode.Thumbnail;
                openPicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
                openPicker.FileTypeFilter.Add("*");
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
                WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hwnd);
                StorageFile file = await openPicker.PickSingleFileAsync();
                string old_dir;
                if (file != null)
                {
                    old_dir = file.Path;
                }
                else
                {
                    return;
                }
                //Esta es la base de datos que se va a integrar
                var con_old = new SqliteConnection("Data Source=" + old_dir);
                con_old.Open();
                var command_old = con_old.CreateCommand();
                command_old.CommandText =
                @"
                    SELECT Matricula, Marca, Modelo , Km, Bastidor, Fabricacion, Archivos
                    FROM Vehiculos
                ";
                using (var reader = command_old.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if(vehicle_exists(actual_vehicles, reader.GetString(1), reader.GetString(2), reader.GetString(0))){
                            continue;
                        }
                        command = con.CreateCommand();
                        command.CommandText = @"
                                INSERT INTO Vehiculos ('Matricula', 'Marca', 'Modelo', 'Km', 'Bastidor', 'Fabricacion', 'Icon', 'Archivos')
                                        Values ($matricula, $marca, $modelo, $km, $bastidor, $fabricacion , $icon, $archivos);
                         ";
                        command.Parameters.AddWithValue("$matricula", reader.GetString(0).ToString());
                        command.Parameters.AddWithValue("$marca", reader.GetString(1).ToString());
                        command.Parameters.AddWithValue("$modelo", reader.GetString(2).ToString());
                        command.Parameters.AddWithValue("$km", reader.GetString(3).ToString());
                        command.Parameters.AddWithValue("$bastidor", reader.GetString(4).ToString());
                        command.Parameters.AddWithValue("$fabricacion", reader.GetString(5).ToString());
                        command.Parameters.AddWithValue("$archivos", reader.GetString(6));
                        command.Parameters.AddWithValue("$icon", "racecar");
                        command.ExecuteNonQuery();
                    }
                }

                command_old = con_old.CreateCommand();
                command_old.CommandText =
                @"
                    SELECT Matricula, Componente, Km, IntervaloKm, Precio, Sitio, Fecha, Hecho, Notas, Archivos
                    FROM Registros
                ";
                List<string> list = new List<string>();
                command = con.CreateCommand();
                command.CommandText =
                @"
                    SELECT Matricula, Componente, Km, IntervaloKm, Fecha
                    FROM Registros
                ";

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var name = reader.GetString(0);
                        name += "||";
                        name += reader.GetString(1);
                        name += "||";
                        name += reader.GetString(2);
                        name += "||";
                        name += reader.GetString(3);
                        name += "||";
                        name += reader.GetString(4);
                        list.Add(name);
                    }
                }

                using (var reader = command_old.ExecuteReader())
                {

                    while (reader.Read())
                    {
                        if(register_exits(list, reader.GetString(0), reader.GetString(1), reader.GetString(6),reader.GetString(2), reader.GetString(3)))
                        {
                            continue;
                        }
                        command = con.CreateCommand();
                        command.CommandText = @"
                                INSERT INTO Registros ('Matricula', 'Componente', 'Km', 'IntervaloKm', 'Precio', 'Sitio', 'Fecha', 'Hecho', 'Notas', 'Archivos')
                                        Values ($matricula, $componente, $km, $intervalokm, $precio, $sitio , $fecha, $hecho, $notas, $archivos);
                            ";
                        command.Parameters.AddWithValue("$matricula", reader.GetString(0).ToString());
                        command.Parameters.AddWithValue("$componente", reader.GetString(1).ToString());
                        command.Parameters.AddWithValue("$km", reader.GetString(2).ToString());
                        command.Parameters.AddWithValue("$intervalokm", reader.GetString(3).ToString());
                        command.Parameters.AddWithValue("$precio", reader.GetString(4).ToString());
                        command.Parameters.AddWithValue("$sitio", reader.GetString(5).ToString());
                        command.Parameters.AddWithValue("$fecha", reader.GetDateTime(6).ToShortDateString());
                        command.Parameters.AddWithValue("$hecho", reader.GetBoolean(7));
                        command.Parameters.AddWithValue("$notas", reader.GetString(8).ToString());
                        command.Parameters.AddWithValue("$archivos", reader.GetString(9).Replace("|", " || "));
                        command.ExecuteNonQuery();
                    }
                }
                con_old.Close();
                con.Close();

            }catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            finally
            {
                NavView.MenuItems.Clear();
                NavView_Loaded(null, null);
            }

        }


        private void NavView_Navigate(
            Type navPageType,
            NavigationTransitionInfo transitionInfo,
            string vehicle)
        {
            Type preNavPageType = ContentFrame.CurrentSourcePageType;

            if (navPageType.Name.ToString().Equals("AddPage"))
            {
                ContentFrame.Navigate(navPageType, this, transitionInfo);
            }
            else
            {
                if (navPageType is not null)
                {
                    if(ContentFrame.BackStack.Count > 0)
                    {
                        ContentFrame.BackStack.Clear();
                    }
                    ContentFrame.Navigate(navPageType, (vehicle, this), transitionInfo);
                }
            }
        }

        private bool vehicle_exists(List<string> actual_vehicles, string marca, string modelo, string matricula)
        {

            foreach(string vehicle in actual_vehicles)
            {
                string[] v = vehicle.Split("||");
                if (v[0].Equals(marca) && v[1].Equals(modelo) && v[2].Equals(matricula))
                {
                    return true;
                }
            }
            return false;
        }

        private bool register_exits(List<string> actual_registers, string matricula, string componente, string fecha, string km, string intervalo_km)
        {
            fecha = DateTime.Parse(fecha).ToShortDateString();
            foreach(string register in actual_registers)
            {
                string[] v = register.Split("||");
                if (v[0].Equals(matricula) && v[1].Equals(componente) && v[2].Equals(km) && v[3].Equals(intervalo_km) && v[4].Equals(fecha))
                {
                    return true;
                }
            }
            return false;
        }

        public async void import_old_tables()
        {
            var con = new SqliteConnection(cs);
            con.Open();
            var command = con.CreateCommand();
            command.CommandText =
            @"
                SELECT Marca, Modelo, Matricula 
                FROM Vehiculos
            ";
            List<string> actual_vehicles = new List<string>();
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    var name = reader.GetString(0);
                    name += "||";
                    name += reader.GetString(1);
                    name += "||";
                    name += reader.GetString(2);
                    actual_vehicles.Add(name);
                }
            }

            FileOpenPicker openPicker = new FileOpenPicker();
            openPicker.ViewMode = PickerViewMode.Thumbnail;
            openPicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            openPicker.FileTypeFilter.Add("*");
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hwnd);
            StorageFile file = await openPicker.PickSingleFileAsync();
            string old_dir;
            if (file != null)
            {
                old_dir = file.Path;
            }
            else
            {
                return;
            }
            var con_old = new SqliteConnection("Data Source=" + old_dir);
            con_old.Open();
            var command_old = con_old.CreateCommand();
            command_old.CommandText =
            @"
                SELECT Matricula, Marca, Modelo , Km, Bastidor, Fabricacion, Archivos
                FROM Vehiculos
            ";
            using (var reader = command_old.ExecuteReader())
            {
                while (reader.Read())
                {
                    if(vehicle_exists(actual_vehicles, reader.GetString(1), reader.GetString(2), reader.GetString(0))){
                        continue;
                    }
                    command = con.CreateCommand();
                    command.CommandText = @"
                            INSERT INTO Vehiculos ('Matricula', 'Marca', 'Modelo', 'Km', 'Bastidor', 'Fabricacion', 'Icon', 'Archivos')
                                    Values ($matricula, $marca, $modelo, $km, $bastidor, $fabricacion , $icon, $archivos);
                     ";
                    command.Parameters.AddWithValue("$matricula", reader.GetString(0).ToString());
                    command.Parameters.AddWithValue("$marca", reader.GetString(1).ToString());
                    command.Parameters.AddWithValue("$modelo", reader.GetString(2).ToString());
                    command.Parameters.AddWithValue("$km", reader.GetString(3).ToString());
                    command.Parameters.AddWithValue("$bastidor", reader.GetString(4).ToString());
                    command.Parameters.AddWithValue("$fabricacion", reader.GetString(5).ToString());
                    command.Parameters.AddWithValue("$archivos", reader.GetString(6).Replace("|", " || "));
                    command.Parameters.AddWithValue("$icon", "racecar");
                    command.ExecuteNonQuery();
                }
            }

            command_old = con_old.CreateCommand();
            command_old.CommandText =
            @"
                SELECT Matricula, Parte, CambioKm, IntervaloKm, Precio, Lugar, Fecha, Hecho, Anotacion, Archivo, Id
                FROM Parametros
            ";

            List<string> list = new List<string>();
            command = con.CreateCommand();
            command.CommandText =
            @"
                SELECT Matricula, Componente, Km, IntervaloKm, Fecha
                FROM Registros
            ";

            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    var name = reader.GetString(0);
                    name += "||";
                    name += reader.GetString(1);
                    name += "||";
                    name += reader.GetString(2);
                    name += "||";
                    name += reader.GetString(3);
                    name += "||";
                    name += reader.GetString(4);
                    list.Add(name);
                }
            }

            using (var reader = command_old.ExecuteReader())
            {

                while (reader.Read())
                {
                    if(register_exits(list, reader.GetString(0), reader.GetString(1), reader.GetString(6),reader.GetString(2), reader.GetString(3)))
                    {
                        continue;
                    }
                    command = con.CreateCommand();
                    command.CommandText = @"
                            INSERT INTO Registros ('Matricula', 'Componente', 'Km', 'IntervaloKm', 'Precio', 'Sitio', 'Fecha', 'Hecho', 'Notas', 'Archivos', 'Id')
                                    Values ($matricula, $componente, $km, $intervalokm, $precio, $sitio , $fecha, $hecho, $notas, $archivos, $id);
                        ";
                    command.Parameters.AddWithValue("$matricula", reader.GetString(0).ToString());
                    command.Parameters.AddWithValue("$componente", reader.GetString(1).ToString());
                    command.Parameters.AddWithValue("$km", reader.GetString(2).ToString());
                    command.Parameters.AddWithValue("$intervalokm", reader.GetString(3).ToString());
                    command.Parameters.AddWithValue("$precio", reader.GetString(4).ToString());
                    command.Parameters.AddWithValue("$sitio", reader.GetString(5).ToString());
                    command.Parameters.AddWithValue("$fecha", reader.GetDateTime(6).ToShortDateString());
                    command.Parameters.AddWithValue("$hecho", Convert.ToBoolean(reader.GetString(7)));
                    command.Parameters.AddWithValue("$notas", reader.GetString(8).ToString());
                    command.Parameters.AddWithValue("$archivos", reader.GetString(9).Replace("|", " || "));
                    command.Parameters.AddWithValue("$id", reader.GetString(10).ToString());
                    command.ExecuteNonQuery();
                }
            }
            con_old.Close();
            con.Close();
        }

        private void CreateTableIfNotExists()
        {
            var con = new SqliteConnection(cs);
            con.Open();
            var command = con.CreateCommand();
            command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Vehiculos (
                        Matricula Text NOT NULL PRIMARY KEY,
                        Marca Text NOT NULL,
                        Modelo Text NOT NULL,
                        Km INTEGER NOT NULL,
                        Bastidor Text NOT NULL,
                        Fabricacion INTEGER NOT NULL,
                        Icon Text Not Null,
                        Archivos TEXT
                    );
                ";
            command.ExecuteNonQuery();

            con.Open();
            command = con.CreateCommand();
            command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Registros (
                        Id INTEGER PRIMARY KEY,
                        Matricula Text NOT NULL,
                        Componente Text NOT NULL,
                        Km INTEGER NOT NULL,
                        IntervaloKm INTEGER NOT NULL,
                        Precio DOUBLE NOT NULL,
                        Sitio TEXT NOT NULL,
                        Fecha date NOT NULL,
                        Hecho BOOLEAN DEFAULT false NOT NULL,
                        Notas Text,
                        Archivos TEXT
                    );
                ";
            command.ExecuteNonQuery();
            //var con_old = new SqliteConnection("Data Source=C:\\Users\\ivan\\Documents\\Proyectos\\Gestor vehiculos v3\\Files\\data.sqlite");
            //con_old.Open();
            //var command_old = con_old.CreateCommand();
            //command_old.CommandText =
            //@"
            //    SELECT Matricula, Marca, Modelo , Km, Bastidor, Fabricacion, Archivos
            //    FROM Vehiculos
            //";
            //using (var reader = command_old.ExecuteReader())
            //{
            //    while (reader.Read())
            //    {
            //        command = con.CreateCommand();
            //        command.CommandText = @"
            //                INSERT INTO Vehiculos ('Matricula', 'Marca', 'Modelo', 'Km', 'Bastidor', 'Fabricacion', 'Icon', 'Archivos')
            //                        Values ($matricula, $marca, $modelo, $km, $bastidor, $fabricacion , $icon, $archivos);
            //            ";
            //        command.Parameters.AddWithValue("$matricula", reader.GetString(0).ToString());
            //        command.Parameters.AddWithValue("$marca", reader.GetString(1).ToString());
            //        command.Parameters.AddWithValue("$modelo", reader.GetString(2).ToString());
            //        command.Parameters.AddWithValue("$km", reader.GetString(3).ToString());
            //        command.Parameters.AddWithValue("$bastidor", reader.GetString(4).ToString());
            //        command.Parameters.AddWithValue("$fabricacion", reader.GetString(5).ToString());
            //        command.Parameters.AddWithValue("$archivos", reader.GetString(6).Replace("|", " || "));
            //        command.Parameters.AddWithValue("$icon", "racecar");
            //        Debug.WriteLine(reader.GetString(2).ToString());
            //        command.ExecuteNonQuery();
            //    }
            //}

            //command_old = con_old.CreateCommand();
            //command_old.CommandText =
            //@"
            //    SELECT Matricula, Parte, CambioKm, IntervaloKm, Precio, Lugar, Fecha, Hecho, Anotacion, Archivo, Id
            //    FROM Parametros
            //";
            //using (var reader = command_old.ExecuteReader())
            //{
            //    while (reader.Read())
            //    {
            //        command = con.CreateCommand();
            //        command.CommandText = @"
            //                INSERT INTO Registros ('Matricula', 'Componente', 'Km', 'IntervaloKm', 'Precio', 'Sitio', 'Fecha', 'Hecho', 'Notas', 'Archivos', 'Id')
            //                        Values ($matricula, $componente, $km, $intervalokm, $precio, $sitio , $fecha, $hecho, $notas, $archivos, $id);
            //            ";
            //        command.Parameters.AddWithValue("$matricula", reader.GetString(0).ToString());
            //        command.Parameters.AddWithValue("$componente", reader.GetString(1).ToString());
            //        command.Parameters.AddWithValue("$km", reader.GetString(2).ToString());
            //        command.Parameters.AddWithValue("$intervalokm", reader.GetString(3).ToString());
            //        command.Parameters.AddWithValue("$precio", reader.GetString(4).ToString());
            //        command.Parameters.AddWithValue("$sitio", reader.GetString(5).ToString());
            //        command.Parameters.AddWithValue("$fecha", reader.GetDateTime(6).ToShortDateString());
            //        command.Parameters.AddWithValue("$hecho", Convert.ToBoolean(reader.GetString(7)));
            //        command.Parameters.AddWithValue("$notas", reader.GetString(8).ToString());
            //        command.Parameters.AddWithValue("$archivos", reader.GetString(9).Replace("|", " || "));
            //        command.Parameters.AddWithValue("$id", reader.GetString(10).ToString());
            //        Debug.WriteLine(reader.GetString(2).ToString());
            //        command.ExecuteNonQuery();
            //    }
            //}
            //con_old.Close();
            con.Close();
        }
    }
}
