﻿// See https://aka.ms/new-console-template for more information

using System.Collections.Immutable;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

var exportedTypes = typeof(RogueGenesia.GameManager.GameManager).Assembly.GetExportedTypes();
var enums = exportedTypes.Where(i => i.IsEnum);

var target = Path.GetFullPath(args.Length > 0 ? args[0] : "enums.json");

var enumDefinitionList = new List<EnumDefinition>();
foreach (var @enum in enums)
{
    var name = @enum.Name;
    var baseType = @enum.GetEnumUnderlyingType();
    var enumNames = @enum.GetEnumNames();
    var rawValues = @enum.GetEnumValues();
    var enumNamesAndValues = enumNames.Zip(rawValues.Cast<object>().Select(Convert.ToUInt64));
    var isFlags = @enum.GetCustomAttribute<FlagsAttribute>() is not null;
    var definition = new EnumDefinition(name, baseType.Name, isFlags,
        enumNamesAndValues.Select(keyValuePair => new EnumMemberDefinition(keyValuePair.First, keyValuePair.Second))
            .ToImmutableArray());
    enumDefinitionList.Add(definition);
}

var enumDefinitionsObject = new EnumDefinitions(enumDefinitionList.ToImmutableArray());
var bytes = JsonSerializer.SerializeToUtf8Bytes(enumDefinitionsObject, JsonContext.Default.EnumDefinitions);
Console.WriteLine("Writing {0} enum definitions, with {1} members total, to {2}", enumDefinitionsObject.Enums.Length, enumDefinitionsObject.Enums.Sum(x => x.Members.Length), target);
await File.WriteAllBytesAsync(target, bytes, CancellationToken.None);


public record EnumDefinition(string Name, string BaseType, bool Flags, ImmutableArray<EnumMemberDefinition> Members);

public record EnumMemberDefinition(string Name, ulong Value);

public record EnumDefinitions(ImmutableArray<EnumDefinition> Enums);

[JsonSourceGenerationOptions(
    GenerationMode = JsonSourceGenerationMode.Serialization,
    DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    IgnoreReadOnlyProperties = false,
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase
)]
[JsonSerializable(typeof(EnumDefinition))]
[JsonSerializable(typeof(EnumMemberDefinition))]
[JsonSerializable(typeof(EnumDefinitions))]
internal partial class JsonContext : JsonSerializerContext
{
}