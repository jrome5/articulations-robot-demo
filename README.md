# Articulations Robot Demo

<img align="right" style="padding-left: 10px; padding-right: 10px; padding-bottom: 10px" width="350px" src="images/RobotHandDemo.gif">

This is a simulation of the [Universal Robotics UR3e](https://www.universal-robots.com/products/ur3-robot/) robot using Unity's new [articulation joint system](https://docs.unity3d.com/2020.1/Documentation/ScriptReference/ArticulationBody.html).

This new joint system, powered by [Nvidia's PhysX 4](https://news.developer.nvidia.com/announcing-physx-sdk-4-0-an-open-source-physics-engine/), is a dramatic improvement over the older joint types available in Unity. It uses Featherstone's algorithm and a reduced coordinate representation to gaurantee no unwanted stretch in the joints. In practice, this means that we can now chain many joints in a row and still achieve stable and precise movement. 


## Getting Started

Requires `2020.1.0b1` build of Unity or later. To get started:
1. Open the `ArmRobot` folder in Unity.
2. Open `Scenes` > `ArticulationRobot`
3. Press play

## Manual Controls

You can move the robot around manually using the following keyboard commands:

```
A/D - rotate base joint
S/W - rotate shoulder joint
Q/E - rotate elbow joint
O/P - rotate wrist1
K/L - rotate wrist2
N/M - rotate wrist3
V/B - rotate hand
X - close pincher
Z - open pincher
```

All manual control is handled through the scripts on the `ManualInput` object. To disable
manual input, just uncheck this object in the Hierarchy window.

## License

[Apache License 2.0](LICENSE)




