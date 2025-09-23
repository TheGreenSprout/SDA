/*
⚠️‼️ AI ASSISTED CODE

This code was written with the assistance of AI.
*/



using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

public static class DocInjector
{
    public static void InjectToFile(string filePath, List<ClassInfo> classes, List<EnumInfo> enums, List<StructInfo> structs, List<InterfaceInfo> interfaces)
    {
        var original = File.ReadAllLines(filePath);
        var lines = RemoveExistingXmlDocs(original);
        var injects = new List<(int idx, List<string> xml)>();

        var methodLineMap = new Dictionary<string, Dictionary<string, int>>();
        string currentTypeName = null;

        for (int i = 0; i < lines.Count; i++)
        {
            string line = lines[i];

            // Track current class/struct/interface name
            if (Regex.IsMatch(line, @"\bclass\b"))
                currentTypeName = ExtractName(line, "class");
            else if (Regex.IsMatch(line, @"\bstruct\b"))
                currentTypeName = ExtractName(line, "struct");
            else if (Regex.IsMatch(line, @"\binterface\b"))
                currentTypeName = ExtractName(line, "interface");

            var match = Regex.Match(line,
                @"^\s*(?:public|protected|internal|private)?\s*(?:static\s+|async\s+|virtual\s+|override\s+|sealed\s+|new\s+)*" +
                @"(?<rtype>[\w\<\>\[\]\.]+)\s+" +
                @"(?<name>\w+)\s*\((?<params>[^\)]*)\)\s*(\{|where|\n)?");

            if (match.Success && !string.IsNullOrWhiteSpace(currentTypeName))
            {
                string name = match.Groups["name"].Value;
                string rtype = match.Groups["rtype"].Value;
                string plist = match.Groups["params"].Value;

                string signature = GenerateSignature(name, plist, rtype);

                if (!methodLineMap.ContainsKey(currentTypeName))
                    methodLineMap[currentTypeName] = new Dictionary<string, int>();

                methodLineMap[currentTypeName][signature] = i;
            }
        }

        foreach (var c in classes)
        {
            int idx = FindLineIndex(lines, $"class {c.Name}");
            if (idx >= 0)
            {
                injects.Add((idx, GenerateClassXml(c.Summary, GetIndentation(lines[idx]))));
                foreach (var m in c.Methods)
                {
                    if (methodLineMap.TryGetValue(c.Name, out var map) &&
                        map.TryGetValue(m.UniqueSignature, out int lineIdx))
                    {
                        injects.Add((lineIdx, GenerateMethodXml(m, GetIndentation(lines[lineIdx]), m)));
                    }
                }
            }
        }

        foreach (var s in structs)
        {
            int idx = FindLineIndex(lines, $"struct {s.Name}");
            if (idx >= 0)
            {
                injects.Add((idx, GenerateClassXml(s.Summary, GetIndentation(lines[idx]))));
                foreach (var m in s.Methods)
                {
                    if (methodLineMap.TryGetValue(s.Name, out var map) &&
                        map.TryGetValue(m.UniqueSignature, out int lineIdx))
                    {
                        injects.Add((lineIdx, GenerateMethodXml(m, GetIndentation(lines[lineIdx]), m)));
                    }
                }
            }
        }

        foreach (var i in interfaces)
        {
            int idx = FindLineIndex(lines, $"interface {i.Name}");
            if (idx >= 0)
            {
                injects.Add((idx, GenerateClassXml(i.Summary, GetIndentation(lines[idx]))));
                foreach (var m in i.Methods)
                {
                    if (methodLineMap.TryGetValue(i.Name, out var map) &&
                        map.TryGetValue(m.UniqueSignature, out int lineIdx))
                    {
                        injects.Add((lineIdx, GenerateMethodXml(m, GetIndentation(lines[lineIdx]), m)));
                    }
                }
            }
        }

        foreach (var e in enums)
        {
            int idx = FindLineIndex(lines, $"enum {e.Name}");
            if (idx >= 0)
            {
                injects.Add((idx, GenerateEnumXml(e.Summary, GetIndentation(lines[idx]))));
                foreach (var m in e.Members)
                {
                    int midx = FindLineIndex(lines, m.Name, idx);
                    if (midx >= 0)
                    {
                        injects.Add((midx, GenerateEnumMemberXml(m.Summary, GetIndentation(lines[midx]))));
                    }
                }
            }
        }

        injects.Sort((a, b) => a.idx.CompareTo(b.idx));
        int offset = 0;
        foreach (var (idx, xml) in injects)
        {
            lines.InsertRange(idx + offset, xml);
            offset += xml.Count;
        }

        File.WriteAllLines(filePath, lines);
    }

    private static string ExtractName(string line, string type)
    {
        var m = Regex.Match(line, $@"\b{type}\s+(\w+)");
        return m.Success ? m.Groups[1].Value : null;
    }

    private static string GenerateSignature(string name, string plist, string returnType)
    {
        var types = new List<string>();
        foreach (var part in plist.Split(','))
        {
            var bits = part.Trim().Split(' ');
            if (bits.Length >= 2)
                types.Add(string.Join(" ", bits, 0, bits.Length - 1));
        }
        return $"{name}({string.Join(",", types)}) : {returnType}";
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

    private static int FindLineIndex(List<string> lines, string keyword, int startAt = 0)
    {
        for (int i = startAt; i < lines.Count; i++)
        {
            if (lines[i].Contains(keyword)) return i;
        }
        return -1;
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