using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BigHandState { Fixed = 0, MovingUp = 1, MovingDown = -1 };

public class GripperDemoController : MonoBehaviour
{

    public BigHandState moveStateX = BigHandState.Fixed;
    public BigHandState moveStateY = BigHandState.Fixed;
    public BigHandState moveStateZ = BigHandState.Fixed;

    public float speed = 1.0f;


    //articulation: X-> up/down
    //              Y->Forward/back
    //              Z->
    private void FixedUpdate()
    {
        if (moveStateX != BigHandState.Fixed)
        {
            ArticulationBody articulation = GetComponent<ArticulationBody>();

            //get jointPosition along y axis
            //Debug.Log(articulation.jointPosition.dofCount);
            float xDrivePostion = articulation.jointPosition[0];
            //Debug.Log(xDrivePostion);

            //increment this y position
            float targetPosition = xDrivePostion + -(float)moveStateX * Time.fixedDeltaTime * speed;

            //set joint Drive to new position
            var drive = articulation.xDrive;
            drive.target = targetPosition;
            articulation.xDrive = drive;
        }
    }
}
