using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Praxis.Application.Interfaces
{
    public interface IDashboardLayoutService
    {
        
    Task<List<string>> GetWidgetOrderAsync(string username);
        Task SaveWidgetOrderAsync(string username, List<string> widgetOrder);
    }
}

