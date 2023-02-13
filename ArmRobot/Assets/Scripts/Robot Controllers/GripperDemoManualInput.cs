using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GripperDemoManualInput : MonoBehaviour
{
    public GameObject hand;


    void Update()
    {
        float inputX = Input.GetAxis("BigHandVertical");
        BigHandState moveStateX = MoveStateForInput(inputX);
        GripperDemoController controller = hand.GetComponent<GripperDemoController>();
        controller.moveStateX = moveStateX;

        float inputZ = Input.GetAxis("BigHandHorizontal");
        BigHandState moveStateZ = MoveStateForInput(inputZ);
        controller.moveStateZ = moveStateZ;

    }

    BigHandState MoveStateForInput(float input)
    {
        if (input > 0)
        {
            return BigHandState.MovingUp;
        }
        else if (input < 0)
        {
            return BigHandState.MovingDown;
        }
        else
        {
            return BigHandState.Fixed;
        }
    }
}
