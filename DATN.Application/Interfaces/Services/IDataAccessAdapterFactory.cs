using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyProject.Application.Interfaces.Services
{
    public interface IDataAccessAdapterFactory
    {
        IDisposable CreateAdapter();
    }
}
