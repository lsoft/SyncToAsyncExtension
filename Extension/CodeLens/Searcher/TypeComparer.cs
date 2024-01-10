using Microsoft.CodeAnalysis;

namespace SyncToAsync.Extension
{
    public sealed class TypeComparer
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
