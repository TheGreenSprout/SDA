/*
⚠️‼️ AI ASSISTED CODE

This code was written with the assistance of AI.
*/



using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

/// <summary>
/// Holds parsed results for a file.
/// </summary>
public class ParseResult
{
    public List<ClassInfo> Classes { get; }
    public List<EnumInfo> Enums { get; }
    public List<ITypeInfo> OrderedTypes { get; }
    public List<StructInfo> Structs { get; }
    public List<InterfaceInfo> Interfaces { get; }

    public ParseResult(
        List<ClassInfo> classes,
        List<EnumInfo> enums,
        List<StructInfo> structs,
        List<InterfaceInfo> interfaces,
        List<ITypeInfo> ordered)
    {
        Classes = classes;
        Enums = enums;
        Structs = structs;
        Interfaces = interfaces;
        OrderedTypes = ordered;
    }
}

public static class CSharpParser
{
    public static ParseResult ParseFile(string filePath)
    {
        var originalText = File.ReadAllText(filePath);

        // Strip #if/#else/#endif but keep all code
        var processedText = StripPreprocessorConditionals(originalText);
        //var text = File.ReadAllText(filePath);
        
        var tree = CSharpSyntaxTree.ParseText(processedText);
        var root = tree.GetCompilationUnitRoot();

        var classes = new List<ClassInfo>();
        var enums = new List<EnumInfo>();
        var structs = new List<StructInfo>();
        var interfaces = new List<InterfaceInfo>();
        var ordered = new List<ITypeInfo>();

        string currentNamespace = "";

        // Walk top-level namespace(s)
        foreach (var ns in root.Members.OfType<NamespaceDeclarationSyntax>())
        {
            currentNamespace = ns.Name.ToString();
            foreach (var member in ns.Members)
            {
                ProcessMember(member, currentNamespace, classes, enums, structs, interfaces, ordered);
            }
        }

        // Handle types in global namespace
        foreach (var member in root.Members.Where(m => m is not NamespaceDeclarationSyntax))
        {
            ProcessMember(member, "", classes, enums, structs, interfaces, ordered);
        }

        return new ParseResult(classes, enums, structs, interfaces, ordered);
    }
    private static string StripPreprocessorConditionals(string source)
    {
        var lines = source.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        var output = new List<string>();

        foreach (var line in lines)
        {
            var trimmed = line.TrimStart();
            // Skip preprocessor directive lines but keep branch code
            if (trimmed.StartsWith("#if ") ||
                trimmed.StartsWith("#elif ") ||
                trimmed.StartsWith("#else") ||
                trimmed.StartsWith("#endif") ||
                trimmed.StartsWith("#define ") ||
                trimmed.StartsWith("#undef "))
            {
                // Replace with empty line to preserve line numbers
                output.Add("");
            }
            else
            {
                output.Add(line);
            }
        }

        return string.Join("\n", output);
    }

    private static void ProcessMember(
        MemberDeclarationSyntax member,
        string ns,
        List<ClassInfo> classes,
        List<EnumInfo> enums,
        List<StructInfo> structs,
        List<InterfaceInfo> interfaces,
        List<ITypeInfo> ordered)
    {
        switch (member)
        {
            case ClassDeclarationSyntax cls:
                {
                    var classInfo = new ClassInfo(cls.Identifier.Text, ns, ExtractSummary(cls));
                    classes.Add(classInfo);
                    ordered.Add(classInfo);

                    // Fields
                    foreach (var field in cls.Members.OfType<FieldDeclarationSyntax>())
                    {
                        foreach (var v in field.Declaration.Variables)
                        {
                            var typeName = field.Declaration.Type.ToString();
                            var fieldInfo = new FieldInfo(v.Identifier.Text, typeName)
                            {
                                Summary = ExtractSummary(field)
                            };
                            classInfo.Fields.Add(fieldInfo);
                        }
                    }

                    // Methods
                    foreach (var m in cls.Members.OfType<MethodDeclarationSyntax>())
                    {
                        classInfo.Methods.Add(ParseMethod(m));
                    }
                }
                break;

            case StructDeclarationSyntax st:
                {
                    var structInfo = new StructInfo(st.Identifier.Text, ns, ExtractSummary(st));
                    structs.Add(structInfo);
                    ordered.Add(structInfo);

                    foreach (var field in st.Members.OfType<FieldDeclarationSyntax>())
                    {
                        foreach (var v in field.Declaration.Variables)
                        {
                            var typeName = field.Declaration.Type.ToString();
                            var fieldInfo = new FieldInfo(v.Identifier.Text, typeName)
                            {
                                Summary = ExtractSummary(field)
                            };
                            structInfo.Fields.Add(fieldInfo);
                        }
                    }

                    foreach (var m in st.Members.OfType<MethodDeclarationSyntax>())
                    {
                        structInfo.Methods.Add(ParseMethod(m));
                    }
                }
                break;

            case InterfaceDeclarationSyntax iface:
                {
                    var interfaceInfo = new InterfaceInfo(iface.Identifier.Text, ns, ExtractSummary(iface));
                    interfaces.Add(interfaceInfo);
                    ordered.Add(interfaceInfo);

                    foreach (var m in iface.Members.OfType<MethodDeclarationSyntax>())
                    {
                        interfaceInfo.Methods.Add(ParseMethod(m));
                    }
                }
                break;

            case EnumDeclarationSyntax en:
                {
                    var enumInfo = new EnumInfo(en.Identifier.Text, ns)
                    {
                        Summary = ExtractSummary(en)
                    };
                    enums.Add(enumInfo);
                    ordered.Add(enumInfo);

                    foreach (var memberDecl in en.Members)
                    {
                        var memberInfo = new EnumMemberInfo(memberDecl.Identifier.Text)
                        {
                            Summary = ExtractSummary(memberDecl)
                        };
                        enumInfo.Members.Add(memberInfo);
                    }
                }
                break;
        }
    }

    private static MethodInfo ParseMethod(MethodDeclarationSyntax m)
    {
        var methodInfo = new MethodInfo(m.Identifier.Text, m.ReturnType.ToString())
        {
            Summary = ExtractSummary(m),
            ReturnDescription = ExtractReturns(m)
        };

        foreach (var p in m.ParameterList.Parameters)
        {
            var paramInfo = new ParamInfo(p.Identifier.Text, p.Type?.ToString() ?? "")
            {
                Description = ExtractParam(m, p.Identifier.Text)
            };
            methodInfo.Parameters.Add(paramInfo);
        }

        return methodInfo;
    }

    #region XML Doc Extraction

    private static string ExtractSummary(SyntaxNode node)
    {
        var trivia = node.GetLeadingTrivia()
            .Select(t => t.GetStructure())
            .OfType<DocumentationCommentTriviaSyntax>()
            .FirstOrDefault();
        if (trivia == null) return "";

        var summary = trivia.Content
            .OfType<XmlElementSyntax>()
            .FirstOrDefault(e => e.StartTag.Name.ToString() == "summary");

        if (summary == null)
            return "";

        // Extract text tokens only (ignore /// and whitespace trivia)
        var text = string.Concat(
            summary.Content
                .OfType<XmlTextSyntax>()
                .SelectMany(x => x.TextTokens)
                .Select(t => t.Text)
        );

        return text.Trim();
    }

    private static string ExtractReturns(MethodDeclarationSyntax node)
    {
        var trivia = node.GetLeadingTrivia()
            .Select(t => t.GetStructure())
            .OfType<DocumentationCommentTriviaSyntax>()
            .FirstOrDefault();
        if (trivia == null) return "";

        var returns = trivia.Content
            .OfType<XmlElementSyntax>()
            .FirstOrDefault(e => e.StartTag.Name.ToString() == "returns");

        if (returns == null)
            return "";

        var text = string.Concat(
            returns.Content
                .OfType<XmlTextSyntax>()
                .SelectMany(x => x.TextTokens)
                .Select(t => t.Text)
        );

        return text.Trim();
    }

    private static string ExtractParam(MethodDeclarationSyntax node, string paramName)
    {
        var trivia = node.GetLeadingTrivia()
            .Select(t => t.GetStructure())
            .OfType<DocumentationCommentTriviaSyntax>()
            .FirstOrDefault();
        if (trivia == null) return "";

        var paramElement = trivia.Content
            .OfType<XmlElementSyntax>()
            .FirstOrDefault(e => e.StartTag.Name.ToString() == "param" &&
                e.StartTag.Attributes.OfType<XmlNameAttributeSyntax>()
                    .Any(a => a.Identifier.Identifier.Text == "name" &&
                              a.Identifier.Equals("name") &&
                              a.Identifier.ToString().Contains("name") &&
                              a.Identifier.Identifier.Text == "name"));

        // Simpler approach:
        var xmlParams = trivia.Content.OfType<XmlElementSyntax>()
            .Where(e => e.StartTag.Name.ToString() == "param");
        foreach (var elem in xmlParams)
        {
            var nameAttr = elem.StartTag.Attributes.OfType<XmlNameAttributeSyntax>()
                .FirstOrDefault(a => a.Identifier.Identifier.Text == "name");
            if (nameAttr != null && nameAttr.Identifier.ToString().Contains("name") &&
                nameAttr.Identifier.Identifier.Text == "name" &&
                nameAttr.Identifier.ToString() == "name=\"" + paramName + "\"")
            {
                var txt = string.Concat(
                    elem.Content
                        .OfType<XmlTextSyntax>()
                        .SelectMany(x => x.TextTokens)
                        .Select(t => t.Text)
                );
                return txt.Trim();
            }
        }

        // Fallback: search manually
        foreach (var elem in xmlParams)
        {
            var text = elem.StartTag.ToFullString();
            if (text.Contains($"\"{paramName}\""))
            {
                var txt = string.Concat(
                    elem.Content
                        .OfType<XmlTextSyntax>()
                        .SelectMany(x => x.TextTokens)
                        .Select(t => t.Text)
                );
                return txt.Trim();
            }

        }

        return "";
    }

    #endregion
}
