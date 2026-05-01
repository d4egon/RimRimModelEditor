using System.Collections.Generic;

namespace D4egon.RimRimModelEditor
{
    /// <summary>
    /// Generic snapshot-based undo/redo stack with a configurable depth limit.
    /// T must be a deep-copyable record (use Clone() convention).
    /// </summary>
    public class UndoRedoStack<T> where T : class
    {
        private readonly LinkedList<T> _undoStack = new LinkedList<T>();
        private readonly LinkedList<T> _redoStack = new LinkedList<T>();
        private int _maxDepth;

        public bool CanUndo => _undoStack.Count > 0;
        public bool CanRedo => _redoStack.Count > 0;
        public int UndoCount => _undoStack.Count;
        public int RedoCount => _redoStack.Count;

        public UndoRedoStack(int maxDepth = 50)
        {
            _maxDepth = maxDepth;
        }

        /// <summary>Push a snapshot of the current state before making changes.</summary>
        public void Push(T snapshot)
        {
            _undoStack.AddLast(snapshot);
            _redoStack.Clear();

            while (_undoStack.Count > _maxDepth)
                _undoStack.RemoveFirst();
        }

        /// <summary>Undo: returns the previous snapshot, pushes current state to redo.</summary>
        public T Undo(T currentState)
        {
            if (!CanUndo) return currentState;
            var prev = _undoStack.Last.Value;
            _undoStack.RemoveLast();
            _redoStack.AddLast(currentState);
            return prev;
        }

        /// <summary>Redo: returns the next snapshot, pushes current state back to undo.</summary>
        public T Redo(T currentState)
        {
            if (!CanRedo) return currentState;
            var next = _redoStack.Last.Value;
            _redoStack.RemoveLast();
            _undoStack.AddLast(currentState);
            return next;
        }

        public void Clear()
        {
            _undoStack.Clear();
            _redoStack.Clear();
        }

        public void SetDepth(int depth)
        {
            _maxDepth = depth;
            while (_undoStack.Count > _maxDepth)
                _undoStack.RemoveFirst();
        }
    }
}
