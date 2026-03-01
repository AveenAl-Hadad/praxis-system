using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Praxis.Infrastructure.Exceptions
{
    public class UserFriendlyException : Exception
    {
        public UserFriendlyException(string message, Exception? inner = null)
            : base(message, inner) { }
    }
}
