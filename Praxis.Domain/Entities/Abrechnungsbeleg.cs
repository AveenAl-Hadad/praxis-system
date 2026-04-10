using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Praxis.Domain.Entities
{
    public class Abrechnungsbeleg
    {
        public int Id { get; set; }
        public string Typ { get; set; }
        public string Zeitraum { get; set; }
        public int Faelle { get; set; }
        public decimal Betrag { get; set; }
        public string Status { get; set; }
        public string Aktion { get; set; }
        
    }
}
