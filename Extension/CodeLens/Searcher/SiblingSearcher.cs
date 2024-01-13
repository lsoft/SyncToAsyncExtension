#nullable enable

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using SyncToAsync.Extension.CodeLens.Searcher.Async;
using SyncToAsync.Extension.CodeLens.Searcher.Sync;

namespace SyncToAsync.Extension.CodeLens.Searcher
{
    internal sealed class SiblingSearcher
    {
        public SiblingSearcher(
            
            )
        {
            
        }

        public async Task<SiblingMethodResult> FindSiblingMethodAsync(
            Document document,
            TextSpan span
            )
        {
            var root = await document.GetSyntaxRootAsync();
            var methodNode = root.FindNode(new TextSpan(span.Start, span.Length));
            if (methodNode is not MethodDeclarationSyntax mds)
            {
                return SiblingMethodResult.NotFound;
            }

            var semanticModel = await document.GetSemanticModelAsync();
            if (semanticModel == null)
            {
                return SiblingMethodResult.NotFound;
            }

            var methodSymbol = semanticModel.GetDeclaredSymbol(mds) as IMethodSymbol;
            if (methodSymbol == null)
            {
                return SiblingMethodResult.NotFound;
            }

            var tds = mds.Up<TypeDeclarationSyntax>();
            if (tds == null)
            {
                return SiblingMethodResult.NotFound;
            }

            var typeSymbol = semanticModel.GetDeclaredSymbol(tds) as INamedTypeSymbol;
            if (typeSymbol == null)
            {
                return SiblingMethodResult.NotFound;
            }


            //determine if target method is async (and sibling will be sync)
            //or vice versa
            if (methodSymbol.IsAsync)
            {
                //target method is async
                return FindSyncSiblingMethod(
                    semanticModel,
                    typeSymbol,
                    methodSymbol
                    );
            }

            //no async modifier

            var compilation = semanticModel.Compilation;

            if (methodSymbol.ReturnsVoid)
            {
                //method returns void
                //so it's 100% sync method
                return FindAsyncSiblingMethod(
                    semanticModel,
                    typeSymbol,
                    methodSymbol
                    );
            }

            //determine is return type has GetAwaiter method
            var awaitableReturnType = methodSymbol.ReturnType.GetMembers()
                .Any(m => m.Kind == SymbolKind.Method && m.Name == "GetAwaiter")
                //TODO: check return type for GetAwaiter method
                ;
            if (awaitableReturnType)
            {
                //method has no async modifier, but returns awaitable return type
                //we interpret this method as async, and its sibling is sync
                return FindSyncSiblingMethod(
                    semanticModel,
                    typeSymbol,
                    methodSymbol
                    );
            }

            //we interpret this method as sync, and its sibling is async
            return FindAsyncSiblingMethod(
                semanticModel,
                typeSymbol,
                methodSymbol
                );
        }

        private SiblingMethodResult FindSyncSiblingMethod(
            SemanticModel semanticModel,
            INamedTypeSymbol typeSymbol,
            IMethodSymbol methodSymbol
            )
        {
            var searcher = new SyncSiblingSearcher();

            return searcher.FindSiblingMethod(
                semanticModel,
                typeSymbol,
                methodSymbol
                );
        }

        private SiblingMethodResult FindAsyncSiblingMethod(
            SemanticModel semanticModel,
            INamedTypeSymbol typeSymbol,
            IMethodSymbol methodSymbol
            )
        {
            var searcher = new AsyncSiblingSearcher();

            return searcher.FindSiblingMethod(
                semanticModel,
                typeSymbol,
                methodSymbol
                );
        }

    }
}
