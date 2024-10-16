using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Lua.SourceGenerator;

[Generator(LanguageNames.CSharp)]
public partial class LuaObjectGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "Lua.LuaObjectAttribute",
                static (node, cancellation) =>
                {
                    return node is ClassDeclarationSyntax
                        or RecordDeclarationSyntax;
                },
                static (context, cancellation) => { return context; })
            .Combine(context.CompilationProvider)
            .WithComparer(Comparer.Instance);

        context.RegisterSourceOutput(
            context.CompilationProvider.Combine(provider.Collect()),
            (sourceProductionContext, t) =>
            {
                var (compilation, list) = t;
                var references = SymbolReferences.Create(compilation);
                if (references == null) return;

                var builder = new CodeBuilder();

                var targetTypes = new List<TypeMetadata>();

                foreach (var (x, _) in list)
                {
                    var typeMeta = new TypeMetadata((TypeDeclarationSyntax)x.TargetNode, (INamedTypeSymbol)x.TargetSymbol, references);

                    if (TryEmit(typeMeta, builder, in sourceProductionContext))
                    {
                        var fullType = typeMeta.FullTypeName
                            .Replace("global::", "")
                            .Replace("<", "_")
                            .Replace(">", "_");

                        sourceProductionContext.AddSource($"{fullType}.LuaObject.g.cs", builder.ToString());
                        targetTypes.Add(typeMeta);
                    }

                    builder.Clear();
                }
            });
    }
}