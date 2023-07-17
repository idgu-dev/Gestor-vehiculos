// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

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
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.Data.Sqlite;
using Windows.Storage.Pickers;
using Windows.Storage;
using WinRT.Interop;
using Gestor_vehiculos;
using System.Linq;
using ctWinUI = CommunityToolkit.WinUI.UI.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using CommunityToolkit.WinUI.UI.Controls;
using Microsoft.UI;
using Windows.UI;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Vehicle_manager
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Vehicle : Page
    {
        private string matricula;
        public MainWindow mainWindow { get; set; }
        private string working_dir = "C:\\Users\\ivan\\Documents\\Proyectos\\Gestor vehiculos v3\\Files";
        private string cs;
        private List<string> componentes_unique = new List<string>();
        private List<string> sitio_unique = new List<string>();
        private List<string> suggestion_source = new List<string>();
        private List<Vehicle_register> registros = new List<Vehicle_register>();
        

        public Vehicle()
        {
            this.InitializeComponent();
        }


        private void semafoto_and_restantes(List<Vehicle_register> l)
        {
            //Seguro que hay otra forma mas optima pero esto funciona jiji
            List<Vehicle_register> rojos = new List<Vehicle_register>();
            List<Vehicle_register> amarillos = new List<Vehicle_register>();
            List<Vehicle_register> resto = new List<Vehicle_register>();
            foreach (Vehicle_register v in l)
            {
                int km_restantes = v.IntervaloKm + v.Km - Convert.ToInt32(textbox_km.Text);
                if(km_restantes<0)
                {
                    rojos.Add(new Vehicle_register(v.Componente, v.Km, v.IntervaloKm, v.Precio, v.Sitio, v.Fecha.ToShortDateString(), v.Hecho,
                        v.Notas, v.Archivos, v.Id, "red", km_restantes));
                }
                else
                {
                    if(km_restantes < v.IntervaloKm * 0.19)
                    {
                        amarillos.Add(new Vehicle_register(v.Componente, v.Km, v.IntervaloKm, v.Precio, v.Sitio, v.Fecha.ToShortDateString(), v.Hecho,
                            v.Notas, v.Archivos, v.Id, "yellow", km_restantes));
                    }
                    else
                    {
                        resto.Add(new Vehicle_register(v.Componente, v.Km, v.IntervaloKm, v.Precio, v.Sitio, v.Fecha.ToShortDateString(), v.Hecho,
                            v.Notas, v.Archivos, v.Id, "", km_restantes));
                    }
                }
            }
            rojos.AddRange(amarillos);
            rojos.AddRange(resto);

            dataGrid.ItemsSource = rojos;
        }

        private void load_data_grid()
        {
            List<Vehicle_register> list = new List<Vehicle_register>();
            var con = new SqliteConnection(cs);
            con.Open();
            var command = con.CreateCommand();
            command.CommandText =
            @"
                SELECT Componente, Km, IntervaloKm, Precio, Sitio, Fecha, Hecho, Notas, Archivos, Id
                FROM Registros
                Where Matricula=$m
            ";

            if (ToggleSwitch_historial.IsOn == false)
            {
                command.CommandText += " AND Hecho=false";
            }
            else
            {
                command.CommandText += " AND Hecho=True";
            }

            command.Parameters.AddWithValue("$m", matricula);

            string notas;
            string archivos;
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    string x = reader.GetValue(0).ToString();

                    if (reader.IsDBNull(7))
                    {
                        notas = ""; 
                    }
                    else
                    {
                        notas = reader.GetString(7);
                    }
                    if (reader.IsDBNull(8))
                    {
                        archivos = "";
                    }
                    else
                    {
                        archivos = reader.GetString(8);
                    }
                    list.Add(new Vehicle_register(reader.GetString(0), 
                        reader.GetInt32(1), reader.GetInt32(2), reader.GetDouble(3), 
                        reader.GetString(4), reader.GetString(5), reader.GetBoolean(6),
                        notas, archivos, reader.GetInt32(9)
                        ));
                }
            }
            //for (int i = 0; i < 100000; i++)
            //{
            //    con = new SqliteConnection(cs);
            //    con.Open();
            //    command = con.CreateCommand();
            //    command.CommandText = @"
            //            INSERT INTO Registros ('Matricula', 'Componente', 'Km', 'IntervaloKm', 'Precio', 'Sitio', 'Fecha', 'Hecho', 'Notas')
            //                    Values ($matricula, $componente, $km, $intervalokm, $precio, $sitio , $fecha, true, $notas);
            //        ";
            //    command.Parameters.AddWithValue("$matricula", matricula);
            //    command.Parameters.AddWithValue("$componente", "componete" + i.ToString());
            //    command.Parameters.AddWithValue("$km", 1000000);
            //    command.Parameters.AddWithValue("$intervalokm", 22222);
            //    command.Parameters.AddWithValue("$precio", 60);
            //    command.Parameters.AddWithValue("$sitio", "sitio de ejemplo" + i.ToString());
            //    command.Parameters.AddWithValue("$fecha", "2022/01/01");
            //    command.Parameters.AddWithValue("$notas", "nota de mierda " + i.ToString());
            //    var result = command.ExecuteNonQuery();

            //}
            if (ToggleSwitch_historial.IsOn == false)
            {
                dataGrid.Columns[4].Visibility = Visibility.Visible;
                semafoto_and_restantes(list);
            }
            else
            {
                dataGrid.Columns[4].Visibility = Visibility.Collapsed;
                dataGrid.ItemsSource = new List<Vehicle_register>(from item in list
                                                                    orderby item.Fecha descending
                                                                    select item);
                dataGrid.Columns[7].SortDirection = DataGridSortDirection.Descending;
                registros = list;
            }
            fill_suggestion_sources();
        }

         protected override void OnNavigatedTo(NavigationEventArgs e)
         {
            ValueTuple<String, MainWindow> t = (ValueTuple<String, MainWindow>)e.Parameter;
            string myData = t.Item1;
            string[] datasplit = myData.Split(' ');
            matricula = datasplit[datasplit.Length - 1];
            string marca_modelo = "";
            for (int i = 0; i < datasplit.Length-1; i++) {
                marca_modelo += datasplit[i];
                marca_modelo += " ";
            }
            info_panel_marca_modelo.Text = marca_modelo;
            info_panel_matricula.Text = matricula;

            mainWindow = (MainWindow)t.Item2;
            cs = mainWindow.cs;
            working_dir = mainWindow.working_dir;

            using (var connection = new SqliteConnection(cs))
            {
                connection.Open();

                var command = connection.CreateCommand();
                command.CommandText =
                @"
                    SELECT Bastidor, Fabricacion, Km, Archivos 
                    FROM Vehiculos
                    Where Matricula=$m
                ";
                command.Parameters.AddWithValue("$m", matricula);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        info_panel_bastidor.Text = reader.GetString(0);
                        info_panel_ano.Text = reader.GetString(1);
                        textbox_km.Text = reader.GetString(2);
                        textbox_km_componente.Text = reader.GetString(2);
                        add_files_to_flyout(reader.GetString(3));
                    }
                }
            }
            load_data_grid();
            base.OnNavigatedTo(e);
        }

        private void add_files_to_flyout(string files)
        {
            if (string.IsNullOrEmpty(files))
            {
                return;
            }
            string[] files_split = files.Split("|| ");
            files_split = files_split.Skip(1).ToArray(); 
            foreach (string file in files_split)
            {
                MenuFlyoutItem m = new MenuFlyoutItem();
                m.Text = file;
                m.Click += open_file;
                menuflyout_files.Items.Add(m);
            }
        }

        private async void open_file(object sender, RoutedEventArgs e)
        {
            MenuFlyoutItem m = (MenuFlyoutItem)e.OriginalSource;
            string vehicle_directory = Path.Join(working_dir, matricula, m.Text);
            StorageFile f = await StorageFile.GetFileFromPathAsync(vehicle_directory);
            _ = Windows.System.Launcher.LaunchFileAsync(f);
        }


        private async void buton_delete_Click(object sender, RoutedEventArgs e)
        {
            ContentDialog dialog = new ContentDialog();
            dialog.XamlRoot = this.XamlRoot;
            dialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
            dialog.Title = "WARNING!";
            dialog.Content = "¿Estás seguro de eliminar este vehículo?";
            dialog.PrimaryButtonText = "Si";
            dialog.CloseButtonText = "No";
            dialog.DefaultButton = ContentDialogButton.Close;

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                var results = 0;
                using (var connection = new SqliteConnection(cs))
                {
                    connection.Open();

                    var command = connection.CreateCommand();
                    command.CommandText =
                    @"
                        DELETE FROM Vehiculos
                        WHERE $matricula=matricula;
                    ";
                    command.Parameters.AddWithValue("$matricula", matricula);
                    results = command.ExecuteNonQuery();
                }
                if (results == 1)
                {
                    string vehicle_directory = Path.Join(working_dir, matricula);
                    if (Directory.Exists(vehicle_directory)) { 
                        Directory.Delete(vehicle_directory , true);
                    }
                    mainWindow.delete_vehicle(matricula);
                }
            }
        }
        private void show_error_dialog(string message)
        {
            ContentDialog dialog = new ContentDialog();
            dialog.XamlRoot = this.XamlRoot;
            dialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
            dialog.Title = "Error modificando el vehiculo";
            dialog.Content = message;
            dialog.CloseButtonText = "Ok";
            dialog.DefaultButton = ContentDialogButton.Close;

            var result = dialog.ShowAsync();
        }

        private void buton_update_km_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var connection = new SqliteConnection(cs))
                {
                    connection.Open();

                    var command = connection.CreateCommand();
                    command.CommandText =
                    @"
                        Update Vehiculos 
                        Set Km = $km
                        Where Matricula=$m
                    ";
                    command.Parameters.AddWithValue("$km", Convert.ToDouble(textbox_km.Text));
                    command.Parameters.AddWithValue("$m", matricula);
                    command.ExecuteNonQuery();
                    load_data_grid();
                }
            }
            catch (Exception ex) 
            {
                if(ex.ToString().ToLower().Contains("input string was not"))
                {
                    show_error_dialog("Formato incorrecto");
                }
                else
                {
                    show_error_dialog(ex.Message.ToString());
                }
            }
        }

        private async void save_file(StorageFile file)
        {
            try
            {
                string vehicle_directory = Path.Join(working_dir, matricula);
                if (!Directory.Exists(vehicle_directory))
                {
                    Directory.CreateDirectory(vehicle_directory);
                }
                var destinationFolder = await StorageFolder.GetFolderFromPathAsync(vehicle_directory);
                await file.CopyAsync(destinationFolder);
            } catch(Exception ex)
            {
                if (ex.Message.Contains("No se puede crear un archivo"))
                {
                    show_error_dialog("No se puede crear el fichero");  
                }
            }
        }
        private async void save_file_row(StorageFile file, string id)
        {
            try
            {
                string vehicle_directory = Path.Join(working_dir, matricula, id);
                if (!Directory.Exists(vehicle_directory))
                {
                    Directory.CreateDirectory(vehicle_directory);
                }
                var destinationFolder = await StorageFolder.GetFolderFromPathAsync(vehicle_directory);
                await file.CopyAsync(destinationFolder);

            } catch(Exception ex)
            {
                if (ex.Message.Contains("No se puede crear un archivo"))
                {
                    show_error_dialog("No se puede crear el fichero");  
                }

            }
        }


        private void update_vehicle_files(string file_name)
        {
            using (var connection = new SqliteConnection(cs))
            {
                string files = "";
                connection.Open();

                var command = connection.CreateCommand();
                command.CommandText =
                @"
                    SELECT Archivos
                    FROM Vehiculos
                    Where Matricula=$m
                ";
                command.Parameters.AddWithValue("$m", matricula);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        files = reader.GetString(0);
                    }
                }

                if (files.Contains(file_name))
                {
                    return;
                }
                files += "|| " + file_name;

                command = connection.CreateCommand();
                command.CommandText =
                @"
                    Update Vehiculos 
                    Set Archivos = $files
                    Where Matricula=$m
                ";
                command.Parameters.AddWithValue("$files", files);
                command.Parameters.AddWithValue("$m", matricula);
                command.ExecuteNonQuery();
                connection.Close(); 
            }
        }

        private async void MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            FileOpenPicker openPicker = new FileOpenPicker();
            openPicker.ViewMode = PickerViewMode.Thumbnail;
            openPicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            openPicker.FileTypeFilter.Add("*");
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this.mainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hwnd);
            // Open the picker for the user to pick a file
            StorageFile file = await openPicker.PickSingleFileAsync();
            if (file != null)
            {
                MenuFlyoutItem m = new MenuFlyoutItem();
                m.Text = file.Name.ToString();
                m.Click += open_file;
                bool has_item = false;
                foreach(MenuFlyoutItem item in menuflyout_files.Items)
                {
                    if (item.Text.Equals(m.Text))
                    {
                        has_item = true;
                        break;
                    }
                }
                if (!has_item)
                {
                    menuflyout_files.Items.Add(m);
                }
                save_file(file);
                update_vehicle_files(m.Text);
            }
            else
            {
                return;
            }
        }

        private void button_add_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var con = new SqliteConnection(cs);
                con.Open();
                var command = con.CreateCommand();
                command.CommandText = @"
                        INSERT INTO Registros ('Matricula', 'Componente', 'Km', 'IntervaloKm', 'Precio', 'Sitio', 'Fecha', 'Hecho', 'Notas')
                                Values ($matricula, $componente, $km, $intervalokm, $precio, $sitio , $fecha, false, $notas);
                    ";
                command.Parameters.AddWithValue("$matricula", matricula);
                command.Parameters.AddWithValue("$componente", textbox_componente.Text);
                command.Parameters.AddWithValue("$km", textbox_km_componente.Text);
                command.Parameters.AddWithValue("$intervalokm", textbox_intervalo.Text);
                command.Parameters.AddWithValue("$precio", textbox_precio.Text);
                command.Parameters.AddWithValue("$sitio", textbox_sitio.Text);
                command.Parameters.AddWithValue("$fecha", datepicker_row.Date.DateTime.ToShortDateString());
                command.Parameters.AddWithValue("$notas", textbox_anotacion.Text);
                var result = command.ExecuteNonQuery();
                if(result == 1)
                {
                    flyout_button_add.Flyout.Hide();
                    textbox_componente.Text = "";
                    textbox_km_componente.Text = textbox_km.Text;
                    textbox_intervalo.Text = "";
                    textbox_precio.Text = "";
                    textbox_sitio.Text = "";
                    textbox_anotacion.Text = "";
                    datepicker_row.Date = DateTimeOffset.Now;
                    //dataGrid.ItemsSource += new Vehicle_register(matricula, textbox_componente.Text,
                    //    textbox_km_componente, textbox_intervalo, textbox_precio.Text, textbox_sitio.Text, 
                    //    datepicker_row.Date, false, textbox_anotacion.Text, "");
                    load_data_grid();
                }
            }catch (Exception ex)
            {
                show_error_dialog(ex.Message);
            }
        }

        private void MenuFlyoutItem_Click_1(object sender, RoutedEventArgs e)
        {

        }

        private void MenuFlyoutItem_Click_2(object sender, RoutedEventArgs e)
        {

        }

        private void MenuFlyoutItem_Click_3(object sender, RoutedEventArgs e)
        {

        }

        private void SearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (string.IsNullOrEmpty(SearchBox.Text))
            {
                return;
            }

            List<Vehicle_register> list = new List<Vehicle_register>();
            var con = new SqliteConnection(cs);
            con.Open();

            var command = con.CreateCommand();
            command.CommandText =
            @"
                SELECT Componente, Km, IntervaloKm, Precio, Sitio, Fecha, Hecho, Notas, Archivos, Id
                FROM Registros
                Where Matricula=$m AND (Componente LIKE $busqueda OR Sitio LIKE $busqueda OR Notas LIKE $busqueda OR Archivos LIKE $busqueda)
            ";
            if (ToggleSwitch_historial.IsOn == false)
            {
                command.CommandText += " AND Hecho=false";
            }
            else
            {
                command.CommandText += " AND Hecho=True";
            }
            command.Parameters.AddWithValue("$m", matricula);
            command.Parameters.AddWithValue("$busqueda", "%"+SearchBox.Text + "%");

            string notas;
            string archivos;
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    string x = reader.GetValue(0).ToString();

                    if (reader.IsDBNull(7))
                    {
                        notas = ""; 
                    }
                    else
                    {
                        notas = reader.GetString(7);
                    }
                    if (reader.IsDBNull(8))
                    {
                        archivos = "";
                    }
                    else
                    {
                        archivos = reader.GetString(8);
                    }
                    list.Add(new Vehicle_register(reader.GetString(0), 
                        reader.GetInt32(1), reader.GetInt32(2), reader.GetDouble(3), 
                        reader.GetString(4), reader.GetString(5), reader.GetBoolean(6),
                        notas, archivos, reader.GetInt32(9)
                        ));
                }
            }
            if (ToggleSwitch_historial.IsOn == false)
            {
                dataGrid.Columns[4].Visibility = Visibility.Visible;
                semafoto_and_restantes(list);
            }
            else
            {
                dataGrid.Columns[4].Visibility = Visibility.Collapsed;
                dataGrid.ItemsSource = new List<Vehicle_register>(from item in list
                                                                    orderby item.Fecha descending
                                                                    select item);
                dataGrid.Columns[7].SortDirection = DataGridSortDirection.Descending;
                registros = list;
            }
        }


        private void sort_column(ctWinUI.DataGridColumnEventArgs e)
        {
            if (e.Column.Header == null)
            {
                return;
            }

            if (e.Column.Header.ToString().Equals("Componente"))
            {
                if (e.Column.SortDirection == null || e.Column.SortDirection == DataGridSortDirection.Descending)
                {
                    dataGrid.ItemsSource = new List<Vehicle_register>(from item in registros
                                                                        orderby item.Componente ascending
                                                                        select item);
                    clean_sorting_headers();
                    e.Column.SortDirection = DataGridSortDirection.Ascending;
                }
                else
                {
                    dataGrid.ItemsSource = new List<Vehicle_register>(from item in registros
                                                                        orderby item.Componente descending
                                                                        select item);
                    clean_sorting_headers();
                    e.Column.SortDirection = DataGridSortDirection.Descending;
                }
                return;
            }
            if (e.Column.Header.ToString().Equals("Km"))
            {
                if (e.Column.SortDirection == null || e.Column.SortDirection == DataGridSortDirection.Descending)
                {
                    dataGrid.ItemsSource = new List<Vehicle_register>(from item in registros
                                                                        orderby item.Km ascending
                                                                        select item);
                    clean_sorting_headers();
                    e.Column.SortDirection = DataGridSortDirection.Ascending;
                }
                else
                {
                    dataGrid.ItemsSource = new List<Vehicle_register>(from item in registros
                                                                        orderby item.Km descending
                                                                        select item);
                    clean_sorting_headers();
                    e.Column.SortDirection = DataGridSortDirection.Descending;
                }
                return;
            }
            if (e.Column.Header.ToString().Equals("IntervaloKm"))
            {
                if (e.Column.SortDirection == null || e.Column.SortDirection == DataGridSortDirection.Descending)
                {
                    dataGrid.ItemsSource = new List<Vehicle_register>(from item in registros
                                                                        orderby item.IntervaloKm ascending
                                                                        select item);
                    clean_sorting_headers();
                    e.Column.SortDirection = DataGridSortDirection.Ascending;
                }
                else
                {
                    dataGrid.ItemsSource = new List<Vehicle_register>(from item in registros
                                                                        orderby item.IntervaloKm descending
                                                                        select item);
                    clean_sorting_headers();
                    e.Column.SortDirection = DataGridSortDirection.Descending;
                }
                return;
            }
            if (e.Column.Header.ToString().Equals("Precio"))
            {
                if (e.Column.SortDirection == null || e.Column.SortDirection == DataGridSortDirection.Descending)
                {
                    dataGrid.ItemsSource = new List<Vehicle_register>(from item in registros
                                                                        orderby item.Precio ascending
                                                                        select item);
                    clean_sorting_headers();
                    e.Column.SortDirection = DataGridSortDirection.Ascending;
                }
                else
                {
                    dataGrid.ItemsSource = new List<Vehicle_register>(from item in registros
                                                                        orderby item.Precio descending
                                                                        select item);
                    clean_sorting_headers();
                    e.Column.SortDirection = DataGridSortDirection.Descending;
                }
                return;
            }
            if (e.Column.Header.ToString().Equals("Sitio"))
            {
                if (e.Column.SortDirection == null || e.Column.SortDirection == DataGridSortDirection.Descending)
                {
                    dataGrid.ItemsSource = new List<Vehicle_register>(from item in registros
                                                                        orderby item.Sitio ascending
                                                                        select item);
                    clean_sorting_headers();
                    e.Column.SortDirection = DataGridSortDirection.Ascending;
                }
                else
                {
                    dataGrid.ItemsSource = new List<Vehicle_register>(from item in registros
                                                                        orderby item.Sitio descending
                                                                        select item);
                    clean_sorting_headers();
                    e.Column.SortDirection = DataGridSortDirection.Descending;
                }
                return;
            }
            if (e.Column.Header.ToString().Equals("Fecha"))
            {
                if (e.Column.SortDirection == null || e.Column.SortDirection == DataGridSortDirection.Descending)
                {
                    dataGrid.ItemsSource = new List<Vehicle_register>(from item in registros
                                                                        orderby item.Fecha ascending
                                                                        select item);
                    clean_sorting_headers();
                    e.Column.SortDirection = DataGridSortDirection.Ascending;
                }
                else
                {
                    dataGrid.ItemsSource = new List<Vehicle_register>(from item in registros
                                                                        orderby item.Fecha descending
                                                                        select item);
                    clean_sorting_headers();
                    e.Column.SortDirection = DataGridSortDirection.Descending;
                }
                return;
            }
            if (e.Column.Header.ToString().Equals("Notas"))
            {
                if (e.Column.SortDirection == null || e.Column.SortDirection == DataGridSortDirection.Descending)
                {
                    dataGrid.ItemsSource = new List<Vehicle_register>(from item in registros
                                                                        orderby item.Notas ascending
                                                                        select item);
                    clean_sorting_headers();
                    e.Column.SortDirection = DataGridSortDirection.Ascending;
                }
                else
                {
                    dataGrid.ItemsSource = new List<Vehicle_register>(from item in registros
                                                                        orderby item.Notas descending
                                                                        select item);
                    clean_sorting_headers();
                    e.Column.SortDirection = DataGridSortDirection.Descending;
                }
                return;
            }
            if (e.Column.Header.ToString().Equals("Archivos") || e.Column.Header.ToString().Equals(""))
            {
                clean_sorting_headers();
                return;
            }
        }

        private void clean_sorting_headers()
        {
             foreach (var dgColumn in   dataGrid.Columns)
             {
                dgColumn.SortDirection = null;
             }
        }

        private void DataGrid_Sorting(object sender, DataGridColumnEventArgs e)
        {
            sort_column(e);
        }

        private void DataGrid_LoadingRowGroup(object sender, ctWinUI.DataGridRowGroupHeaderEventArgs e)
        {

        }

        private void AppBarButton_Click_1(object sender, RoutedEventArgs e)
        {

        }



        private void search_suggestion(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args, List<string> data_source)
        {
            if(args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                var suitableItems = new List<string>();
                var splitText = sender.Text.ToLower().Split(" ");
                foreach(var cat in data_source)
                {
                    var found = splitText.All((key)=>
                    {
                        return cat.ToLower().Contains(key);
                    });
                    if(found)
                    {
                        suitableItems.Add(cat);
                    }
                }
                if(suitableItems.Count == 0)
                {
                    suitableItems.Add("Sin resultados");
                }
                sender.ItemsSource = suitableItems;
            }

        }

        private void textbox_componente_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            search_suggestion(sender, args, componentes_unique);
        }

        private void textbox_componente_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            textbox_componente.Text = args.SelectedItem.ToString();
        }

        private void fill_suggestion_sources()
        {
            componentes_unique.Clear();
            sitio_unique.Clear();
            suggestion_source.Clear();
            using (var connection = new SqliteConnection(cs))
            {
                connection.Open();

                var command = connection.CreateCommand();
                command.CommandText =
                @"
                    SELECT DISTINCT Componente
                    FROM Registros
                ";
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        componentes_unique.Add(reader.GetString(0));
                    }
                }
                command = connection.CreateCommand();
                command.CommandText =
                @"
                    SELECT DISTINCT Componente
                    FROM Registros
                    WHERE Matricula=$m;
                ";
                command.Parameters.AddWithValue("$m", matricula);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        suggestion_source.Add(reader.GetString(0));
                    }
                }

                command = connection.CreateCommand();
                command.CommandText =
                @"
                    SELECT DISTINCT Sitio
                    FROM Registros
                ";
                command.Parameters.AddWithValue("$m", matricula);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        sitio_unique.Add(reader.GetString(0));
                    }
                }
                command = connection.CreateCommand();
                command.CommandText =
                @"
                    SELECT DISTINCT Sitio
                    FROM Registros
                    WHERE Matricula=$m;
                ";
                command.Parameters.AddWithValue("$m", matricula);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        suggestion_source.Add(reader.GetString(0));
                    }
                }
                connection.Close();
            }
        }


        private void textbox_sitio_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            search_suggestion(sender, args, sitio_unique);
        }

        private void textbox_sitio_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            textbox_sitio.Text = args.SelectedItem.ToString();
        }

        private void dropdownbutton_archivos_row_Click(object sender, RoutedEventArgs e)
        {

        }

        private void AppBarButton_Click_2(object sender, RoutedEventArgs e)
        {
            load_data_grid();
            SearchBox.Text = "";
             foreach (var dgColumn in dataGrid.Columns)
             {
                dgColumn.SortDirection = null;
             }
        }


        private async void AppBarButton_Click_3(object sender, RoutedEventArgs e)
        {
            if(dataGrid.SelectedIndex == -1)
            {
                return;
            }

            ContentDialog dialog = new ContentDialog();
            dialog.XamlRoot = this.XamlRoot;
            dialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
            dialog.Title = "WARNING!";
            dialog.Content = "¿Estás seguro de eliminar esta fila?";
            dialog.PrimaryButtonText = "Si";
            dialog.CloseButtonText = "No";
            dialog.DefaultButton = ContentDialogButton.Close;

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                var results = 0;
                using (var connection = new SqliteConnection(cs))
                {
                    connection.Open();
                    foreach(Vehicle_register r in dataGrid.SelectedItems)
                    {
                        var command = connection.CreateCommand();
                        command.CommandText =
                        @"
                            DELETE FROM Registros
                            WHERE $matricula=matricula AND $Id=Id;
                        ";
                        command.Parameters.AddWithValue("$matricula", matricula);
                        command.Parameters.AddWithValue("$Id", r.Id);
                        results = command.ExecuteNonQuery();
                        if (results == 1)
                        {
                            string vehicle_directory = Path.Join(working_dir, matricula, r.Id.ToString());
                            if (Directory.Exists(vehicle_directory)) { 
                                Directory.Delete(vehicle_directory , true);
                            }
                        }
                    }
                    load_data_grid();
                }
            }
        }

        private void dataGrid_CellEditEnded(object sender, ctWinUI.DataGridCellEditEndedEventArgs e)
        {
            Vehicle_register r = (Vehicle_register)dataGrid.SelectedItem;
            Debug.WriteLine(r.Id.ToString()); 
            Debug.WriteLine(r.Componente.ToString());
            using (var connection = new SqliteConnection(cs))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText =
                @"
                    UPDATE Registros SET 
                    Componente=$componente,
                    Km=$km,
                    IntervaloKm=$intervalo_km,
                    Precio=$precio,
                    Sitio=$sitio,
                    Fecha=$fecha,
                    Hecho=$hecho,
                    Notas=$notas,
                    Archivos=$archivos
                    WHERE Id=$id;
                ";
                command.Parameters.AddWithValue("$id", r.Id);
                command.Parameters.AddWithValue("$componente", r.Componente);
                command.Parameters.AddWithValue("$km", r.Km);
                command.Parameters.AddWithValue("$intervalo_km", r.IntervaloKm);
                command.Parameters.AddWithValue("$precio", r.Precio);
                command.Parameters.AddWithValue("$sitio", r.Sitio);
                command.Parameters.AddWithValue("$fecha", r.Fecha);
                command.Parameters.AddWithValue("$hecho", r.Hecho);
                command.Parameters.AddWithValue("$notas", r.Notas);
                command.Parameters.AddWithValue("$archivos", r.Archivos);
                command.ExecuteNonQuery();
            }

        }

        private void datepicker_row_Click(object sender, RoutedEventArgs e)
        {

        }

        private void button_group_by_sitio_Click(object sender, RoutedEventArgs e)
        {

        }

        private void button_group_by_componente_Click(object sender, RoutedEventArgs e)
        {

        }

        private void buton_search_componente_sitio_Click(object sender, RoutedEventArgs e)
        {
        }

        private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            search_suggestion(sender, args, suggestion_source);

        }

        private void SearchBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            SearchBox.Text = args.SelectedItem.ToString();
        }

        private string add_file_to_row(string id, string file_name)
        {
            using (var connection = new SqliteConnection(cs))
            {
                string files = "";
                connection.Open();

                var command = connection.CreateCommand();
                command.CommandText =
                @"
                    SELECT Archivos
                    FROM Registros
                    Where Id=$id
                ";
                command.Parameters.AddWithValue("$id", id);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (reader.IsDBNull(0))
                        {
                            break;
                        }
                        files = reader.GetString(0);
                    }
                }

                if(files.Contains(file_name))
                {
                    return files;
                }
                files += "|| " + file_name;

                command = connection.CreateCommand();
                command.CommandText =
                @"
                    Update Registros 
                    Set Archivos = $files
                    Where Id=$id
                ";
                command.Parameters.AddWithValue("$files", files);
                command.Parameters.AddWithValue("$id", id);
                command.ExecuteNonQuery();
                connection.Close();

                return files;
            }
        }

        private async void button_add_file_row_Click(object sender, RoutedEventArgs e)
        {
            FileOpenPicker openPicker = new FileOpenPicker();
            openPicker.ViewMode = PickerViewMode.Thumbnail;
            openPicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            openPicker.FileTypeFilter.Add("*");
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this.mainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hwnd);
            // Open the picker for the user to pick a file
            StorageFile file = await openPicker.PickSingleFileAsync();
            if (file != null)
            {
                Vehicle_register r = (Vehicle_register)dataGrid.SelectedItem;
                save_file_row(file, r.Id.ToString());
                string files = add_file_to_row(r.Id.ToString(), file.Name.ToString());
                r.Archivos = files;
            }
            else
            {
                return;
            }

        }

        private void dropdownbutton_archivos_row_Click_1(object sender, RoutedEventArgs e)
        {
            DropDownButton b = (DropDownButton)sender;
            Vehicle_register r = (Vehicle_register)b.DataContext;
            string files = r.Archivos;
            if (string.IsNullOrEmpty(files))
            {
                return;
            }
            string[] files_split = files.Split("|| ");
            files_split = files_split.Skip(1).ToArray(); 
            MenuFlyout menuFlyout = (MenuFlyout)b.Flyout;
            foreach (string file in files_split)
            {
                MenuFlyoutItem m = new MenuFlyoutItem();
                m.Text = file;
                m.Click += open_file;
                bool has_item = false;
                foreach(MenuFlyoutItem item in menuFlyout.Items)
                {
                    if (item.Text.Equals(m.Text))
                    {
                        has_item = true;
                        break;
                    }
                }
                if (!has_item)
                {
                    menuFlyout.Items.Add(m);
                }
            }
        }

        private void buton_filter_Click(object sender, RoutedEventArgs e)
        {
            List<Vehicle_register> list = new List<Vehicle_register>();
            var con = new SqliteConnection(cs);
            con.Open();

            var command = con.CreateCommand();
            command.CommandText =
            @"
                SELECT Componente, Km, IntervaloKm, Precio, Sitio, Fecha, Hecho, Notas, Archivos, Id
                FROM Registros
                Where (Matricula=$m AND 
            ";

            command.CommandText += combobox_columna_filter.SelectedItem + " ";
            command.CommandText += combobox_type_filter.SelectedItem + " ";
            command.CommandText += textbox_filter_value.Text + ")";
            if (ToggleSwitch_historial.IsOn == false)
            {
                command.CommandText += " AND Hecho=false";
            }
            else
            {
                command.CommandText += " AND Hecho=true";
            }
            command.Parameters.AddWithValue("$m", matricula);
            //command.Parameters.AddWithValue("$comparacion", combobox_type_filter.SelectedItem);

            string notas;
            string archivos;
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    string x = reader.GetValue(0).ToString();

                    if (reader.IsDBNull(7))
                    {
                        notas = ""; 
                    }
                    else
                    {
                        notas = reader.GetString(7);
                    }
                    if (reader.IsDBNull(8))
                    {
                        archivos = "";
                    }
                    else
                    {
                        archivos = reader.GetString(8);
                    }
                    list.Add(new Vehicle_register(reader.GetString(0), 
                        reader.GetInt32(1), reader.GetInt32(2), reader.GetDouble(3), 
                        reader.GetString(4), reader.GetString(5), reader.GetBoolean(6),
                        notas, archivos, reader.GetInt32(9)
                        ));
                }
            }
            if (ToggleSwitch_historial.IsOn == false)
            {
                dataGrid.Columns[4].Visibility = Visibility.Visible;
                semafoto_and_restantes(list);
            }
            else
            {
                dataGrid.Columns[4].Visibility = Visibility.Collapsed;
                dataGrid.ItemsSource = new List<Vehicle_register>(from item in list
                                                                    orderby item.Fecha descending
                                                                    select item);
                dataGrid.Columns[7].SortDirection = DataGridSortDirection.Descending;
                registros = list;
            }
        }

        private void checkbox_row_Checked(object sender, RoutedEventArgs e)
        {

            

        }

        private void checkbox_row_Unchecked(object sender, RoutedEventArgs e)
        {

        }

        private void button_done_row_Click(object sender, RoutedEventArgs e)
        {
            Button c = (Button)sender;
            Vehicle_register r = (Vehicle_register)c.DataContext;
            if (r.Hecho)
            {
                return;
            }
            r.Color_row = "";
            using (var connection = new SqliteConnection(cs))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText =
                @"
                    UPDATE Registros SET 
                    Hecho=true
                    WHERE Id=$id;
                ";
                command.Parameters.AddWithValue("$id", r.Id);
                command.ExecuteNonQuery();

                command = connection.CreateCommand();
                command.CommandText = @"
                        INSERT INTO Registros ('Matricula', 'Componente', 'Km', 'IntervaloKm', 'Precio', 'Sitio', 'Fecha', 'Hecho', 'Notas')
                                Values ($matricula, $componente, $km, $intervalokm, $precio, $sitio , $fecha, false, $notas);
                    ";
                command.Parameters.AddWithValue("$matricula", matricula);
                command.Parameters.AddWithValue("$componente", r.Componente);
                command.Parameters.AddWithValue("$km", textbox_km.Text);
                command.Parameters.AddWithValue("$intervalokm", r.IntervaloKm);
                command.Parameters.AddWithValue("$precio", r.Precio);
                command.Parameters.AddWithValue("$sitio", r.Sitio);
                command.Parameters.AddWithValue("$fecha", DateTime.Today.ToShortDateString());
                command.Parameters.AddWithValue("$hecho", false);
                command.Parameters.AddWithValue("$notas", r.Notas);
                command.Parameters.AddWithValue("$archivos", r.Archivos);
                command.ExecuteNonQuery();
                load_data_grid();
            }
        }

        private void ToggleSwitch_historial_Toggled(object sender, RoutedEventArgs e)
        {
            if (ToggleSwitch_historial.IsOn) 
            { 
                dataGrid.Columns[0].Visibility = Visibility.Collapsed;
            } 
            else 
            { 
                dataGrid.Columns[0].Visibility= Visibility.Visible; 
            }

            load_data_grid();
             foreach (var dgColumn in dataGrid.Columns)
             {
                dgColumn.SortDirection = null;
             }
        }

        private void dataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            if(ToggleSwitch_historial.IsOn == true) 
            {
                return;
            }

            var item = e.Row.DataContext as Vehicle_register; // Reemplaza "YourItemType" con el tipo de objeto que representa cada fila en tu DataGrid
            if (item != null)
            {
                if (item.Color_row == "red")
                {
                    e.Row.Background = new SolidColorBrush(Color.FromArgb(70, 255, 0, 063));
                }
                if (item.Color_row == "yellow")
                {
                    e.Row.Background = new SolidColorBrush(Color.FromArgb(70, 255, 178, 50));
                }
            }
        }
    }


    public class Vehicle_register
    {
        public string Matricula { get; set; }
        public string Componente { get; set; }
        public int Km {get; set;}
        public int IntervaloKm { get; set;}
        public double Precio { get; set; }
        public string Sitio { get; set; } 
        public DateOnly Fecha { get; set; }    
        public Boolean Hecho { get; set; }
        public string Notas { get; set; }
        public string Archivos { get; set; }
        public string Color_row { get; set; }
        public int Km_restantes { get; set; }

        [Key]
        public int Id { get; set; }

        public Vehicle_register(string componente, int km, int intervaloKm, double precio, string sitio, string fecha, bool hecho, string notas, string archivos, int id)
        {
            Componente = componente;
            Km = km;
            IntervaloKm = intervaloKm;
            Precio = precio;
            Sitio = sitio;
            Fecha = DateOnly.Parse(fecha);
            Hecho = hecho;
            Notas = notas;
            Archivos = archivos;
            Id = id;
        }
        public Vehicle_register(string componente, int km, int intervaloKm, double precio, string sitio, string fecha, bool hecho, string notas, string archivos, int id, string color_row, int km_restantes)
        {
            Componente = componente;
            Km = km;
            IntervaloKm = intervaloKm;
            Precio = precio;
            Sitio = sitio;
            Fecha = DateOnly.Parse(fecha);
            Hecho = hecho;
            Notas = notas;
            Archivos = archivos;
            Id = id;
            Color_row = color_row;
            Km_restantes = km_restantes;
        }

    }
    public class ValueToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            string color = (string)value;

            if (color == "red")
            {
                return new SolidColorBrush(Colors.Red);
            }
            if(color == "yellow")
            {
                return new SolidColorBrush(Colors.Yellow);
            }
            else
            {
                return new SolidColorBrush(Colors.Transparent);

            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
