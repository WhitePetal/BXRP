using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BXGraphing;

namespace BXGeometryGraph
{
    public interface IGeometryProperty
    {
        public string displayName { get; set; }

        public string referenceName { get; }

        public PropertyType propertyType { get; }
        public Guid guid { get; }
        public bool generatePropertyBlock { get; set; }
        public Vector4 defaultValue { get; }
        public string overrideReferenceName { get; set; }

        public string GetPropertyBlockString();
        public string GetPropertyDeclarationString(string delimiter = ";");

        public string GetPropertyAsArgumentString();

        public PreviewProperty GetPreviewGeometryProperty();
        public INode ToConcreteNode();
        public IGeometryProperty Copy();
    }
}
