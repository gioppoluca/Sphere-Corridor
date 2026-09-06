using System.IO;
using SphereCorridor.Combat;
using SphereCorridor.Corridor;
using SphereCorridor.Foundation;
using SphereCorridor.Movement;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SphereCorridor.Editor
{
    /// <summary>
    /// Adds the M2 combat slice to the generated movement lab after scripts compile.
    /// The menu command remains only as a recovery and validation entry point.
    /// </summary>
    [InitializeOnLoad]
    public static class M2CombatLabSetup
    {
        private const int CurrentRevision = 3;
        private const string LogSubsystem = "M2Setup";
        private const string LabRootName = "[M1 Movement Lab]";
        private const string SessionKey = "SphereCorridor.M2.Revision3.AutomaticSetupAttempted";
        private const string DefinitionsDirectory = "Assets/_Project/Definitions/Combat";
        private const string MaterialsDirectory = "Assets/_Project/Art/Materials";
        private const string PrefabsDirectory = "Assets/_Project/Prefabs/Combat";
        private const string SegmentPrefabPath = "Assets/_Project/Prefabs/Corridor/BasicMovementSegment.prefab";
        private const string SegmentDefinitionPath = "Assets/_Project/Definitions/BasicMovementSegment.asset";
        private const string PlasmaTypePath = DefinitionsDirectory + "/PlasmaDamage.asset";
        private const string StraightTrajectoryPath = DefinitionsDirectory + "/StraightTrajectory.asset";
        private const string DestroyEffectPath = DefinitionsDirectory + "/DestroyOnDeath.asset";
        private const string PlayerHealthPath = DefinitionsDirectory + "/PlayerHealth.asset";
        private const string ImmuneHealthPath = DefinitionsDirectory + "/PlasmaImmuneObstacleHealth.asset";
        private const string VulnerableHealthPath = DefinitionsDirectory + "/PlasmaVulnerableTargetHealth.asset";
        private const string WeaponPath = DefinitionsDirectory + "/PlayerPlasmaWeapon.asset";
        private const string ProjectilePrefabPath = PrefabsDirectory + "/PlasmaProjectile.prefab";

        /// <summary>
        /// Defers generation until Unity has loaded this editor assembly; direct asset
        /// writes inside a static constructor can collide with script import.
        /// </summary>
        static M2CombatLabSetup()
        {
            EditorApplication.delayCall += ApplyAutomaticallyIfNeeded;
        }

        /// <summary>Waits for a safe AssetDatabase state and attempts setup once per session.</summary>
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
            ApplyCombatLabSetup();
        }

        /// <summary>Creates combat data and patches only the generated M1 laboratory content.</summary>
        [MenuItem("Tools/Sphere Corridor/M2/Apply Combat Lab Setup")]
        public static void ApplyCombatLabSetup()
        {
            // Calling the idempotent M1 setup first makes this patch safe on both the
            // user's updated project and a newly restored clean checkout.
            M1MovementLabSetup.ApplyMovementLabSetup();
            EnsureDirectories();

            Scene gameplayScene = EditorSceneManager.OpenScene(SceneIds.GameplayPath, OpenSceneMode.Single);
            GameObject labRoot = GameObject.Find(LabRootName);
            if (labRoot == null)
            {
                AppLog.Error(LogSubsystem, "The generated M1 laboratory root was not found.");
                return;
            }

            MovementLabRevision revision = labRoot.GetComponent<MovementLabRevision>();
            if (revision != null && revision.Revision >= CurrentRevision)
            {
                AppLog.Info(LogSubsystem, "M2 Combat Lab is already current; authored tuning was preserved.", labRoot);
                return;
            }

            DamageTypeDefinition plasma = EnsureAsset<DamageTypeDefinition>(PlasmaTypePath);
            plasma.Configure("plasma", "Plasma");

            StraightProjectileTrajectoryDefinition trajectory =
                EnsureAsset<StraightProjectileTrajectoryDefinition>(StraightTrajectoryPath);
            DestroyOnDeathEffectDefinition destroyEffect =
                EnsureAsset<DestroyOnDeathEffectDefinition>(DestroyEffectPath);

            Material plasmaMaterial = EnsureRenderMaterial(
                "M2_Plasma",
                new Color(0.08f, 0.95f, 1f),
                0.2f,
                0.9f,
                emissionColor: new Color(0.02f, 0.8f, 1f) * 3f);
            Material immuneMaterial = EnsureRenderMaterial(
                "M2_PlasmaImmune",
                new Color(1f, 0.40f, 0.06f),
                0.35f,
                0.45f);
            Material vulnerableMaterial = EnsureRenderMaterial(
                "M2_PlasmaVulnerable",
                new Color(0.95f, 0.12f, 0.72f),
                0.15f,
                0.65f);

            ProjectileInstance projectilePrefab = EnsureProjectilePrefab(plasmaMaterial);
            if (projectilePrefab == null)
            {
                AppLog.Error(LogSubsystem, "The plasma projectile prefab is invalid.");
                return;
            }
            WeaponDefinition weapon = EnsureAsset<WeaponDefinition>(WeaponPath);
            weapon.ConfigurePlasmaLab(plasma, projectilePrefab, trajectory);

            HealthDefinition playerHealth = EnsureAsset<HealthDefinition>(PlayerHealthPath);
            playerHealth.Configure(
                5f,
                CombatFaction.Player,
                false,
                System.Array.Empty<DamageResistanceEntry>(),
                System.Array.Empty<DeathEffectDefinition>());

            HealthDefinition immuneHealth = EnsureAsset<HealthDefinition>(ImmuneHealthPath);
            immuneHealth.Configure(
                3f,
                CombatFaction.Environment,
                false,
                new[] { new DamageResistanceEntry(plasma, 0f) },
                new DeathEffectDefinition[] { destroyEffect });

            HealthDefinition vulnerableHealth = EnsureAsset<HealthDefinition>(VulnerableHealthPath);
            vulnerableHealth.Configure(
                3f,
                CombatFaction.Environment,
                false,
                System.Array.Empty<DamageResistanceEntry>(),
                new DeathEffectDefinition[] { destroyEffect });

            MarkDirty(
                plasma,
                trajectory,
                destroyEffect,
                weapon,
                playerHealth,
                immuneHealth,
                vulnerableHealth);

            bool prefabConfigured = ConfigureCombatTargetsOnGeneratedSegment(
                immuneMaterial,
                vulnerableMaterial,
                immuneHealth,
                vulnerableHealth);
            bool segmentValid = ValidateSegmentDefinition();
            bool sceneConfigured = ConfigureCombatScene(labRoot, projectilePrefab, weapon, playerHealth);
            if (!prefabConfigured || !segmentValid || !sceneConfigured)
            {
                AppLog.Error(LogSubsystem, "M2 setup stopped because one generated dependency is invalid.");
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
            AssetDatabase.Refresh();
            Selection.activeGameObject = labRoot;
            AppLog.Info(
                LogSubsystem,
                "M2 created automatically: lateral movement, plasma combat, typed damage, and target variants are ready.",
                labRoot);
        }

        /// <summary>Checks the generated combat slice without changing it.</summary>
        [MenuItem("Tools/Sphere Corridor/M2/Validate Combat Lab")]
        public static void ValidateCombatLab()
        {
            bool valid = true;
            WeaponDefinition weapon = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(WeaponPath);
            string weaponReason = weapon == null ? "Weapon asset is missing." : string.Empty;
            if (weapon == null || !weapon.IsValid(out weaponReason))
            {
                AppLog.Error(LogSubsystem, $"Weapon validation failed: {weaponReason}");
                valid = false;
            }

            CorridorSegmentDefinition segment =
                AssetDatabase.LoadAssetAtPath<CorridorSegmentDefinition>(SegmentDefinitionPath);
            string segmentReason = segment == null ? "Segment asset is missing." : string.Empty;
            if (segment == null || !segment.IsValid(out segmentReason))
            {
                AppLog.Error(LogSubsystem, $"Segment validation failed: {segmentReason}");
                valid = false;
            }
            else if (!CorridorSegmentCompatibility.CanFollow(segment, segment, out string compatibilityReason))
            {
                AppLog.Error(LogSubsystem, $"Training segment cannot follow itself: {compatibilityReason}");
                valid = false;
            }

            if (valid)
            {
                AppLog.Info(LogSubsystem, "M2 Combat Lab validation passed.");
            }
        }

        /// <summary>Creates directories before AssetDatabase writes generated assets.</summary>
        private static void EnsureDirectories()
        {
            Directory.CreateDirectory(DefinitionsDirectory);
            Directory.CreateDirectory(MaterialsDirectory);
            Directory.CreateDirectory(PrefabsDirectory);
            AssetDatabase.Refresh();
        }

        /// <summary>Creates the compact visual projectile prefab used by the pool.</summary>
        private static ProjectileInstance EnsureProjectilePrefab(Material material)
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectilePrefabPath);
            if (existing != null)
            {
                return existing.GetComponent<ProjectileInstance>();
            }

            GameObject template = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            template.name = "Plasma Projectile";
            template.transform.localScale = Vector3.one * 0.3f;
            Object.DestroyImmediate(template.GetComponent<Collider>());
            template.GetComponent<Renderer>().sharedMaterial = material;
            template.AddComponent<ProjectileInstance>();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(template, ProjectilePrefabPath);
            Object.DestroyImmediate(template);
            AppLog.Info(LogSubsystem, $"Created projectile prefab '{ProjectilePrefabPath}'.", prefab);
            return prefab.GetComponent<ProjectileInstance>();
        }

        /// <summary>Reauthors only the two generated training obstacles inside the prefab.</summary>
        private static bool ConfigureCombatTargetsOnGeneratedSegment(
            Material immuneMaterial,
            Material vulnerableMaterial,
            HealthDefinition immuneHealth,
            HealthDefinition vulnerableHealth)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(SegmentPrefabPath);
            try
            {
                Transform immune = FindDescendant(root.transform, "Low Jump Barrier");
                immune ??= FindDescendant(root.transform, "Plasma Immune Block");
                if (immune == null)
                {
                    AppLog.Error(LogSubsystem, "The generated Low Jump Barrier was not found in the segment prefab.");
                    return false;
                }

                immune.name = "Plasma Immune Block";
                immune.localPosition = new Vector3(8f, 0.6f, -3.2f);
                immune.localRotation = Quaternion.identity;
                immune.localScale = new Vector3(1f, 1.2f, 2.6f);
                immune.GetComponent<Renderer>().sharedMaterial = immuneMaterial;
                Health immuneComponent = GetOrAdd<Health>(immune.gameObject);
                immuneComponent.Configure(immuneHealth);

                Transform vulnerable = FindDescendant(root.transform, "Plasma Vulnerable Target");
                if (vulnerable == null)
                {
                    GameObject target = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    target.name = "Plasma Vulnerable Target";
                    target.transform.SetParent(root.transform);
                    vulnerable = target.transform;
                }

                vulnerable.localPosition = new Vector3(14f, 0.75f, 3.2f);
                vulnerable.localRotation = Quaternion.identity;
                vulnerable.localScale = new Vector3(1.2f, 0.75f, 1.2f);
                vulnerable.GetComponent<Renderer>().sharedMaterial = vulnerableMaterial;
                Health vulnerableComponent = GetOrAdd<Health>(vulnerable.gameObject);
                vulnerableComponent.Configure(vulnerableHealth);

                PrefabUtility.SaveAsPrefabAsset(root, SegmentPrefabPath);
                AppLog.Info(LogSubsystem, "Added immune and vulnerable combat targets to the segment prefab.", root);
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>
        /// Verifies the planner-facing segment contract before combat objects refer
        /// to it. Generation stops on invalid data rather than silently repairing it.
        /// </summary>
        private static bool ValidateSegmentDefinition()
        {
            CorridorSegmentDefinition segment =
                AssetDatabase.LoadAssetAtPath<CorridorSegmentDefinition>(SegmentDefinitionPath);
            if (segment == null)
            {
                AppLog.Error(LogSubsystem, "Basic segment definition is missing.");
                return false;
            }

            if (!segment.IsValid(out string reason))
            {
                AppLog.Error(LogSubsystem, $"Segment definition is invalid: {reason}", segment);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Wires health, weapon, projectile pool, and debug display onto the generated
        /// scene. Keeping this wiring here makes a clean checkout playable without a
        /// manual Tools-menu operation.
        /// </summary>
        private static bool ConfigureCombatScene(
            GameObject labRoot,
            ProjectileInstance projectilePrefab,
            WeaponDefinition weapon,
            HealthDefinition playerHealthDefinition)
        {
            Transform player = FindDescendant(labRoot.transform, "Player Sphere");
            if (player == null)
            {
                AppLog.Error(LogSubsystem, "Generated Player Sphere was not found.", labRoot);
                return false;
            }

            Rigidbody playerBody = player.GetComponent<Rigidbody>();
            if (playerBody != null)
            {
                playerBody.constraints = RigidbodyConstraints.FreezeRotation;
            }

            Health playerHealth = GetOrAdd<Health>(player.gameObject);
            playerHealth.Configure(playerHealthDefinition);

            Transform muzzle = FindDescendant(player, "Plasma Muzzle");
            if (muzzle == null)
            {
                GameObject muzzleObject = new GameObject("Plasma Muzzle");
                muzzleObject.transform.SetParent(player);
                muzzle = muzzleObject.transform;
            }
            muzzle.localPosition = new Vector3(0.85f, 0f, 0f);
            muzzle.localRotation = Quaternion.Euler(0f, 90f, 0f);

            Transform combatRoot = FindDescendant(labRoot.transform, "Combat Systems");
            if (combatRoot == null)
            {
                GameObject combatObject = new GameObject("Combat Systems");
                combatObject.transform.SetParent(labRoot.transform);
                combatRoot = combatObject.transform;
            }

            Transform poolRoot = FindDescendant(combatRoot, "Inactive Projectiles");
            if (poolRoot == null)
            {
                GameObject poolObject = new GameObject("Inactive Projectiles");
                poolObject.transform.SetParent(combatRoot);
                poolRoot = poolObject.transform;
            }

            ProjectilePoolFactory pool = GetOrAdd<ProjectilePoolFactory>(combatRoot.gameObject);
            pool.Configure(projectilePrefab, poolRoot, 12, 32);

            PlayerInputReader input = player.GetComponent<PlayerInputReader>();
            PlayerWeaponController weaponController = GetOrAdd<PlayerWeaponController>(player.gameObject);
            weaponController.Configure(weapon, input, muzzle, pool, CombatFaction.Player);

            CombatDebugOverlay overlay = GetOrAdd<CombatDebugOverlay>(combatRoot.gameObject);
            overlay.Configure(playerHealth, weaponController);
            return true;
        }

        /// <summary>Finds a generated object by exact name without a scene-wide runtime search.</summary>
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
                Transform result = FindDescendant(root.GetChild(index), objectName);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        /// <summary>Returns an existing generated component or adds it once.</summary>
        private static T GetOrAdd<T>(GameObject owner) where T : Component
        {
            T component = owner.GetComponent<T>();
            return component == null ? owner.AddComponent<T>() : component;
        }

        /// <summary>Creates one ScriptableObject asset and otherwise preserves its file identity.</summary>
        private static T EnsureAsset<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
            {
                return asset;
            }

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            AppLog.Info(LogSubsystem, $"Created combat asset '{path}'.", asset);
            return asset;
        }

        /// <summary>Creates a URP material with optional emission for readable plasma shots.</summary>
        private static Material EnsureRenderMaterial(
            string name,
            Color baseColor,
            float metallic,
            float smoothness,
            Color? emissionColor = null)
        {
            string path = $"{MaterialsDirectory}/{name}.mat";
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

            material = new Material(shader) { name = name };
            material.SetColor("_BaseColor", baseColor);
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Smoothness", smoothness);
            if (emissionColor.HasValue)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emissionColor.Value);
            }

            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        /// <summary>Marks generated assets once before the shared save operation.</summary>
        private static void MarkDirty(params Object[] assets)
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
