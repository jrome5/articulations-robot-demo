using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DummyMover : MonoBehaviour
{
    //Transform transform;
    public float speed = 0.0001f;

    void Update()
    {
        float inputX = Input.GetAxis("BigHandForward");
        float inputY = Input.GetAxis("BigHandHorizontal");
        float inputZ = Input.GetAxis("BigHandVertical");

        //get jointPosition along y axis
        Vector3 new_vec = transform.position;

        //increment this y position
        new_vec.x += inputX * speed;
        new_vec.y += inputY * speed;
        new_vec.z += inputZ * speed;
        transform.position = new_vec;
    }
}
