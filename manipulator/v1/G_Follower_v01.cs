using System;
using System.Collections;
using System.Collections.Generic;
using Unigine;

[Component(PropertyGuid = "09b2b0211fe80b0c63ca4afb3a17c41eceb5dce5")]
public class G_Follower_v01 : Component
{
	[ShowInEditor]
	private Node parent;
	private vec3 parentLastPosition;


	[ShowInEditor]
	private Node pivot;
	[ShowInEditor]
	private Node child;
	private vec3 deltaPosition;

	private void Init()
	{
		Log.Message("Parent.ID - {0}\n", parent.ID);
		
	}
	
	private void Update()
	{
		// vec3 parentCurrentPosition = parent.Position;
		// deltaPosition = parentLastPosition - parentCurrentPosition;
		
	}

	private void UpdatePhysics()
	{
		// child.Position = child.Position + deltaPosition;
		// parentLastPosition = parent.Position;

		child.Position = pivot.Position;
	}
}