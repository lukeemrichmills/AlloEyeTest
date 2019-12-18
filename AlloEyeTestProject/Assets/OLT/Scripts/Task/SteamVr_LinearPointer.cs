
using UnityEngine;
using System.Collections;

public class SteamVr_LinearPointer : MonoBehaviour {

    [Header("Pointer settings", order = 1)]
    [SerializeField]
    protected Material m_pointerMaterial;
    [SerializeField]
    protected Color m_pointerHitColor = new Color(0.0f, 0.5f, 0.0f);
    [SerializeField]
    protected Color m_pointerMissColor = new Color(0.5f, 0.0f, 0.0f);
    [SerializeField]
    protected float m_pointerLenght = 100f;
    [SerializeField]
    protected float m_pointerThickness = 0.002f;
    [SerializeField]
    protected GameObject m_pointerTipObject = null;
    [SerializeField]
    protected LayerMask layersToIgnore = Physics.IgnoreRaycastLayer;
    [SerializeField]
    protected bool m_useOriginalNormal = true;

    //[SerializeField]
    //private Transform m_pointerOriginTransform = null;

    protected GameObject m_pointerHolder;
    protected GameObject m_pointerBeam;
    protected GameObject m_pointerTip;

    protected Vector3 m_pointerTipScale = new Vector3(0.05f, 0.05f, 0.05f);
    protected Vector3 m_pointerCursorOriginalScale = Vector3.one;

    protected Vector3 m_originalForwardDirection = Vector3.zero;

    protected Vector3 m_destinationPosition;
    protected float m_pointerContactDistance = 0.0f;
    protected Transform m_pointerContactTarget = null;
    protected RaycastHit m_pointerContactRyacastHit = new RaycastHit();
    protected Color m_currColor;

    protected bool isActive;

    protected SteamVr_ControllerEvents m_controllerEvents;

    public event InteractionClikedEventHandler TouchpadReleasedInteraction;
    public event InteractionClikedEventHandler TriggerPressedInteraction;

    public virtual bool IsActive()
    {
        return isActive;
    }

    protected virtual void SubscribeControllerEvents()
    {
        m_controllerEvents.TouchpadPressed += new ControllerClickedEventHandler(EnablePointer);
        m_controllerEvents.TouchpadUnPressed += new ControllerClickedEventHandler(DisablePointer);
        m_controllerEvents.TriggerClicked += new ControllerClickedEventHandler(ConfirmPointer);
    }

    protected virtual void UnsubscribeControllerEvents()
    {
        m_controllerEvents.TouchpadPressed -= new ControllerClickedEventHandler(EnablePointer);
        m_controllerEvents.TouchpadUnPressed -= new ControllerClickedEventHandler(DisablePointer);
        m_controllerEvents.TriggerClicked -= new ControllerClickedEventHandler(ConfirmPointer);
    }

    protected virtual void InitPointer()
    {
        m_pointerHolder = new GameObject("Pointer_Holder");
        m_pointerHolder.transform.localPosition = Vector3.zero;

        //Andrea:
        // Unsuscribing and forcing pointer to be called
        SubscribeControllerEvents();

        var tmpMaterial = Resources.Load("WorldPointer") as Material;
        if (m_pointerMaterial != null)
        {
            tmpMaterial = m_pointerMaterial;
        }

        m_pointerMaterial = new Material(tmpMaterial);
        m_pointerMaterial.color = m_pointerMissColor;

        m_pointerBeam = GameObject.CreatePrimitive(PrimitiveType.Cube);
        m_pointerBeam.transform.name = ("Pointer_Beam");
        m_pointerBeam.transform.localPosition = Vector3.zero;
        m_pointerBeam.transform.SetParent(m_pointerHolder.transform);
        m_pointerBeam.GetComponent<BoxCollider>().isTrigger = true;
        m_pointerBeam.AddComponent<Rigidbody>().isKinematic = true;
        m_pointerBeam.layer = LayerMask.NameToLayer("Ignore Raycast");

        var pointerRenderer = m_pointerBeam.GetComponent<MeshRenderer>();
        pointerRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        pointerRenderer.receiveShadows = false;
        pointerRenderer.material = m_pointerMaterial;

        if (m_pointerTipObject)
        {
            m_pointerTip = GameObject.Instantiate(m_pointerTipObject);
            if (m_useOriginalNormal)
            {
                m_originalForwardDirection = m_pointerTip.transform.forward;
            }
        }
        else
        {
            m_pointerTip = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            m_pointerTip.transform.localScale = m_pointerTipScale;
            m_pointerTip.GetComponent<Collider>().isTrigger = true;
            m_pointerTip.AddComponent<Rigidbody>().isKinematic = true;
            var pointerTipRenderer = m_pointerTip.GetComponentInChildren<MeshRenderer>();
            pointerTipRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            pointerTipRenderer.receiveShadows = false;
            pointerTipRenderer.material = m_pointerMaterial;
            if (m_useOriginalNormal)
            {
                //override for the primitive
                m_useOriginalNormal = false;
            }
        }        
        
        m_pointerCursorOriginalScale = m_pointerTip.transform.localScale;

        m_pointerTip.transform.name = ("Pointer_Tip");
        m_pointerTip.transform.SetParent(m_pointerHolder.transform);
        m_pointerTip.layer = LayerMask.NameToLayer("Ignore Raycast");

        SetPointerTransform(m_pointerLenght, m_pointerThickness);
        TogglePointer(false);
    }

    public virtual void ActivateBeam(bool activeOn)
    {       
        if (activeOn)
            TurnOnBeam();
        else
            TurnOffBeam();
    }

    protected void EnablePointer(object sender, ControllerClickedEventArgs e)
    {
        //TurnOnBeam();
    }

    protected void DisablePointer(object sender, ControllerClickedEventArgs e)
    {
        //TurnOffBeam();
        ////ACK to external subscribers
        //if (TouchpadReleasedInteraction != null)
        //{
        //    InteractionClickedEventArgs e_interaction;
        //    e_interaction.interactionPoint = m_destinationPosition;
        //    TouchpadReleasedInteraction(sender, e_interaction);
        //}
    }

    protected void ConfirmPointer(object sender, ControllerClickedEventArgs e)
    {
        if (!isActive)
            return;

        TurnOffBeam();
        ////ACK to external subscribers
        if (TriggerPressedInteraction != null)
        {
            InteractionClickedEventArgs e_interaction;
            e_interaction.interactionPoint = m_destinationPosition;
            TriggerPressedInteraction(sender, e_interaction);
        }

    }

    protected void TurnOnBeam()
    {
        TogglePointer(true);
        isActive = true;
    }

    protected void TurnOffBeam()
    {
        isActive = false;
        TogglePointer(false);
    }

    protected void TogglePointer(bool state)
    {
        if (m_pointerBeam)
        {
            m_pointerBeam.SetActive(state);
        }
        if (m_pointerTip)
        {
            m_pointerTip.SetActive(state);
        }
    }

    protected void SetPointerTransform(float setLenght, float setThickness)
    {
        var beamPosition = setLenght / (2 + 0.00001f);

        m_pointerBeam.transform.localScale = new Vector3(setThickness, setThickness, setLenght);
        m_pointerBeam.transform.localPosition = new Vector3(0.0f, 0.0f, beamPosition);
        //m_pointerTip.transform.localPosition = new Vector3(0f, 0f, setLenght - (m_pointerTip.transform.localScale.y / 2));
        m_pointerTip.transform.localPosition = new Vector3(0f, 0f, setLenght);

        m_pointerHolder.transform.position = this.transform.position;
        m_pointerHolder.transform.rotation = this.transform.rotation;
    }

    protected void OnEnable()
    {
        InitPointer();
    }

    protected void Awake()
    {
        m_controllerEvents = transform.parent.gameObject.GetComponent<SteamVr_ControllerEvents>();
    }

    // Use this for initialization
    protected virtual void Start () {
	
	}

    // Update is called once per frame
    protected virtual void Update () {

        if (m_pointerBeam && isActive)
        {
            Ray pointerRayCast = new Ray(transform.position, transform.forward);
            RaycastHit pointerCollideWith;
            var rayHit = Physics.Raycast(pointerRayCast, out pointerCollideWith, m_pointerLenght, ~layersToIgnore);
            var pointerBeamLength = GetPointerBeamLenght(rayHit, pointerCollideWith);
            SetPointerTransform(pointerBeamLength, m_pointerThickness);
            if (m_useOriginalNormal)
            {
                m_pointerTip.transform.forward = m_originalForwardDirection;
            }
        }

	}

    void OnDestroy()
    {
        UnsubscribeControllerEvents();
    }

    protected float GetPointerBeamLenght(bool hasRayHit, RaycastHit collidewith)
    {
        var actualLength = m_pointerLenght;

        if (!hasRayHit)
        {
            m_pointerContactDistance = 0f;
            m_pointerContactTarget = null;
            m_pointerContactRyacastHit = new RaycastHit();
            m_destinationPosition = Vector3.zero;
        }

        if (hasRayHit)
        {
            m_pointerContactDistance = collidewith.distance;
            m_pointerContactTarget = collidewith.transform;
            m_pointerContactRyacastHit = collidewith;
            m_destinationPosition = m_pointerTip.transform.position;
        }

        if (hasRayHit && m_pointerContactDistance < m_pointerLenght)
        {
            actualLength = m_pointerContactDistance;
        }

        return actualLength;

    }
}
