using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace ECXR
{
    /// <summary>
    /// Rúbrica 4 - Interacción a distancia.
    /// Cambia el color del objeto al seleccionarlo con el rayo XR (sin tocarlo físicamente).
    /// Se suscribe al evento selectEntered de su propio XRSimpleInteractable.
    /// </summary>
    [RequireComponent(typeof(XRSimpleInteractable))]
    public class ECColorCycler : MonoBehaviour
    {
        [Header("Objetivo")]
        [Tooltip("Renderer cuyo color se va a modificar. Si se deja vacío se usa el primer Renderer del objeto.")]
        [SerializeField] Renderer targetRenderer;

        [Header("Paleta de colores")]
        [SerializeField]
        Color[] palette =
        {
            new Color(0.90f, 0.25f, 0.25f), // rojo
            new Color(0.25f, 0.60f, 0.95f), // azul
            new Color(0.35f, 0.85f, 0.40f), // verde
            new Color(0.98f, 0.82f, 0.22f), // amarillo
        };

        [Tooltip("Índice de la paleta con el que arranca la escena.")]
        [SerializeField] int startIndex;

        static readonly int k_BaseColorId = Shader.PropertyToID("_BaseColor");

        XRSimpleInteractable m_Interactable;
        MaterialPropertyBlock m_PropertyBlock;
        int m_Index;

        void Awake()
        {
            m_Interactable = GetComponent<XRSimpleInteractable>();
            if (targetRenderer == null)
                targetRenderer = GetComponentInChildren<Renderer>();

            m_PropertyBlock = new MaterialPropertyBlock();
            m_Index = Mathf.Max(0, startIndex);
        }

        void OnEnable()
        {
            if (m_Interactable != null)
                m_Interactable.selectEntered.AddListener(OnSelected);

            ApplyColor();
        }

        void OnDisable()
        {
            if (m_Interactable != null)
                m_Interactable.selectEntered.RemoveListener(OnSelected);
        }

        void OnSelected(SelectEnterEventArgs args)
        {
            if (palette == null || palette.Length == 0)
                return;

            m_Index = (m_Index + 1) % palette.Length;
            ApplyColor();
        }

        void ApplyColor()
        {
            if (targetRenderer == null || palette == null || palette.Length == 0)
                return;

            m_Index = Mathf.Clamp(m_Index, 0, palette.Length - 1);
            targetRenderer.GetPropertyBlock(m_PropertyBlock);
            m_PropertyBlock.SetColor(k_BaseColorId, palette[m_Index]);
            targetRenderer.SetPropertyBlock(m_PropertyBlock);
        }
    }
}
