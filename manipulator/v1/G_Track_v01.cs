using System;
using System.Collections;
using System.Collections.Generic;
using Unigine;

[Component(PropertyGuid = "03a1a349c4d454140aab3063b612da66525eae8d")]
public class G_Track_v01 : Component
{

	[ShowInEditor]
	bool isEnable = false;                                       // флаг включения скрипта
	bool selfTestFlag = false;                                   // флаг успешной инициализации
	
	[ShowInEditor]
    private Node frontLeftBigWheel;                              // базовая нода переднего левого мотор-колеса
	
	[ShowInEditor]
    private string frontLeftBigWheelJointName = "";              // колесный коллайдер переднего левого мотор-колеса
    
    [ShowInEditor]
    private Node frontRightBigWheel;                             // базовая нода переднего правого мотор-колеса
	
	[ShowInEditor]
    private string frontRightBigWheelJointName = "";             // колесный коллайдер переднего правого мотор-колеса
    
    [ShowInEditor]
    private Node rearLeftBigWheel;                               // базовая нода заднего левого мотор-колеса
	
	[ShowInEditor]
    private string rearLeftBigWheelJointName = "";               // колесный коллайдер заднего левого мотор-колеса
    
    [ShowInEditor]
    private Node rearRightBigWheel;                              // базовая нода заднего правого мотор-колеса
	
	[ShowInEditor]
    private string rearRightBigWheelJointName = "";              // колесный коллайдер заднего правого мотор-колеса
	
	[ShowInEditor]
	float maxTorque = 0.0f;                                      // максимальный момент для двигателя
	[ShowInEditor]
	float maxVelocity = 0.0f;                                    // максимальная скорость вращения мотор-колеса
	
	[ShowInEditor]
	private float linearTangentFriction = 0.0f;                  // базовая величина сопротивления скольжению колес по оси движения
	[ShowInEditor]
	private float linearBionormalFriction = 0.0f;                // базовая величина сопротивления скольжению колес перпендикулярно оси движения
	
	[ShowInEditor]
	private float onStandTangentFriction = 0.0f;                 // величина сопротивления скольжению колес по оси движения при вращении на месте
	[ShowInEditor]
	private float onStandBionormalFriction = 0.0f;               // величина сопротивления скольжению колес перпендикулярно оси движения при вращении на месте
	
	[ShowInEditor]
	private float complexTangentFriction = 0.0f;                 // комплексная величина сопротивления скольжению колес по оси движения при обычном перемещении с поворотами
	[ShowInEditor]
	private float complexBionormalFriction = 0.0f;               // комплексная величина сопротивления скольжению колес перпендикулярно оси движения при обычном перемещении с поворотами
	
	
	[ShowInEditor] 
	private Input.KEY leftTrackForwardKey;
	[ShowInEditor] 
	private Input.KEY leftTrackReverseKey;

	[ShowInEditor] 
	private Input.KEY rightTrackForwardKey;
	[ShowInEditor] 
	private Input.KEY rightTrackReverseKey;
	
	// колесные коллайдеры соответствующих мотор-колес
	private JointWheel frontLeftBigWheelCollider;
    private JointWheel frontRightBigWheelCollider;
	private JointWheel rearLeftBigWheelCollider;
    private JointWheel rearRightBigWheelCollider;
	
	// текущие скорости вращения соответствующих мотор-колес
	private float frontLeftBigWheelCurrentVelocity = 0.0f;
	private float frontRightBigWheelCurrentVelocity = 0.0f;
	private float rearLeftBigWheelCurrentVelocity = 0.0f;
	private float rearRightBigWheelCurrentVelocity = 0.0f;
	
	// текущие скорости вращения соответствующих мотор-колес
	private float frontLeftBigWheelVelocityChange = 0.0f;
	private float frontRightBigWheelVelocityChange = 0.0f;
	private float rearLeftBigWheelVelocityChange = 0.0f;
	private float rearRightBigWheelVelocityChange = 0.0f;
	
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
    
    private void InitWheelParam(ref JointWheel wheel) 
	{
		wheel.AngularTorque = maxTorque;
		wheel.TangentFriction = linearTangentFriction;
		wheel.BinormalFriction = linearBionormalFriction;
	}
    
    private void InitializaionBigWheelsParam() 
	{
		InitWheelParam(ref frontLeftBigWheelCollider);
		InitWheelParam(ref frontRightBigWheelCollider);
		InitWheelParam(ref rearLeftBigWheelCollider);
		InitWheelParam(ref rearRightBigWheelCollider);
	}
	
	private void Init()
	{
		frontLeftBigWheelCollider = FindJoint(frontLeftBigWheel, frontLeftBigWheelJointName);
		frontRightBigWheelCollider = FindJoint(frontRightBigWheel, frontRightBigWheelJointName);
		rearLeftBigWheelCollider = FindJoint(rearLeftBigWheel, rearLeftBigWheelJointName);
		rearRightBigWheelCollider = FindJoint(rearRightBigWheel, rearRightBigWheelJointName);
		
		if (frontLeftBigWheelCollider != null
			&& frontRightBigWheelCollider != null
			&& rearLeftBigWheelCollider != null
			&& rearRightBigWheelCollider != null) 
		{
			InitializaionBigWheelsParam(); 
			selfTestFlag = true;
			Log.Message("BigWheel Collider's Initializaion complete\n");
		}
		else  
		{
			selfTestFlag = false;
			isEnable = false;
			Log.Message("BigWheel Collider's Initializaion is fail\n");
		}
	}
	
	private float GetVelocityFromInput(Input.KEY forwardKey, Input.KEY backwardKey)
	{
		var velocity = 0.0f;
		var ifps = Game.IFps;
		if (Input.IsKeyPressed(forwardKey))
		{
			velocity += ifps * maxVelocity;
		}
		if (Input.IsKeyPressed(backwardKey))
		{
			velocity += -ifps * maxVelocity;
		}
		velocity = MathLib.Clamp(velocity, -1f, 1f);
		return velocity;
	}
	
	private void AddVelocity(ref float currentVelocity,float deltaVelocity)
	{
		if (deltaVelocity == 0f)
		{
			currentVelocity = 0f;
		}
		else
		{
			currentVelocity += deltaVelocity;
		}
	}
	
	private void UpdateBigWheelsVelocity() 
	{
		frontLeftBigWheelVelocityChange = GetVelocityFromInput(leftTrackForwardKey, leftTrackReverseKey);
		frontRightBigWheelVelocityChange = GetVelocityFromInput(rightTrackForwardKey, rightTrackReverseKey);
		rearLeftBigWheelVelocityChange = GetVelocityFromInput(leftTrackForwardKey, leftTrackReverseKey);
		rearRightBigWheelVelocityChange = GetVelocityFromInput(rightTrackForwardKey, rightTrackReverseKey);
		
		AddVelocity(ref frontLeftBigWheelCurrentVelocity, frontLeftBigWheelVelocityChange);
		AddVelocity(ref frontRightBigWheelCurrentVelocity, frontRightBigWheelVelocityChange);
		AddVelocity(ref rearLeftBigWheelCurrentVelocity, rearLeftBigWheelVelocityChange);
		AddVelocity(ref rearRightBigWheelCurrentVelocity, rearRightBigWheelVelocityChange);
		
		Log.Message("frontLeftBigWheelCurrentVelocity = {0}\n", frontLeftBigWheelCurrentVelocity);
		Log.Message("rearLeftBigWheelCurrentVelocity = {0}\n", rearLeftBigWheelCurrentVelocity);
		Log.Message("frontRightBigWheelCurrentVelocity = {0}\n", frontRightBigWheelCurrentVelocity);
		Log.Message("rearRightBigWheelCurrentVelocity = {0}\n", rearRightBigWheelCurrentVelocity);
	}
	
	private void Update()
	{
		if (selfTestFlag && isEnable)
		{
			UpdateBigWheelsVelocity();                  // обновление скорости вращения колес по входящим данным
		}
	}
	
	private void UpdateWheelVelocity(float CurrentVelocity, ref JointWheel wheel) {
		
		wheel.AngularVelocity = CurrentVelocity * wheel.AngularTorque;
	}
	
	void UpdatePhysics()
    {
		if (selfTestFlag && isEnable) 
		{
			UpdateWheelVelocity(frontLeftBigWheelCurrentVelocity, ref frontLeftBigWheelCollider);
			UpdateWheelVelocity(frontRightBigWheelCurrentVelocity, ref frontRightBigWheelCollider);
			UpdateWheelVelocity(rearLeftBigWheelCurrentVelocity, ref rearLeftBigWheelCollider);
			UpdateWheelVelocity(rearRightBigWheelCurrentVelocity, ref rearRightBigWheelCollider);
		} 
    }
}
