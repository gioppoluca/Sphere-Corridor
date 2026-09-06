using NUnit.Framework;
using SphereCorridor.Corridor.Grid;
using UnityEngine;

namespace SphereCorridor.Tests.EditMode
{
    /// <summary>Protects flattened indexing, jump-aware reachability, and safety rules.</summary>
    public sealed class SegmentBlueprintTests
    {
        private FloorTileDefinition walkable;
        private FloorTileDefinition gap;
        private BoundaryTileDefinition wall;
        private SegmentBlueprintDefinition blueprint;

        /// <summary>Creates isolated ScriptableObject definitions for each test.</summary>
        [SetUp]
        public void SetUp()
        {
            walkable = ScriptableObject.CreateInstance<FloorTileDefinition>();
            walkable.Configure(
                "walkable",
                FloorTraversalKind.Walkable,
                null,
                GridPrimitiveShape.Cube,
                null,
                1f,
                0f);

            gap = ScriptableObject.CreateInstance<FloorTileDefinition>();
            gap.Configure(
                "gap",
                FloorTraversalKind.Gap,
                null,
                GridPrimitiveShape.Cube,
                null,
                1f,
                0f);

            wall = ScriptableObject.CreateInstance<BoundaryTileDefinition>();
            wall.ConfigureStructural("wall", null, 1f, 0.5f);
            blueprint = ScriptableObject.CreateInstance<SegmentBlueprintDefinition>();
        }

        /// <summary>Removes all temporary Unity objects after every assertion.</summary>
        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(walkable);
            Object.DestroyImmediate(gap);
            Object.DestroyImmediate(wall);
            Object.DestroyImmediate(blueprint);
        }

        /// <summary>Confirms Unity's flat array maps to the intended matrix coordinates.</summary>
        [Test]
        public void FlattenedIndexUsesLengthThenWidth()
        {
            ConfigureBlueprint(maximumJumpGap: 1);

            Assert.That(blueprint.GetIndex(0, 0), Is.EqualTo(0));
            Assert.That(blueprint.GetIndex(1, 0), Is.EqualTo(2));
            Assert.That(blueprint.GetIndex(3, 1), Is.EqualTo(7));
        }

        /// <summary>Confirms one full missing column is legal when the jump budget allows it.</summary>
        [Test]
        public void OneCellGapHasAValidatedRouteWhenJumpIsAllowed()
        {
            ConfigureBlueprint(maximumJumpGap: 1);

            bool valid = blueprint.IsValid(out string reason);

            Assert.That(valid, Is.True, reason);
        }

        /// <summary>Confirms the same matrix is rejected when no gap can be crossed.</summary>
        [Test]
        public void OneCellGapHasNoRouteWhenJumpIsDisabled()
        {
            ConfigureBlueprint(maximumJumpGap: 0);

            bool valid = blueprint.IsValid(out string reason);

            Assert.That(valid, Is.False);
            Assert.That(reason, Does.Contain("no route"));
        }

        /// <summary>Builds a small 4x2 blueprint with a complete gap in column one.</summary>
        private void ConfigureBlueprint(int maximumJumpGap)
        {
            const int length = 4;
            const int width = 2;
            FloorGridCell[] floors = new FloorGridCell[length * width];
            ContentGridCell[] contents = new ContentGridCell[length * width];
            BoundaryGridCell[] near = new BoundaryGridCell[length];
            BoundaryGridCell[] far = new BoundaryGridCell[length];

            for (int lengthIndex = 0; lengthIndex < length; lengthIndex++)
            {
                near[lengthIndex] = new BoundaryGridCell(wall);
                far[lengthIndex] = new BoundaryGridCell(wall);
                for (int widthIndex = 0; widthIndex < width; widthIndex++)
                {
                    int index = lengthIndex * width + widthIndex;
                    floors[index] = new FloorGridCell(lengthIndex == 1 ? gap : walkable);
                }
            }

            blueprint.Configure(
                "test_blueprint",
                length,
                width,
                2f,
                1,
                1,
                maximumJumpGap,
                floors,
                contents,
                near,
                far);
        }
    }
}
