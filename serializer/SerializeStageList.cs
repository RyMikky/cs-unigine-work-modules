using System;
using System.Collections;
using System.Collections.Generic;
using Unigine;

[Component(PropertyGuid = "024d2f56ba9c9ebd9e1750f87618dff7040dbcea")]
public class SerializeStageList : Component
{

	public enum MODE {
		DISABLE, REWRITE, READ_ONLY
	}

	[ShowInEditor]
	private MODE mode = MODE.DISABLE;

	public int x = 600;
    public int y = 50;
    public int fontSize = 16;
    public int width=100;
    public int height=50;

    [ParameterColor]
    public vec4 color = new vec4(0.42f,0.674f,0.89f,1);

	[ShowInEditor]
    private string[] stagesLabel;

	[ShowInEditor]
	private SerializeUnit serializeUnit;

	private WidgetComboBox stagesBox = null;


	/// <summary>
	/// Выполняет инициализацию всех калбеков в зависимости от установленного режима
	/// </summary>
	private void InitCallBack() 
	{
		switch (mode) 
		{
			case MODE.READ_ONLY:
				stagesBox.AddCallback(Gui.CALLBACK_INDEX.CHANGED, () => LoadStage(GetCurrentItemEngName()));
				break;

			case MODE.REWRITE:
				stagesBox.AddCallback(Gui.CALLBACK_INDEX.CHANGED, () => SaveStage(GetCurrentItemEngName()));
				break;
		}
	}

	private void InitGuiModule() 
	{
		Gui gui = Gui.GetCurrent();

        stagesBox = new WidgetComboBox(gui);
        stagesBox.SetPosition(x, y);
        stagesBox.FontSize = fontSize;
        stagesBox.Width=width;
        stagesBox.Height=height;
        stagesBox.BorderColor=color;
        stagesBox.ButtonColor=color;
        stagesBox.SelectionColor=color;
        stagesBox.ListBackgroundColor=color;
        stagesBox.MainBackgroundColor=color;

        foreach (var text in stagesLabel)
        {
            stagesBox.AddItem(text);
        }

		InitCallBack();

        gui.AddChild(stagesBox, Gui.ALIGN_OVERLAP);
	}

	/// <summary>
	/// Инициализирует свой собственный юнит, если не указан в качестве параметра в редакторе
	/// </summary>
	private void InitSerialUnit() 
	{
		if (serializeUnit == null) serializeUnit = new SerializeUnit().ExternalInit();
	}

	private void Init()
	{
		InitSerialUnit(); 
		InitGuiModule();
	}

    private static readonly Dictionary<char, char> __CHAR_TO_ENG_CHAR__ = new Dictionary<char, char>
	{
        { 'а', 'a'}, {'б', 'b'}, {'в', 'v'}, {'г', 'g'}, {'д', 'd'}, {'е', 'e'}, {'ё', 'e'}, /*{'ж', 'zh'}*/ {'з', 'z'}, {'и', 'i'}, {'й', 'y'}, 
		{'к', 'k'}, {'л', 'l'}, {'м', 'm'}, {'н', 'n'}, {'о', 'o'}, {'п', 'p'}, {'р', 'r'}, {'с', 's'}, {'т', 't'}, {'у', 'u'}, {'ф', 'f'}, 
		{'х', 'h'}, {'ц', 'c'}, /*{'ч', 'ch'} {'ш', 'sh'}, {'щ', 'sh\''}*/ {'ъ', '^'}, {'ы', 'i'}, {'ь', '\''}, {'э', 'e'}, /*{'ю', 'yu'}, {'я', 'ya'}*/

		{ 'А', 'A'}, {'Б', 'B'}, {'В', 'V'}, {'Г', 'G'}, {'Д', 'D'}, {'Е', 'E'}, {'Ё', 'E'}, /*{'Ж', 'ZH'}*/ {'З', 'Z'}, {'И', 'I'}, {'Й', 'Y'}, 
		{'К', 'K'}, {'Л', 'L'}, {'М', 'M'}, {'Н', 'N'}, {'О', 'O'}, {'П', 'P'}, {'Р', 'R'}, {'С', 'S'}, {'Т', 'T'}, {'У', 'U'}, {'Ф', 'F'}, 
		{'Х', 'H'}, {'Ц', 'C'}, /*{'Ч', 'CH'} {'Ш', 'SH'}, {'Щ', 'SH\''}*/ {'Ъ', '^'}, {'Ы', 'I'}, {'Ь', '\''}, {'Э', 'E'}, /*{'Ю', 'YU'}, {'Я', 'YA'}*/
	};

	private static readonly Dictionary<char, string> __CHAR_TO_ENG_STRING__ = new Dictionary<char, string>
	{
        {'ж', "zh"}, {'ч', "ch"}, {'ш', "sh"}, {'щ', "sh\'"}, {'ю', "yu"}, {'я', "ya"},
		{'Ж', "ZH"}, {'Ч', "CH"}, {'Ш', "SH"}, {'Щ', "SH\'"}, {'Ю', "YU"}, {'Я', "YA"}
	};

	/// <summary>
	/// 
	/// </summary>
	/// <param name="label"></param>
	/// <returns></returns>
	private string ConvertItemNameToEng(string label) 
	{
		label = label.ToLower();
		string result = "";

		for (int i = 0; i != label.Length; ++i) {
			
			if (label[i] == ' ') {
				result += new string("_");
			}

			else if (__CHAR_TO_ENG_CHAR__.ContainsKey(label[i]) 
				&& __CHAR_TO_ENG_CHAR__.TryGetValue(label[i], out char c)) 
			{
				result += new string("" + c);
			} 
			else if (__CHAR_TO_ENG_STRING__.ContainsKey(label[i]) 
				&& __CHAR_TO_ENG_STRING__.TryGetValue(label[i], out string str)) {
				result += str;
			}
			else {
				result += new string("" + label[i]);
			}
		}
		return result;
	}

	/// <summary>
	/// Возвращае название текущего выбранного элемента бокса
	/// </summary>
	/// <returns></returns>
	private string GetCurrentItemName() 
	{
		return stagesBox.GetItemText(stagesBox.CurrentItem);
	}

	/// <summary>
	/// Возвращает название текущего выбранного элемента бокс латиницей
	/// </summary>
	/// <returns></returns>
	private string GetCurrentItemEngName() 
	{
		return ConvertItemNameToEng(GetCurrentItemName());
	}

	/// <summary>
	/// Безопасно производит запрос на загрузку сохраненных данных
	/// </summary>
	/// <param name="stage"></param>
	/// <exception cref="System.Exception"></exception>
	public void LoadStage(string stage) 
	{
		if (serializeUnit != null) {
			serializeUnit.TryLoadStage(stage);
		}
		else {
			throw new System.Exception("SerializeStageList::LoadStage(string)::Error::Serial unit == null");
		}
		
	}

	/// <summary>
	/// Безопасно перезаписывает данные по выбранной стадии
	/// </summary>
	/// <param name="stage"></param>
	/// <exception cref="System.Exception"></exception>
	public void SaveStage(string stage) 
	{
		if (serializeUnit != null) {
			serializeUnit.TrySaveStage(stage, true);
		}
		else {
			throw new System.Exception("SerializeStageList::LoadStage(string)::Error::Serial unit == null");
		}
	}
	
	private void Update()
	{
		// write here code to be called before updating each render frame
		
	}

	private void Shutdown()
    {
        Gui.GetCurrent().RemoveChild(stagesBox);
    }
}