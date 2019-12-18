using UnityEngine;
using System.Collections;

public class SteamVR_LinearPointer_Touch : SteamVr_LinearPointer {

    public struct TriggerSelectionEventArgs
    {
        public GameObject selctedGameObject;
    }

    public delegate void TriggerClickedSelectionEventHandler(object sender, TriggerSelectionEventArgs e);
    public event TriggerClickedSelectionEventHandler TriggerSelecteInteraction;
    private GameObject currActivatedObject;
    

    protected override void SubscribeControllerEvents()
    {
        m_controllerEvents.TouchpadPressed += new ControllerClickedEventHandler(EnablePointer);
        m_controllerEvents.TouchpadUnPressed += new ControllerClickedEventHandler(DisablePointer);
        m_controllerEvents.TriggerClicked += new ControllerClickedEventHandler(ConfirmObjectPointer);
    }

    protected override void UnsubscribeControllerEvents()
    {
        m_controllerEvents.TouchpadPressed -= new ControllerClickedEventHandler(EnablePointer);
        m_controllerEvents.TouchpadUnPressed -= new ControllerClickedEventHandler(DisablePointer);
        m_controllerEvents.TriggerClicked -= new ControllerClickedEventHandler(ConfirmObjectPointer);
    }

    protected override void Update()
    {
        if (m_pointerBeam && isActive)
        {
            Ray pointerRayCast = new Ray(transform.position, transform.forward);
            RaycastHit pointerCollideWith;
            var rayHit = Physics.Raycast(pointerRayCast, out pointerCollideWith, m_pointerLenght, ~layersToIgnore);
            var pointerBeamLength = GetPointerBeamLenght(rayHit, pointerCollideWith);
            SetPointerTransform(pointerBeamLength, m_pointerThickness);

            if (pointerCollideWith.collider != null && pointerCollideWith.collider.gameObject != null && pointerCollideWith.collider.gameObject.tag == "OLT_Interactible")
            {
                SteamVr_Interactible interactible = pointerCollideWith.transform.gameObject.GetComponent<SteamVr_Interactible>();
                if (interactible != null && interactible.IsInteractable)
                {
                    //Check if there is a curr active object first
                    if (currActivatedObject != null)
                    {
                        //ColumnItemInfo ci = currActivatedObject.GetComponent<ColumnItemInfo>();
                        //ColumnItemInfo newCi = interactible.gameObject.GetComponent<ColumnItemInfo>();
                        //if (ci != null && newCi != null)
                        //{
                        //    if (ci.m_CurrPlacedIndex != newCi.m_CurrPlacedIndex)
                        //    {
                        SteamVr_Interactible previousinteractible = currActivatedObject.GetComponent<SteamVr_Interactible>();
                        previousinteractible.ToggleHighlight(false);
                        interactible.ToggleHighlight(true);
                        currActivatedObject = pointerCollideWith.transform.gameObject;
                        //    }
                        //    else
                        //    {
                        //        //do nothing
                        //    }
                        //}
                    }
                    else
                    {
                        interactible.ToggleHighlight(true);
                        currActivatedObject = pointerCollideWith.transform.gameObject;
                    }                    
                }
            }
            else if (currActivatedObject != null)
            {
                SteamVr_Interactible interactible = currActivatedObject.GetComponent<SteamVr_Interactible>();
                interactible.ToggleHighlight(false);
                currActivatedObject = null;
            }

            if (m_useOriginalNormal)
            {
                m_pointerTip.transform.forward = m_originalForwardDirection;
            }
        }
    }

    private void ConfirmObjectPointer(object sender, ControllerClickedEventArgs e)
    {
        if (!isActive)
            return;

        if (currActivatedObject == null)
            return;

        TurnOffBeam();

        if (TriggerSelecteInteraction != null)
        {
            TriggerSelectionEventArgs e_selected;
            e_selected.selctedGameObject = currActivatedObject;
            SteamVr_Interactible interactible = currActivatedObject.GetComponent<SteamVr_Interactible>();
            interactible.ToggleHighlight(false);
            currActivatedObject = null;
            TriggerSelecteInteraction(sender, e_selected);
        }

    }

}
