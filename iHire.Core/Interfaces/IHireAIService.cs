using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IHire.Core.Interfaces
{
    interface IHireAIService
    {
        Task<string> ExtractCandidateInfo(string fileName, string queries);
    }
}
