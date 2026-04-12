using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Praxis.Application.Interfaces;
using Praxis.Domain.Entities;
using MessageBox = System.Windows.MessageBox;

namespace Praxis.Client.Views.Pages.UserManagement
{
    public partial class RoomsPage : System.Windows.Controls.UserControl
    {
        private readonly IRoomService _roomService;
        private Room? _selectedRoom;

        public RoomsPage(IRoomService roomService)
        {
            InitializeComponent();
            _roomService = roomService;
            Loaded += RoomsPage_Loaded;
        }

        private async void RoomsPage_Loaded(object sender, RoutedEventArgs e)
        {
            await RefreshAsync();
        }

        public async Task RefreshAsync()
        {
            RoomsGrid.ItemsSource = await _roomService.GetAllAsync();
        }

        private void ClearForm()
        {
            _selectedRoom = null;
            NameTextBox.Text = string.Empty;
            BeschreibungTextBox.Text = string.Empty;
            IsActiveCheckBox.IsChecked = true;
            RoomsGrid.SelectedItem = null;
            NameTextBox.Focus();
        }

        private async void SpeichernButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_selectedRoom == null)
                {
                    var newRoom = new Room
                    {
                        Name = NameTextBox.Text,
                        Beschreibung = BeschreibungTextBox.Text,
                        IsActive = IsActiveCheckBox.IsChecked == true
                    };

                    await _roomService.AddAsync(newRoom);
                    MessageBox.Show("Raum wurde angelegt.", "Erfolg",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    _selectedRoom.Name = NameTextBox.Text;
                    _selectedRoom.Beschreibung = BeschreibungTextBox.Text;
                    _selectedRoom.IsActive = IsActiveCheckBox.IsChecked == true;

                    await _roomService.UpdateAsync(_selectedRoom);
                    MessageBox.Show("Raum wurde aktualisiert.", "Erfolg",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }

                await RefreshAsync();
                ClearForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Fehler",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void LoeschenButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedRoom == null)
            {
                MessageBox.Show("Bitte zuerst einen Raum auswählen.", "Hinweis",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"Raum '{_selectedRoom.Name}' wirklich löschen?",
                "Bestätigung",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                await _roomService.DeleteAsync(_selectedRoom.Id);
                await RefreshAsync();
                ClearForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Fehler",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void NeuButton_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
        }

        private void RoomsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RoomsGrid.SelectedItem is not Room room)
                return;

            _selectedRoom = room;
            NameTextBox.Text = room.Name;
            BeschreibungTextBox.Text = room.Beschreibung;
            IsActiveCheckBox.IsChecked = room.IsActive;
        }
    }
}