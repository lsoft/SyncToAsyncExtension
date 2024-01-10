using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncToAsync.Extension.Helper
{
    public static class SymbolHelper
    {
        public static List<IMethodSymbol> GetAllMethods(
            this INamedTypeSymbol typeSymbol
            )
        {
            return typeSymbol
                .GetMembers()
                .Where(m => m is IMethodSymbol)
                .Cast<IMethodSymbol>()
                .Where(m => m.Kind == SymbolKind.Method && !typeSymbol.Constructors.Any(c => SymbolEqualityComparer.IncludeNullability.Equals(c, m)))
                .ToList();
        }
    }
}
