using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Praxis.Client.Logic.UI
{
    public interface IMessageBoxService
    {
        MessageBoxResult Confirm(string message, string title);
        void ShowError(string v1, string v2);
        void ShowInfo(string message, string title);
        

    }
}
