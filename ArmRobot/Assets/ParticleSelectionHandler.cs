using System;
using System.IO;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Obi
{
    [RequireComponent(typeof(LineRenderer))]
    [RequireComponent(typeof(ObiParticlePicker))]
    public class ParticleSelectionHandler : MonoBehaviour
    {
        public bool capture_data = true;
        public GameObject positionSphere;
        public GameObject gripper;
        public GameObject hand;
        public float speed = 0.03f;
        public Camera cam;
        public bool point_grabbed = false;
        public GameObject agent;


        private float y_offset = 0.16f;
        private Vector3 gripper_target;
        private bool has_target = false;
        private bool grippers_closed = false;
        private ObiParticlePicker picker;
        private ObiParticlePicker.ParticlePickEventArgs pickArgs;
        private int picks = 0;
        private int first_target_index;
        private Vector3 second_target;
        private int second_target_index;

        private string csv_path = "/home/jack/Unity/Unity-ComputerVisionSim/Captures/Cloth/rotations.csv";

        private string date;
        private string filename;
        private int rotations;
        private float lift_height = 3.5f;

        private ObiParticleGroup group;
        
        private List<int> vertex_indexes = new List<int>();
        private List<int> inflated_indexes = new List<int>();
  

        void Start()
        {
            date = GetDateTime();   
            GetIndexes();
            SelectRandomPick();
        }

        void GetIndexes()
        {
            //get the 4 corners
            ObiCloth cloth = GetComponent<ObiCloth>();
            ObiActorBlueprint blueprint = cloth.sourceBlueprint;
            ObiParticleGroup corner_group = blueprint.groups[1];
            vertex_indexes = corner_group.particleIndices;
        }

        void SelectRandomPick()
        {
            System.Random rnd = new System.Random();
            var index = vertex_indexes[rnd.Next(vertex_indexes.Count)];
            Debug.Log("random: " + index);
            gripper_target = calculateIndexPosition(index);
            gripper_target.y = 0.08f + y_offset;
            CalculateSecondPick(index);
            has_target = true;
            picks++;        
            Debug.Log(gripper_target);
        }

        void OnEnable()
        {
            picker = GetComponent<ObiParticlePicker>();
            picker.OnParticlePicked.AddListener(Picker_OnParticleDragged);
            picker.OnParticleDragged.AddListener(Picker_OnParticleDragged);
            picker.OnParticleReleased.AddListener(Picker_OnParticleReleased);
        }

        void OnDisable()
        {
            picker.OnParticlePicked.RemoveListener(Picker_OnParticleDragged);
            picker.OnParticleDragged.RemoveListener(Picker_OnParticleDragged);
            picker.OnParticleReleased.RemoveListener(Picker_OnParticleReleased);
        }

        void GetVerticesObs()
        {
            ObiSolver solver = picker.solver;
            float[] obs = new float[solver.positions.count];
            for(int i = 0; i < solver.positions.count; i++)
            {
                Vector4 vertexLocalPosition = solver.positions[i];
                Vector3 vertexGlobalPosition = solver.transform.TransformPoint(vertexLocalPosition);
                obs[i] = Vector3.Distance(vertexGlobalPosition, second_target);
            }
            agent.SendMessage("GetMeshObs", obs);
        }

        void FixedUpdate()
        {
            if(has_target)
            {
                GrabFirstPoint();
               //add sphere to second point
            }
            else
            {
                if(point_grabbed)
                {
                    // ArticulationBody articulation = hand.GetComponent<ArticulationBody>();
                    // var anchor_rot = articulation.parentAnchorRotation;
                    // var target_rot = Quaternion.Euler(0, 270+rotations*60, 270);
                    // var target_met = anchor_rot == target_rot;
                    // if(target_met)
                    // {
                    //     rotations++;
                    //     Debug.Log(rotations);
                    //     if(capture_data)
                    //     {
                    //         filename = date + rotations;
                    //         Capture();
                    //         WriteData();
                    //         Debug.Log("image captured");
                    //     }
                    // }

                    // if(rotations == 6)
                    // {
                    //     rotations = 0;
                    //     Application.LoadLevel(0);
                    //     return;
                    // }       

                    // //rotate arm
                    // var step = 10f * Time.deltaTime;
                    // anchor_rot = Quaternion.RotateTowards(anchor_rot, target_rot, step);
        
                    // articulation.parentAnchorRotation = anchor_rot;             
                   // Debug.Log("reset");
                    // GrabSecondPoint();
                }
            }
            positionSphere.transform.position = calculateIndexPosition(second_target_index);
            //GetVerticesObs();
        }

        string GetDateTime()
        {
            var dt = DateTime.Now;
            return dt.ToString("yyyyMMdd-hmmss-tt"); //e.g. 06/18/2021 12:44 PM
        }

        void GrabFirstPoint()
        {
            Vector3 displacement = gripper_target - gripper.transform.position;
            //displacement.y +=y_offset;
            var actionX = speed * Mathf.Clamp(displacement.x, -2f, 2f);
            var actionY = speed * Mathf.Clamp(displacement.y , -1f, 1f);
            var actionZ = speed * Mathf.Clamp(displacement.z, -2f, 2f);

            //move art body parent anchor
            ArticulationBody articulation = hand.GetComponent<ArticulationBody>();

            //get jointPosition along y axis
            Vector3 anchor = articulation.parentAnchorPosition;

            //do y last
            if(NearlyEqual(displacement.x, 0f) && NearlyEqual(displacement.z, 0f))
            {
                if(NearlyEqual(displacement.y, 0f))
                {
                    //grasp
                    PincherController pincherController = hand.GetComponent<PincherController>();
                    grippers_closed = pincherController.grip > 0.21;
                    
                    if(grippers_closed)
                    {
                        //send message here for agent to grab particle
                       agent.SendMessage("StartRequesting");
                        //if target is reached at 2f then the cloth has been grasped and lifted. we can return
                        if(NearlyEqual(gripper_target.y, lift_height))
                        {
                            has_target = false;
                            point_grabbed = true;
                            return;
                        }
                        gripper_target.y = lift_height;
                    }
                    else
                    {
                        pincherController.gripState = GripState.Closing;
                    }
                }
                else
                {
                    anchor.y += actionY;
                }
            }
            else
            {
                anchor.x += actionX;
                anchor.z += actionZ;
            }

            articulation.parentAnchorPosition = anchor;
        }

        //THIS NEEDS A BETTER METHOD
        //FIND CLOSEST Z disp
        //THEN CLOSEST X disp
        void CalculateSecondPick(int pickIndex)
        {
            ObiSolver solver = picker.solver;
            Vector4 position = solver.positions[pickIndex];
            Vector3 pickPosition = solver.transform.TransformPoint(position);

            //find desired coords for new point by adding 2.0f to X and Z then find closest vertex            
            var cloth_size = 2.0f;//2.0f;
            //Vector3 desired_coord = new Vector3(pickPosition.x + cloth_size, pickPosition.y, pickPosition.z);
            Vector3[] desired_coords = new Vector3[2];
            desired_coords[0] = new Vector3(pickPosition.x + cloth_size, pickPosition.y, pickPosition.z);
            desired_coords[1] = new Vector3(pickPosition.x - cloth_size, pickPosition.y, pickPosition.z);
            var closest_dist = 1000f;
            for(int i = 0; i < solver.positions.count; i++)
            {
                Vector4 secondLocalPosition = solver.positions[i];
                Vector3 secondPickPosition = solver.transform.TransformPoint(secondLocalPosition);
                foreach(Vector3 point in desired_coords)
                {
                    var dist = Vector3.Distance(secondPickPosition, point);
                    if(dist < closest_dist)
                    {
                        second_target = secondPickPosition;
                        second_target_index = i;
                        closest_dist = dist;
                    }
                }
            }
            Debug.Log(second_target_index);
            positionSphere.transform.position = second_target;
        }

        // void CalculateSecondPick(int pickIndex)
        // {
        //     ObiSolver solver = picker.solver;
        //     Vector4 position = solver.positions[pickIndex];
        //     Vector3 pickPosition = solver.transform.TransformPoint(position);

        //     //find desired coords for new point by adding 2.0f to X and Z then find closest vertex            
        //     var cloth_size = 2.0f;//2.0f;
        //     //Vector3 desired_coord = new Vector3(pickPosition.x + cloth_size, pickPosition.y, pickPosition.z);
        //     Vector3[] desired_coords = new Vector3[4];
        //     desired_coords[0] = new Vector3(pickPosition.x + cloth_size, pickPosition.y, pickPosition.z);
        //     desired_coords[1] = new Vector3(pickPosition.x - cloth_size, pickPosition.y, pickPosition.z);
        //     desired_coords[2] = new Vector3(pickPosition.x, pickPosition.y, pickPosition.z + cloth_size);
        //     desired_coords[3] = new Vector3(pickPosition.x, pickPosition.y, pickPosition.z - cloth_size);
        //     var closest_dist = 1000f;
        //     for(int i = 0; i < solver.positions.count; i++)
        //     {
        //         Vector4 secondLocalPosition = solver.positions[i];
        //         Vector3 secondPickPosition = solver.transform.TransformPoint(secondLocalPosition);
        //         foreach(Vector3 point in desired_coords)
        //         {
        //             var dist = Vector3.Distance(secondPickPosition, point);
        //             if(dist < closest_dist)
        //             {
        //                 second_target = secondPickPosition;
        //                 second_target_index = i;
        //                 closest_dist = dist;
        //             }
        //         }
        //     }
        //     Debug.Log(second_target_index);
        //     positionSphere.transform.position = second_target;
        // }

        Vector3 calculateIndexPosition(int index)
        {
            ObiSolver solver = picker.solver;
            Vector4 position = solver.positions[index];
            Vector3 newPosition = solver.transform.TransformPoint(position);
            return newPosition;
        }

        void WriteData()
        {
            
            Vector3 pick1 = cam.WorldToScreenPoint(calculateIndexPosition(first_target_index));
            Vector3 pick2 = cam.WorldToScreenPoint(calculateIndexPosition(second_target_index));
            // RayCastFromCamera(pick2);
            var line = string.Format("{0},{1},{2},{3},{4},{5},{6}, \r", filename, pick1.x, pick1.y, pick1.z, pick2.x, pick2.y, pick2.z);
            File.AppendAllText(csv_path, line);
        }

        void RayCastFromCamera(Vector3 coords)
        {
            RaycastHit hit;
            Debug.Log(coords);
            Vector2 screen_position = new Vector2(coords.x, coords.y);
            Ray ray = cam.ScreenPointToRay(screen_position);
            // ObiSolver solver = picker.solver;
            // if(solver.Raycast(ray, out hit))
            // {
            //     Transform objectHit = hit
            // }
            if(Physics.Raycast(ray, out hit))
            {
                Transform objectHit = hit.transform;
                Debug.Log(objectHit.gameObject.name);
                Debug.DrawLine (ray.origin, hit.point,Color.red);
            }
            
            // RaycastHit hit;
            // Vector2 screen_position = new Vector2(coords.x, coords.y);
            // Ray ray = cam.ScreenPointToRay(screen_position);
            
            // if (Physics.Raycast(ray, out hit)) 
            // {
            //     Transform objectHit = hit.transform;
            //     GameObject object = objectHit.gameObject;
            //     Debug.Log(object.name);
            // }
        }

        void Picker_OnParticleDragged(ObiParticlePicker.ParticlePickEventArgs e)
        {
            pickArgs = e;
            ObiSolver solver = picker.solver;
            Vector4 position = solver.positions[e.particleIndex];
            Vector3 newPosition = solver.transform.TransformPoint(position);
            positionSphere.transform.position = newPosition;
            if(picks == 0)
            {
                gripper_target = newPosition;
                has_target = true;
            }
            picks++;        
            Debug.Log(newPosition);
        }

        public void Capture()
        {
            cam.SendMessage("SaveMessage", filename);
        }

        void Picker_OnParticleReleased(ObiParticlePicker.ParticlePickEventArgs e)
        {
            //pickArgs = null;
        }

        public static bool NearlyEqual(float a, float b, float epsilon=1e-2f) 
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
                return diff < epsilon;
            }
        }
    }
}

