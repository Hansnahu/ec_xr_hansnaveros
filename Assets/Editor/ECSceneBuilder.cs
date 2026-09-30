#if UNITY_EDITOR
using System.IO;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace ECXR.EditorTools
{
    /// <summary>
    /// Constructor automático de la escena del examen EC_XR_NaverosHans.
    /// Genera el escenario, la iluminación, los objetos 3D, los objetos agarrables,
    /// las interacciones a distancia (rayo) y el reto libre (aparición de objetos + contador en UI espacial).
    ///
    /// Uso: menú superior -> EC -> Construir escena EC_XR_NaverosHans
    /// </summary>
    public static class ECSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/EC_XR_NaverosHans.unity";
        const string k_MaterialsFolder = "Assets/Materials";
        const string k_ScenesFolder = "Assets/Scenes";

        const string k_XrOriginPrefab =
            "Assets/Samples/XR Interaction Toolkit/3.6.1/Starter Assets/Prefabs/XR Origin (XR Rig).prefab";
        const string k_SimulatorPrefab =
            "Assets/Samples/XR Interaction Toolkit/3.6.1/XR Interaction Simulator/XR Interaction Simulator.prefab";
        const string k_TeleportAreaPrefab =
            "Assets/Samples/XR Interaction Toolkit/3.6.1/Starter Assets/DemoAssets/Prefabs/Teleport/Teleport Area.prefab";

        static readonly Vector3 k_RoomSize = new Vector3(12f, 3f, 12f);

        [MenuItem("EC/Construir escena EC_XR_NaverosHans")]
        public static void BuildScene()
        {
            EnsureFolder(k_MaterialsFolder);
            EnsureFolder(k_ScenesFolder);

            var materials = CreateMaterials();

            // --- Escena nueva, sin cámara ni luz por defecto ---
            // Se guardan las escenas abiertas para evitar un diálogo modal al crear la nueva.
            EditorSceneManager.SaveOpenScenes();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Se guarda de inmediato con su ruta final: instanciar prefabs en una escena sin ruta
            // abre el diálogo modal "Save Scene" y bloquea la automatización.
            EditorSceneManager.SaveScene(scene, ScenePath);

            var environment = new GameObject("Environment").transform;
            var interactables = new GameObject("Interactables").transform;
            var gameplay = new GameObject("Gameplay").transform;

            BuildLighting();
            BuildRoom(environment, materials);
            BuildTable(environment, materials);
            BuildGrabbables(interactables, materials);
            var lamp = BuildLamp(interactables, materials);
            BuildColorStation(interactables, materials);
            var spawner = BuildSpawnStation(interactables, materials, out var spawnCounter);
            BuildHud(gameplay, spawnCounter);
            var teleportArea = BuildTeleportArea(environment);
            InstantiateXrRig();
            InstantiateSimulator();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            RegisterSceneInBuildSettings();

            Debug.Log("[EC] Escena construida y guardada en " + ScenePath +
                      " | TeleportArea=" + teleportArea.name +
                      " | Lamp=" + lamp.name +
                      " | Spawner=" + spawner.name);
        }

        // ------------------------------------------------------------------
        // Iluminación
        // ------------------------------------------------------------------
        static void BuildLighting()
        {
            var sun = new GameObject("Directional Light (Sun)");
            sun.transform.SetPositionAndRotation(new Vector3(0f, 6f, 0f), Quaternion.Euler(50f, -30f, 0f));
            var light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.96f, 0.90f);
            light.intensity = 1.1f;
            light.shadows = LightShadows.Soft;

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.20f, 0.21f, 0.24f);
            RenderSettings.ambientIntensity = 1f;

            // Luz de relleno para que el escenario no quede plano
            var fill = new GameObject("Fill Light");
            fill.transform.position = new Vector3(-3f, 2.6f, -3f);
            var fillLight = fill.AddComponent<Light>();
            fillLight.type = LightType.Point;
            fillLight.color = new Color(0.65f, 0.78f, 1f);
            fillLight.intensity = 0.9f;
            fillLight.range = 12f;
        }

        // ------------------------------------------------------------------
        // Escenario: piso, límites visuales y techo
        // ------------------------------------------------------------------
        static void BuildRoom(Transform parent, Materials m)
        {
            var floor = CreatePrimitive("Floor", PrimitiveType.Cube, parent, new Vector3(0f, -0.05f, 0f),
                new Vector3(k_RoomSize.x, 0.1f, k_RoomSize.z), m.floor, true);
            floor.name = "Floor";

            float halfX = k_RoomSize.x * 0.5f;
            float halfZ = k_RoomSize.z * 0.5f;
            float halfY = k_RoomSize.y * 0.5f;

            CreatePrimitive("Wall_North", PrimitiveType.Cube, parent, new Vector3(0f, halfY, halfZ),
                new Vector3(k_RoomSize.x, k_RoomSize.y, 0.1f), m.wall, true);
            CreatePrimitive("Wall_South", PrimitiveType.Cube, parent, new Vector3(0f, halfY, -halfZ),
                new Vector3(k_RoomSize.x, k_RoomSize.y, 0.1f), m.wall, true);
            CreatePrimitive("Wall_East", PrimitiveType.Cube, parent, new Vector3(halfX, halfY, 0f),
                new Vector3(0.1f, k_RoomSize.y, k_RoomSize.z), m.wall, true);
            CreatePrimitive("Wall_West", PrimitiveType.Cube, parent, new Vector3(-halfX, halfY, 0f),
                new Vector3(0.1f, k_RoomSize.y, k_RoomSize.z), m.wall, true);

            // Sin techo: es un entorno de entrenamiento XR y así el escenario queda visible
            // en la vista de escena, en las capturas de evidencia y en el video demostrativo.
        }

        static void BuildTable(Transform parent, Materials m)
        {
            CreatePrimitive("Table", PrimitiveType.Cube, parent, new Vector3(0f, 0.4f, 1.6f),
                new Vector3(2.6f, 0.8f, 1.2f), m.table, true);
        }

        // ------------------------------------------------------------------
        // Rúbrica 3: objetos manipulables con Rigidbody + XR Grab Interactable
        // ------------------------------------------------------------------
        static void BuildGrabbables(Transform parent, Materials m)
        {
            CreateGrabbable("Objeto_Cubo", PrimitiveType.Cube, parent, new Vector3(-0.75f, 1.05f, 1.6f),
                Vector3.one * 0.3f, m.red, 0.8f);

            CreateGrabbable("Objeto_Esfera", PrimitiveType.Sphere, parent, new Vector3(0f, 1.05f, 1.6f),
                Vector3.one * 0.3f, m.blue, 0.8f);

            CreateGrabbable("Objeto_Llave", PrimitiveType.Cylinder, parent, new Vector3(0.75f, 1.05f, 1.6f),
                new Vector3(0.1f, 0.16f, 0.1f), m.metal, 0.6f);
        }

        // ------------------------------------------------------------------
        // Rúbrica 4: interacción a distancia (rayo) -> encender / apagar luz
        // ------------------------------------------------------------------
        static GameObject BuildLamp(Transform parent, Materials m)
        {
            var root = new GameObject("Lamp");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = new Vector3(-4.6f, 0f, 2.6f);

            CreatePrimitive("Post", PrimitiveType.Cylinder, root.transform, new Vector3(0f, 1.05f, 0f),
                new Vector3(0.12f, 1.05f, 0.12f), m.metal, false);

            var head = CreatePrimitive("Lamp_Head (rayo para encender/apagar)", PrimitiveType.Sphere,
                root.transform, new Vector3(0f, 2.15f, 0f), Vector3.one * 0.42f, m.yellow, false);

            var lampLight = head.AddComponent<Light>();
            lampLight.type = LightType.Point;
            lampLight.color = new Color(1f, 0.88f, 0.55f);
            lampLight.intensity = 3.2f;
            lampLight.range = 10f;
            lampLight.shadows = LightShadows.Soft;

            head.AddComponent<XRSimpleInteractable>();
            var toggle = head.AddComponent<ECLightToggle>();
            SetSerializedField(toggle, "targetLight", lampLight);
            SetSerializedField(toggle, "indicatorRenderer", head.GetComponent<Renderer>());

            return root;
        }

        // ------------------------------------------------------------------
        // Rúbrica 4: interacción a distancia (rayo) -> cambiar color
        // ------------------------------------------------------------------
        static void BuildColorStation(Transform parent, Materials m)
        {
            CreatePrimitive("Pedestal_Color", PrimitiveType.Cube, parent, new Vector3(-3.4f, 0.5f, 2.6f),
                new Vector3(0.7f, 1f, 0.7f), m.wall, true);

            var target = CreatePrimitive("Objeto_Color (rayo para cambiar color)", PrimitiveType.Sphere,
                parent, new Vector3(-3.4f, 1.32f, 2.6f), Vector3.one * 0.45f, m.green, false);

            target.AddComponent<XRSimpleInteractable>();
            var cycler = target.AddComponent<ECColorCycler>();
            SetSerializedField(cycler, "targetRenderer", target.GetComponent<Renderer>());
        }

        // ------------------------------------------------------------------
        // Rúbrica 5: reto libre -> aparición de objetos + contador
        // ------------------------------------------------------------------
        static GameObject BuildSpawnStation(Transform parent, Materials m, out TMP_Text counterLabel)
        {
            CreatePrimitive("Pedestal_Spawn", PrimitiveType.Cube, parent, new Vector3(3.4f, 0.5f, 2.6f),
                new Vector3(0.7f, 1f, 0.7f), m.wall, true);

            var button = CreatePrimitive("Boton_Aparicion (rayo para generar objetos)", PrimitiveType.Cube,
                parent, new Vector3(3.4f, 1.1f, 2.6f), new Vector3(0.42f, 0.18f, 0.42f), m.orange, false);

            var spawnPoint = new GameObject("SpawnPoint");
            spawnPoint.transform.SetParent(parent, false);
            spawnPoint.transform.localPosition = new Vector3(3.4f, 1.75f, 2.6f);

            var container = new GameObject("SpawnedObjects");
            container.transform.SetParent(parent, false);

            button.AddComponent<XRSimpleInteractable>();
            var spawner = button.AddComponent<ECObjectSpawner>();
            SetSerializedField(spawner, "spawnPoint", spawnPoint.transform);
            SetSerializedField(spawner, "spawnedParent", container.transform);
            SetMaterialsArray(spawner, "materials", new[] { m.red, m.blue, m.green, m.yellow, m.metal });

            counterLabel = null; // se asigna en BuildHud
            m_PendingSpawner = spawner;
            return button;
        }

        static ECObjectSpawner m_PendingSpawner;

        // ------------------------------------------------------------------
        // UI espacial con el contador
        // ------------------------------------------------------------------
        static void BuildHud(Transform parent, TMP_Text counterLabel)
        {
            var hud = new GameObject("HUD (UI espacial)", typeof(Canvas), typeof(CanvasScaler),
                typeof(TrackedDeviceGraphicRaycaster));
            hud.transform.SetParent(parent, false);
            // Rotación identidad: el Canvas queda legible para un observador que mira desde -Z (la sala).
            hud.transform.SetPositionAndRotation(new Vector3(0f, 1.85f, 5.88f), Quaternion.identity);
            hud.transform.localScale = Vector3.one * 0.0045f;

            var canvas = hud.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            var hudRect = hud.GetComponent<RectTransform>();
            hudRect.sizeDelta = new Vector2(560f, 320f);

            var background = new GameObject("Background", typeof(Image));
            background.transform.SetParent(hud.transform, false);
            var backgroundRect = background.GetComponent<RectTransform>();
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;
            background.GetComponent<Image>().color = new Color(0.05f, 0.06f, 0.09f, 0.72f);

            var title = CreateHudText(hud.transform, "Title", "EC XR - Naveros Huaman Hans Eduard",
                new Vector2(0f, 108f), new Vector2(520f, 70f), 46f, TextAlignmentOptions.Center);
            title.color = new Color(0.85f, 0.92f, 1f);

            counterLabel = CreateHudText(hud.transform, "CounterText", "Objetos: 0 / 12",
                new Vector2(0f, 6f), new Vector2(520f, 150f), 60f, TextAlignmentOptions.Center);
            counterLabel.color = Color.white;

            var help = CreateHudText(hud.transform, "HelpText",
                "Rayo + gatillo: cambiar color / encender luz / generar objeto",
                new Vector2(0f, -110f), new Vector2(520f, 80f), 30f, TextAlignmentOptions.Center);
            help.color = new Color(0.75f, 0.80f, 0.88f);

            if (m_PendingSpawner != null)
            {
                SetSerializedField(m_PendingSpawner, "counterLabel", counterLabel);
                m_PendingSpawner = null;
            }
        }

        static TMP_Text CreateHudText(Transform parent, string name, string text, Vector2 anchoredPosition,
            Vector2 size, float fontSize, TextAlignmentOptions alignment)
        {
            var go = new GameObject(name, typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            var label = go.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.alignment = alignment;
            if (label.font == null && TMP_Settings.defaultFontAsset != null)
                label.font = TMP_Settings.defaultFontAsset;

            return label;
        }

        // ------------------------------------------------------------------
        // Locomoción: zona de teletransporte
        // ------------------------------------------------------------------
        static GameObject BuildTeleportArea(Transform parent)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(k_TeleportAreaPrefab);
            if (prefab == null)
            {
                Debug.LogWarning("[EC] No se encontró el prefab Teleport Area, se omite el teletransporte.");
                return null;
            }

            var area = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            area.name = "Teleport Area";
            area.transform.position = new Vector3(0f, 0.16f, 0f);
            area.transform.localScale = new Vector3(1.16f, 1f, 2.32f);

            // Zona de teletransporte invisible: conserva el collider y el componente
            // TeleportationArea, pero no tapa el material del piso.
            var areaRenderer = area.GetComponentInChildren<MeshRenderer>();
            if (areaRenderer != null)
                areaRenderer.enabled = false;

            return area;
        }

        // ------------------------------------------------------------------
        // XR: XR Origin + XR Interaction Simulator + XR Interaction Manager
        // ------------------------------------------------------------------
        static void InstantiateXrRig()
        {
            var manager = new GameObject("XR Interaction Manager");
            manager.AddComponent<XRInteractionManager>();

            var originPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(k_XrOriginPrefab);
            if (originPrefab == null)
            {
                Debug.LogError("[EC] No se encontró el prefab XR Origin (XR Rig). Revisa el sample Starter Assets.");
                return;
            }

            var origin = (GameObject)PrefabUtility.InstantiatePrefab(originPrefab);
            origin.name = "XR Origin (XR Rig)";
            origin.transform.SetPositionAndRotation(new Vector3(0f, 0f, -3.5f), Quaternion.identity);

            var xrOrigin = origin.GetComponent<XROrigin>();
            if (xrOrigin == null)
                xrOrigin = origin.AddComponent<XROrigin>();

            if (xrOrigin != null)
                xrOrigin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;

            CreateEventSystem();
        }

        static void InstantiateSimulator()
        {
            var simulatorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(k_SimulatorPrefab);
            if (simulatorPrefab == null)
            {
                Debug.LogWarning("[EC] No se encontró el prefab del XR Interaction Simulator. " +
                                 "Importa el sample desde Package Manager > XR Interaction Toolkit.");
                return;
            }

            var simulator = (GameObject)PrefabUtility.InstantiatePrefab(simulatorPrefab);
            simulator.name = "XR Interaction Simulator (editor)";
        }

        static void CreateEventSystem()
        {
            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<XRUIInputModule>();
        }

        // ------------------------------------------------------------------
        // Utilidades
        // ------------------------------------------------------------------
        sealed class Materials
        {
            public Material floor;
            public Material wall;
            public Material table;
            public Material red;
            public Material blue;
            public Material green;
            public Material yellow;
            public Material orange;
            public Material metal;
        }

        static Materials CreateMaterials()
        {
            return new Materials
            {
                floor = CreateMaterial("M_Floor", new Color(0.22f, 0.23f, 0.26f), 0f, 0.25f),
                wall = CreateMaterial("M_Wall", new Color(0.66f, 0.67f, 0.70f), 0f, 0.20f),
                table = CreateMaterial("M_Table", new Color(0.45f, 0.30f, 0.19f), 0f, 0.35f),
                red = CreateMaterial("M_Red", new Color(0.88f, 0.24f, 0.24f), 0.05f, 0.45f),
                blue = CreateMaterial("M_Blue", new Color(0.22f, 0.55f, 0.92f), 0.05f, 0.45f),
                green = CreateMaterial("M_Green", new Color(0.32f, 0.82f, 0.38f), 0.05f, 0.45f),
                yellow = CreateMaterial("M_Yellow", new Color(0.98f, 0.82f, 0.24f), 0.05f, 0.50f),
                orange = CreateMaterial("M_Orange", new Color(0.95f, 0.55f, 0.18f), 0.05f, 0.45f),
                metal = CreateMaterial("M_Metal", new Color(0.66f, 0.68f, 0.72f), 0.85f, 0.80f),
            };
        }

        static Material CreateMaterial(string name, Color color, float metallic, float smoothness)
        {
            var path = k_MaterialsFolder + "/" + name + ".mat";
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");

            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
            }

            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            material.color = color;
            if (material.HasProperty("_Metallic"))
                material.SetFloat("_Metallic", metallic);
            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", smoothness);

            EditorUtility.SetDirty(material);
            return material;
        }

        static GameObject CreatePrimitive(string name, PrimitiveType type, Transform parent, Vector3 localPosition,
            Vector3 localScale, Material material, bool isStatic)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;
            go.isStatic = isStatic;

            var renderer = go.GetComponent<Renderer>();
            if (renderer != null && material != null)
                renderer.sharedMaterial = material;

            return go;
        }

        static GameObject CreateGrabbable(string name, PrimitiveType type, Transform parent, Vector3 localPosition,
            Vector3 localScale, Material material, float mass)
        {
            var go = CreatePrimitive(name, type, parent, localPosition, localScale, material, false);

            var body = go.AddComponent<Rigidbody>();
            body.mass = mass;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            var grab = go.AddComponent<XRGrabInteractable>();
            grab.throwOnDetach = true;
            grab.throwSmoothingDuration = 0.25f;

            return go;
        }

        static void SetSerializedField(Object target, string fieldName, Object value)
        {
            if (target == null)
                return;

            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(fieldName);
            if (property == null)
            {
                Debug.LogWarning("[EC] Campo no encontrado: " + fieldName + " en " + target.name);
                return;
            }

            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        static void SetMaterialsArray(Object target, string fieldName, Material[] materials)
        {
            if (target == null)
                return;

            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(fieldName);
            if (property == null || !property.isArray)
            {
                Debug.LogWarning("[EC] Array no encontrado: " + fieldName + " en " + target.name);
                return;
            }

            property.arraySize = materials.Length;
            for (int i = 0; i < materials.Length; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = materials[i];

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            var parent = Path.GetDirectoryName(path);
            var leaf = Path.GetFileName(path);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(leaf))
                return;

            parent = parent.Replace('\\', '/');
            if (!AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);

            AssetDatabase.CreateFolder(parent, leaf);
        }

        static void RegisterSceneInBuildSettings()
        {
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (scenes.Exists(s => s.path == ScenePath))
                return;

            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        [MenuItem("EC/Validar escena EC_XR_NaverosHans")]
        public static void ValidateScene()
        {
            var scene = SceneManager.GetActiveScene();
            var roots = scene.GetRootGameObjects();
            var report = new System.Text.StringBuilder();
            report.AppendLine("Escena activa: " + scene.name + " (" + scene.path + ")");
            report.AppendLine("Objetos raíz: " + roots.Length);

            foreach (var root in roots)
                report.AppendLine("  - " + root.name);

            var grabbables = Object.FindObjectsByType<XRGrabInteractable>(FindObjectsSortMode.None);
            var simple = Object.FindObjectsByType<XRSimpleInteractable>(FindObjectsSortMode.None);
            var teleport = Object.FindObjectsByType<TeleportationArea>(FindObjectsSortMode.None);
            report.AppendLine("XRGrabInteractable: " + grabbables.Length);
            report.AppendLine("XRSimpleInteractable: " + simple.Length);
            report.AppendLine("TeleportationArea: " + teleport.Length);

            Debug.Log("[EC] Validación\n" + report);
        }
    }
}
#endif
