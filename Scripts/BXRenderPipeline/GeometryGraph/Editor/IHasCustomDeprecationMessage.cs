using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BXGeometryGraph
{
    interface IHasCustomDeprecationMessage
    {
        public void GetCustomDeprecationMessage(out string deprecationString, out string buttonText, out string labelText, out MessageType messageType);
        public string GetCustomDeprecationLabel();
    }
}
