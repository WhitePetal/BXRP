using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace BXGeometryGraph
{
    class FloatField : DoubleField
    {
        protected override string ValueToString(double v)
        {
            return ((float)v).ToString(CultureInfo.InvariantCulture.NumberFormat);
        }
    }
}
