using System;
using System.Collections;
using System.Collections.Generic;
using Unigine;

/// <summary>
/// Подкласс для сериализации текстовых полей в игровом GUI
/// </summary>
[Component(PropertyGuid = "b0589c10bcad402f1d64efc25c086c39d58c2844")]
public class SerializeTextField : SerializeBaseItem
{
	public SerializeTextField() {
		this._type = SERIAL_OBJECT_TYPE.GUI_TEXT_FIELD;
	}

	private LabelInBox _script = null;

	/// <summary>
	/// Инициализирует скрипт управляющий размещением текста в боксе
	/// </summary>
	private void InitLabelInBoxScript() 
	{
		if (IsNode()) 
		{
			_script = node.GetComponent<LabelInBox>();
		}
		else 
		{
			throw new System.Exception("InitLabelInBoxScript Error");
		}
	}

	private void Init() 
	{
		InitLabelInBoxScript();
	}

	public override void SerializeNodeData(Unigine.File fileSource)
	{
		if (IsNode()) 
		{
			WriteTypeLabel(fileSource, DATA_BEGIN_SUFFIX);
			if (_script != null) 
			{
				WriteBodyFlag(fileSource, true);

				fileSource.WriteInt(node.ID);                                             // записываем ID текущей ноды
				fileSource.WriteString(node.Name);                                        // записываем название текущей ноды

				WidgetScrollBox box = _script.GetScrollBoxWidget();

				fileSource.WriteInt(box.Width);                                           // записываем длину поля
				fileSource.WriteInt(box.Height);                                          // записываем ширину поля
				fileSource.WriteVec4(box.BackgroundColor);                                // цвет бекграунда
				fileSource.WriteInt(box.Background);                                      // индекс бекграунда
				fileSource.WriteVec4(box.BorderColor);                                    // цвет границы
				fileSource.WriteVec4(box.HscrollColor);                                   // цвет прокрутки по вертикали
				fileSource.WriteVec4(box.VscrollColor);                                   // цвет прокрутки по ширине

				fileSource.WriteInt(box.PositionX);                                       // положение по горизонтали
				fileSource.WriteInt(box.PositionY);                                       // положение по вертикали
					
				WidgetLabel label = _script.GetTextWidget();

				fileSource.WriteInt(label.FontWrap);                                      // индекс шрифта
				fileSource.WriteInt(label.FontSize);                                      // размер шрифта
				fileSource.WriteInt(label.FontOutline);                                   // толщину обводной линии
				fileSource.WriteInt(label.PositionX);                                     // положение по горизонтали
				fileSource.WriteInt(label.PositionY);                                     // положение по вертикали

				fileSource.WriteString(label.Text);                                       // текстовое поле
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
			if (IsNode() && _script != null && ReadTypeLabel(fileSource, DATA_BEGIN_SUFFIX) && fileSource.ReadBool()) 
			{
				int nodeId = fileSource.ReadInt();                                    // получаем ID текущей ноды         
				string nodeName = fileSource.ReadString();                            // получаем название текущей ноды   
			
				if (node.ID == nodeId) 
				{
					
					int boxWidth = fileSource.ReadInt();                              // длину поля
					int boxHeight = fileSource.ReadInt();                             // ширину поля
					vec4 boxBackgroundColor = fileSource.ReadVec4();                  // цвет бекграунда
					int boxBackground = fileSource.ReadInt();                         // индекс бекграунда
					vec4 boxBorderColor = fileSource.ReadVec4();                      // цвет границы
					vec4 boxHscrollColor = fileSource.ReadVec4();                     // цвет прокрутки по вертикали
					vec4 boxVscrollColor = fileSource.ReadVec4();                     // цвет прокрутки по ширине
					int boxPositionX = fileSource.ReadInt();                          // положение по горизонтали
					int boxPositionY = fileSource.ReadInt();                          // положение по вертикали


					int labelFontWrap = fileSource.ReadInt();                         // индекс шрифта
					int labelFontSize = fileSource.ReadInt();                         // размер шрифта
					int labelFontOutline = fileSource.ReadInt();                      // толщину обводной линии
					int labelPositionX = fileSource.ReadInt();                        // положение по горизонтали
					int labelPositionY = fileSource.ReadInt();                        // положение по вертикали
					string labelText = fileSource.ReadString();                       // текстовое поле

					WidgetScrollBox box = _script.GetScrollBoxWidget();
					WidgetLabel label = _script.GetTextWidget();

					if (box == null && label == null) throw new System.Exception("Widget box or label is null");
					
					// box.Width = boxWidth;
					// box.Height = boxHeight;
					// box.BackgroundColor = boxBackgroundColor;
					// box.Background = boxBackground;
					// box.BorderColor = boxBorderColor;
					// box.HscrollColor = boxHscrollColor;
					// box.VscrollColor = boxVscrollColor;
					// box.SetPosition(boxPositionX, boxPositionY);

					// label.FontWrap = labelFontWrap;
					// label.FontSize = labelFontSize;
					// label.FontOutline = labelFontOutline;
					// label.SetPosition(labelPositionX, labelPositionY);
					label.Text = labelText;

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