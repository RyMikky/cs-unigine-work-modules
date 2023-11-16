using System;
using System.Collections;
using System.Collections.Generic;
using Unigine;

/// <summary>
/// Подкласс предназначенный для выполнения сохранения/загрузки состояния контроллера анимации
/// </summary>
[Component(PropertyGuid = "ac886c81f330dbec8796265bb13a27fe50202f45")]
public class SerializeAnimationHandler : SerializeBaseItem
{
	
	public SerializeAnimationHandler() {
		this.type = SERIAL_OBJECT_TYPE.ANIMATION_HANDLER;
	}

	private AnimatorController controller = null;

	/// <summary>
	/// Инициализирует скрипт управляющий контроллером анимации
	/// </summary>
	private void InitControllerScript() 
	{
		if (IsNode()) 
		{
			controller = node.GetComponent<AnimatorController>();
		}
		else 
		{
			throw new System.Exception("InitControllerScript Error");
		}
	}

	private void Init()
	{
		InitControllerScript();		
	}

	public override void SerializeNodeData(Unigine.File fileSource)
	{
		if (controller == null) InitControllerScript();

		if (IsNode()) 
		{
			WriteTypeLabel(fileSource, DATA_BEGIN_SUFFIX);
			if (controller != null) 
			{
				WriteBodyFlag(fileSource, true);                                          // флаг наличия скрипта

				fileSource.WriteInt(node.ID);                                             // записываем ID текущей ноды
				fileSource.WriteString(node.Name);                                        // записываем название текущей ноды

				// если происходит проигрывание анимации на моменте сохранения, то останавливаем ее
				if (controller.GetPlayStatus() || controller.GetInversePlayStatus()) controller.Stop();

				fileSource.WriteInt(controller.GetCurrentAnimationIndex());               // записываем индекс текущей установленной анимации
				fileSource.WriteFloat(controller.GetCurrentAnimationTime());              // записываем время текущей установленной анимации
				fileSource.WriteFloat(controller.GetAnimationSpeed());                    // записываем установленную скорость анимации

				int numAnimation = controller.GetAnimatoinListSize();                     // получаем количество загруженных анимаций
				fileSource.WriteInt(numAnimation);                                        // записываем количество загруженных анимаций
				for (int i = 0; i != numAnimation; ++i)
				{
					fileSource.WriteString(controller.GetAnimationNameByIndex(i));        // записываем анимацию
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
			if (controller == null) InitControllerScript();
			// считывание объекта начнется в том случае если корректно считывается хеддер и флаг тела объекта
			if (IsNode() && controller != null && ReadTypeLabel(fileSource, DATA_BEGIN_SUFFIX) && fileSource.ReadBool()) 
			{
				int nodeId = fileSource.ReadInt();                                    // получаем ID текущей ноды         
				string nodeName = fileSource.ReadString();                            // получаем название текущей ноды   
			
				if (node.ID == nodeId) 
				{
					// если происходит проигрывание анимации на моменте загрузки, то останавливаем ее
					if (controller.GetPlayStatus() || controller.GetInversePlayStatus()) controller.Stop();

					int animationIndex = fileSource.ReadInt();
					float animationTime = fileSource.ReadFloat();
					float animationSpeed = fileSource.ReadFloat();
					int numAnimation = fileSource.ReadInt();

					List<string> animations = new List<string>();
					for (int i = 0; i != numAnimation; ++i)
					{
						animations.Add(fileSource.ReadString());
					}

					// если полученный из сохранения лист с анимациями не совпадает с имеющимися, то записываем загруженные из потока
					if (!ListStringCompare(controller.GetAnimationList(), animations)) controller.SetAnimationList(animations);

					controller.SetAnimationSpeed(animationSpeed);
					controller.SetAnimation(animationIndex);
		
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
	/// Сравнивает два контейнера на предмет существования данных, размеров коллекций и поэлементное сравнение
	/// </summary>
	/// <param name="controllerList"></param>
	/// <param name="serilizedList"></param>
	/// <returns></returns>
	private bool ListStringCompare(List<string> controllerList, List<string> serilizedList)
	{
		if (controllerList == null || serilizedList == null 
			|| controllerList.Count != serilizedList.Count) return false;

		for(int i = 0; i != controllerList.Count; ++i)
		{
			if (controllerList[i] != serilizedList[i]) return false;
		}

		return true;
	}
}