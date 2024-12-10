using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BXGraphing;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace BXGeometryGraph
{
    public class BlackboardProvider
    {
        private readonly AbstructGeometryGraph m_Graph;
        private readonly Texture2D m_ExposedIcon;
        private readonly Dictionary<Guid, BlackboardRow> m_PropertyRows;
        private readonly BlackboardSection m_Section;
        //WindowDraggable m_WindowDraggable;
        //ResizeBorderFrame m_ResizeBorderFrame;
        public Blackboard blackboard { get; private set; }
        private Label m_PathLabel;
        private TextField m_PathLabelTextField;
        private bool m_EditPathCancelled = false;

        private const int k_DefaultWidht = 200;
        private const int k_DefaultHeight = 400;

        //public Action onDragFinished
        //{
        //    get { return m_WindowDraggable.OnDragFinished; }
        //    set { m_WindowDraggable.OnDragFinished = value; }
        //}

        //public Action onResizeFinished
        //{
        //    get { return m_ResizeBorderFrame.OnResizeFinished; }
        //    set { m_ResizeBorderFrame.OnResizeFinished = value; }
        //}

        public BlackboardProvider(string assetName, AbstructGeometryGraph graph)
        {
            m_Graph = graph;
            m_ExposedIcon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Scripts/BXRenderPipeline/GeometryGraph/Editor/Resources/Icons/BlackboardFieldExposed.png");
            m_PropertyRows = new Dictionary<Guid, BlackboardRow>();

            blackboard = new Blackboard()
            {
                scrollable = true,
                title = assetName.Split('/').Last(),
                subTitle = FormatPath(graph.path),
                editTextRequested = EditTextRequested,
                addItemRequested = AddItemRequested,
                moveItemRequested = MoveItemRequested
            };
            //if(blackboard.layout.width == 0 || blackboard.layout.height == 0)
            //{
            blackboard.SetPosition(new Rect(blackboard.layout.x, blackboard.layout.y, k_DefaultWidht, k_DefaultHeight));
            //}

            m_PathLabel = blackboard.hierarchy.ElementAt(0).Q<Label>("subTitleLabel");
            m_PathLabel.RegisterCallback<MouseDownEvent>(OnMouseDownEvent);

            m_PathLabelTextField = new TextField { visible = false };
            m_PathLabelTextField.RegisterCallback<FocusOutEvent>(e => { OnEditPathTextFinished(); });
            m_PathLabelTextField.RegisterCallback<KeyDownEvent>(OnPathTextFieldKeyPressed);
            blackboard.hierarchy.Add(m_PathLabelTextField);

            // m_WindowDraggable = new WindowDraggable(blackboard.shadow.Children().First().Q("header"));
            // blackboard.AddManipulator(m_WindowDraggable);

            // m_ResizeBorderFrame = new ResizeBorderFrame(blackboard) { name = "resizeBorderFrame" };
            // blackboard.shadow.Add(m_ResizeBorderFrame);

            m_Section = new BlackboardSection { headerVisible = false };
            foreach (var property in graph.properties)
                AddProperty(property);
            blackboard.Add(m_Section);
        }

        private void OnMouseDownEvent(MouseDownEvent evt)
        {
            if(evt.clickCount == 2 && evt.button == (int)MouseButton.LeftMouse)
            {
                StartEditingPath();
                evt.PreventDefault();
            }
        }

        private void StartEditingPath()
        {
            m_PathLabelTextField.visible = true;

            m_PathLabelTextField.value = m_PathLabel.text;
            m_PathLabelTextField.style.position = Position.Absolute;
            var rect = m_PathLabel.ChangeCoordinatesTo(blackboard, new Rect(Vector2.zero, m_PathLabel.layout.size));
            m_PathLabelTextField.style.left = rect.xMin;
            m_PathLabelTextField.style.top = rect.yMin;
            m_PathLabelTextField.style.width = rect.width;
            m_PathLabelTextField.style.fontSize = 11;
            m_PathLabelTextField.style.marginLeft = 0;
            m_PathLabelTextField.style.marginRight = 0;
            m_PathLabelTextField.style.marginTop = 0;
            m_PathLabelTextField.style.marginBottom = 0;

            m_PathLabel.visible = false;

            m_PathLabelTextField.Focus();
            m_PathLabelTextField.SelectAll();
        }

        private void OnPathTextFieldKeyPressed(KeyDownEvent evt)
        {
            switch (evt.keyCode)
            {
                case KeyCode.Escape:
                    m_EditPathCancelled = true;
                    m_PathLabelTextField.Blur();
                    break;
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    m_PathLabelTextField.Blur();
                    break;
                default:
                    break;
            }
        }

        private void OnEditPathTextFinished()
        {
            m_PathLabel.visible = true;
            m_PathLabelTextField.visible = false;

            var newPath = m_PathLabelTextField.text;
            if(!m_EditPathCancelled && (newPath != m_PathLabel.text))
            {
                newPath = SanitizePath(newPath);
            }

            m_Graph.path = newPath;
            m_PathLabel.text = FormatPath(newPath);
            m_EditPathCancelled = false;
        }

        private static string FormatPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return "-";
            return path;
        }

        private static string SanitizePath(string path)
        {
            var splitString = path.Split('/');
            List<string> newString = new List<string>();
            foreach(string s in splitString)
            {
                var str = s.Trim();
                if (!string.IsNullOrEmpty(str))
                {
                    newString.Add(str);
                }
            }

            return string.Join('/', newString.ToArray());
        }

        private void MoveItemRequested(Blackboard blackboard, int newIndex, VisualElement visualElement)
        {
            var property = visualElement.userData as IGeometryProperty;
            if (property == null)
                return;
            m_Graph.owner.RegisterCompleteObjectUndo("Move Property");
            m_Graph.MoveGeometryProperty(property, newIndex);
        }

        private void AddItemRequested(Blackboard blackboard)
        {
            var gm = new GenericMenu();
            gm.AddItem(new GUIContent("Vector1"), false, () => AddProperty(new Vector1GeometryProperty(), true));
            gm.AddItem(new GUIContent("Vector2"), false, () => AddProperty(new Vector2GeometryProperty(), true));
            gm.AddItem(new GUIContent("Vector3"), false, () => AddProperty(new Vector3GeometryProperty(), true));
            gm.AddItem(new GUIContent("Vector4"), false, () => AddProperty(new Vector4GeometryProperty(), true));
            //gm.AddItem(new GUIContent("Color"), false, () => AddProperty(new ColorGeometryProperty(), true));
            gm.AddItem(new GUIContent("Texture"), false, () => AddProperty(new TextureGeometryProperty(), true));
            //gm.AddItem(new GUIContent("Cubemap"), false, () => AddProperty(new CubemapGeometryProperty(), true));
            //gm.AddItem(new GUIContent("Boolean"), false, () => AddProperty(new BooleanGeometryProperty(), true));

            gm.ShowAsContext();
        }

        private void EditTextRequested(Blackboard blackboard, VisualElement visualElement, string newText)
        {
            var field = (BlackboardField)visualElement;
            var property = (IGeometryProperty)field.userData;
            if(!string.IsNullOrEmpty(newText) && newText != property.displayName)
            {
                m_Graph.owner.RegisterCompleteObjectUndo("Edit Property Name");
                newText = m_Graph.SanitizePropertyName(newText, property.guid);
                property.displayName = newText;
                field.text = newText;
                DirtyNodes();
            }
        }


        public void HandleGraphChanges()
        {
            foreach(var propertyGuid in m_Graph.removedProperties)
            {
                BlackboardRow row;
                if(m_PropertyRows.TryGetValue(propertyGuid, out row))
                {
                    row.RemoveFromHierarchy();
                    m_PropertyRows.Remove(propertyGuid);
                }
            }

            foreach (var property in m_Graph.addedProperties)
                AddProperty(property, index: m_Graph.GetGeometryPropertyIndex(property));

            if (m_Graph.movedProperties.Any())
            {
                foreach (var row in m_PropertyRows.Values)
                    row.RemoveFromHierarchy();

                foreach (var property in m_Graph.properties)
                    m_Section.Add(m_PropertyRows[property.guid]);
            }
        }

        private void AddProperty(IGeometryProperty property, bool create = false, int index = -1)
        {
            if (m_PropertyRows.ContainsKey(property.guid))
                return;

            if (create)
                property.displayName = m_Graph.SanitizePropertyName(property.displayName);

            var field = new BlackboardField(m_ExposedIcon, property.displayName, property.propertyType.ToString()) { userData = property };
            var row = new BlackboardRow(field, new BlackboardFieldPropertyView(m_Graph, property));
            row.userData = property;
            if (index < 0)
                index = m_PropertyRows.Count;
            if (index == m_PropertyRows.Count)
                m_Section.Add(row);
            else
                m_Section.Insert(index, row);
            m_PropertyRows[property.guid] = row;

            if (create)
            {
                row.expanded = true;
                m_Graph.owner.RegisterCompleteObjectUndo("Create Property");
                m_Graph.AddGeometryProperty(property);
                field.OpenTextEditor();
            }
        }

        private void DirtyNodes()
        {
            foreach(var node in m_Graph.GetNodes<PropertyNode>())
            {
                node.OnEnable();
                node.Dirty(ModificationScope.Node);
            }
        }
    }
}
