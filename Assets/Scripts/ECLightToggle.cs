using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace ECXR
{
    /// <summary>
    /// Rúbrica 4 - Interacción a distancia.
    /// Enciende y apaga una luz al seleccionar el objeto con el rayo XR.
    /// También cambia el color del indicador físico (la propia lámpara) para dar feedback.
    /// </summary>
    [RequireComponent(typeof(XRSimpleInteractable))]
    public class ECLightToggle : MonoBehaviour
    {
        [Header("Luz a controlar")]
        [Tooltip("Luz que se enciende/apaga. Si se deja vacía se busca una Light en el objeto o en sus hijos.")]
        [SerializeField] Light targetLight;

        [Header("Indicador visual")]
        [Tooltip("Renderer que cambia de color según el estado. Si se deja vacío se usa el primer Renderer del objeto.")]
        [SerializeField] Renderer indicatorRenderer;

        [SerializeField] Color onColor = new Color(1.00f, 0.86f, 0.35f);
        [SerializeField] Color offColor = new Color(0.25f, 0.25f, 0.28f);

        static readonly int k_BaseColorId = Shader.PropertyToID("_BaseColor");

        XRSimpleInteractable m_Interactable;
        MaterialPropertyBlock m_PropertyBlock;

        void Awake()
        {
            m_Interactable = GetComponent<XRSimpleInteractable>();
            m_PropertyBlock = new MaterialPropertyBlock();

            if (targetLight == null)
                targetLight = GetComponentInChildren<Light>();

            if (indicatorRenderer == null)
                indicatorRenderer = GetComponentInChildren<Renderer>();
        }

        void OnEnable()
        {
            if (m_Interactable != null)
                m_Interactable.selectEntered.AddListener(OnSelected);

            ApplyVisualState();
        }

        void OnDisable()
        {
            if (m_Interactable != null)
                m_Interactable.selectEntered.RemoveListener(OnSelected);
        }

        void OnSelected(SelectEnterEventArgs args)
        {
            if (targetLight != null)
                targetLight.enabled = !targetLight.enabled;

            ApplyVisualState();
        }

        void ApplyVisualState()
        {
            if (indicatorRenderer == null)
                return;

            bool isOn = targetLight == null || targetLight.enabled;
            indicatorRenderer.GetPropertyBlock(m_PropertyBlock);
            m_PropertyBlock.SetColor(k_BaseColorId, isOn ? onColor : offColor);
            indicatorRenderer.SetPropertyBlock(m_PropertyBlock);
        }
    }
}
