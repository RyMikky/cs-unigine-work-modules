using System;
using System.Collections;
using System.Collections.Generic;
using Unigine;

[Component(PropertyGuid = "468f6cb694cdfc9d88e05558e76c58d7d554204a")]
public class SerializeImageWidget : SerializeBaseItem
{

	public SerializeImageWidget() {
		this.type = SERIAL_OBJECT_TYPE.GUI_IMAGE_WIDGET;
	}

	private Image script = null;


	/// <summary>
	/// Инициализирует скрипт управляющий размещением текста в боксе
	/// </summary>
	private void InitImageScript() 
	{
		if (IsNode()) 
		{
			script = node.GetComponent<Image>();
		}
		else 
		{
			throw new System.Exception("InitImageScript Error");
		}
	}

	private void Init()
	{
		InitImageScript();
	}
	
	public override void SerializeNodeData(Unigine.File fileSource)
	{
		if (IsNode()) 
		{
			WriteTypeLabel(fileSource, DATA_BEGIN_SUFFIX);
			if (script != null) 
			{
				WriteBodyFlag(fileSource, true);                                          // флаг наличия скрипта

				fileSource.WriteInt(node.ID);                                             // записываем ID текущей ноды
				fileSource.WriteString(node.Name);                                        // записываем название текущей ноды

				WriteBodyFlag(fileSource, script.isActive);                              // флаг активного спрайта скрипта
				WidgetSprite sprite = script.GetCurrentSprite();                         // берем текущий спрайт
				fileSource.WriteString(sprite.Texture);                                   // записываем путь к спрайту

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
			if (IsNode() && script != null && ReadTypeLabel(fileSource, DATA_BEGIN_SUFFIX) && fileSource.ReadBool()) 
			{
				int nodeId = fileSource.ReadInt();                                    // получаем ID текущей ноды         
				string nodeName = fileSource.ReadString();                            // получаем название текущей ноды   
			
				if (node.ID == nodeId) 
				{
					
					bool active = fileSource.ReadBool();
					WidgetSprite sprite = script.GetCurrentSprite();
					sprite.Texture = fileSource.ReadString();
					script.SetActive(active);

					if(active)
					{
						script.SetEnable();
					}
					else 
					{
						script.SetDisable();
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

}