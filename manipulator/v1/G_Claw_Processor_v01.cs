using System;
using System.Collections;
using System.Collections.Generic;
using Unigine;

[Component(PropertyGuid = "d33f7f22ada1f4196b5b4ea4bf6b49b3e6ba0376")]
public class G_Claw_Processor_v01 : Component
{
	public enum ClawStatus {
		Free,
		Get,
		SelfLock
	};
	
	[ShowInEditor]
	private bool isEnable = false;
	
	[ShowInEditor]
	private bool isDebug = false;
	
	private bool isInit = false;
	
	
	[ShowInEditor]
	private Node leftFinger;                          // левый "палец"
	
	[ShowInEditor]
	private Node rightFinger;                         // правый "палец"
	
	[ShowInEditor]
	private float timeDelta = 0.1f;
	[ShowInEditor]
	private float posDelta = 0.01f;
	
	private Body leftBody;
	private int leftBodyID;
	private Body rightBody;
	private int rightBodyID;
	
	private ClawStatus status;                        // статус текущего состояния клешни
	private DateTime lastContactTime;                 // время последнего касания клешни
	
	private bool InitClawBodyes()
	{
		if (leftFinger != null && rightFinger != null) {

			leftBody = leftFinger.ObjectBody;
			leftBodyID = leftBody.ID;

			rightBody = rightFinger.ObjectBody;
			rightBodyID = rightBody.ID;

			return true;
		}
		else {
			return false;
		}
	}
	
	private void Init()
	{
		if (InitClawBodyes())
		{
			leftBody.AddContactsCallback((l_body) => l_body.RenderContacts()); 
            leftBody.AddContactEnterCallback(OnContactEnter); 
			
			rightBody.AddContactsCallback((r_body) => r_body.RenderContacts()); 
            rightBody.AddContactEnterCallback(OnContactEnter);
			
			isInit = true;
			status = ClawStatus.Free;
		}
	}

	private void OnContactEnter(Body body, int num) 
    {
		Visualizer.Enabled = true;
		
        if(body.IsContactEnter(num))
        {
            if (isEnable && isDebug && isInit)
			{
				lastContactTime = DateTime.Now;
                Body body0 = body.GetContactBody0(num);
                Body body1 = body.GetContactBody1(num);

                Body touchedBody = null;
 
                if (body0 && body0 != body) touchedBody = body0;  
                if (body1 && body1 != body) touchedBody = body1;
                
                if (touchedBody)
                {
                    Visualizer.RenderObject(touchedBody.Object, vec4.BLUE, 0.5f); 
                }
				else
				{
					Visualizer.RenderObjectSurface(body.GetContactObject(num), body.GetContactSurface(num), vec4.BLUE, 0.5f); 
				}
            }
        }
    }
    
    public ClawStatus GetStatus()
	{
		return status;
	}
	
	private bool PositionCompare(vec3 lhs, vec3 rhs)
	{
		if (lhs == vec3.ONE && rhs == vec3.ONE) return true;
		if (MathLib.Abs(lhs.x - rhs.x) > posDelta) return false;
		if (MathLib.Abs(lhs.y - rhs.y) > posDelta) return false;
		if (MathLib.Abs(lhs.z - rhs.z) > posDelta) return false;
		
		return true;
	}
	
	private void Update()
	{
		//Log.Message("ClawStatus - {0}\n", status.ToString());
	}
}
