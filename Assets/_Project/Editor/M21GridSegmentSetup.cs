using System;
using System.IO;
using SphereCorridor.Combat;
using SphereCorridor.Corridor;
using SphereCorridor.Corridor.Grid;
using SphereCorridor.Foundation;
using SphereCorridor.Movement;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SphereCorridor.Editor
{
    /// <summary>
    /// Generates the M2.1 layered grid blueprint and assigns it to the combat lab
    /// without requiring a Tools-menu installation action.
    /// </summary>
    [InitializeOnLoad]
    public static class M21GridSegmentSetup
    {
        private const int CurrentRevision = 4;
        private const int LengthCells = 20;
        private const int WidthCells = 6;
        private const float CellSize = 2f;
        private const string LogSubsystem = "M2.1Setup";
        private const string LabRootName = "[M1 Movement Lab]";
        private const string SessionKey = "SphereCorridor.M2.1.Revision4.AutomaticSetupAttempted";
        private const string SegmentDefinitionPath = "Assets/_Project/Definitions/BasicMovementSegment.asset";
        private const string SegmentDefinitionsDirectory = "Assets/_Project/Definitions/Segments";
        private const string GridPrefabDirectory = "Assets/_Project/Prefabs/Corridor/Grid";
        private const string GridPrefabPath = GridPrefabDirectory + "/GridSegment.prefab";
        private const string FixedGeometryPrefabPath = "Assets/_Project/Prefabs/Corridor/BasicMovementSegment.prefab";
        private const string FloorDefinitionPath = SegmentDefinitionsDirectory + "/StandardFloor.asset";
        private const string GapDefinitionPath = SegmentDefinitionsDirectory + "/GapFloor.asset";
        private const string WallDefinitionPath = SegmentDefinitionsDirectory + "/StructuralWall.asset";
        private const string ImmunePlaceablePath = SegmentDefinitionsDirectory + "/PlasmaImmuneBlock.asset";
        private const string VulnerablePlaceablePath = SegmentDefinitionsDirectory + "/PlasmaVulnerableTarget.asset";
        private const string BlueprintPath = SegmentDefinitionsDirectory + "/TrainingCombatGrid.asset";
        private const string FloorMaterialPath = "Assets/_Project/Art/Materials/M1_Floor.mat";
        private const string WallMaterialPath = "Assets/_Project/Art/Materials/M1_Wall.mat";
        private const string ImmuneMaterialPath = "Assets/_Project/Art/Materials/M2_PlasmaImmune.mat";
        private const string VulnerableMaterialPath = "Assets/_Project/Art/Materials/M2_PlasmaVulnerable.mat";
        private const string ImmuneHealthPath = "Assets/_Project/Definitions/Combat/PlasmaImmuneObstacleHealth.asset";
        private const string VulnerableHealthPath = "Assets/_Project/Definitions/Combat/PlasmaVulnerableTargetHealth.asset";

        /// <summary>Schedules setup after compilation and asset import settle.</summary>
        static M21GridSegmentSetup()
        {
            EditorApplication.delayCall += ApplyAutomaticallyIfNeeded;
        }

        /// <summary>
        /// Attempts the idempotent grid-generation pass once per Editor session. The
        /// session guard prevents repeated asset imports when no revision is required.
        /// </summary>
        private static void ApplyAutomaticallyIfNeeded()
        {
            if (SessionState.GetBool(SessionKey, false))
            {
                return;
            }

            if (EditorApplication.isCompiling || EditorApplication.isUpdating ||
                EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.delayCall += ApplyAutomaticallyIfNeeded;
                return;
            }

            SessionState.SetBool(SessionKey, true);
            ApplyGridSegmentSetup();
        }

        /// <summary>Creates definition assets and replaces generated preview instances.</summary>
        [MenuItem("Tools/Sphere Corridor/M2.1/Apply Grid Segment Setup")]
        public static void ApplyGridSegmentSetup()
        {
            // M2 is idempotent and supplies the combat definitions referenced by the
            // initial placeable definitions on both clean and already-generated projects.
            M2CombatLabSetup.ApplyCombatLabSetup();
            BootstrapPlayModeSetup.Configure();
            EnsureDirectories();

            Scene gameplayScene = EditorSceneManager.OpenScene(SceneIds.GameplayPath, OpenSceneMode.Single);
            GameObject labRoot = GameObject.Find(LabRootName);
            if (labRoot == null)
            {
                AppLog.Error(LogSubsystem, "The generated movement/combat laboratory root was not found.");
                return;
            }

            MovementLabRevision revision = labRoot.GetComponent<MovementLabRevision>();
            if (revision != null && revision.Revision >= CurrentRevision)
            {
                AppLog.Info(LogSubsystem, "M2.1 grid segment setup is already current.", labRoot);
                return;
            }

            if (!TryLoadDependencies(
                    out Material floorMaterial,
                    out Material wallMaterial,
                    out Material immuneMaterial,
                    out Material vulnerableMaterial,
                    out HealthDefinition immuneHealth,
                    out HealthDefinition vulnerableHealth))
            {
                return;
            }

            FloorTileDefinition standardFloor = EnsureAsset<FloorTileDefinition>(FloorDefinitionPath);
            standardFloor.Configure(
                "standard_floor",
                FloorTraversalKind.Walkable,
                null,
                GridPrimitiveShape.Cube,
                floorMaterial,
                1f,
                0f);

            FloorTileDefinition gapFloor = EnsureAsset<FloorTileDefinition>(GapDefinitionPath);
            gapFloor.Configure(
                "gap",
                FloorTraversalKind.Gap,
                null,
                GridPrimitiveShape.Cube,
                null,
                1f,
                0f);

            BoundaryTileDefinition structuralWall = EnsureAsset<BoundaryTileDefinition>(WallDefinitionPath);
            structuralWall.ConfigureStructural("structural_wall", wallMaterial, 0.9f, 0.5f);

            SegmentPlaceableDefinition immuneBlock =
                EnsureAsset<SegmentPlaceableDefinition>(ImmunePlaceablePath);
            immuneBlock.Configure(
                "plasma_immune_block",
                null,
                GridPrimitiveShape.Cube,
                immuneMaterial,
                new Vector3(1f, 1.2f, 1.6f),
                new Vector3(0f, 0.6f, 0f),
                1,
                1,
                true,
                1f,
                immuneHealth);

            SegmentPlaceableDefinition vulnerableTarget =
                EnsureAsset<SegmentPlaceableDefinition>(VulnerablePlaceablePath);
            vulnerableTarget.Configure(
                "plasma_vulnerable_target",
                null,
                GridPrimitiveShape.Capsule,
                vulnerableMaterial,
                new Vector3(1.2f, 0.75f, 1.2f),
                new Vector3(0f, 0.75f, 0f),
                1,
                1,
                true,
                1f,
                vulnerableHealth);

            SegmentBlueprintDefinition blueprint = EnsureAsset<SegmentBlueprintDefinition>(BlueprintPath);
            ConfigureTrainingBlueprint(
                blueprint,
                standardFloor,
                gapFloor,
                structuralWall,
                immuneBlock,
                vulnerableTarget);

            if (!blueprint.IsValid(out string reason))
            {
                AppLog.Error(LogSubsystem, $"Generated training blueprint is invalid: {reason}", blueprint);
                return;
            }

            GameObject gridPrefab = EnsureGridSegmentPrefab();
            CorridorSegmentDefinition segment =
                AssetDatabase.LoadAssetAtPath<CorridorSegmentDefinition>(SegmentDefinitionPath);
            if (segment == null || gridPrefab == null)
            {
                AppLog.Error(LogSubsystem, "Segment definition or generic grid prefab is missing.");
                return;
            }

            segment.ConfigureBlueprint(blueprint, gridPrefab);
            EditorUtility.SetDirty(segment);
            MarkDirty(
                standardFloor,
                gapFloor,
                structuralWall,
                immuneBlock,
                vulnerableTarget,
                blueprint);

            if (!RebuildPreviewSegments(labRoot.transform, gridPrefab, segment))
            {
                return;
            }

            if (revision == null)
            {
                revision = labRoot.AddComponent<MovementLabRevision>();
            }
            revision.Configure(CurrentRevision);

            EditorSceneManager.MarkSceneDirty(gameplayScene);
            EditorSceneManager.SaveScene(gameplayScene);
            AssetDatabase.SaveAssets();

            // The old asset contains the fixed floor/wall/obstacle children that this
            // milestone replaces. Delete only this known generated prefab after every
            // reference has been redirected to the generic grid prefab.
            if (AssetDatabase.LoadAssetAtPath<GameObject>(FixedGeometryPrefabPath) != null &&
                AssetDatabase.DeleteAsset(FixedGeometryPrefabPath))
            {
                AppLog.Info(LogSubsystem, "Removed the obsolete fixed-geometry segment prefab.");
            }

            AssetDatabase.Refresh();
            Selection.activeObject = blueprint;
            AppLog.Info(
                LogSubsystem,
                "M2.1 created automatically: the training corridor now comes from layered grid data.",
                blueprint);
        }

        /// <summary>Validates the blueprint, generic prefab, scene revision, and Bootstrap flow.</summary>
        [MenuItem("Tools/Sphere Corridor/M2.1/Validate Grid Segment Setup")]
        public static void ValidateGridSegmentSetup()
        {
            bool valid = true;
            SegmentBlueprintDefinition blueprint =
                AssetDatabase.LoadAssetAtPath<SegmentBlueprintDefinition>(BlueprintPath);
            string reason = blueprint == null ? "Blueprint asset is missing." : string.Empty;
            if (blueprint == null || !blueprint.IsValid(out reason))
            {
                AppLog.Error(LogSubsystem, $"Blueprint validation failed: {reason}");
                valid = false;
            }

            CorridorSegmentDefinition segment =
                AssetDatabase.LoadAssetAtPath<CorridorSegmentDefinition>(SegmentDefinitionPath);
            if (segment == null || segment.Blueprint != blueprint ||
                segment.Prefab == null || segment.Prefab.GetComponent<GridSegmentAssembler>() == null)
            {
                AppLog.Error(LogSubsystem, "The segment definition is not connected to its grid blueprint/prefab.");
                valid = false;
            }

            SceneAsset bootstrap = AssetDatabase.LoadAssetAtPath<SceneAsset>(SceneIds.BootstrapPath);
            if (EditorSceneManager.playModeStartScene != bootstrap)
            {
                AppLog.Error(LogSubsystem, "Editor Play Mode is not configured to start from Bootstrap.");
                valid = false;
            }

            if (valid)
            {
                AppLog.Info(LogSubsystem, "M2.1 grid segment and Bootstrap validation passed.");
            }
        }

        /// <summary>Creates the initial authored 20x6 matrix and its two wall strips.</summary>
        private static void ConfigureTrainingBlueprint(
            SegmentBlueprintDefinition blueprint,
            FloorTileDefinition standardFloor,
            FloorTileDefinition gapFloor,
            BoundaryTileDefinition structuralWall,
            SegmentPlaceableDefinition immuneBlock,
            SegmentPlaceableDefinition vulnerableTarget)
        {
            int cellCount = LengthCells * WidthCells;
            FloorGridCell[] floors = new FloorGridCell[cellCount];
            ContentGridCell[] contents = new ContentGridCell[cellCount];
            BoundaryGridCell[] nearWalls = new BoundaryGridCell[LengthCells];
            BoundaryGridCell[] farWalls = new BoundaryGridCell[LengthCells];

            for (int lengthIndex = 0; lengthIndex < LengthCells; lengthIndex++)
            {
                nearWalls[lengthIndex] = new BoundaryGridCell(structuralWall);
                farWalls[lengthIndex] = new BoundaryGridCell(structuralWall);

                for (int widthIndex = 0; widthIndex < WidthCells; widthIndex++)
                {
                    int index = lengthIndex * WidthCells + widthIndex;
                    // One complete gap column retains the existing jump exercise.
                    floors[index] = new FloorGridCell(lengthIndex == 17 ? gapFloor : standardFloor);
                }
            }

            // Obstacles are now data entries at cell centres rather than prefab children.
            contents[14 * WidthCells + 1] = new ContentGridCell(immuneBlock);
            contents[16 * WidthCells + 4] = new ContentGridCell(vulnerableTarget);

            blueprint.Configure(
                "training_combat_grid",
                LengthCells,
                WidthCells,
                CellSize,
                2,
                2,
                1,
                floors,
                contents,
                nearWalls,
                farWalls);
        }

        /// <summary>Builds a geometry-free prefab whose children come exclusively from data.</summary>
        private static GameObject EnsureGridSegmentPrefab()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GridPrefabPath);
            if (prefab != null)
            {
                return prefab;
            }

            GameObject template = new GameObject("Grid Segment");
            template.AddComponent<CorridorSegmentInstance>();
            template.AddComponent<GridSegmentAssembler>();
            prefab = PrefabUtility.SaveAsPrefabAsset(template, GridPrefabPath);
            UnityEngine.Object.DestroyImmediate(template);
            AppLog.Info(LogSubsystem, $"Created generic grid segment prefab '{GridPrefabPath}'.", prefab);
            return prefab;
        }

        /// <summary>Replaces only generated preview occurrences; unknown scene content is preserved.</summary>
        private static bool RebuildPreviewSegments(
            Transform labRoot,
            GameObject gridPrefab,
            CorridorSegmentDefinition segment)
        {
            Transform instancesRoot = FindDescendant(labRoot, "Live Corridor Segments");
            if (instancesRoot == null)
            {
                AppLog.Error(LogSubsystem, "Live Corridor Segments root was not found.", labRoot);
                return false;
            }

            for (int index = instancesRoot.childCount - 1; index >= 0; index--)
            {
                UnityEngine.Object.DestroyImmediate(instancesRoot.GetChild(index).gameObject);
            }

            for (int index = 0; index < 3; index++)
            {
                GameObject preview = (GameObject)PrefabUtility.InstantiatePrefab(gridPrefab, instancesRoot);
                preview.transform.position = new Vector3((index - 1) * segment.Length, 0f, 0f);
                CorridorSegmentInstance instance = preview.GetComponent<CorridorSegmentInstance>();
                instance.Configure(segment, index);
            }

            AppLog.Info(LogSubsystem, "Rebuilt three preview segments from the authored matrix.", instancesRoot);
            return true;
        }

        /// <summary>Loads the assets created by M1 and M2 with clear failure logs.</summary>
        private static bool TryLoadDependencies(
            out Material floorMaterial,
            out Material wallMaterial,
            out Material immuneMaterial,
            out Material vulnerableMaterial,
            out HealthDefinition immuneHealth,
            out HealthDefinition vulnerableHealth)
        {
            floorMaterial = AssetDatabase.LoadAssetAtPath<Material>(FloorMaterialPath);
            wallMaterial = AssetDatabase.LoadAssetAtPath<Material>(WallMaterialPath);
            immuneMaterial = AssetDatabase.LoadAssetAtPath<Material>(ImmuneMaterialPath);
            vulnerableMaterial = AssetDatabase.LoadAssetAtPath<Material>(VulnerableMaterialPath);
            immuneHealth = AssetDatabase.LoadAssetAtPath<HealthDefinition>(ImmuneHealthPath);
            vulnerableHealth = AssetDatabase.LoadAssetAtPath<HealthDefinition>(VulnerableHealthPath);

            if (floorMaterial == null || wallMaterial == null || immuneMaterial == null ||
                vulnerableMaterial == null || immuneHealth == null || vulnerableHealth == null)
            {
                AppLog.Error(LogSubsystem, "M1/M2 material or health dependencies are incomplete.");
                return false;
            }

            return true;
        }

        /// <summary>Creates asset directories before AssetDatabase writes definitions.</summary>
        private static void EnsureDirectories()
        {
            Directory.CreateDirectory(SegmentDefinitionsDirectory);
            Directory.CreateDirectory(GridPrefabDirectory);
            AssetDatabase.Refresh();
        }

        /// <summary>Finds an exact generated child name without scene-wide runtime searches.</summary>
        private static Transform FindDescendant(Transform root, string objectName)
        {
            if (root == null)
            {
                return null;
            }

            if (root.name == objectName)
            {
                return root;
            }

            for (int index = 0; index < root.childCount; index++)
            {
                Transform found = FindDescendant(root.GetChild(index), objectName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        /// <summary>Creates one ScriptableObject asset while retaining its stable file identity.</summary>
        private static T EnsureAsset<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
            {
                return asset;
            }

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            AppLog.Info(LogSubsystem, $"Created grid definition '{path}'.", asset);
            return asset;
        }

        /// <summary>Marks generated definitions before one shared save operation.</summary>
        private static void MarkDirty(params UnityEngine.Object[] assets)
        {
            for (int index = 0; index < assets.Length; index++)
            {
                if (assets[index] != null)
                {
                    EditorUtility.SetDirty(assets[index]);
                }
            }
        }
    }
}
