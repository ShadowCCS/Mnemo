using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MnemoProject.Services
{
    public interface IModelProvider
    {
        Task<string> GenerateTextAsync(string prompt);
    }

}
