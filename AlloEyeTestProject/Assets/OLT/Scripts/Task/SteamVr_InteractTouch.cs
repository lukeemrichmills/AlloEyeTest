using UnityEngine;
using System.Collections;

public class SteamVr_InteractTouch : MonoBehaviour {

    private SteamVr_ControllerEvents m_controllerEvents;
    public ControllerClickedEventHandler TriggerButtonPressed;
    GameObject touchedObject = null;

    // Use this for initialization
    void Start () {
        m_controllerEvents = GetComponent<SteamVr_ControllerEvents>();
        m_controllerEvents.TriggerClicked += HandleTriggerInput;
        SphereCollider collider = this.gameObject.AddComponent<SphereCollider>();
        collider.radius = 0.06f;
        collider.center = new Vector3(0.0f, -0.05f, 0.0f);
        collider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider collider)
    {
        if (touchedObject == null && CheckInteraction(collider.gameObject))
        {
            touchedObject = collider.gameObject;
            SteamVr_Interactible interactible = touchedObject.GetComponent<SteamVr_Interactible>();
            interactible.ToggleHighlight(true);
        }
    }

    private void OnTriggerExit(Collider collider)
    {
        if (touchedObject != null && touchedObject == collider.gameObject)
        {
            DisableTouchedObject();
        }
    }

    private void DisableTouchedObject()
    {
        SteamVr_Interactible interactible = touchedObject.GetComponent<SteamVr_Interactible>();
        interactible.ToggleHighlight(false);
        touchedObject = null;
    }

    private bool CheckInteraction(GameObject checkedObject)
    {
        return checkedObject != null && checkedObject.GetComponent<SteamVr_Interactible>() != null && checkedObject.GetComponent<SteamVr_Interactible>().IsInteractable;        
    }

    private void HandleTriggerInput(object sender, ControllerClickedEventArgs e)
    {
#if PIT
        if (touchedObject != null)
        {
            TaskManager.Instance.AcknoledgePressedFlag(touchedObject.GetComponent<PathIntegrationTask.FlagController>().m_index.ToString());
            DisableTouchedObject();
        }
#elif OLT
#endif
    }
}
