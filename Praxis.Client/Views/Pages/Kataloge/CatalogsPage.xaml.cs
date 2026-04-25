using Praxis.Application.Interfaces;
using Praxis.Domain.Entities;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Praxis.Infrastructure.Services;
using System.IO;
using Praxis.Client.Session;
using Praxis.Domain.Constants;
using Praxis.Client.Security;

using System.Windows.Controls;
using MessageBox = System.Windows.MessageBox;

namespace Praxis.Client.Views.Pages.Kataloge;

public partial class CatalogsPage : System.Windows.Controls.UserControl
{
    private readonly ICatalogService _catalogService;
  
    private readonly IIcdImportService _icdImportService;

    private readonly IMedicationImportService _medicationImportService;

    private readonly IServiceCatalogImportService _serviceCatalogImportService;

    
    private readonly ObservableCollection<CatalogItem> _items = new();
    private List<CatalogItem> _allItems = new();
    private CatalogItem? _selectedItem;



    public CatalogsPage(ICatalogService catalogService,
                        IIcdImportService icdImportService,
                        IMedicationImportService medicationImportService,
                        IServiceCatalogImportService serviceCatalogImportService)
    {
        InitializeComponent();
        ImportPanel.Visibility = PermissionHelper.CanImportCatalogs
                    ? Visibility.Visible
                    : Visibility.Collapsed;

        SaveButton.Visibility = PermissionHelper.CanEditCatalogs
                  ? Visibility.Visible
                  : Visibility.Collapsed;

        DeleteButton.Visibility = PermissionHelper.CanEditCatalogs
                    ? Visibility.Visible
                    : Visibility.Collapsed;

        NewButton.Visibility = PermissionHelper.CanEditCatalogs
                ? Visibility.Visible
                : Visibility.Collapsed;

        ImportPanel.Visibility = UserSession.HasRole(Roles.Administrator)
                    ? Visibility.Visible
                    : Visibility.Collapsed;

        _catalogService = catalogService;
        _icdImportService = icdImportService;
        _medicationImportService = medicationImportService;
        _serviceCatalogImportService = serviceCatalogImportService;

        CatalogGrid.ItemsSource = _items;
        CatalogTypeBox.SelectedIndex = 0;

        Loaded += async (_, _) => await LoadDataAsync();

    }
    
    public async Task LoadDataAsync()
    {
        if (CatalogTypeBox.SelectedItem is not ComboBoxItem item)
            return;

        var category = item.Content?.ToString() ?? "";

        _allItems = await _catalogService.GetByCategoryAsync(category);
        ApplyFilter();
    }

    private async void CatalogTypeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ClearForm();
        await LoadDataAsync();
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var search = SearchBox.Text?.Trim().ToLower() ?? "";

        var result = _allItems
            .Where(x =>
                string.IsNullOrWhiteSpace(search) ||
                x.Code.ToLower().Contains(search) ||
                x.Name.ToLower().Contains(search) ||
                x.Description.ToLower().Contains(search))
            .ToList();

        _items.Clear();

        foreach (var item in result)
            _items.Add(item);
    }

    private void CatalogGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CatalogGrid.SelectedItem is not CatalogItem item)
            return;

        _selectedItem = item;
        CodeBox.Text = item.Code;
        NameBox.Text = item.Name;
        DescriptionBox.Text = item.Description;
    }

    private void New_Click(object sender, RoutedEventArgs e)
    {
        ClearForm();
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var category = GetSelectedCategory();

            if (_selectedItem == null)
            {
                var item = new CatalogItem
                {
                    Category = category,
                    Code = CodeBox.Text,
                    Name = NameBox.Text,
                    Description = DescriptionBox.Text,
                    IsActive = true
                };

                await _catalogService.AddAsync(item);
            }
            else
            {
                _selectedItem.Category = category;
                _selectedItem.Code = CodeBox.Text;
                _selectedItem.Name = NameBox.Text;
                _selectedItem.Description = DescriptionBox.Text;

                await _catalogService.UpdateAsync(_selectedItem);
            }

            await LoadDataAsync();
            ClearForm();

            MessageBox.Show("Katalogeintrag wurde gespeichert.");
        }
        catch (System.Exception ex)
        {
            MessageBox.Show(ex.Message, "Fehler beim Speichern");
        }
    }

    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedItem == null)
        {
            MessageBox.Show("Bitte zuerst einen Eintrag auswählen.");
            return;
        }

        var result = MessageBox.Show(
            "Diesen Katalogeintrag wirklich löschen?",
            "Löschen bestätigen",
            MessageBoxButton.YesNo);

        if (result != MessageBoxResult.Yes)
            return;

        try
        {
            await _catalogService.DeleteAsync(_selectedItem.Id);
            await LoadDataAsync();
            ClearForm();

            MessageBox.Show("Katalogeintrag wurde gelöscht.");
        }
        catch (System.Exception ex)
        {
            MessageBox.Show(ex.Message, "Fehler beim Löschen");
        }
    }

    private string GetSelectedCategory()
    {
        if (CatalogTypeBox.SelectedItem is ComboBoxItem item)
            return item.Content?.ToString() ?? "";

        return "";
    }

    private void ClearForm()
    {
        _selectedItem = null;
        CatalogGrid.SelectedItem = null;
        CodeBox.Clear();
        NameBox.Clear();
        DescriptionBox.Clear();
        CodeBox.Focus();
    }

    private async void ImportIcd_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var path = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Data",
                "icd10gm2026syst_claml_20250912.xml");

            await _icdImportService.ImportAsync(path);
            await LoadDataAsync();

            MessageBox.Show("ICD-10-GM wurde importiert.");
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "ICD Import Fehler");
        }
    }

    private async void ImportMed_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var path = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Data",
                "medikamente.csv");

            await _medicationImportService.ImportAsync(path);
            await LoadDataAsync();

            MessageBox.Show("Medikamente importiert.");
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    private async void ImportServices_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var path = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Data",
                "leistungen.csv");

            await _serviceCatalogImportService.ImportAsync(path);

            CatalogTypeBox.SelectedIndex = 1; // Leistungen / GOÄ / EBM
            SearchBox.Clear();

            await LoadDataAsync();

            MessageBox.Show("Leistungen wurden importiert.");
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.ToString(), "Leistungen Import Fehler");
        }
    }
  
    public async Task SelectCategoryAsync(string category)
    {
        for (int i = 0; i < CatalogTypeBox.Items.Count; i++)
        {
            if (CatalogTypeBox.Items[i] is ComboBoxItem item &&
                item.Content?.ToString() == category)
            {
                CatalogTypeBox.SelectedIndex = i;
                break;
            }
        }

        SearchBox.Clear();
        ClearForm();

        await LoadDataAsync();
    }
    public async Task ShowOverviewAsync()
    {
        SearchBox.Clear();
        ClearForm();

        _allItems = await _catalogService.GetAllAsync();

        ApplyFilter();
    }
}