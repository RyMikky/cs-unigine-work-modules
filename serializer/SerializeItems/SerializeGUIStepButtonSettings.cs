using System;
using System.Collections;
using System.Collections.Generic;
using Unigine;

[Component(PropertyGuid = "70abb0c6a0ebd16dd17f03e8dd189627ec511d5a")]
public class SerializeGUIStepButtonSettings : SerializeBaseItem
{
	public SerializeGUIStepButtonSettings() {
		this.type = SERIAL_OBJECT_TYPE.GUI_STEP_BUTTON_SETTINGS;
	}
	public bool serializeble;
	private bool IsLoad = false;               // флаг уже загруженных настроек
	private bool IsSaved = false;              // флаг сохраненных настроек

	public int xPos;               // 325    1625
	public int yPos;               // 780     780
	public int width;              // 50       50
	public int height;             // 50       50
	public string label;           // 
	public int fontSize;           // 12       12

	[ParameterColor]
	public vec4 color;             // new vec4(0.42f,0.674f,0.89f,1)
	[ParameterColor]
	public vec4 fontColor;         // new vec4(1f,1f,1f,1)
	[ParameterFile]
	public string spriteImage;

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
				fileSource.WriteInt(width); 
				fileSource.WriteInt(height); 
				fileSource.WriteString(label);
				fileSource.WriteInt(fontSize); 

				fileSource.WriteVec4(color);
				fileSource.WriteVec4(fontColor);
				fileSource.WriteString(spriteImage);

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
						int income_width = fileSource.ReadInt();
						int income_height = fileSource.ReadInt();
						string inclome_label = fileSource.ReadString();  
						int income_fontSize = fileSource.ReadInt();

						vec4 inclome_color = fileSource.ReadVec4();
						vec4 inclome_fontColor = fileSource.ReadVec4();
						string inclome_spriteImage = fileSource.ReadString(); 

			
						if (!ReadTypeLabel(fileSource, DATA_END_SUFFIX)) throw new System.Exception("Node END_label reading is corrupt");

						xPos = income_xPos; yPos = income_yPos;
						width = income_width; height = income_height;
						label = inclome_label;
						fontSize = income_fontSize;
						
						color = inclome_color; fontColor = inclome_fontColor;
						spriteImage = inclome_spriteImage;

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