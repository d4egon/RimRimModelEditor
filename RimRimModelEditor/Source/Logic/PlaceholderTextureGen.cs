using UnityEngine;
using Object = UnityEngine.Object;
using Verse;

namespace D4egon.RimRimModelEditor.Logic
{
    public static class PlaceholderTextureGen
    {
        private static Texture2D _checkerPlaceholder;
        private static Texture2D _missingPlaceholder;

        public static Texture2D GetChecker(int size = 64)
        {
            if (_checkerPlaceholder != null) return _checkerPlaceholder;

            // We change this to a single transparent texture
            _checkerPlaceholder = new Texture2D(size, size, TextureFormat.RGBA32, false);
            _checkerPlaceholder.filterMode = FilterMode.Point;
    
            // Fill with a neutral "Studio Grey" or Transparent
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                _checkerPlaceholder.SetPixel(x, y, new Color(0f, 0f, 0f, 0f)); 

            _checkerPlaceholder.Apply();
            return _checkerPlaceholder;
        }

        public static Texture2D GetMissing(int size = 64)
        {
            if (_missingPlaceholder != null) return _missingPlaceholder;

            _missingPlaceholder = new Texture2D(size, size, TextureFormat.RGBA32, false);
            _missingPlaceholder.filterMode = FilterMode.Point;

            int half = size / 2;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool bright = (x < half) == (y < half);
                    _missingPlaceholder.SetPixel(x, y, bright ? Color.magenta : Color.black);
                }
            }
            _missingPlaceholder.Apply();
            return _missingPlaceholder;
        }

        // --- SURGICAL ADDITION: MEMORY MANAGEMENT ---
        public static void Cleanup()
        {
            if (_checkerPlaceholder != null) { Object.Destroy(_checkerPlaceholder); _checkerPlaceholder = null; }
            if (_missingPlaceholder != null) { Object.Destroy(_missingPlaceholder); _missingPlaceholder = null; }
        }
    }
}