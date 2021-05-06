using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SourceGenerator
{
    [Generator]
    public class Generator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            // Register a factory that can create our custom syntax receiver
            context.RegisterForSyntaxNotifications(() => new MySyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            const string code = @"
using System;
namespace NAMESPACE
{   
    public partial class CLASSNAME : IEquatable<CLASSNAME>{

        public static bool operator ==(CLASSNAME obj1, CLASSNAME obj2)
        {
            if (object.Equals(obj1, null))
            {
                if (object.Equals(obj2, null))
                {
                    return true;
                }
                return false;
            }
            return obj1.Equals(obj2);
        }

        public static bool operator !=(CLASSNAME obj1, CLASSNAME obj2)
        {
            return !(obj1 == obj2);
        }

        public bool Equals(CLASSNAME other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return EQUALITY;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != this.GetType())
            {
                return false;
            }

            return Equals((CLASSNAME) obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(PROPERTIES_COMMA_SEPARATED);
        }
    }
}";
            
            // the generator infrastructure will create a receiver and populate it
            // we can retrieve the populated instance via the context
            MySyntaxReceiver syntaxReceiver = (MySyntaxReceiver)context.SyntaxReceiver;

            // get the recorded user class
            var userClass = syntaxReceiver.ClassToAugment;
            if (userClass is null)
            {
                // if we didn't find the user class, there is nothing to do
                return;
            }

            foreach (var clasyntax in userClass)
            {
                var className = clasyntax.Identifier.ValueText;

                var ns = string.Empty;
                if (clasyntax.Parent is NamespaceDeclarationSyntax namespaceDeclarationSyntax)
                {
                    ns = namespaceDeclarationSyntax.Name.ToString();
                }

                StringBuilder propertiesCommaSeparated = new StringBuilder();
                StringBuilder equality = new StringBuilder();
                foreach (var m in clasyntax.Members.Where(x => x is PropertyDeclarationSyntax).Cast<PropertyDeclarationSyntax>())
                {
                    if (propertiesCommaSeparated.Length > 0)
                        propertiesCommaSeparated.Append(',');
                    propertiesCommaSeparated.Append(m.Identifier.ValueText);

                    if (equality.Length > 0)
                        equality.Append(" && ");
                    equality.Append(m.Identifier.ValueText);
                    equality.Append(" == other.");
                    equality.Append(m.Identifier.ValueText);
                }
                
                context.AddSource(ns+"."+className + ".Equals.cs", code
                    .Replace("NAMESPACE",ns)
                    .Replace("CLASSNAME",className)
                    .Replace("PROPERTIES_COMMA_SEPARATED",propertiesCommaSeparated.ToString())
                    .Replace("EQUALITY",equality.ToString()));
            }
        }
        
        class MySyntaxReceiver : ISyntaxReceiver
        {
            public List<ClassDeclarationSyntax> ClassToAugment { get; private set; } = new();
            
            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                if (syntaxNode is ClassDeclarationSyntax cds)
                {
                    if (cds.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
                    {
                        var parent = cds.BaseList?.Types.FirstOrDefault()?.Type.ToString();
                        if (parent == "ValueObject")
                        {
                            ClassToAugment.Add(cds);
                        }
                    }
                }
            }
        }
    }
}
