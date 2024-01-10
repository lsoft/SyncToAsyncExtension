using System;
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
using System.Collections.Immutable;
using Microsoft.VisualStudio.Language.Intellisense;
using System.Reflection.Metadata;
using System.IO;
using SyncToAsync.Extension.Helper;

namespace SyncToAsync.Extension
{
    [Export(typeof(ICodeLensCallbackListener))]
    [ContentType("CSharp")]
    public class CodeLensListener : ICodeLensCallbackListener, ICodeLensListener
    {
        private const string AsyncSuffix = "Async";

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

            var spanStart = target.MethodSpanStart.Value;
            var spanLength = target.MethodSpanLength.Value;

            var root = await document.GetSyntaxRootAsync();
            var methodNode = root.FindNode(new TextSpan(spanStart, spanLength));
            if (methodNode is not MethodDeclarationSyntax mds)
            {
                return SiblingInformationContainer.GetNoSibling(target);
            }

            var semanticModel = await document.GetSemanticModelAsync();
            if (semanticModel == null)
            {
                return SiblingInformationContainer.GetNoSibling(target);
            }

            var methodSymbol = semanticModel.GetDeclaredSymbol(mds) as IMethodSymbol;
            if (methodSymbol == null)
            {
                return SiblingInformationContainer.GetNoSibling(target);
            }

            var tds = mds.Up<TypeDeclarationSyntax>();
            if (tds == null)
            {
                return SiblingInformationContainer.GetNoSibling(target);
            }

            var typeSymbol = semanticModel.GetDeclaredSymbol(tds) as INamedTypeSymbol;
            if (typeSymbol == null)
            {
                return SiblingInformationContainer.GetNoSibling(target);
            }

            var siblingSymbol = FindSiblingMethod(
                semanticModel,
                typeSymbol,
                methodSymbol,
                mds
                );
            if (siblingSymbol == null)
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
                    siblingSymbol.Name,
                    siblingMethodBody,
                    siblingSymbol.DeclaringSyntaxReferences[0].Span.Start,
                    siblingSymbol.DeclaringSyntaxReferences[0].Span.Length
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

        private IMethodSymbol? FindSiblingMethod(
            SemanticModel semanticModel,
            INamedTypeSymbol typeSymbol,
            IMethodSymbol methodSymbol,
            MethodDeclarationSyntax mds
            )
        {
            //determine if target method is async (and sibling will be sync)
            //or vice versa
            if (methodSymbol.IsAsync)
            {
                //target method is async
                return FindSyncSiblingMethod(
                    semanticModel,
                    typeSymbol,
                    methodSymbol,
                    mds
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
                    methodSymbol,
                    mds
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
                    methodSymbol,
                    mds
                    );
            }

            //we interpret this method as sync, and its sibling is async
            return FindAsyncSiblingMethod(
                semanticModel,
                typeSymbol,
                methodSymbol,
                mds
                );
        }

        private IMethodSymbol? FindAsyncSiblingMethod(
            SemanticModel semanticModel,
            INamedTypeSymbol typeSymbol,
            IMethodSymbol methodSymbol,
            MethodDeclarationSyntax mds
            )
        {
            try
            {
                var compilation = semanticModel.Compilation;

                var methods = GetAllMethods(typeSymbol);

                //determine the return type for sibling
                var siblingReturnTypes = DetermineAsyncSiblingReturnType(compilation, methodSymbol);
                if (siblingReturnTypes == null || siblingReturnTypes.Count == 0)
                {
                    return null;
                }
                //take methods with correct return type
                var methodsWithCorrectReturnType = new List<IMethodSymbol>();
                //search for methods with async modifier (the first priority)
                foreach (var siblingReturnType in siblingReturnTypes)
                {
                    var filtered = methods
                        .Where(m => m.IsAsync && siblingReturnType.Equivalent(m.ReturnType))
                        .ToList()
                        ;
                    methodsWithCorrectReturnType.AddRange(filtered);
                }
                //search for methods without async modifier (the second priority)
                foreach (var siblingReturnType in siblingReturnTypes)
                {
                    var filtered = methods
                        .Where(m => !m.IsAsync && !m.ReturnsVoid && siblingReturnType.Equivalent(m.ReturnType))
                        .ToList()
                        ;
                    methodsWithCorrectReturnType.AddRange(filtered);
                }

                //filter these methods with correct method's parameters
                var correctParameters = DetermineAsyncSiblingParameters(compilation, methodSymbol);
                if (correctParameters == null)
                {
                    return null;
                }
                var methodsCandidate = methodsWithCorrectReturnType
                    .Where(m => CheckAsyncMethodParameters(compilation, m, correctParameters))
                    .ToList()
                    ;

                var idealMethodName = methodSymbol.Name + AsyncSuffix;
                var idealMethod = methodsCandidate.FirstOrDefault(m => m.Name == idealMethodName);
                if (idealMethod != null)
                {
                    return idealMethod;
                }

                return methodsCandidate.FirstOrDefault();
            }
            catch(Exception ex)
            {
                Logging.LogVS(ex);
            }

            return null;
        }

        private IMethodSymbol? FindSyncSiblingMethod(
            SemanticModel semanticModel,
            INamedTypeSymbol typeSymbol,
            IMethodSymbol methodSymbol,
            MethodDeclarationSyntax mds
            )
        {
            var compilation = semanticModel.Compilation;

            var methods = GetAllMethods(typeSymbol);

            //determine the return type for sibling
            var siblingReturnType1 = DetermineSyncSiblingReturnType(compilation, methodSymbol);
            if (!siblingReturnType1.HasValue)
            {
                return null;
            }
            var siblingReturnType = siblingReturnType1.Value;
            //take methods with correct return type
            var methodsWithCorrectReturnType = methods
                .Where(m => siblingReturnType.Equivalent(m.ReturnType))
                .ToList()
                ;

            //filter these methods with correct method's parameters
            var correctParameters = DetermineSyncSiblingParameters(compilation, methodSymbol);
            if (correctParameters == null)
            {
                return null;
            }
            var methodsCandidate = methodsWithCorrectReturnType
                .Where(m => CheckSyncMethodParameters(compilation, m, correctParameters))
                .ToList()
                ;

            var idealMethodName = methodSymbol.Name;
            if (idealMethodName.Length > AsyncSuffix.Length && idealMethodName.EndsWith(AsyncSuffix))
            {
                idealMethodName = idealMethodName.Substring(0, idealMethodName.Length - AsyncSuffix.Length);
            }
            var idealMethod = methodsCandidate.FirstOrDefault(m => m.Name == idealMethodName);
            if (idealMethod != null)
            {
                return idealMethod;
            }

            return methodsCandidate.FirstOrDefault();
        }

        private static List<IMethodSymbol> GetAllMethods(INamedTypeSymbol typeSymbol)
        {
            return typeSymbol
                .GetMembers()
                .Cast<IMethodSymbol>()
                .Where(m => m.Kind == SymbolKind.Method && !typeSymbol.Constructors.Any(c => SymbolEqualityComparer.IncludeNullability.Equals(c, m)))
                .ToList();
        }

        private bool CheckAsyncMethodParameters(
            Compilation compilation,
            IMethodSymbol methodSymbol,
            ImmutableArray<IParameterSymbol> correctParameters
            )
        {
            var methodParameters = methodSymbol.Parameters;

            var correctParametersIndex = 0;
            for (var pi = 0; pi < methodParameters.Length; pi++)
            {
                var methodParameter = methodParameters[pi];
                if (SymbolEqualityComparer.Default.Equals(methodParameter.Type, compilation.CancellationToken()))
                {
                    continue;
                }
                if (methodParameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).StartsWith("global::System.IProgress<"))
                {
                    continue;
                }

                if (correctParameters.Length <= correctParametersIndex)
                {
                    return false;
                }

                var correctParameter = correctParameters[correctParametersIndex];

                var methodParameterTypeComparer = new TypeComparer(methodParameter.Type);
                var correctParameterTypeComparer = new TypeComparer(correctParameter.Type);

                if (!methodParameterTypeComparer.Equivalent(correctParameterTypeComparer))
                {
                    return false;
                }

                correctParametersIndex++;
            }

            return true;
        }

        private bool CheckSyncMethodParameters(
            Compilation compilation,
            IMethodSymbol methodSymbol,
            ImmutableArray<IParameterSymbol> correctParameters
            )
        {
            var methodParameters = methodSymbol.Parameters;

            if (correctParameters.Length != methodParameters.Length)
            {
                return false;
            }

            for (var pi = 0; pi < methodParameters.Length; pi++)
            {
                var methodParameter = methodParameters[pi];
                var correctParameter = correctParameters[pi];

                var methodParameterTypeComparer = new TypeComparer(methodParameter.Type);
                var correctParameterTypeComparer = new TypeComparer(correctParameter.Type);

                if (!methodParameterTypeComparer.Equivalent(correctParameterTypeComparer))
                {
                    return false;
                }
            }

            return true;
        }

        private ImmutableArray<IParameterSymbol> DetermineAsyncSiblingParameters(
            Compilation compilation,
            IMethodSymbol methodSymbol
            )
        {
            return methodSymbol.Parameters;
        }

        private ImmutableArray<IParameterSymbol> DetermineSyncSiblingParameters(
            Compilation compilation,
            IMethodSymbol methodSymbol
            )
        {
            var result = new List<IParameterSymbol>();

            foreach (var parameter in methodSymbol.Parameters)
            {
                if (SymbolEqualityComparer.Default.Equals(parameter.Type, compilation.CancellationToken()))
                {
                    continue;
                }
                if (parameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).StartsWith("global::System.IProgress<"))
                {
                    continue;
                }

                result.Add(parameter);
            }

            return result.ToImmutableArray();
        }

        private static List<TypeComparer> DetermineAsyncSiblingReturnType(
            Compilation compilation,
            IMethodSymbol methodSymbol
            )
        {
            var correctReturnTypes = new List<TypeComparer>();

            var tvoid = compilation.Void();
            if (methodSymbol.ReturnsVoid)
            {
                //if sync method returns void, async sibling can returns void, Task, ValueTask or even something exotic with GetAwaiter, which we do not support now (TODO)

                //ORDER HAS VALUE! the first is the candidate with highest priority
                correctReturnTypes.Add(new TypeComparer(compilation.Task()));
                correctReturnTypes.Add(new TypeComparer(compilation.ValueTask()));
                correctReturnTypes.Add(new TypeComparer(tvoid));
            }
            else
            {
                //if sync method returns some type T, async sibling can returns Task<T>, ValueTask<T> or even something exotic with GetAwaiter, which we do not support now (TODO)

                //ORDER HAS VALUE! the first is the candidate with highest priority
                correctReturnTypes.Add(new TypeComparer(compilation.Task(methodSymbol.ReturnType)));
                correctReturnTypes.Add(new TypeComparer(compilation.ValueTask(methodSymbol.ReturnType)));
            }

            return correctReturnTypes;
        }

        private static TypeComparer? DetermineSyncSiblingReturnType(
            Compilation compilation,
            IMethodSymbol methodSymbol
            )
        {
            if (methodSymbol.ReturnType is not INamedTypeSymbol namedReturnType)
            {
                return null;
            }

            ITypeSymbol correctReturnType;
            var tvoid = compilation.Void();
            var ttask = compilation.Task();
            var tvtask = compilation.ValueTask();
            if (methodSymbol.ReturnsVoid)
            {
                correctReturnType = tvoid;
            }
            else if (SymbolEqualityComparer.Default.Equals(namedReturnType, ttask))
            {
                correctReturnType = tvoid;
            }
            else if (SymbolEqualityComparer.IncludeNullability.Equals(namedReturnType, tvtask))
            {
                correctReturnType = tvoid;
            }
            else if (namedReturnType.IsGenericType)
            {
                //something complex, like Task<object>, ValueTask<string>, Task<T> etc.
                //we need to unwrap here ValueTask<string> to string here
                correctReturnType = namedReturnType.TypeArguments[0];
            }
            else
            {
                //something unknown
                correctReturnType = null;
            }

            return new TypeComparer(correctReturnType);
        }
    }

    public readonly struct TypeComparer 
    {
        public readonly ITypeSymbol Type;

        public TypeComparer(
            ITypeSymbol type
            )
        {
            Type = type;
        }

        public bool Equivalent(TypeComparer comparer)
        {
            var comparerType = comparer.Type;
            return Equivalent(comparerType);
        }

        public bool Equivalent(ITypeSymbol comparerType)
        {
            //check for generic types T <-> T
            if (Type.TypeKind == TypeKind.TypeParameter
                && comparerType.TypeKind == TypeKind.TypeParameter
                )
            {
                return true;
            }

            //check for generic types like Task<T> <-> Task<T>
            if (Type is INamedTypeSymbol namedType)
            {
                if (comparerType is INamedTypeSymbol comparerNamedType)
                {
                    if (namedType.TypeArguments.Length > 0)
                    {
                        var ta = namedType.TypeArguments[0];
                        if (ta.TypeKind == TypeKind.TypeParameter)
                        {
                            if (comparerNamedType.TypeArguments.Length > 0)
                            {
                                var unboundNamedType = namedType.ConstructUnboundGenericType();
                                var unboundComparerNamedType = comparerNamedType.ConstructUnboundGenericType();
                                if (SymbolEqualityComparer.IncludeNullability.Equals(unboundNamedType, unboundComparerNamedType))
                                {
                                    var cta = comparerNamedType.TypeArguments[0];
                                    if (cta.TypeKind == TypeKind.TypeParameter)
                                    {
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            //check for regular types
            var result = SymbolEqualityComparer.Default.Equals(Type, comparerType);
            return result;
        }

        public override int GetHashCode()
        {
            throw new InvalidOperationException("Not supported");
        }
    }
}
