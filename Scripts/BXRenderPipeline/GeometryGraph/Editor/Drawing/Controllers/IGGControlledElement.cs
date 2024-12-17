using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXGeometryGraph
{
    interface IGGControlledElement
    {
        GGController controller
        {
            get;
        }

        void OnControllerChanged(ref GGControllerChangedEvent e);

        void OnControllerEvent(GGControllerEvent e);
    }

    interface IGGControlledElement<T> : IGGControlledElement where T : GGController
    {
        // This provides a way to access the controller of a ControlledElement at both the base class SGController level and child class level
        new T controller { get; }

    }
}
