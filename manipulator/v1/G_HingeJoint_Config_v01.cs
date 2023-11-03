using System;
using System.Collections;
using System.Collections.Generic;
using Unigine;

[Component(PropertyGuid = "f5b995774107f46a939298c10e969b2c23345c9b")]
public class G_HingeJoint_Config_v01 : Component
{
	[ShowInEditor]
	private string configName = "";
	[ShowInEditor]
	private int numIterations = 1;
	[ShowInEditor]
	private float linearRestitution = 0.2f;
	[ShowInEditor]
	private float angularRestitution = 0.2f;
	[ShowInEditor]
	private float angularDamping = 0.0f;
	[ShowInEditor]
	private float angularVelocity = 0.0f;
	[ShowInEditor]
	private float angularTorque = 0.0f;
	[ShowInEditor]
	private float angularAngle = 0.0f;
	[ShowInEditor]
	private float angularSpring = 0.0f;
	
	public string GetConfigName() 
	{
		return configName;
	}
	
	public int GetNumIterations() 
	{
		return numIterations;
	}
	
	public float GetLinearRestitution()
	{
		return linearRestitution;
	}
	
	public float GetAngularRestitution()
	{
		return angularRestitution;
	}
	
	public float GetAngularDamping()
	{
		return angularDamping;
	}
	
	public float GetAngularVelocity()
	{
		return angularVelocity;
	}
	
	public float GetAngularTorque()
	{
		return angularTorque;
	}
	
	public float GetAngularAngle()
	{
		return angularAngle;
	}
	
	public float GetAngularSpring()
	{
		return angularSpring;
	}
}
