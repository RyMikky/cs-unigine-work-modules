using System;
using System.Collections;
using System.Collections.Generic;
using Unigine;

[Component(PropertyGuid = "abf77335d013eb10b1aa432fa87016c2ac236739")]
public class G_Manipulator_Config : Component
{	

	[ShowInEditor]
	private Node[] manipulatorSegments;                          // базовые ноды элементов манипулятора
	
	[ShowInEditor]
	private string[] manipulatorSegmentJointNames;               // названия сочленений элементов манипулятора
	
	private List<JointHinge> manipulatorSegmentJoints = new List<JointHinge>();
	
	[ShowInEditor]
	bool useManipulatorCustomConfig = false;                     // флаг включения скрипта
	
	[ShowInEditor]
	private G_HingeJoint_Config_v01 MSJ_Config;                      // блок настроек конфигурации сочленений
	
	[ShowInEditor]
	private Node[] handSegments;                                 // базовые ноды элементов клешни
	
	[ShowInEditor]
	string handSegmentJointName;                                 // названия сочленений элементов клешни
	
	private List<JointHinge> handSegmentJoints = new List<JointHinge>();
	
	[ShowInEditor]
	bool useClawCustomConfig = false;                            // флаг включения скрипта
	
	[ShowInEditor]
	private G_HingeJoint_Config_v01 Claw_Config;                     // блок настроек конфигурации сочленений

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
		if (manipulatorSegments.Length != 0 && manipulatorSegmentJointNames.Length != 0  
			&& manipulatorSegments.Length == manipulatorSegmentJointNames.Length) 
		{
			for (int i = 0; i != manipulatorSegments.Length; ++i) 
			{
				manipulatorSegmentJoints.Add(FindHingeJoint(manipulatorSegments[i], manipulatorSegmentJointNames[i]));
			}
		}
		
		if (handSegments.Length != 0)
		{
			for (int i = 0; i != handSegments.Length; ++i) 
			{
				handSegmentJoints.Add(FindHingeJoint(handSegments[i], handSegmentJointName));
			}
		}
	
		if (manipulatorSegmentJoints.Count == manipulatorSegmentJointNames.Length)
		{
			return true;
		}
		else 
		{
			return false;
		}
		
	}
	
	private void InitCustomParam() 
	{

		if (useManipulatorCustomConfig) 
		{
			foreach (JointHinge item in manipulatorSegmentJoints) 
			{
				item.NumIterations = MSJ_Config.GetNumIterations();
				
				item.LinearRestitution = MSJ_Config.GetLinearRestitution();
				item.AngularRestitution = MSJ_Config.GetAngularRestitution();
				
				item.AngularDamping = MSJ_Config.GetAngularDamping();
				item.AngularVelocity = MSJ_Config.GetAngularVelocity();
				item.AngularTorque = MSJ_Config.GetAngularTorque();
				item.AngularAngle = MSJ_Config.GetAngularAngle();
				item.AngularSpring = MSJ_Config.GetAngularSpring();
				
			}
		}
		
		
		if (useClawCustomConfig) 
		{
			foreach (JointHinge item in handSegmentJoints) 
			{
				item.NumIterations = Claw_Config.GetNumIterations();
				
				item.LinearRestitution = Claw_Config.GetLinearRestitution();
				item.AngularRestitution = Claw_Config.GetAngularRestitution();
				
				item.AngularDamping = Claw_Config.GetAngularDamping();
				item.AngularVelocity = Claw_Config.GetAngularVelocity();
				item.AngularTorque = Claw_Config.GetAngularTorque();
				item.AngularAngle = Claw_Config.GetAngularAngle();
				item.AngularSpring = Claw_Config.GetAngularSpring();
				
			}
		}
	}
	
	private void Init()
	{		
		if (!InitCollidres())
		{
			Log.Message("InitCollidres() is fail\n");
			return;
		}
		
		else  
		{
			InitCustomParam();
			Log.Message("InitCustomParam() complete\n");
		}
	}
	
	private void Update()
	{
		// write here code to be called before updating each render frame
		
	}
}
