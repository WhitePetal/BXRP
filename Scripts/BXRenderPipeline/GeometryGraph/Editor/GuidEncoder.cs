using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXGeometryGraph
{
    public static class GuidEncoder
    {
        public static string Encode(Guid guid)
        {
            string enc = Convert.ToBase64String(guid.ToByteArray());
            return String.Format("{0:X}", enc.GetHashCode());
        }
    }
}
