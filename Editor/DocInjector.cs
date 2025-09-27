/*
⚠️‼️ AI ASSISTED CODE

This code was written with the assistance of AI.
*/



using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public static class DocInjector
{
    public static void InjectToFile(
        string filePath,
        List<ClassInfo> classes,
        List<EnumInfo> enums,
        List<StructInfo> structs,
        List<InterfaceInfo> interfaces)
    {
        var original = File.ReadAllLines(filePath);
        var lines = RemoveExistingXmlDocs(original);

        var injects = new List<(int idx, List<string> xml)>();

        // Parse file with Roslyn
        var tree = CSharpSyntaxTree.ParseText(string.Join("\n", lines));
        var root = tree.GetCompilationUnitRoot();

        // Helper: find line number from syntax node
        int GetLine(SyntaxNode node) =>
            node.GetLocation().GetLineSpan().StartLinePosition.Line;

        // ---- Classes ----
        foreach (var c in classes)
        {
            var classDecl = root.DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .FirstOrDefault(cd => cd.Identifier.Text == c.Name);

            if (classDecl != null)
            {
                int idx = GetLine(classDecl);
                injects.Add((idx, GenerateClassXml(c.Summary, GetIndentation(lines[idx]))));

                foreach (var m in c.Methods)
                {
                    var methodDecl = classDecl.Members
                        .OfType<MethodDeclarationSyntax>()
                        .FirstOrDefault(md => MatchesSignature(md, m));

                    if (methodDecl != null)
                    {
                        int midx = GetLine(methodDecl);
                        injects.Add((midx, GenerateMethodXml(m, GetIndentation(lines[midx]), m)));
                    }
                }
            }
        }

        // ---- Structs ----
        foreach (var s in structs)
        {
            var structDecl = root.DescendantNodes()
                .OfType<StructDeclarationSyntax>()
                .FirstOrDefault(sd => sd.Identifier.Text == s.Name);

            if (structDecl != null)
            {
                int idx = GetLine(structDecl);
                injects.Add((idx, GenerateClassXml(s.Summary, GetIndentation(lines[idx]))));

                foreach (var m in s.Methods)
                {
                    var methodDecl = structDecl.Members
                        .OfType<MethodDeclarationSyntax>()
                        .FirstOrDefault(md => MatchesSignature(md, m));

                    if (methodDecl != null)
                    {
                        int midx = GetLine(methodDecl);
                        injects.Add((midx, GenerateMethodXml(m, GetIndentation(lines[midx]), m)));
                    }
                }
            }
        }

        // ---- Interfaces ----
        foreach (var i in interfaces)
        {
            var ifaceDecl = root.DescendantNodes()
                .OfType<InterfaceDeclarationSyntax>()
                .FirstOrDefault(id => id.Identifier.Text == i.Name);

            if (ifaceDecl != null)
            {
                int idx = GetLine(ifaceDecl);
                injects.Add((idx, GenerateClassXml(i.Summary, GetIndentation(lines[idx]))));

                foreach (var m in i.Methods)
                {
                    var methodDecl = ifaceDecl.Members
                        .OfType<MethodDeclarationSyntax>()
                        .FirstOrDefault(md => MatchesSignature(md, m));

                    if (methodDecl != null)
                    {
                        int midx = GetLine(methodDecl);
                        injects.Add((midx, GenerateMethodXml(m, GetIndentation(lines[midx]), m)));
                    }
                }
            }
        }

        // ---- Enums ----
        foreach (var e in enums)
        {
            var enumDecl = root.DescendantNodes()
                .OfType<EnumDeclarationSyntax>()
                .FirstOrDefault(ed => ed.Identifier.Text == e.Name);

            if (enumDecl != null)
            {
                int idx = GetLine(enumDecl);
                injects.Add((idx, GenerateEnumXml(e.Summary, GetIndentation(lines[idx]))));

                foreach (var member in e.Members)
                {
                    var enumMember = enumDecl.Members
                        .FirstOrDefault(em => em.Identifier.Text == member.Name);

                    if (enumMember != null)
                    {
                        int midx = GetLine(enumMember);
                        injects.Add((midx, GenerateEnumMemberXml(member.Summary, GetIndentation(lines[midx]))));
                    }
                }
            }
        }

        // ---- Apply injects ----
        injects.Sort((a, b) => a.idx.CompareTo(b.idx));
        int offset = 0;
        foreach (var (idx, xml) in injects)
        {
            lines.InsertRange(idx + offset, xml);
            offset += xml.Count;
        }

        File.WriteAllLines(filePath, lines);
    }

    private static bool MatchesSignature(MethodDeclarationSyntax md, MethodInfo info)
    {
        // Compare name first
        if (md.Identifier.Text != info.Name) return false;

        // Compare parameter types count & names
        var methodParams = md.ParameterList.Parameters
            .Select(p => p.Type?.ToString().Trim())
            .ToList();

        var infoParams = info.Parameters
            .Select(p => p.Type.Trim())
            .ToList();

        if (methodParams.Count != infoParams.Count) return false;

        for (int i = 0; i < methodParams.Count; i++)
        {
            if (methodParams[i] != infoParams[i]) return false;
        }

        // Compare return type
        var returnType = md.ReturnType.ToString().Trim();
        return returnType == info.ReturnType.Trim();
    }

    private static List<string> RemoveExistingXmlDocs(string[] lines)
    {
        var outL = new List<string>();
        bool inside = false;
        foreach (var l in lines)
        {
            var t = l.TrimStart();
            if (t.StartsWith("#region XML doc")) { inside = true; continue; }
            if (inside && t.StartsWith("#endregion")) { inside = false; continue; }
            if (!inside) outL.Add(l);
        }
        return outL;
    }

    private static string GetIndentation(string line)
    {
        int i = 0;
        while (i < line.Length && char.IsWhiteSpace(line[i])) i++;
        return line.Substring(0, i);
    }

    private static List<string> GenerateClassXml(string summary, string indent)
    {
        if (string.IsNullOrWhiteSpace(summary)) return new List<string>();
        return new List<string>
        {
            $"{indent}#region XML doc",
            $"{indent}/// <summary>",
            $"{indent}/// {summary.Trim()}",
            $"{indent}/// </summary>",
            $"{indent}#endregion"
        };
    }

    private static List<string> GenerateEnumXml(string summary, string indent)
    {
        return GenerateClassXml(summary, indent);
    }

    private static List<string> GenerateEnumMemberXml(string summary, string indent)
    {
        if (string.IsNullOrWhiteSpace(summary)) return new List<string>();
        return new List<string>
        {
            $"{indent}#region XML doc",
            $"{indent}/// <summary>{summary.Trim()}</summary>",
            $"{indent}#endregion"
        };
    }

    private static List<string> GenerateMethodXml(MethodInfo m, string indent, MethodInfo info)
    {
        var xml = new List<string>();
        bool hasDoc = !string.IsNullOrWhiteSpace(info.Summary)
                      || info.Parameters.Exists(p => !string.IsNullOrWhiteSpace(p.Description))
                      || (!info.ReturnType.Equals("void", System.StringComparison.OrdinalIgnoreCase) &&
                          !string.IsNullOrWhiteSpace(info.ReturnDescription));

        if (!hasDoc) return xml;

        xml.Add($"{indent}#region XML doc");
        if (!string.IsNullOrWhiteSpace(info.Summary))
        {
            xml.Add($"{indent}/// <summary>");
            xml.Add($"{indent}/// {info.Summary.Trim()}");
            xml.Add($"{indent}/// </summary>");
        }

        foreach (var p in info.Parameters)
        {
            if (!string.IsNullOrWhiteSpace(p.Description))
                xml.Add($"{indent}/// <param name=\"{p.Name}\">{p.Description.Trim()}</param>");
        }

        if (!info.ReturnType.Equals("void", System.StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(info.ReturnDescription))
        {
            xml.Add($"{indent}/// <returns>{info.ReturnDescription.Trim()}</returns>");
        }

        xml.Add($"{indent}#endregion");
        return xml;
    }
}