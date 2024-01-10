using Microsoft.CodeAnalysis;
using System;
using System.Threading;

namespace SyncToAsync.Extension
{
    public static class CompilationHelper
    {
        public static INamedTypeSymbol Void(
            this Compilation compilation
            )
        {
            if (compilation is null)
            {
                throw new ArgumentNullException(nameof(compilation));
            }

            return compilation.GetTypeByMetadataName("System.Void")!;
        }

        public static INamedTypeSymbol CancellationToken(
            this Compilation compilation
            )
        {
            if (compilation is null)
            {
                throw new ArgumentNullException(nameof(compilation));
            }

            return compilation.GetTypeByMetadataName("System.Threading.CancellationToken")!;
        }

        public static INamedTypeSymbol ValueTask(
            this Compilation compilation
            )
        {
            if (compilation is null)
            {
                throw new ArgumentNullException(nameof(compilation));
            }

            return compilation
                .GetTypeByMetadataName("System.Threading.Tasks.ValueTask")!
                .WithNullableAnnotation(NullableAnnotation.None) as INamedTypeSymbol
                ;
        }

        public static INamedTypeSymbol ValueTask(
            this Compilation compilation,
            params ITypeSymbol[] genericParameters
            )
        {
            if (compilation is null)
            {
                throw new ArgumentNullException(nameof(compilation));
            }

            return
                compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask`" + genericParameters.Length)!
                    .Construct(genericParameters)
                    .WithNullableAnnotation(NullableAnnotation.None) as INamedTypeSymbol
                    ;
        }

        public static INamedTypeSymbol Task(
            this Compilation compilation
            )
        {
            if (compilation is null)
            {
                throw new ArgumentNullException(nameof(compilation));
            }

            return compilation.GetTypeByMetadataName("System.Threading.Tasks.Task")!;
        }

        public static INamedTypeSymbol Task(
            this Compilation compilation,
            params ITypeSymbol[] genericParameters
            )
        {
            if (compilation is null)
            {
                throw new ArgumentNullException(nameof(compilation));
            }

            return
                compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`" + genericParameters.Length)!
                    .Construct(genericParameters)
                    ;
        }

    }
}
