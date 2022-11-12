using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using static FriUMLToJava.Class;
using Attribute = FriUMLToJava.Class.Attribute;

namespace FriUMLToJava
{
    /// <summary>
    /// Class that converts UML to java
    /// </summary>
    internal class UMLConverter
    {
        private readonly List<Class> _classes = new List<Class>();

        internal async Task ConvertFileAsync(string filePath) 
        {
            if (string.IsNullOrEmpty(filePath))
                return;

            if (filePath.EndsWith(".xml", StringComparison.CurrentCultureIgnoreCase))
            {
                await ConvertFromXmlAsync(filePath);
            }
            else if(filePath.EndsWith(".frip2", StringComparison.CurrentCultureIgnoreCase))
            {
                await ConvertFromNodeAsync(Frip2ToXmlConverter.GetXmlNodeFromZip(filePath));
            }
        }

        private async Task ConvertFromXmlAsync(string filePath)
        {
            if (!File.Exists(filePath))
                throw new IOException("file not found!");

            XmlDocument doc = new XmlDocument();
            doc.Load(filePath);
            var projectNode = doc.ChildNodes[1];
            await ConvertFromNodeAsync(projectNode);
        }

        #region Read
        private async Task ConvertFromNodeAsync(XmlNode node) {
            var childNode = node.ChildNodes[1];


            foreach (XmlNode classElementNode in childNode.ChildNodes)    
            {
                if (classElementNode.Attributes[1].Value != "class")
                    continue;
                try
                {
                    var c = await Task.Run(() => LoadClass(classElementNode));
                    if (c is null) 
                        continue;
                    _classes.Add(c);
                    await Task.Delay(100);

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }  
        }

        private Class.Attribute LoadAttribute(XmlNode itemNode) {
            
            var nameValue = new Class.Attribute();

            foreach (XmlNode attributeNode in itemNode.ChildNodes)
            {
                if (attributeNode.Attributes[0].Value == "name")
                    nameValue.Name = attributeNode.Attributes[1].Value;
                else if (attributeNode.Attributes[0].Value == "type")
                    nameValue.Type = attributeNode.Attributes[1].Value;
                else if (attributeNode.Attributes[0].Value == "visibility")
                    nameValue.IsPublic = (attributeNode.Attributes[1].Value == "+");
                else
                    throw new Exception("uknown attribute in " + attributeNode.ToString());
            }
            return nameValue;
        }

        private Class LoadClass(XmlNode classElementNode)
        {
            if(classElementNode.ChildNodes.Count == 0)
                throw new Exception("element is null!");
            string className = classElementNode.ChildNodes[0].Attributes[1].Value;
            Class c = new Class(className);

            foreach (XmlNode node in classElementNode.ChildNodes)
            {
                if (c is null)
                    continue;

                // add class' attributes
                if (node.Attributes[0].Value == "attributes")
                {
                    foreach (XmlNode itemNode in node.ChildNodes)
                    {
                        var attribute = LoadAttribute(itemNode);
                        c.Attributes.Add(attribute);
                    }
                }

                // add class' operations
                if (node.Attributes[0].Value == "operations")
                {
                    foreach (XmlNode itemNode in node.ChildNodes)
                    {
                        var operation = new Operation();

                        foreach (XmlNode attributeNode in itemNode.ChildNodes)
                        {
                            if (attributeNode.Attributes[0].Value == "name")
                                operation.Name = attributeNode.Attributes[1].Value;
                            else if (attributeNode.Attributes[0].Value == "rtype")
                                operation.RType = attributeNode.Attributes[1].Value;
                            else if (attributeNode.Attributes[0].Value == "parameters")
                            {
                                //if(itemNode.Attributes)
                                foreach (XmlNode itemNode2 in attributeNode.ChildNodes)
                                {
                                    var parameter = LoadAttribute(itemNode2);
                                    operation.Parameters.Add(parameter);
                                }
                            }
                            else if (attributeNode.Attributes[0].Value == "visibility")
                            {
                                operation.IsPublic = (attributeNode.Attributes[1].Value == "+");

                            }

                            else if (attributeNode.Attributes[0].Value == "static") 
                                operation.IsStatic = bool.Parse(attributeNode.Attributes[1].Value);
                            else
                                throw new Exception("uknown attribute in " + attributeNode.ToString());
                        }

                        c.Operations.Add(operation);
                    }

                }
            }
            return c;
        }

        #endregion
        #region Export

        internal async Task WriteToJavaAsync(string solutionName, string outputFolder, bool overWrite)
        {
            // Folder that contains converted data
            var curOutputFolder = Path.Combine(outputFolder, solutionName);

            if (Directory.Exists(curOutputFolder))
            {
                if(overWrite)
                    foreach (var file in Directory.GetFiles(outputFolder))
                    {
                        File.Delete(file);
                    }
                else
                    throw new Exception("Name conflict - choose different name!");
            }
            else
                Directory.CreateDirectory(curOutputFolder);

            foreach (Class c in _classes)
            {
                var classFileName = Path.Combine(curOutputFolder, c.Name) + ".java";
                FileStream fs = new FileStream(classFileName, FileMode.Append, FileAccess.Write, FileShare.Write);
                fs.Close();
                StreamWriter sw = new StreamWriter(classFileName, true, Encoding.ASCII);


                List<Task> writeTasks = new List<Task>();
                // Write text to file:
                
                // Declare class
                writeTasks.Add(sw.WriteAsync("public class " + c.Name + "  {\n\n"));

                // Declare all attributes 
                foreach (Class.Attribute attribute in c.Attributes)
                {
                    string accessibilityModifier = "public";
                    if (!attribute.IsPublic)
                        accessibilityModifier = "private";

                    writeTasks.Add(sw.WriteAsync("  " + accessibilityModifier + " " + attribute.Type + " " + attribute.Name + ";\n"));
                }

                // Add new line
                writeTasks.Add(sw.WriteAsync("\n"));

                // Add all methods
                foreach (Operation operation in c.Operations)
                {
                   // Accessiblity modifier
                    string accessibilityModifier = "public";
                    if (!operation.IsPublic)
                        accessibilityModifier = "private";
                    writeTasks.Add(sw.WriteAsync("  " + accessibilityModifier + " "));

                    // Method's name and type
                    if (operation.Name.ToLower() == "new")
                        writeTasks.Add(sw.WriteAsync(operation.RType));
                    else
                        writeTasks.Add(sw.WriteAsync(operation.RType + " " + operation.Name));

                    // Method's parameters
                    string parameters = "";
                    if (operation.Parameters.Count > 0)
                    {
                        operation.Parameters.ForEach(p => parameters += p.Type + " " + p.Name + ", ");
                        parameters = parameters.Remove(parameters.Length - 2, 2);
                    }
                    writeTasks.Add(sw.WriteAsync("(" + parameters + ")   {\n"));

                    // Initialize constructor
                    if (operation.Name.ToLower() == "new") {
                        await WriteContructorAsync(writeTasks, sw, c.Attributes, operation);
                    }

                    // Assign getter
                    if (operation.Name.ToLower().StartsWith("get"))
                    {
                        string returnVariable = operation.Name.Replace("get", "");
                        char firstChar = char.ToLower(returnVariable[0]);
                        returnVariable = returnVariable.Remove(0, 1);
                        returnVariable = firstChar + returnVariable;
                        writeTasks.Add(sw.WriteAsync("      return this." + returnVariable + ";"));
                    }
                    // Assign setter
                    if (operation.Name.ToLower().StartsWith("set"))
                    {
                        writeTasks.Add(sw.WriteAsync("      this." + operation.Parameters[0].Name + " = " + operation.Parameters[0].Name + ";"));
                    }


                    // End method
                    writeTasks.Add(sw.WriteAsync("  }\n\n"));

                }

                //end class
                writeTasks.Add(sw.WriteAsync("}\n"));

                await Task.WhenAll(writeTasks);
                sw.Close();
            }
        }

        private async Task WriteContructorAsync(List<Task> writeTasks, StreamWriter sw, List<Attribute> classAttributes, Operation operation)
        {
            List<Attribute> notFoundAttributes = new List<Class.Attribute>();
            notFoundAttributes.AddRange(classAttributes);

            // if the parameter of method WAS found in class, assign it to a attribute
            foreach (var parameter in operation.Parameters)
            {
                if (classAttributes.Find(x => x.Name == parameter.Name && x.Type == parameter.Type) != null)
                {
                    writeTasks.Add(sw.WriteAsync("      this." + parameter.Name + " = " + parameter.Name + ";\n"));
                    var aa = notFoundAttributes.Find(x => x.Name == parameter.Name && x.Type == parameter.Type);
                    if (aa is null)
                        continue;
                    notFoundAttributes.Remove(aa);
                }
            }
            // if the parameter WAS NOT found in class, initialize its value
            foreach (var attribute in notFoundAttributes)
            {
                string[] numberTypes = { "int", "Integer", "double", "Double", "float", "Float" };

                if (numberTypes.Contains(attribute.Type))
                    writeTasks.Add(sw.WriteAsync("      this." + attribute.Name + " = 0;\n"));
                else if (attribute.Type.ToLower() == "string")
                    writeTasks.Add(sw.WriteAsync("      this." + attribute.Name + " = \"\";\n"));
                else if (attribute.Type.ToLower() == "boolean")
                    writeTasks.Add(sw.WriteAsync("      this." + attribute.Name + " = false;\n"));
                else
                    writeTasks.Add(sw.WriteAsync("      this." + attribute.Name + " = new " + attribute.Type + "();\n"));

            }
            await Task.WhenAll(writeTasks);
        }


        #endregion
    }

}
