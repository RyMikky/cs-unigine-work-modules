using System;
using System.Collections;
using System.Collections.Generic;
using Unigine;

[Component(PropertyGuid = "cc3feb671087f19ad5a39431bb91ffdceb46998b")]
public class SerializeNodeDisabler : SerializeBaseItem
{

	public SerializeNodeDisabler() {
		this.type = SERIAL_OBJECT_TYPE.NODE_DISABLER;
	}

	[ShowInEditor][Parameter(Tooltip = "Включает сериализацию объекта")]
	private bool serializeble;

	[ShowInEditor][Parameter(Tooltip = "Узлы, выбранные для отключения")]
	private List<Node> disabledNode = new List<Node>();

	private bool currentStatus = true;

	public bool DeactivateNodes()
	{
		return SetEnable(false);
	}

	public bool ActivateNodes()
	{
		return SetEnable(true);
	}

	private bool SetEnable(bool flag)
	{
		try
		{
			if (flag != currentStatus)
			{
				for (int i = 0; i != disabledNode.Count; ++i)
				{	
					disabledNode[i].Enabled = flag;
				}

				currentStatus = flag;
			}
			
			return true;
		}
		catch (SystemException e)
		{
			throw new System.Exception($"SetEnable(bool {flag})" + e.Message);
		}
	}

	public override void SerializeNodeData(Unigine.File fileSource)
	{
		if (!serializeble) return;

		WriteTypeLabel(fileSource, DATA_BEGIN_SUFFIX);
		int numNodes = disabledNode.Count;

		if (numNodes != 0) 
		{
			WriteBodyFlag(fileSource, true);                                          // флаг наличия скрипта
			WriteBodyFlag(fileSource, currentStatus);                                 // текущее состояние элементов

			fileSource.WriteInt(numNodes);                                            // записываем количество загруженных анимаций
			for (int i = 0; i != numNodes; ++i)
			{
				fileSource.WriteInt(disabledNode[i].ID);                              // записываем ID ноды
				fileSource.WriteString(disabledNode[i].Name);                         // записываем название ноды
			}
		}
		else 
		{
			WriteBodyFlag(fileSource, false);
		}
		
		WriteTypeLabel(fileSource, DATA_END_SUFFIX);
	}

	public override void RestoreData(Unigine.File fileSource) 
	{
		if (!serializeble) return;

		try 
		{
			// считывание объекта начнется в том случае если корректно считывается хеддер и флаг тела объекта
			if (ReadTypeLabel(fileSource, DATA_BEGIN_SUFFIX) && fileSource.ReadBool()) 
			{
				bool flag = fileSource.ReadBool();
				int numNodes = fileSource.ReadInt();

				for (int i = 0; i != numNodes; ++i)
				{
					Node incomeNode = World.GetNodeByID(fileSource.ReadInt());
					if (incomeNode.Name != fileSource.ReadString() 
						|| incomeNode != disabledNode[i]) throw new System.Exception("Node END_label reading is corrupt");
				}
		
				if (!ReadTypeLabel(fileSource, DATA_END_SUFFIX)) throw new System.Exception("Node END_label reading is corrupt");

				if (flag != currentStatus) SetEnable(flag);
				
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