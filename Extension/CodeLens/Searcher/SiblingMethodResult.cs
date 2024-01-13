using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncToAsync.Extension.CodeLens.Searcher
{
    /// <summary>
    /// Result of sibling method search.
    /// </summary>
    public readonly struct SiblingMethodResult
    {
        public static readonly SiblingMethodResult NotFound = new SiblingMethodResult();

        /// <summary>
        /// Sibling method symbol, if found.
        /// </summary>
        public readonly IMethodSymbol? SiblingMethod;

        /// <summary>
        /// Is sibling has the appropriate name (SomeMethod <-> SomeMethodAsync).
        /// </summary>
        public readonly bool IsStrictCompliance;

        public SiblingMethodResult()
        {
            SiblingMethod = null;
            IsStrictCompliance = false;
        }

        public SiblingMethodResult(
            IMethodSymbol siblingMethod,
            bool isStrictCompliance
            )
        {
            SiblingMethod = siblingMethod.PartialImplementationPart ?? siblingMethod;
            IsStrictCompliance = isStrictCompliance;
        }
    }
}
