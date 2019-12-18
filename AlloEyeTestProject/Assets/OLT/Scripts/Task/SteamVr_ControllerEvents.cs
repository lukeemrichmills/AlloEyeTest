using UnityEngine;
using System.Collections;

public struct ControllerClickedEventArgs
{
    public uint controllerIndex;
    public float buttonPressure;
    public Vector2 touchpadAxis;
}

public struct InteractionClickedEventArgs
{
    public Vector3 interactionPoint;
}

public delegate void ControllerClickedEventHandler(object sender, ControllerClickedEventArgs e);
public delegate void InteractionClikedEventHandler(object sender, InteractionClickedEventArgs e);

public class SteamVr_ControllerEvents : MonoBehaviour {

    public enum ButtonAlias
    {
        Trigger,
        Application_Menu
    }

    [Header("Controller Interactions", order = 1)]
    public bool triggerPressed = false;
    public bool touchpadPressed = false;

    public event ControllerClickedEventHandler TriggerClicked;
    public event ControllerClickedEventHandler TriggerUnclicked;

    public event ControllerClickedEventHandler TouchpadPressed;
    public event ControllerClickedEventHandler TouchpadUnPressed;
        
    private SteamVR_TrackedObject m_trackedController;
    private SteamVR_Controller.Device m_device;
    private uint m_controllerIndex;

    #region Events

    public virtual void OnTriggerClicked(ControllerClickedEventArgs e)
    {
        if (TriggerClicked != null)
            TriggerClicked(this, e);
    }

    public virtual void OnTriggerUnclicked(ControllerClickedEventArgs e)
    {
        if (TriggerUnclicked != null)
            TriggerUnclicked(this, e);
    }

    public virtual void OnTouchpadPressed(ControllerClickedEventArgs e)
    {
        if (TouchpadPressed != null)
        {
            TouchpadPressed(this, e);
        }
    }

    public virtual void OnTouchpadUnpressed(ControllerClickedEventArgs e)
    {
        if (TouchpadUnPressed != null)
        {
            TouchpadUnPressed(this, e);
        }
    }

    private ControllerClickedEventArgs SetButtonEvent(ref bool currButtonState, bool value, float buttonPressure)
    {
        currButtonState = value;
        ControllerClickedEventArgs e;
        e.controllerIndex = m_controllerIndex;
        e.buttonPressure = buttonPressure;
        e.touchpadAxis = m_device.GetAxis();
        return e;
    }

    #endregion Events

    void Awake()
    {
        m_trackedController = GetComponent<SteamVR_TrackedObject>();
    }

    // Use this for initialization
    void Start () {
        m_controllerIndex = (uint)m_trackedController.index;
        m_device = SteamVR_Controller.Input((int)m_controllerIndex);
    }
	
	// Update is called once per frame
	void Update () {

        //Put this here because tracked controller can change dynamically
        m_controllerIndex = (uint)m_trackedController.index;
        m_device = SteamVR_Controller.Input((int)m_controllerIndex);

        if (m_device.GetTouchDown(Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger))
        {
            OnTriggerClicked(SetButtonEvent(ref triggerPressed, true, 1.0f));
        }
        else if (m_device.GetTouchUp(Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger))
        {
            OnTriggerUnclicked(SetButtonEvent(ref triggerPressed, false, 0.0f));
        }
        if (m_device.GetPressDown(SteamVR_Controller.ButtonMask.Touchpad))
        {
            OnTouchpadPressed(SetButtonEvent(ref touchpadPressed, true, 1.0f));
        }
        else if (m_device.GetPressUp(SteamVR_Controller.ButtonMask.Touchpad))
        {
            OnTouchpadUnpressed(SetButtonEvent(ref touchpadPressed, false, 1.0f));
        }
	}

    private void OnDestroy()
    {
        TriggerClicked = null;
        TriggerUnclicked = null;
    }
}
