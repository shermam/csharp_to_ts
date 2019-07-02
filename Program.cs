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
      {"bool", "boolean"},
      {"TimeSpan", "string"},
      {"ICollection", "Array"},
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
                  .Where(t => walkers.Any(w => w.Classes.First().Identifier.ValueText == t));

                string fileContent = $@"{string.Join("\n", imports.Select(i => $"import {{ {i} }} from \"./{CamelCaseToDash(i)}.model\""))}

export class {classDeclaration.Identifier.ValueText} {{
{String.Join("\n", collector.Properties.Select(GetPropertyString))}
}}";
                File.WriteAllText($@"{destinationFolderPath}\{CamelCaseToDash(classDeclaration.Identifier.ValueText)}.model.ts", fileContent.Trim());
            }
        }

        static string[] GetPropertieTypes(PropertyDeclarationSyntax prop)
        {
            if (prop.Type.Kind() == SyntaxKind.GenericName)
            {
                var genType = prop.Type as GenericNameSyntax;
                return genType.TypeArgumentList.Arguments
                    .Select(a => a.ToString()).ToArray();
            }
            else
            {
                return new string[] { prop.Type.ToString() };
            }
        }

        static string GetPropertyString(PropertyDeclarationSyntax prop)
        {

            string propName = prop.Identifier.ValueText;
            string nullable = prop.Type.Kind() == SyntaxKind.NullableType ? "?" : "";
            string typeName = prop.Type.ToString().Replace("?", "");
            string type = types.GetValueOrDefault(typeName, typeName);

            if (prop.Type.Kind() == SyntaxKind.GenericName)
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
