using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Praxis.Domain.Entities
{
    public class LaborRecord
    {
        public int Id { get; set; }
        public string Datei { get; set; }
        public string Labor { get; set; }
        public string Erstellt { get; set; }
        public string Betriebsstaette { get; set; }
        public string Bsnr { get; set; }
        public string Kundennummer { get; set; }
        public string Status { get; set; }
    }
}
