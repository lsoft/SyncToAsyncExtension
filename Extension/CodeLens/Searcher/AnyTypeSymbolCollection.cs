using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace SyncToAsync.Extension.CodeLens.Searcher
{
    public sealed class AnyTypeSymbolCollection
    {
        private readonly List<TypeComparer> _typeComparers;

        public AnyTypeSymbolCollection(
            )
        {
            _typeComparers = new List<TypeComparer>();
        }

        public AnyTypeSymbolCollection(
            ITypeSymbol typeSymbol
            ) : this()
        {
            Add(typeSymbol);
        }

        public void Add(ITypeSymbol typeSymbol)
        {
            _typeComparers.Add(new TypeComparer(typeSymbol));
        }

        public bool MatchWith(ITypeSymbol incomingTypeSymbol)
        {
            var methodParameterTypeComparer = new TypeComparer(incomingTypeSymbol);

            foreach (var typeSymbol in _typeComparers)
            {
                if (methodParameterTypeComparer.Equivalent(typeSymbol))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
