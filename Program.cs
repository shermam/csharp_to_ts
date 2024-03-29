﻿// dotnet publish -r win10-x64

using System;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace csharp_to_ts
{
    class Config
    {
        public IEnumerable<MappingClass> Mapping { get; set; }

        public class MappingClass
        {
            public string From { get; set; }
            public string To { get; set; }

        }
    }


    class Program
    {
        static Dictionary<string, string> types = new Dictionary<string, string>(){
          {"int", "number"},
          {"long", "number"},
          {"double", "number"},
          {"IGrouping", "Array"},
          {"bool", "boolean"},
          {"TimeSpan", "string"},
          {"Guid", "string"},
          {"ICollection", "Array"},
          {"IList", "Array"},
          {"List", "Array"},
          {"IEnumerable", "Array"},
          {"DateTimeOffset", "Date | string"},
          {"DateTime", "Date | string"},
        };

        static void Main(string[] args)
        {

            var config = File.ReadAllText("./csharp-to-ts.config.json");
            var configObj = JsonConvert.DeserializeObject<Config>(config);
            foreach (var mapping in configObj.Mapping)
            {
                GenerateModels(mapping.From, mapping.To);
            }
        }

        static void GenerateModels(string from, string to)
        {
            string originFolderPath = from;
            string destinationFolderPath = to;

            string[] fileNames = Directory.GetFiles(originFolderPath);
            var walkers = new List<PropertyCollector>();


            foreach (var filePath in fileNames)
            {
                string file = File.ReadAllText(filePath);
                SyntaxTree tree = CSharpSyntaxTree.ParseText(file);

                var root = (CompilationUnitSyntax)tree.GetRoot();
                var collector = new PropertyCollector();
                collector.Visit(root);

                if (collector.Classes.Count < 1) continue;

                if (collector.Classes.Count > 1)
                    throw new Exception($@"File must have only one class declaration. 
  FileName: {filePath}
  Classes: {string.Join(", ", collector.Classes.Select(c => c.Identifier.ValueText))}");

                walkers.Add(collector);
            }

            foreach (var collector in walkers)
            {
                ClassDeclarationSyntax classDeclaration = collector.Classes.First();

                var imports = collector.Properties
                  .SelectMany(GetPropertieTypes)
                  .Where(t => walkers.Any(w => w.Classes.First().Identifier.ValueText == t))
                  .Where(t => t != classDeclaration.Identifier.ValueText);

                string fileContent = string.Join("\r\n", new List<string>{
                    $@"{string.Join("\r\n", imports.Select(i => $"import {{ {i} }} from \"./{CamelCaseToDash(i)}.model\""))}" + "\r\n",
                    $@"export class {classDeclaration.Identifier.ValueText} {{",
                    $@"{String.Join("\r\n", collector.Properties.Select(GetPropertyString))}",
                    "}"
                });

                File.WriteAllText($@"{destinationFolderPath}\{CamelCaseToDash(classDeclaration.Identifier.ValueText)}.model.ts", fileContent.Trim());
            }
        }

        static string[] GetPropertieTypes(PropertyDeclarationSyntax prop)
        {
            if (prop.Type.IsKind(SyntaxKind.GenericName))
            {
                var genType = prop.Type as GenericNameSyntax;
                return genType.TypeArgumentList.Arguments
                    .Select(a => types.GetValueOrDefault(a.ToString(), a.ToString())).ToArray();
            }
            else if (prop.Type.IsKind(SyntaxKind.ArrayType))
            {
                var genType = prop.Type as ArrayTypeSyntax;
                var elementType = genType.ElementType.ToString().Replace("?", "");
                return new string[] { types.GetValueOrDefault(elementType, elementType) };
            }
            else
            {
                return new string[] { prop.Type.ToString() };
            }
        }

        static string GetPropertyString(PropertyDeclarationSyntax prop)
        {

            string propName = prop.Identifier.ValueText;
            string nullable = prop.Type.IsKind(SyntaxKind.NullableType) ? "?" : "";
            string typeName = prop.Type.ToString().Replace("?", "");
            string type = types.GetValueOrDefault(typeName, typeName);

            if (prop.Type.IsKind(SyntaxKind.ArrayType))
            {
                var genType = prop.Type as ArrayTypeSyntax;
                var elementType = genType.ElementType.ToString().Replace("?", "");
                type = $"Array<{types.GetValueOrDefault(elementType, elementType)}>";
            }

            if (prop.Type.IsKind(SyntaxKind.GenericName))
            {
                var genType = prop.Type as GenericNameSyntax;
                string genericType = types.GetValueOrDefault(genType.Identifier.ValueText, genType.Identifier.ValueText);
                string argTypes = string.Join(
                  ", ",
                  genType.TypeArgumentList.Arguments
                    .Select(a => types.GetValueOrDefault(a.ToString(), a.ToString()))
                );
                type = $"{genericType}<{argTypes}>";
            }

            return $"  {propName}{nullable}: {type};";
        }

        static string CamelCaseToDash(string myStr)
        {
            return Regex.Replace(myStr, @"([a-z0-9])([A-Z])", "$1-$2").ToLower();
        }
    }
}
