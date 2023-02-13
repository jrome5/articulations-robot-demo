using UnityEngine;
using System.Collections;

public class TrackObject: MonoBehaviour 
{
    public Transform target;
    public float smooth= 5.0f;
    void  Update ()
    {
        // Rotate the camera every frame so it keeps looking at the target
        transform.LookAt(target);

        // Same as above, but setting the worldUp parameter to Vector3.left in this example turns the camera on its side
        // transform.LookAt(target, Vector3.left);
    } 

} 