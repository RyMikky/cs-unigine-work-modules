using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.NetworkInformation;
using Unigine;

[Component(PropertyGuid = "9f44304634f74aedc5fd2f68cc55cba7af3dd4da")]
public class SerializeSerialHandler : SerializeBaseItem
{

	static readonly string __STAGES_LIST_NODE_NAME__ = "HandlerStagesList";

	public SerializeSerialHandler() {
		this._type = SERIAL_OBJECT_TYPE.SERIAL_HANDLER;
	}

	private SerializeHandler _handler = null;

	/// <summary>
	/// Инициализирует скрипт обработчика сериализации
	/// </summary>
	private void InitHandlerScript() 
	{
		if (IsNode()) 
		{
			_handler = node.GetComponent<SerializeHandler>();
		}
		else 
		{
			throw new System.Exception("InitHandlerScript Error");
		}
	}

	private void Init()
	{
		InitHandlerScript();
	}
	
	public override void SerializeNodeData(Unigine.File fileSource)
	{
		if (_handler == null) InitHandlerScript();

		if (IsNode()) 
		{
			WriteTypeLabel(fileSource, DATA_BEGIN_SUFFIX);
			if (_handler != null) 
			{
				WriteBodyFlag(fileSource, true);                                          // флаг наличия скрипта

				fileSource.WriteInt(node.ID);                                             // записываем ID текущей ноды
				fileSource.WriteString(node.Name);                                        // записываем название текущей ноды

				_handler.GetGUIStageListBoxSettings().SerializeNodeData(fileSource);      // записываем настройки листа стадий
				_handler.GetGUINextStepButtonSettings().SerializeNodeData(fileSource);    // записываем настройки кнопки вперед
				_handler.GetGUIPrevStepButtonSettings().SerializeNodeData(fileSource);    // записываем настройки кнопки назад

				fileSource.WriteInt(_handler.GetDefStartIndex());                         // записываем индекс базового шага
				fileSource.WriteInt(_handler.GetCurStageIndex());                         // записываем индекс текущей стадии
				fileSource.WriteInt(_handler.GetCurStepIndex());                          // записываем индекс текущего шага

				// сериализация листа стадий производится только тогда когда она загружена впервые
				if (_handler.GetStageListStatus() == SerializeHandler.LIST_STATUS.LOADED)
				{
					int numStages = _handler.GetNumStages();                              // получаем количество стадий
					fileSource.WriteInt(numStages);                                       // записываем количество стадий

					for (int i = 0; i != numStages; ++i) 
					{
						_handler.GetStageByIndex(i).SerializeDescription(fileSource);     // записывает данные по указанной стадии
					}
				}
			}
			else 
			{
				WriteBodyFlag(fileSource, false);
			}
			
			WriteTypeLabel(fileSource, DATA_END_SUFFIX);
		}
	}

	public override void RestoreData(Unigine.File fileSource) 
	{
		try 
		{
			if (_handler == null) InitHandlerScript();
			// считывание объекта начнется в том случае если корректно считывается хеддер и флаг тела объекта
			if (IsNode() && _handler != null && ReadTypeLabel(fileSource, DATA_BEGIN_SUFFIX) && fileSource.ReadBool()) 
			{
				int nodeId = fileSource.ReadInt();                                    // получаем ID текущей ноды         
				string nodeName = fileSource.ReadString();                            // получаем название текущей ноды   
			
				if (node.ID == nodeId) 
				{
					_handler.GetGUIStageListBoxSettings().RestoreData(fileSource);      // восстанавливаем настройки листа стадий
					_handler.GetGUINextStepButtonSettings().RestoreData(fileSource);    // восстанавливаем настройки кнопки вперед
					_handler.GetGUIPrevStepButtonSettings().RestoreData(fileSource);    // восстанавливаем настройки кнопки назад

					int income_defStageIndex = fileSource.ReadInt();                    // получаем индекс базовой стадии
					int income_curStageIndex = fileSource.ReadInt();					// получаем индекс текущей стадии
					int income_curStepIndex = fileSource.ReadInt();                     // получаем индекс текущего шага

					// сериализация листа стадий производится только тогда когда она вообще не загружена
					if (_handler.GetStageListStatus() == SerializeHandler.LIST_STATUS.EMPTY)
					{
						RestoreHandlerStagesList(fileSource, fileSource.ReadInt());         // восстанавливаем список стадий
					}
		
					if (!ReadTypeLabel(fileSource, DATA_END_SUFFIX)) throw new System.Exception("Node END_label reading is corrupt");
				}
				else 
				{
					throw new System.Exception("Serial data node ID != Current node ID");
				}
			}
		}
		catch (System.Exception e)
		{
			throw new System.Exception(e.Message);
		}
	}

	/// <summary>
	/// Создаёт ноду пустышку с указаным именем у указанного родителя
	/// </summary>
	/// <param name="name"></param>
	/// <returns></returns>
	private Unigine.Node MakeSpecificNamedDummyNode(Unigine.Node parrent, string name = "")
	{
		Unigine.NodeDummy new_node = new NodeDummy();
		parrent.AddChild(new_node);
		new_node.Name = name;
		return new_node;
	}

	/// <summary>
	/// Возвращает ноду с указанным именем в указанном родителе. Если нода не найдена, то будет создана
	/// </summary>
	private Unigine.Node GetSpecificNamedNode(Unigine.Node parrent, string name)
	{
		Unigine.Node stageListNode =  GetChildNodeByName(parrent, name);
		return stageListNode != null ? stageListNode : MakeSpecificNamedDummyNode(parrent, name);
	}

	/// <summary>
	/// Ищет ноду в указанном родителе по названию, возвращает null или ноду если найдет
	/// </summary>
	/// <param name="parrent"></param>
	/// <param name="name"></param>
	/// <returns></returns>
	private Unigine.Node GetChildNodeByName(Unigine.Node parrent, string name)
	{
		int childNum = parrent.FindChild(name);
		return childNum < 0 ? null : parrent.GetChild(childNum);
	}

	/// <summary>
	/// Удаляет все наследниики в указанной ноде
	/// </summary>
	/// <param name="parrent"></param>
	/// <param name="numChildren"></param>
	private void RemoveOldChildren(Unigine.Node parrent, int numChildren)
	{
		for (int i = numChildren - 1; i >= 0; --i)
		{
			parrent.RemoveChild(parrent.GetChild(i));
		}
	}

	/// <summary>
	/// Восстанавливает данные по списку стадий
	/// </summary>
	/// <param name="fileSource"></param>
	/// <param name="numStages"></param>
	private void RestoreHandlerStagesList(Unigine.File fileSource, int numStages)
	{
		if (numStages > 0) 
		{
			// Создаём новый массив с описаниями стадий
			SerializeStageDesricpion[] stageList = new SerializeStageDesricpion[numStages];
			// Создаёт новую или подготовленную заранее ноду-коллектор описаний стедий
			Unigine.Node stageListNode = GetSpecificNamedNode(node, __STAGES_LIST_NODE_NAME__);
			
			// Удаляем все имеющиеся ноды наследники коллектора
			int numChildren = stageListNode.NumChildren; 
			if (numChildren != 0) RemoveOldChildren(stageListNode, numChildren);

			for (int i = 0; i != numStages; ++i)
			{
				// Создаём нового наследника
				Unigine.Node newChildren = MakeSpecificNamedDummyNode(stageListNode, "Stage_" + i.ToString());
				// Создаём компонент описания стадии
				SerializeStageDesricpion description = newChildren.AddComponent<SerializeStageDesricpion>();
				// Десереализируем данные по стадии
				description.RestoreDescription(fileSource);
				// Добавляем описание в массив
				stageList.SetValue(description, i);
			}

			// Загружаем в обработчик новый список стадий
			_handler.SetNewWorldStagesArray(stageList);
		}
	}
}