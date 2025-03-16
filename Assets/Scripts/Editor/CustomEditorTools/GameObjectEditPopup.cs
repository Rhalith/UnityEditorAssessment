using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Editor.CustomEditorTools
{
    /// <summary>
    /// Popup window for editing a list of GameObjects (transform + active state + batch component ops).
    /// Now uses a drop-down to pick from all available component types.
    /// </summary>
    public class GameObjectEditPopup : EditorWindow
    {
        private static List<GameObject> _selectedObjects;
        private static ChangeHistoryManager _history;
        private static GameObjectEditController _editController;

        private Vector3 _position, _rotation, _scale;
        private bool _isActive;
        private bool _changesPending;

        private bool _showTransform = true;
        private bool _showHistory = true;
        private Vector2 _scrollPosition;

        private static List<Type> _allComponentTypes;
        private static string[] _allComponentTypeNames;
        private int _selectedComponentIndex;

        private bool _canUndo => _history?.CanUndo ?? false;
        private bool _canRedo => _history?.CanRedo ?? false;

        /// <summary>
        /// Opens the popup for editing the given list of GameObjects.
        /// </summary>
        public static void Open(List<GameObject> objects)
        {
            if (objects == null || objects.Count == 0) return;

            GameObjectEditPopup window = GetWindow<GameObjectEditPopup>("Edit GameObjects", true);

            // Initialize static references
            _selectedObjects = new List<GameObject>(objects);
            _history = new ChangeHistoryManager();
            _editController = new GameObjectEditController(_history);

            // Position the popup near the center
            Rect main = EditorGUIUtility.GetMainWindowPosition();
            float centerX = main.x + (main.width - 550) / 2;
            float centerY = main.y + (main.height - 600) / 2;
            window.position = new Rect(centerX, centerY, 550, 600);

            window.InitializeTransformValues();
            window.InitializeComponentDropdown();

            Undo.undoRedoPerformed += window.OnUnityUndoRedo;
            window.Show();
        }

        private void OnDestroy()
        {
            Undo.undoRedoPerformed -= OnUnityUndoRedo;
        }

        private void InitializeTransformValues()
        {
            if (_selectedObjects.Count == 0) return;

            // Initialize from the first object
            Transform firstTransform = _selectedObjects[0].transform;
            _position = firstTransform.localPosition;
            _rotation = firstTransform.localEulerAngles;
            _scale = firstTransform.localScale;
            _isActive = _selectedObjects[0].activeSelf;

            _changesPending = false;
        }

        /// <summary>
        /// Builds a list of all non-abstract, non-generic Component types in all assemblies,
        /// sorted by FullName for convenience.
        /// </summary>
        private void InitializeComponentDropdown()
        {
            if (_allComponentTypes != null && _allComponentTypes.Count > 0) return;

            _allComponentTypes = new List<Type>();

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try
                {
                    types = asm.GetTypes();
                }
                catch
                {
                    continue;
                }

                foreach (var t in types)
                {
                    if (t.IsSubclassOf(typeof(Component)) && !t.IsAbstract && !t.IsGenericType)
                    {
                        _allComponentTypes.Add(t);
                    }
                }
            }

            // Sort for a consistent order
            _allComponentTypes = _allComponentTypes.OrderBy(t => t.FullName).ToList();

            // Create array of names to display
            _allComponentTypeNames = _allComponentTypes.Select(t => t.FullName).ToArray();

            // Default to the first item in the list
            _selectedComponentIndex = 0;
        }

        private void OnGUI()
        {
            // If objects are missing, close
            if (_selectedObjects == null || _selectedObjects.Count == 0)
            {
                Close();
                return;
            }

            GUILayout.Space(10);
            string selectedObjectNames = string.Join(", ", _selectedObjects.Select(gameObject => gameObject.name).ToArray());
            GUILayout.Label($"Editing {_selectedObjects.Count} GameObjects ({selectedObjectNames})", EditorStyles.boldLabel);
            GUILayout.Space(10);

            // --- Active State Toggle ---
            bool newActiveState = EditorGUILayout.Toggle("Active", _isActive);
            if (newActiveState != _isActive)
            {
                _editController.ToggleActive(_selectedObjects, newActiveState);
                _isActive = newActiveState;
            }

            GUILayout.Space(10);

            // --- Transform Properties (Foldout) ---
            _showTransform = EditorGUILayout.Foldout(_showTransform, "Transform Properties", true);
            if (_showTransform)
            {
                GUILayout.BeginVertical("box");
                Vector3 newPos = EditorGUILayout.Vector3Field("Position", _position);
                Vector3 newRot = EditorGUILayout.Vector3Field("Rotation", _rotation);
                Vector3 newScl = EditorGUILayout.Vector3Field("Scale", _scale);
                GUILayout.EndVertical();

                if (newPos != _position || newRot != _rotation || newScl != _scale)
                {
                    _changesPending = true;
                    _position = newPos;
                    _rotation = newRot;
                    _scale = newScl;
                }
            }

            GUILayout.Space(10);

            // --- Undo/Redo Buttons ---
            GUILayout.BeginHorizontal();
            GUI.enabled = _canUndo;
            if (GUILayout.Button("Undo", GUILayout.Height(30), GUILayout.Width(100)))
            {
                EditorApplication.delayCall += () =>
                {
                    string undone = _history.Undo();
                    if (!string.IsNullOrEmpty(undone))
                    {
                        Undo.PerformUndo();
                        Repaint();
                    }
                };
            }

            GUI.enabled = _canRedo;
            if (GUILayout.Button("Redo", GUILayout.Height(30), GUILayout.Width(100)))
            {
                EditorApplication.delayCall += () =>
                {
                    string redone = _history.Redo();
                    if (!string.IsNullOrEmpty(redone))
                    {
                        Undo.PerformRedo();
                        Repaint();
                    }
                };
            }
            
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            // --- Apply Button ---
            GUI.enabled = _changesPending;
            if (GUILayout.Button("Apply Changes", GUILayout.Height(35)))
            {
                _editController.ApplyTransformChanges(_selectedObjects, _position, _rotation, _scale);
                _changesPending = false;
            }

            GUI.enabled = true;

            // --- Component Operations ---
            GUILayout.Space(10);
            EditorGUILayout.LabelField("Component Operations", EditorStyles.boldLabel);

            if (_allComponentTypes == null || _allComponentTypes.Count == 0)
            {
                EditorGUILayout.HelpBox("No Component types found or reflection failed.", MessageType.Warning);
            }
            else
            {
                // Show a popup for the user to pick which component type to add/remove
                _selectedComponentIndex = EditorGUILayout.Popup("Component Type", _selectedComponentIndex, _allComponentTypeNames);

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Add Component", GUILayout.Height(25)))
                {
                    Type chosenType = _allComponentTypes[_selectedComponentIndex];
                    _editController.AddComponentToAll(_selectedObjects, chosenType);
                }

                if (GUILayout.Button("Remove Component", GUILayout.Height(25)))
                {
                    Type chosenType = _allComponentTypes[_selectedComponentIndex];
                    _editController.RemoveComponentFromAll(_selectedObjects, chosenType);
                }

                GUILayout.EndHorizontal();
            }

            GUILayout.Space(10);

            // --- Close Button ---
            if (GUILayout.Button("Close", GUILayout.Height(25)))
            {
                Close();
            }

            GUILayout.Space(10);

            // --- Change History (Foldout) ---
            _showHistory = EditorGUILayout.Foldout(_showHistory, "Change History", true);
            if (_showHistory)
            {
                GUILayout.BeginVertical("box", GUILayout.Height(150));
                _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(130));

                foreach (var change in _history.ChangeHistory)
                {
                    EditorGUILayout.LabelField("- " + change);
                }

                GUILayout.EndScrollView();
                GUILayout.EndVertical();
            }
        }

        private void OnUnityUndoRedo()
        {
            // If external Unity undo/redo was triggered, we simply refresh displayed transforms
            if (_selectedObjects.Count == 0) return;

            _position = _selectedObjects[0].transform.position;
            _rotation = _selectedObjects[0].transform.eulerAngles;
            _scale = _selectedObjects[0].transform.localScale;
            _isActive = _selectedObjects[0].activeSelf;

            Repaint();
        }
    }
}