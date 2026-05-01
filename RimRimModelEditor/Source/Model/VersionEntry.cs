using System;

namespace D4egon.RimRimModelEditor.Model
{
    /// <summary>
    /// A named snapshot stored in the history sidebar.
    /// </summary>
    public class VersionEntry
    {
        public string Label;
        public string Timestamp;
        public EditorState Snapshot;

        public VersionEntry(string label, EditorState snapshot)
        {
            Label = label;
            Timestamp = DateTime.Now.ToString("HH:mm:ss");
            Snapshot = snapshot.Clone();
        }

        public override string ToString() => $"[{Timestamp}] {Label}";
    }
}
