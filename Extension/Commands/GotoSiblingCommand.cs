using Microsoft;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Shell.Interop;
using SyncToAsync.Extension.Helper;
using SyncToAsync.Shared;
using SyncToAsync.Shared.Dto;
using System.ComponentModel.Design;

namespace SyncToAsync.Extension
{
    [Command(PackageGuids.ExtensionString, PackageIds.GotoSiblingCommandId)]
    internal sealed class GotoSiblingCommand : BaseCommand<GotoSiblingCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            var siblingInfo = e.InValue as SiblingInformationContainer;
            await GotoSiblingAsync(siblingInfo);
        }

        public async Task GotoSiblingAsync(SiblingInformationContainer sic)
        {
            if (sic is null || sic.Disabled || sic.Sibling is null)
            {
                return;
            }

            var projectId = ProjectId.CreateFromSerialized(sic.Sibling.ProjectGuid, null);
            var documentId = DocumentId.CreateFromSerialized(
                projectId,
                sic.Sibling.DocumentGuid
                );

            var opener = new VisualStudioDocumentHelper(
                sic.Sibling.FilePath
                );
            if (sic.Sibling.IsSourceGeneratedDocument)
            {
                await opener.OpenNavigateViewRoslynInternalsAsync(
                    documentId,
                    sic.Sibling.MethodSpanStart.Value,
                    0
                    );
            }
            else
            {
                opener.OpenNavigate(
                    sic.Sibling.MethodSpanStart.Value,
                    sic.Sibling.MethodSpanLength.Value
                    );
            }
        }

    }

}
