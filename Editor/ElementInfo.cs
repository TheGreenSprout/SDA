using System.Collections.Generic;


public interface ITypeInfo
{
    string Name { get; }
    string Summary { get; set; }
}

public class ClassInfo : ITypeInfo
{
    public string Name { get; set; }
    public string Summary { get; set; } = "";
    public bool IncludeInDoc { get; set; } = true;
    public string Namespace { get; set; } = "";

    public List<FieldInfo> Fields { get; set; } = new List<FieldInfo>();
    public List<MethodInfo> Methods { get; set; } = new List<MethodInfo>();

    public ClassInfo(string name, string ns, string summary = "")
    {
        Name = name;
        Namespace = ns;
        Summary = summary;
    }
}

public class FieldInfo
{
    public string Name { get; set; }
    public string Type { get; set; }
    public string Summary { get; set; } = "";

    public FieldInfo(string name, string type)
    {
        Name = name;
        Type = type;
    }
}

public class MethodInfo
{
    public string Name { get; set; }
    public string ReturnType { get; set; }
    public string Summary { get; set; } = "";
    public string ReturnDescription { get; set; } = "";
    public bool IncludeInDoc { get; set; } = true;
    public List<ParamInfo> Parameters { get; set; } = new List<ParamInfo>();

    public string UniqueSignature => GenerateSignature();

    private string GenerateSignature()
    {
        var paramTypes = string.Join(",", Parameters.ConvertAll(p => p.Type.Trim()));
        return $"{Name}({paramTypes}) : {ReturnType}";
    }

    public MethodInfo(string name, string returnType)
    {
        Name = name;
        ReturnType = returnType;
    }
}

public class ParamInfo
{
    public string Name { get; set; }
    public string Type { get; set; }
    public string Description { get; set; } = "";

    public ParamInfo(string name, string type)
    {
        Name = name;
        Type = type;
    }
}

public class EnumInfo : ITypeInfo
{
    public string Name { get; set; }
    public string Summary { get; set; } = "";
    public string Namespace { get; set; } = "";
    public List<EnumMemberInfo> Members { get; set; } = new List<EnumMemberInfo>();

    public EnumInfo(string name, string ns)
    {
        Name = name;
        Namespace = ns;
    }
}

public class EnumMemberInfo
{
    public string Name { get; set; }
    public string Summary { get; set; } = "";

    public EnumMemberInfo(string name)
    {
        Name = name;
    }
}

public class InterfaceInfo : ITypeInfo
{
    public string Name { get; set; }
    public string Summary { get; set; } = "";
    public string Namespace { get; set; } = "";
    public List<MethodInfo> Methods { get; set; } = new List<MethodInfo>();

    public InterfaceInfo(string name, string ns, string summary)
    {
        Name = name;
        Namespace = ns;
        Summary = summary;
    }
}

public class StructInfo : ITypeInfo
{
    public string Name { get; set; }
    public string Summary { get; set; } = "";
    public string Namespace { get; set; } = "";
    public List<FieldInfo> Fields { get; set; } = new List<FieldInfo>();
    public List<MethodInfo> Methods { get; set; } = new List<MethodInfo>();

    public StructInfo(string name, string ns, string summary)
    {
        Name = name;
        Namespace = ns;
        Summary = summary;
    }
}