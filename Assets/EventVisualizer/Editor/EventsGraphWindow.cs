﻿using UnityEditor;
using UnityEngine;

namespace EventVisualizer.Base
{
	public class EventsGraphWindow : EditorWindow
	{
		[SerializeField]
		private EventsGraph _graph;
		[SerializeField]
		private EventsGraphGUI _graphGUI;

		private const float kZoomMin = 0.1f;
		private const float kZoomMax = 1.0f;

		private Rect _zoomArea = new Rect(0.0f, 75.0f, 600.0f, 300.0f - 100.0f);
		private float _zoom = 1f;
		private Vector2 _zoomCoordsOrigin = Vector2.zero;

		private const float kBarHeight = 17;

		[MenuItem("Window/Events Graph editor")]
		static void ShowEditor()
		{
			EventsGraphWindow editor = EditorWindow.GetWindow<EventsGraphWindow>();
			editor.hideFlags = HideFlags.HideAndDontSave;
			editor.Initialize();
		}

		public void Initialize()
		{
			_graph = EventsGraph.Create();
			_graph.RebuildGraph();

			_graphGUI = _graph.GetEditor();
			_graphGUI.CenterGraph();

			EditorUtility.SetDirty(_graphGUI);
			EditorUtility.SetDirty(_graph);
		}

		void OnGUI()
		{
			var width = position.width;
			var height = position.height;
			_zoomArea = new Rect(0, 0, width, height);
			HandleEvents();

			if (_graphGUI != null)
			{
				Rect r = EditorZoomArea.Begin(_zoom, _zoomArea);
				// Main graph area
				_graphGUI.BeginGraphGUI(this, r);
				_graphGUI.OnGraphGUI();
				_graphGUI.EndGraphGUI();

				// Clear selection on background click
				var e = Event.current;
				if (e.type == EventType.MouseDown && e.clickCount == 1)
					_graphGUI.ClearSelection();


				EditorZoomArea.End();
			}


			// Status bar
			GUILayout.BeginArea(new Rect(0, 0, width, kBarHeight+5));
			string[] toolbarStrings = new string[] { "Update connections", "Clear" };
			int result = GUILayout.Toolbar(-1, toolbarStrings);
			if (result == 0)
			{
				RefreshGraphConnections();
			}
			else if(result == 1)
			{
				RebuildGraph();
			}
			GUILayout.EndArea();

		}

		private void Update()
		{
			if (EdgeTriggersTracker.HasData())
			{
				Repaint();
			}
		}

		public void OverrideSelection(int overrideIndex)
		{
			_graphGUI.SelectionOverride = overrideIndex;
		}

		public Vector2 ConvertScreenCoordsToZoomCoords(Vector2 screenCoords)
		{
			return (screenCoords - _zoomArea.TopLeft()) / _zoom + _zoomCoordsOrigin;
		}


		private void HandleEvents()
		{
			if (Event.current.type == EventType.ScrollWheel)
			{
				Vector2 screenCoordsMousePos = Event.current.mousePosition;
				Vector2 delta = Event.current.delta;
				Vector2 zoomCoordsMousePos = ConvertScreenCoordsToZoomCoords(screenCoordsMousePos);
				float zoomDelta = -delta.y / 150.0f;
				float oldZoom = _zoom;
				_zoom += zoomDelta;
				_zoom = Mathf.Clamp(_zoom, kZoomMin, kZoomMax);
				_zoomCoordsOrigin += (zoomCoordsMousePos - _zoomCoordsOrigin) - (oldZoom / _zoom) * (zoomCoordsMousePos - _zoomCoordsOrigin);

				Event.current.Use();
			}
		}

		void RebuildGraph()
		{
			if(_graph != null)
			{
				_graph.RebuildGraph();
			}
		}
		void RefreshGraphConnections()
		{
			if (_graph != null)
			{
				_graph.RefreshGraphConnections();
			}
		}
	}
}