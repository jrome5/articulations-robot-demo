using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Random = UnityEngine.Random;


// state -> camera
// actions -> dx(-1->1), dy(-1->1), dz(-1->1), gripper(0,1]
public class GripperAgent : Agent
{
    [Header("Specific to Ball3D")]
    public float speed = 0.01f;
    EnvironmentParameters m_ResetParams;
    public GameObject cubes;
    public GameObject hand;
    public GameObject finger;
    Vector3[] cube_init_positions;

    //reward variables
    float max_force = 2000f;
    float existential_penalty = 1e-3f;
    int timesteps = 0;
    int max_timesteps = 2000;
    float gripper_rew_weight = -0.0001f;
    float cube_dist_weight = 0.0001f;
    float ep_rew = 0f;

    public override void Initialize()
    {
        cube_init_positions = new Vector3[3];
        int i = 0;
        Transform[] allChildren = cubes.GetComponentsInChildren<Transform>();
        //TODO -> find better way of accessing children transforms
        //List<Transform> children = new();
        foreach(Transform t in allChildren)
        {
            if(i == 0)
            {
                i++;
                continue;
            }
            cube_init_positions[i-1] = t.position;
            i++;
        }
        m_ResetParams = Academy.Instance.EnvironmentParameters;
        SetResetParameters();
    }


    //full obs state: cube positions xyz * 3, gripper position, finger length
    public override void CollectObservations(VectorSensor sensor)
    {
        Transform[] allChildren = cubes.GetComponentsInChildren<Transform>();
        int i = 0;
        foreach(Transform t in allChildren)
        {
            if(i == 0)
            {
                i++;
                continue;
            }
            sensor.AddObservation(t.position);
            //Debug.Log("Cube: " + t.position);

        }
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
        else
        {
            //check stacked progress
            int i = 0;
            float dist_2d = 0f;
            float gripper_dist_rew = 0f;
            Transform[] allChildren = cubes.GetComponentsInChildren<Transform>();
            int stacked_blocks = 0;
            var finger_pos = finger.GetComponent<Transform>().position;
            foreach(Transform t in allChildren)
            {
                if(i == 0)
                {
                    i++;
                    continue;
                }
                //Debug.Log(i);
                //first check proximity in X and Z coordinates of cubes (small reward max 0.2 total)
                dist_2d += Mathf.Abs(t.position.x);
                dist_2d += Mathf.Abs(t.position.z);

                gripper_dist_rew += gripper_rew_weight * (1f/Mathf.Abs(t.position.x - finger_pos.x));
                gripper_dist_rew += gripper_rew_weight * (1f/Mathf.Abs(t.position.y - finger_pos.y));
                gripper_dist_rew += gripper_rew_weight * (1f/Mathf.Abs(t.position.z - finger_pos.z));

                //check y coords of cubes equal to 0.65 (0.3 rew) and 1.05 (1.0 rew and end eps)
                if(NearlyEqual(t.position.y, 0.65f))
                {
                    stacked_blocks++;
                }
                else if(NearlyEqual(t.position.y, 1.05f))
                {
                    stacked_blocks++;
                }
                i++;
            }
            if(stacked_blocks == 2)
            {
                AddReward(1f);
                ep_rew += 1;
                EndEpisode();
            }
            else if(stacked_blocks == 1)
            {
                AddReward(0.3f);
                ep_rew += 0.3f;
                Debug.Log("stacked 1");
            }
            else
            {
                var dist_reward = gripper_dist_rew + cube_dist_weight* (1f/dist_2d) - existential_penalty;
                AddReward(dist_reward);
                ep_rew += dist_reward;
            }
        }
        timesteps++;
        if(timesteps == max_timesteps)
        {
            timesteps = 0;
            EndEpisode();
        }



        // var actionZ = 2f * Mathf.Clamp(actionBuffers.ContinuousActions[0], -1f, 1f);
        // var actionX = 2f * Mathf.Clamp(actionBuffers.ContinuousActions[1], -1f, 1f);

        // if ((gameObject.transform.rotation.z < 0.25f && actionZ > 0f) ||
        //     (gameObject.transform.rotation.z > -0.25f && actionZ < 0f))
        // {
        //     gameObject.transform.Rotate(new Vector3(0, 0, 1), actionZ);
        // }

        // if ((gameObject.transform.rotation.x < 0.25f && actionX > 0f) ||
        //     (gameObject.transform.rotation.x > -0.25f && actionX < 0f))
        // {
        //     gameObject.transform.Rotate(new Vector3(1, 0, 0), actionX);
        // }
        // if ((ball.transform.position.y - gameObject.transform.position.y) < -2f ||
        //     Mathf.Abs(ball.transform.position.x - gameObject.transform.position.x) > 3f ||
        //     Mathf.Abs(ball.transform.position.z - gameObject.transform.position.z) > 3f)
        // {
        //     SetReward(-1f);
        //     EndEpisode();
        // }
        // else
        // {
        //     SetReward(0.1f);
        // }

        //Rewards:
        // +1 for full stack (end episode)
        // -1 for high forces (end episode)
        // +small amount for box proximity to each other
        // +small for box height above 0.3
        // -small existential penalty
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
        //Debug.Log("Ep rew: " + ep_rew);
        ep_rew = 0f;
        //reset positions of cubes
        int i = 0;
        Transform[] allChildren = cubes.GetComponentsInChildren<Transform>();
        foreach(Transform t in allChildren)
        {
            if(i == 0)
            {
                i++;
                continue;
            }
            //Debug.Log(i);
            t.position = cube_init_positions[i-1];
            t.rotation = Quaternion.Euler(0, 0, 0);
            i++;
        }
        //move art body parent anchor
        ArticulationBody articulation = hand.GetComponent<ArticulationBody>();

        //get jointPosition along y axis
        Vector3 anchor = new Vector3();
        articulation.parentAnchorPosition = anchor;


        // gameObject.transform.rotation = new Quaternion(0f, 0f, 0f, 0f);
        // gameObject.transform.Rotate(new Vector3(1, 0, 0), Random.Range(-10f, 10f));
        // gameObject.transform.Rotate(new Vector3(0, 0, 1), Random.Range(-10f, 10f));
        // m_BallRb.velocity = new Vector3(0f, 0f, 0f);
        // ball.transform.position = new Vector3(Random.Range(-1.5f, 1.5f), 4f, Random.Range(-1.5f, 1.5f))
        //     + gameObject.transform.position;
        // //Reset the parameters when the Agent is reset.
        // SetResetParameters();
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = Input.GetAxis("BigHandForward");
        continuousActionsOut[1] = Input.GetAxis("BigHandHorizontal");
        continuousActionsOut[2] = Input.GetAxis("BigHandVertical");
        continuousActionsOut[3] = Input.GetAxis("Fingers");
    }

    public void SetBall()
    {
        // //Set the attributes of the ball by fetching the information from the academy
        // m_BallRb.mass = m_ResetParams.GetWithDefault("mass", 1.0f);
        // var scale = m_ResetParams.GetWithDefault("scale", 1.0f);
        // ball.transform.localScale = new Vector3(scale, scale, scale);
    }

    public void SetResetParameters()
    {
        // SetBall();
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
