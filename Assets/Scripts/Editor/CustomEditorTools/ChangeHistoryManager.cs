using System.Collections.Generic;
using System.Collections;

namespace Editor.CustomEditorTools
{
    /// <summary>
    /// Manages a simple text history and Undo/Redo stacks for custom operations.
    /// </summary>
    public class ChangeHistoryManager
    {
        private readonly Stack<string> _undoStack = new();
        private readonly Stack<string> _redoStack = new();
        private readonly List<string> _changeHistory = new();

        public IReadOnlyList<string> ChangeHistory => _changeHistory;
        public bool CanUndo => _undoStack.Count > 0;
        public bool CanRedo => _redoStack.Count > 0;

        /// <summary>
        /// Adds a new change to the Undo stack and history, clearing the Redo stack.
        /// </summary>
        public void RecordChange(string description)
        {
            _undoStack.Push(description);
            _redoStack.Clear();
            _changeHistory.Add(description);
        }

        /// <summary>
        /// Adds an entry to the change history (without pushing to the Undo stack).
        /// </summary>
        public void AddToHistory(string record)
        {
            _changeHistory.Add(record);
        }

        /// <summary>
        /// Pops the Undo stack, pushes onto Redo, returns the undone description.
        /// </summary>
        public string Undo()
        {
            if (!CanUndo) return null;

            var lastChange = _undoStack.Pop();
            _redoStack.Push(lastChange);
            _changeHistory.Add($"Undo: {lastChange}");
            return lastChange;
        }

        /// <summary>
        /// Pops the Redo stack, pushes onto Undo, returns the redone description.
        /// </summary>
        public string Redo()
        {
            if (!CanRedo) return null;

            var lastRedo = _redoStack.Pop();
            _undoStack.Push(lastRedo);
            _changeHistory.Add($"Redo: {lastRedo}");
            return lastRedo;
        }
    }
}