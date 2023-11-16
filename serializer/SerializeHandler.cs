using System.Net.Sockets;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using Unigine;
using System.Dynamic;

[Component(PropertyGuid = "024d2f56ba9c9ebd9e1750f87618dff7040dbcea")]
public class SerializeHandler : Component, ISProvider
{

	/// <summary>
	/// Данные по шагу. Может являться основной стадией (шаг 1) или шагом N внутри какой-то основной стадии
	/// </summary>
	struct StepInfo 
	{
		public bool isStep;
		public bool isStage;

		public int stepIndex;
		public int stageIndex;

		public string stepName;
		public string stageName;		

		public StepInfo(bool isStp, int stepIdx, int stageIdx, string stepNm, string stageNm)
		{
			isStage = !isStp;
			isStep = isStp;

			stepIndex = stepIdx; stageIndex = stageIdx;
			stepName = stepNm; stageName = stageNm;
		}

		public string GetTotalStepName() { return stageName + "_" + stepName; }
	}

	/// <summary>
	/// Режим работы обработчика. DISABLE - отключен, REWRITE - записывает данные по стадиям, READ_ONLY - загружает данные из ранее сохраненных файлов
	/// </summary>
	public enum MODE 
	{
		DISABLE, READ_ONLY, REWRITE_AUTO, REWRITE_MANUAL, AUTO_REW_READ
	}

	[ShowInEditor][Parameter(Tooltip = "Флаг выбора режима работы обработчика сериализации")]
	private MODE mode = MODE.DISABLE;
	public void SetHandlerMode(MODE flag) { mode = flag; }
	public MODE GetHandlerMode() { return mode; }

	[ShowInEditor][Parameter(Tooltip = "Флаг использования суффикса-приписки к одиночной стадии без шагов")]
	private bool useSigleStageStepSuffix = true;
	[ShowInEditor][Parameter(Tooltip = "Приписка к одиночной стадии без шагов")]
	private string singleStageStepSuffix = "Шаг 1";

	// ----------------------------------- параметры комбобокса выбора стадий проекта ------------------------------------------------

	public SerializeGUIListBoxSettings stageListBoxSettings;
	public SerializeGUIListBoxSettings GetGUIStageListBoxSettings() { return stageListBoxSettings; }
	public SerializeHandler SetGUIStageListBoxSettings(SerializeGUIListBoxSettings settings)
	{
		stageListBoxSettings = settings;
		return this;
	}

	// ----------------------------------- параметры кнопок перехода по шагам проекта ------------------------------------------------

	private enum BUTTON
	{
		NEXT, PREV, NONE
	}

	public SerializeGUIStepButtonSettings prevStepButtonSettings;
	public SerializeGUIStepButtonSettings GetGUIPrevStepButtonSettings() { return prevStepButtonSettings; }
	public SerializeHandler SetGUIPrevStepButtonSettings(SerializeGUIStepButtonSettings settings)
	{
		prevStepButtonSettings = settings;
		return this;
	}
	public SerializeGUIStepButtonSettings nextStepButtonSettings;
	public SerializeGUIStepButtonSettings GetGUINextStepButtonSettings() { return nextStepButtonSettings; }
	public SerializeHandler SetGUINextStepButtonSettings(SerializeGUIStepButtonSettings settings)
	{
		nextStepButtonSettings = settings;
		return this;
	}

	// ----------------------------------- внутренние поля класса-обработчика сериализации -------------------------------------------
	
	private SerializerBase serializer = null;                                                  // ссылка на класс сериализатор
	private string defaultWorldStageName = "";

	private List<StepInfo> mainStagesList = new List<StepInfo>();                              // список основных стадий, без учёта шагов
	private Dictionary<int, StepInfo> totalStageSteps = new Dictionary<int, StepInfo>();       // общий список всех стадий и шагов по очереди

	private List<ISDisabler> disabledElements = new List<ISDisabler>();                        // элементы отключаемые в режиме READ_ONLY
	private bool disabledElementsFlag = true;                                                  // флаг состояния отключаемых элементов

	/// <summary>
	/// Статус состояния листа шагов и стадий, применяется для управления сохранениями и загрузкой листа
	/// </summary>
	public enum LIST_STATUS
	{
		EMPTY, LOADED, ACTUAL
	}

	private LIST_STATUS stageListStatus = LIST_STATUS.EMPTY;                                   // флаг состояния списка стадий

	/// <summary>
	/// Возвращает текущий статус листа стадий
	/// </summary>
	/// <returns></returns>
	public LIST_STATUS GetStageListStatus() { return stageListStatus; }

	/// <summary>
	/// Устанавливает статус по состоянию листа стадий
	/// </summary>
	/// <param name="status"></param>
	public void SetStageListStatus(LIST_STATUS status) { stageListStatus = status; } 

	/// <summary>
	/// Метод удаляет всё данные в листах и словарях по стадиям и шагам
	/// </summary>
	private void ClearAllStagesToStepsListData()
	{
		HANDLER_STEPS_COUNT = 0;
		mainStagesList.Clear();
		totalStageSteps.Clear();
		stageListStatus = LIST_STATUS.EMPTY;
	}

	private WidgetComboBox stageListBox = null;                                                // заготовка под GUI лист стадий
	private WidgetButton prevStepButton = null;                                                // заготовка под кнопку перехода на предыдущий шаг
	private WidgetButton nextStepButton = null;                                                // заготовка под кнопку перехода на следующий шаг

	private IntPtr listBoxCallbackPtr;                                                         // заготовка под указатель калбека листа стадий
	private IntPtr prevStepCallbackPtr;                                                        // заготовка под указатель калбека кнопки назад
	private IntPtr nextStepCallbackPtr;                                                        // заготовка под указатель калбека кнопки вперед


	[ShowInEditor][Parameter(Tooltip = "Список стадий игрового мира")]
	SerializeStageDesricpion[] worldStages;

	/// <summary>
	/// Возвращает количество стадий на текущий момент
	/// </summary>
	/// <returns></returns>
	public int GetNumStages() { return worldStages.Length; }

	/// <summary>
	/// Возвращает описание стадии по индексу
	/// </summary>
	/// <param name="idx"></param>
	/// <returns></returns>
	/// <exception cref="System.IndexOutOfRangeException"></exception>
	public SerializeStageDesricpion GetStageByIndex(int idx)
	{
		if (idx >= 0 && idx < GetNumStages()) return worldStages[idx];
		throw new System.IndexOutOfRangeException("Index is out of range");
	} 

	/// <summary>
	/// Устанавливает новый список стадий
	/// </summary>
	/// <param name="newWorldStages"></param>
	public void SetNewWorldStagesArray(SerializeStageDesricpion[] newWorldStages) { worldStages = newWorldStages; }

	[ShowInEditor][Parameter(Tooltip = "Индекс открывающего шага при загрузке")]
	private int defStartIndex = 0;
	public int GetDefStartIndex() { return defStartIndex; }
	private int curStageIndex = 0;                                                             // индекс основных стадий
	public int GetCurStageIndex() { return curStageIndex; }

	private int prevStepIndex = 0;                                                             // индекс предыдущего шага
	public int GetPrevStepIndex() { return prevStepIndex; }
	private int curStepIndex = 0;                                                              // индекс текущего шага
	public int GetCurStepIndex() { return curStepIndex; }
	private int nextStepIndex = 0;                                                             // индекс следующего шаха
	public int GetNextStepIndex() { return curStepIndex; }

	private static int HANDLER_STEPS_COUNT = 0;

	// ----------------------------------- блок обработки индексов для перехода по шагам ---------------------------------------------

	private bool IsEmptyStages() { return mainStagesList.Count == 0; }
	private bool IsEmptySteps() { return totalStageSteps.Count == 0; }
	private bool IsOneStage() { return mainStagesList.Count == 1; }
	private bool IsOneStep() { return totalStageSteps.Count == 1; }
	private bool IsTwoStep() { return totalStageSteps.Count == 2; }

	/// <summary>
	/// Устанавливает индекс текущей стадии
	/// </summary>
	/// <param name="idx"></param>
	/// <exception cref="System.IndexOutOfRangeException"></exception>
	private void SetCurStageIndex(int idx)
	{
		if (idx >= 0 && idx < mainStagesList.Count)
		{
			curStepIndex = mainStagesList[idx].stepIndex;
			UpdateStepIndexes();
			UpdateCurStageIndex();
		}
		else 
		{
			throw new System.IndexOutOfRangeException("Index is out of range");
		}
	}

	/// <summary>
	/// Обновляет индекс текущей стадии
	/// </summary>
	private void UpdateCurStageIndex()
	{
		curStageIndex = totalStageSteps[curStepIndex].stageIndex;
		if (stageListBox != null) stageListBox.CurrentItem = curStageIndex;
	}

	/// <summary>
	/// Проверяет соответствие текущего установленного индекса стадии, установленному в списке элемента GUI
	/// </summary>
	/// <returns></returns>
	private bool CheckCurStageIndex()
	{
		return curStageIndex == stageListBox.CurrentItem ? true : false;
	}

	/// <summary>
	/// Обновляет предыдущий и последующий индексы шагов в обработчике
	/// </summary>
	private void UpdateStepIndexes()
	{
		if (IsEmptySteps() || IsOneStep()) return;          // если шагов нет или он один, то выходим и ничего не делаем ибо значения не будет

		if (IsTwoStep())                                    // если всего два шага, то обрабатываем перещелкивание 0/1
		{
			if (curStepIndex == 0) 
			{
				prevStepIndex = nextStepIndex = curStepIndex++;
			}
			else 
			{
				prevStepIndex = nextStepIndex = curStepIndex++;
			}
		}
		else
		{
			if (curStepIndex == 0) 
			{
				prevStepIndex = totalStageSteps.Count - 1;
				nextStepIndex = curStepIndex + 1;
			}
			else if (curStepIndex == totalStageSteps.Count - 1)
			{
				prevStepIndex = curStepIndex - 1;
				nextStepIndex = 0;
			}
			else 
			{
				prevStepIndex = curStepIndex - 1;
				nextStepIndex = curStepIndex + 1;
			}
		}
	}

	/// <summary>
	/// Инкрементирует индексы шагов
	/// </summary>
	private void StepIncrement()
	{
		if (IsEmptySteps() || IsOneStep()) return;          // если шагов нет или он один, то выходим и ничего не делаем ибо значения не будет

		if (!IsTwoStep())                                   // если шагов больше двух, то выставляем требуемое смещение
		{
			if (curStepIndex + 1 == totalStageSteps.Count)
			{
				curStepIndex = 0;
			}
			else 
			{;
				curStepIndex++;
			}
		}
	
		UpdateStepIndexes();
		UpdateCurStageIndex();
	}

	/// <summary>
	/// Декрементирует индексы шагов
	/// </summary>
	private void StepDecrement()
	{

		if (IsEmptySteps() || IsOneStep()) return;          // если шагов нет или он один, то выходим и ничего не делаем ибо значения не будет

		if (!IsTwoStep())                                   // если шагов больше двух, то выставляем требуемое смещение
		{
			if (curStepIndex - 1 < 0)
			{
				curStepIndex = totalStageSteps.Count - 1;
			}
			else 
			{
				curStepIndex--;
			}
		}

		UpdateStepIndexes();
		UpdateCurStageIndex();
	}

	/// <summary>
	/// Устанавливает индекс текущего шага
	/// </summary>
	/// <param name="idx"></param>
	/// <exception cref="System.IndexOutOfRangeException"></exception>
	private void SetCurStepIndex(int idx)
	{
		if (idx >= 0 && idx < totalStageSteps.Count)
		{
			curStepIndex = idx;
			UpdateStepIndexes();
			UpdateCurStageIndex();
		}
		else 
		{
			throw new System.IndexOutOfRangeException("Index is out of range");
		}
	}

	// ----------------------------------- реализация компонентов интерфейса ISProvider ----------------------------------------------

	public void SerializerInit() 
	{
		try
		{
			serializer = UnigineApp.AppSystemLogic
				.GetStaticSerializer()
				.ExternanInit();
		}
		catch (System.Exception e)
		{	
			throw new System.Exception("SerializerInit()::ERROR::" + e.Message);
		}
	}

	public void UnsafeSaveStage(string stage, bool rewrite = false)
	{
		if (serializer != null) serializer.UnsafeSaveStage(stage, rewrite);
	}

	public void TrySaveStage(string stage, bool rewrite = false) 
	{
		if (serializer != null) 
		{
			SerializerBase.SERIAL_RESULT result = serializer.TrySaveStage(stage, rewrite);

			if (result != SerializerBase.SERIAL_RESULT.SAVE_STAGE_COMPLETE) {
				Log.Message("Stage {0} serialization is fail\n", stage);

				switch (result)
				{
					case SerializerBase.SERIAL_RESULT.SAVE_ERROR_SAVE_EXIST:
						Log.Message("Serialization is already exist\n", stage);
						break;
					case SerializerBase.SERIAL_RESULT.SAVE_ERROR_EXCEPTION:
						Log.Message("Serialization process an exception capture\n");
						break;
				}
			}
		}
	}

	public void UnsafeLoadStage(string stage)
	{
		if (serializer != null) serializer.UnsafeLoadStage(stage);
	}

	public void TryLoadStage(string stage)
	{
		if (serializer != null) 
		{
			SerializerBase.SERIAL_RESULT result = serializer.TryLoadStage(stage);

			if (result != SerializerBase.SERIAL_RESULT.LOAD_STAGE_COMPLETE) {
				Log.Message("Stage {0} deserialization is fail\n", stage);

				switch (result)
				{
					case SerializerBase.SERIAL_RESULT.LOAD_ERROR_NOT_EXIST:
						Log.Message("Deserialization is not exist\n", stage);
						break;
					case SerializerBase.SERIAL_RESULT.LOAD_ERROR_EXCEPTION:
						Log.Message("Deserialization process an exception capture\n");
						break;
				}
			}
		}
	}

	// ----------------------------------- блок базовой инициализации класса-обработчика ---------------------------------------------

	private enum OP_FLAG 
	{
		SET_STAGE_TO_WRITE, SET_STAGE_TO_READ, SET_NEXT_STEP_TO_WRITE, SET_NEXT_STEP_TO_READ, SET_PREV_STEP_TO_WRITE, SET_PREV_STEP_TO_READ, NONE
	}

	private void OperationProvider(OP_FLAG flag)
	{
		switch (flag)
		{
			case OP_FLAG.SET_STAGE_TO_WRITE:
				// чтобы избежать повторной записи/загрузки от случайного перехода устанавливается сверка индекса
				if (!CheckCurStageIndex()) 
				{
					SetCurStageIndex(stageListBox.CurrentItem);
					TrySaveStage(GetCurrentItemEngName(), true);
				}

				break;
			
			case OP_FLAG.SET_STAGE_TO_READ:
				
				if (!CheckCurStageIndex()) 
				{
					SetCurStageIndex(stageListBox.CurrentItem);
					TryLoadStage(GetCurrentItemEngName());
				}
				
				break;

			case OP_FLAG.SET_NEXT_STEP_TO_WRITE:
				StepIncrement();
				TrySaveStage(GetCurrentItemEngName(), true);
				break;
			
			case OP_FLAG.SET_NEXT_STEP_TO_READ:
				StepIncrement();
				TryLoadStage(GetCurrentItemEngName());
				break;

			case OP_FLAG.SET_PREV_STEP_TO_WRITE:
				StepDecrement();
				TrySaveStage(GetCurrentItemEngName(), true);
				break;
			
			case OP_FLAG.SET_PREV_STEP_TO_READ:
				StepDecrement();
				TryLoadStage(GetCurrentItemEngName());
				break;
		}
	}

	/// <summary>
	/// Выполняет инициализацию всех калбеков в зависимости от установленного режима
	/// </summary>
	private void InitGUICallBacks() 
	{
		switch (mode) 
		{
			case MODE.READ_ONLY:
				listBoxCallbackPtr = stageListBox.AddCallback(Gui.CALLBACK_INDEX.CHANGED, () => OperationProvider(OP_FLAG.SET_STAGE_TO_READ));
				nextStepCallbackPtr = nextStepButton.AddCallback(Gui.CALLBACK_INDEX.CLICKED, () => OperationProvider(OP_FLAG.SET_NEXT_STEP_TO_READ));
				prevStepCallbackPtr = prevStepButton.AddCallback(Gui.CALLBACK_INDEX.CLICKED, () => OperationProvider(OP_FLAG.SET_PREV_STEP_TO_READ));
				break;

			case MODE.REWRITE_AUTO:
			case MODE.REWRITE_MANUAL:
			case MODE.AUTO_REW_READ:
				listBoxCallbackPtr = stageListBox.AddCallback(Gui.CALLBACK_INDEX.CHANGED, () => OperationProvider(OP_FLAG.SET_STAGE_TO_WRITE));
				nextStepCallbackPtr = nextStepButton.AddCallback(Gui.CALLBACK_INDEX.CLICKED, () => OperationProvider(OP_FLAG.SET_NEXT_STEP_TO_WRITE));
				prevStepCallbackPtr = prevStepButton.AddCallback(Gui.CALLBACK_INDEX.CLICKED, () => OperationProvider(OP_FLAG.SET_PREV_STEP_TO_WRITE));
				break;
		}
	}

	/// <summary>
	/// Использует массив worldStages SerializeStageDesricpion.cs
	/// и наполняет базовый список. В режиме записи REWRITE_MODE, подразумевает предварительное 
	/// наполнение мира в World.Editor для последующей сериализации. В режиме загрузки READ_ONLY, 
	/// требует предваритальной загрузки из ранее созраненных данных в defaultWorldStage
	/// </summary>
	private void InitHandlerStagesList() 
	{
		if (node == null)
		{
			throw new System.Exception("Base node is null");
		}

		for (int i = 0; i != GetNumStages(); ++i) 
		{
			SerializeStageDesricpion desricpion = worldStages[i];
			if (desricpion != null) 
			{			
				StepInfo step;                                                  // заготовка под шаг
				string stageName = desricpion.GetStageName();                   // получает название стадии
				int stepsCount = desricpion.GetStageStepsCount();               // получает количество шагов
				
				if (stepsCount > 1)
				{
					bool isFirst = true;
					for (int j = 0; j != stepsCount; ++j)
					{
						if (isFirst)
						{
							// первый шаг в списке также записывается в сокращенный список стадий
							step = new StepInfo(false, HANDLER_STEPS_COUNT, mainStagesList.Count, desricpion.GetStageStep(j), stageName);
							mainStagesList.Add(step);
							isFirst = false;
						}
						else 
						{
							step = new StepInfo(true, HANDLER_STEPS_COUNT, mainStagesList.Count - 1, desricpion.GetStageStep(j), stageName);
						}

						totalStageSteps.Add(HANDLER_STEPS_COUNT, step);

						HANDLER_STEPS_COUNT++;                                                   // инкрементируем статический счетчик 
					}
				}
				else 
				{
					// иначе сохраняется только одна стадия как в общем, так и в сокращенном списке
					
					if (useSigleStageStepSuffix)
					{
						step = new StepInfo(false, HANDLER_STEPS_COUNT, mainStagesList.Count, singleStageStepSuffix, stageName);
					}
					else
					{
						step = new StepInfo(false, HANDLER_STEPS_COUNT, mainStagesList.Count, "", stageName);
					}

					totalStageSteps.Add(HANDLER_STEPS_COUNT, step);
					mainStagesList.Add(step);

					HANDLER_STEPS_COUNT++;                                                     // инкрементируем статический счетчик 
				}
			}
		}
	
		SetStageListStatus(LIST_STATUS.LOADED);                                                // выставляем статус, что данные загружены
	}

	/// <summary>
	/// Инициализирует данные по стадиям
	/// </summary>
	private void InitStagesList() 
	{
		switch (mode)
		{
			case MODE.READ_ONLY:
				/*
					использование режима предполагает, что все требуемые для работы данные, а именно:
					настройки размещения элементов GUI (список, кнопки), описания стадий с подшагами;
					будут загруженны из ранее сохраненного файла сериализации базового состояния
				*/
				TryLoadStage(defaultWorldStageName);             // сначала выполняем десериализацию
				InitHandlerStagesList();                         // загружаем стадии
				break;

			case MODE.REWRITE_AUTO:
			case MODE.REWRITE_MANUAL:
			case MODE.AUTO_REW_READ:    
				/*
					использование режима предполагает, что все требуемые для работы данные, а именно:
					настройки размещения элементов GUI (список, кнопки), описания стадий с подшагами;
					выполнены внутри World.Editor
				*/
				InitHandlerStagesList();                         // загружаем стадии
				TrySaveStage(defaultWorldStageName, true);       // выполняем сериализацию
				break;
		}

		/*
			после операций загрузки выставляется статус актуальности листа 
			чтобы в будущем его не сериализовать/загружать 
		*/
		SetStageListStatus(LIST_STATUS.ACTUAL);
	}

	/// <summary>
	/// Непосредственная инициализация кнопки в зависимости от типа и переданных параметров
	/// </summary>
	/// <param name="gui"></param>
	/// <param name="button"></param>
	/// <param name="settings"></param>
	private void InitGUIWidgetButton(ref Gui gui, ref WidgetButton button, SerializeGUIStepButtonSettings settings)
	{
        button = new WidgetButton(gui, settings.label);
        button.SetPosition(settings.xPos, settings.yPos);
        button.Width = settings.width;
        button.Height = settings.height;
        button.FontSize = settings.fontSize;
        button.ButtonColor = settings.color;
        button.FontColor = settings.fontColor;
        if (settings.spriteImage != "")
        {
            button.Texture = settings.spriteImage;
        }

        gui.AddChild(button, Gui.ALIGN_OVERLAP);
	}

	/// <summary>
	/// Инициализирует кнопки перехода по шагам проекта
	/// </summary>
	/// <param name="button"></param>
	private void InitGUIStepChangeButton(BUTTON button)
	{
		Gui gui = Gui.GetCurrent();
		switch (button)
		{
			case BUTTON.NEXT:
				InitGUIWidgetButton(ref gui, ref nextStepButton, nextStepButtonSettings);
				break;
			
			case BUTTON.PREV:
				InitGUIWidgetButton(ref gui, ref prevStepButton, prevStepButtonSettings);
				break;
		}
	}

	/// <summary>
	/// Создаёт и добавляет в GUI лист выбора стадий 
	/// </summary>
	private void InitGUIStageListBox()
	{
		Gui gui = Gui.GetCurrent();
		stageListBox = new WidgetComboBox(gui);
        stageListBox.SetPosition(stageListBoxSettings.xPos, stageListBoxSettings.yPos);
        stageListBox.FontSize = stageListBoxSettings.fontSize;
        stageListBox.Width = stageListBoxSettings.width;
        stageListBox.Height = stageListBoxSettings.height;
        stageListBox.BorderColor = stageListBoxSettings.color;
        stageListBox.ButtonColor = stageListBoxSettings.color;
        stageListBox.SelectionColor = stageListBoxSettings.color;
        stageListBox.ListBackgroundColor = stageListBoxSettings.color;
        stageListBox.MainBackgroundColor = stageListBoxSettings.color;

        foreach (StepInfo stage in mainStagesList)
        {
            stageListBox.AddItem(stage.stageName);
        }

		stageListBox.CurrentItem = defStartIndex;

        gui.AddChild(stageListBox, Gui.ALIGN_OVERLAP);
	}

	/// <summary>
	/// Запускает инициализацию всех модулей GUI
	/// </summary>
	private void InitGUIModules() 
	{
		InitGUIStageListBox();
		InitGUIStepChangeButton(BUTTON.PREV);
		InitGUIStepChangeButton(BUTTON.NEXT);
	}

	/// <summary>
	/// Инициализирует название для базовой стадии загружаемой по умолчанию
	/// </summary>
	private void InitDefStageName()
	{
		defaultWorldStageName = __DEFAUL_LOAD_WORLD_STAGE_PREFIX__ + serializer.GetCurrentWorldName();
	}

	/// <summary>
	/// Активирует отключение указанных нод, чтобы они не мешали работе
	/// </summary>
	private void InitDisabledElements()
	{
		List<Node> worldNodes = new List<Node>();
		World.GetNodes(worldNodes);

		for (int i = 0; i != worldNodes.Count; ++i) 
		{
			ISDisabler script = worldNodes[i].GetComponent<ISDisabler>();
			if (script != null) disabledElements.Add(script);
		}

		UpdateDeisabledElements();
	}

	/// <summary>
	/// Запускает процесс автоматической перезаписи сохранений
	/// </summary>
	private void InitAutoWriteProcess()
	{
		if (mode == MODE.REWRITE_AUTO || mode == MODE.AUTO_REW_READ)
		{
			ActionManager manager = FindComponentInWorld<ActionManager>();
			if (manager == null) throw new System.Exception("World has not Node whit ActionManager.cs");

			// специально делаем включая размер, чтобы счётчик вернулся к первому элементу через NextStep
			for (int i = 0; i != manager.GetActionsCount(); ++i)
			{
				manager.NextAction();
				OperationProvider(OP_FLAG.SET_NEXT_STEP_TO_WRITE);
			}

			if (mode == MODE.AUTO_REW_READ)
			{
				mode = MODE.READ_ONLY;
				UpdateDeisabledElements();
				ClearAllStagesToStepsListData();
				InitStagesList(); 
				ReInitGUICallbacks();
			}
		}
	}

	/// <summary>
	/// Общая инициализация обработчика
	/// </summary>
	/// <returns></returns>
	private SerializeHandler HandlerInitialization()
	{
		SerializerInit();
		InitDefStageName();
		InitDisabledElements();
		InitStagesList();
		InitGUIModules();
		InitGUICallBacks();
		InitAutoWriteProcess();
		return this;
	}

	/// <summary>
	/// Метод внешней инициализации класса
	/// </summary>
	/// <returns></returns>
	public SerializeHandler ExternanInit() { return HandlerInitialization(); }

	private void Init()
	{
		HandlerInitialization();
	}

	// ----------------------------------- блок обработки текстовых данных класса ----------------------------------------------------

	private static readonly string __DEFAUL_LOAD_WORLD_STAGE_PREFIX__ = "dlws_";

	private static readonly string __STAGE_TO_STEP_SEPARATOR__ = ". ";

    private static readonly Dictionary<char, char> __CHAR_TO_ENG_CHAR__ = new Dictionary<char, char>
	{
        { 'а', 'a'}, {'б', 'b'}, {'в', 'v'}, {'г', 'g'}, {'д', 'd'}, {'е', 'e'}, {'ё', 'e'}, /*{'ж', 'zh'}*/ {'з', 'z'}, {'и', 'i'}, {'й', 'y'}, 
		{'к', 'k'}, {'л', 'l'}, {'м', 'm'}, {'н', 'n'}, {'о', 'o'}, {'п', 'p'}, {'р', 'r'}, {'с', 's'}, {'т', 't'}, {'у', 'u'}, {'ф', 'f'}, 
		{'х', 'h'}, {'ц', 'c'}, /*{'ч', 'ch'} {'ш', 'sh'}, {'щ', 'sh\''}*/ {'ъ', '^'}, {'ы', 'i'}, {'ь', '\''}, {'э', 'e'}, /*{'ю', 'yu'}, {'я', 'ya'}*/

		{ 'А', 'A'}, {'Б', 'B'}, {'В', 'V'}, {'Г', 'G'}, {'Д', 'D'}, {'Е', 'E'}, {'Ё', 'E'}, /*{'Ж', 'ZH'}*/ {'З', 'Z'}, {'И', 'I'}, {'Й', 'Y'}, 
		{'К', 'K'}, {'Л', 'L'}, {'М', 'M'}, {'Н', 'N'}, {'О', 'O'}, {'П', 'P'}, {'Р', 'R'}, {'С', 'S'}, {'Т', 'T'}, {'У', 'U'}, {'Ф', 'F'}, 
		{'Х', 'H'}, {'Ц', 'C'}, /*{'Ч', 'CH'} {'Ш', 'SH'}, {'Щ', 'SH\''}*/ {'Ъ', '^'}, {'Ы', 'I'}, {'Ь', '\''}, {'Э', 'E'}, /*{'Ю', 'YU'}, {'Я', 'YA'}*/
		{ ' ', '_'}, {'.', '-'}
	};

	private static readonly Dictionary<char, string> __CHAR_TO_ENG_STRING__ = new Dictionary<char, string>
	{
        {'ж', "zh"}, {'ч', "ch"}, {'ш', "sh"}, {'щ', "sh\'"}, {'ю', "yu"}, {'я', "ya"},
		{'Ж', "ZH"}, {'Ч', "CH"}, {'Ш', "SH"}, {'Щ', "SH\'"}, {'Ю', "YU"}, {'Я', "YA"}
	};

	/// <summary>
	/// Использует описанные выше словари, чтобы перевести латинские буквы в русские
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
		return totalStageSteps[curStepIndex].GetTotalStepName();
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
	/// Обновляет состояние отключаемых элементов игрового мира
	/// </summary>
	private void UpdateDeisabledElements()
	{
		switch (mode)
		{
			case MODE.REWRITE_AUTO:
			case MODE.AUTO_REW_READ:
			case MODE.REWRITE_MANUAL:
				UpdateDeisabledElements(disabledElementsFlag = true);
				break;

			case MODE.READ_ONLY:
				UpdateDeisabledElements(disabledElementsFlag = false);
				break;
		}
	}

	/// <summary>
	/// Обновляет состояние отключаемых элементов игрового мира
	/// </summary>
	private void UpdateDeisabledElements(bool flag)
	{
		foreach (ISDisabler item in disabledElements)
		{
			switch (flag)
			{
				case true:
					item.EnableWorldElement();
					break;

				case false:
					item.DisableWorldElement();
					break;
			}
		}
	}

	/// <summary>
	/// Перезагружает калбеки элементов управления в графическом интерфейсе
	/// </summary>
	private void ReInitGUICallbacks()
	{
		stageListBox.RemoveCallback(Gui.CALLBACK_INDEX.CHANGED, listBoxCallbackPtr);
		nextStepButton.RemoveCallback(Gui.CALLBACK_INDEX.CLICKED, nextStepCallbackPtr);
		prevStepButton.RemoveCallback(Gui.CALLBACK_INDEX.CLICKED, prevStepCallbackPtr);

		InitGUICallBacks();
	}

	/// <summary>
	/// Удаляет все созданные компоненты графического интерфейса
	/// </summary>
	private void RemoveGUIComponent()
	{
		Gui.GetCurrent().RemoveChild(stageListBox);
		Gui.GetCurrent().RemoveChild(prevStepButton);
		Gui.GetCurrent().RemoveChild(nextStepButton);
	}

	private void Shutdown()
    {
		ClearAllStagesToStepsListData();
        RemoveGUIComponent();
    }
}