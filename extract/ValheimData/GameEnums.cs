using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

/// <summary>
/// Enum names read straight from assembly_valheim.dll, so numeric Unity values like
/// `m_itemType: 5` can be shown as names without hardcoding Valheim's enums.
/// </summary>
sealed class GameEnums
{
    readonly Dictionary<string, Dictionary<int, string>> byType = new();

    public GameEnums(string? assemblyPath)
    {
        if (assemblyPath is null || !File.Exists(assemblyPath))
        {
            Console.Error.WriteLine("Warning: assembly_valheim.dll not found; enum values will be shown as numbers");
            return;
        }

        using var stream = File.OpenRead(assemblyPath);
        using var pe = new PEReader(stream);
        var reader = pe.GetMetadataReader();

        foreach (var handle in reader.TypeDefinitions)
        {
            var type = reader.GetTypeDefinition(handle);
            if (!IsEnum(reader, type))
            {
                continue;
            }

            var values = new Dictionary<int, string>();
            foreach (var fieldHandle in type.GetFields())
            {
                var field = reader.GetFieldDefinition(fieldHandle);
                if ((field.Attributes & FieldAttributes.Literal) == 0)
                {
                    continue;
                }
                var constant = reader.GetConstant(field.GetDefaultValue());
                var value = Convert.ToInt32(reader.GetBlobReader(constant.Value).ReadConstant(constant.TypeCode));
                values.TryAdd(value, reader.GetString(field.Name));
            }
            byType[reader.GetString(type.Name)] = values;
        }
    }

    static bool IsEnum(MetadataReader reader, TypeDefinition type) =>
        type.BaseType.Kind == HandleKind.TypeReference &&
        reader.GetString(reader.GetTypeReference((TypeReferenceHandle)type.BaseType).Name) == "Enum";

    public bool Has(string enumName) => byType.ContainsKey(enumName);

    /// <summary>The name for a value, or the number itself if the enum or value is unknown.</summary>
    public string Name(string enumName, int value) =>
        byType.TryGetValue(enumName, out var values) && values.TryGetValue(value, out var name)
            ? name
            : value.ToString();
}
