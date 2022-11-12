using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;

namespace FriUMLToJava
{
    internal class Class
    {
        internal class NameValue {
            public string Name { get; internal set; }
            public string Type { get; internal set; }
        }

        internal class Attribute: NameValue
        {
            public bool IsPublic { get; internal set; } = false;
        }

        internal class Operation
        {
            public string Name { get; internal set; }
            public string RType { get; internal set; }
            public List<NameValue> Parameters { get; internal set; } = new List<NameValue>();
            public bool IsPublic { get; internal set; } = true;
            public bool IsStatic { get; internal set; }
        }


        public string Name { get; set; }
        public List<Attribute> Attributes { get; set; } = new List<Attribute>();
        public List<Operation> Operations { get; set; } = new List<Operation>();

        public Class(string name)
        {
            Name = name;
        }

    }
}
