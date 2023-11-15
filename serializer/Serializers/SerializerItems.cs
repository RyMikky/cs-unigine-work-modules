using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unigine;

/// <summary>
/// Сериалайзер-наследник от SerialHandler.cs, работающий статически из AppSystemLogic.cs, сохраняющий только элементы отмеченные SerializeItem.cs или его наследниками
/// </summary>
[Component(PropertyGuid = "773070c299c3f93977405144dab82bd300da3f16")]
public class SerializerItems : SerializerBase
{

	private List<Node> worldSerialNodes = new List<Node>();       // список конкретных элементов содержащих SerializeItem
	private List<Widget> worldGuiWidgets = new List<Widget>();    // список всех виджетов гуя загруженного мира

	/// <summary>
	/// Сортировка всех листов с нодами, чтобы сохранения и загрузки выполнялись корректно
	/// </summary>
	protected override void SortAllNodeLists() 
	{
		Comparison<Unigine.Node> nodeCompararor = (lhs, rhs) => lhs.ID.CompareTo(rhs.ID);
		Comparison<Unigine.Widget> widgetComparator = (lhs, rhs) => lhs.Order.CompareTo(rhs.Order);

		worldSerialNodes.Sort(nodeCompararor);
		worldGuiWidgets.Sort(widgetComparator);
	}

	/// <summary>
	/// Проходит по всему списку виджетов и записывает в список
	/// </summary>
	private void InitGUIWidgets() 
	{
		if (worldGuiWidgets.Count != 0) worldGuiWidgets.Clear();

		Gui gui = Gui.GetCurrent();
		for (int i = 0; i != gui.NumChildren; i++) 
		{
			worldGuiWidgets.Add(gui.GetChild(i));
		}
	}

	/// <summary>
	/// Проходит по всему списку нод игрового мира и записывает в список те, которые имеют модуль SerializeItem
	/// </summary>
	private void InitSerialNodes() 
	{
		if (worldSerialNodes.Count != 0) worldSerialNodes.Clear();

		List<Node> worldNodes = new List<Node>();
		World.GetNodes(worldNodes);

		for (int i = 0; i != worldNodes.Count; ++i) 
		{
			if (worldNodes[i].GetComponent<SerializeBaseItem>() != null) worldSerialNodes.Add(worldNodes[i]);
		}
	}

	/// <summary> 
	/// Получает от игрового мира информацию по всем корневым нодам
	/// </summary>
	protected override void InitWorldData() 
	{
		InitGUIWidgets();
		InitSerialNodes();
		SortAllNodeLists();
	}

	public override SerializerItems ExternanInit()
	{
		SerializerInit(); return this;
	}

	private void Init()
	{
		SerializerInit();
	}
	
    protected override void SerializeWorldData()
    {
        if (FileSourceIsOpen())
		{
			foreach (Node node in worldSerialNodes)
			{
				node.GetComponent<SerializeBaseItem>().SerializeNodeData(fileSource);
			}
		}
		else 
		{
			throw new System.ArgumentException("STATIC_ITEM_SERIALIZER::SerializeWorldData()::fileStatus != STATUS.OPEN");
		}
    }

    protected override void LoadWorldData()
    {
        if (FileSourceIsOpen()) 
		{
			foreach (Node node in worldSerialNodes)
			{
				node.GetComponent<SerializeBaseItem>().RestoreData(fileSource);
			}
		}
		else 
		{
			throw new System.ArgumentException("STATIC_ITEM_SERIALIZER::SerializeWorldData()::fileStatus != STATUS.OPEN");
		}
    }
}