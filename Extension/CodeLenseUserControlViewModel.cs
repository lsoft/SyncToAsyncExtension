using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Threading;
using SyncToAsync.Extension.Helper;
using SyncToAsync.Shared.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SyncToAsync.Extension
{
    public class CodeLenseUserControlViewModel : BaseViewModel
    {
        private readonly SiblingInformationContainer _sic;

        public string MethodBody
        {
            get;
            set;
        }

        public CodeLenseUserControlViewModel(
            SiblingInformationContainer sic
            )
        {
            _sic = sic;

            if (sic.Sibling == null)
            {
                MethodBody = string.Empty;
                return;
            }

            MethodBody = sic.Sibling.MethodBody;
        }

        public async Task GotoSiblingAsync()
        {
            if (_sic is null || _sic.Disabled || _sic.Sibling is null)
            {
                return;
            }

            var projectId = ProjectId.CreateFromSerialized(_sic.Sibling.ProjectGuid, null);
            var documentId = DocumentId.CreateFromSerialized(
                projectId,
                _sic.Sibling.DocumentGuid
                );

            var opener = new VisualStudioDocumentHelper(
                _sic.Sibling.FilePath
                );
            if (_sic.Sibling.IsSourceGeneratedDocument)
            {
                await opener.OpenNavigateViewRoslynInternalsAsync(
                    documentId,
                    _sic.Sibling.MethodSpanStart.Value,
                    _sic.Sibling.MethodSpanLength.Value
                    );
            }
            else
            {
                opener.OpenNavigate(
                    _sic.Sibling.MethodSpanStart.Value,
                    _sic.Sibling.MethodSpanLength.Value
                    );
            }
        }
    }
}
