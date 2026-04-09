using System;
using System.Reflection;
using ModelContextProtocol.Server;

class Program
{
    static void Main()
    {
        var type = typeof(RequestContext<ModelContextProtocol.Protocol.CallToolRequestParams>);
        Console.WriteLine("Type: " + type.FullName);
        foreach (var prop in type.GetProperties()) Console.WriteLine("PROP: " + prop.Name);
        foreach (var field in type.GetFields()) Console.WriteLine("FIELD: " + field.Name);
    }
}
