using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unigine;

[Component(PropertyGuid = "996305fb105b431efbbd4fe5403e40008d18335f")]
public class G_CollisionResult : Component
{	
	int _count;
	CollisionData _result;
	CollisionData[] _data;

	public G_CollisionResult SetNewDataSize(int count)
	{	
		_count = count;
		_data = new CollisionData[_count];

		return this;
	}

	/// <summary>
	/// Подготоавливает финальный результат обработки коллизий в текущем фрейме
	/// </summary>
	public void ProcessCollisionResult() 
	{
		_result.ClearData();   // предварительно затираем данные если были записанны до этого
		// словарь содержит ID объекта контакта в качестве ключа, и лист с индексами в _data ему соответствующие
		Dictionary<int, List<int>> objectSet = new Dictionary<int, List<int>>();

		for (int i = 0; i != _data.Length; i++)
		{
			if (_data[i].GetCollisionType() == CollisionData.CollisionType.None
			|| _data[i].GetCollisionType() == CollisionData.CollisionType.ExceptionObject
			|| _data[i].GetCollisionType() == CollisionData.CollisionType.ExceptionBody
			|| _data[i].GetCollisionType() == CollisionData.CollisionType.DifferentObject
			|| _data[i].GetCollisionType() == CollisionData.CollisionType.DifferentBody
			|| _data[i].GetCollisionType() == CollisionData.CollisionType.Landscape)
			{
				continue;
			}

			else
			{
				if (objectSet.ContainsKey(_data[i]._leftContactObjectId))
				{
					objectSet[_data[i]._leftContactObjectId].Add(i);
				}
				else
				{
					objectSet.Add(_data[i]._leftContactObjectId, new List<int>(){i});
				}
			}
		}

		int count = 0;
		foreach (var data in objectSet) 
		{
			if (_result.IsClear())
			{
				_result = _data[data.Value.First()];
				count = data.Value.Count();
			}
			else
			{
				if (count < data.Value.Count())
				{
					_result = _data[data.Value.First()];
					count = data.Value.Count();
				}
			}
		}
	}

	public bool GetResult()
	{
		if (_count <= 0) return false;

		int negativeCount = _data.Length / 2;

		foreach (var item in _data)
		{
			if (item.IsOneObjectCollision()) --negativeCount;

			if (negativeCount <= 0) return true;
		}

		if (negativeCount > 0)
		{
			return false;
		}
		 
		return false;
	}
	
	public void SetCollisionData(int index, CollisionData data)
	{
		if (index >= _count)
		{
			return;
		}

		_data[index] = data;
	}

	/// <summary>
	/// Возвращает флаг коллизии по индексу. Если указан неверный индекс вернёт false
	/// </summary>
	/// <param name="index"></param>
	/// <returns></returns>
	public bool GetIndexFlag(int index)
	{
		if (index >= _count)
		{
			return false;
		}

		return _data[index].IsOneObjectCollision();
	}	

	/// <summary>
	/// Возвращает данные по номеру коллизии. Если номер указан неверно вернет пустышку
	/// </summary>
	/// <param name="index"></param>
	/// <returns></returns>
	public CollisionData GetIndexData(int index)
	{
		if (index >= _count)
		{
			return new CollisionData();
		}

		return _data[index];
	}	

	/// <summary>
	/// Возвращает результирующие данные по коллизии
	/// </summary>
	/// <returns></returns>
	public CollisionData GetResultData()
	{
		return _result;
	}

	public struct CollisionData 
	{
		public enum CollisionType
		{
			None, OneBody, DifferentBody, ExceptionBody, OneObject, DifferentObject, ExceptionObject, Landscape
		}

		public CollisionData(int[] exceptionBodyId, int[] exceptionObjectId)
		{
			_leftContactBodyId = -1;
			_rightContactBodyId = -1;

			_leftContactBody = null;
			_rightContactBody = null;

			_leftContactObjectId = -1;
			_rightContactObjectId = -1;

			_leftContactObject = null;
			_rightContactObject = null;

			_exceptionBodyId = exceptionBodyId;
			_exceptionObjectId = exceptionObjectId;
		}

		public CollisionData(CollisionData other)
		{
			_isClear = other._isClear;
			_type = other._type;

			_leftContactBodyId = other._leftContactBodyId;
			_rightContactBodyId = other._rightContactBodyId;

			_leftContactBody = other._leftContactBody;
			_rightContactBody = other._rightContactBody;

			_leftContactObjectId = other._leftContactObjectId;
			_rightContactObjectId = other._rightContactObjectId;

			_leftContactObject = other._leftContactObject;
			_rightContactObject = other._rightContactObject;

			_exceptionBodyId = other._exceptionBodyId;
			_exceptionObjectId = other._exceptionObjectId;
		}

		public CollisionData(int leftContactBodyId, int rightContactBodyId, Unigine.Body leftContactBody, 
			Unigine.Body rightContactBody, int leftContactObjectId, int rightContactObjectId, 
			Unigine.Object leftContactObject, Unigine.Object rightContactObject, int[] exceptionBodyId, int[] exceptionObjectId)
		{
			_leftContactBodyId = leftContactBodyId;
			_rightContactBodyId = rightContactBodyId;

			_leftContactBody = leftContactBody;
			_rightContactBody = rightContactBody;

			_leftContactObjectId = leftContactObjectId;
			_rightContactObjectId = rightContactObjectId;

			_leftContactObject = leftContactObject;
			_rightContactObject = rightContactObject;

			_exceptionBodyId = exceptionBodyId;
			_exceptionObjectId = exceptionObjectId;

			if(_leftContactObject != null && _rightContactObject != null)
			{
				_isClear = false;
			}
		}

		private bool _isClear = true;
		private CollisionType _type = CollisionType.None;

		public int _leftContactBodyId {get; set;}
		public Unigine.Body _leftContactBody {get; set;}
		public int _leftContactObjectId {get; set;}                // индекс объекта с которым произошёл полноценный контакт 
		public Unigine.Object _leftContactObject {get; set;}

		public int _rightContactBodyId {get; set;} 
		public Unigine.Body _rightContactBody {get; set;}
		public int _rightContactObjectId {get; set;}               // индекс объекта с которым произошёл полноценный контакт 
		public Unigine.Object _rightContactObject {get; set;}

		public int[] _exceptionBodyId;
		public int[] _exceptionObjectId;

		public CollisionData SetLeftContactBodyID(int leftContactBodyId)
		{
			_leftContactBodyId = leftContactBodyId;
			return this;
		}

		public CollisionData SetLeftContactBody(Unigine.Body leftContactBody)
		{
			_leftContactBody = leftContactBody;
			return this;
		}

		public CollisionData SetLeftContactObjectID(int leftContactObjectId)
		{
			_leftContactObjectId = leftContactObjectId;
			return this;
		}

		public CollisionData SetLeftContactObject(Unigine.Object leftContactObject)
		{
			_leftContactObject = leftContactObject;
			UpdateIsClearFlag();
			return this;
		}

		public CollisionData SetRightContactBodyID(int rightContactBodyId)
		{
			_rightContactBodyId = rightContactBodyId;
			return this;
		}

		public CollisionData SetRightContactBody(Unigine.Body rightContactBody)
		{
			_rightContactBody = rightContactBody;
			return this;
		}

		public CollisionData SetRightContactObjectID(int rightContactObjectId)
		{
			_rightContactObjectId = rightContactObjectId;
			return this;
		}

		public CollisionData SetRightContactObject(Unigine.Object rightContactObject)
		{
			_rightContactObject = rightContactObject;
			UpdateIsClearFlag();
			return this;
		}

		/// <summary>
		/// Метод применяемый после загрузки данных, для проведения анализа данных по указанной коллизии
		/// </summary>
		/// <returns></returns>
		public CollisionData ProcessCollisionData()
		{
			if (_leftContactBody != null) 
			{
				_leftContactBodyId = _leftContactBody.ID;
				_leftContactObject = _leftContactBody.Object;
			}
			if (_rightContactBody != null)
			{
				_rightContactBodyId = _rightContactBody.ID;
				_rightContactObject = _rightContactBody.Object;
			}

			if (_leftContactObject != null) _leftContactObjectId = _leftContactObject.ID;
			if (_rightContactObject != null) _rightContactObjectId = _rightContactObject.ID;

			UpdateCollisionType();
			UpdateIsClearFlag();

			return this;
		}

		/// <summary>
		/// Сверяет текущий ID тела контакта со списком исключений
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		private bool CheckBodyExceptionId(int id)
		{
			foreach (int except in _exceptionBodyId)
			{
				if (except == id)
				{
					return true;
				}

				
			}

			return false;
		}

		/// <summary>
		/// Сверяет текущий ID объекта контакта со списком исключений
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		private bool CheckObjectExceptionId(int id)
		{
			foreach (int except in _exceptionObjectId)
			{
				if (except == id)
				{
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Определяет внутренне поле типа коллизии
		/// </summary>
		private void UpdateCollisionType()
		{
			if (_leftContactBodyId != -1 && _rightContactBodyId != -1)
			{
				if (CheckBodyExceptionId(_leftContactBodyId) || CheckBodyExceptionId(_rightContactBodyId))
				{
					_type = CollisionType.ExceptionBody;    // если какое-то из тел соответствует исключению
				}
				else if (_leftContactBodyId == _rightContactBodyId)
				{
					_type = CollisionType.OneBody;          // если есть тела соприкосновения, и они равны
				}
				else 
				{
					_type = CollisionType.DifferentBody;    // если тела есть, но по id они не равны
				}
			}
			else if (_leftContactObjectId != -1 && _rightContactObjectId != -1)
			{
				if (CheckObjectExceptionId(_leftContactObjectId) || CheckObjectExceptionId(_rightContactObjectId))
				{
					_type = CollisionType.ExceptionObject;  // если какой-то из объектов соответствует исключению
				}
				else if (_leftContactObjectId == _rightContactObjectId)
				{
					if (_leftContactObject.Type == Node.TYPE.OBJECT_LANDSCAPE_TERRAIN)
					{
						_type = CollisionType.Landscape;         // может быть контакт с ландшафтом
					}
					else 
					{
						_type = CollisionType.OneObject;        // если тела нет (например коллизия с поверхностью), но объекты равны
					}
				}
				else 
				{
					_type = CollisionType.DifferentObject;        // если тела нет, и объекты разные
				}
			}
			else 
			{
				_type = CollisionType.None;                 // нет ни тел ни объектов
			}
		}

		/// <summary>
		/// Обновляет состояние флага пустых данных
		/// </summary>
		private void UpdateIsClearFlag()
		{
			if (_leftContactObject != null && _rightContactObject != null)
			{
				_isClear = false;
			}
			else 
			{
				_isClear = true;
			}
		}

		/// <summary>
		/// Возвращает флаг пустых данных или наличе полноценной коллизии
		/// </summary>
		/// <returns></returns>
		public bool IsClear()
		{
			return _isClear;
		}

		/// <summary>
		/// Стирает все данные по текущей коллизии
		/// </summary>
		public void ClearData()
		{
			_leftContactBodyId = -1;
			_rightContactBodyId = -1;

			_leftContactBody = null;
			_rightContactBody = null;

			_leftContactObjectId = -1;
			_rightContactObjectId = -1;

			_leftContactObject = null;
			_rightContactObject = null;

			_exceptionBodyId = null;
			_exceptionObjectId = null;

			_isClear = true;
			_type = CollisionType.None;
		}

		/// <summary>
		/// Возвращает тип коллизии
		/// </summary>
		/// <returns></returns>
		public CollisionType GetCollisionType()
		{
			return _type;
		}

		/// <summary>
		/// Возвращает флаг коллизии с одним объектом (включая коллизию с одним телом)
		/// </summary>
		/// <returns></returns>
		public bool IsOneObjectCollision()
		{
			if (!IsClear() && (_type == CollisionType.OneBody || _type == CollisionType.OneObject))
			{
				return true;
			}

			return false;
		}
	}
}