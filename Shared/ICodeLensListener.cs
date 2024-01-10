using SyncToAsync.Shared.Dto;
using System;
using System.Threading.Tasks;

namespace SyncToAsync.Shared
{
    public interface ICodeLensListener
    {
        Task<bool> IsEnabled(
            );

        Task<SiblingInformationContainer> GetSiblingInformation(
            CodeLensTarget target
            );

        int GetVisualStudioPid();
    }
}
