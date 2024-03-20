﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace Zooyard.SourceGenerator;

/// <summary>
///  The generator of Rpc client
/// </summary>
[Generator(LanguageNames.CSharp)]
public class RpcClientGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
    }

    public void Execute(GeneratorExecutionContext context)
    {

        context.LogMessage("Client Generator started");

        if (context.SyntaxReceiver is not SyntaxReceiver receiver)
            return;

        var namespaceNameSet = new HashSet<string>();
        var clientClasses = new List<string>();
        const string namespaceName = "Zooyard";

        foreach (var @interface in receiver.CandidateInterfaces)
        {
            // first get the semantic model for the interface, and make sure it's annotated
            var model = context.Compilation.GetSemanticModel(@interface.SyntaxTree);
            if (model.GetDeclaredSymbol(@interface) is not INamedTypeSymbol symbol) continue;
            if (!symbol.GetAttributes().Any(ad =>
                    ad.AttributeClass?.BaseType?.ToDisplayString() == "Zooyard.DataAnnotations.ZooyardAttribute")) continue;

            var rpcAttribute = symbol.GetAttributes().FirstOrDefault(ad =>
                ad.AttributeClass?.BaseType?.ToDisplayString() == "Zooyard.DataAnnotations.ZooyardAttribute");

            if (rpcAttribute == null) continue;

            var generateClient = (bool?)rpcAttribute
                .NamedArguments
                .FirstOrDefault(kvp => kvp.Key == "GenerateClient").Value.Value ?? true;
            var generateDependencyInjection = (bool?)rpcAttribute
                .NamedArguments
                .FirstOrDefault(kvp => kvp.Key == "GenerateDependencyInjection").Value.Value ?? true;

            // Skip if both are false
            if (!generateClient && !generateDependencyInjection) continue;

            // below is the code to generate the client
            var serviceName = (string?)rpcAttribute.ConstructorArguments.FirstOrDefault().Value;

            context.LogMessage(serviceName ?? "Null");

            var className = symbol.Name.Substring(1) + "Client"; // Changed the class name
            var interfaceName = symbol.Name;

            if (generateDependencyInjection)
            {
                clientClasses.Add($"{interfaceName}, {className}");
            }


            var symbolNamespace = symbol.ContainingNamespace.ToDisplayString();

            namespaceNameSet.Add(symbolNamespace);


            var modelCollector = new ModelCollector();
            modelCollector.Visit(@interface.SyntaxTree.GetRoot());

            var stringBuilder = new StringBuilder();
            var usingList = GenerateUsing(modelCollector);
            stringBuilder.AppendLine("// <auto-generated/>");
            foreach (var usingStr in usingList)
            {
                stringBuilder.AppendLine(usingStr);
            }
            stringBuilder.AppendLine($"using {symbolNamespace};");
            stringBuilder.AppendLine();
            stringBuilder.AppendLine($"namespace {symbolNamespace}");
            stringBuilder.AppendLine("{");
            stringBuilder.AppendLine($"   public class {className} : ProxyExecutor, {interfaceName}");
            stringBuilder.AppendLine("   {");
            stringBuilder.AppendLine("         private readonly InterfaceMapping _interfaceMapping;");
            stringBuilder.AppendLine("         private readonly ZooyardInvoker _invoker;");
            stringBuilder.AppendLine("         private readonly Type _declaringType;");
            stringBuilder.AppendLine();
            stringBuilder.AppendLine($"         public {className}(ILogger<{interfaceName}> logger, IZooyardPools zooyardPools, IEnumerable<IInterceptor> interceptors)");
            stringBuilder.AppendLine("         {");
            stringBuilder.AppendLine($"             _declaringType = typeof({interfaceName});");
            stringBuilder.AppendLine("             var zooyardAttr = _declaringType.GetCustomAttribute<ZooyardAttribute>();");
            stringBuilder.AppendLine("             _invoker = new ZooyardInvoker(logger, zooyardPools, interceptors, zooyardAttr);");
            stringBuilder.AppendLine("             _interfaceMapping = this.GetType().GetInterfaceMap(_declaringType);");
            stringBuilder.AppendLine("         }");

            // Assume all methods return Task or Task<T>
            foreach (var member in symbol.GetMembers().OfType<IMethodSymbol>())
            {

                var parameters = string.Join(", ", member.Parameters.Select(p => $"{p.Type} {p.Name}"));
                var callParameters = string.Join(",", member.Parameters.Select(p => p.Name));
                var methodName = member.Name;

                var returnType = GetReturnType(member.ReturnType.ToString()); // Changed the return type

                string GetReturnType(string returnType)
                {
                    if (returnType == "void")
                    {
                        return "void";
                    }
                    else if (returnType.StartsWith("System.Threading.Tasks.Task"))
                    {
                        if (returnType == "System.Threading.Tasks.Task")
                        {
                            return "async Task";
                        }
                        else
                        {
                            return $"async Task<{GetTypeArguments(((INamedTypeSymbol)member.ReturnType).TypeArguments[0])}>";
                        }
                    }
                    else
                    {
                        //return member.ReturnType.ToString();
                        return GetTypeArguments(((INamedTypeSymbol)member.ReturnType).TypeArguments[0]);
                    }
                }

                var invokeAsync = GetInvoke(member.ReturnType.ToString()); // Changed the return type
                string GetInvoke(string returnType)
                {
                    if (returnType == "void")
                    {
                        return "_invoker.Invoke";
                    }
                    else if (returnType.StartsWith("System.Threading.Tasks.Task"))
                    {
                        if (returnType == "System.Threading.Tasks.Task")
                        {
                            return "await _invoker.InvokeAsync";
                        }
                        else
                        {
                            return $"return await _invoker.InvokeAsync<{GetTypeArguments(((INamedTypeSymbol)member.ReturnType).TypeArguments[0])}>";
                        }
                    }
                    else
                    {
                        //return member.ReturnType.ToString();
                        return $"return _invoker.Invoke<{GetTypeArguments(((INamedTypeSymbol)member.ReturnType).TypeArguments[0])}> ";
                    }
                }
                string GetTypeArguments(ITypeSymbol returnType)
                {
                    if (returnType is not INamedTypeSymbol namedType)
                    {
                        return "";
                    }

                    if (namedType.TypeArguments.Length == 0)
                    {
                        return namedType.Name;
                    }

                    var argStr = "<";
                    foreach (var arg in namedType.TypeArguments)
                    {
                        var argInner = GetTypeArguments(arg);
                        if (string.IsNullOrWhiteSpace(argInner))
                        {
                            continue;
                        }
                        argStr += argInner;
                        argStr += ", ";
                    }
                    if (argStr.Length > 1)
                    {
                        argStr = argStr.Substring(0, argStr.Length - 2);
                    }
                    argStr += ">";
                    return namedType.Name + argStr;
                }
                stringBuilder.AppendLine();
                stringBuilder.AppendLine("        [ZooyardImpl]");
                stringBuilder.AppendLine($"        public {returnType} {methodName}({parameters})");
                stringBuilder.AppendLine("        {");
                stringBuilder.AppendLine("             var stackTrace = new StackTrace(true);");
                stringBuilder.AppendLine("             var (mi, mtoken) = _invoker.GetInterfaceMethod(stackTrace,_interfaceMapping);");
                stringBuilder.AppendLine($"             object[] args = [{callParameters}];");
                stringBuilder.AppendLine("             var context = _invoker.GetMethodResolverContext(this, _declaringType, mi, mtoken, args);");


                stringBuilder.AppendLine($"             {invokeAsync}(context);");

                stringBuilder.AppendLine("        }");

                context.LogMessage($"Generator method: {methodName} success");
            }

            stringBuilder.AppendLine("    }");
            stringBuilder.AppendLine("}");

            context.AddSource($"{className}.g.cs", SourceText.From(stringBuilder.ToString(), Encoding.UTF8));

            context.LogMessage("Client Generator finished");

            context.LogMessage("DI Generator started");

            // Generate a new class to register clients to DI container
            var registrationClassBuilder = new StringBuilder();
            registrationClassBuilder.AppendLine("// <auto-generated/>");
            registrationClassBuilder.AppendLine("using Microsoft.Extensions.DependencyInjection;");
            foreach (var name in namespaceNameSet)
            {
                registrationClassBuilder.AppendLine($"using {name};");
            }
            registrationClassBuilder.AppendLine();
            registrationClassBuilder.AppendLine($"namespace {namespaceName}");
            registrationClassBuilder.AppendLine("{");
            registrationClassBuilder.AppendLine($"    public static class {className}Extensions");
            registrationClassBuilder.AppendLine("    {");
            registrationClassBuilder.AppendLine("        public static IServiceCollection AddAutoGeneratedClients(this IServiceCollection services)");
            registrationClassBuilder.AppendLine("        {");

            for (int i = 0; i < clientClasses.Count; i++)
            {
                registrationClassBuilder.AppendLine($"            services.AddSingleton<{clientClasses[i]}>();");
            }

            registrationClassBuilder.AppendLine("            return services;");
            registrationClassBuilder.AppendLine("        }");
            registrationClassBuilder.AppendLine("    }");
            registrationClassBuilder.AppendLine("}");

            context.AddSource($"{className}Extensions.g.cs", SourceText.From(registrationClassBuilder.ToString(), Encoding.UTF8));

            context.LogMessage("DI Generator finished");
        }
    }


    private List<string> GenerateUsing(ModelCollector typeSymbol)
    {
        var result = new[]{
                    "using Microsoft.Extensions.Logging;",
                    "using System.Diagnostics;",
                    "using System.Reflection;",
                    "using Zooyard.DataAnnotations;",
                    "using Zooyard.DynamicProxy;"
        };

        return result.Concat(typeSymbol.UsingDirectiveList).Distinct().OrderBy(w => w).ToList();
    }
    private class ModelCollector : CSharpSyntaxWalker
    {
        public List<string> UsingDirectiveList { get; set; } = new();
        public override void VisitUsingDirective(UsingDirectiveSyntax node)
        {
            if (node.Name != null)
            {
                UsingDirectiveList.Add("using " + node.Name.ToFullString() + ";");
            }
            base.VisitUsingDirective(node);
        }
    }

    private class SyntaxReceiver : ISyntaxReceiver
    {
        public List<InterfaceDeclarationSyntax> CandidateInterfaces { get; } = new();

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            // We only care about interface declarations
            if (syntaxNode is InterfaceDeclarationSyntax { AttributeLists.Count: > 0 } @interface)
            {
                CandidateInterfaces.Add(@interface);
            }
        }
    }
}

