using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace Editor.CustomEditorTools
{
    /// <summary>
    /// Popup window for editing a list of GameObjects (transform + active state).
    /// </summary>
    public class GameObjectEditPopup : EditorWindow
    {
        // --- Static references (shared across instance) ---
        private static List<GameObject> _selectedObjects;
        private static ChangeHistoryManager _history;
        private static GameObjectEditController _editController;

        // --- Editable fields ---
        private Vector3 _position, _rotation, _scale;
        private bool _isActive;
        private bool _changesPending;

        // --- UI Toggles ---
        private bool _showTransform = true;
        private bool _showHistory   = true;
        private Vector2 _scrollPosition;

        // --- Local convenience flags for Undo/Redo ---
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
            _history         = new ChangeHistoryManager();
            _editController  = new GameObjectEditController(_history);

            // Position the popup near the center
            Rect main = EditorGUIUtility.GetMainWindowPosition();
            float centerX = main.x + (main.width - 500) / 2;
            float centerY = main.y + (main.height - 450) / 2;
            window.position = new Rect(centerX, centerY, 500, 450);

            window.InitializeTransformValues();
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
            _position = firstTransform.position;
            _rotation = firstTransform.eulerAngles;
            _scale    = firstTransform.localScale;
            _isActive = _selectedObjects[0].activeSelf;

            _changesPending = false;
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
            GUILayout.Label($"Editing {_selectedObjects.Count} GameObjects", EditorStyles.boldLabel);
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
                    _scale    = newScl;
                }
            }

            GUILayout.Space(10);
            GUILayout.BeginHorizontal();

            // --- Undo Button ---
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
            GUI.enabled = true;

            // --- Redo Button ---
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
            GUI.enabled = true;

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
            _scale    = _selectedObjects[0].transform.localScale;
            _isActive = _selectedObjects[0].activeSelf;

            Repaint();
        }
    }
}
