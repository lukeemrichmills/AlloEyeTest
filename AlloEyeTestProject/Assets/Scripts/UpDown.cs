using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UpDown : MonoBehaviour
{
    public float smoothing = 1f;
    public void Lifting(Vector3 UpPosition)
    {
        //StartCoroutine(LiftUpCoroutine(UpPosition)); //buggy
        transform.position = UpPosition;
    }
    public void Dropping(Vector3 DownPosition)
    {
        //StartCoroutine(DropDownCoroutine(DownPosition)); //buggy
        transform.position = DownPosition;
    }
    //IEnumerator LiftUpCoroutine(Vector3 UpPosition)
    //{
    //    while (Vector3.Distance(transform.position, UpPosition) > 0)
    //    {
    //        transform.position = Vector3.Lerp(transform.position, UpPosition, smoothing * Time.deltaTime);
    //        yield return null;
    //    }
    //}
    //IEnumerator DropDownCoroutine(Vector3 DownPosition)
    //{
    //    while (Vector3.Distance(transform.position, DownPosition) > 0)
    //    {
    //        transform.position = Vector3.Lerp(transform.position, DownPosition, smoothing * Time.deltaTime);
    //        yield return null;
    //    }
    //}
}
