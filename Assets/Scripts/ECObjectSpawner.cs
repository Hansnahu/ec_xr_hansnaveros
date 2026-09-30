using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace ECXR
{
    /// <summary>
    /// Rúbrica 5 - Reto libre (aparición de objetos + contador en UI espacial).
    /// Cada activación del rayo instancia un objeto 3D agarrable en el punto de aparición.
    /// Al llegar al límite, la siguiente activación limpia la zona y reinicia el contador.
    /// </summary>
    [RequireComponent(typeof(XRSimpleInteractable))]
    public class ECObjectSpawner : MonoBehaviour
    {
        [Header("Aparición")]
        [Tooltip("Punto donde aparecen los objetos. Si se deja vacío se usa este mismo objeto.")]
        [SerializeField] Transform spawnPoint;

        [Tooltip("Padre opcional para mantener la jerarquía ordenada.")]
        [SerializeField] Transform spawnedParent;

        [Tooltip("Materiales que se van alternando entre los objetos generados.")]
        [SerializeField] Material[] materials;

        [SerializeField] float spawnRadius = 0.10f;
        [SerializeField] float minScale = 0.16f;
        [SerializeField] float maxScale = 0.24f;

        [Header("Límite y contador")]
        [SerializeField] int maxObjects = 12;

        [Tooltip("Texto del contador en la UI espacial.")]
        [SerializeField] TMP_Text counterLabel;

        readonly List<GameObject> m_Spawned = new List<GameObject>();

        static readonly PrimitiveType[] k_Types =
        {
            PrimitiveType.Cube,
            PrimitiveType.Sphere,
            PrimitiveType.Cylinder,
        };

        XRSimpleInteractable m_Interactable;

        public int SpawnedCount => m_Spawned.Count;

        void Awake()
        {
            m_Interactable = GetComponent<XRSimpleInteractable>();
        }

        void OnEnable()
        {
            if (m_Interactable != null)
                m_Interactable.selectEntered.AddListener(OnSelected);

            UpdateCounter();
        }

        void OnDisable()
        {
            if (m_Interactable != null)
                m_Interactable.selectEntered.RemoveListener(OnSelected);
        }

        void OnSelected(SelectEnterEventArgs args)
        {
            if (m_Spawned.Count >= maxObjects)
                ClearSpawned();
            else
                SpawnOne();
        }

        /// <summary>Instancia un objeto 3D con física e interacción de agarre.</summary>
        public GameObject SpawnOne()
        {
            var type = k_Types[m_Spawned.Count % k_Types.Length];
            var spawned = GameObject.CreatePrimitive(type);
            spawned.name = "Objeto_" + (m_Spawned.Count + 1).ToString("00");

            var origin = spawnPoint != null ? spawnPoint : transform;
            spawned.transform.SetParent(spawnedParent, true);
            spawned.transform.position = origin.position + Random.insideUnitSphere * spawnRadius;
            spawned.transform.rotation = Random.rotation;
            spawned.transform.localScale = Vector3.one * Random.Range(minScale, maxScale);

            // Física
            var body = spawned.AddComponent<Rigidbody>();
            body.mass = 0.5f;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            // Interacción de agarre (XR Interaction Toolkit)
            var grab = spawned.AddComponent<XRGrabInteractable>();
            grab.throwOnDetach = true;
            grab.throwSmoothingDuration = 0.25f;

            // Material alternado
            if (materials != null && materials.Length > 0)
            {
                var renderer = spawned.GetComponent<Renderer>();
                if (renderer != null)
                    renderer.sharedMaterial = materials[m_Spawned.Count % materials.Length];
            }

            m_Spawned.Add(spawned);
            UpdateCounter();
            return spawned;
        }

        /// <summary>Elimina todos los objetos generados y reinicia el contador.</summary>
        public void ClearSpawned()
        {
            for (int i = m_Spawned.Count - 1; i >= 0; i--)
            {
                if (m_Spawned[i] != null)
                    Destroy(m_Spawned[i]);
            }

            m_Spawned.Clear();
            UpdateCounter();
        }

        void UpdateCounter()
        {
            if (counterLabel == null)
                return;

            counterLabel.text = m_Spawned.Count >= maxObjects
                ? "Objetos: " + m_Spawned.Count + " / " + maxObjects + "\nLIMITE ALCANZADO\n(activa el rayo para reiniciar)"
                : "Objetos: " + m_Spawned.Count + " / " + maxObjects;
        }
    }
}
