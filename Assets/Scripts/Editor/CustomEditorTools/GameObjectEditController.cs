using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;

namespace Editor.CustomEditorTools
{
    /// <summary>
    /// Handles modifications to a set of GameObjects (transform changes, active state, etc.).
    /// </summary>
    public class GameObjectEditController
    {
        private readonly ChangeHistoryManager _history;

        public GameObjectEditController(ChangeHistoryManager history)
        {
            _history = history;
        }

        /// <summary>
        /// Applies position, rotation, scale changes as a single Undo group.
        /// </summary>
        public void ApplyTransformChanges(List<GameObject> objects, Vector3 position, Vector3 rotation, Vector3 scale)
        {
            if (objects == null || objects.Count == 0) return;

            Undo.SetCurrentGroupName("Modify Multiple Transforms");
            int undoGroup = Undo.GetCurrentGroup();

            foreach (GameObject obj in objects)
            {
                Undo.RecordObject(obj.transform, "Modify Transform");
                obj.transform.position = position;
                obj.transform.rotation = Quaternion.Euler(rotation);
                obj.transform.localScale = scale;
                EditorUtility.SetDirty(obj);
            }

            Undo.CollapseUndoOperations(undoGroup);

            string description = $"Applied Changes to {objects.Count} Objects (P:{position}, R:{rotation}, S:{scale})";
            _history.RecordChange(description);
        }

        /// <summary>
        /// Toggles the active state of the given objects as a single Undo group.
        /// </summary>
        public void ToggleActive(List<GameObject> objects, bool newState)
        {
            if (objects == null || objects.Count == 0) return;

            Undo.SetCurrentGroupName("Toggle Active State");
            int undoGroup = Undo.GetCurrentGroup();

            foreach (GameObject obj in objects)
            {
                Undo.RecordObject(obj, "Toggle Active");
                obj.SetActive(newState);
                EditorUtility.SetDirty(obj);
            }

            Undo.CollapseUndoOperations(undoGroup);

            string changeDesc = newState ? "Set Active" : "Set Inactive";
            _history.AddToHistory(changeDesc);
        }

        /// <summary>
        /// Adds a component (by System.Type) to each selected GameObject.
        /// </summary>
        public void AddComponentToAll(List<GameObject> objects, Type componentType)
        {
            if (objects == null || objects.Count == 0 || componentType == null) return;

            Undo.SetCurrentGroupName("Add Component");
            int undoGroup = Undo.GetCurrentGroup();

            foreach (GameObject gameObject in objects)
            {
                Undo.AddComponent(gameObject, componentType);
                EditorUtility.SetDirty(gameObject);
            }

            Undo.CollapseUndoOperations(undoGroup);

            _history.RecordChange($"Added Component '{componentType.FullName}' to {objects.Count} objects");
        }

        /// <summary>
        /// Removes a component (by System.Type) from each selected GameObject.
        /// Removes all instances if multiple exist.
        /// </summary>
        public void RemoveComponentFromAll(List<GameObject> objects, Type componentType)
        {
            if (objects == null || objects.Count == 0 || componentType == null) return;

            Undo.SetCurrentGroupName("Remove Component");
            int undoGroup = Undo.GetCurrentGroup();

            foreach (GameObject gameObject in objects)
            {
                var components = gameObject.GetComponents(componentType);
                foreach (var c in components)
                {
                    Undo.DestroyObjectImmediate(c);
                }

                EditorUtility.SetDirty(gameObject);
            }

            Undo.CollapseUndoOperations(undoGroup);

            _history.RecordChange($"Removed Component '{componentType.FullName}' from {objects.Count} objects");
        }
    }
}