using System;
using System.IO;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Obi
{
    [RequireComponent(typeof(LineRenderer))]
    [RequireComponent(typeof(ObiParticlePicker))]
    public class DualParticleSelectionHandler : MonoBehaviour
    {
        public GameObject positionSphere;
        public GameObject gripper;
        public GameObject hand;
        public float speed = 0.01f;
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
        private string csv_path = "/home/jack/Unity/Unity-ComputerVisionSim/Captures/Cloth/test.csv";

        // public GameObject second_hand;
        // public GameObject second_gripper;
        public bool point_grabbed = false;

        public Camera cam;

        void Start()
        {
            // var articulation = second_hand.GetComponent<ArticulationBody>();
            // articulation.parentAnchorRotation = Quaternion.Euler(0f, 270f, 270f);


            //cam = GetComponent<Camera>();
            SelectRandomPick();
        }

        void SelectRandomPick()
        {
            ObiSolver solver = picker.solver;
            System.Random rnd = new System.Random();
            var index = rnd.Next(solver.positions.count);
            first_target_index = index;
            Vector4 position = solver.positions[index];
            gripper_target = solver.transform.TransformPoint(position);
            gripper_target.y = 0.08f + y_offset;
            // CalculateSecondPick(index);
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

        void FixedUpdate()
        {
            if(has_target)
            {
               GrabFirstPoint();
            }
            // else
            // {
            //     if(point_grabbed)
            //     {
            //         GrabSecondPoint();
            //     }
            // }
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
                        //if target is reached at 2f then the cloth has been grasped and lifted. we can return
                        if(NearlyEqual(gripper_target.y, 2.8f))
                        {
                            has_target = false;
                            point_grabbed = true;
                            //Capture();
                            //WriteData();
                            Debug.Log("image captured");
                            return;
                        }
                        gripper_target.y = 2.8f;
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

            //add sphere to second point
            positionSphere.transform.position = calculateIndexPosition(second_target_index);
        }

        // void GrabSecondPoint()
        // {
        //     Vector3 displacement = second_target - second_gripper.transform.position;
        //     //displacement.y +=y_offset;
        //     var actionX = speed * Mathf.Clamp(displacement.x, -2f, 2f);
        //     var actionY = speed * Mathf.Clamp(displacement.y , -1f, 1f);
        //     var actionZ = speed * Mathf.Clamp(displacement.z, -2f, 2f);

        //     //move art body parent anchor
        //     ArticulationBody articulation = second_hand.GetComponent<ArticulationBody>();

        //     //get jointPosition along y axis
        //     Vector3 anchor = articulation.parentAnchorPosition;

        //     //do y last
        //     if(NearlyEqual(displacement.x, 0f) && NearlyEqual(displacement.z, 0f))
        //     {
        //         // if(NearlyEqual(displacement.y, 0f))
        //         // {
        //         //     //grasp
        //         //     PincherController pincherController = second_hand.GetComponent<PincherController>();
        //         //     grippers_closed = pincherController.grip > 0.21;
                    
        //         //     if(grippers_closed)
        //         //     {
        //         //         //if target is reached at 2f then the cloth has been grasped and lifted. we can return
        //         //         if(NearlyEqual(gripper_target.y, 2.8f))
        //         //         {
        //         //             has_target = false;
        //         //             point_grabbed = true;
        //         //             //Capture();
        //         //             //WriteData();
        //         //             Debug.Log("image captured");
        //         //             return;
        //         //         }
        //         //         gripper_target.y = 2.8f;
        //         //     }
        //         //     else
        //         //     {
        //         //         pincherController.gripState = GripState.Closing;
        //         //     }
        //         // }
        //         // else
        //         {
        //             anchor.y += actionY;
        //         }
        //     }
        //     else
        //     {
        //         anchor.x += actionX;
        //         anchor.z += actionZ;
        //     }

        //     articulation.parentAnchorPosition = anchor;

        //     //add sphere to second point
        //     positionSphere.transform.position = calculateIndexPosition(second_target_index);
        // }

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
            
            var pick1 = cam.WorldToScreenPoint(calculateIndexPosition(first_target_index)).ToString();
            var pick2 = cam.WorldToScreenPoint(calculateIndexPosition(second_target_index)).ToString();
            using(var w = new StreamWriter(csv_path))
            {
                string filename = "test";
                var line = string.Format("{0},{1}, {2}", filename, pick1, pick2);
                w.WriteLine(line);
                w.Flush();
            }
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
            cam.SendMessage("SaveMessage", "newname.png");
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

