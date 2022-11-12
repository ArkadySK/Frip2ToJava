using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Windows.Data;
using System.Xml;

namespace FriUMLToJava
{
    internal static class Frip2ToXmlConverter
    {
        internal static XmlNode GetXmlNodeFromZip(string zipFile)
        {
            using (ZipArchive zip = ZipFile.Open(zipFile, ZipArchiveMode.Read))
                foreach (ZipArchiveEntry entry in zip.Entries)
                    if (entry.FullName.Contains("project"))
                    {
                        var xmlFileName = zipFile.Replace(".frip2", ".xml");
                        entry.ExtractToFile(xmlFileName);

                        XmlDocument doc = new XmlDocument();
                        doc.Load(xmlFileName);
                        var projectNode = doc.ChildNodes[1];
                        File.Delete(xmlFileName);
                        return projectNode;
                    }

            return null;
        }
    }
}
