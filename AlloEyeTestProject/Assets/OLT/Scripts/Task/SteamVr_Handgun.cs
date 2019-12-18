using UnityEngine;
using System.Collections;

public class SteamVr_Handgun : MonoBehaviour {

    [SerializeField] private GameObject m_shotPrefab;
    [SerializeField]
    private Transform m_muzzlePosition;
    private SteamVr_ControllerEvents m_controllerEvents;

    // Use this for initialization
    void Start () {
        m_controllerEvents = GetComponent<SteamVr_ControllerEvents>();       
    }
	
	// Update is called once per frame
	void Update () {
	
	}

    private void HandleFireInput(object sender, ControllerClickedEventArgs e)
    {
        Fire();
    }

    public void ActivateFiregun()
    {
        m_controllerEvents.TriggerClicked += HandleFireInput;
    }

    public void DisableFiregun()
    {
        m_controllerEvents.TriggerClicked -= HandleFireInput;
    }

    private void Fire()
    {
        GameObject go = GameObject.Instantiate(m_shotPrefab, m_muzzlePosition.position, m_muzzlePosition.rotation) as GameObject;
        GameObject.Destroy(go, 3f);
    }

    void OnDisable()
    {
        if(m_controllerEvents != null)
            m_controllerEvents.TriggerClicked -= HandleFireInput;
    }
}
