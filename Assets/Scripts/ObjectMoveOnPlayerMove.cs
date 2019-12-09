using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectMoveOnPlayerMove : MonoBehaviour
{
    public GameObject cameraHead;
    public GameObject cameraEye;
    public Vector3 shiftVector;
    public GameObject triggerAreaObject;
    public Bounds b;
    Collider col;
    Vector3 boundSize;
    bool inBounds;

    private void Awake()
    {

    }

    // Start is called before the first frame update
    void Start()
    {
        col = triggerAreaObject.GetComponent<Collider>();
        boundSize = new Vector3(col.bounds.size.x, col.bounds.size.y+4, col.bounds.size.z)  ;
        b = new Bounds(triggerAreaObject.transform.position, boundSize);
        shiftVector = new Vector3(gameObject.transform.position.x-0.14f, gameObject.transform.position.y, gameObject.transform.position.z-0.08f);
        inBounds = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (cameraEye.transform.position != new Vector3(0, 0, 0))
        {
            //Debug.Log(cameraEye.transform.position);
            if (b.Contains(cameraEye.transform.position) && inBounds == false)
            {
                ObjectMoveTrigger();
            }
        }
    }

    void ObjectMoveTrigger()
    {
        gameObject.transform.position = shiftVector;
        inBounds = true;
    }

    IEnumerator WaitCoroutine()
    {
        //yield on a new YieldInstruction that waits for 5 seconds.
        yield return new WaitForSeconds(2);
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawCube(b.center, b.size);
    }
}
