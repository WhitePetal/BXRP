using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXGeometryGraph
{
    public interface IControl
    {
        GeometryGraphRequirements GetRequirements();
    }

    public class GeometryControl : IControl
    {
        public GeometryGraphRequirements GetRequirements()
        {
            return new GeometryGraphRequirements()
            {
                requiresGeometry = true
            };
        }
    }

    public class FloatControl : IControl
    {
        public float value { get; private set; }

        public FloatControl(float value)
        {
            this.value = value;
        }

        public GeometryGraphRequirements GetRequirements()
        {
            return GeometryGraphRequirements.none;
        }
    }

    public class ColorControl : IControl
    {
        public Color value { get; private set; }
        public bool hdr { get; private set; }

        public ColorControl(Color value, bool hdr)
        {
            this.value = value;
            this.hdr = hdr;
        }

        public GeometryGraphRequirements GetRequirements()
        {
            return GeometryGraphRequirements.none;
        }
    }
}
