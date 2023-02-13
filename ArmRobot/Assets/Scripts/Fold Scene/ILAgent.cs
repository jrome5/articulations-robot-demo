using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Random = UnityEngine.Random;
using UnityEngine.SceneManagement;
using UnityEngine;

namespace Obi
{
public class ILAgent : Agent
{
    public GameObject agent;
    public float speed = 0.01f;
    int timesteps = 0;
    public GameObject cloth_prefab;
    ObiCloth cloth;
    public GameObject cloth_solver;
    private int[] corners;
    private bool ready = false;
    GameObject cloth_object;

    public override void Initialize()
    {
        cloth_object = Instantiate(cloth_prefab, new Vector3(0, 0.3f, 0), Quaternion.identity);
        cloth_object.transform.parent = cloth_solver.transform;
        corners = new int[4];
        // Debug.Log(agent.name);
        //GameObject cloth_object = GameObject.FindGameObjectsWithTag("Cloth")[0];
        cloth = cloth_object.GetComponent<ObiCloth>();
        ObiActorBlueprint blueprint = cloth.sourceBlueprint;
        ObiParticleGroup corner_group = blueprint.groups[0];
        int i = 0;
        foreach(var index in corner_group.particleIndices)
        {
            corners[i] = index;
            i += 1;
        }
    }

        // //full obs state: cube positions xyz * 3, gripper position, finger length
    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(agent.transform.position.x);
        sensor.AddObservation(agent.transform.position.y);
        sensor.AddObservation(agent.transform.position.z);
        ObiSolver solver = cloth.solver;
        float[] obs = new float[solver.positions.count];
        for(int i = 0; i < solver.positions.count; i++)
        {
            Vector4 vertexLocalPosition = solver.positions[i];
            Vector3 vertexGlobalPosition = solver.transform.TransformPoint(vertexLocalPosition);
            sensor.AddObservation(vertexGlobalPosition.x);
            sensor.AddObservation(vertexGlobalPosition.y);
            sensor.AddObservation(vertexGlobalPosition.z);
        }     
    }


    // public override void OnActionReceived(ActionBuffers actionBuffers)
    // {
    //     var X_displace = speed * Mathf.Clamp(actionBuffers.ContinuousActions[0], -1f, 1f);
    //     var Y_displace = speed * Mathf.Clamp(actionBuffers.ContinuousActions[2], -1f, 1f);
    //     var Z_displace = speed * Mathf.Clamp(actionBuffers.ContinuousActions[1], -1f, 1f);
    //     var grab = Mathf.Clamp(actionBuffers.ContinuousActions[3], -1f, 1f);

    //     if(grab <= -0.5f)
    //     {
    //         agent.SendMessage("Grab");
    //     }
    //     else if(grab >= 0.5f)
    //     {
    //         agent.SendMessage("Release");
    //     }

    //     var pos = agent.transform.position;
    //     pos.x += X_displace;
    //     pos.y += Y_displace;
    //     pos.z += Z_displace;

    //     agent.transform.position = pos;

    //     var bounds = 1.5f;
    //     if(pos.x < -bounds || pos.x > bounds)
    //     {
    //         AddReward(-100);
    //         EndEpisode();
    //     }
    //     if(pos.y < 0.0 || pos.y > bounds)
    //     {
    //         AddReward(-100);
    //         EndEpisode();
    //     }
    //     if(pos.z < -bounds || pos.z > bounds)
    //     {
    //         AddReward(-100);
    //         EndEpisode();
    //     }
    //     //a dummy reward could be the distance between the corner vertex pairs: 2,960 468,593 or [0],[3] [1],[2]
    //     Vector3[] positions = new Vector3[4];
    //     int i = 0;
    //     foreach(var index in corners)
    //     {
    //         ObiSolver solver = cloth.solver;
    //         Vector4 position = solver.positions[index];
    //         positions[i] = solver.transform.TransformPoint(position);
    //         i += 1;
    //     }
    //     //match pairs
    //     var pair1_dist = Vector3.Distance(positions[0], positions[3]);
    //     var pair2_dist = Vector3.Distance(positions[1], positions[2]);
    //     var reward = (2.5f - (pair1_dist + pair2_dist))/100f;
    //     AddReward(reward);
    //     // Debug.Log(reward);
    //     if(reward > 0.021f)
    //     {
    //         AddReward(10f);
    //         EndEpisode();
    //     }
    //     timesteps += 1;
    //     if(timesteps > 5000)
    //     {
    //         EndEpisode();
    //     }
    // }


    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        int x_movement = actionBuffers.DiscreteActions[0];
        int y_movement = actionBuffers.DiscreteActions[2];
        int z_movement = actionBuffers.DiscreteActions[1];
        // Get the action index for jumping
        int gripper_action = actionBuffers.DiscreteActions[3];

        int directionX = 0, directionY = 0, directionZ = 0;

        // Look up the index in the movement action list:
        if (x_movement == 0) { directionX = -1; }
        if (x_movement == 1) { directionX = 0; }
        if (x_movement == 2) { directionX = 1; }
        if (y_movement == 0) { directionY = -1; }
        if (y_movement == 1) { directionY = 0; }
        if (y_movement == 2) { directionY = 1; }
        if (z_movement == 0) { directionZ = -1; }
        if (z_movement == 1) { directionZ = 0; }
        if (z_movement == 2) { directionZ = 1; }
        
        var pos = agent.transform.position;
        pos.x += speed * directionX;
        pos.y += speed * directionY;
        pos.z += speed * directionZ;

        agent.transform.position = pos;

        //Grab or dont grab
        if(gripper_action == 0)
        {
            agent.SendMessage("Grab");
        }
        else if(gripper_action == 2)
        {
            agent.SendMessage("Release");
        }

        CalculateReward();
    }

    public void CalculateReward()
    {
        var pos = agent.transform.position;
        var bounds = 1.5f;
        if(pos.x < -bounds || pos.x > bounds)
        {
            AddReward(-100);
            EndEpisode();
        }
        if(pos.y < 0.0 || pos.y > bounds)
        {
            AddReward(-100);
            EndEpisode();
        }
        if(pos.z < -bounds || pos.z > bounds)
        {
            AddReward(-100);
            EndEpisode();
        }
        //a dummy reward could be the distance between the corner vertex pairs: 2,960 468,593 or [0],[3] [1],[2]
        Vector3[] positions = new Vector3[4];
        int i = 0;
        foreach(var index in corners)
        {
            ObiSolver solver = cloth.solver;
            Vector4 position = solver.positions[index];
            positions[i] = solver.transform.TransformPoint(position);
            i += 1;
        }
        //match pairs
        var pair1_dist = Vector3.Distance(positions[0], positions[3]);
        var pair2_dist = Vector3.Distance(positions[1], positions[2]);
        var reward = (2.5f - (pair1_dist + pair2_dist))/100f;
        AddReward(reward);
        // Debug.Log(reward);
        if(reward > 0.021f)
        {
            AddReward(10f);
            EndEpisode();
        }
        timesteps += 1;
        if(timesteps > 5000)
        {
            EndEpisode();
        }
    }

    public override void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
    {
        var pos = agent.transform.position;
        var bounds = 1.5f
        if(pos.x < -bounds)
        {
            actionMask.SetActionEnabled(0, 0, false);
        }
        if(pos.x > bounds)
        {
            actionMask.SetActionEnabled(0, 2, false);
        }
        if(pos.y < 0.0f)
        {
            actionMask.SetActionEnabled(2, 0, false);
        }
        if(pos.y > bounds)
        {
            actionMask.SetActionEnabled(2, 2, false);
        }
        if(pos.z < -bounds)
        {
            actionMask.SetActionEnabled(1, 0, false);
        }
        if(pos.z > bounds)
        {
            actionMask.SetActionEnabled(1, 2, false);
        }
    }

    public override void OnEpisodeBegin()
    {
        agent.SendMessage("Release");
        //Destroy cloth
        Destroy(cloth_object);
        //instatiate new
        cloth_object = Instantiate(cloth_prefab, new Vector3(0, 0.3f, 0), Quaternion.identity);
        cloth_object.transform.parent = cloth_solver.transform;
        //assign references
        cloth = cloth_object.GetComponent<ObiCloth>();
        //reset position of ball
        var bounds = 1f;
        agent.transform.position = new Vector3(UnityEngine.Random.Range(-bounds, bounds), 0.5f, UnityEngine.Random.Range(-bounds, bounds));
        // Debug.Log("episode");
        timesteps = 0;
        // request = false;
    
        //randomise position of ball
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        // var continuousActionsOut = actionsOut.ContinuousActions;
        // continuousActionsOut[0] = Input.GetAxis("BigHandHorizontal");
        // continuousActionsOut[1] = Input.GetAxis("BigHandForward");
        // continuousActionsOut[2] = Input.GetAxis("BigHandVertical");
        // continuousActionsOut[3] = Input.GetAxis("Fingers");

        //x movement
        var discreteActionsOut = actionsOut.DiscreteActions;
        if(Input.GetKey("a")){ discreteActionsOut[0] = 0; }
        else if(Input.GetKey("d")){ discreteActionsOut[0] = 2;}
        else{ discreteActionsOut[0] = 1;}
        
        //z movement
        if(Input.GetKey("w")){ discreteActionsOut[1] = 0; }
        else if(Input.GetKey("s")){ discreteActionsOut[1] = 2;}
        else{ discreteActionsOut[1] = 1;}

        //y movement
        if(Input.GetKey("q")){ discreteActionsOut[2] = 0; }
        else if(Input.GetKey("e")){ discreteActionsOut[2] = 2;}
        else{ discreteActionsOut[2] = 1;}

        //grippers
        if(Input.GetKey("z")){ discreteActionsOut[3] = 0; }
        else if(Input.GetKey("x")){ discreteActionsOut[3] = 2;}
        else{ discreteActionsOut[3] = 1;}
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
        else 
        { // use relative error
            return diff / (absA + absB) < epsilon;
        }
    }
}
}