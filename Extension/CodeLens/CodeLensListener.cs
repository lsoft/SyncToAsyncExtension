using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Threading.Tasks;
using SyncToAsync.Shared;
using SyncToAsync.Shared.Dto;
using Extension;
using Microsoft.VisualStudio.Language.CodeLens;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices;
using System.Linq;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SyncToAsync.Extension.Helper;
using SyncToAsync.Extension.CodeLens.Searcher;
using Microsoft.VisualStudio.Threading;

namespace SyncToAsync.Extension
{
    [Export(typeof(ICodeLensCallbackListener))]
    [ContentType("CSharp")]
    public class CodeLensListener : ICodeLensCallbackListener, ICodeLensListener
    {
        public const string AsyncSuffix = "Async";

        public static bool IdleMode = false;

        private readonly IComponentModel _componentModel;

        [ImportingConstructor]
        public CodeLensListener(
            )
        {
            _componentModel = (IComponentModel)Package.GetGlobalService(typeof(SComponentModel));
        }

        public Task<bool> IsEnabled(
            )
        {
            var opts = General.Instance;
            return Task.FromResult(opts.Enabled);
        }

        public int GetVisualStudioPid() => Process.GetCurrentProcess().Id;

        public async Task<SiblingInformationContainer> GetSiblingInformation(
            CodeLensTarget target
            )
        {
            var opts = General.Instance;
            if (!opts.Enabled)
            {
                return SiblingInformationContainer.GetDisabled(target);
            }

            if (IdleMode)
            {
                return SiblingInformationContainer.GetIdle(target);
            }

            if (!target.MethodSpanStart.HasValue || !target.MethodSpanLength.HasValue)
            {
                return SiblingInformationContainer.GetNoSibling(target);
            }

            //switch to background thread
            await TaskScheduler.Default;

            var workspace = (Workspace)_componentModel.GetService<VisualStudioWorkspace>();
            if (workspace == null)
            {
                return SiblingInformationContainer.GetNoSibling(target);
            }
            if (!workspace.CurrentSolution.Projects.Any())
            {
                return SiblingInformationContainer.GetNoSibling(target);
            }

            var pid = ProjectId.CreateFromSerialized(target.RoslynProjectIdGuid, null);
            var project = workspace.CurrentSolution.GetProject(
                pid
                );
            if (project == null)
            {
                return SiblingInformationContainer.GetNoSibling(target);
            }

            var document = await project.GetDocumentByDocumentIdAsync(
                DocumentId.CreateFromSerialized(pid, target.RoslynDocumentIdGuid)
                );
            if (document == null)
            {
                return SiblingInformationContainer.GetNoSibling(target);
            }


            var searcher = new SiblingSearcher();
            var siblingSearchResult = await searcher.FindSiblingMethodAsync(
                document,
                new TextSpan(
                    target.MethodSpanStart.Value,
                    target.MethodSpanLength.Value
                    )
                );
            var siblingSymbol = siblingSearchResult.SiblingMethod;

            if (siblingSymbol is null)
            {
                return SiblingInformationContainer.GetNoSibling(target);
            }

            var siblingSyntaxReference = siblingSymbol.DeclaringSyntaxReferences[0];


            var siblingDocument = workspace.CurrentSolution.GetDocument(siblingSyntaxReference.SyntaxTree);
            var siblingFilePath = siblingDocument.FilePath;
            var siblingIsInSourceGeneratedDocument = siblingDocument is SourceGeneratedDocument;
            var siblingMethodBody = await GetSiblingMethodBodyAsync(siblingSyntaxReference);

            return SiblingInformationContainer.GetWithSibling(
                target,
                new CodeLensSibling(
                    siblingDocument.Project.Id.Id,
                    siblingDocument.Id.Id,
                    siblingFilePath,
                    siblingIsInSourceGeneratedDocument,
                    siblingSearchResult.IsStrictCompliance,
                    siblingSymbol.Name,
                    siblingMethodBody,
                    siblingSyntaxReference.Span.Start,
                    siblingSyntaxReference.Span.Length
                    )
                );
        }

        private static async Task<string> GetSiblingMethodBodyAsync(SyntaxReference siblingSyntaxReference)
        {
            var siblingRoot = await siblingSyntaxReference.SyntaxTree.GetRootAsync();
            var siblingSyntax = siblingRoot.FindNode(siblingSyntaxReference.Span);
            var ltSiblingText = siblingSyntax.GetLeadingTrivia()
                .ToString()
                .Replace("\r", "")
                .Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries)
                ;
            var siblingBody = (ltSiblingText.LastOrDefault() ?? string.Empty)
                + siblingSyntax.ToString();

            return siblingBody;
        }
    }
}
