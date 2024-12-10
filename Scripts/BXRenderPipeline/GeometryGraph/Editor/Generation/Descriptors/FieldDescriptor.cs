using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXGeometryGraph
{
    [GenerationAPI]
    public class FieldDescriptor
    {
        // Default
        public string tag { get; }
        public string name { get; }
        public string define { get; }
        public string interpolation { get; }

        // StructField
        public string type { get; }
        public int vectorCount { get; }
        public string semantic { get; }
        public string preprocessor { get; }
        public StructFieldOptions subscriptOptions { get; }

        public FieldDescriptor(string tag, string name, string define)
        {
            this.tag = tag;
            this.name = name;
            this.define = define;
        }

        public FieldDescriptor(string tag, string name, string define, GeometryValueType type,
                            string semantic = "", string preprocessor = "", StructFieldOptions subscriptOptions = StructFieldOptions.Static, string interpolation = "")
        {
            this.tag = tag;
            this.name = name;
            this.define = define;
            this.type = type.ToGeometryString();
            this.vectorCount = type.GetVectorCount();
            this.semantic = semantic;
            this.preprocessor = preprocessor;
            this.interpolation = interpolation;
            this.subscriptOptions = subscriptOptions;
        }

        public FieldDescriptor(string tag, string name, string define, string type,
                       string semantic = "", string preprocessor = "", StructFieldOptions subscriptOptions = StructFieldOptions.Static, string interpolation = "")
        {
            this.tag = tag;
            this.name = name;
            this.define = define;
            this.type = type;
            this.vectorCount = 0;
            this.semantic = semantic;
            this.preprocessor = preprocessor;
            this.interpolation = interpolation;
            this.subscriptOptions = subscriptOptions;
        }
    }
}
