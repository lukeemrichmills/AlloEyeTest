using UnityEngine;

namespace ViveSR.anipal.Eye
{
    [RequireComponent(typeof(Renderer), typeof(Collider))]
    public class ArrayObjects : MonoBehaviour //CHANGE NAME TO FocusProperties OR SOMETHING
    {
        private Renderer Renderer;
        
        int sphereCounter = 0;
        int cylinderCounter = 0;
        int cubeCounter = 0;
        int capsuleCounter = 0;
        
        private void Awake()
        {
            Renderer = GetComponent<Renderer>(); //renderer of this object     
            

        }

        public void Focus()
        {
            Renderer.material.SetColor("_Color", Color.red); //turns red when Focus is called
            
            //Debug.Log("Focus");  
            if (gameObject.name == "Sphere")
            {
                sphereCounter++;
                Debug.Log("Sphere Counter = " + sphereCounter);
            }                
            else if (gameObject.name == "Cylinder")
            {
                cylinderCounter++;
                Debug.Log("Cylinder Counter = " + cylinderCounter);
            }
            else if (gameObject.name == "Cube")
            {
                cubeCounter++;
                Debug.Log("Cube Counter = " + cubeCounter);
            }
            else
            {
                capsuleCounter++;
                Debug.Log("capsule Counter = " + capsuleCounter);
            }
        }
        public void Unfocus()
        {
            Renderer.material.SetColor("_Color", Color.blue); //turns blue when Unfocus is called
            //Debug.Log("Unfocus");
        }
    }
}
