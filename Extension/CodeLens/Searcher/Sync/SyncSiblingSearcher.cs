using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SyncToAsync.Shared;
using SyncToAsync.Extension.Helper;

namespace SyncToAsync.Extension.CodeLens.Searcher.Sync
{
    public sealed class SyncSiblingSearcher
    {
        public IMethodSymbol? FindSiblingMethod(
            SemanticModel semanticModel,
            INamedTypeSymbol typeSymbol,
            IMethodSymbol methodSymbol
            )
        {
            try
            {
                var compilation = semanticModel.Compilation;

                var methods = typeSymbol.GetAllMethods();

                //determine the return type for sibling
                var siblingReturnType = DetermineSiblingReturnType(compilation, methodSymbol);
                if (siblingReturnType is null)
                {
                    return null;
                }

                //take methods with correct return type
                var methodsWithCorrectReturnType = methods
                    .Where(m => siblingReturnType.Equivalent(m.ReturnType))
                    .ToList()
                    ;

                //filter these methods with correct method's parameters
                var correctParameters = DetermineSiblingParameters(compilation, methodSymbol);
                if (correctParameters == null)
                {
                    return null;
                }
                var methodsCandidate = methodsWithCorrectReturnType
                    .Where(m => CheckMethodParameters(compilation, m, correctParameters))
                    .ToList()
                    ;

                var idealMethodName = methodSymbol.Name;
                if (idealMethodName.Length > CodeLensListener.AsyncSuffix.Length
                    && idealMethodName.EndsWith(CodeLensListener.AsyncSuffix)
                    )
                {
                    idealMethodName = idealMethodName.Substring(0, idealMethodName.Length - CodeLensListener.AsyncSuffix.Length);
                }
                var idealMethod = methodsCandidate.FirstOrDefault(m => m.Name == idealMethodName);
                if (idealMethod != null)
                {
                    return idealMethod;
                }

                return methodsCandidate.FirstOrDefault();
            }
            catch (Exception ex)
            {
                Logging.LogVS(ex);
            }

            return null;
        }



        private bool CheckMethodParameters(
            Compilation compilation,
            IMethodSymbol methodSymbol,
            IReadOnlyList<ITypeSymbol> correctParameters
            )
        {
            var methodParameters = methodSymbol.Parameters;

            if (correctParameters.Count != methodParameters.Length)
            {
                return false;
            }

            for (var pi = 0; pi < methodParameters.Length; pi++)
            {
                var methodParameter = methodParameters[pi];
                var correctParameter = correctParameters[pi];

                var methodParameterTypeComparer = new TypeComparer(methodParameter.Type);
                var correctParameterTypeComparer = new TypeComparer(correctParameter);

                if (!methodParameterTypeComparer.Equivalent(correctParameterTypeComparer))
                {
                    return false;
                }
            }

            return true;
        }

        private IReadOnlyList<ITypeSymbol> DetermineSiblingParameters(
            Compilation compilation,
            IMethodSymbol methodSymbol
            )
        {
            var result = new List<ITypeSymbol>();

            foreach (var parameter in methodSymbol.Parameters)
            {
                var parameterType = parameter.Type;

                if (SymbolEqualityComparer.Default.Equals(parameterType, compilation.CancellationToken()))
                {
                    continue;
                }

                if (parameterType is INamedTypeSymbol namedParameterType
                    && namedParameterType.TypeArguments.Length > 0
                    )
                {
                    var typeArgument0 = namedParameterType.TypeArguments[0];

                    var tip = compilation.IProgress(typeArgument0);
                    if (SymbolEqualityComparer.Default.Equals(namedParameterType, tip))
                    {
                        //it is IProgress<T>
                        continue;
                    }
                    var tiae = compilation.IAsyncEnumerable(typeArgument0);
                    if (SymbolEqualityComparer.Default.Equals(namedParameterType, tiae))
                    {
                        //it is IAsyncEnumerable<T>
                        //for its sync sibling it will be IEnumerable<T>
                        var tie = compilation.IEnumerable(typeArgument0);
                        result.Add(tie);
                    }
                }
                else
                {
                    result.Add(parameterType);
                }
            }

            return result;
        }

        private static TypeComparer? DetermineSiblingReturnType(
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
                //something complex, like Task<object>, ValueTask<string>, Task<T>, IAsyncEnumerable<T> etc.
                var typeArgument0 = namedReturnType.TypeArguments[0];

                //check for special case of IAsyncEnumerable<T>
                var tai = compilation.IAsyncEnumerable(typeArgument0);
                if (SymbolEqualityComparer.Default.Equals(tai, namedReturnType))
                {
                    correctReturnType = compilation.IEnumerable(typeArgument0);
                }
                else
                {
                    //we need to unwrap here Task<string>, ValueTask<string> to string (or other type) here
                    correctReturnType = typeArgument0;
                }
            }
            else
            {
                //something unknown
                correctReturnType = null;
            }

            return new TypeComparer(correctReturnType);
        }

    }
}
