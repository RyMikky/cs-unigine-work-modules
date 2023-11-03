using System;
using System.Collections;
using System.Collections.Generic;
using Unigine;

[Component(PropertyGuid = "2222955c55af310b2df1d2ee84ff7d7509d1f050")]
public class G_Rotator_v01 : Component
{
	private float __EPSILON__ = 0.00001f;
	
	private bool CompareTwoFloat(float lhs, float rhs) 
	{
		if (lhs == rhs
			|| (lhs < rhs && rhs - lhs <= __EPSILON__)
			|| (lhs > rhs && lhs - rhs <= __EPSILON__))
		{
			return true;
		}
		
		return false;
	}	
	
	[ShowInEditor]
	bool isEnable = false;                                                          // флаг включения скрипта
	
	[ShowInEditor]
    private Node hingeJointRotatorNode;                                             // базовая нода объекта с сочленением вращения
	
	[ShowInEditor]
    private string targetHingeJointName = "";                                       // название сочленения объекта
	
	[ShowInEditor]
	float maxTorque = 0.0f;                                                         // максимальный момент для двигателя вращения
	float currentTorque = 0.0f;                                                     // текущий установленный момент для двигателя вращения
	[ShowInEditor]
	float maxVelocity = 0.0f;                                                       // максимальная скорость вращения шпинделя
	float currentVelocity = 0.0f;                                                   // текущая скорость вращения шпинделя
	
	[ShowInEditor]
	bool freeRotate = true;                                                         // флаг свободного врящения, при таком режиме момент равен нулю
	

	[ShowInEditor] 
	private Input.KEY moveRotatorUp;

	[ShowInEditor] 
	private Input.KEY moveRotatorDown;
	
	[ShowInEditor] 
	private Input.KEY activateTorque;
	
	
	private JointHinge hingeJointRotator;                                           // базовый объект вращения - HingeJoint

	
	private JointHinge FindJoint(Node node, string jointName)
    {
        var body = node.ObjectBody;
        for (int i = 0; i < body.NumJoints; i++)
        {
            var joint = body.GetJoint(i) as JointHinge;
            if (joint == null || joint.Name != jointName)
            {
                continue;
            }

            return joint;
        }

        return null;
    }
	
	private void Init()
	{
		if (hingeJointRotatorNode == null || targetHingeJointName == null) {
			isEnable = false;
		}
		
		else {
			hingeJointRotator = FindJoint(hingeJointRotatorNode, targetHingeJointName);
		}
		
		if (isEnable) {
			Log.Message("{0} Initializaion complete\n", targetHingeJointName);
		}
	}
	
	private void Update()
	{	
		if (isEnable)
		{
			if (Input.IsKeyDown(activateTorque))
			{
				if (freeRotate) {
					SetRotatorAngularTorque(maxTorque);
					freeRotate = false;
					Log.Message("Torque Enable\n");
				}
				else {
					SetRotatorAngularTorque(0.0f);
					freeRotate = true;
					Log.Message("Torque Disable\n");
				}
			}
			
			if (!freeRotate && Input.IsKeyDown(moveRotatorUp))
			{
				RotateUp();
			}
			
			if (!freeRotate && Input.IsKeyDown(moveRotatorDown))
			{
				RotateDown();
			}
			
			if (!freeRotate && (Input.IsKeyUp(moveRotatorUp)
				|| Input.IsKeyUp(moveRotatorDown)))
			{
				HoldPosition();
			}
		}

	}
	
	public float GetRotatorAngularTorque()
	{
		return currentTorque;
	}
	
	public void SetRotatorAngularTorque(float torque)
	{
		if (isEnable && MathLib.Abs(torque) <= MathLib.Abs(maxTorque))
		{
			if (torque != currentTorque) {
				currentTorque = torque;
			}
			
			if (CompareTwoFloat(currentTorque, 0.0f)) {
				hingeJointRotator.AngularTorque = 0.0f;
				freeRotate = true;
			}
			else {
				hingeJointRotator.AngularTorque = currentTorque;
				freeRotate = false;
			}
		}
	}
	
	public float GetRotatorAngularVelocity()
	{
		return currentVelocity;
	}
	
	public void SetRotatorAngularVelocity(float velocity)
	{
		if (isEnable && MathLib.Abs(velocity) <= MathLib.Abs(maxVelocity))
		{

			if (velocity != currentVelocity) {
				currentVelocity = velocity;
			}
			
			if (CompareTwoFloat(currentVelocity, 0.0f)) {
				hingeJointRotator.AngularVelocity = 0.0f;
			}
			else {
				hingeJointRotator.AngularVelocity = currentVelocity;
			}
		}
		
	}
	
	public void RotateUp()
	{
		SetRotatorAngularVelocity(maxVelocity);
	}

	public void RotateDown()
	{
		SetRotatorAngularVelocity(-maxVelocity);
	}
	
	public void HoldPosition() 
	{
		SetRotatorAngularVelocity(0.0f);
	}
}
