using System.Windows;
using System.Windows.Controls;
using Praxis.Application.Interfaces;
using Praxis.Domain.Entities;
using MessageBox = System.Windows.MessageBox;

namespace Praxis.Client.Views.Pages;

public partial class MessagesPage : System.Windows.Controls.UserControl
{
    private readonly IPracticeMessageService _messageService;

    private const string CurrentUser = "MFA";

    public MessagesPage(IPracticeMessageService messageService)
    {
        InitializeComponent();

        _messageService = messageService;

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

        if (int.TryParse(PatientIdText.Text, out var parsedPatientId))
        {
            patientId = parsedPatientId;
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
        PatientIdText.Clear();

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
}