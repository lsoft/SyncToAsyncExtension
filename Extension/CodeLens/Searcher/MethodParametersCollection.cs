using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace SyncToAsync.Extension.CodeLens.Searcher
{
    public sealed class MethodParametersCollection
    {
        private readonly Compilation _compilation;
        private readonly INamedTypeSymbol _tct;

        private readonly List<AnyTypeSymbolCollection> _parameters;

        public MethodParametersCollection(
            Compilation compilation
            )
        {
            _compilation = compilation;
            _tct = _compilation.CancellationToken();

            _parameters = new List<AnyTypeSymbolCollection>();
        }

        public void Add(AnyTypeSymbolCollection parameter)
        {
            _parameters.Add(parameter);
        }

        public bool MatchMethodParameters(
            IMethodSymbol methodSymbol
            )
        {
            var methodParameters = methodSymbol.Parameters;


            var correctParametersIndex = 0;
            for (var pi = 0; pi < methodParameters.Length; pi++)
            {
                var methodParameter = methodParameters[pi];
                var methodParameterType = methodParameter.Type;

                //if we found CancellationToken in a parameter list of async method
                //then skip it!
                if (SymbolEqualityComparer.Default.Equals(methodParameterType, _tct))
                {
                    continue;
                }

                //if we found IProgress<> in a parameter list of async method
                //then skip it!
                if (methodParameterType is INamedTypeSymbol namedMethodParameterType
                    && namedMethodParameterType.TypeArguments.Length > 0
                    )
                {
                    var typeArgument0 = namedMethodParameterType.TypeArguments[0];

                    var tip = _compilation.IProgress(typeArgument0);
                    if (SymbolEqualityComparer.Default.Equals(namedMethodParameterType, tip))
                    {
                        //it is IProgress<T>
                        continue;
                    }
                }

                //if expected parameters count is lesser than the parameter count of the real method
                //then it is not a sibling candidate at all
                if (_parameters.Count <= correctParametersIndex)
                {
                    return false;
                }

                var correctParameter = _parameters[correctParametersIndex];
                if (!correctParameter.MatchWith(methodParameterType))
                {
                    //at least one method parameter is not matched
                    //then it is not a sibling candidate at all
                    return false;
                }

                correctParametersIndex++;
            }

            return true;
        }

    }
}
