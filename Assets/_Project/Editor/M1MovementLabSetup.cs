using System.IO;
using SphereCorridor.CameraSystem;
using SphereCorridor.Corridor;
using SphereCorridor.Foundation;
using SphereCorridor.Movement;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace SphereCorridor.Editor
{
    /// <summary>
    /// Creates the generated M1 movement laboratory without requiring
    /// manual scene authoring or a Tools-menu installation step.
    /// </summary>
    [InitializeOnLoad]
    public static class M1MovementLabSetup
    {
        private const int CurrentRevision = 2;
        private const float SegmentLength = 40f;
        private const string LogSubsystem = "M1Setup";
        private const string LabRootName = "[M1 Movement Lab]";
        private const string AutomaticSetupSessionKey = "SphereCorridor.M1.Revision2.AutomaticSetupAttempted";
        private const string InputActionsPath = "Assets/_Project/Input/SphereCorridorInputActions.inputactions";
        private const string DefinitionsDirectory = "Assets/_Project/Definitions";
        private const string MaterialsDirectory = "Assets/_Project/Art/Materials";
        private const string PhysicsDirectory = "Assets/_Project/Physics";
        private const string PrefabsDirectory = "Assets/_Project/Prefabs/Corridor";
        private const string MovementDefinitionPath = DefinitionsDirectory + "/PlayerMovementDefinition.asset";
        private const string CameraDefinitionPath = DefinitionsDirectory + "/MovementCameraDefinition.asset";
        private const string SegmentDefinitionPath = DefinitionsDirectory + "/BasicMovementSegment.asset";
        private const string SegmentPrefabPath = PrefabsDirectory + "/BasicMovementSegment.prefab";
        private const string FrictionMaterialPath = PhysicsDirectory + "/PlayerZeroFriction.physicMaterial";

        /// <summary>
        /// Schedules setup after Unity finishes importing and compiling the surgical patch.
        /// </summary>
        static M1MovementLabSetup()
        {
            EditorApplication.delayCall += ApplyAutomaticallyIfNeeded;
        }

        /// <summary>
        /// Runs once per editor session and waits for a safe asset-database state.
        /// </summary>
        private static void ApplyAutomaticallyIfNeeded()
        {
            if (SessionState.GetBool(AutomaticSetupSessionKey, false))
            {
                return;
            }

            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.delayCall += ApplyAutomaticallyIfNeeded;
                return;
            }

            SessionState.SetBool(AutomaticSetupSessionKey, true);
            ApplyMovementLabSetup();
        }

        /// <summary>
        /// Builds revision 2 or preserves an already-current generated laboratory.
        /// </summary>
        [MenuItem("Tools/Sphere Corridor/M1/Apply Movement Lab Setup")]
        public static void ApplyMovementLabSetup()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                AppLog.Warning(LogSubsystem, "Movement Lab generation cancelled because the open scene was not saved.");
                return;
            }

            InputActionAsset inputAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            if (inputAsset == null)
            {
                AppLog.Error(LogSubsystem, $"Required input asset is missing: {InputActionsPath}");
                return;
            }

            EnsureDirectories();

            PlayerMovementDefinition movementDefinition = EnsureAsset<PlayerMovementDefinition>(MovementDefinitionPath);
            MovementCameraDefinition cameraDefinition = EnsureAsset<MovementCameraDefinition>(CameraDefinitionPath);

            Material floorMaterial = EnsureRenderMaterial("M1_Floor", new Color(0.10f, 0.15f, 0.22f), 0.05f, 0.25f);
            Material wallMaterial = EnsureRenderMaterial("M1_Wall", new Color(0.18f, 0.30f, 0.42f), 0.15f, 0.35f);
            Material playerMaterial = EnsureRenderMaterial("M1_Player", new Color(0.15f, 0.75f, 1f), 0.65f, 0.75f);
            Material obstacleMaterial = EnsureRenderMaterial("M1_Obstacle", new Color(1f, 0.45f, 0.08f), 0.1f, 0.35f);
            PhysicsMaterial frictionMaterial = EnsureZeroFrictionMaterial();

            CorridorSegmentDefinition segmentDefinition = EnsureAsset<CorridorSegmentDefinition>(SegmentDefinitionPath);

            // Later setup stages replace this starter prefab with a grid-built one.
            // Reusing the assigned prefab prevents the M1 generator from undoing a
            // newer stage every time scripts recompile.
            GameObject segmentPrefab = segmentDefinition.Prefab;
            if (segmentPrefab == null)
            {
                segmentPrefab = EnsureSegmentPrefab(
                    segmentDefinition,
                    floorMaterial,
                    wallMaterial,
                    obstacleMaterial);
            }
            segmentDefinition.Configure("movement_training", SegmentLength, segmentPrefab);
            EditorUtility.SetDirty(segmentDefinition);

            Scene gameplayScene = EditorSceneManager.OpenScene(SceneIds.GameplayPath, OpenSceneMode.Single);
            GameObject existingLab = GameObject.Find(LabRootName);
            if (IsCurrentRevision(existingLab))
            {
                Selection.activeGameObject = existingLab;
                AppLog.Info(LogSubsystem, "M1.1 Movement Lab is already current; scene content was preserved.", existingLab);
                return;
            }

            if (existingLab != null)
            {
                Object.DestroyImmediate(existingLab);
                AppLog.Info(LogSubsystem, "Removed the generated M1 revision 1 laboratory before upgrading.");
            }

            RemoveEarlierGeneratedSceneObjects();

            GameObject labRoot = new GameObject(LabRootName);
            MovementLabRevision revision = labRoot.AddComponent<MovementLabRevision>();
            revision.Configure(CurrentRevision);

            GameObject segmentsRoot = new GameObject("Live Corridor Segments");
            segmentsRoot.transform.SetParent(labRoot.transform);
            CreatePreviewSegments(segmentsRoot.transform, segmentDefinition);

            PlayerMovementMotor motor = CreatePlayer(
                labRoot.transform,
                inputAsset,
                movementDefinition,
                playerMaterial,
                frictionMaterial,
                out Rigidbody playerBody);
            OverheadFollowCamera followCamera = CreateOverheadCamera(labRoot.transform, motor, cameraDefinition);

            PlayerRearBoundary rearBoundary = motor.gameObject.AddComponent<PlayerRearBoundary>();
            rearBoundary.Configure(playerBody, followCamera);

            GameObject streamerObject = new GameObject("Corridor Segment Streamer");
            streamerObject.transform.SetParent(labRoot.transform);
            CorridorSegmentStreamer streamer = streamerObject.AddComponent<CorridorSegmentStreamer>();
            streamer.Configure(motor.transform, followCamera, segmentsRoot.transform, new[] { segmentDefinition });

            GameObject overlayObject = new GameObject("Movement Debug Overlay");
            overlayObject.transform.SetParent(labRoot.transform);
            PlayerMovementDebugOverlay overlay = overlayObject.AddComponent<PlayerMovementDebugOverlay>();
            overlay.Configure(motor, streamer);

            CreateLighting(labRoot.transform);

            EditorSceneManager.MarkSceneDirty(gameplayScene);
            EditorSceneManager.SaveScene(gameplayScene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeGameObject = labRoot;
            AppLog.Info(LogSubsystem, "M1.1 created: friction fixed, camera reframed, and three segment instances active.");
        }

        /// <summary>
        /// Checks the generated scene and data assets without modifying them.
        /// </summary>
        [MenuItem("Tools/Sphere Corridor/M1/Validate Movement Lab")]
        public static void ValidateMovementLab()
        {
            bool valid = true;
            PlayerMovementDefinition movementDefinition =
                AssetDatabase.LoadAssetAtPath<PlayerMovementDefinition>(MovementDefinitionPath);
            CorridorSegmentDefinition segmentDefinition =
                AssetDatabase.LoadAssetAtPath<CorridorSegmentDefinition>(SegmentDefinitionPath);
            PhysicsMaterial frictionMaterial =
                AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(FrictionMaterialPath);

            if (movementDefinition == null)
            {
                AppLog.Error(LogSubsystem, "Movement definition is missing.");
                valid = false;
            }
            else if (!movementDefinition.IsValid(out string movementReason))
            {
                AppLog.Error(LogSubsystem, $"Movement definition is invalid: {movementReason}");
                valid = false;
            }

            if (segmentDefinition == null)
            {
                AppLog.Error(LogSubsystem, "Segment definition is missing.");
                valid = false;
            }
            else if (!segmentDefinition.IsValid(out string segmentReason))
            {
                AppLog.Error(LogSubsystem, $"Segment definition is invalid: {segmentReason}");
                valid = false;
            }

            if (frictionMaterial == null ||
                !Mathf.Approximately(frictionMaterial.staticFriction, 0f) ||
                !Mathf.Approximately(frictionMaterial.dynamicFriction, 0f))
            {
                AppLog.Error(LogSubsystem, "The player zero-friction PhysicsMaterial is missing or invalid.");
                valid = false;
            }

            Scene gameplayScene = EditorSceneManager.OpenScene(SceneIds.GameplayPath, OpenSceneMode.Single);
            GameObject labRoot = GameObject.Find(LabRootName);
            if (!IsCurrentRevision(labRoot) || labRoot.scene != gameplayScene)
            {
                AppLog.Error(LogSubsystem, "Gameplay does not contain the current M1.1 lab revision.");
                valid = false;
            }

            if (valid)
            {
                AppLog.Info(LogSubsystem, "M1.1 Movement Lab validation passed.");
            }
        }

        /// <summary>
        /// Creates asset folders before asking the AssetDatabase to create files inside them.
        /// </summary>
        private static void EnsureDirectories()
        {
            Directory.CreateDirectory(DefinitionsDirectory);
            Directory.CreateDirectory(MaterialsDirectory);
            Directory.CreateDirectory(PhysicsDirectory);
            Directory.CreateDirectory(PrefabsDirectory);
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// Returns true only for a generated root carrying the current revision marker.
        /// </summary>
        private static bool IsCurrentRevision(GameObject labRoot)
        {
            if (labRoot == null)
            {
                return false;
            }

            MovementLabRevision marker = labRoot.GetComponent<MovementLabRevision>();
            return marker != null && marker.Revision >= CurrentRevision;
        }

        /// <summary>
        /// Removes the exact disposable objects generated by M0 before M1 creates
        /// their replacements. Unknown scene objects are deliberately left alone so
        /// an automatic setup pass cannot delete hand-authored work.
        /// </summary>
        private static void RemoveEarlierGeneratedSceneObjects()
        {
            DestroySceneObjectIfPresent("Main Camera");
            DestroySceneObjectIfPresent("Directional Light");
            DestroySceneObjectIfPresent("M0 Floor Placeholder");
            DestroySceneObjectIfPresent("M0 Sphere Placeholder");
            DestroySceneObjectIfPresent("[M0 Gameplay View]");
        }

        /// <summary>
        /// Deletes one known generated object when it exists. The narrow name-based
        /// target is intentional because this code runs automatically in the Editor.
        /// </summary>
        private static void DestroySceneObjectIfPresent(string objectName)
        {
            GameObject sceneObject = GameObject.Find(objectName);
            if (sceneObject != null)
            {
                Object.DestroyImmediate(sceneObject);
            }
        }

        /// <summary>
        /// Creates the one M1 segment prefab. Future types use the same definition contract.
        /// </summary>
        private static GameObject EnsureSegmentPrefab(
            CorridorSegmentDefinition definition,
            Material floorMaterial,
            Material wallMaterial,
            Material obstacleMaterial)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SegmentPrefabPath);
            if (prefab != null)
            {
                return prefab;
            }

            GameObject template = new GameObject("Basic Movement Segment");
            CorridorSegmentInstance instance = template.AddComponent<CorridorSegmentInstance>();
            instance.Configure(definition, 0);
            template.name = "Basic Movement Segment";

            // Two floor pieces retain a 2 m jump gap near the end of every training segment.
            CreatePrimitive("Floor Before Gap", PrimitiveType.Cube, new Vector3(-2.5f, -0.5f, 0f),
                new Vector3(35f, 1f, 12f), floorMaterial, template.transform);
            CreatePrimitive("Floor After Gap", PrimitiveType.Cube, new Vector3(18.5f, -0.5f, 0f),
                new Vector3(3f, 1f, 12f), floorMaterial, template.transform);

            // Low rails establish corridor depth without consuming excessive screen space.
            CreatePrimitive("Near Wall", PrimitiveType.Cube, new Vector3(0f, 0.45f, -6.25f),
                new Vector3(SegmentLength, 0.9f, 0.5f), wallMaterial, template.transform);
            CreatePrimitive("Far Wall", PrimitiveType.Cube, new Vector3(0f, 0.45f, 6.25f),
                new Vector3(SegmentLength, 0.9f, 0.5f), wallMaterial, template.transform);
            CreatePrimitive("Low Jump Barrier", PrimitiveType.Cube, new Vector3(8f, 0.35f, 0f),
                new Vector3(1f, 0.7f, 11.5f), obstacleMaterial, template.transform);

            prefab = PrefabUtility.SaveAsPrefabAsset(template, SegmentPrefabPath);
            Object.DestroyImmediate(template);
            AppLog.Info(LogSubsystem, $"Created prefab-backed segment type at '{SegmentPrefabPath}'.", prefab);
            return prefab;
        }

        /// <summary>
        /// Instantiates previous, current, and next segments for an immediate scene preview.
        /// </summary>
        private static void CreatePreviewSegments(Transform parent, CorridorSegmentDefinition definition)
        {
            for (int index = 0; index < 3; index++)
            {
                GameObject instanceObject = (GameObject)PrefabUtility.InstantiatePrefab(definition.Prefab, parent);
                instanceObject.transform.position = new Vector3((index - 1) * definition.Length, 0f, 0f);
                CorridorSegmentInstance instance = instanceObject.GetComponent<CorridorSegmentInstance>();
                instance.Configure(definition, index);
            }
        }

        /// <summary>
        /// Creates the dynamic motor root, frictionless collider, and visual-only shell.
        /// </summary>
        private static PlayerMovementMotor CreatePlayer(
            Transform parent,
            InputActionAsset inputAsset,
            PlayerMovementDefinition definition,
            Material playerMaterial,
            PhysicsMaterial frictionMaterial,
            out Rigidbody playerBody)
        {
            GameObject player = new GameObject("Player Sphere");
            player.transform.SetParent(parent);
            // Start well before the first obstacle so grounded cruise is obvious.
            player.transform.position = new Vector3(-10f, definition.SphereRadius, 0f);

            SphereCollider sphereCollider = player.AddComponent<SphereCollider>();
            sphereCollider.radius = definition.SphereRadius;
            sphereCollider.sharedMaterial = frictionMaterial;

            playerBody = player.AddComponent<Rigidbody>();
            playerBody.mass = 2f;
            playerBody.useGravity = false;
            playerBody.interpolation = RigidbodyInterpolation.Interpolate;
            playerBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            playerBody.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;

            GameObject shell = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            shell.name = "Visual Shell";
            shell.transform.SetParent(player.transform);
            shell.transform.localPosition = Vector3.zero;
            shell.transform.localRotation = Quaternion.identity;
            shell.transform.localScale = Vector3.one * definition.SphereRadius * 2f;
            Object.DestroyImmediate(shell.GetComponent<Collider>());
            shell.GetComponent<Renderer>().sharedMaterial = playerMaterial;

            PlayerInputReader inputReader = player.AddComponent<PlayerInputReader>();
            inputReader.Configure(inputAsset);

            PlayerMovementMotor motor = player.AddComponent<PlayerMovementMotor>();
            motor.Configure(definition, inputReader, playerBody, sphereCollider, shell.transform);

            PlayerLandingFeedback landingFeedback = player.AddComponent<PlayerLandingFeedback>();
            landingFeedback.Configure(motor, shell.transform);

            MovementLabRespawner respawner = player.AddComponent<MovementLabRespawner>();
            respawner.Configure(playerBody, player.transform.position, -8f);
            return motor;
        }

        /// <summary>
        /// Creates the forward-only high camera and returns its shared boundary provider.
        /// </summary>
        private static OverheadFollowCamera CreateOverheadCamera(
            Transform parent,
            PlayerMovementMotor motor,
            MovementCameraDefinition definition)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(parent);

            UnityEngine.Camera sceneCamera = cameraObject.AddComponent<UnityEngine.Camera>();
            sceneCamera.clearFlags = CameraClearFlags.SolidColor;
            sceneCamera.backgroundColor = new Color(0.008f, 0.014f, 0.025f);
            sceneCamera.fieldOfView = 50f;
            cameraObject.AddComponent<AudioListener>();

            OverheadFollowCamera followCamera = cameraObject.AddComponent<OverheadFollowCamera>();
            followCamera.Configure(motor.transform, motor, definition);
            return followCamera;
        }

        /// <summary>
        /// Adds one shared directional light outside the replaceable segment instances.
        /// </summary>
        private static void CreateLighting(Transform parent)
        {
            GameObject lightObject = new GameObject("Directional Light");
            lightObject.transform.SetParent(parent);
            lightObject.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
            Light sceneLight = lightObject.AddComponent<Light>();
            sceneLight.type = LightType.Directional;
            sceneLight.intensity = 1.25f;
        }

        /// <summary>
        /// Creates one primitive child using local coordinates suitable for a prefab.
        /// </summary>
        private static GameObject CreatePrimitive(
            string objectName,
            PrimitiveType primitiveType,
            Vector3 localPosition,
            Vector3 localScale,
            Material material,
            Transform parent)
        {
            GameObject primitive = GameObject.CreatePrimitive(primitiveType);
            primitive.name = objectName;
            primitive.transform.SetParent(parent);
            primitive.transform.localPosition = localPosition;
            primitive.transform.localRotation = Quaternion.identity;
            primitive.transform.localScale = localScale;
            primitive.GetComponent<Renderer>().sharedMaterial = material;
            return primitive;
        }

        /// <summary>
        /// Creates a definition asset only once so later Inspector tuning is preserved.
        /// </summary>
        private static T EnsureAsset<T>(string assetPath) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (asset != null)
            {
                return asset;
            }

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, assetPath);
            AppLog.Info(LogSubsystem, $"Created tuning asset '{assetPath}'.", asset);
            return asset;
        }

        /// <summary>
        /// Creates and reuses the zero-friction material required by a non-rolling motor.
        /// </summary>
        private static PhysicsMaterial EnsureZeroFrictionMaterial()
        {
            PhysicsMaterial material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(FrictionMaterialPath);
            if (material == null)
            {
                material = new PhysicsMaterial("PlayerZeroFriction");
                AssetDatabase.CreateAsset(material, FrictionMaterialPath);
            }

            material.staticFriction = 0f;
            material.dynamicFriction = 0f;
            material.bounciness = 0f;
            material.frictionCombine = PhysicsMaterialCombine.Minimum;
            material.bounceCombine = PhysicsMaterialCombine.Minimum;
            EditorUtility.SetDirty(material);
            return material;
        }

        /// <summary>
        /// Creates one reusable URP material and preserves it on later setup runs.
        /// </summary>
        private static Material EnsureRenderMaterial(
            string materialName,
            Color baseColor,
            float metallic,
            float smoothness)
        {
            string path = $"{MaterialsDirectory}/{materialName}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                return material;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                throw new System.InvalidOperationException("URP Lit shader was not found.");
            }

            material = new Material(shader)
            {
                name = materialName
            };
            material.SetColor("_BaseColor", baseColor);
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Smoothness", smoothness);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }
    }
}
