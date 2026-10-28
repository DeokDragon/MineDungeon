using System;
using System.Collections.Generic;

// Manual-only recovery utility. No startup, reload, update, or Play callback registers this code.
// Do not invoke through MCP. Leftover trial sizes are harmless and may be left in place.
public static class TitleScreenAspectCleanup
{
    internal const int MaximumRemovals = 2;
    private const int MaximumListedSizes = 256;
    internal struct Size
    {
        public string Name;
        public int Width, Height;
        public bool Fixed;
    }
    internal interface IGroup
    {
        int BuiltinCount { get; }
        int Count { get; }
        Size At(int totalIndex);
        void RemoveAtTotalIndex(int totalIndex);
    }
    private struct Candidate { public int Index; public Size Size; }
    private static bool Same(Size a, Size b) => a.Name == b.Name && a.Width == b.Width && a.Height == b.Height && a.Fixed == b.Fixed;
    private static bool IsTrialName(string name) => name == "OREPLUME Trial 1440x1080" || name == "OREPLUME Trial 2560x1080";
    private static bool MatchesTrial(Size size) => size.Fixed && size.Height == 1080 &&
        ((size.Name == "OREPLUME Trial 1440x1080" && size.Width == 1440) ||
         (size.Name == "OREPLUME Trial 2560x1080" && size.Width == 2560));

    internal static bool TryCleanup(IGroup group, out string report)
    {
        int removed = 0;
        try
        {
            int builtin = group.BuiltinCount, initialCount = group.Count;
            if (builtin < 0 || initialCount < builtin || initialCount > MaximumListedSizes)
                throw new InvalidOperationException("Unexpected size counts; nothing removed.");
            var plan = new List<Candidate>();
            var names = new HashSet<string>(StringComparer.Ordinal);
            // Snapshot only the custom range. Never derive a total index from a custom count.
            for (int index = builtin; index < initialCount; index++)
            {
                var size = group.At(index);
                if (!IsTrialName(size.Name)) continue;
                if (!MatchesTrial(size) || !names.Add(size.Name))
                    throw new InvalidOperationException("Ambiguous trial identity; nothing removed.");
                plan.Add(new Candidate { Index = index, Size = size });
            }
            if (plan.Count > MaximumRemovals)
                throw new InvalidOperationException("Removal limit exceeded; nothing removed.");
            // Descending TOTAL indices keep the remaining snapshot indices valid.
            for (int attempt = plan.Count - 1; attempt >= 0; attempt--)
            {
                var candidate = plan[attempt];
                if (group.BuiltinCount != builtin || group.Count != initialCount - removed ||
                    candidate.Index < builtin || candidate.Index >= group.Count ||
                    !Same(group.At(candidate.Index), candidate.Size))
                    throw new InvalidOperationException("Size list changed; cleanup stopped.");
                int before = group.Count;
                group.RemoveAtTotalIndex(candidate.Index);
                if (group.Count != before - 1)
                    throw new InvalidOperationException("Deletion did not reduce the count; no retry.");
                for (int index = builtin; index < group.Count; index++)
                    if (Same(group.At(index), candidate.Size))
                        throw new InvalidOperationException("Wrong item removed; no further deletion.");
                removed++;
            }
            report = "Verified trial sizes removed: " + removed + ". Other sizes preserved.";
            return true;
        }
        catch (Exception error)
        {
            report = "Stopped after " + removed + " verified removal(s): " + error.Message;
            return false;
        }
    }

#if UNITY_EDITOR
    [UnityEditor.MenuItem("Tools/OREPLUME/Remove Verified Trial Aspect Sizes (Manual Only)")]
    private static void CleanupManually()
    {
        if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode ||
            UnityEditor.EditorApplication.isCompiling || UnityEditor.EditorApplication.isUpdating)
        {
            UnityEngine.Debug.LogWarning("Trial-size cleanup requires idle Edit mode. Nothing changed.");
            return;
        }
        string report;
        try
        {
            var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Instance;
            var type = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameViewSizes", true);
            var instance = type.BaseType.GetProperty("instance", flags).GetValue(null, null);
            var group = type.GetProperty("currentGroup", flags).GetValue(instance, null);
            if (TryCleanup(new UnityGroup(group), out report)) UnityEngine.Debug.Log(report);
            else UnityEngine.Debug.LogError(report);
        }
        catch (Exception error) { UnityEngine.Debug.LogError("Cleanup stopped without retry: " + error.Message); }
    }
    private sealed class UnityGroup : IGroup
    {
        private readonly object group;
        private readonly Type type;
        private const System.Reflection.BindingFlags Flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        public UnityGroup(object value) { group = value ?? throw new ArgumentNullException(nameof(value)); type = group.GetType(); }
        private object Call(string name, params object[] args) => type.GetMethod(name, Flags).Invoke(group, args);
        public int BuiltinCount => (int)Call("GetBuiltinCount");
        public int Count => (int)Call("GetTotalCount");
        public Size At(int totalIndex)
        {
            var size = Call("GetGameViewSize", totalIndex); var sizeType = size.GetType();
            return new Size
            {
                Name = (string)sizeType.GetProperty("baseText", Flags).GetValue(size, null),
                Width = (int)sizeType.GetProperty("width", Flags).GetValue(size, null),
                Height = (int)sizeType.GetProperty("height", Flags).GetValue(size, null),
                Fixed = sizeType.GetProperty("sizeType", Flags).GetValue(size, null).ToString() == "FixedResolution"
            };
        }
        public void RemoveAtTotalIndex(int totalIndex)
        {
            if (totalIndex < BuiltinCount || totalIndex >= Count)
                throw new ArgumentOutOfRangeException(nameof(totalIndex));
            Call("RemoveCustomSize", totalIndex);
        }
    }
#endif
}
