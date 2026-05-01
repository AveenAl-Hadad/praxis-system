using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Praxis.Client.Views.Main
{
    public partial class MainWindow
    {
        public async Task OpenPatientDeletePageAsync()
        {
            LoadPage(_patientDeletePage);
            await _patientDeletePage.RefreshAsync();
        }
    }
}
