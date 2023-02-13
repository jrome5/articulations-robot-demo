using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Random = UnityEngine.Random;

public class BasicAgent : Agent
{
    private GameObject agent;
    public GameObject gripper;
    public GameObject target;
    public float speed = 0.1f;
    int timesteps = 0;
    bool request = false;
    float target_thresh = 0.1f;
    private float[] meshObs;
    private (byte[], byte[]) camObs;
    private float displace;

    public override void Initialize()
    {
        meshObs = new float[961]; //need to get this automatically
        agent = transform.GetChild(0).gameObject;
        Debug.Log(agent.name);
    }


    //full obs state: cube positions xyz * 3, gripper position, finger length
    public override void CollectObservations(VectorSensor sensor)
    {
        //for mesh obs
        sensor.AddObservation(target.transform.position);
        sensor.AddObservation(agent.transform.position);
        sensor.AddObservation(gripper.transform.position);

        for(int i =0; i < meshObs.Length; i++)
        {
            sensor.AddObservation(meshObs[i]);
        }
        //for camera obs
        // var RGB = camObs.Item1;
        // var depth = camObs.Item2;
        // // Debug.Log("rgb: " + RGB.Length);
        // // Debug.Log("depth: " + depth.Length);
        // for(int i =0; i < RGB.Length; i++)
        // {
        //     float obs = RGB[i]/255f;
        //     sensor.AddObservation(RGB[i]);
        // }
        // for(int i =0; i < depth.Length; i++)
        // {
        //     float obs = depth[i]/255f;
        //     sensor.AddObservation(obs);
        //     //Debug.Log(depth[i]);
        // }
        //Application.Quit();
        //Debug.Log(meshObs.ToString());
    }

    public void GetCameraObs((byte[], byte[]) obs)
    {
        camObs = obs;
    }

    public void GetMeshObs(float[] obs)
    {
        meshObs = obs;
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        var X_displace = speed * Mathf.Clamp(actionBuffers.ContinuousActions[0], -1f, 1f);
        var Y_displace = speed * Mathf.Clamp(actionBuffers.ContinuousActions[1], -1f, 1f);
        var Z_displace = speed * Mathf.Clamp(actionBuffers.ContinuousActions[2], -1f, 1f);
        var twist = speed * Mathf.Clamp(actionBuffers.ContinuousActions[3], -1f, 1f);
        var pos = agent.transform.position;
        pos.x += X_displace;
        pos.y += Y_displace;
        pos.z += Z_displace;

        var bounds = 2f;
        pos.x = Mathf.Max(-bounds, pos.x);
        pos.x = Mathf.Min(bounds, pos.x);
        pos.y = Mathf.Max(0.1f, pos.y);
        pos.y = Mathf.Min(bounds, pos.y);
        pos.z = Mathf.Max(-bounds, pos.z);
        pos.z = Mathf.Min(bounds, pos.z);

        RotateGripper(twist);

        agent.transform.position = pos;
        var dist = Vector3.Distance(pos, target.transform.position);
        if(dist < target_thresh)
        {
            //target_thresh = Mathf.Max(target_thresh*0.9f, 0.1f);
            AddReward(1f);
            EndEpisode();
            Application.LoadLevel(0);
        }
        AddReward(-dist/100f);

        if(timesteps > 3000)
        {
            AddReward(-10f);
            EndEpisode();
            Application.LoadLevel(0);
        }
        timesteps += 1;
        //AddReward(-0.01f);
    }

    public void RotateGripper(float twist)
    {
        ArticulationBody articulation = gripper.GetComponent<ArticulationBody>();
        var anchor_rot = articulation.parentAnchorRotation;
        displace += twist;
        var target_rot = Quaternion.Euler(0, 270+displace, 270);
        //rotate arm
        var step = 10f * Time.deltaTime;
        anchor_rot = Quaternion.RotateTowards(anchor_rot, target_rot, step);
        articulation.parentAnchorRotation = anchor_rot; 
    }
     

    public void FixedUpdate()
    {
        if(request)
        {
            RequestDecision();
        }
        //WaitTimeInference();
    }

    // public override void EndEpisode()
    // {
    //     Application.LoadLevel(0);
    // }

    public void StartRequesting()
    {
        request = true;
    }

    public override void OnEpisodeBegin()
    {
        Debug.Log("episode");
        timesteps = 0;
        request = false;
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = Input.GetAxis("BigHandForward");
        continuousActionsOut[1] = Input.GetAxis("BigHandHorizontal");
        continuousActionsOut[2] = Input.GetAxis("BigHandVertical");
        continuousActionsOut[3] = Input.GetAxis("Twist");
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
