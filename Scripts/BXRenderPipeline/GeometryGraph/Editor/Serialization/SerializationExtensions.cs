using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXGeometryGraph
{
    static class SerializationExtensions
    {
        public static RefValueEnumerable<T> SelectValue<T>(this List<JsonRef<T>> list) where T : JsonObject =>
            new RefValueEnumerable<T>(list);

        public static DataValueEnumerable<T> SelectValue<T>(this List<JsonData<T>> list) where T : JsonObject =>
            new DataValueEnumerable<T>(list);
    }
}
