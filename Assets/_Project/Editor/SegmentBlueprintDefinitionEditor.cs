using SphereCorridor.Corridor.Grid;
using UnityEditor;
using UnityEngine;

namespace SphereCorridor.Editor
{
    /// <summary>
    /// Presents the flattened serialized arrays as a readable matrix when a blueprint
    /// asset is selected. Raw arrays remain available for precise advanced editing.
    /// </summary>
    [CustomEditor(typeof(SegmentBlueprintDefinition))]
    public sealed class SegmentBlueprintDefinitionEditor : UnityEditor.Editor
    {
        private bool showRawMatrices;

        /// <summary>Draws dimensions first, then a compact longitudinal/lateral preview.</summary>
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawPropertiesExcluding(
                serializedObject,
                "m_Script",
                "floorCells",
                "contentCells",
                "nearBoundary",
                "farBoundary");
            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Grid Preview", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Rows progress along world X. Columns cross the corridor on world Z. " +
                ". = floor, G = gap, I = immune block, V = vulnerable target.",
                MessageType.Info);
            DrawMatrixPreview((SegmentBlueprintDefinition)target);

            EditorGUILayout.Space();
            showRawMatrices = EditorGUILayout.Foldout(showRawMatrices, "Raw Serialized Matrices", true);
            if (showRawMatrices)
            {
                serializedObject.Update();
                EditorGUILayout.PropertyField(serializedObject.FindProperty("floorCells"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("contentCells"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("nearBoundary"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("farBoundary"), true);
                serializedObject.ApplyModifiedProperties();
            }
        }

        /// <summary>Renders a non-editable cell map without relying on color alone.</summary>
        private static void DrawMatrixPreview(SegmentBlueprintDefinition blueprint)
        {
            if (blueprint == null || blueprint.FloorCellCount != blueprint.LengthCells * blueprint.WidthCells ||
                blueprint.ContentCellCount != blueprint.LengthCells * blueprint.WidthCells)
            {
                EditorGUILayout.LabelField("Matrix data is incomplete; run validation for details.");
                return;
            }

            EditorGUI.BeginDisabledGroup(true);
            for (int lengthIndex = 0; lengthIndex < blueprint.LengthCells; lengthIndex++)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label($"{lengthIndex:00}", GUILayout.Width(24f));
                for (int widthIndex = 0; widthIndex < blueprint.WidthCells; widthIndex++)
                {
                    string symbol = GetCellSymbol(blueprint, lengthIndex, widthIndex);
                    GUILayout.Button(symbol, GUILayout.Width(28f), GUILayout.Height(20f));
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUI.EndDisabledGroup();
        }

        /// <summary>Chooses an accessible text symbol for one composed cell.</summary>
        private static string GetCellSymbol(
            SegmentBlueprintDefinition blueprint,
            int lengthIndex,
            int widthIndex)
        {
            SegmentPlaceableDefinition content =
                blueprint.GetContentCell(lengthIndex, widthIndex).Definition;
            if (content != null)
            {
                if (content.StableId.Contains("immune"))
                {
                    return "I";
                }

                if (content.StableId.Contains("vulnerable"))
                {
                    return "V";
                }

                return "O";
            }

            FloorTileDefinition floor = blueprint.GetFloorCell(lengthIndex, widthIndex).Definition;
            return floor == null || floor.TraversalKind == FloorTraversalKind.Gap ? "G" : ".";
        }
    }
}
