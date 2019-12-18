using UnityEngine;
using System.Collections;

public class SteamVr_Interactible : MonoBehaviour {

    [Header("Touch Interactions", order = 1)]
    public bool highlightOnTouch = true;
    public Color touchHilightColor = Color.cyan;
    private Color startingColor = Color.clear;
    private Renderer render;

    [Header("Interactions")]
    private bool isInteractable = true;
    [SerializeField]
    public bool IsInteractable { get { return isInteractable; } set { isInteractable = value; } }
                                             
    void Start () {
        render = GetComponentInChildren<Renderer>();
        StoreinitialColor();
    }

    void StoreinitialColor()
    {
        //Renderer renderer = GetComponent<Renderer>();
        if (render.material.HasProperty("_Color"))
        {
            startingColor = render.material.color;
        }
    }

    public void ToggleHighlight(bool activation)
    {
        //Renderer renderer = GetComponent<Renderer>();
        if (render.material.HasProperty("_Color"))
        {
            if (activation)
                render.material.color = touchHilightColor;
            else
                render.material.color = startingColor;
        }
    }

}
