using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using SyncToAsync.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SyncToAsync.Extension.Helper;

namespace SyncToAsync.Extension.CodeLens.Searcher.Async
{
    public sealed class AsyncSiblingSearcher
    {

        public SiblingMethodResult FindSiblingMethod(
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
                var siblingReturnTypes = DetermineSiblingReturnType(compilation, methodSymbol);
                if (siblingReturnTypes == null || siblingReturnTypes.Count == 0)
                {
                    return SiblingMethodResult.NotFound;
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
                var correctParameters = DetermineSiblingParameters(compilation, methodSymbol);
                if (correctParameters == null)
                {
                    return SiblingMethodResult.NotFound;
                }
                var methodsCandidate = methodsWithCorrectReturnType
                    .Where(m => correctParameters.MatchMethodParameters(m))
                    .ToList()
                    ;
                if (methodsCandidate.Count == 0)
                {
                    return SiblingMethodResult.NotFound;
                }

                var idealMethodName = methodSymbol.Name + CodeLensListener.AsyncSuffix;
                var idealMethod = methodsCandidate.FirstOrDefault(m => m.Name == idealMethodName);
                if (idealMethod != null)
                {
                    return new SiblingMethodResult(idealMethod, true);
                }

                var notIdealMethod = methodsCandidate.First();
                return new SiblingMethodResult(notIdealMethod, false);
            }
            catch (Exception ex)
            {
                Logging.LogVS(ex);
            }

            return SiblingMethodResult.NotFound;
        }

        private MethodParametersCollection DetermineSiblingParameters(
            Compilation compilation,
            IMethodSymbol methodSymbol
            )
        {
            var result = new MethodParametersCollection(
                compilation
                );

            foreach (var parameter in methodSymbol.Parameters)
            {
                var parameterType = parameter.Type;

                if (parameterType is INamedTypeSymbol namedParameterType
                    && namedParameterType.TypeArguments.Length > 0
                    )
                {
                    var typeArgument0 = namedParameterType.TypeArguments[0];

                    var tie = compilation.IEnumerable(typeArgument0);
                    if (SymbolEqualityComparer.Default.Equals(namedParameterType, tie))
                    {
                        //it is IEnumerable<T>
                        //for its async sibling it will be IEnumerable<T> or IAsyncEnumerable<T>
                        var tiae = compilation.IAsyncEnumerable(typeArgument0);
                        var atcp = new AnyTypeSymbolCollection(tiae, tie);
                        result.Add(atcp);
                    }
                    else
                    {
                        var atcp = new AnyTypeSymbolCollection(parameterType);
                        result.Add(atcp);
                    }
                }
                else
                {
                    var atcp = new AnyTypeSymbolCollection(parameterType);
                    result.Add(atcp);
                }
            }

            return result;
        }

        private static List<TypeComparer> DetermineSiblingReturnType(
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
                var returnType = methodSymbol.ReturnType;

                //if sync method returns IEnumerable<T> then
                //async sibling can returns IAsyncEnumerable<T>, Task<IEnumerable<T>>, ValueTask<IEnumerable<T>>
                //or even something exotic with GetAwaiter, which we do not support now (TODO)
                if (methodSymbol.ReturnType is INamedTypeSymbol namedReturnType
                    && namedReturnType.TypeArguments.Length > 0
                    )
                {
                    var typeArgument0 = namedReturnType.TypeArguments[0];

                    var ti = compilation.IEnumerable(typeArgument0);
                    if (SymbolEqualityComparer.Default.Equals(ti, namedReturnType))
                    {
                        //it is IEnumerable<T>

                        //ORDER HAS VALUE! the first is the candidate with highest priority
                        correctReturnTypes.Add(new TypeComparer(compilation.IAsyncEnumerable(typeArgument0)));
                        correctReturnTypes.Add(new TypeComparer(compilation.Task(returnType)));
                        correctReturnTypes.Add(new TypeComparer(compilation.ValueTask(returnType)));
                    }

                }
                else
                {
                    //if sync method returns some type T then
                    //async sibling can returns Task<T>, ValueTask<T>
                    //or even something exotic with GetAwaiter, which we do not support now (TODO)

                    //ORDER HAS VALUE! the first is the candidate with highest priority
                    correctReturnTypes.Add(new TypeComparer(compilation.Task(returnType)));
                    correctReturnTypes.Add(new TypeComparer(compilation.ValueTask(returnType)));
                }
            }

            return correctReturnTypes;
        }
    }
}
