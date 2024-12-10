using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXGeometryGraph
{
    [Serializable]
    public class StickyNoteData : JsonObject, IGroupItem, IRectInterface
    {
        [SerializeField]
        private string m_Title;

        public string title
        {
            get => m_Title;
            set => m_Title = value;
        }

        [SerializeField]
        private string m_Content;

        public string content
        {
            get => m_Content;
            set => m_Content = value;
        }

        [SerializeField]
        private int m_TextSize;

        public int textSize
        {
            get => m_TextSize;
            set => m_TextSize = value;
        }

        [SerializeField]
        private int m_Theme;

        public int theme
        {
            get => m_Theme;
            set => m_Theme = value;
        }

        [SerializeField]
        private Rect m_Position;

        public Rect position
        {
            get => m_Position;
            set => m_Position = value;
        }

        Rect IRectInterface.rect
        {
            get => position;
            set
            {
                position = value;
            }
        }

        [SerializeField]
        private JsonRef<GroupData> m_Group = null;

        public GroupData group
        {
            get => m_Group;
            set
            {
                if (m_Group == value)
                    return;

                m_Group = value;
            }
        }

        public StickyNoteData() : base() { }
        public StickyNoteData(string title, string content, Rect position)
        {
            m_Title = title;
            m_Position = position;
            m_Content = content;
        }
    }
}
