using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Random = UnityEngine.Random;


// state -> camera
// actions -> dx(-1->1), dy(-1->1), dz(-1->1), gripper(0,1]
public class GripperClothAgent : Agent
{
    [Header("Specific to Ball3D")]
    public float speed = 0.01f;
    EnvironmentParameters m_ResetParams;
    public GameObject target;
    public GameObject hand;
    private GameObject finger;

    //reward variables
    float max_force = 2000f;
    int timesteps = 0;
    int max_timesteps = 2000;
    float ep_rew = 0f;

    public override void Initialize()
    {
        finger = hand.transform.GetChild(1).gameObject;
        m_ResetParams = Academy.Instance.EnvironmentParameters;
    }


    //full obs state: cube positions xyz * 3, gripper position, finger length
    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(hand.GetComponent<Transform>().position);
        //Debug.Log("Hand: " + hand.GetComponent<Transform>().position);
        ArticulationBody finger_ab = finger.GetComponent<ArticulationBody>();
        var drive_target = finger_ab.jointPosition[0];
        sensor.AddObservation(drive_target);
        //Debug.Log("Finger: " + drive_target);
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        var actionX = speed * Mathf.Clamp(actionBuffers.ContinuousActions[0], -1f, 1f);
        var actionY = speed * Mathf.Clamp(actionBuffers.ContinuousActions[1], -1f, 1f);
        var actionZ = speed * Mathf.Clamp(actionBuffers.ContinuousActions[2], -1f, 1f);

        //move art body parent anchor
        ArticulationBody articulation = hand.GetComponent<ArticulationBody>();

        //get jointPosition along y axis
        Vector3 anchor = articulation.parentAnchorPosition;

        //increment this y position
        anchor.x += actionX;
        anchor.y += actionY;
        anchor.z += actionZ;
        articulation.parentAnchorPosition = anchor;

        //move fingers
        PincherController pincherController = hand.GetComponent<PincherController>();
        pincherController.gripState = GripStateForInput(actionBuffers.ContinuousActions[3]);

        var ab = finger.GetComponent<ArticulationBody>();
        List<float> m_ExternalForces = new List<float>();
        ab.GetDriveForces(m_ExternalForces);
        // Debug.Log(m_ExternalForces);
        // Debug.Log(m_ExternalForces.Count); //xyz -> 1.6K with cube in hand
        bool dangerous_force = false;
        foreach(var f in m_ExternalForces)
        {
            if(f >= max_force)
            {
                dangerous_force = true;
                break;
            }
        }

        if(dangerous_force)
        {
            AddReward(-1f);
            ep_rew -= 1;
            EndEpisode();
        }
    }

    static GripState GripStateForInput(float input)
    {
        if (input > 0)
        {
            return GripState.Closing;
        }
        else if (input < 0)
        {
            return GripState.Opening;
        }
        else
        {
            return GripState.Fixed;
        }
    }

    public override void OnEpisodeBegin()
    {
        //move art body parent anchor
        ArticulationBody articulation = hand.GetComponent<ArticulationBody>();

        //get jointPosition along y axis
        Vector3 anchor = new Vector3();
        articulation.parentAnchorPosition = anchor;
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = Input.GetAxis("BigHandForward");
        continuousActionsOut[1] = Input.GetAxis("BigHandHorizontal");
        continuousActionsOut[2] = Input.GetAxis("BigHandVertical");
        continuousActionsOut[3] = Input.GetAxis("Fingers");
    }

    public static bool NearlyEqual(float a, float b, float epsilon=1e-3f) 
    {
        float absA = Mathf.Abs(a);
        float absB = Mathf.Abs(b);
        float diff = Mathf.Abs(a - b);

        if (a == b) 
        { // shortcut, handles infinities
            return true;
        } 
        // else if (a == 0 || b == 0 || absA + absB < Float.MIN_NORMAL)
        // {
        //     // a or b is zero or both are extremely close to it
        //     // relative error is less meaningful here
        //     return diff < (epsilon * Float.MIN_NORMAL);
        // }
        else 
        { // use relative error
            return diff / (absA + absB) < epsilon;
        }
    }
}
