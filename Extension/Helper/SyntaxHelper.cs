using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncToAsync.Extension
{
    public static class SyntaxHelper
    {
        public static T? Up<T>(this SyntaxNode node)
            where T : SyntaxNode
        {
            SyntaxNode? cnode = node;
            while (cnode != null)
            {
                if (cnode is T t)
                {
                    return t;
                }

                cnode = cnode.Parent;
            }

            return null;
        }
    }
}
