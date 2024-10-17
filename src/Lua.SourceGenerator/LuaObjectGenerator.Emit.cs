using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Lua.SourceGenerator;

partial class LuaObjectGenerator
{
    static bool TryEmit(TypeMetadata typeMetadata, CodeBuilder builder, SymbolReferences references, Compilation compilation, in SourceProductionContext context)
    {
        try
        {
            var error = false;

            // must be partial
            if (!typeMetadata.IsPartial())
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.MustBePartial,
                    typeMetadata.Syntax.Identifier.GetLocation(),
                    typeMetadata.Symbol.Name));
                error = true;
            }

            // nested is not allowed
            if (typeMetadata.IsNested())
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.NestedNotAllowed,
                    typeMetadata.Syntax.Identifier.GetLocation(),
                    typeMetadata.Symbol.Name));
                error = true;
            }

            // verify abstract/interface
            if (typeMetadata.Symbol.IsAbstract)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.AbstractNotAllowed,
                    typeMetadata.Syntax.Identifier.GetLocation(),
                    typeMetadata.TypeName));
                error = true;
            }

            if (!ValidateMembers(typeMetadata, compilation, references, context))
            {
                error = true;
            }

            if (error)
            {
                return false;
            }

            builder.AppendLine("// <auto-generated />");
            builder.AppendLine("#nullable enable");
            builder.AppendLine("#pragma warning disable CS0162 // Unreachable code");
            builder.AppendLine("#pragma warning disable CS0219 // Variable assigned but never used");
            builder.AppendLine("#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.");
            builder.AppendLine("#pragma warning disable CS8601 // Possible null reference assignment");
            builder.AppendLine("#pragma warning disable CS8602 // Possible null return");
            builder.AppendLine("#pragma warning disable CS8604 // Possible null reference argument for parameter");
            builder.AppendLine("#pragma warning disable CS8631 // The type cannot be used as type parameter in the generic type or method");
            builder.AppendLine();

            var ns = typeMetadata.Symbol.ContainingNamespace;
            if (!ns.IsGlobalNamespace)
            {
                builder.AppendLine($"namespace {ns}");
                builder.BeginBlock();
            }

            var typeDeclarationKeyword = (typeMetadata.Symbol.IsRecord, typeMetadata.Symbol.IsValueType) switch
            {
                (true, true) => "record struct",
                (true, false) => "record",
                (false, true) => "struct",
                (false, false) => "class",
            };

            using var _ = builder.BeginBlockScope($"partial {typeDeclarationKeyword} {typeMetadata.TypeName} : global::Lua.ILuaUserData");

            var metamethodSet = new HashSet<LuaObjectMetamethod>();

            if (!TryEmitMethods(typeMetadata, builder, metamethodSet, context))
            {
                return false;
            }

            if (!TryEmitIndexMetamethod(typeMetadata, builder, context))
            {
                return false;
            }

            if (!TryEmitNewIndexMetamethod(typeMetadata, builder, context))
            {
                return false;
            }

            if (!TryEmitMetatable(builder, metamethodSet, context))
            {
                return false;
            }

            // implicit operator
            builder.AppendLine($"public static implicit operator global::Lua.LuaValue({typeMetadata.FullTypeName} value)");
            using (builder.BeginBlockScope())
            {
                builder.AppendLine("return new(value);");
            }

            if (!ns.IsGlobalNamespace) builder.EndBlock();

            builder.AppendLine("#pragma warning restore CS0162 // Unreachable code");
            builder.AppendLine("#pragma warning restore CS0219 // Variable assigned but never used");
            builder.AppendLine("#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.");
            builder.AppendLine("#pragma warning restore CS8601 // Possible null reference assignment");
            builder.AppendLine("#pragma warning restore CS8602 // Possible null return");
            builder.AppendLine("#pragma warning restore CS8604 // Possible null reference argument for parameter");
            builder.AppendLine("#pragma warning restore CS8631 // The type cannot be used as type parameter in the generic type or method");
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    static bool ValidateMembers(TypeMetadata typeMetadata, Compilation compilation, SymbolReferences references, in SourceProductionContext context)
    {
        var isValid = true;

        foreach (var property in typeMetadata.Properties)
        {
            if (SymbolEqualityComparer.Default.Equals(property.Type, references.LuaValue)) continue;
            if (SymbolEqualityComparer.Default.Equals(property.Type, typeMetadata.Symbol)) continue;

            var conversion = compilation.ClassifyConversion(property.Type, references.LuaValue);
            if (!conversion.Exists)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.InvalidPropertyType,
                    property.Symbol.Locations.FirstOrDefault(),
                    property.Type.Name));

                isValid = false;
            }
        }

        foreach (var method in typeMetadata.Methods)
        {
            if (!method.Symbol.ReturnsVoid)
            {
                var typeSymbol = method.Symbol.ReturnType;

                if (method.IsAsync)
                {
                    var namedType = (INamedTypeSymbol)typeSymbol;
                    if (namedType.TypeArguments.Length == 0) goto PARAMETERS;

                    typeSymbol = namedType.TypeArguments[0];
                }

                if (SymbolEqualityComparer.Default.Equals(typeSymbol, references.LuaValue)) goto PARAMETERS;
                if (SymbolEqualityComparer.Default.Equals(typeSymbol, typeMetadata.Symbol)) goto PARAMETERS;

                var conversion = compilation.ClassifyConversion(typeSymbol, references.LuaValue);
                if (!conversion.Exists)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.InvalidReturnType,
                        typeSymbol.Locations.FirstOrDefault(),
                        typeSymbol.Name));

                    isValid = false;
                }
            }

        PARAMETERS:
            foreach (var typeSymbol in method.Symbol.Parameters
                .Select(x => x.Type))
            {
                if (SymbolEqualityComparer.Default.Equals(typeSymbol, references.LuaValue)) continue;
                if (SymbolEqualityComparer.Default.Equals(typeSymbol, typeMetadata.Symbol)) continue;

                var conversion = compilation.ClassifyConversion(typeSymbol, references.LuaValue);
                if (!conversion.Exists)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.InvalidParameterType,
                        typeSymbol.Locations.FirstOrDefault(),
                        typeSymbol.Name));

                    isValid = false;
                }
            }
        }

        return isValid;
    }

    static bool TryEmitIndexMetamethod(TypeMetadata typeMetadata, CodeBuilder builder, in SourceProductionContext context)
    {
        builder.AppendLine("static readonly global::Lua.LuaFunction __metamethod_index = new global::Lua.LuaFunction((context, buffer, ct) =>");

        using (builder.BeginBlockScope())
        {
            builder.AppendLine($"var userData = context.GetArgument<{typeMetadata.FullTypeName}>(0);");
            builder.AppendLine($"var key = context.GetArgument<global::System.String>(1);");
            builder.AppendLine("var result = key switch");

            using (builder.BeginBlockScope())
            {
                foreach (var propertyMetadata in typeMetadata.Properties)
                {
                    if (propertyMetadata.IsStatic)
                    {
                        builder.AppendLine(@$"""{propertyMetadata.LuaMemberName}"" => new global::Lua.LuaValue({typeMetadata.FullTypeName}.{propertyMetadata.Symbol.Name}),");
                    }
                    else
                    {
                        builder.AppendLine(@$"""{propertyMetadata.LuaMemberName}"" => new global::Lua.LuaValue(userData.{propertyMetadata.Symbol.Name}),");
                    }
                }

                foreach (var methodMetadata in typeMetadata.Methods
                    .Where(x => x.HasMemberAttribute))
                {
                    builder.AppendLine(@$"""{methodMetadata.LuaMemberName}"" => new global::Lua.LuaValue(__function_{methodMetadata.LuaMemberName}),");
                }

                builder.AppendLine(@$"_ => global::Lua.LuaValue.Nil,");
            }
            builder.AppendLine(";");

            builder.AppendLine("buffer.Span[0] = result;");
            builder.AppendLine("return new(1);");
        }

        builder.AppendLine(");");

        return true;
    }

    static bool TryEmitNewIndexMetamethod(TypeMetadata typeMetadata, CodeBuilder builder, in SourceProductionContext context)
    {
        builder.AppendLine("static readonly global::Lua.LuaFunction __metamethod_newindex = new global::Lua.LuaFunction((context, buffer, ct) =>");

        using (builder.BeginBlockScope())
        {
            builder.AppendLine($"var userData = context.GetArgument<{typeMetadata.FullTypeName}>(0);");
            builder.AppendLine($"var key = context.GetArgument<global::System.String>(1);");
            builder.AppendLine("switch (key)");

            using (builder.BeginBlockScope())
            {
                foreach (var propertyMetadata in typeMetadata.Properties)
                {
                    builder.AppendLine(@$"case ""{propertyMetadata.LuaMemberName}"":");

                    using (builder.BeginIndentScope())
                    {
                        if (propertyMetadata.IsReadOnly)
                        {
                            builder.AppendLine($@"throw new global::Lua.LuaRuntimeException(context.State.GetTraceback(), $""'{{key}}' cannot overwrite."");");
                        }
                        else if (propertyMetadata.IsStatic)
                        {
                            builder.AppendLine(@$"{typeMetadata.FullTypeName}.{propertyMetadata.Symbol.Name} = context.GetArgument<{propertyMetadata.TypeFullName}>(2);");
                            builder.AppendLine("break;");
                        }
                        else
                        {
                            builder.AppendLine(@$"userData.{propertyMetadata.Symbol.Name} = context.GetArgument<{propertyMetadata.TypeFullName}>(2);");
                            builder.AppendLine("break;");
                        }
                    }
                }

                foreach (var methodMetadata in typeMetadata.Methods
                    .Where(x => x.HasMemberAttribute))
                {
                    builder.AppendLine(@$"case ""{methodMetadata.LuaMemberName}"":");

                    using (builder.BeginIndentScope())
                    {
                        builder.AppendLine($@"throw new global::Lua.LuaRuntimeException(context.State.GetTraceback(), $""'{{key}}' cannot overwrite."");");
                    }
                }

                builder.AppendLine(@$"default:");

                using (builder.BeginIndentScope())
                {
                    builder.AppendLine(@$"throw new global::Lua.LuaRuntimeException(context.State.GetTraceback(), $""'{{key}}'  not found."");");
                }
            }

            builder.AppendLine("return new(0);");
        }

        builder.AppendLine(");");

        return true;
    }

    static bool TryEmitMethods(TypeMetadata typeMetadata, CodeBuilder builder, HashSet<LuaObjectMetamethod> metamethodSet, in SourceProductionContext context)
    {
        builder.AppendLine();

        foreach (var methodMetadata in typeMetadata.Methods)
        {
            string? functionName = null;

            if (methodMetadata.HasMemberAttribute)
            {
                functionName = $"__function_{methodMetadata.LuaMemberName}";
                EmitMethodFunction(functionName, typeMetadata, methodMetadata, builder);
            }

            if (methodMetadata.HasMetamethodAttribute)
            {
                if (!metamethodSet.Add(methodMetadata.Metamethod))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.DuplicateMetamethod,
                        methodMetadata.Symbol.Locations.FirstOrDefault(),
                        typeMetadata.TypeName,
                        methodMetadata.Metamethod
                    ));

                    continue;
                }

                if (functionName == null)
                {
                    EmitMethodFunction($"__metamethod_{methodMetadata.Metamethod}", typeMetadata, methodMetadata, builder);
                }
                else
                {
                    builder.AppendLine($"static global::Lua.LuaFunction __metamethod_{methodMetadata.Metamethod} => {functionName};");
                }
            }
        }

        return true;
    }

    static void EmitMethodFunction(string functionName, TypeMetadata typeMetadata, MethodMetadata methodMetadata, CodeBuilder builder)
    {
        builder.AppendLine($"static readonly global::Lua.LuaFunction {functionName} = new global::Lua.LuaFunction({(methodMetadata.IsAsync ? "async" : "")} (context, buffer, ct) =>");

        using (builder.BeginBlockScope())
        {
            var index = 0;

            if (!methodMetadata.IsStatic)
            {
                builder.AppendLine($"var userData = context.GetArgument<{typeMetadata.FullTypeName}>(0);");
                index++;
            }

            foreach (var parameter in methodMetadata.Symbol.Parameters)
            {
                builder.AppendLine($"var arg{index} = context.GetArgument<{parameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>({index});");
                index++;
            }

            if (methodMetadata.HasReturnValue)
            {
                builder.Append("var result = ");
            }

            if (methodMetadata.IsAsync)
            {
                builder.Append("await ", false);
            }

            if (methodMetadata.IsStatic)
            {
                builder.Append($"{typeMetadata.FullTypeName}.{methodMetadata.Symbol.Name}(", false);
                builder.Append(string.Join(",", Enumerable.Range(0, index).Select(x => $"arg{x}")), false);
                builder.AppendLine(");", false);
            }
            else
            {
                builder.Append($"userData.{methodMetadata.Symbol.Name}(");
                builder.Append(string.Join(",", Enumerable.Range(1, index - 1).Select(x => $"arg{x}")), false);
                builder.AppendLine(");", false);
            }

            if (methodMetadata.HasReturnValue)
            {
                builder.AppendLine("buffer.Span[0] = new global::Lua.LuaValue(result);");
                builder.AppendLine($"return {(methodMetadata.IsAsync ? "1" : "new(1)")};");
            }
            else
            {
                builder.AppendLine($"return {(methodMetadata.IsAsync ? "0" : "new(0)")};");
            }
        }
        builder.AppendLine(");");
        builder.AppendLine();
    }

    static bool TryEmitMetatable(CodeBuilder builder, IEnumerable<LuaObjectMetamethod> metamethods, in SourceProductionContext context)
    {
        builder.AppendLine("global::Lua.LuaTable? global::Lua.ILuaUserData.Metatable");
        using (builder.BeginBlockScope())
        {
            builder.AppendLine("get");
            using (builder.BeginBlockScope())
            {
                builder.AppendLine("if (__metatable != null) return __metatable;");
                builder.AppendLine();
                builder.AppendLine("__metatable = new();");
                builder.AppendLine("__metatable[global::Lua.Runtime.Metamethods.Index] = __metamethod_index;");
                builder.AppendLine("__metatable[global::Lua.Runtime.Metamethods.NewIndex] = __metamethod_newindex;");
                foreach (var metamethod in metamethods)
                {
                    builder.AppendLine($"__metatable[global::Lua.Runtime.Metamethods.{metamethod}] = __metamethod_{metamethod};");
                }
                builder.AppendLine("return __metatable;");
            }

            builder.AppendLine("set");
            using (builder.BeginBlockScope())
            {
                builder.AppendLine("__metatable = value;");
            }
        }

        builder.AppendLine("static global::Lua.LuaTable? __metatable;");
        builder.AppendLine();

        return true;
    }
}