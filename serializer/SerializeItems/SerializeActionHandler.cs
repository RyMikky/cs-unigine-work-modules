using System;
using System.Collections;
using System.Collections.Generic;
using Unigine;

[Component(PropertyGuid = "349a6c5d048882188f8b1e97f16eab0a4b2d9ee1")]
public class SerializeActionHandler : SerializeBaseItem
{

	public SerializeActionHandler() {
		this._type = SERIAL_OBJECT_TYPE.ACTION_HANDLER;
	}

	private ActionManager _manager = null;

	/// <summary>
	/// Инициализирует скрипт управляющий менеджером действий
	/// </summary>
	private void InitManagerScript() 
	{
		if (IsNode()) 
		{
			_manager = node.GetComponent<ActionManager>();
		}
		else 
		{
			throw new System.Exception("InitManagerScript Error");
		}
	}

	private void Init()
	{
		InitManagerScript();		
	}
	
	
	public override void SerializeNodeData(Unigine.File fileSource)
	{
		if (IsNode()) 
		{
			WriteTypeLabel(fileSource, DATA_BEGIN_SUFFIX);
			if (_manager != null) 
			{
				WriteBodyFlag(fileSource, true);                                          // флаг наличия скрипта

				fileSource.WriteInt(node.ID);                                             // записываем ID текущей ноды
				fileSource.WriteString(node.Name);                                        // записываем название текущей ноды

				fileSource.WriteInt(_manager.GetActionIndex());                           // записываем индекс текущей операции

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
			// считывание объекта начнется в том случае если корректно считывается хеддер и флаг тела объекта
			if (IsNode() && _manager != null && ReadTypeLabel(fileSource, DATA_BEGIN_SUFFIX) && fileSource.ReadBool()) 
			{
				int nodeId = fileSource.ReadInt();                                    // получаем ID текущей ноды         
				string nodeName = fileSource.ReadString();                            // получаем название текущей ноды   
			
				if (node.ID == nodeId) 
				{
					
					int currentAction = fileSource.ReadInt();
					_manager.SetAction(currentAction);
		
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

}