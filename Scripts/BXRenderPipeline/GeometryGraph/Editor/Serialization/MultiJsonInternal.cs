using System;
using System.Collections;
using System.Collections.Generic;
using BXGraphing;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace BXGeometryGraph
{
    static class MultiJsonInternal
    {
        public class UnknownJsonObject : JsonObject
        {
            public string typeInfo;
            public string jsonData;
            public JsonData<JsonObject> castedObject;

            public UnknownJsonObject(string typeInfo)
            {
                this.typeInfo = typeInfo;
            }

            public override void Deserailize(string typeInfo, string jsonData)
            {
                this.jsonData = jsonData;
            }

            public override string Serialize()
            {
                return jsonData;
            }

            public override void OnAfterDeserialize()
            {
                if (castedObject.value != null)
                {
                    Enqueue(castedObject, jsonData.Trim());
                }
            }

            public override void OnAfterMultiDeserialize(string json)
            {
                if (castedObject.value == null)
                {
                    //Never got casted so nothing ever reffed this object
                    //likely that some other unknown json object had a ref
                    //to this thing. Need to include it in the serialization
                    //step of the object still.
                    if (jsonBlobs.TryGetValue(currentRoot.objectId, out var blobs))
                    {
                        blobs[objectId] = jsonData.Trim();
                    }
                    else
                    {
                        var lookup = new Dictionary<string, string>();
                        lookup[objectId] = jsonData.Trim();
                        jsonBlobs.Add(currentRoot.objectId, lookup);
                    }
                }
            }

            public override T CastTo<T>()
            {
                if (castedObject.value != null)
                    return castedObject.value.CastTo<T>();

                Type t = typeof(T);
                if (t == typeof(AbstractGeometryNode) || t.IsSubclassOf(typeof(AbstractGeometryNode)))
                {
                    UnknownNodeType unt = new UnknownNodeType(jsonData);
                    valueMap[objectId] = unt;
                    s_ObjectIdField.SetValue(unt, objectId);
                    castedObject = unt;
                    return unt.CastTo<T>();
                }
                //else if (t == typeof(Target) || t.IsSubclassOf(typeof(Target)))
                //{
                //    UnknownTargetType utt = new UnknownTargetType(typeInfo, jsonData);
                //    valueMap[objectId] = utt;
                //    s_ObjectIdField.SetValue(utt, objectId);
                //    castedObject = utt;
                //    return utt.CastTo<T>();
                //}
                //else if (t == typeof(SubTarget) || t.IsSubclassOf(typeof(SubTarget)))
                //{
                //    UnknownSubTargetType ustt = new UnknownSubTargetType(typeInfo, jsonData);
                //    valueMap[objectId] = ustt;
                //    s_ObjectIdField.SetValue(ustt, objectId);
                //    castedObject = ustt;
                //    return ustt.CastTo<T>();
                //}
                //else if (t == typeof(ShaderInput) || t.IsSubclassOf(typeof(ShaderInput)))
                //{
                //    UnknownShaderPropertyType usp = new UnknownShaderPropertyType(typeInfo, jsonData);
                //    valueMap[objectId] = usp;
                //    s_ObjectIdField.SetValue(usp, objectId);
                //    castedObject = usp;
                //    return usp.CastTo<T>();
                //}
                else if (t == typeof(GeometrySlot) || t.IsSubclassOf(typeof(GeometrySlot)))
                {
                    UnknownGeometrySlotType umst = new UnknownGeometrySlotType(typeInfo, jsonData);
                    valueMap[objectId] = umst;
                    s_ObjectIdField.SetValue(umst, objectId);
                    castedObject = umst;
                    return umst.CastTo<T>();
                }
                //else if (t == typeof(AbstractGeometryGraphDataExtension) || t.IsSubclassOf(typeof(AbstractGeometryGraphDataExtension)))
                //{
                //    UnknownGraphDataExtension usge = new UnknownGraphDataExtension(typeInfo, jsonData);
                //    valueMap[objectId] = usge;
                //    s_ObjectIdField.SetValue(usge, objectId);
                //    castedObject = usge;
                //    return usge.CastTo<T>();
                //}
                else
                {
                    Debug.LogError($"Unable to evaluate type {typeInfo} : {jsonData}");
                }
                return null;
            }
        }

        //public class UnknownTargetType : Target
        //{

        //}

        //private class UnknownSubTargetType : SubTarget
        //{

        //}

        //interface class UnknownGeometryPropertyType : AbstractGeometryProperty
        //{

        //}

        internal class UnknownGeometrySlotType : GeometrySlot
        {
            // used to deserialize some data out of an unknown MaterialSlot
            class SerializerHelper
            {
                [SerializeField]
                public string m_DisplayName = null;

                [SerializeField]
                public SlotType m_SlotType = SlotType.Input;

                [SerializeField]
                public bool m_Hidden = false;

                [SerializeField]
                public string m_ShaderOutputName = null;

                //[SerializeField]
                //public ShaderStageCapability m_StageCapability = ShaderStageCapability.All;
            }

            public string jsonData;

            public UnknownGeometrySlotType(string displayName, string jsonData) : base()
            {
                // copy some minimal information to try to keep the UI as similar as possible
                var helper = new SerializerHelper();
                JsonUtility.FromJsonOverwrite(jsonData, helper);
                this.displayName = helper.m_DisplayName;
                this.hidden = helper.m_Hidden;
                //this.stageCapability = helper.m_StageCapability;
                this.SetInternalData(helper.m_SlotType, helper.m_ShaderOutputName);

                // save the original json for saving
                this.jsonData = jsonData;
            }

            public override void Deserailize(string typeInfo, string jsonData)
            {
                this.jsonData = jsonData;
                base.Deserailize(typeInfo, jsonData);
            }

            public override string Serialize()
            {
                return jsonData.Trim();
            }

            public override bool isDefaultValue => true;

            public override SlotValueType valueType => SlotValueType.Vector1;

            public override ConcreteSlotValueType concreteValueType => ConcreteSlotValueType.Vector1;

            public override void AddDefaultProperty(PropertyCollector properties, GenerationMode generationMode) { }

            public override void CopyValuesFrom(GeometrySlot foundSlot)
            {
                // we CANNOT copy data from another slot, as the GUID in the serialized jsonData would not match our real GUID
                throw new NotSupportedException();
            }
        }

        internal class UnknownGraphDataExtension : AbstractGeometryGraphDataExtension
        {
            public string name;
            public string jsonData;
            internal override string displayName => name;

            internal UnknownGraphDataExtension() : base() { }

            internal UnknownGraphDataExtension(string displayName, string jsonData)
            {
                name = displayName;
                this.jsonData = jsonData;
            }

            public override void Deserailize(string typeInfo, string jsonData)
            {
                this.jsonData = jsonData;
                base.Deserailize(typeInfo, jsonData);
            }

            public override string Serialize()
            {
                return jsonData.Trim();
            }

            internal override void OnPropertiesGUI(VisualElement context, Action onChange, Action<string> registerUndo, GraphData owner)
            {
                var helpBox = new HelpBoxRow(MessageType.Info);
                helpBox.Add(new Label("Cannot find the code for this data extension, a package may be missing."));
                context.hierarchy.Add(helpBox);
            }
        }

        static readonly Dictionary<string, Type> k_TypeMap = CreateTypeMap();

        internal static bool isDeserializing;

        internal static readonly Dictionary<string, JsonObject> valueMap = new Dictionary<string, JsonObject>();

        static List<MultiJsonEntry> s_Entries;

        internal static bool isSerializing;

        internal static readonly List<JsonObject> serializationQueue = new List<JsonObject>();

        internal static readonly HashSet<string> serializedSet = new HashSet<string>();

        static JsonObject currentRoot = null;

        static Dictionary<string, Dictionary<string, string>> jsonBlobs = new Dictionary<string, Dictionary<string, string>>();

        static Dictionary<string, Type> CreateTypeMap()
        {
            var map = new Dictionary<string, Type>();
            foreach (var type in TypeCache.GetTypesDerivedFrom<JsonObject>())
            {
                if (type.FullName != null)
                {
                    map[type.FullName] = type;
                }
            }

            foreach (var type in TypeCache.GetTypesWithAttribute(typeof(FormerNameAttribute)))
            {
                if (type.IsAbstract || !typeof(JsonObject).IsAssignableFrom(type))
                {
                    continue;
                }

                foreach (var attribute in type.GetCustomAttributes(typeof(FormerNameAttribute), false))
                {
                    var legacyAttribute = (FormerNameAttribute)attribute;
                    map[legacyAttribute.fullName] = type;
                }
            }

            return map;
        }

        public static Type ParseType(string typeString)
        {
            k_TypeMap.TryGetValue(typeString, out var type);
            return type;
        }

        //public static List<MultiJsonEntry> Parse(string str)
        //{
        //    var result = new List<MultiJsonEntry>();
        //    const string separatorStr = "\n\n";
        //    var startIndex = 0;
        //    var raw = new FakeJsonObject();

        //    while (startIndex < str.Length)
        //    {
        //        var jsonBegin = str.IndexOf("{", startIndex, StringComparison.Ordinal);
        //        if (jsonBegin == -1)
        //        {
        //            break;
        //        }

        //        var jsonEnd = str.IndexOf(separatorStr, jsonBegin, StringComparison.Ordinal);
        //        if (jsonEnd == -1)
        //        {
        //            jsonEnd = str.IndexOf("\n\r\n", jsonBegin, StringComparison.Ordinal);
        //            if (jsonEnd == -1)
        //            {
        //                jsonEnd = str.LastIndexOf("}", StringComparison.Ordinal) + 1;
        //            }
        //        }

        //        var json = str.Substring(jsonBegin, jsonEnd - jsonBegin);

        //        JsonUtility.FromJsonOverwrite(json, raw);
        //        if (startIndex != 0 && string.IsNullOrWhiteSpace(raw.type))
        //        {
        //            throw new InvalidOperationException($"Type is null or whitespace in JSON:\n{json}");
        //        }

        //        result.Add(new MultiJsonEntry(raw.type, raw.id, json));
        //        raw.Reset();

        //        startIndex = jsonEnd + separatorStr.Length;
        //    }

        //    return result;
        //}

        public static void Enqueue(JsonObject jsonObject, string json)
        {
            if (s_Entries == null)
            {
                throw new InvalidOperationException("Can only Enqueue during JsonObject.OnAfterDeserialize.");
            }

            valueMap.Add(jsonObject.objectId, jsonObject);
            s_Entries.Add(new MultiJsonEntry(jsonObject.GetType().FullName, jsonObject.objectId, json));
        }
    }
}
