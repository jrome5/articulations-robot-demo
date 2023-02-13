using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GripperXYZdemo : MonoBehaviour
{
    public GameObject hand;
    GameObject l_finger;
    GameObject r_finger;
    public float speed = 0.0001f;

    void Start()
    {
        l_finger = hand.transform.GetChild(1).gameObject;
        r_finger = hand.transform.GetChild(2).gameObject;
    }

    void Update()
    {
        float inputX = Input.GetAxis("BigHandForward");
        float inputY = Input.GetAxis("BigHandHorizontal");
        float inputZ = Input.GetAxis("BigHandVertical");

        //move art body parent anchor
        ArticulationBody articulation = hand.GetComponent<ArticulationBody>();

        //get jointPosition along y axis
        Vector3 anchor = articulation.parentAnchorPosition;

        //increment this y position
        anchor.x += inputX * speed;
        anchor.y += inputY * speed;
        anchor.z += inputZ * speed;
        articulation.parentAnchorPosition = anchor;
    }
}
