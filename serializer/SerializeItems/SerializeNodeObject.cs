using System;
using System.Collections;
using System.Collections.Generic;
using Unigine;

[Component(PropertyGuid = "5bd67492bb070ac997b79594b1ce83e36da317e6")]
public class SerializeNodeObject : SerializeItem
{

	public SerializeNodeObject() {
		this._type = SERIAL_OBJECT_TYPE.OBJECT_NODE;
	}

	private Unigine.Object _nodeObject;

	/// <summary>
	/// Инициализирует ноду как объект
	/// </summary>
	private void InitNodeObject() 
	{
		if (IsObject()) {
			_nodeObject = node as Unigine.Object;
		}
		else {
			throw new System.Exception("InitNodeObject Error");
		}
	}

	private void Init() 
	{
		InitNodeObject();
	}

	public override void SerializeNodeData(Unigine.File fileSource)
	{
		if (IsObject()) 
		{
			WriteTypeLabel(fileSource, DATA_BEGIN_SUFFIX);
			WriteBodyFlag(fileSource, true);

			fileSource.WriteInt(node.ID);                                             // записываем ID текущей ноды
			fileSource.WriteString(node.Name);                                        // записываем название текущей ноды
			fileSource.WriteMat4(node.WorldTransform);                                // матрицу трансформации
			fileSource.WriteQuat(node.GetWorldRotation());                            // кватернион поворота
			fileSource.WriteVec3(node.WorldPosition);                                 // мировую позицию
			fileSource.WriteVec3(node.WorldScale);                                    // мировой масштаб

			fileSource.WriteInt(_nodeObject.NumSurfaces);                             // записываем количество поверхностей
			
			for (int i = 0; i != _nodeObject.NumSurfaces; i++) {
				Material mat = _nodeObject.GetMaterial(i);                       
				fileSource.WriteVec4(mat.GetParameterFloat4("auxiliary_color"));      // сохраняем цвет засветки auxiliary
			}

			WriteTypeLabel(fileSource, DATA_END_SUFFIX);
		}
	}


	public override void RestoreData(Unigine.File fileSource) 
	{
		try 
		{
			// считывание объекта начнется в том случае если корректно считывается хеддер и флаг тела объекта
			if (ReadTypeLabel(fileSource, DATA_BEGIN_SUFFIX) && fileSource.ReadBool()) 
			{
				int nodeId = fileSource.ReadInt();                                    // получаем ID текущей ноды         
				string nodeName = fileSource.ReadString();                            // получаем название текущей ноды   
				mat4 nodeWorldTransform = fileSource.ReadMat4();                      // матрицу трансформации     
				quat nodeWorldRotation = fileSource.ReadQuat();                       // кватернион поворота
				vec3 nodeWorldPosition = fileSource.ReadVec3();                       // мировую позицию
				vec3 nodeWorldScale = fileSource.ReadVec3();                          // мировой масштаб
				int nodeNumSurfaces = fileSource.ReadInt();                           // количество поверхностей

				List<Unigine.vec4> auxColors = new List<vec4>();
				for (int i = 0; i != nodeNumSurfaces; ++i)
				{
					auxColors.Add(fileSource.ReadVec4());                             // цвета засветки поверхностей
				}

				if (node.ID == nodeId) 
				{
					node.WorldTransform = nodeWorldTransform;
					node.SetWorldRotation(nodeWorldRotation);
					node.WorldPosition = nodeWorldPosition;
					node.WorldScale = nodeNumSurfaces;

					for (int i = 0; i != nodeNumSurfaces; ++i)
					{
						Material mat = _nodeObject.GetMaterial(i);
						mat.SetParameterFloat4("auxiliary_color", auxColors[i]);
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