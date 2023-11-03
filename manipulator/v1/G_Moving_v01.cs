using Unigine;

public class Mover : Component 
{
    [ShowInEditor]
    private Node frontLeftWheel;
	
	[ShowInEditor]
    private string frontLeftWheelJointName = "";
    
    [ShowInEditor]
    private Node frontRightWheel;
	
	[ShowInEditor]
    private string frontRightWheelJointName = "";
    
    [ShowInEditor]
    private Node rearLeftWheel;
	
	[ShowInEditor]
    private string rearLeftWheelJointName = "";
    
    [ShowInEditor]
    private Node rearRightWheel;
	
	[ShowInEditor]
    private string rearRightWheelJointName = "";
    
    [ShowInEditor]
	private Input.KEY forwardMoveKey;
    
	[ShowInEditor]
	private Input.KEY rearMoveKey;
    
    [ShowInEditor]
	private float maxSpeed = 10f;
    
    private float drivingWheelsCurrentVelocity=0;
    
	
	private float DEBUG_CURRENT_INPUT_VELOCITY=0;
	
	private JointWheel frontLeftWheelRoller;
    private JointWheel frontRightWheelRoller;
	private JointWheel rearLeftWheelRoller;
    private JointWheel rearRightWheelRoller;
	
    private void Init()
	{
		frontLeftWheelRoller = FindJoint(frontLeftWheel, frontLeftWheelJointName);
		frontRightWheelRoller = FindJoint(frontRightWheel, frontRightWheelJointName);
		rearLeftWheelRoller = FindJoint(rearLeftWheel, rearLeftWheelJointName);
		rearRightWheelRoller = FindJoint(rearRightWheel, rearRightWheelJointName);
		
		Log.Message("Wheel Collider's Initializaion complete\n");
	}
	
	private JointWheel FindJoint(Node node, string jointName)
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
	
	private void Update()
	{
		var drivingWheelsVelocityChange = GetVelocityFromInput(forwardMoveKey, rearMoveKey);

		AddVelocity(ref drivingWheelsCurrentVelocity, drivingWheelsVelocityChange);
		
		//Log.Message("DEBUG_CURRENT_INPUT_VELOCITY = ");
		//Log.Message(DEBUG_CURRENT_INPUT_VELOCITY);
		//Log.Message(" | ");
		//Log.Message("DEBUG_CURRENT_INPUT_VELOCITY * AngularTorque = ");
		//Log.Message(DEBUG_CURRENT_INPUT_VELOCITY * rearRightWheelRoller.AngularTorque);
		//Log.Message("\n");
	}
	
	private float GetVelocityFromInput(Input.KEY forwardKey, Input.KEY backwardKey)
	{
		var velocity = 0.0f;
		var ifps = Game.IFps;
		if (Input.IsKeyPressed(forwardKey))
		{
			velocity += ifps * maxSpeed;
		}
		if (Input.IsKeyPressed(backwardKey))
		{
			velocity += -ifps * maxSpeed;
		}
		velocity = MathLib.Clamp(velocity, -1f, 1f);
        DEBUG_CURRENT_INPUT_VELOCITY = velocity;
		return velocity;
	}
	
	private void AddVelocity(ref float currentVelocity,float deltaVelocity)
	{
		if (deltaVelocity==0f)
		{
			currentVelocity = 0f;
		}
		else
		{
			currentVelocity += deltaVelocity;
		}
	}
	
	private void SetWheelVelocity(float CurrentVelocity, ref JointWheel wheel) {
		
		wheel.AngularVelocity = CurrentVelocity * wheel.AngularTorque;
	}
	
	void UpdatePhysics()
    {
        SetWheelVelocity(drivingWheelsCurrentVelocity, ref rearLeftWheelRoller);
		SetWheelVelocity(drivingWheelsCurrentVelocity, ref rearRightWheelRoller);
    }
}
