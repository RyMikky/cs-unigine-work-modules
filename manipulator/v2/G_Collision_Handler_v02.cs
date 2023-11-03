using System.Transactions;
using System;
using System.Collections;
using System.Collections.Generic;
using Unigine;

[Component(PropertyGuid = "f72213ce4c2c08d582fd4430752689cfff836ce2")]
public class G_Collision_Handler_v02 : Component
{

	[ShowInEditor][Parameter(Tooltip = "Включение работы скрипта")]
	private bool isEnable = false;

	[ShowInEditor][Parameter(Tooltip = "Нода левого пальца клешни манипулятора")]
	private Node leftFinger;
	private BodyRigid leftBody;

	[ShowInEditor][Parameter(Tooltip = "Нода правого пальца клешни манипулятора")]
	private Node rightFinger;
	private BodyRigid rightBody;

	[ShowInEditor][Parameter(Tooltip = "Минимальное количество точек контакта для принятия решения о захвате")]
	private int minCollisionCount;

	[ShowInEditor][Parameter(Tooltip = "Разница в расстояниях сумм компонент вектора, для принятия решения о захвате")]
	private float posDelta;

	[ShowInEditor][Parameter(Tooltip = "Включение визуализации объектов с которыми происходит контакт")]
	private bool visualContact = false;

	[ShowInEditor][Parameter(Tooltip = "Ноды-исключения, для которых коллизии обрабатываться не будут")]
	private Node[] exceptionNodes;

	private int[] exceptionBodyId;
	private int[] exceptionObjectId;

	bool isInit = false;                // флаг успешной инициализации

	private int leftNum;                // текущее количество контактов на левом пальце
	private int rightNum;               // текущее количество контактов на правом пальце

	private bool haveObject = false;

	private int contactBodyId;          // индекс тела с которым произошёл полноценный контакт
	private int contactObjectId;        // индекс объекта с которым произошёл полноценный контакт 
	private Unigine.Object contactObject;

	private G_CollisionResult collisionResult;
	private G_CollisionResult.CollisionData collisionData;

	public bool IsEnable()
	{
		return isEnable;
	}

	public bool IsInit()
	{
		return isInit;
	}

	public G_Collision_Handler_v02 SetEnable(bool flag)
	{
		isEnable = flag;
		return this;
	}

	public G_Collision_Handler_v02 SetVisualEnable(bool flag)
	{
		visualContact = flag;
		return this;
	}

	public G_Collision_Handler_v02 SetLeftFinger(Node node)
	{
		leftFinger = node;
		return this;
	}

	public G_Collision_Handler_v02 SetRightFinger(Node node)
	{
		rightFinger = node;
		return this;
	}

	public G_Collision_Handler_v02 ResetRefNodes()
	{
		leftBody = null;
		rightBody = null;

		leftFinger = null;
		rightFinger = null;

		contactBodyId = -1;
		leftNum = 0;
		rightNum = 0;

		isEnable = false;
		visualContact = false;
		isInit = false;

		return this;
	}

	public G_Collision_Handler_v02 InitFingerBodys()
	{
		if (CheckNodeStatus() && (!leftBody || !rightBody)){
			leftBody = leftFinger.ObjectBodyRigid;
			rightBody = rightFinger.ObjectBodyRigid;
		}

		if (visualContact){
			leftBody.AddContactsCallback((b) => b.RenderContacts());
			leftBody.AddContactEnterCallback(OnContactTouchedBodyVisualizer);

			rightBody.AddContactsCallback((b) => b.RenderContacts());
			rightBody.AddContactEnterCallback(OnContactTouchedBodyVisualizer);

			isInit = true;
		}

		contactObjectId = -1;
		contactBodyId = -1;
		return this;
	}

	/// <summary>
	/// Подготавливает списки с ID объектов и тел, для которых, коллизии клешней обрабатываться не будут
	/// </summary>
	private void PrepareExceptionID()
	{
		if (exceptionNodes.Length != 0)
		{
			exceptionBodyId = new int[exceptionNodes.Length];
			exceptionObjectId = new int[exceptionNodes.Length];

			for (int i = 0; i != exceptionNodes.Length; ++i)
			{
				exceptionBodyId[i] = exceptionNodes[i].ObjectBodyRigid.ID;
				exceptionObjectId[i] = exceptionNodes[i].ID;
			}
		}
	}

	private void Init()
	{
		InitFingerBodys();
		PrepareExceptionID();
		collisionResult = new G_CollisionResult();
	}

	/// <summary>
	/// Проверяет что ноды пальцев присутствуют
	/// </summary>
	/// <returns></returns>
	private bool CheckNodeStatus()
	{
		return leftFinger && rightFinger;
	}

	/// <summary>
	/// Проверяет достаточное расстояние между точками контакта в массиве
	/// </summary>
	/// <param name="points"></param>
	/// <returns></returns>
	private bool CheckCollisionsPosDelta(vec3[] points)
	{
		bool result = false;

		for (int i = 1; i != points.Length; ++i) 
		{
			var lhs = Math.Abs(points[i - 1].Sum);
			var rhs = Math.Abs(points[i].Sum);

			if (lhs > rhs)
			{
				result = ((lhs - rhs) >= posDelta);
			}
			else if (lhs < rhs)
			{
				result = ((rhs - lhs) >= posDelta);
			}

			if (result) {
				return result;
			}
		}
		return result;
	}

	/// <summary>
	/// Возвращает объект, с которым происходит соприкосновение. Принимает основное тело, относительно которого проверяется контакт и номер контакта
	/// </summary>
	/// <param name="body"></param>
	/// <param name="num"></param>
	/// <returns></returns>
	private Body GetContactBody(Body body, int num)
	{
		Body body0 = body.GetContactBody0(num);
		Body body1 = body.GetContactBody1(num);

		if (body0 && body0 != body) return body0;  
		if (body1 && body1 != body) return body1;

		return null;
	}

	private Unigine.Object GetContactObject(Body body, int num)
	{
		Unigine.Object obj = body.GetContactObject(num);

		if (obj.ID != leftFinger.ID && obj.ID != rightFinger.ID)
		{
			return obj;
		}

		return contactObject;
	}

	/// <summary>
	/// Метод проверяет условие контакта с одним и тем же объектом на обоих "пальцах"
	/// </summary>
	/// <returns></returns>
	private bool CheckSemiObjectContact()
	{
		/* 1. Определяем минимальное количество контактов */
		int minCurrentCollisionCount = Math.Min(leftNum, rightNum);
		int check_count = Math.Max(minCurrentCollisionCount, minCollisionCount);

		/* 2. Создаём новые данные по результатам обработки коллизий */
		collisionResult.SetNewDataSize(check_count);
		
		/* 3. Перебираем результаты коллизий для обоих пальцев сразу */ 
		for (int i = 0; i != check_count; ++i)
		{
			G_CollisionResult.CollisionData data = new G_CollisionResult.CollisionData(exceptionBodyId, exceptionObjectId);

			data._leftContactBody = GetContactBody(leftBody, i);;
			data._rightContactBody = GetContactBody(rightBody, i);;
			data._leftContactObject = leftBody.GetContactObject(i);;
			data._rightContactObject = rightBody.GetContactObject(i);;
			data.ProcessCollisionData();

			collisionResult.SetCollisionData(i, data);
		}

		collisionResult.ProcessCollisionResult();
		haveObject = collisionResult.GetResult();
		return haveObject;
	}

	/// <summary>
	/// Обновляет данные о объекте коллизии
	/// </summary>
	private void UpdateContactObject()
	{
		if (haveObject)
		{
			collisionData = collisionResult.GetResultData();
			contactBodyId = collisionData._leftContactBodyId;
			contactObjectId = collisionData._leftContactObjectId;
			contactObject = collisionData._leftContactObject;
		}
		else 
		{
			collisionData.ClearData();
			contactBodyId = -1;
			contactObjectId = -1;
			contactObject = null;
		}
	}

	/// <summary>
	/// Проверяет все коллизии на обоих пальцах на предмет геометрического соответствия
	/// </summary>
	/// <returns></returns>
	private bool CheckCollisionPointDelta()
	{
		vec3[] leftFingerCollisionPoints = new vec3[leftNum];
		vec3[] rightFingerCollisionPoints = new vec3[rightNum];

		/* Получаем координаты всех коллизий на каждом из пальцев */
		for (int i = 0; i != leftNum; ++i) 
		{
			leftFingerCollisionPoints[i] = leftBody.GetContactPoint(i);
		}

		for (int i = 0; i != rightNum; ++i) 
		{
			rightFingerCollisionPoints[i] = rightBody.GetContactPoint(i);
		}

		/* Проверяем разницу по суммам компонент-веторов */
		return CheckCollisionsPosDelta(leftFingerCollisionPoints) && CheckCollisionsPosDelta(rightFingerCollisionPoints);
	}

	/// <summary>
	/// Проверяет соответствие минимальному количеству контактов, также обнуляет ID объекта контакта
	/// </summary>
	/// <returns></returns>
	private bool CheckMinCollisionCount()
	{
		UpdateCurrentContactNum(); 

		bool lhs = leftNum >= minCollisionCount;
		bool rhs = rightNum >= minCollisionCount;

		//if ((!lhs || !rhs) && contactObjectId != -1) contactObjectId = -1;
		if (!lhs || !rhs)
		{
			haveObject = false;
			UpdateContactObject();
		}

		return lhs && rhs;
	}

	/// <summary>
	/// Обновляет количество контактов на каждом из пальцев клешни
	/// </summary>
	private void UpdateCurrentContactNum()
	{
		leftNum = leftBody.NumContacts;
		rightNum = rightBody.NumContacts;
	}

	/// <summary>
	/// Базовый информационный метод обновления состояния по коллизиям
	/// </summary>
	private void UpdateCollisionInfo()
	{
		/* 1. Проверяем соответсиве минимальному количеству коллизий */
		if (!CheckMinCollisionCount()) return;

		/* 2. Проверяем что минимальное количество контактов на обоих пальцах с одним и тем же объектом */
		if (!CheckSemiObjectContact()) return;

		/* 3. Обновляем данные о объекте коллизии */
		UpdateContactObject();

		/* 3. Получаем координаты всех коллизий на каждом из пальцев */
		bool pos = CheckCollisionPointDelta();

		if (pos)
		{
			//Log.Message("We Take the Object");
		}
		
	}

	/// <summary>
	/// Возвращает ID объекта с которым произошёл контакт, в противном случае возвращает -1;
	/// </summary>
	/// <returns></returns>
	public int GetContactObjectId()
	{
		return contactObjectId;
	}

	public Unigine.Object GetContactObject()
	{
		return contactObject;
	}

	/// <summary>
	/// Возвращает результирующие данные по коллизии
	/// </summary>
	/// <returns></returns>
	public G_CollisionResult.CollisionData GetCollisionData()
	{
		return collisionData;
	}

	private void OnContactTouchedBodyVisualizer(Body body, int num) 
	{

		if (isInit && isEnable && visualContact)
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
					//Visualizer.RenderObject(touchedBody.Object, vec4.BLUE, 0.5f); 

					for (int i = 0; i != touchedBody.Object.NumSurfaces; i++)
					{
						Visualizer.RenderObjectSurface(touchedBody.Object, i, vec4.BLUE, 0.5f); 
					}

				}
				else
				{
					Visualizer.RenderObjectSurface(body.GetContactObject(num), body.GetContactSurface(num), vec4.BLUE, 0.5f); 
				}
			}
		}
	}
	
	private void Update()
	{
		if (isInit && CheckNodeStatus() && isEnable)
		{
			UpdateCollisionInfo();
		}
	}
}