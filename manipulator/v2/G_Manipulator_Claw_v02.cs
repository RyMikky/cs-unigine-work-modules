using System;
using System.Collections.Generic;
using Unigine;

[Component(PropertyGuid = "97fc318b468dd96e9202d1f547283ad09f38cfef")]
public class G_Manipulator_Claw_v02 : Component
{

	public enum Mode
	{
		None, Joint, Child
	}

	[ShowInEditor][Parameter(Tooltip = "Выбор режима работы скрипта")]
	Mode mode = Mode.Child;

	[ShowInEditor][Parameter(Tooltip = "Флаг активации скрипта")]
	bool isEnable = false;

	[ShowInEditor][Parameter(Tooltip = "Скорость движения объектов")]
	float speed = 0.0f;
	[ShowInEditor][Parameter(Tooltip = "Минимальный угол раскрытия клешней")]
	float fromLimit = 0.0f;
	[ShowInEditor][Parameter(Tooltip = "Максимальный угол раскрытия клешней")]
	float toLimit = 0.0f;                              

	[ShowInEditor][Parameter(Tooltip = "Кнопка сведения клешней")]
	private Input.KEY forwardKey;
	[ShowInEditor][Parameter(Tooltip = "Кнопка разведения клешней")]
	private Input.KEY backwardKey;

	[ShowInEditor]
	private G_HingeJoint_Config_v01 config;
	
	[ShowInEditor][Parameter(Tooltip = "Время отключения основных нод и коллайдеров с момента последнего действия, в мс")]
	private float freezeTime = 0.5f;
	[ShowInEditor][Parameter(Tooltip = "Массив основных нод, которые являются носителями физических тел и сочленений")]
	private Node[] controlNodes;
	[ShowInEditor][Parameter(Tooltip = "Преднастроенный обработчик коллизий для пальцев клешни")]
	private G_Collision_Handler_v02 collisionHandler;

	[ShowInEditor][Parameter(Tooltip = "Точка привязки \"подхваченного\" клешнёй элемента")]
	private Node catchPivot;
	[ShowInEditor][Parameter(Tooltip = "Нода, содержащая форму триггера для подсветки того, что есть между клешнями")]
	private Node triggerNode;
	private Shape triggerShape;


	private float angle = 0.0f;                     // текущий расчётный угол поворота

	private bool isFreeze = false;                  // флаг состояния активности основных нод

	private DateTime lastInputTime;                 // время последнего нажатия на кнопки управления

	private List<JointHinge> clawJoints = new List<JointHinge>();

	private Node leftClaw;
	private Node rightClaw;

	private bool isCatched = false;

	private Node catchedNode = null;                   // пойманная нода
	private vec3 catchedLocalDelta = new vec3();       // расстояние от центра пойманного объекта до точки привязки

	private bool isNewCatched = false;                 // флаг того, что пойманный объект еще не записал локальный поворот
	private quat catchedLocalRotation = new quat();    // локальный поворот пойманного объекта

	/// <summary>
	/// Метод базовой инициализации сочленений по переданным нодам. Ищет все доступные уникальные JointHinge и добавляет в список 
	/// </summary>
	/// <param name="node"></param>
	private void InitJoints(Node node)
	{
		var body = node.ObjectBody;
		for (int i = 0; i < body.NumJoints; ++i)
		{
			var joint = body.GetJoint(i) as JointHinge;
			if (joint != null && !clawJoints.Contains(joint))
			{
				clawJoints.Add(joint);
			}
		}

		if (body.Name == "LeftFinger") {
			leftClaw = node;
		}

		if (body.Name == "RightFinger") { 
			rightClaw = node;
		}
	}

	private void Init()
	{
		foreach (var node in controlNodes)
		{
			InitJoints(node);
		}

		triggerShape = triggerNode.ObjectBodyRigid.GetShape(triggerNode.ObjectBodyRigid.FindShape("TriggerBox"));
		triggerNode.ObjectBody.AddContactEnterCallback(OnContactTouchedBodyVisualizer);
		
	}

	/// <summary>
	/// Метод изменения действительного угла при вводе с контроллера. 
	/// При выходе за лимиты, установит соответствующее ограничение и вернет false.
	/// False "заморозит" клешни и не даст возможности изменять угол в том же направлении дальше.
	/// </summary>
	/// <param name="value"></param>
	/// <returns></returns>
	private bool AddAngle(float value)
	{
		float newAngle = angle + value;

		if (newAngle >= toLimit)
		{
			SetAngle(toLimit);
			return false;
		}
		else if (newAngle <= fromLimit)
		{
			SetAngle(fromLimit);
			return false;
		}
		else
		{
			isFreeze = false;
			SetAngle(newAngle);
			return true;
		}
	}

	/// <summary>
	/// Публичный сеттер напрямую работает с действительным углом раскрытия клешней. Проверяет выход за лимиты. 
	/// Работает только в случае активности скрипта и активного состояния основных нод.
	/// </summary>
	/// <param name="Angle"></param>
	public void SetAngle(float value)
	{
		if (isEnable && !isFreeze)
		{
			if (value < fromLimit)
			{
				angle = fromLimit;
			}
			else if (value > toLimit)
			{
				angle = toLimit;
			}
			else
			{
				angle = value;
			}
		}
	}

	private vec3 GetAxis1(Node node)
	{
		vec3 result = vec3.ZERO;
		quat catchQuat = node.GetWorldRotation();
		vec3 catchRotation = catchQuat.Euler;
		
		result = catchQuat.Tangent;

		result.x = (float)(int)Math.Round(result.x);
		result.y = (float)(int)Math.Round(result.y);
		result.z = (float)(int)Math.Round(result.z);

		return result;
	}

	/// <summary>
	/// Создаёт два joint для привязки пойманного объекта к клешне.
	/// Joint-ы привязываются к левому и правому "пальцу"
	/// </summary>
	private void MakeCatchJoints()
	{
		if (!isCatched && mode == Mode.Joint && catchedNode != null)
		{
			vec3 leftClawLocalPos = leftClaw.Position;
			vec3 leftClawWorldPos = leftClaw.WorldPosition;

			vec3 rightClawLocalPos = rightClaw.Position;
			vec3 rightClawWorldPos = rightClaw.WorldPosition;

			vec3 LocalDistance = leftClaw.Position - rightClaw.Position;
			vec3 WorldDistance = leftClaw.WorldPosition - rightClaw.WorldPosition;

			vec3 catchedLocalPos = catchedNode.Position;
			vec3 catchedWorldPos = catchedNode.WorldPosition;


			//float posDelta = (leftClaw.Position.z - rightClaw.Position.z) / 2;
			float posDelta = (Math.Abs(leftClaw.Position.z) + Math.Abs(rightClaw.Position.z)) / 2;

			vec3 pivotLocalPos = new vec3(
				((leftClaw.Position.x + rightClaw.Position.x) / 2), 
				((leftClaw.Position.y + rightClaw.Position.y) / 2), 
				((leftClaw.Position.z - rightClaw.Position.z) / 2));

			JointHinge leftJoint = new JointHinge();
			//leftJoint.Collision = 1;
			leftJoint.NumIterations = 40;
			leftJoint.Name = "LeftCatchedJoint";
			leftJoint.Body0 = leftClaw.ObjectBody;
			leftJoint.Body1 = catchedNode.ObjectBody;
			leftJoint.MaxForce = 5000;

			leftJoint.Anchor0 = new vec3(0, -0.15, -posDelta/*-0.15*/);
			//leftJoint.Anchor0 = pivotLocalPos;
			leftJoint.Anchor1 = new vec3(0, 0, 0/*0.05*/);
			leftJoint.Axis0 = new vec3(0, 0, 1);
			leftJoint.Axis1 = new vec3(1, 0, 0);

			leftJoint.Axis1 = GetAxis1(catchedNode);

			//var axis = GetAxis1(catchedNode);

			leftJoint.LinearRestitution = 1;
			leftJoint.AngularRestitution = 1;
			leftJoint.LinearSoftness = 1;
			leftJoint.AngularSoftness = 1;
			leftJoint.AngularDamping = 10;
			leftJoint.AngularTorque = 5000;

			leftClaw.ObjectBody.AddJoint(leftJoint);


			JointHinge RightJoint = new JointHinge();
			//RightJoint.Collision = 1;
			RightJoint.NumIterations = 40;
			RightJoint.Name = "RightCatchedJoint";
			RightJoint.Body0 = leftClaw.ObjectBody;
			RightJoint.Body1 = catchedNode.ObjectBody;
			RightJoint.MaxForce = 5000;

			//RightJoint.Anchor0 = pivotLocalPos;
			RightJoint.Anchor0 = new vec3(0, -0.15, posDelta/*0.15*/);
			RightJoint.Anchor1 = new vec3(0, 0, 0);
			RightJoint.Axis0 = new vec3(0, 0, 1);
			RightJoint.Axis1 = new vec3(1, 0, 0);

			RightJoint.LinearRestitution = 1;
			RightJoint.AngularRestitution = 1;
			RightJoint.LinearSoftness = 1;
			RightJoint.AngularSoftness = 1;
			RightJoint.AngularDamping = 10;
			RightJoint.AngularTorque = 5000;

			rightClaw.ObjectBody.AddJoint(RightJoint);


			catchedNode.ObjectBodyRigid.AngularVelocity = vec3.ZERO;
			catchedNode.ObjectBodyRigid.LinearVelocity = vec3.ZERO;

			isCatched = true;
		}
	}

	/// <summary>
	/// Удаляет ранее созданные джоинты
	/// </summary>
	private void DeleteCatchJoint()
	{
		if (isCatched && mode == Mode.Joint && catchedNode != null)
		{
			leftClaw.ObjectBody.RemoveJoint(leftClaw.ObjectBody.FindJoint("LeftCatchedJoint"));
			leftClaw.ObjectBody.RemoveJoint(leftClaw.ObjectBody.FindJoint("RightCatchedJoint"));

			catchedNode.ObjectBodyRigid.AngularVelocity = vec3.ZERO;
			catchedNode.ObjectBodyRigid.LinearVelocity = vec3.ZERO;

			// for (int i = 0; i != catchedNode.ObjectBodyRigid.NumShapes; i++)
			// 	{
			// 		Shape shape = catchedNode.ObjectBodyRigid.GetShape(i) as Shape;
			// 		int mask = shape.ExclusionMask;
			// 		mask -= 1 << 28; 
			// 		catchedNode.ObjectBodyRigid.GetShape(i).ExclusionMask = mask;
			// 	}

			catchedNode = null;
			isCatched = false;
		}
	}

	/// <summary>
	/// Отображение соединений в рантайме
	/// </summary>
	private void RenderCatchedJoint()
	{
		if(isCatched)
		{
			Visualizer.Enabled = true;
			Visualizer.Mode=Visualizer.MODE.ENABLED_DEPTH_TEST_DISABLED;

			Joint lJoint = leftClaw.ObjectBody.GetJoint(leftClaw.ObjectBody.FindJoint("LeftCatchedJoint"));
			Joint rJoint = rightClaw.ObjectBody.GetJoint(rightClaw.ObjectBody.FindJoint("RightCatchedJoint"));

			lJoint.RenderVisualizer(vec4.RED);
			rJoint.RenderVisualizer(vec4.GREEN);
		}
		
	}

	/// <summary>
	/// Добавляет пойманную ноду в дочерние узлы точки привязки и отключает её физику
	/// </summary>
	/// <param name="id"></param>
	private void MakeCatchChild()
	{
		if (!isCatched && mode == Mode.Child && catchedNode != null)
		{
			// необходимо сохранить исходные данные объекта перед добавление в наследники
			vec3 catchedWorldPosition = catchedNode.WorldPosition;
			quat catchedWorldRotation = catchedNode.GetWorldRotation();

			triggerNode.AddChild(catchedNode);
			// тут важна последовательность восстановления параметров, 
			// вращение влияет на позицию потому задается первым
			catchedNode.SetWorldRotation(catchedWorldRotation);
			catchedNode.WorldPosition = catchedWorldPosition;

			catchedNode.ObjectBodyRigid.Gravity = false;

			// дельта позиции указывает на расстояние пойманного объекта до базовой точки привязки
			catchedLocalDelta = catchedNode.Position - catchPivot.Position;

			for (int i = 0; i != catchedNode.ObjectBodyRigid.NumShapes; ++i)
			{
				catchedNode.ObjectBody.GetShape(i).ExclusionMask = 1;
			}

			isCatched = true;
			isNewCatched = true;
		}
	}

	/// <summary>
	/// Отвязывает захваченный клешнёй объект
	/// </summary>
	private void DeleteCatchChild()
	{
		if (isCatched && mode == Mode.Child && catchedNode != null)
		{
			vec3 catchedNodeWorldPosition = catchedNode.WorldPosition;
			quat catchedNodeWorldRotation = catchedNode.GetWorldRotation();

			triggerNode.RemoveChild(catchedNode);

			catchedNode.SetWorldRotation(catchedNodeWorldRotation);
			catchedNode.Position = catchedNodeWorldPosition;
			
			catchedNode.ObjectBodyRigid.Gravity = true;

			for (int i = 0; i != catchedNode.ObjectBodyRigid.NumShapes; ++i)
			{
				catchedNode.ObjectBody.GetShape(i).ExclusionMask = 0;
			}

			catchedNode = null;
			isCatched = false;
		}
	}

	/// <summary>
	/// Обновляет позицию таскаемой за клешней ноды
	/// </summary>
	private void UpdateCatchPosition()
	{
		if (isCatched && mode == Mode.Child && catchedNode != null)
		{
			catchedNode.SetRotation(catchedLocalRotation);
			catchedNode.Position = (catchPivot.Position + catchedLocalDelta);
			
			Visualizer.Enabled = true;
			Visualizer.RenderLine3D(catchedNode.WorldPosition, catchPivot.WorldPosition, vec4.GREEN, 0.05f);
		}
	}

	/// <summary>
	/// Метод выполняющий заморозку/разморозку основных нод клешней.
	/// При этом происходит отключение основных нод, чтобы они не "болтались".
	/// True - заморозка, False - активация.
	/// </summary>
	/// <param name="flag"></param>
	private void FreezeClawHandler(bool flag)
	{
		foreach (Node node in controlNodes) 
		{
			//node.ObjectBodyRigid.Gravity = !flag;
		}

		isFreeze = flag;
	}

	/// <summary>
	/// Обновляет информацию по пойманному объекту и вызывает методы его привязки, в зависимости от режима работы
	/// </summary>
	private void UpdateCatchedNodeFromHandler()
	{
		if (!isCatched && collisionHandler.IsEnable())
		{
			var data = collisionHandler.GetCollisionData();
			int contactObjectId = collisionHandler.GetContactObjectId();

			if (!data.IsClear() && data.IsOneObjectCollision())
			{
				catchedNode = World.GetNodeByID(contactObjectId);

				switch (mode)
				{
					case Mode.Joint:
					MakeCatchJoints();
					break;

					case Mode.Child:
					MakeCatchChild();
					break;
				}

				AddAngle(-speed * Game.IFps * 10);
				
				isCatched = true;
			}
		}
	}

	/// <summary>
	/// Визуализирует триггер между клешнями
	/// </summary>
	private void RenderTriggerBox()
	{
		Visualizer.Enabled = true;
		ShapeBox box = triggerShape as ShapeBox;
		box.RenderVisualizer(vec4.BLUE);
	}

	/// <summary>
	/// Обновляет размер шейпа триггера, согласно позиций клешней
	/// </summary>
	private void UpdateTriggerShapeSize()
	{
		ShapeBox box = triggerShape as ShapeBox;

		vec3 new_size = box.Size;
		new_size.z = (Math.Abs(leftClaw.Position.z) - 0.04f) + (Math.Abs(rightClaw.Position.z) - 0.04f);
		new_size.y = (Math.Abs(leftClaw.Position.y) + (Math.Abs(rightClaw.Position.y)) / 2) - 0.1f;
		box.Size = new_size;

		//triggerShape.GetCollision()

		// "магические" числа 0,04 и 0,1 взяты опытным путём!!!!!
		// изменение позиции не работает!

		// vec3 new_pos = box.Position;
		// new_pos.y = -(Math.Abs(leftClaw.Position.y) + (Math.Abs(rightClaw.Position.y)) / 2) - 0.1f;
		// box.Position = new_pos;

		// Log.Message("Current LeftClawPos - {0}\n", leftClaw.Position);
		// Log.Message("Current RightClawPos - {0}\n", rightClaw.Position);
		// Log.Message("Current trigger Pos - {0}\n", new_pos);
		// Log.Message("Current trigger size - {0}\n", new_size);
	}

	private void Update()
	{
		UpdateCatchedNodeFromHandler();
		UpdateCatchedLocalRotation();
		UpdateCatchPosition();

		if (Input.IsKeyPressed(forwardKey))
		{
			RotateForward();
		}

		if (Input.IsKeyPressed(backwardKey))
		{
			RotateBackward();

			DeleteCatchChild();
			DeleteCatchJoint();
		}

		//RenderTriggerBox();
		//RenderCatchedJoint();
	}

	public void RotateForward()
	{
		if (!isCatched && AddAngle(speed * Game.IFps)) 
		{
			FreezeClawHandler(false);
			lastInputTime = DateTime.Now;
		}
	}

	public void RotateBackward()
	{
		if (AddAngle(-speed * Game.IFps)) 
		{
			FreezeClawHandler(false);
			lastInputTime = DateTime.Now;
		}
	}

	/// <summary>
	/// Проверяет время с последнего ввода с контроллера. Если разница во времени превышает параметр "freezeTime", то клешни будут заморожены
	/// </summary>
	public void UpdateLastControlTime()
	{
		if ((DateTime.Now - lastInputTime).Milliseconds > freezeTime)
		{
			if (!isFreeze)
			{
				FreezeClawHandler(true);
			}
		}
	}

	public void UpdateCatchedLocalRotation()
	{
		if (isNewCatched)
		{
			catchedLocalRotation = catchedNode.GetRotation();
			isNewCatched = false;
		}
	}

	void UpdatePhysics()
	{
		if (!isFreeze)
		{		
			foreach (var joint in clawJoints)
			{
    			if (joint.Enabled)
				{
					joint.AngularAngle = angle;
				}
			}
		}

		//UpdateTriggerShapeSize();
	}

	private void OnContactTouchedBodyVisualizer(Body body, int num) 
	{

		if (isEnable)
		{
			Visualizer.Enabled = true;
		
			if (body.IsContactEnter(num))
			{
				Body body0 = body.GetContactBody0(num);
				Body body1 = body.GetContactBody1(num);

				Body touchedBody = null;

				if (body0 && body0 != body) touchedBody = body0;  
				if (body1 && body1 != body) touchedBody = body1;
				
				if (touchedBody)
				{

					for (int i = 0; i != touchedBody.Object.NumSurfaces; i++)
					{
						Visualizer.RenderObjectSurface(touchedBody.Object, i, vec4.BLUE, 0.5f); 
					}

					//Visualizer.RenderObject(touchedBody.Object, vec4.BLUE, 0.5f); 
					
				}
				else
				{
					Visualizer.RenderObjectSurface(body.GetContactObject(num), body.GetContactSurface(num), vec4.BLUE, 0.5f); 
				}
			}
		}
	}
}