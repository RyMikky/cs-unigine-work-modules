using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Unigine;

[Component(PropertyGuid = "2e78ea4da7728748952e82070a4957ba8c4683d1")]
public class G_Manipulator_Claw_v01 : Component
{
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
	[ShowInEditor][Parameter(Tooltip = "Массив нод-клонов, которые \"следуют\" за основными и подменяют их в момент отключения")]
	private Node[] cloneNodes;
	[ShowInEditor][Parameter(Tooltip = "Точка привязки \"подхваченного\" клешнёй элемента")]
	private Node catchPivot;

	float angle = 0.0f;                             // текущий расчётный угол поворота

	private bool isFreeze = false;                  // флаг состояния активности основных нод

	private DateTime lastInputTime;                 // время последнего нажатия на кнопки управления

	private List<JointHinge> clawJoints = new List<JointHinge>();

	private ClawFinger leftFinger = null;
	private ClawFinger rightFinger = null;

	private bool isCatched = false;

	private Node catchedNode = null;
	private vec3 catchedDelta = new vec3();

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
			leftFinger = new ClawFinger(node)
			.InitRenderContacts()
			.InitContactEnterCallback();
		}

		if (body.Name == "RightFinger") { 
			rightFinger = new ClawFinger(node)
			.InitRenderContacts()
			.InitContactEnterCallback();
		}
	}

	private void Init()
	{
		foreach (var node in controlNodes)
		{
			InitJoints(node);
		}
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
			//node.Enabled = !(flag);
			
			//node.ObjectBodyRigid.Gravity = !flag;
			
			//node.ObjectBodyRigid.LinearVelocity = vec3.ZERO;
			//node.ObjectBodyRigid.AngularVelocity = vec3.ZERO;
		}

		isFreeze = flag;
	}

	/// <summary>
	/// Метод копирует параметры трансформа и позиции от нод в массиве "from" в ноды массива "to". Требует чтобы ноды в массивах были активны и длины массивов совпадали.
	/// </summary>
	/// <param name="from"></param>
	/// <param name="to"></param>
	private void UpdateNodesTransform(ref Node[] from, ref Node[] to)
	{
		if (isEnable && from.Length == to.Length) 
		{
			for (int i = 0; i != from.Length; ++i)
			{
				to[i].Transform = from[i].Transform;

				// if (to[i].ObjectBodyRigid.Enabled && from[i].ObjectBodyRigid.Enabled)
				// {
				// 	//to[i].WorldPosition = from[i].WorldPosition;
				// 	to[i].Transform = from[i].Transform;
				// }
			}
		}
	}

	private void Update()
	{
		if (Input.IsKeyPressed(forwardKey))
		{
			RotateForward();
		}

		if (Input.IsKeyPressed(backwardKey))
		{
			RotateBackward();
		}

		CheckFingersContact();
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

		ReleaseCatchedNode();
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

	private void UpdateCatchedPosition()
	{
		if (isCatched)
		{
			//catchedNode.Position = catchPivot.Position;
			//catchedNode.SetWorldRotation(catchPivot.GetWorldRotation());




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

			//UpdateNodesTransform(ref controlNodes, ref cloneNodes);
		}
		else 
		{
			//UpdateNodesTransform(ref cloneNodes, ref controlNodes);
		}

		//UpdateLastControlTime();
		//UpdateCatchedPosition();
	}

	void AddCatchedNode(int id)
	{
		if (catchedNode == null)
		{
			catchedNode = World.GetNodeByID(id);
			catchedNode.ObjectBodyRigid.Gravity = false;
			catchedNode.ObjectBodyRigid.Enabled = false;
			catchPivot.AddChild(catchedNode);
			isCatched = true;
		}
	}

	/// <summary>
	/// Отвязывает захваченный клешнёй объект
	/// </summary>
	void ReleaseCatchedNode()
	{
		if (isCatched && catchedNode != null)
		{
			vec3 catchedNodeWorldPosition = catchedNode.WorldPosition;
			quat catchedNodeWorldRotation = catchedNode.GetWorldRotation();

			catchPivot.RemoveChild(catchedNode);

			catchedNode.Position = catchedNodeWorldPosition;
			catchedNode.SetWorldRotation(catchedNodeWorldRotation);

			catchedNode.ObjectBodyRigid.Gravity = true;
			catchedNode.ObjectBodyRigid.Enabled = true;
			catchedNode = null;
			isCatched = false;
		}
	}

	void CheckFingersContact()
	{
		if (isEnable && !isFreeze && leftFinger != null && rightFinger != null) 
		{
			int leftContactsNum = leftFinger.GetNumContacts();
			int rightContactsNum = rightFinger.GetNumContacts();

			//Log.Message("Total contact count - {0}\n",(leftContactsNum + rightContactsNum));

			for (int i = 0; i != MathLib.Min(leftContactsNum, rightContactsNum); ++i)
			{
				int leftTObjectId = leftFinger.GetContactObjectId(i);
				int rightTObjectId = rightFinger.GetContactObjectId(i);
				string leftTObjectName = leftFinger.GetContactObjectName(i);
				string rightTObjectName = rightFinger.GetContactObjectName(i);

				if (leftTObjectId == rightTObjectId && !isCatched
					&& (leftContactsNum + rightContactsNum) >= 10 
					&& (leftTObjectName == "Brick_Small_Broken" || leftTObjectName == "Brick_Small")) 
				{
					//Log.Message("TouchedObject is same, We catch - {0}\n", leftTObjectName);
					//AddCatchedNode(leftTObjectId);
					return;
				}
			}
		}
	}

	private class ClawFinger {

		public ClawFinger (Node node) {
			_node = node;
			_body = node.ObjectBody;
			_id = _body.ID;
		}

		private Node _node { get; set; }
		private Body _body { get; set; }
		private int _id { get; set; }

		public ClawFinger InitRenderContacts()
		{
			_body.AddContactsCallback((b) => b.RenderContacts()); 
			return this;
		}

		public ClawFinger InitContactEnterCallback() {
			return InitContactEnterCallback(OnContactTouchedBodyVisualizer);
		}

		public ClawFinger InitContactEnterCallback(Body.ContactEnterDelegate action) {
			_body.AddContactEnterCallback(action);
			return this;
		}

		public int GetNumContacts()
		{
			return _body.NumContacts;
		}

		public string GetContactObjectName(int num)
		{
			return _body.GetContactObject(num).Name;
		}

		public int GetContactObjectId(int num)
		{		
			return _body.GetContactObject(num).ID;
		}

		private void OnContactTouchedBodyVisualizer(Body body, int num) 
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
					Visualizer.RenderObject(touchedBody.Object, vec4.BLUE, 0.5f); 
				}
				else
				{
					Visualizer.RenderObjectSurface(body.GetContactObject(num), body.GetContactSurface(num), vec4.BLUE, 0.5f); 
				}
			}
		}
	};
}