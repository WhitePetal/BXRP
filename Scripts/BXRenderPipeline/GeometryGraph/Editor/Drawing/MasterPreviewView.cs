using System.Collections;
using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace BXGeometryGraph
{
    class MasterPreviewView
    {
        PreviewManager m_PreviewManager;
        GraphData m_Graph;

        PreviewRenderData m_PreviewRenderHandle;
        Image m_PreviewTextureView;

        public Image previewTextureView
        {
            get { return m_PreviewTextureView; }
        }

        Vector2 m_PreviewScrollPosition;
        ObjectField m_PreviewMeshPicker;

        Mesh m_PreviousMesh;

        bool m_RecalculateLayout;

        //ResizeBorderFrame m_PreviewResizeBorderFrame;

        //public ResizeBorderFrame previewResizeBorderFrame
        //{
        //    get { return m_PreviewResizeBorderFrame; }
        //}

        VisualElement m_Preview;
        Label m_Title;

        public VisualElement preview
        {
            get { return m_Preview; }
        }


    }
}
