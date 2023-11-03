using System;
using System.Collections;
using System.Collections.Generic;
using Unigine;

[Component(PropertyGuid = "4fcdf92ab04556814304bdec1364a05bf95259fe")]
public class G_Asseble_Config_v01 : Component
{
	
	[ShowInEditor]
	bool isEnable = false;                                       // флаг включения скрипта
	bool selfTestFlag = false;                                   // флаг успешной инициализации
	
	[ShowInEditor]
	private Node[] trackAssebles;                                // базовые ноды "лапок" робота
	
	[ShowInEditor]
	private string[] assebleJointNames;                          // названия сочленений "лапок"
	
	private JointHinge[] assebleJoints;
	
	// коллайдеры сочленения "лапок"
	private JointHinge FR_AssembleJoint;
    private JointHinge FL_AssembleJoint;
	private JointHinge RL_AssembleJoint;
    private JointHinge RR_AssembleJoint;
	
	[ShowInEditor]
	private float assebleJointDamping = 0.0f;
	[ShowInEditor]
	private float assebleJointVelocity = 0.0f;
	[ShowInEditor]
	private float assebleJointTorque = 0.0f;
	[ShowInEditor]
	private float assebleJointAngle = 0.0f;
	[ShowInEditor]
	private float assebleJointSpring = 0.0f;
	
	[ShowInEditor]
	private Node[] smallWheels;                                   // опорные мелкие колеса на лапках
	
	[ShowInEditor]
	private string[] smallWheelJointNames;                        // названия сочленений опорных катков
	
	// колесные коллайдеры соответствующих колес
	private JointWheel FR_SmallWheelJoint;
    private JointWheel FL_SmallWheelJoint;
	private JointWheel RL_SmallWheelJoint;
    private JointWheel RR_SmallWheelJoint;
	
	[ShowInEditor]
	private float smallWheelLDamping = 0.0f;
	[ShowInEditor]
	private float smallWheelLFrom = 0.0f;
	[ShowInEditor]
	private float smallWheelLTo = 0.0f;
	[ShowInEditor]
	private float smallWheelLDistance = 0.0f;
	[ShowInEditor]
	private float smallWheelLSpring = 0.0f;
	
	[ShowInEditor]
	private float smallWheelAVelocity = 0.0f;
	[ShowInEditor]
	private float smallWheelLTorque = 0.0f;
	
	[ShowInEditor]
	private float smallWheelTFriction = 0.0f;
	[ShowInEditor]
	private float smallWheelBFriction = 0.0f;
	
	[ShowInEditor]
	private float smallWheelMass = 0.0f;
	[ShowInEditor]
	private float smallWheelRadius = 0.0f;
	
	
	private JointWheel FindWheelJoint(Node node, string jointName)
    {
        var body = node.ObjectBody;
        for (int i = 0; i < body.NumJoints; i++)
        {
            var joint = body.GetJoint(i) as JointWheel;
            if (joint == null || joint.Name != jointName)
            {
                continue;
            }

            return joint;
        }

        return null;
    }
    
    private JointHinge FindHingeJoint(Node node, string jointName)
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
    
    private bool InitCollidres() 
	{
		/*
		if (!trackAssebles.Empty && !assebleJointNames.Empty 
			&& trackAssebles.Length == assebleJointNames.Length) 
		{
			//assebleJoints = new 
			
			for (int i = 0; i != trackAssebles.Length; ++i) 
			{
				
			}
		}
		
		*/
		
		return true;
	}
	
	private void Init()
	{
		/*
		if (!InitCollidres())
		{
			return;
		}
		*/
	}
	
	private void Update()
	{
		// write here code to be called before updating each render frame
		
	}
}
