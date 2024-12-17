using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;


namespace BXGeometryGraph
{
    class DummyChangeAction : IGraphDataAction
    {
        void OnDummyChangeAction(GraphData m_GraphData)
        {
        }

        public Action<GraphData> modifyGraphDataAction => OnDummyChangeAction;
    }

    struct GGControllerChangedEvent
    {
        public IGGControlledElement target;
        public GGController controller;
        public IGraphDataAction change;

        private bool m_PropagationStopped;
        void StopPropagation()
        {
            m_PropagationStopped = true;
        }

        public bool isPropagationStopped => m_PropagationStopped;
    }

    class GGControllerEvent
    {
        IGGControlledElement target = null;

        GGControllerEvent(IGGControlledElement controlledTarget)
        {
            target = controlledTarget;
        }
    }

    abstract class GGController
    {
        public bool m_DisableCalled = false;

        protected IGraphDataAction DummyChange = new DummyChangeAction();

        public virtual void OnDisable()
        {
            if (m_DisableCalled)
                Debug.LogError(GetType().Name + ".Disable called twice");

            m_DisableCalled = true;
            foreach (var element in allChildren)
            {
                UnityEngine.Profiling.Profiler.BeginSample(element.GetType().Name + ".OnDisable");
                element.OnDisable();
                UnityEngine.Profiling.Profiler.EndSample();
            }
        }

        internal void RegisterHandler(IGGControlledElement handler)
        {
            //Debug.Log("RegisterHandler  of " + handler.GetType().Name + " on " + GetType().Name );

            if (m_EventHandlers.Contains(handler))
                Debug.LogError("Handler registered twice");
            else
            {
                m_EventHandlers.Add(handler);

                NotifyEventHandler(handler, DummyChange);
            }
        }

        internal void UnregisterHandler(IGGControlledElement handler)
        {
            m_EventHandlers.Remove(handler);
        }

        protected void NotifyChange(IGraphDataAction changeAction)
        {
            var eventHandlers = m_EventHandlers.ToArray(); // Some notification may trigger Register/Unregister so duplicate the collection.

            foreach (var eventHandler in eventHandlers)
            {
                UnityEngine.Profiling.Profiler.BeginSample("NotifyChange:" + eventHandler.GetType().Name);
                NotifyEventHandler(eventHandler, changeAction);
                UnityEngine.Profiling.Profiler.EndSample();
            }
        }

        void NotifyEventHandler(IGGControlledElement eventHandler, IGraphDataAction changeAction)
        {
            GGControllerChangedEvent e = new GGControllerChangedEvent();
            e.controller = this;
            e.target = eventHandler;
            e.change = changeAction;
            eventHandler.OnControllerChanged(ref e);
            if (e.isPropagationStopped)
                return;
            if (eventHandler is VisualElement)
            {
                var element = eventHandler as VisualElement;
                eventHandler = element.GetFirstOfType<IGGControlledElement>();
                while (eventHandler != null)
                {
                    eventHandler.OnControllerChanged(ref e);
                    if (e.isPropagationStopped)
                        break;
                    eventHandler = (eventHandler as VisualElement).GetFirstAncestorOfType<IGGControlledElement>();
                }
            }
        }

        public void SendEvent(GGControllerEvent e)
        {
            var eventHandlers = m_EventHandlers.ToArray(); // Some notification may trigger Register/Unregister so duplicate the collection.

            foreach (var eventHandler in eventHandlers)
            {
                eventHandler.OnControllerEvent(e);
            }
        }

        public abstract void ApplyChanges();

        public virtual IEnumerable<GGController> allChildren
        {
            get { return Enumerable.Empty<GGController>(); }
        }

        protected List<IGGControlledElement> m_EventHandlers = new List<IGGControlledElement>();
    }

    abstract class GGController<T> : GGController
    {
        DataStore<GraphData> m_DataStore;
        protected DataStore<GraphData> DataStore => m_DataStore;

        protected GGController(T model, DataStore<GraphData> dataStore)
        {
            m_Model = model;
            m_DataStore = dataStore;
            DataStore.Subscribe += ModelChanged;
        }

        protected abstract void RequestModelChange(IGraphDataAction changeAction);

        protected abstract void ModelChanged(GraphData graphData, IGraphDataAction changeAction);

        T m_Model;
        public T Model => m_Model;

        // Cleanup delegate association before destruction
        public void Cleanup()
        {
            if (m_DataStore == null)
                return;
            DataStore.Subscribe -= ModelChanged;
            m_Model = default;
            m_DataStore = null;

        }
    }

    abstract class GGViewController<ModelType, ViewModelType> : GGController<ModelType>
    {
        protected GGViewController(ModelType model, ViewModelType viewModel, DataStore<GraphData> graphDataStore) : base(model, graphDataStore)
        {
            m_ViewModel = viewModel;
            try
            {
                // Need ViewModel to be initialized before we call ModelChanged() [as view model might need to update]
                ModelChanged(DataStore.State, DummyChange);
            }
            catch (Exception e)
            {
                Debug.Log("Failed to initialize View Controller of type: " + this.GetType() + " due to exception: " + e);
            }
        }

        // Holds data specific to the views this controller is responsible for
        ViewModelType m_ViewModel;
        public ViewModelType ViewModel => m_ViewModel;

        public override void ApplyChanges()
        {
            foreach (var controller in allChildren)
            {
                controller.ApplyChanges();
            }
        }

        public virtual void Dispose()
        {
            m_EventHandlers.Clear();
            m_ViewModel = default;
        }
    }
}
