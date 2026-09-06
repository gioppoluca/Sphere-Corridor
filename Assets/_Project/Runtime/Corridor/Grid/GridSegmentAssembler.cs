using SphereCorridor.Combat;
using SphereCorridor.Foundation;
using UnityEngine;

namespace SphereCorridor.Corridor.Grid
{
    /// <summary>
    /// Materializes one immutable blueprint beneath a segment root. Segment lifecycle
    /// remains fresh instantiate/destroy; only its composition is now data-driven.
    /// </summary>
    public sealed class GridSegmentAssembler : MonoBehaviour
    {
        private const string LogSubsystem = "SegmentGrid";
        private const string GeneratedRootName = "[Generated Grid]";

        [SerializeField] private SegmentBlueprintDefinition blueprint;

        public SegmentBlueprintDefinition Blueprint => blueprint;

        /// <summary>
        /// Assigns the immutable recipe and immediately creates its visible instance.
        /// Returning success lets the streamer discard a bad segment before it enters
        /// the live corridor.
        /// </summary>
        public bool ConfigureAndBuild(SegmentBlueprintDefinition segmentBlueprint)
        {
            blueprint = segmentBlueprint;
            return Build();
        }

        /// <summary>
        /// Validates first, removes only this assembler's previous output, and builds
        /// three named layers. Separate layers keep the Hierarchy readable while the
        /// blueprint remains the single source of truth.
        /// </summary>
        public bool Build()
        {
            string reason = blueprint == null ? "Blueprint is missing." : string.Empty;
            if (blueprint == null || !blueprint.IsValid(out reason))
            {
                AppLog.Error(LogSubsystem, $"Cannot build segment '{name}': {reason}", this);
                return false;
            }

            ClearGeneratedHierarchy();
            GameObject generatedRoot = new GameObject(GeneratedRootName);
            generatedRoot.transform.SetParent(transform, false);

            GameObject floors = new GameObject("Floor Grid");
            floors.transform.SetParent(generatedRoot.transform, false);
            GameObject contents = new GameObject("Content Grid");
            contents.transform.SetParent(generatedRoot.transform, false);
            GameObject boundaries = new GameObject("Boundary Strips");
            boundaries.transform.SetParent(generatedRoot.transform, false);

            BuildFloorLayer(floors.transform);
            BuildBoundaryStrip(true, boundaries.transform);
            BuildBoundaryStrip(false, boundaries.transform);

            for (int lengthIndex = 0; lengthIndex < blueprint.LengthCells; lengthIndex++)
            {
                for (int widthIndex = 0; widthIndex < blueprint.WidthCells; widthIndex++)
                {
                    BuildContent(lengthIndex, widthIndex, contents.transform);
                }
            }

            AppLog.Development(
                LogSubsystem,
                $"Built '{blueprint.StableId}' as {blueprint.LengthCells}x{blueprint.WidthCells} cells.",
                this);
            return true;
        }

        /// <summary>
        /// Builds runtime-instantiated prefabs whose generated children are not stored
        /// in the prefab asset. The name guard prevents a second copy when the Editor
        /// setup already saved generated children into its preview instance.
        /// </summary>
        private void Awake()
        {
            if (blueprint != null && transform.Find(GeneratedRootName) == null)
            {
                Build();
            }
        }

        /// <summary>
        /// Scans along world X independently for each lateral lane. Consecutive cells
        /// using the same mergeable definition become one longer object, reducing
        /// renderers, colliders, and hierarchy objects without losing the matrix data.
        /// Gaps produce no object and custom behavioural tiles can disable merging.
        /// </summary>
        private void BuildFloorLayer(Transform parent)
        {
            for (int widthIndex = 0; widthIndex < blueprint.WidthCells; widthIndex++)
            {
                int lengthIndex = 0;
                while (lengthIndex < blueprint.LengthCells)
                {
                    FloorGridCell firstCell = blueprint.GetFloorCell(lengthIndex, widthIndex);
                    FloorTileDefinition definition = firstCell.Definition;
                    if (definition == null || definition.TraversalKind == FloorTraversalKind.Gap)
                    {
                        lengthIndex++;
                        continue;
                    }

                    int runLength = 1;
                    if (definition.MergeAdjacentCells)
                    {
                        while (lengthIndex + runLength < blueprint.LengthCells)
                        {
                            FloorGridCell candidate = blueprint.GetFloorCell(lengthIndex + runLength, widthIndex);
                            if (candidate.Definition != definition || candidate.QuarterTurns != firstCell.QuarterTurns)
                            {
                                break;
                            }

                            runLength++;
                        }
                    }

                    BuildFloorRun(lengthIndex, widthIndex, runLength, firstCell, parent);
                    lengthIndex += runLength;
                }
            }
        }

        /// <summary>
        /// Creates the single object representing a consecutive floor run. Its centre
        /// is shifted from the first cell to the run midpoint, then its X scale covers
        /// every source cell exactly.
        /// </summary>
        private void BuildFloorRun(
            int startLengthIndex,
            int widthIndex,
            int runLength,
            FloorGridCell cell,
            Transform parent)
        {
            FloorTileDefinition definition = cell.Definition;
            GameObject tile = CreateVisual(
                definition.PrefabOverride,
                definition.FallbackShape,
                definition.Material,
                $"Floor [{startLengthIndex:00}+{runLength:00},{widthIndex:00}] {definition.StableId}",
                parent);
            Vector3 centre = blueprint.GetLocalCellCenter(startLengthIndex, widthIndex);
            centre.x += (runLength - 1) * blueprint.CellSize * 0.5f;
            tile.transform.localPosition = centre +
                                           Vector3.up * (definition.SurfaceElevation - definition.Thickness * 0.5f);
            tile.transform.localRotation = Quaternion.Euler(0f, cell.QuarterTurns * 90f, 0f);
            bool rotatedAcrossCorridor = (cell.QuarterTurns & 1) != 0;
            tile.transform.localScale = rotatedAcrossCorridor
                ? new Vector3(blueprint.CellSize, definition.Thickness, blueprint.CellSize * runLength)
                : new Vector3(blueprint.CellSize * runLength, definition.Thickness, blueprint.CellSize);
        }

        /// <summary>
        /// Creates the optional object anchored at one cell centre, then applies the
        /// reusable HealthDefinition. Content is intentionally not merged because each
        /// obstacle may move, take damage, fire, or die independently later.
        /// </summary>
        private void BuildContent(int lengthIndex, int widthIndex, Transform parent)
        {
            ContentGridCell cell = blueprint.GetContentCell(lengthIndex, widthIndex);
            SegmentPlaceableDefinition definition = cell.Definition;
            if (definition == null)
            {
                return;
            }

            GameObject content = CreateVisual(
                definition.PrefabOverride,
                definition.FallbackShape,
                definition.Material,
                $"Content [{lengthIndex:00},{widthIndex:00}] {definition.StableId}",
                parent);
            content.transform.localPosition = blueprint.GetLocalCellCenter(lengthIndex, widthIndex) +
                                              definition.LocalOffset;
            content.transform.localRotation = Quaternion.Euler(0f, cell.QuarterTurns * 90f, 0f);
            content.transform.localScale = definition.LocalScale;

            if (definition.HealthDefinition != null)
            {
                Health health = content.GetComponent<Health>();
                if (health == null)
                {
                    health = content.AddComponent<Health>();
                }

                health.Configure(definition.HealthDefinition);
            }
        }

        /// <summary>
        /// Scans one near/far wall strip along world X. Only adjacent cells referencing
        /// the exact same mergeable definition are combined, so doors, hazards, and
        /// destructible wall types retain independent objects when introduced.
        /// </summary>
        private void BuildBoundaryStrip(bool nearSide, Transform parent)
        {
            int lengthIndex = 0;
            while (lengthIndex < blueprint.LengthCells)
            {
                BoundaryTileDefinition definition = nearSide
                    ? blueprint.GetNearBoundary(lengthIndex).Definition
                    : blueprint.GetFarBoundary(lengthIndex).Definition;
                if (definition == null || definition.Kind == BoundaryKind.Breach)
                {
                    lengthIndex++;
                    continue;
                }

                int runLength = 1;
                if (definition.MergeAdjacentCells)
                {
                    while (lengthIndex + runLength < blueprint.LengthCells)
                    {
                        BoundaryTileDefinition candidate = nearSide
                            ? blueprint.GetNearBoundary(lengthIndex + runLength).Definition
                            : blueprint.GetFarBoundary(lengthIndex + runLength).Definition;
                        if (candidate != definition)
                        {
                            break;
                        }

                        runLength++;
                    }
                }

                BuildBoundaryRun(lengthIndex, runLength, nearSide, definition, parent);
                lengthIndex += runLength;
            }
        }

        /// <summary>
        /// Creates one wall object for a consecutive run and places it just outside
        /// the playable floor width. Near and far strips are never merged together
        /// because they occupy opposite sides of the corridor.
        /// </summary>
        private void BuildBoundaryRun(
            int startLengthIndex,
            int runLength,
            bool nearSide,
            BoundaryTileDefinition definition,
            Transform parent)
        {
            string side = nearSide ? "Near" : "Far";
            GameObject wall = CreateVisual(
                definition.PrefabOverride,
                definition.FallbackShape,
                definition.Material,
                $"{side} Boundary [{startLengthIndex:00}+{runLength:00}] {definition.StableId}",
                parent);

            float x = (startLengthIndex + runLength * 0.5f) * blueprint.CellSize -
                      blueprint.LengthMeters * 0.5f;
            float zDirection = nearSide ? -1f : 1f;
            float z = zDirection * (blueprint.WidthMeters * 0.5f + definition.Thickness * 0.5f);
            wall.transform.localPosition = new Vector3(x, definition.Height * 0.5f, z);
            wall.transform.localRotation = Quaternion.identity;
            wall.transform.localScale = new Vector3(
                blueprint.CellSize * runLength,
                definition.Height,
                definition.Thickness);

            if (!definition.BlocksActors && !definition.BlocksProjectiles)
            {
                Collider collider = wall.GetComponent<Collider>();
                if (collider != null)
                {
                    DestroyGeneratedObject(collider);
                }
            }
        }

        /// <summary>Instantiates a custom prefab or creates the selected geometric fallback.</summary>
        private static GameObject CreateVisual(
            GameObject prefab,
            GridPrimitiveShape shape,
            Material material,
            string objectName,
            Transform parent)
        {
            GameObject visual = prefab != null
                ? Instantiate(prefab, parent)
                : GameObject.CreatePrimitive(ToPrimitiveType(shape));
            visual.name = objectName;
            if (visual.transform.parent != parent)
            {
                visual.transform.SetParent(parent, false);
            }

            Renderer renderer = visual.GetComponentInChildren<Renderer>();
            if (renderer != null && material != null)
            {
                renderer.sharedMaterial = material;
            }

            return visual;
        }

        /// <summary>Maps the serializable vocabulary enum to Unity's primitive factory.</summary>
        private static PrimitiveType ToPrimitiveType(GridPrimitiveShape shape)
        {
            switch (shape)
            {
                case GridPrimitiveShape.Sphere:
                    return PrimitiveType.Sphere;
                case GridPrimitiveShape.Capsule:
                    return PrimitiveType.Capsule;
                case GridPrimitiveShape.Cylinder:
                    return PrimitiveType.Cylinder;
                default:
                    return PrimitiveType.Cube;
            }
        }

        /// <summary>Removes only the hierarchy owned by this assembler.</summary>
        private void ClearGeneratedHierarchy()
        {
            Transform existing = transform.Find(GeneratedRootName);
            if (existing != null)
            {
                DestroyGeneratedObject(existing.gameObject);
            }
        }

        /// <summary>Uses immediate destruction only while authoring outside Play Mode.</summary>
        private static void DestroyGeneratedObject(Object target)
        {
            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }
    }
}
