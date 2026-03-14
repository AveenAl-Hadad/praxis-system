using System.Collections.ObjectModel;
using System.Windows;
using Praxis.Domain.Entities;
using Praxis.Infrastructure.Services;

namespace Praxis.Client.Views;

public partial class AddInvoiceWindow : Window
{
    private readonly IPatientService _patientService;
    public Invoice? CreatedInvoice { get; private set; }

    private ObservableCollection<InvoiceItem> _items = new();

    public AddInvoiceWindow(IPatientService patientService)
    {
        InitializeComponent();
        _patientService = patientService;
        ItemsGrid.ItemsSource = _items;
        Loaded += AddInvoiceWindow_Loaded;
    }

    private async void AddInvoiceWindow_Loaded(object sender, RoutedEventArgs e)
    {
        var patients = await _patientService.GetAllPatientsAsync();
        PatientComboBox.ItemsSource = patients.ToList();
    }

    private void AddItem_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(DescriptionBox.Text))
        {
            MessageBox.Show("Bitte Beschreibung eingeben.");
            return;
        }

        if (!int.TryParse(QuantityBox.Text, out int quantity) || quantity <= 0)
        {
            MessageBox.Show("Ungültige Menge.");
            return;
        }

        if (!decimal.TryParse(UnitPriceBox.Text, out decimal unitPrice) || unitPrice < 0)
        {
            MessageBox.Show("Ungültiger Preis.");
            return;
        }

        _items.Add(new InvoiceItem
        {
            Description = DescriptionBox.Text.Trim(),
            Quantity = quantity,
            UnitPrice = unitPrice
        });

        UpdateTotal();

        DescriptionBox.Clear();
        QuantityBox.Clear();
        UnitPriceBox.Clear();
    }

    private void RemoveItem_Click(object sender, RoutedEventArgs e)
    {
        if (ItemsGrid.SelectedItem is InvoiceItem item)
        {
            _items.Remove(item);
            UpdateTotal();
        }
    }

    private void UpdateTotal()
    {
        var total = _items.Sum(i => i.TotalPrice);
        TotalText.Text = $"Gesamt: {total:N2} €";
        ItemsGrid.Items.Refresh();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (PatientComboBox.SelectedItem is not Patient patient)
        {
            MessageBox.Show("Bitte einen Patienten auswählen.");
            return;
        }

        if (_items.Count == 0)
        {
            MessageBox.Show("Bitte mindestens eine Leistung hinzufügen.");
            return;
        }

        CreatedInvoice = new Invoice
        {
            PatientId = patient.Id,
            InvoiceDate = DateTime.Now,
            InvoiceNumber = $"RE-{DateTime.Now:yyyyMMddHHmmss}",
            Items = _items.ToList(),
            TotalAmount = _items.Sum(i => i.TotalPrice)
        };

        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}