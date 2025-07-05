using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace AutofacGodotDi;

[Generator]
public class InjectSourceGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(() => new InjectSyntaxReceiver());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        if (context.SyntaxReceiver is not InjectSyntaxReceiver receiver)
            return;

        var injectMethods = new List<(INamedTypeSymbol classSymbol, IMethodSymbol method)>();

        var compilation = context.Compilation;

        var injectAttr = compilation
            .GetTypeByMetadataName("AutofacGodotDi.Attributes.InjectAttribute");

        foreach (var methodDecl in receiver.CandidateMethods)
        {
            var model = compilation.GetSemanticModel(methodDecl.SyntaxTree);
            var symbol = model.GetDeclaredSymbol(methodDecl) as IMethodSymbol;
            if (symbol == null)
                continue;

            if (symbol.GetAttributes().Any(a =>
                SymbolEqualityComparer.Default.Equals(a.AttributeClass, injectAttr)))
            {
                if (symbol.ContainingType is INamedTypeSymbol classSymbol)
                    injectMethods.Add((classSymbol, symbol));
            }
        }

        var bindingClasses = new StringBuilder();
        var registrarBuilder = new StringBuilder();

        registrarBuilder.AppendLine("using System;");
        registrarBuilder.AppendLine("using Autofac;");
        registrarBuilder.AppendLine("using Godot;");
        registrarBuilder.AppendLine("using AutofacGodotDi;");
        registrarBuilder.AppendLine();
        registrarBuilder.AppendLine("public static class BindingRegistrar");
        registrarBuilder.AppendLine("{");
        registrarBuilder.AppendLine("    public static void RegisterBindings()");
        registrarBuilder.AppendLine("    {");

        foreach (var group in injectMethods.GroupBy(x => x.classSymbol, SymbolEqualityComparer.Default))
        {
            var classSymbol = group.Key;
            var className = classSymbol.Name;
            var fullName = classSymbol.ToDisplayString();
            var bindingClassName = $"{className}Binding";

            var bindMethod = new StringBuilder();
            bindMethod.AppendLine("using Autofac;");
            bindMethod.AppendLine("using Godot;");
            bindMethod.AppendLine();
            bindMethod.AppendLine($"public static class {bindingClassName}");
            bindMethod.AppendLine("{");
            bindMethod.AppendLine("    public static void Bind(Node node, ILifetimeScope container)");
            bindMethod.AppendLine("    {");
            bindMethod.AppendLine($"        if (node is {fullName} instance)");
            bindMethod.AppendLine("        {");

            foreach (var (_, method) in group)
            {
                var parameters = method.Parameters
                    .Select(p => $"container.Resolve<{p.Type.ToDisplayString()}>()")
                    .ToArray();

                bindMethod.AppendLine($"            instance.{method.Name}({string.Join(", ", parameters)});");
            }

            bindMethod.AppendLine("        }");
            bindMethod.AppendLine("    }");
            bindMethod.AppendLine("}");

            context.AddSource($"{bindingClassName}.g.cs", bindMethod.ToString());

            registrarBuilder.AppendLine(
                $"        StaticDependencyInjector.RegisteredBindings[typeof({fullName})] = {bindingClassName}.Bind;");
        }

        registrarBuilder.AppendLine("    }");
        registrarBuilder.AppendLine("}");

        context.AddSource("BindingRegistrar.g.cs", registrarBuilder.ToString());
    }
}
