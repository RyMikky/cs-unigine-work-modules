using Unigine;

[Component(PropertyGuid = "ac85c6bab49226d16dbb31fc4676e52b90fab88e")]
public class ObjectHighlightHandler : Component
{
	public enum MODE 
	{
		DIRECT_THIS,             // управляет объектом за которым закреплен скрипт напрямую
		ABSTRACT_REF,            // выполняет изменения объектов по переданной ссылке
		STATE_DEBUG,             // состояние выполнения дебага для работы с кнопками
		STATE_ERROR,             // состояние ошибки
		STATE_DISABLE            // модуль выключен и не работает
	}

	[ShowInEditor][Parameter(Tooltip = "Режим выполнения скрипта")]
	private MODE mode = MODE.STATE_DISABLE;

	[ShowInEditor][Parameter(Tooltip = "Кнопка включения выборки")]
	private Input.KEY DebugEnableKey;
	[ShowInEditor][Parameter(Tooltip = "Кнопка отключения выборки")]
	private Input.KEY DebugDisableKey;
	[ShowInEditor][Parameter(Tooltip = "Материал пост-обработки, который применяется в данной сцене")]
	private Material postMaterial;

	private Object nodeObject;
	private vec4 auxiliaryMatchColor;
	private vec4 auxiliaryDummyColor = new vec4(0,0,0,0);

	private string text_error;

	private void ErrorLog(string text)
	{
		Log.Message("ERROR!\n");
		Log.Message("MODULE - \"ObjectHighlightHandler\"\n");
		Log.Message("NODE_ID - {0}\n", node.ID);
		Log.Message("TEXT - {0}\n", text);
	}

	void InitAuxColor()
	{
		if (mode != MODE.STATE_DISABLE && mode != MODE.STATE_ERROR && postMaterial != null)
		{
			// забираем у пост материала цвет для засветки
			auxiliaryMatchColor = postMaterial.GetParameterFloat4("match_color");
		}
		else 
		{
			text_error = "Initialisation error, auxiliary_color is not found";
			mode = MODE.STATE_ERROR;
		}
	}

	void InitNode()
	{
		switch (mode) 
		{
			case MODE.DIRECT_THIS:
				nodeObject = node as Object;
				break;
		}
	}

	private void Init()
	{
		InitNode();
		InitAuxColor();
	}


	public void SetAuxiliary(vec4 color)
	{
		SetMaterialAuxiliary(color);
	}

	public vec4 GetAuxiliaryMatchColor()
	{
		return auxiliaryMatchColor;
	}

	private void SetMaterialAuxiliary(vec4 color)
	{
		for (int i = 0; i != nodeObject.NumSurfaces; ++i)
		{
			if (node.IsObject)
			{
				Material mat = nodeObject.GetMaterial(i);
				SetMaterialAuxiliary(ref mat, color);
			}
		}
	}

	private void SetMaterialAuxiliary(Node target, vec4 color)
	{
		Object refNode = target as Object;

		if (refNode != null && refNode.IsObject)
		{
			for (int i = 0; i != refNode.NumSurfaces; ++i)
			{
				Material mat = refNode.GetMaterial(i);
				SetMaterialAuxiliary(ref mat, color);	
			}
		}
	}

	private void SetMaterialAuxiliary(ref Material mat, vec4 color)
	{
		mat.SetParameterFloat4("auxiliary_color", color);
	}

	private void Enable(bool enable)
	{
		if (enable)
		{
			SetMaterialAuxiliary(auxiliaryMatchColor);
		}
		else
		{
			SetMaterialAuxiliary(auxiliaryDummyColor);
		}
	}

	private void Enable(Node target, bool enable)
	{
		if (enable)
		{
			SetMaterialAuxiliary(target, auxiliaryMatchColor);
		}
		else
		{
			SetMaterialAuxiliary(target, auxiliaryDummyColor);
		}
	}

	public void HighlightEnable(bool enable)
	{
		Enable(enable);
	}

	public void HighlightEnable(Node target, bool enable)
	{
		Enable(target, enable);
	}
	
	void DebugInputHandle()
	{
		if (Input.IsKeyPressed(DebugEnableKey))
		{
			Enable(true);
		}

		if (Input.IsKeyPressed(DebugDisableKey))
		{
			Enable(false);
		}
	}
	
	private void Update()
	{
		switch (mode)
		{
			case MODE.STATE_ERROR:
				ErrorLog(text_error);
				break;

			case MODE.STATE_DEBUG:
				DebugInputHandle();
				break;
		}
	}
}