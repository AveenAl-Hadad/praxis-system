using System.Windows.Controls;

namespace Praxis.Client.Views.Pages.Labor
{
    public partial class LaborPage : UserControl
    {
        public LaborPage()
        {
            InitializeComponent();

            LaborGrid.ItemsSource = new[]
            {
                new
                {
                    Datei = "labor_2024_08_12.ldt",
                    Labor = "MVZ Labor Bamberg",
                    Erstellt = "12.08.2024 08:15",
                    Betriebsstaette = "Hauptstandort",
                    Bsnr = "123456789",
                    Kundennummer = "4711",
                    Status = "Bereit"
                },
                new
                {
                    Datei = "labor_2024_08_11.ldt",
                    Labor = "Labor Nord",
                    Erstellt = "11.08.2024 17:40",
                    Betriebsstaette = "Filiale 1",
                    Bsnr = "987654321",
                    Kundennummer = "1288",
                    Status = "Importiert"
                }
            };
        }
    }
}