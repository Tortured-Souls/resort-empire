using System.Linq;
using ResortEmpire.NPC;
using ResortEmpire.World;
using UnityEditor;
using UnityEngine;

namespace ResortEmpire.EditorTools
{
    /// <summary>
    /// Building inspector with visitor pivot tools: adds pivots as child transforms, compares them with
    /// the capacity of every level and draws a draggable handle with a preview of the guest on each pivot.
    /// </summary>
    [CustomEditor(typeof(Building), true), CanEditMultipleObjects]
    public sealed class BuildingEditor : UnityEditor.Editor
    {
        private const string AddPivotUndo = "Add Visitor Pivot";
        private static readonly Color GhostTint = new(1f, 1f, 1f, 0.6f);
        private static readonly Color HandleColor = new(1f, 0.55f, 0.1f, 1f);

        private static CasualTourist _guest;
        private static bool _guestSearched;

        private SerializedProperty _pivots;

        private void OnEnable()
        {
            _pivots = serializedObject.FindProperty("pivots");
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            if (targets.Length != 1) return;

            var building = (Building)target;
            serializedObject.Update();
            EditorGUILayout.Space();

            int pivotCount = 0;
            for (int i = 0; i < _pivots.arraySize; i++)
            {
                if (_pivots.GetArrayElementAtIndex(i).objectReferenceValue != null) pivotCount++;
            }

            int[] capacities = Enumerable.Range(1, Building.MaxLevel).Select(building.CapacityAt).ToArray();
            string summary = $"{pivotCount} pivots, capacity by level {string.Join(" / ", capacities)}.";
            if (pivotCount < capacities.Max()) summary += " Guests in slots without a pivot stay inside, hidden.";
            if (pivotCount < _pivots.arraySize) summary += " Empty entries count as slots without a pivot.";
            summary += "\nDrag the orange handles in the Scene view; each pivot marks the guest's feet.";
            EditorGUILayout.HelpBox(summary, pivotCount == 0 ? MessageType.Warning : MessageType.Info);

            // Children cannot be added to a prefab asset from the Project window, only in Prefab Mode or a scene.
            bool isAsset = EditorUtility.IsPersistent(building);
            using (new EditorGUI.DisabledScope(isAsset))
            {
                if (GUILayout.Button(isAsset ? "Open the prefab to add pivots" : "Add Pivot")) AddPivot(building);
            }
        }

        // Called once per selected building with target set to it; reads the target directly because
        // serializedObject must not be used here.
        private void OnSceneGUI()
        {
            var building = (Building)target;
            CasualTourist guest = Guest;
            Sprite sprite = guest == null ? null : building.StandingOnly ? guest.StandingSprite : guest.SeatedSprite;
            float guestScale = guest == null ? 1f : guest.transform.localScale.x;

            for (int i = 0; i < building.Pivots.Count; i++)
            {
                Transform pivot = building.Pivots[i];
                if (pivot == null) continue;

                Vector3 position = pivot.position;
                DrawGhost(sprite, position, guestScale);

                float size = HandleUtility.GetHandleSize(position) * 0.04f;
                Handles.color = HandleColor;
                EditorGUI.BeginChangeCheck();
                Vector3 moved = Handles.FreeMoveHandle(position, size, Vector3.zero, Handles.DotHandleCap);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(pivot, "Move Visitor Pivot");
                    pivot.position = new Vector3(moved.x, moved.y, position.z);
                }

                Handles.Label(position + new Vector3(size, size * 4f, 0f), (i + 1).ToString(), EditorStyles.boldLabel);
            }
        }

        private void AddPivot(Building building)
        {
            int index = _pivots.arraySize;
            var previous = index > 0 ? _pivots.GetArrayElementAtIndex(index - 1).objectReferenceValue as Transform : null;
            Vector3 localPosition = previous != null
                ? previous.localPosition + Vector3.right * 0.2f
                : building.transform.InverseTransformPoint(building.EntrancePosition) + Vector3.up * 0.3f;

            var pivot = new GameObject($"Pivot {index + 1}").transform;
            Undo.RegisterCreatedObjectUndo(pivot.gameObject, AddPivotUndo);
            Undo.SetTransformParent(pivot, building.transform, AddPivotUndo);
            pivot.localPosition = localPosition;
            pivot.localRotation = Quaternion.identity;
            pivot.localScale = Vector3.one;

            _pivots.arraySize = index + 1;
            _pivots.GetArrayElementAtIndex(index).objectReferenceValue = pivot;
            serializedObject.ApplyModifiedProperties();
        }

        // Guest prefab whose sprites preview the pivots; looked up once per domain reload.
        private static CasualTourist Guest
        {
            get
            {
                if (_guestSearched) return _guest;
                _guestSearched = true;
                foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }))
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                    if (prefab != null && prefab.TryGetComponent(out _guest)) break;
                }

                return _guest;
            }
        }

        // Draws the guest sprite with its own pivot on the visitor pivot, as the guest will show in game.
        private static void DrawGhost(Sprite sprite, Vector3 feet, float scale)
        {
            if (sprite == null || Event.current.type != EventType.Repaint) return;

            Rect rect = sprite.rect;
            float unit = scale / sprite.pixelsPerUnit;
            Vector3 min = feet - (Vector3)(sprite.pivot * unit);
            Vector3 max = min + new Vector3(rect.width, rect.height) * unit;
            Vector2 topLeft = HandleUtility.WorldToGUIPoint(new Vector3(min.x, max.y, feet.z));
            Vector2 bottomRight = HandleUtility.WorldToGUIPoint(new Vector3(max.x, min.y, feet.z));

            Texture texture = sprite.texture;
            var uv = new Rect(rect.x / texture.width, rect.y / texture.height, rect.width / texture.width, rect.height / texture.height);

            Handles.BeginGUI();
            Color previousColor = GUI.color;
            GUI.color = GhostTint;
            GUI.DrawTextureWithTexCoords(Rect.MinMaxRect(topLeft.x, topLeft.y, bottomRight.x, bottomRight.y), texture, uv);
            GUI.color = previousColor;
            Handles.EndGUI();
        }
    }
}
