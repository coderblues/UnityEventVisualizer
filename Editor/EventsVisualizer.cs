using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using System;
using UnityEditor.Callbacks;
using System.Linq;

namespace EventVisualizer.Base
{
    public static class EventsFinder
    {
        public static List<EventCall> FindAllEvents(GameObject[] roots, bool searchHierarchy = true)
        {
            HashSet<EventCall> calls = new HashSet<EventCall>();
			foreach (var type in ComponentsThatCanHaveUnityEvent)
			{
				if(type.IsGenericTypeDefinition)
				{	
					// skip the non-concrete types
					continue;
				}

				// find all the unity objects in the decendents
				HashSet<UnityEngine.Object> unityComponents = new HashSet<UnityEngine.Object>();
				if(roots != null && roots.Length > 0)
				{
					foreach(var root in roots)
					{
						if(root != null)
						{
							if(searchHierarchy)
							{
								unityComponents.UnionWith(root.GetComponentsInChildren(type));
							}
							else
							{
								unityComponents.Add(root.GetComponent(type));
							}
						}
					}
				}
				else 
				{
					unityComponents = new HashSet<UnityEngine.Object>(GameObject.FindObjectsOfType(type));
				}

				foreach (UnityEngine.Object caller in unityComponents) {
					Component comp = caller as Component;
					if(comp != null) {
						// find all the Event notifications originating
						// from the unity Component
						ExtractDefaultEventTriggers(calls, comp);
						ExtractEvents(calls, comp);
					}
				}
			}
			return calls.ToList();
        }

		private static void ExtractEvents(HashSet<EventCall> calls, Component caller)
        {
            SerializedProperty iterator = new SerializedObject(caller).GetIterator();
            iterator.Next(true);
			RecursivelyExtractEvents(calls, caller, iterator, 0);
        }

		private static bool RecursivelyExtractEvents(HashSet<EventCall> calls, Component caller, SerializedProperty iterator, int level) {
			bool hasData = true;

			do {
				SerializedProperty persistentCalls = iterator.FindPropertyRelative("m_PersistentCalls.m_Calls");
				bool isUnityEvent = persistentCalls != null;
				if (isUnityEvent && persistentCalls.arraySize > 0) {
					UnityEventBase unityEvent = SerializedPropertyHelper.GetTargetObjectOfProperty<UnityEventBase>(iterator);
					AddEventCalls(calls, caller, unityEvent, iterator.displayName, iterator.propertyPath);
				}
				hasData = iterator.Next(!isUnityEvent);
				if (hasData) {
					if (iterator.depth < level) return hasData;
					else if (iterator.depth > level) hasData = RecursivelyExtractEvents(calls, caller, iterator, iterator.depth);
				}
			}
			while (hasData);
			return false;
		}

        private static void ExtractDefaultEventTriggers(HashSet<EventCall> calls, Component caller)
        {
            EventTrigger eventTrigger = caller as EventTrigger;
            if (eventTrigger != null)
            {
                foreach (EventTrigger.Entry trigger in eventTrigger.triggers)
                {
					string name = trigger.eventID.ToString();
					AddEventCalls(calls, caller, trigger.callback, name, name);
                }
            }
        }

		private static void AddEventCalls(HashSet<EventCall> calls, Component caller, UnityEventBase unityEvent, string eventShortName, string eventFullName) {
			for (int i = 0; i < unityEvent.GetPersistentEventCount(); i++) {
				string methodName = unityEvent.GetPersistentMethodName(i);
				UnityEngine.Object receiver = unityEvent.GetPersistentTarget(i);

				if (receiver != null && methodName != null && methodName != "") {
					calls.Add(new EventCall(caller, receiver, eventShortName, eventFullName, methodName, unityEvent));
				}
			}
		}
		
		public static bool NeedsGraphRefresh = false;
		
		private static HashSet<Type> ComponentsThatCanHaveUnityEvent = new HashSet<Type>();

		[DidReloadScripts, InitializeOnLoadMethod]
		static void RefreshTypesThatCanHoldUnityEvents() {
			var sw = System.Diagnostics.Stopwatch.StartNew();

			Dictionary<Type, bool> tmpSearchedTypes = new Dictionary<Type, bool>();
			var types = TypesHelper.GetTypesMatching<Component>();
			foreach (var obj in types) {
				if (TypesHelper.RecursivelyFindFieldsOfType<UnityEventBase>(obj, tmpSearchedTypes)) {
					ComponentsThatCanHaveUnityEvent.Add(obj);
				}
			}
			// TmpSearchedTypes.Clear();
			
			Debug.Log("UnityEventVisualizer Updated Components that can have UnityEvents (" + ComponentsThatCanHaveUnityEvent.Count + "). Milliseconds: " + sw.Elapsed.TotalMilliseconds);
		}

	}
}