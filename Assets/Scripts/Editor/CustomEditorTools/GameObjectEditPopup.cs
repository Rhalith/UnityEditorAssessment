using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Editor.CustomEditorTools
{
    public class GameObjectEditPopup : EditorWindow
    {
        private static List<GameObject> _selectedObjects = new();
        private Vector3 _position, _rotation, _scale;
        private readonly List<string> _changeHistory = new();
        private readonly Stack<string> _undoStack = new();
        private readonly Stack<string> _redoStack = new();
        private Vector2 _scrollPosition;
        private bool _changesPending;
        private bool _isActive;
        private bool _showTransform = true;
        private bool _showHistory = true;
        private bool _canUndo;
        private bool _canRedo;

        public static void Open(List<GameObject> objects)
        {
            if (objects == null || objects.Count == 0) return;

            GameObjectEditPopup window = GetWindow<GameObjectEditPopup>("Edit GameObjects", true);
            _selectedObjects = new List<GameObject>(objects);

            Rect main = EditorGUIUtility.GetMainWindowPosition();
            float centerX = main.x + (main.width - 500) / 2;
            float centerY = main.y + (main.height - 450) / 2;
            window.position = new Rect(centerX, centerY, 500, 450);

            window.InitializeTransformValues();
            Undo.undoRedoPerformed += window.OnUndoRedo;
            window.UpdateUndoRedoState();
            window.Show();
        }

        private void InitializeTransformValues()
        {
            if (_selectedObjects.Count == 0) return;

            Transform firstTransform = _selectedObjects[0].transform;
            _position = firstTransform.position;
            _rotation = firstTransform.eulerAngles;
            _scale = firstTransform.localScale;
            _isActive = _selectedObjects[0].activeSelf;

            _changeHistory.Clear();
            _changesPending = false;
            UpdateUndoRedoState();
        }

        private void OnGUI()
        {
            if (_selectedObjects == null || _selectedObjects.Count == 0)
            {
                Close();
                return;
            }

            GUILayout.Space(10);
            GUILayout.Label("Editing " + _selectedObjects.Count + " GameObjects", EditorStyles.boldLabel);
            GUILayout.Space(10);

            // **Active State Toggle**
            bool newActiveState = EditorGUILayout.Toggle("Active", _isActive);
            if (newActiveState != _isActive)
            {
                ToggleActiveState(newActiveState);
            }

            GUILayout.Space(10);

            // **Transform Properties - Collapsible**
            _showTransform = EditorGUILayout.Foldout(_showTransform, "Transform Properties", true);
            if (_showTransform)
            {
                GUILayout.BeginVertical("box");
                Vector3 newPosition = EditorGUILayout.Vector3Field("Position", _position);
                Vector3 newRotation = EditorGUILayout.Vector3Field("Rotation", _rotation);
                Vector3 newScale = EditorGUILayout.Vector3Field("Scale", _scale);
                GUILayout.EndVertical();

                if (newPosition != _position || newRotation != _rotation || newScale != _scale)
                {
                    _changesPending = true;
                    _position = newPosition;
                    _rotation = newRotation;
                    _scale = newScale;
                }
            }

            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            
            // **Undo Button**
            GUI.enabled = _canUndo;
            if (GUILayout.Button("Undo", GUILayout.Height(30), GUILayout.Width(100)))
            {
                EditorApplication.delayCall += () =>
                {
                    if (_undoStack.Count > 0)
                    {
                        string lastChange = _undoStack.Pop();
                        _redoStack.Push(lastChange);
                        _changeHistory.Add($"Undo: {lastChange}");
                        Undo.PerformUndo();
                    }

                    UpdateUndoRedoState();
                    Repaint();
                };
            }

            GUI.enabled = true;

            // **Redo Button**
            GUI.enabled = _canRedo;
            if (GUILayout.Button("Redo", GUILayout.Height(30), GUILayout.Width(100)))
            {
                EditorApplication.delayCall += () =>
                {
                    if (_redoStack.Count > 0)
                    {
                        string lastRedo = _redoStack.Pop();
                        _undoStack.Push(lastRedo);
                        _changeHistory.Add($"Redo: {lastRedo}");
                        Undo.PerformRedo();
                    }

                    UpdateUndoRedoState();
                    Repaint();
                };
            }

            GUI.enabled = true;


            GUILayout.EndHorizontal();
            GUILayout.Space(10);

            // **Apply Button**
            GUI.enabled = _changesPending;
            if (GUILayout.Button("Apply Changes", GUILayout.Height(35)))
            {
                ApplyChanges();
            }

            GUI.enabled = true;

            if (GUILayout.Button("Close", GUILayout.Height(25)))
            {
                Close();
            }

            GUILayout.Space(10);

            // **Change History - Collapsible**
            _showHistory = EditorGUILayout.Foldout(_showHistory, "Change History", true);
            if (_showHistory)
            {
                GUILayout.BeginVertical("box", GUILayout.Height(150));
                _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(130));

                foreach (var change in _changeHistory)
                {
                    EditorGUILayout.LabelField("- " + change);
                }

                GUILayout.EndScrollView();
                GUILayout.EndVertical();
            }
        }

        private void ApplyChanges()
        {
            if (_selectedObjects.Count == 0) return;

            Undo.SetCurrentGroupName("Modify Multiple Transforms");
            int undoGroup = Undo.GetCurrentGroup();

            foreach (GameObject obj in _selectedObjects)
            {
                Undo.RecordObject(obj.transform, "Modify Transform");
                obj.transform.position = _position;
                obj.transform.rotation = Quaternion.Euler(_rotation);
                obj.transform.localScale = _scale;
                EditorUtility.SetDirty(obj);
            }

            Undo.CollapseUndoOperations(undoGroup);

            string appliedChange =
                $"Applied Changes to {_selectedObjects.Count} Objects (P:{_position}, R:{_rotation}, S:{_scale})";
            _undoStack.Push(appliedChange); // 🔥 Push the change to Undo stack
            _redoStack.Clear(); // 🔥 Clear redo stack on new change
            _changeHistory.Add(appliedChange);
            _changesPending = false;

            UpdateUndoRedoState(); // 🔥 Update button states
        }


        private void ToggleActiveState(bool newState)
        {
            if (_selectedObjects.Count == 0) return;

            Undo.SetCurrentGroupName("Toggle Active State");
            int undoGroup = Undo.GetCurrentGroup();

            foreach (GameObject obj in _selectedObjects)
            {
                Undo.RecordObject(obj, "Toggle Active");
                obj.SetActive(newState);
                EditorUtility.SetDirty(obj);
            }

            Undo.CollapseUndoOperations(undoGroup);

            _isActive = newState;

            string activeStateChange = newState ? "Set Active" : "Set Inactive";
            _changeHistory.Add(activeStateChange);

            UpdateUndoRedoState();
            Repaint();
        }

        private void LogUndoRedoChange(string action)
        {
            if (_selectedObjects.Count == 0) return;

            _position = _selectedObjects[0].transform.position;
            _rotation = _selectedObjects[0].transform.eulerAngles;
            _scale = _selectedObjects[0].transform.localScale;
            _isActive = _selectedObjects[0].activeSelf;

            string logEntry = $"{action}: Position ({_position.x:F2}, {_position.y:F2}, {_position.z:F2}), " +
                              $"Rotation ({_rotation.x:F2}, {_rotation.y:F2}, {_rotation.z:F2}), " +
                              $"Scale ({_scale.x:F2}, {_scale.y:F2}, {_scale.z:F2})";

            _changeHistory.Add(logEntry);
        }

        private void OnUndoRedo()
        {
            if (_selectedObjects.Count == 0) return;

            _position = _selectedObjects[0].transform.position;
            _rotation = _selectedObjects[0].transform.eulerAngles;
            _scale = _selectedObjects[0].transform.localScale;
            _isActive = _selectedObjects[0].activeSelf;

            UpdateUndoRedoState();
            Repaint();
        }


        private void UpdateUndoRedoState()
        {
            _canUndo = _undoStack.Count > 0;
            _canRedo = _redoStack.Count > 0;
        }

        private void OnDestroy()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
        }
    }
}