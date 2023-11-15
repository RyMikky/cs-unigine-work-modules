using System;
using System.Collections;
using System.Collections.Generic;
using Unigine;

[Component(PropertyGuid = "68684b4f727340624182445923f8e1e4495743f3")]
public class SerializeGUIListBoxSettings : SerializeBaseItem
{
	public SerializeGUIListBoxSettings() {
		this._type = SERIAL_OBJECT_TYPE.GUI_LIST_BOX_SETTINGS;
	}
	public bool serializeble;
	private bool IsLoad = false;               // флаг уже загруженных настроек
	private bool IsSaved = false;              // флаг сохраненных настроек

	public int xPos;         // 50/600
	public int yPos;         // 50
	public int fontSize;     // 16
	public int width;        // 1000
	public int height;       // 500
	[ParameterColor]
	public vec4 color;       // new vec4(0.42f,0.674f,0.89f,1)

	public override void SerializeNodeData(Unigine.File fileSource)
	{
		if (!IsSaved) // настройки сериализуются только один раз
		{
			WriteTypeLabel(fileSource, DATA_BEGIN_SUFFIX);
			if (IsNode() && serializeble) 
			{
				WriteBodyFlag(fileSource, true);                                          // флаг наличия скрипта

				fileSource.WriteInt(node.ID);                                             // записываем ID текущей ноды
				fileSource.WriteString(node.Name);                                        // записываем название текущей ноды

				fileSource.WriteInt(xPos); 
				fileSource.WriteInt(yPos); 
				fileSource.WriteInt(fontSize); 
				fileSource.WriteInt(width); 
				fileSource.WriteInt(height); 
				fileSource.WriteVec4(color);

				IsSaved = true;

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
			if (!IsLoad) // настройки загружаются только один раз
			{
				// считывание объекта начнется в том случае если корректно считывается хеддер и флаг тела объекта
				if (IsNode() && serializeble && ReadTypeLabel(fileSource, DATA_BEGIN_SUFFIX) && fileSource.ReadBool()) 
				{
					int nodeId = fileSource.ReadInt();                                    // получаем ID текущей ноды         
					string nodeName = fileSource.ReadString();                            // получаем название текущей ноды   
				
					if (node.ID == nodeId) 
					{
						int income_xPos = fileSource.ReadInt();
						int income_yPos = fileSource.ReadInt();
						int income_fontSize = fileSource.ReadInt();
						int income_width = fileSource.ReadInt();
						int income_height = fileSource.ReadInt();
						vec4 inclome_color = fileSource.ReadVec4();

						if (!ReadTypeLabel(fileSource, DATA_END_SUFFIX)) throw new System.Exception("Node END_label reading is corrupt");

						xPos = income_xPos; yPos = income_yPos;
						fontSize = income_fontSize;
						width = income_width; height = income_height;
						color = inclome_color;

						IsLoad = true;

					}
					else 
					{
						throw new System.Exception("Serial data node ID != Current node ID");
					}
				}
			}
			
		}
		catch (System.Exception e)
		{
			throw new System.Exception(e.Message);
		}
	}
}