using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;

namespace BXGeometryGraph
{
    [GGPropertyDrawer(typeof(GeometryInput))]
    class GeometryInputPropertyDrawer : IPropertyDrawer
    {
        internal delegate void ChangeExposedFieldCallback(bool newValue);
        internal delegate void ChangeValueCallback(object newValue);
        internal delegate void PreChangeValueCallback(string actionName);
        internal delegate void PostChangeValueCallback(bool bTriggerPropertyUpdate = false, ModificationScope modificationScope = ModificationScope.Node);

        // Keyword
        ReorderableList m_KeywordReorderableList;
        int m_KeywordSelectedIndex;

        // Dropdown
        ReorderableList m_DropdownReorderableList;
        GeometryDropdown m_Dropdown;
        int m_DropdownId;
        int m_DropdownSelectedIndex;

        //    //Virtual Texture
        //    //ReorderableList m_VTReorderableList;
        //    //int m_VTSelectedIndex;
        //    //private static GUIStyle greyLabel;
        //    //TextField m_VTLayer_Name;
        //    //IdentifierField m_VTLayer_RefName;
        //    //ObjectField m_VTLayer_Texture;
        //    //EnumField m_VTLayer_TextureType;

        // Display Name
        TextField m_DisplayNameField;

        TextField m_CustomSlotLabelField;

        // Reference Name
        TextPropertyDrawer m_ReferenceNameDrawer;
        TextField m_ReferenceNameField;

        GeometryInput geometryInput;

        Toggle exposedToggle;
        VisualElement keywordScopeField;
        // Should be provided by the Inspectable
        GeometryInputViewModel m_ViewModel;
        GeometryInputViewModel ViewModel => m_ViewModel;

        const string m_DisplayNameDisallowedPattern = "[^\\w_ ]";
        const string m_ReferenceNameDisallowedPattern = @"(?:[^A-Za-z_0-9_])";

        public GeometryInputPropertyDrawer()
        {
            //greyLabel = new GUIStyle(EditorStyles.label);
            //greyLabel.normal = new GUIStyleState { textColor = Color.grey };
            //greyLabel.focused = new GUIStyleState { textColor = Color.grey };
            //greyLabel.hover = new GUIStyleState { textColor = Color.grey };
        }

        GraphData graphData;
        bool isSubGraph { get; set; }
        ChangeExposedFieldCallback _exposedFieldChangedCallback;
        Action _precisionChangedCallback;
        Action _keywordChangedCallback;
        Action _dropdownChangedCallback;
        Action<string> _displayNameChangedCallback;
        Action<string> _referenceNameChangedCallback;
        ChangeValueCallback _changeValueCallback;
        PreChangeValueCallback _preChangeValueCallback;
        PostChangeValueCallback _postChangeValueCallback;

        internal void GetViewModel(GeometryInputViewModel shaderInputViewModel, GraphData inGraphData, PostChangeValueCallback postChangeValueCallback)
        {
            m_ViewModel = shaderInputViewModel;
            this.isSubGraph = m_ViewModel.isSubGraph;
            this.graphData = inGraphData;
            this._keywordChangedCallback = () => graphData.OnKeywordChanged();
            this._dropdownChangedCallback = () => graphData.OnDropdownChanged();
            this._precisionChangedCallback = () => graphData.ValidateGraph();

            this._exposedFieldChangedCallback = newValue =>
            {
                var changeExposedFlagAction = new ChangeExposedFlagAction(geometryInput, newValue);
                ViewModel.requestModelChangeAction(changeExposedFlagAction);
            };

            this._displayNameChangedCallback = newValue =>
            {
                var changeDisplayNameAction = new ChangeDisplayNameAction();
                changeDisplayNameAction.geometryInputReference = geometryInput;
                changeDisplayNameAction.newDisplayNameValue = newValue;
                ViewModel.requestModelChangeAction(changeDisplayNameAction);
            };

            this._changeValueCallback = newValue =>
            {
                var changeDisplayNameAction = new ChangePropertyValueAction();
                changeDisplayNameAction.geometryInputReference = geometryInput;
                changeDisplayNameAction.newGeometryInputValue = newValue;
                ViewModel.requestModelChangeAction(changeDisplayNameAction);
            };

            this._referenceNameChangedCallback = newValue =>
            {
                var changeReferenceNameAction = new ChangeReferenceNameAction();
                changeReferenceNameAction.geometryInputReference = geometryInput;
                changeReferenceNameAction.newReferenceNameValue = newValue;
                ViewModel.requestModelChangeAction(changeReferenceNameAction);
            };

            this._preChangeValueCallback = (actionName) => this.graphData.owner.RegisterCompleteObjectUndo(actionName);

            if (geometryInput is AbstractGeometryProperty abstractShaderProperty)
            {
                var changePropertyValueAction = new ChangePropertyValueAction();
                changePropertyValueAction.geometryInputReference = abstractShaderProperty;
                this._changeValueCallback = newValue =>
                {
                    changePropertyValueAction.newGeometryInputValue = newValue;
                    ViewModel.requestModelChangeAction(changePropertyValueAction);
                };
            }

            this._postChangeValueCallback = postChangeValueCallback;
        }

        public Action inspectorUpdateDelegate { get; set; }

        public VisualElement DrawProperty(
            PropertyInfo propertyInfo,
            object actualObject,
            InspectableAttribute attribute)
        {
            var propertySheet = new PropertySheet();
            geometryInput = actualObject as GeometryInput;
            BuildPropertyNameLabel(propertySheet);
            BuildDisplayNameField(propertySheet);
            BuildReferenceNameField(propertySheet);
            BuildPropertyFields(propertySheet);
            BuildKeywordFields(propertySheet, geometryInput);
            BuildDropdownFields(propertySheet, geometryInput);
            UpdateEnableState();
            return propertySheet;
        }

        void IPropertyDrawer.DisposePropertyDrawer() { }

        void BuildPropertyNameLabel(PropertySheet propertySheet)
        {
            string prefix;
            if (geometryInput is ShaderKeyword)
                prefix = "Keyword";
            else if (geometryInput is GeometryDropdown)
                prefix = "Dropdown";
            else
                prefix = "Property";

            propertySheet.headerContainer.Add(PropertyDrawerUtils.CreateLabel($"{prefix}: {geometryInput.displayName}", 0, FontStyle.Bold));
        }

        void BuildExposedField(PropertySheet propertySheet)
        {
            if (!isSubGraph)
            {
                var toggleDataPropertyDrawer = new ToggleDataPropertyDrawer();
                propertySheet.Add(toggleDataPropertyDrawer.CreateGUI(
                    evt =>
                    {
                        this._preChangeValueCallback("Change Exposed Toggle");
                        this._exposedFieldChangedCallback(evt.isOn);
                        this._postChangeValueCallback(false, ModificationScope.Graph);
                    },
                    new ToggleData(geometryInput.isExposed),
                    geometryInput is ShaderKeyword ? "Generate Geometry Property" : "Show In Inspector",
                    out var exposedToggleVisualElement,
                    tooltip: geometryInput is ShaderKeyword ? "Generate a geometry property declaration to show this field in the geometry inspector." : "Hide or Show this property in the geometry inspector."));
                exposedToggle = exposedToggleVisualElement as Toggle;
            }
        }

        void BuildCustomBindingField(PropertySheet propertySheet, GeometryInput property)
        {
            if (isSubGraph && property.isCustomSlotAllowed)
            {
                var toggleDataPropertyDrawer = new ToggleDataPropertyDrawer();
                propertySheet.Add(toggleDataPropertyDrawer.CreateGUI(
                    newValue =>
                    {
                        if (property.useCustomSlotLabel == newValue.isOn)
                            return;
                        this._preChangeValueCallback("Change Custom Binding");
                        property.useCustomSlotLabel = newValue.isOn;
                        graphData.ValidateGraph();
                        this._postChangeValueCallback(true, ModificationScope.Topological);
                    },
                    new ToggleData(property.isConnectionTestable),
                    "Use Custom Binding",
                    out var exposedToggleVisualElement));
                exposedToggleVisualElement.SetEnabled(true);

                if (property.useCustomSlotLabel)
                {
                    var textPropertyDrawer = new TextPropertyDrawer();
                    var guiElement = textPropertyDrawer.CreateGUI(
                        null,
                        (string)geometryInput.customSlotLabel,
                        "Label",
                        1);

                    m_CustomSlotLabelField = textPropertyDrawer.textField;
                    m_CustomSlotLabelField.RegisterValueChangedCallback(
                        evt =>
                        {
                            if (evt.newValue != geometryInput.customSlotLabel)
                            {
                                this._preChangeValueCallback("Change Custom Binding Label");
                                geometryInput.customSlotLabel = evt.newValue;
                                m_CustomSlotLabelField.AddToClassList("modified");
                                this._postChangeValueCallback(true, ModificationScope.Topological);
                            }
                        });

                    if (!string.IsNullOrEmpty(geometryInput.customSlotLabel))
                        m_CustomSlotLabelField.AddToClassList("modified");
                    m_CustomSlotLabelField.styleSheets.Add(Resources.Load<StyleSheet>("Styles/CustomSlotLabelField"));

                    propertySheet.Add(guiElement);
                }
            }
        }

        void UpdateEnableState()
        {
            // some changes may change the exposed state
            exposedToggle?.SetValueWithoutNotify(geometryInput.isExposed);
            if (geometryInput is ShaderKeyword keyword)
            {
                // TODO
                //keywordScopeField?.SetEnabled(!keyword.isBuiltIn && (keyword.keywordDefinition != KeywordDefinition.Predefined));
                //exposedToggle?.SetEnabled((keyword.keywordDefinition != KeywordDefinition.Predefined));
                //this._exposedFieldChangedCallback(keyword.generatePropertyBlock); // change exposed icon appropriately
            }
            else if (geometryInput is AbstractGeometryProperty property)
            {
                // this is the best field to represent whether the toggle should be presented or not.
                exposedToggle?.SetEnabled(property.shouldForceExposed);
            }
            else
            {
                exposedToggle?.SetEnabled(geometryInput.isExposable && !geometryInput.isAlwaysExposed);
            }
        }

        void BuildDisplayNameField(PropertySheet propertySheet)
        {
            var textPropertyDrawer = new TextPropertyDrawer();
            propertySheet.Add(textPropertyDrawer.CreateGUI(
                null,
                (string)geometryInput.displayName,
                "Name",
                tooltip: "Display name used in the material inspector."));

            m_DisplayNameField = textPropertyDrawer.textField;
            m_DisplayNameField.RegisterValueChangedCallback(
                evt =>
                {
                    if (evt.newValue != geometryInput.displayName)
                    {
                        this._preChangeValueCallback("Change Display Name");
                        geometryInput.SetDisplayNameAndSanitizeForGraph(graphData, evt.newValue);
                        this._displayNameChangedCallback(evt.newValue);

                        if (string.IsNullOrEmpty(geometryInput.displayName))
                            m_DisplayNameField.RemoveFromClassList("modified");
                        else
                            m_DisplayNameField.AddToClassList("modified");

                        this._postChangeValueCallback(true, ModificationScope.Layout);
                    }
                });

            if (!string.IsNullOrEmpty(geometryInput.displayName))
                m_DisplayNameField.AddToClassList("modified");
            m_DisplayNameField.SetEnabled(geometryInput.isRenamable);
            m_DisplayNameField.styleSheets.Add(Resources.Load<StyleSheet>("Styles/PropertyNameReferenceField"));
        }

        void BuildReferenceNameField(PropertySheet propertySheet)
        {
            if (!isSubGraph || geometryInput is ShaderKeyword)
            {
                m_ReferenceNameDrawer = new TextPropertyDrawer();
                propertySheet.Add(m_ReferenceNameDrawer.CreateGUI(
                    null,
                    (string)geometryInput.referenceNameForEditing,
                    "Reference",
                    tooltip: "HLSL identifier used in the generated shader code. Use this with the material scripting API."));

                m_ReferenceNameField = m_ReferenceNameDrawer.textField;
                m_ReferenceNameField.RegisterValueChangedCallback(
                    evt =>
                    {
                        this._preChangeValueCallback("Change Reference Name");

                        if (evt.newValue != geometryInput.referenceName)
                        {
                            geometryInput.SetReferenceNameAndSanitizeForGraph(graphData, evt.newValue);
                            this._referenceNameChangedCallback(evt.newValue);
                        }

                        if (string.IsNullOrEmpty(geometryInput.overrideReferenceName))
                        {
                            m_ReferenceNameField.RemoveFromClassList("modified");
                            m_ReferenceNameDrawer.label.RemoveFromClassList("modified");
                        }
                        else
                        {
                            m_ReferenceNameField.AddToClassList("modified");
                            m_ReferenceNameDrawer.label.AddToClassList("modified");
                        }

                        this._postChangeValueCallback(true, ModificationScope.Graph);
                    });

                if (!string.IsNullOrEmpty(geometryInput.overrideReferenceName))
                {
                    m_ReferenceNameDrawer.textField.AddToClassList("modified");
                    m_ReferenceNameDrawer.label.AddToClassList("modified");
                }
                m_ReferenceNameDrawer.textField.SetEnabled(geometryInput.isReferenceRenamable);

                // add the right click context menu to the label
                IManipulator contextMenuManipulator = new ContextualMenuManipulator((evt) => AddGeometryInputOptionsToContextMenu(geometryInput, evt));
                m_ReferenceNameDrawer.label.AddManipulator(contextMenuManipulator);
            }
        }

        void AddGeometryInputOptionsToContextMenu(GeometryInput geometryInput, ContextualMenuPopulateEvent evt)
        {
            if (geometryInput.isRenamable && !string.IsNullOrEmpty(geometryInput.overrideReferenceName))
                evt.menu.AppendAction(
                    "Reset Reference",
                    e => { ResetReferenceName(); },
                    DropdownMenuAction.AlwaysEnabled);

            if (geometryInput.IsUsingOldDefaultRefName())
                evt.menu.AppendAction(
                    "Upgrade To New Reference Name",
                    e => { UpgradeDefaultReferenceName(); },
                    DropdownMenuAction.AlwaysEnabled);
        }

        public void ResetReferenceName()
        {
            this._preChangeValueCallback("Reset Reference Name");
            var refName = geometryInput.ResetReferenceName(graphData);
            m_ReferenceNameField.value = refName;
            this._referenceNameChangedCallback(refName);
            this._postChangeValueCallback(true, ModificationScope.Graph);
        }

        public void UpgradeDefaultReferenceName()
        {
            this._preChangeValueCallback("Upgrade Reference Name");
            var refName = geometryInput.UpgradeDefaultReferenceName(graphData);
            m_ReferenceNameField.value = refName;
            this._referenceNameChangedCallback(refName);
            this._postChangeValueCallback(true, ModificationScope.Graph);
        }

        bool isCurrentPropertyGlobal;
        void BuildPropertyFields(PropertySheet propertySheet)
        {
            if (geometryInput is AbstractGeometryProperty property)
            {
                if (property.ggVersion < property.latestVersion)
                {
                    var typeString = property.propertyType.ToString();

                    Action dismissAction = null;
                    if (property.dismissedUpdateVersion < property.latestVersion)
                    {
                        dismissAction = () =>
                        {
                            _preChangeValueCallback("Dismiss Property Update");
                            property.dismissedUpdateVersion = property.latestVersion;
                            _postChangeValueCallback();
                            inspectorUpdateDelegate?.Invoke();
                        };
                    }

                    var help = HelpBoxRow.TryGetDeprecatedHelpBoxRow($"{typeString} Property",
                        () => property.ChangeVersion(property.latestVersion),
                        dismissAction);
                    if (help != null)
                    {
                        propertySheet.Insert(0, help);
                    }
                }

                isCurrentPropertyGlobal = property.GetDefaultHLSLDeclaration() == HLSLDeclaration.Global;

                BuildPrecisionField(propertySheet, property);
                BuildHLSLDeclarationOverrideFields(propertySheet, property);
                BuildExposedField(propertySheet);

                switch (property)
                {
                    case IGeometryPropertyDrawer propDrawer:
                        propDrawer.HandlePropertyField(propertySheet, _preChangeValueCallback, _postChangeValueCallback);
                        break;
                    case MultiJsonInternal.UnknownGeometryPropertyType unknownProperty:
                        var helpBox = new HelpBoxRow(MessageType.Warning);
                        helpBox.Add(new Label("Cannot find the code for this Property, a package may be missing."));
                        propertySheet.Add(helpBox);
                        break;
                    case Vector1GeometryProperty vector1Property:
                        HandleVector1GeometryProperty(propertySheet, vector1Property);
                        break;
                    case Vector2GeometryProperty vector2Property:
                        HandleVector2GeometryProperty(propertySheet, vector2Property);
                        break;
                    case Vector3GeometryProperty vector3Property:
                        HandleVector3GeometryProperty(propertySheet, vector3Property);
                        break;
                    case Vector4GeometryProperty vector4Property:
                        HandleVector4GeometryProperty(propertySheet, vector4Property);
                        break;
                    case ColorGeometryProperty colorProperty:
                        HandleColorProperty(propertySheet, colorProperty);
                        break;
                    //case Texture2DShaderProperty texture2DProperty:
                    //    HandleTexture2DProperty(propertySheet, texture2DProperty);
                    //    break;
                    //case Texture2DArrayShaderProperty texture2DArrayProperty:
                    //    HandleTexture2DArrayProperty(propertySheet, texture2DArrayProperty);
                    //    break;
                    //case VirtualTextureShaderProperty virtualTextureProperty:
                    //    HandleVirtualTextureProperty(propertySheet, virtualTextureProperty);
                    //    break;
                    //case Texture3DShaderProperty texture3DProperty:
                    //    HandleTexture3DProperty(propertySheet, texture3DProperty);
                    //    break;
                    //case CubemapShaderProperty cubemapProperty:
                    //    HandleCubemapProperty(propertySheet, cubemapProperty);
                    //    break;
                    //case BooleanShaderProperty booleanProperty:
                    //    HandleBooleanProperty(propertySheet, booleanProperty);
                    //    break;
                    //case Matrix2ShaderProperty matrix2Property:
                    //    HandleMatrix2PropertyField(propertySheet, matrix2Property);
                    //    break;
                    //case Matrix3ShaderProperty matrix3Property:
                    //    HandleMatrix3PropertyField(propertySheet, matrix3Property);
                    //    break;
                    //case Matrix4ShaderProperty matrix4Property:
                    //    HandleMatrix4PropertyField(propertySheet, matrix4Property);
                    //    break;
                    //case SamplerStateShaderProperty samplerStateProperty:
                    //    HandleSamplerStatePropertyField(propertySheet, samplerStateProperty);
                    //    break;
                    //case GradientShaderProperty gradientProperty:
                    //    HandleGradientPropertyField(propertySheet, gradientProperty);
                    //    break;
                }
            }

            BuildCustomBindingField(propertySheet, geometryInput);
        }

        static string[] allHLSLDeclarationStrings = new string[]
        {
            "Do Not Declare",       // HLSLDeclaration.DoNotDeclare
            "Global",               // HLSLDeclaration.Global
            "Per Material",         // HLSLDeclaration.UnityPerMaterial
            "Hybrid Per Instance",  // HLSLDeclaration.HybridPerInstance
        };

        void BuildHLSLDeclarationOverrideFields(PropertySheet propertySheet, AbstractGeometryProperty property)
        {
            if (isSubGraph)
                return;

            var hlslDecls = Enum.GetValues(typeof(HLSLDeclaration));
            var allowedDecls = new List<HLSLDeclaration>();

            bool anyAllowed = false;
            for (int i = 0; i < hlslDecls.Length; i++)
            {
                HLSLDeclaration decl = (HLSLDeclaration)hlslDecls.GetValue(i);
                var allowed = property.AllowHLSLDeclaration(decl);
                anyAllowed = anyAllowed || allowed;
                if (allowed)
                    allowedDecls.Add(decl);
            }

            const string tooltip = "Indicate where the property is expected to be changed.";
            if (anyAllowed)
            {
                var propRow = new PropertyRow(PropertyDrawerUtils.CreateLabel("Scope", 0));
                propRow.tooltip = tooltip;
                var popupField = new PopupField<HLSLDeclaration>(
                    allowedDecls,
                    property.GetDefaultHLSLDeclaration(),
                    (h => allHLSLDeclarationStrings[(int)h]),
                    (h => allHLSLDeclarationStrings[(int)h]));

                popupField.RegisterValueChangedCallback(
                    evt =>
                    {
                        this._preChangeValueCallback("Change Scope");
                        if (property.hlslDeclarationOverride == evt.newValue)
                            return;
                        property.hlslDeclarationOverride = evt.newValue;
                        property.overrideHLSLDeclaration = true;
                        UpdateEnableState();
                        this._exposedFieldChangedCallback(evt.newValue != HLSLDeclaration.Global);
                        property.generatePropertyBlock = evt.newValue != HLSLDeclaration.Global;
                        this._postChangeValueCallback(true, ModificationScope.Graph);
                    });

                propRow.Add(popupField);
                propertySheet.Add(propRow);
            }
        }

        void BuildPrecisionField(PropertySheet propertySheet, AbstractGeometryProperty property)
        {
            var enumPropertyDrawer = new EnumPropertyDrawer();
            propertySheet.Add(enumPropertyDrawer.CreateGUI(newValue =>
            {
                this._preChangeValueCallback("Change Precision");
                if (property.precision == (Precision)newValue)
                    return;
                property.precision = (Precision)newValue;
                this._precisionChangedCallback();
                this._postChangeValueCallback();
            },
                (PropertyDrawerUtils.UIPrecisionForShaderGraphs)property.precision,
                "Precision",
                PropertyDrawerUtils.UIPrecisionForShaderGraphs.Inherit,
                out var precisionField,
                tooltip: "Request that the property uses Single 32-bit or Half 16-bit floating point precision."));
            if (property is MultiJsonInternal.UnknownGeometryPropertyType)
                precisionField.SetEnabled(false);
        }

        void HandleVector1GeometryProperty(PropertySheet propertySheet, Vector1GeometryProperty vector1ShaderProperty)
        {
            var floatType = isCurrentPropertyGlobal ? FloatType.Default : vector1ShaderProperty.floatType;
            // Handle vector 1 mode parameters
            switch (floatType)
            {
                case FloatType.Slider:
                    var floatPropertyDrawer = new FloatPropertyDrawer();
                    // Default field
                    propertySheet.Add(floatPropertyDrawer.CreateGUI(
                        newValue =>
                        {
                            _preChangeValueCallback("Change Property Value");
                            _changeValueCallback(newValue);
                            _postChangeValueCallback();
                        },
                        vector1ShaderProperty.value,
                        isCurrentPropertyGlobal ? "Preview Value" : "Default Value",
                        out var propertyFloatField));

                    // Min field
                    propertySheet.Add(floatPropertyDrawer.CreateGUI(
                        newValue =>
                        {
                            if (newValue > vector1ShaderProperty.rangeValues.y)
                                propertySheet.warningContainer.Q<Label>().text = "Min cannot be greater than Max.";
                            _preChangeValueCallback("Change Range Property Minimum");
                            vector1ShaderProperty.rangeValues = new Vector2(newValue, vector1ShaderProperty.rangeValues.y);
                            _postChangeValueCallback();
                        },
                        vector1ShaderProperty.rangeValues.x,
                        "Min",
                        out var minFloatField));

                    // Max field
                    propertySheet.Add(floatPropertyDrawer.CreateGUI(
                        newValue =>
                        {
                            if (newValue < vector1ShaderProperty.rangeValues.x)
                                propertySheet.warningContainer.Q<Label>().text = "Max cannot be lesser than Min.";
                            this._preChangeValueCallback("Change Range Property Maximum");
                            vector1ShaderProperty.rangeValues = new Vector2(vector1ShaderProperty.rangeValues.x, newValue);
                            this._postChangeValueCallback();
                        },
                        vector1ShaderProperty.rangeValues.y,
                        "Max",
                        out var maxFloatField));

                    var defaultField = (FloatField)propertyFloatField;
                    var minField = (FloatField)minFloatField;
                    var maxField = (FloatField)maxFloatField;

                    minField.Q("unity-text-input").RegisterCallback<FocusOutEvent>(evt =>
                    {
                        propertySheet.warningContainer.Q<Label>().text = "";
                        vector1ShaderProperty.value = Mathf.Max(Mathf.Min(vector1ShaderProperty.value, vector1ShaderProperty.rangeValues.y), vector1ShaderProperty.rangeValues.x);
                        defaultField.value = vector1ShaderProperty.value;
                        _postChangeValueCallback();
                    }, TrickleDown.TrickleDown);

                    maxField.Q("unity-text-input").RegisterCallback<FocusOutEvent>(evt =>
                    {
                        propertySheet.warningContainer.Q<Label>().text = "";
                        vector1ShaderProperty.value = Mathf.Max(Mathf.Min(vector1ShaderProperty.value, vector1ShaderProperty.rangeValues.y), vector1ShaderProperty.rangeValues.x);
                        defaultField.value = vector1ShaderProperty.value;
                        _postChangeValueCallback();
                    }, TrickleDown.TrickleDown);
                    break;

                case FloatType.Integer:
                    var integerPropertyDrawer = new IntegerPropertyDrawer();
                    // Default field
                    propertySheet.Add(integerPropertyDrawer.CreateGUI(
                        newValue =>
                        {
                            this._preChangeValueCallback("Change property value");
                            this._changeValueCallback((float)newValue);
                            this._postChangeValueCallback();
                        },
                        (int)vector1ShaderProperty.value,
                        isCurrentPropertyGlobal ? "Preview Value" : "Default Value",
                        out var integerPropertyField));
                    break;

                default:
                    var defaultFloatPropertyDrawer = new FloatPropertyDrawer();
                    // Default field
                    propertySheet.Add(defaultFloatPropertyDrawer.CreateGUI(
                        newValue =>
                        {
                            this._preChangeValueCallback("Change property value");
                            this._changeValueCallback(newValue);
                            this._postChangeValueCallback();
                        },
                        vector1ShaderProperty.value,
                        isCurrentPropertyGlobal ? "Preview Value" : "Default Value",
                        out var defaultFloatPropertyField));
                    break;
            }

            if (!isSubGraph && !isCurrentPropertyGlobal)
            {
                var enumPropertyDrawer = new EnumPropertyDrawer();
                propertySheet.Add(enumPropertyDrawer.CreateGUI(
                    newValue =>
                    {
                        this._preChangeValueCallback("Change Vector1 Mode");
                        vector1ShaderProperty.floatType = (FloatType)newValue;
                        this._postChangeValueCallback(true);
                    },
                    (FloatTypeForUI)vector1ShaderProperty.floatType,
                    "Mode",
                    FloatTypeForUI.Default,
                    out var modePropertyEnumField,
                    tooltip: "Indicate how this float property should appear in the material inspector UI."));
            }
        }
        enum FloatTypeForUI { Default = FloatType.Default, Integer = FloatType.Integer, Slider = FloatType.Slider }

        void HandleVector2ShaderProperty(PropertySheet propertySheet, Vector2GeometryProperty vector2ShaderProperty)
        {
            var vector2PropertyDrawer = new Vector2PropertyDrawer();
            vector2PropertyDrawer.preValueChangeCallback = () => this._preChangeValueCallback("Change property value");
            vector2PropertyDrawer.postValueChangeCallback = () => this._postChangeValueCallback();

            propertySheet.Add(vector2PropertyDrawer.CreateGUI(
                newValue => _changeValueCallback(newValue),
                vector2ShaderProperty.value,
                isCurrentPropertyGlobal ? "Preview Value" : "Default Value",
                out var propertyVec2Field));
        }

        void HandleVector3ShaderProperty(PropertySheet propertySheet, Vector3GeometryProperty vector3ShaderProperty)
        {
            var vector3PropertyDrawer = new Vector3PropertyDrawer();
            vector3PropertyDrawer.preValueChangeCallback = () => this._preChangeValueCallback("Change property value");
            vector3PropertyDrawer.postValueChangeCallback = () => this._postChangeValueCallback();

            propertySheet.Add(vector3PropertyDrawer.CreateGUI(
                newValue => _changeValueCallback(newValue),
                vector3ShaderProperty.value,
                isCurrentPropertyGlobal ? "Preview Value" : "Default Value",
                out var propertyVec3Field));
        }

        void HandleVector4ShaderProperty(PropertySheet propertySheet, Vector4GeometryProperty vector4Property)
        {
            var vector4PropertyDrawer = new Vector4PropertyDrawer();
            vector4PropertyDrawer.preValueChangeCallback = () => this._preChangeValueCallback("Change property value");
            vector4PropertyDrawer.postValueChangeCallback = () => this._postChangeValueCallback();

            propertySheet.Add(vector4PropertyDrawer.CreateGUI(
                newValue => _changeValueCallback(newValue),
                vector4Property.value,
                isCurrentPropertyGlobal ? "Preview Value" : "Default Value",
                out var propertyVec4Field));
        }

        void HandleColorProperty(PropertySheet propertySheet, ColorGeometryProperty colorProperty)
        {
            var colorPropertyDrawer = new ColorPropertyDrawer();

            if (!isSubGraph)
            {
                if (colorProperty.isMainColor)
                {
                    var mainColorLabel = new IMGUIContainer(() =>
                    {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.LabelField("Main Color", EditorStyles.largeLabel);
                        EditorGUILayout.Space();
                        EditorGUI.indentLevel--;
                    });
                    propertySheet.Insert(2, mainColorLabel);
                }
            }

            propertySheet.Add(colorPropertyDrawer.CreateGUI(
                newValue =>
                {
                    this._preChangeValueCallback("Change property value");
                    this._changeValueCallback(newValue);
                    this._postChangeValueCallback();
                },
                colorProperty.value,
                isCurrentPropertyGlobal ? "Preview Value" : "Default Value",
                out var propertyColorField));

            var colorField = (ColorField)propertyColorField;
            colorField.hdr = colorProperty.colorMode == ColorMode.HDR;

            if (!isSubGraph && !isCurrentPropertyGlobal)
            {
                var enumPropertyDrawer = new EnumPropertyDrawer();

                propertySheet.Add(enumPropertyDrawer.CreateGUI(
                    newValue =>
                    {
                        this._preChangeValueCallback("Change Color Mode");
                        colorProperty.colorMode = (ColorMode)newValue;
                        this._postChangeValueCallback(true, ModificationScope.Graph);
                    },
                    colorProperty.colorMode,
                    "Mode",
                    ColorMode.Default,
                    out var colorModeField));
            }
        }

        //enum KeywordGeometryStageDropdownUI    // maps to KeywordShaderStage, this enum ONLY used for the UI dropdown menu
        //{
        //    All = KeywordGeometryStage.All,
        //    Vertex = KeywordGeometryStage.Vertex,
        //    Fragment = KeywordGeometryStage.Fragment,
        //}

        void BuildKeywordFields(PropertySheet propertySheet, GeometryInput shaderInput)
        {
            // TODO
            //var keyword = shaderInput as ShaderKeyword;
            //if (keyword == null)
            //    return;

            //var enumPropertyDrawer = new EnumPropertyDrawer();
            //propertySheet.Add(enumPropertyDrawer.CreateGUI(
            //    newValue =>
            //    {
            //        this._preChangeValueCallback("Change Keyword type");
            //        if (keyword.keywordDefinition == (KeywordDefinition)newValue)
            //            return;
            //        keyword.keywordDefinition = (KeywordDefinition)newValue;
            //        UpdateEnableState();
            //        this._postChangeValueCallback(true, ModificationScope.Nothing);
            //    },
            //    keyword.keywordDefinition,
            //    "Definition",
            //    KeywordDefinition.ShaderFeature,
            //    out var typeField,
            //    tooltip: "Indicate how the keyword is defined and under what circumstances its permutations will be compiled."));

            //if (keyword.keywordDefinition == KeywordDefinition.ShaderFeature && isSubGraph)
            //{
            //    var help = new HelpBoxRow(MessageType.Info);
            //    var warning = new TextElement();
            //    warning.tabIndex = 1;
            //    warning.style.alignSelf = Align.Center;
            //    warning.text = "Shader Feature Keywords in SubGraphs do not generate variant permutations.";
            //    help.Add(warning);
            //    propertySheet.Add(help);
            //}

            //typeField.SetEnabled(!keyword.isBuiltIn);
            //{
            //    var isOverridablePropertyDrawer = new ToggleDataPropertyDrawer();
            //    bool enabledState = keyword.keywordDefinition != KeywordDefinition.Predefined;
            //    bool toggleState = keyword.keywordScope == KeywordScope.Global || !enabledState;
            //    propertySheet.Add(isOverridablePropertyDrawer.CreateGUI(
            //        newValue =>
            //        {
            //            this._preChangeValueCallback("Change Keyword Is Overridable");
            //            keyword.keywordScope = newValue.isOn
            //                ? KeywordScope.Global
            //                : KeywordScope.Local;
            //        },
            //        new ToggleData(toggleState),
            //        "Is Overridable",
            //        out keywordScopeField,
            //        tooltip: "Indicate whether this keyword's state can be overridden through the Shader.SetKeyword scripting interface."));
            //    keywordScopeField.SetEnabled(enabledState);
            //}
            //BuildExposedField(propertySheet);
            //{
            //    propertySheet.Add(enumPropertyDrawer.CreateGUI(
            //        newValue =>
            //        {
            //            this._preChangeValueCallback("Change Keyword stage");
            //            if (keyword.keywordStages == (KeywordShaderStage)newValue)
            //                return;
            //            keyword.keywordStages = (KeywordShaderStage)newValue;
            //        },
            //        (KeywordShaderStageDropdownUI)keyword.keywordStages,
            //        "Stages",
            //        KeywordShaderStageDropdownUI.All,
            //        out keywordScopeField,
            //        tooltip: "Indicates which shader stages this keyword is relevant for."));
            //}

            //switch (keyword.keywordType)
            //{
            //    case KeywordType.Boolean:
            //        BuildBooleanKeywordField(propertySheet, keyword);
            //        break;
            //    case KeywordType.Enum:
            //        BuildEnumKeywordField(propertySheet, keyword);
            //        break;
            //}
        }

        void BuildBooleanKeywordField(PropertySheet propertySheet, ShaderKeyword keyword)
        {
            // TODO
            //var toggleDataPropertyDrawer = new ToggleDataPropertyDrawer();
            //propertySheet.Add(toggleDataPropertyDrawer.CreateGUI(
            //    newValue =>
            //    {
            //        this._preChangeValueCallback("Change property value");
            //        keyword.value = newValue.isOn ? 1 : 0;
            //        if (graphData.owner.materialArtifact)
            //        {
            //            graphData.owner.materialArtifact.SetFloat(keyword.referenceName, keyword.value);
            //            MaterialEditor.ApplyMaterialPropertyDrawers(graphData.owner.materialArtifact);
            //        }
            //        this._postChangeValueCallback(false, ModificationScope.Graph);
            //    },
            //    new ToggleData(keyword.value == 1),
            //    keyword.keywordDefinition == KeywordDefinition.Predefined ? "Preview Value" : "Default Value",
            //    out var boolKeywordField));
        }

        void BuildEnumKeywordField(PropertySheet propertySheet, ShaderKeyword keyword)
        {
            // TODO
            //// Clamp value between entry list
            //int value = Mathf.Clamp(keyword.value, 0, keyword.entries.Count - 1);

            //// Default field
            //var field = new PopupField<string>(keyword.entries.Select(x => x.displayName).ToList(), value);
            //field.RegisterValueChangedCallback(evt =>
            //{
            //    this._preChangeValueCallback("Change Keyword Value");
            //    keyword.value = field.index;
            //    if (graphData.owner.materialArtifact)
            //    {
            //        graphData.owner.materialArtifact.SetFloat(keyword.referenceName, field.index);
            //        MaterialEditor.ApplyMaterialPropertyDrawers(graphData.owner.materialArtifact);
            //    }
            //    this._postChangeValueCallback(false, ModificationScope.Graph);
            //});

            //AddPropertyRowToSheet(propertySheet, field, keyword.keywordDefinition == KeywordDefinition.Predefined ? "Preview Value" : "Default Value");

            //var container = new IMGUIContainer(() => OnKeywordGUIHandler()) { name = "ListContainer" };
            //AddPropertyRowToSheet(propertySheet, container, "Entries");
            //container.SetEnabled(!keyword.isBuiltIn);
        }

        static void AddPropertyRowToSheet(PropertySheet propertySheet, VisualElement control, string labelName)
        {
            propertySheet.Add(new PropertyRow(new Label(labelName)), (row) =>
            {
                row.styleSheets.Add(Resources.Load<StyleSheet>("Styles/PropertyRow"));
                row.Add(control);
            });
        }

        void OnKeywordGUIHandler()
        {
            if (m_KeywordReorderableList == null)
            {
                KeywordRecreateList();
                KeywordAddCallbacks();
            }

            m_KeywordReorderableList.index = m_KeywordSelectedIndex;
            m_KeywordReorderableList.DoLayoutList();
        }

        internal void KeywordRecreateList()
        {
            if (!(geometryInput is ShaderKeyword keyword))
                return;

            // TODO
            // Create reorderable list from entries
            //m_KeywordReorderableList = new ReorderableList(keyword.entries, typeof(KeywordEntry), true, true, true, true);
        }

        void KeywordAddCallbacks()
        {
            if (!(geometryInput is ShaderKeyword keyword))
                return;

            // TODO
            //// Draw Header
            //m_KeywordReorderableList.drawHeaderCallback = (Rect rect) =>
            //{
            //    int indent = 14;
            //    var displayRect = new Rect(rect.x + indent, rect.y, (rect.width - indent) / 2, rect.height);
            //    EditorGUI.LabelField(displayRect, "Entry Name");
            //    var referenceRect = new Rect((rect.x + indent) + (rect.width - indent) / 2, rect.y, (rect.width - indent) / 2, rect.height);
            //    EditorGUI.LabelField(referenceRect, "Reference Suffix", keyword.isBuiltIn ? EditorStyles.label : greyLabel);
            //};

            //// Draw Element
            //m_KeywordReorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            //{
            //    KeywordEntry entry = ((KeywordEntry)m_KeywordReorderableList.list[index]);
            //    EditorGUI.BeginChangeCheck();

            //    Rect displayRect = new Rect(rect.x, rect.y, rect.width / 2, EditorGUIUtility.singleLineHeight);
            //    var displayName = EditorGUI.DelayedTextField(displayRect, entry.displayName, EditorStyles.label);
            //    //This is gross but I cant find any other way to make a DelayedTextField have a tooltip (tried doing the empty label on the field itself and it didnt work either)
            //    EditorGUI.LabelField(displayRect, new GUIContent("", "Enum keyword display names can only use alphanumeric characters, whitespace and `_`"));
            //    var referenceName = EditorGUI.TextField(new Rect(rect.x + rect.width / 2, rect.y, rect.width / 2, EditorGUIUtility.singleLineHeight), entry.referenceName,
            //        keyword.isBuiltIn ? EditorStyles.label : greyLabel);

            //    if (EditorGUI.EndChangeCheck())
            //    {
            //        displayName = GetSanitizedDisplayName(displayName);
            //        referenceName = GetSanitizedReferenceName(displayName.ToUpper());
            //        var duplicateIndex = FindDuplicateKeywordReferenceNameIndex(entry.id, referenceName);
            //        if (duplicateIndex != -1)
            //        {
            //            var duplicateEntry = ((KeywordEntry)m_KeywordReorderableList.list[duplicateIndex]);
            //            Debug.LogWarning($"Display name '{displayName}' will create the same reference name '{referenceName}' as entry {duplicateIndex + 1} with display name '{duplicateEntry.displayName}'.");
            //        }
            //        else if (string.IsNullOrWhiteSpace(displayName))
            //            Debug.LogWarning("Invalid display name. Display names cannot be empty or all whitespace.");
            //        else if (int.TryParse(displayName, out int intVal) || float.TryParse(displayName, out float floatVal))
            //            Debug.LogWarning("Invalid display name. Display names cannot be valid integer or floating point numbers.");
            //        else
            //            keyword.entries[index] = new KeywordEntry(GetFirstUnusedKeywordID(), displayName, referenceName);

            //        this._postChangeValueCallback(true);
            //    }
            //};

            //// Element height
            //m_KeywordReorderableList.elementHeightCallback = (int indexer) =>
            //{
            //    return m_KeywordReorderableList.elementHeight;
            //};

            //// Can add
            //m_KeywordReorderableList.onCanAddCallback = (ReorderableList list) =>
            //{
            //    return list.count < 8;
            //};

            //// Can remove
            //m_KeywordReorderableList.onCanRemoveCallback = (ReorderableList list) =>
            //{
            //    return list.count > 2;
            //};

            //// Add callback delegates
            //m_KeywordReorderableList.onSelectCallback += KeywordSelectEntry;
            //m_KeywordReorderableList.onAddCallback += KeywordAddEntry;
            //m_KeywordReorderableList.onRemoveCallback += KeywordRemoveEntry;
            //m_KeywordReorderableList.onReorderCallback += KeywordReorderEntries;
        }

        void KeywordSelectEntry(ReorderableList list)
        {
            m_KeywordSelectedIndex = list.index;
        }

        // Allowed indicies are 1-MAX_ENUM_ENTRIES
        int GetFirstUnusedKeywordID()
        {
            if (!(geometryInput is ShaderKeyword keyword))
                return 0;

            // TODO
            //List<int> unusedIDs = new List<int>();

            //foreach (KeywordEntry keywordEntry in keyword.entries)
            //{
            //    unusedIDs.Add(keywordEntry.id);
            //}

            //for (int x = 1; x <= KeywordNode.k_MaxEnumEntries; x++)
            //{
            //    if (!unusedIDs.Contains(x))
            //        return x;
            //}

            Debug.LogError("GetFirstUnusedID: Attempting to get unused ID when all IDs are used.");
            return -1;
        }

        void KeywordAddEntry(ReorderableList list)
        {
            if (!(geometryInput is ShaderKeyword keyword))
                return;

            // TODO
            //this._preChangeValueCallback("Add Keyword Entry");

            //int index = GetFirstUnusedKeywordID();
            //if (index <= 0)
            //    return; // Error has already occured, don't attempt to add this entry.

            //var displayName = "New";
            //var referenceName = "NEW";
            //GetDuplicateSafeEnumNames(index, "New", out displayName, out referenceName);

            //// Add new entry
            //keyword.entries.Add(new KeywordEntry(index, displayName, referenceName));

            //// Update GUI
            //this._postChangeValueCallback(true);
            //this._keywordChangedCallback();
            //m_KeywordSelectedIndex = list.list.Count - 1;
        }

        void KeywordRemoveEntry(ReorderableList list)
        {
            if (!(geometryInput is ShaderKeyword keyword))
                return;

            // TODO
            //this._preChangeValueCallback("Remove Keyword Entry");

            //// Remove entry
            //m_KeywordSelectedIndex = list.index;
            //var selectedEntry = (KeywordEntry)m_KeywordReorderableList.list[list.index];
            //keyword.entries.Remove(selectedEntry);

            //// Clamp value within new entry range
            //int value = Mathf.Clamp(keyword.value, 0, keyword.entries.Count - 1);
            //keyword.value = value;

            //// Rebuild();
            //this._postChangeValueCallback(true);
            //this._keywordChangedCallback();
            //m_KeywordSelectedIndex = m_KeywordSelectedIndex >= list.list.Count - 1 ? list.list.Count - 1 : m_KeywordSelectedIndex;
        }

        void KeywordReorderEntries(ReorderableList list)
        {
            this._postChangeValueCallback(true);
        }

        public string GetDuplicateSafeEnumDisplayName(int id, string name)
        {
            // TODO
            //name = name.Trim();
            //var entryList = m_KeywordReorderableList.list as List<KeywordEntry>;
            //return GraphUtil.SanitizeName(entryList.Where(p => p.id != id).Select(p => p.displayName), "{0} {1}", name, m_DisplayNameDisallowedPattern);
            return "";
        }

        void GetDuplicateSafeEnumNames(int id, string name, out string displayName, out string referenceName)
        {
            name = name.Trim();
            // Get de-duplicated display and reference names
            displayName = GetDuplicateSafeEnumDisplayName(id, name);
            referenceName = GetDuplicateSafeReferenceName(id, displayName.ToUpper());
            // Check when the simple reference name should be for the display name.
            // If these don't match then there will be a desync which causes the enum entry to not work.
            // An example where this happens is ["new 1", "NEW_1"] already exists.
            // The display name "New_1" is added.
            // This new display name doesn't exist, but it finds the reference name of "NEW_1" already exists so we get the pair ["New_1", "NEW_2"] which is invalid.
            // The easiest fix in this case is to just use the safe reference name as the new display name which is guaranteed to be unique.
            var simpleReferenceName = Regex.Replace(displayName.ToUpper(), m_ReferenceNameDisallowedPattern, "_");
            if (referenceName != simpleReferenceName)
                displayName = referenceName;
        }

        string GetSanitizedDisplayName(string name)
        {
            name = name.Trim();
            return Regex.Replace(name, m_DisplayNameDisallowedPattern, "_");
        }

        public string GetDuplicateSafeReferenceName(int id, string name)
        {
            // TODO
            //name = name.Trim();
            //var entryList = m_KeywordReorderableList.list as List<KeywordEntry>;
            //return GraphUtil.SanitizeName(entryList.Where(p => p.id != id).Select(p => p.referenceName), "{0}_{1}", name, m_ReferenceNameDisallowedPattern);
            return "";
        }

        string GetSanitizedReferenceName(string name)
        {
            name = name.Trim();
            return Regex.Replace(name, m_ReferenceNameDisallowedPattern, "_");
        }

        int FindDuplicateKeywordReferenceNameIndex(int id, string referenceName)
        {
            // TODO
            //var entryList = m_KeywordReorderableList.list as List<KeywordEntry>;
            //return entryList.FindIndex(entry => entry.id != id && entry.referenceName == referenceName);
            return -1;
        }

        void BuildDropdownFields(PropertySheet propertySheet, GeometryInput geometryInput)
        {
            var dropdown = geometryInput as GeometryDropdown;
            if (dropdown == null)
                return;

            BuildDropdownField(propertySheet, dropdown);
            BuildExposedField(propertySheet);
        }

        void BuildDropdownField(PropertySheet propertySheet, GeometryDropdown dropdown)
        {
            // Clamp value between entry list
            int value = Mathf.Clamp(dropdown.value, 0, dropdown.entries.Count - 1);

            // Default field
            var field = new PopupField<string>(dropdown.entries.Select(x => x.displayName).ToList(), value);
            field.RegisterValueChangedCallback(evt =>
            {
                this._preChangeValueCallback("Change Dropdown Value");
                dropdown.value = field.index;
                m_DropdownId = dropdown.entryId;
                this._postChangeValueCallback(false, ModificationScope.Graph);
            });

            AddPropertyRowToSheet(propertySheet, field, "Default");

            var container = new IMGUIContainer(() => OnDropdownGUIHandler()) { name = "ListContainer" };
            AddPropertyRowToSheet(propertySheet, container, "Entries");
            container.SetEnabled(true);
        }

        void OnDropdownGUIHandler()
        {
            if (m_DropdownReorderableList == null)
            {
                DropdownRecreateList();
                DropdownAddCallbacks();
            }

            m_DropdownReorderableList.index = m_DropdownSelectedIndex;
            m_DropdownReorderableList.DoLayoutList();
        }

        internal void DropdownRecreateList()
        {
            if (!(geometryInput is GeometryDropdown dropdown))
                return;

            // Create reorderable list from entries
            m_DropdownReorderableList = new ReorderableList(dropdown.entries, typeof(DropdownEntry), true, true, true, true);
            m_Dropdown = dropdown;
            m_DropdownId = dropdown.entryId;
        }

        void DropdownAddCallbacks()
        {
            if (!(geometryInput is GeometryDropdown dropdown))
                return;

            // Draw Header
            m_DropdownReorderableList.drawHeaderCallback = (Rect rect) =>
            {
                int indent = 14;
                var displayRect = new Rect(rect.x + indent, rect.y, (rect.width - indent) / 2, rect.height);
                EditorGUI.LabelField(displayRect, "Entry Name");
            };

            // Draw Element
            m_DropdownReorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                DropdownEntry entry = ((DropdownEntry)m_DropdownReorderableList.list[index]);
                EditorGUI.BeginChangeCheck();

                Rect displayRect = new Rect(rect.x, rect.y, rect.width / 2, EditorGUIUtility.singleLineHeight);
                var displayName = EditorGUI.DelayedTextField(displayRect, entry.displayName, EditorStyles.label);

                if (EditorGUI.EndChangeCheck())
                {
                    displayName = GetSanitizedDisplayName(displayName);
                    var duplicateIndex = FindDuplicateDropdownDisplayNameIndex(entry.id, displayName);
                    if (duplicateIndex != -1)
                    {
                        var duplicateEntry = ((DropdownEntry)m_DropdownReorderableList.list[duplicateIndex]);
                        Debug.LogWarning($"Display name '{displayName}' will create the same display name as entry {duplicateIndex + 1}.");
                    }
                    else if (string.IsNullOrWhiteSpace(displayName))
                        Debug.LogWarning("Invalid display name. Display names cannot be empty or all whitespace.");
                    else if (int.TryParse(displayName, out int intVal) || float.TryParse(displayName, out float floatVal))
                        Debug.LogWarning("Invalid display name. Display names cannot be valid integer or floating point numbers.");
                    else
                        dropdown.entries[index] = new DropdownEntry(GetFirstUnusedDropdownID(), displayName);

                    this._postChangeValueCallback(true);
                }
            };

            // Element height
            m_DropdownReorderableList.elementHeightCallback = (int indexer) =>
            {
                return m_DropdownReorderableList.elementHeight;
            };

            // Can add
            m_DropdownReorderableList.onCanAddCallback = (ReorderableList list) =>
            {
                return true;
            };

            // Can remove
            m_DropdownReorderableList.onCanRemoveCallback = (ReorderableList list) =>
            {
                return list.count > DropdownNode.k_MinEnumEntries;
            };

            // Add callback delegates
            m_DropdownReorderableList.onSelectCallback += DropdownSelectEntry;
            m_DropdownReorderableList.onAddCallback += DropdownAddEntry;
            m_DropdownReorderableList.onRemoveCallback += DropdownRemoveEntry;
            m_DropdownReorderableList.onReorderCallback += DropdownReorderEntries;
        }

        void DropdownSelectEntry(ReorderableList list)
        {
            m_DropdownSelectedIndex = list.index;
        }

        int GetFirstUnusedDropdownID()
        {
            if (!(geometryInput is GeometryDropdown dropdown))
                return 0;

            List<int> ids = new List<int>();

            foreach (DropdownEntry dropdownEntry in dropdown.entries)
            {
                ids.Add(dropdownEntry.id);
            }

            for (int x = 1; ; x++)
            {
                if (!ids.Contains(x))
                    return x;
            }
        }

        void DropdownAddEntry(ReorderableList list)
        {
            if (!(geometryInput is GeometryDropdown dropdown))
                return;

            this._preChangeValueCallback("Add Dropdown Entry");

            int index = GetFirstUnusedDropdownID();

            var displayName = GetDuplicateSafeDropdownDisplayName(index, "New");

            // Add new entry
            dropdown.entries.Add(new DropdownEntry(index, displayName));

            // Update GUI
            this._postChangeValueCallback(true);
            this._dropdownChangedCallback();
            m_DropdownSelectedIndex = list.list.Count - 1;
        }

        void DropdownRemoveEntry(ReorderableList list)
        {
            if (!(geometryInput is GeometryDropdown dropdown))
                return;

            this._preChangeValueCallback("Remove Dropdown Entry");

            // Remove entry
            m_DropdownSelectedIndex = list.index;
            var selectedEntry = (DropdownEntry)m_DropdownReorderableList.list[list.index];
            dropdown.entries.Remove(selectedEntry);

            // Clamp value within new entry range
            int value = Mathf.Clamp(dropdown.value, 0, dropdown.entries.Count - 1);
            dropdown.value = value;

            this._postChangeValueCallback(true);
            this._dropdownChangedCallback();
            m_DropdownSelectedIndex = m_DropdownSelectedIndex >= list.list.Count - 1 ? list.list.Count - 1 : m_DropdownSelectedIndex;
        }

        void DropdownReorderEntries(ReorderableList list)
        {
            var index = m_Dropdown.IndexOfId(m_DropdownId);
            if (index != m_Dropdown.value)
                m_Dropdown.value = index;
            this._postChangeValueCallback(true);
        }

        public string GetDuplicateSafeDropdownDisplayName(int id, string name)
        {
            var entryList = m_DropdownReorderableList.list as List<DropdownEntry>;
            return GraphUtil.SanitizeName(entryList.Where(p => p.id != id).Select(p => p.displayName), "{0} {1}", name, m_DisplayNameDisallowedPattern);
        }

        int FindDuplicateDropdownDisplayNameIndex(int id, string displayName)
        {
            var entryList = m_DropdownReorderableList.list as List<DropdownEntry>;
            return entryList.FindIndex(entry => entry.id != id && entry.displayName == displayName);
        }
    }
}
