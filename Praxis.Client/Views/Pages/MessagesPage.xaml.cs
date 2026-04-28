using System.Windows;
using System.Windows.Controls;
using Praxis.Application.Interfaces;
using Praxis.Domain.Entities;
using MessageBox = System.Windows.MessageBox;

namespace Praxis.Client.Views.Pages;

public partial class MessagesPage : System.Windows.Controls.UserControl
{
    private readonly IPracticeMessageService _messageService;
    private readonly IPatientService _patientService;

    private const string CurrentUser = "MFA";

    public MessagesPage(
                        IPracticeMessageService messageService,
                        IPatientService patientService)
    {
        InitializeComponent();

        _messageService = messageService;
        _patientService = patientService;

        RecipientCombo.SelectedIndex = 0;
        PriorityCombo.SelectedIndex = 0;
    }

    public async Task RefreshAsync()
    {
        InboxGrid.ItemsSource = await _messageService.GetInboxAsync(CurrentUser);
        SentGrid.ItemsSource = await _messageService.GetSentAsync(CurrentUser);
    }

    private async void Send_Click(object sender, RoutedEventArgs e)
    {
        var recipient = (RecipientCombo.SelectedItem as ComboBoxItem)?.Content?.ToString();
        var priority = (PriorityCombo.SelectedItem as ComboBoxItem)?.Content?.ToString();

        if (string.IsNullOrWhiteSpace(recipient))
        {
            MessageBox.Show("Bitte Empfänger auswählen.");
            return;
        }

        if (string.IsNullOrWhiteSpace(SubjectText.Text))
        {
            MessageBox.Show("Bitte Betreff eingeben.");
            return;
        }

        if (string.IsNullOrWhiteSpace(BodyText.Text))
        {
            MessageBox.Show("Bitte Nachricht eingeben.");
            return;
        }

        int? patientId = null;

        if (PatientCombo.SelectedItem is Patient selectedPatient)
        {
            patientId = selectedPatient.Id;
        }

        var message = new PracticeMessage
        {
            Sender = CurrentUser,
            Recipient = recipient,
            Priority = priority ?? "Normal",
            Subject = SubjectText.Text.Trim(),
            Body = BodyText.Text.Trim(),
            PatientId = patientId
        };

        await _messageService.SendAsync(message);

        SubjectText.Clear();
        BodyText.Clear();
        PatientSearchText.Clear();
        PatientCombo.ItemsSource = null;

        MessageBox.Show("Nachricht wurde gesendet.");

        await RefreshAsync();
    }

    private async void InboxGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (InboxGrid.SelectedItem is not PracticeMessage message)
            return;

        InboxBodyText.Text = message.Body;

        if (!message.IsRead)
        {
            await _messageService.MarkAsReadAsync(message.Id);
            await RefreshAsync();
        }
    }

    private async void MarkRead_Click(object sender, RoutedEventArgs e)
    {
        if (InboxGrid.SelectedItem is not PracticeMessage message)
        {
            MessageBox.Show("Bitte zuerst eine Nachricht auswählen.");
            return;
        }

        await _messageService.MarkAsReadAsync(message.Id);
        await RefreshAsync();
    }

    private async void DeleteMessage_Click(object sender, RoutedEventArgs e)
    {
        if (InboxGrid.SelectedItem is not PracticeMessage message)
        {
            MessageBox.Show("Bitte zuerst eine Nachricht auswählen.");
            return;
        }

        var result = MessageBox.Show(
            "Möchtest du diese Nachricht wirklich löschen?",
            "Nachricht löschen",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes)
            return;

        await _messageService.DeleteAsync(message.Id);

        InboxBodyText.Clear();

        await RefreshAsync();
    }

    private async void SearchPatient_Click(object sender, RoutedEventArgs e)
    {
        var search = PatientSearchText.Text.Trim();

        if (string.IsNullOrWhiteSpace(search))
        {
            MessageBox.Show("Bitte Name, E-Mail oder Telefonnummer eingeben.");
            return;
        }

        var patients = await _patientService.SearchPatientsAsync(search);

        PatientCombo.ItemsSource = patients;

        if (patients.Count == 0)
        {
            MessageBox.Show("Kein Patient gefunden.");
            return;
        }

        PatientCombo.SelectedIndex = 0;
    }

}