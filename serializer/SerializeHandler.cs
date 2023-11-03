using System.Collections.Generic;
using Unigine;
using System.IO;
using System.Text.RegularExpressions;
using System;

/// <summary>
/// Основной класс обеспечивающий сериализацию данных. 
/// Может работать как самостоятельный модуль для одного игрового мира, так и как общий модуль определенный статически в AppSystemLogic.cs
/// </summary>
[Component(PropertyGuid = "5dfaf545f3a3e5e05c38dfbf6e1f6a12b40c8249")]
public class SerializeHandler : Component
{
	static readonly string STREAM_MODE_READ_ONLY = "r";
	static readonly string STREAM_MODE_READ_BINARY = "rb";
	static readonly string STREAM_MODE_WRITE = "w";
	static readonly string STREAM_MODE_WRITE_BINARY = "wb";
	static readonly string STREAM_MODE_APPEND = "a";
	static readonly string STREAM_MODE_APPEND_BINARY = "ab";

	static readonly string DATA_FOLDER = "../data/";
	static readonly string BINARY_FOLDER = "../bin/";

	enum ROOT {
		DATA, BINARY
	}

	static readonly string SHUTDOWN_SUFFIX = "_sd_backup";                // суффикс файла сохранения при выклчюении
	static readonly string DEFAULT_SUFFIX = "_ds_backup";                 // суффикс сохраненных базовых настроек мира
	static readonly string REGULAR_SUFFIX = "_rt_backup";                 // обычный суффикс файла сохранения по времени
	static readonly string PATH_LIST_SUFFIX = "_pl_backup";               // суффикс для сохранения листа с данными
	static readonly string STAGE_SUFFIX = "_st_backup";                   // суффикс для сохранения стадии

	static readonly string DATA_STAMP = @"[0-9]{4}-[0-9]{2}-[0-9]{2}-[0-9]{2}-[0-9]{2}-[0-9]{2}";     // штамп для времени
	static readonly string META_STAMP = @"meta";

	/// <summary>
	/// Флаг размещения и режима выборки элементов для сериализации. 
	/// Доступно как статическое размещение в общей логике игры, так и в каждом конкретном игровом мире.
	/// Запись инофрмации осуществляется или для всех имеющихся нод, или для отмеченных специальным модулем-свойством SerializeItem
	/// </summary>
	public enum MODE_PL
	{
		DIRECT_WORLD_ITEMS,                 // размещение только в одном мире, запись только специальных нод с модулем SerializeItem
		DIRECT_WORLD_NODES,                 // размещение только в одном мире, запись всех нод записанных в игровом мире
		STATIC_ALL_ITEMS,                   // статическое размещение в AppSystemLogic.cs, запись только специальных нод с модулем SerializeItem
		STATIC_ALL_NODES                    // статическое размещение в AppSystemLogic.cs, запись всех нод записанных в активном игровом мире
	}

	/// <summary>
	/// Флаг первичного запуска, выбор режима чтения записи
	/// </summary>
	public enum MODE_RW 
	{
		WRITE_NEW_DATA,                     // режим запуска "с чистого листа"
		READ_SHDN_DATA                      // запуск с чтением информации с прошлого выключения
	}

	/// <summary>
	/// Статус потока чтения/сохранения данных
	/// </summary>
	public enum STATUS {                    // статус выбранного файла сохранения
		OPEN, CLOSE, ERROR
	}

	/// <summary>
	/// Тип файла в потоке, зависит от его состояния
	/// </summary>
	public enum TYPE {                      // тип выбранного файла сохранения
		NONE, ERROR, SHDN_FILE, PATH_LIST, REGULAR, DEFAULT, STAGE
	}

	[ShowInEditor][Parameter(Tooltip = "Флаг размещения")]
	private MODE_PL placeSerialMode = MODE_PL.STATIC_ALL_ITEMS;

	[ShowInEditor][Parameter(Tooltip = "Флаг работы модуля сериализации")]
	private MODE_RW readWriteMode = MODE_RW.WRITE_NEW_DATA;

	[ShowInEditor][Parameter(Tooltip = "Корневая папка, в которую будет происходить сохранение")]
	private ROOT folderRoot = ROOT.DATA;

	[ShowInEditor][Parameter(Tooltip = "Путь к папке сохранений")]
	private string saveStateFolder = "serialization";

	[ShowInEditor][Parameter(Tooltip = "Включение использвоания в имени записи времени и даты")]
	private bool useTimeStamp = false;

	// -------------------------- блок внутренних полей класса ---------------------------------

	private Regex shutdownSaveRegExp;                             // регулярка для формирования и поиска сейвов по выключению
	private Regex defaultStateRegExp;                             // регулярка для формирования базового сейва
	private Regex regularSaveRegExp;                              // общая регулярка сохранения по времени
	private Regex pathListSaveRegExp;                             // регулярка для сохранения листа сейвов
	private Regex stageStateRegExp;                               // регулярка для сохранения стейжа
	private Regex fileExtMetaException;                           // регулярка исключения ".meta" (для файлов unigine)
	
	private string[] initFolderFiles;                             // массив под хранение путей к файлам в рабочей папке

	private string worldName;                                     // название текущего игрового мира
	private FileInfo fileInfo;                                    // заглушка для проверки наличия файла
	private string filePath;                                      // путь для работы файлового потока
	private Unigine.File fileSource = new Unigine.File();         // главный поток сохранения/загрузки
	private STATUS fileStatus = STATUS.CLOSE;                     // статус потока сохранения/загрузки
	private TYPE fileType = TYPE.NONE;                            // тип файла-потока сохранения/загрузки
	private string fileMode = "";                                 // режим открытия файла-потока
                        
	private List<Node> worldRootNodes = new List<Node>();         // список всех корневых нод загруженного мира
	private List<Node> worldSerialNodes = new List<Node>();       // список конкретных элементов содержащих SerializeItem
	private List<Widget> worldGuiWidgets = new List<Widget>();    // список всех виджетов гуя загруженного мира

	/// <summary>
	/// Структура хранящая в себе данные по названиям и путям к файлам сохраненных стадий для конкретного игрового мира
	/// </summary>
	struct WorldSavePathData
	{
		public WorldSavePathData() 
		{
			saveStagePaths = new Dictionary<string, string>();
			savePathStages = new Dictionary<string, string>();
		}

		public WorldSavePathData(WorldSavePathData other) 
		{
			saveStagePaths = other.saveStagePaths;
			savePathStages = other.savePathStages;
		}

		/// <summary>
		/// Сохраняет данные по стадии
		/// </summary>
		/// <param name="stage"></param>
		/// <param name="path"></param>
		/// <returns></returns>
		public WorldSavePathData AddData(string stage, string path)
		{
			if (saveStagePaths.ContainsKey(stage)) saveStagePaths.Remove(stage);
			if (savePathStages.ContainsKey(path)) savePathStages.Remove(path);

			saveStagePaths.Add(stage, path);
			savePathStages.Add(path, stage);
			
			return this;
		}

		/// <summary>
		/// Проверка наличия записи по указанной стадии
		/// </summary>
		/// <param name="stage"></param>
		/// <returns></returns>
		public bool CheckDataByStage(string stage) { return saveStagePaths.ContainsKey(stage); }
		
		/// <summary>
		/// Проверка наличия записи по заданному пути к файлу
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public bool CheckDataByPath(string path) { return savePathStages.ContainsKey(path); }

		/// <summary>
		/// Возвращает путь к сохраненному стейжу, если такой имеется, иначе возвращает пустую строку
		/// </summary>
		/// <param name="stage"></param>
		/// <returns></returns>
		public string GetPathByStage(string stage) 
		{ 
			string result = "";

			if (CheckDataByStage(stage))
			{
				saveStagePaths.TryGetValue(stage, out result);
			}

			return result;
		}

		/// <summary>
		/// Возвращает название стейжа по пути, если такой имеется, иначе возвращает пустую строку
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public string GetStageByPath(string path) 
		{ 
			string result = "";

			if (CheckDataByPath(path))
			{
				savePathStages.TryGetValue(path, out result);
			}

			return result;
		}

		/// <summary>
		/// Удаляет данные по имени стейжа, или возвращает false
		/// </summary>
		/// <param name="stage"></param>
		/// <returns></returns>
		public bool RemoveDataByStage(string stage) 
		{ 	
			bool result = false;

			if (CheckDataByStage(stage) && saveStagePaths.Remove(stage, out string path))
			{
				result = savePathStages.Remove(path);
			}

			return result;
		}

		/// <summary>
		/// Удаляет данные по пути к файлу или возвращает false
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public bool RemoveDataByPath(string path) 
		{
			bool result = false;

			if (CheckDataByPath(path) && savePathStages.Remove(path, out string stage))
			{
				result = saveStagePaths.Remove(stage);
			}

			return result;
		}

		// хеш, сохраняющий названия стадий и пути к файлам сохранения стадий
		private Dictionary<string, string> saveStagePaths; //= new Dictionary<string, string>();
		// обратный хеш, сохраняющий пути к файлам сохранения и название стадий 
		private Dictionary<string, string> savePathStages; // = new Dictionary<string, string>();
	}

	private Dictionary<string, WorldSavePathData> saveData = new Dictionary<string, WorldSavePathData>();

	
	// ------------------------------------------- блок методов инициализации класса ----------------------------------------------------

	/// <summary>
	/// Инициализирует файл с информацией о раннее сохраненных при выключении данных
	/// </summary>
	private void InitShutDownData() 
	{
		if (readWriteMode == MODE_RW.READ_SHDN_DATA)
		{
			// в папке будет находится только один полноценный файл состояния после предыдущего выключения
			foreach (string file in initFolderFiles)
			{
				if (!fileExtMetaException.IsMatch(file) && shutdownSaveRegExp.IsMatch(file))
				{
					// запрашиваем инициализацию файла по условию наличия суффикса и не имеющего разрешения
					OpenRecentFile(file, STREAM_MODE_READ_BINARY, TYPE.SHDN_FILE);
					LoadWorldData();                             // выполняем загрузку данных в игровой мир
				}
			}
		}
	}
	
	/// <summary>
	/// Инициализирует названия файлов из указанной папки
	/// </summary>
	private void InitSaveFiles()
	{
		Directory.CreateDirectory(GetSaveFolder());
		initFolderFiles = Directory.GetFiles(GetSaveFolder());
	}

	/// <summary>
	/// Инициализирует запись в saveData по текущему игровому миру
	/// </summary>
	private void InitSaveData() 
	{
		// создаём запись в хеше по данному игровому миру
		if (!saveData.TryGetValue(worldName, out WorldSavePathData worldData)) {
			saveData.Add(worldName, worldData = new WorldSavePathData());
		}

		// бежим по полученному списку файлов
		foreach (string file in initFolderFiles) {

			if (!fileExtMetaException.IsMatch(file) && stageStateRegExp.IsMatch(file))
			{
				string stage = System.IO.Path.GetFileNameWithoutExtension(file);
				stage = stage.Substring(0, stage.Length - STAGE_SUFFIX.Length);

				if (!worldData.CheckDataByStage(stage) && !worldData.CheckDataByPath(file)) worldData.AddData(stage, file);
			}
		}
	}

	/// <summary>
	/// Сортировка всех листов с нодами, чтобы сохранения и загрузки выполнялись корректно
	/// </summary>
	private void SortAllNodeLists() 
	{
		Comparison<Unigine.Node> nodeCompararor = (lhs, rhs) => lhs.ID.CompareTo(rhs.ID);
		Comparison<Unigine.Widget> widgetComparator = (lhs, rhs) => lhs.Order.CompareTo(rhs.Order);

		worldRootNodes.Sort(nodeCompararor);
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
			if (worldNodes[i].GetComponent<SerializeItem>() != null) worldSerialNodes.Add(worldNodes[i]);
		}
	}

	/// <summary> 
	/// Получает от игрового мира информацию по всем корневым нодам
	/// </summary>
	private void InitWorldData() 
	{
		
		switch (placeSerialMode)
		{
			case MODE_PL.STATIC_ALL_ITEMS:
			case MODE_PL.DIRECT_WORLD_ITEMS:
			
				InitSerialNodes(); 
				break;
			

			case MODE_PL.STATIC_ALL_NODES:
			case MODE_PL.DIRECT_WORLD_NODES:

				World.GetRootNodes(worldRootNodes);
				break;
		}

		InitGUIWidgets();
		SortAllNodeLists();
	}

	/// <summary>
	/// Получает название текущего игрового мира
	/// </summary>
	private void InitWorldName() 
	{
		worldName = System.IO.Path.GetFileNameWithoutExtension(World.Path);
	}

	/// <summary>
	/// Инициализирует регулярные выражения
	/// </summary>
	private void InitRegExps() 
	{
		shutdownSaveRegExp = new Regex((useTimeStamp ? DATA_STAMP : "") + SHUTDOWN_SUFFIX);
		defaultStateRegExp = new Regex((useTimeStamp ? DATA_STAMP : "")  + DEFAULT_SUFFIX);
		regularSaveRegExp = new Regex((useTimeStamp ? DATA_STAMP : "")  + REGULAR_SUFFIX);
		pathListSaveRegExp = new Regex((useTimeStamp ? DATA_STAMP : "")  + PATH_LIST_SUFFIX);
		stageStateRegExp = new Regex((useTimeStamp ? DATA_STAMP : "")  + STAGE_SUFFIX);
		fileExtMetaException = new Regex(META_STAMP);
	}

 
	/// <summary>
	/// Выполняет сериализацию базового сотояния всех нод игрового мира
	/// </summary>
	private void SaveDefaultWorldStage() 
	{
		UnsafeSaveStage("start_stage", true);
	}

	/// <summary>
	/// Метод внешней инициализации. Используется для внешнего вызова, если сериализатор находится в AppSystemLogic.cs
	/// </summary>
	public SerializeHandler ExternanInit()
	{
		Init(); return this;
	}

	private void Init()
	{
		InitWorldName();
		InitWorldData();
		InitRegExps();
		InitSaveFiles();
		InitSaveData();
		//SaveDefaultWorldStage();

		switch (readWriteMode) 
		{
			case MODE_RW.READ_SHDN_DATA:
				InitShutDownData();
				break;
		}
	}

	/// <summary>
	/// Проверяем доступность файла по указанному пути
	/// </summary>
	/// <param name="path"></param>
	/// <returns></returns>
	private bool CheckFilePath(string path)
	{
		fileInfo = new FileInfo(path);
		return fileInfo.Exists;
	}

	/// <summary>
	/// Возвращает наличие сохраненного файла по имени стадии
	/// </summary>
	/// <param name="stage"></param>
	/// <returns></returns>
	private bool CheckStageByName(string stage)
	{
		return saveData.TryGetValue(worldName, out WorldSavePathData data) && data.CheckDataByStage(stage);
	}

	/// <summary>
	/// Возвращает наличие сохраненного файла по пути к файлу стадии
	/// </summary>
	/// <param name="path"></param>
	/// <returns></returns>
	private bool CheckStageBypath(string path)
	{
		return saveData.TryGetValue(worldName, out WorldSavePathData data) && data.CheckDataByPath(path);
	}


	// ------------------------------------------- блок внутренних общих прикладных геттеров --------------------------------------------

	/// <summary>
	/// Возвращает выбранный файловый корень, куда будут происходить сохранения и создания доп папок
	/// </summary>
	/// <returns></returns>
	private string GetFolderRoot() 
	{
		switch (folderRoot)
		{
			case ROOT.DATA:
				return DATA_FOLDER;
			
			case ROOT.BINARY:
				return BINARY_FOLDER;
		}

		return "";
	}

	/// <summary>
	/// Возвращает путь к папке сохранения
	/// </summary>
	/// <returns></returns>
	private string GetSaveFolder() 
	{
		if (saveStateFolder.Length != 0)
		{
			// сохраняет в папку "{рут}/{папка сохранения}/{название игрового мира}/"
			return GetFolderRoot() + saveStateFolder + "/" + worldName + "/";
		}
		else 
		{
			return GetFolderRoot() + worldName + "/";
		}
	}

	/// <summary>
	/// Возвращает текущую дату и время
	/// </summary>
	/// <returns></returns>
	private string GetTimeStamp() 
	{
		string year = System.DateTime.Now.Year.ToString();
		string month = System.DateTime.Now.Month.ToString();
		if (month.Length == 1) month = "0" + month;
		string day = System.DateTime.Now.Day.ToString();
		if (day.Length == 1) day = "0" + day;

		string hour = System.DateTime.Now.Hour.ToString();
		if (hour.Length == 1) hour = "0" + hour;
		string min = System.DateTime.Now.Minute.ToString();
		if (min.Length == 1) min = "0" + min;
		string sec = System.DateTime.Now.Second.ToString();
		if (sec.Length == 1) sec = "0" + sec;

		return year + "-" + month + "-" + day + "-" + hour + "-" + min + "-" + sec;
	}

	// ------------------------------------------- блок методов для работы с файлами ----------------------------------------------------

	/// <summary>
	/// Создаёт новый файл согласно переданному типу и режиму открытия. Режим открытия по умолчанию "wb". Название стейжа = ""
	/// </summary>
	/// <param name="type"></param>
	/// <param name="mode"></param>
	private void WriteNewFile(TYPE type, string stage = "")
	{
		CloseOpenedFile();

		switch (type)
		{
			case TYPE.SHDN_FILE:
				ClearShutDownSaves();         // удаляет файлы с суфиксом сохранений при выключении
				filePath = GetSaveFolder() + (useTimeStamp ? GetTimeStamp() : "") + SHUTDOWN_SUFFIX;
				break;

			case TYPE.DEFAULT:
				filePath = GetSaveFolder() + (useTimeStamp ? GetTimeStamp() : "") + DEFAULT_SUFFIX;
				break;	

			case TYPE.PATH_LIST:
				filePath = GetSaveFolder() + (useTimeStamp ? GetTimeStamp() : "") + PATH_LIST_SUFFIX;
				break;	

			case TYPE.STAGE:
				filePath = GetSaveFolder() + stage + (useTimeStamp ? GetTimeStamp() : "") + STAGE_SUFFIX;
				break;
		}

		if (fileSource.Open(filePath, STREAM_MODE_WRITE_BINARY))
		{
			fileStatus = STATUS.OPEN;
			fileType = type; fileMode = STREAM_MODE_WRITE_BINARY;
		}
		else 
		{
			fileStatus = STATUS.ERROR;
			fileType = TYPE.ERROR; fileMode = "error"; filePath = "error";
			throw new System.Exception("SERIALIZE_HANDLER::WriteNewFile(TYPE, string)::Open file error");
		}
	}

	/// <summary>
	/// Загружает в поток файл по переданному пути, в соответствующем режиме  
	/// </summary>
	/// <param name="path"></param>
	/// <param name="mode"></param>
	/// <param name="type"></param>
	private void OpenRecentFile(string path, string mode, TYPE type)
	{
		if (CheckFilePath(path)) 
		{
			if (fileStatus == STATUS.OPEN && fileSource.IsOpened) CloseOpenedFile();

			if (fileSource.Open(path, mode)) 
			{
				fileStatus = STATUS.OPEN;
				fileType = type;
			}
			else 
			{
				fileStatus = STATUS.ERROR;
				fileType = TYPE.ERROR;
			}
		}
		else 
		{
			fileStatus = STATUS.ERROR;
			fileType = TYPE.ERROR;
		}
	}

	/// <summary>
	/// Закрывает открытый файл потока
	/// </summary>
	private void CloseOpenedFile()
	{
		if (fileStatus != STATUS.CLOSE) {
			fileStatus = STATUS.CLOSE;
			fileType = TYPE.NONE;
			fileSource.Close();
			filePath = "";
		}
	}

	// ------------------------------------------- блок методов для вызова сериализации -------------------------------------------------

	/// <summary>
	/// Коды возвращаемых операций работы по (де)сериализации данных
	/// </summary>
	public enum SERIAL_RESULT 
	{
		SAVE_STAGE_COMPLETE,
		SAVE_ERROR_SAVE_EXIST,
		SAVE_ERROR_EXCEPTION,
		LOAD_STAGE_COMPLETE,
		LOAD_ERROR_NOT_EXIST,
		LOAD_ERROR_EXCEPTION
	}

	/// <summary>
	/// Удаляет ранее созданные файлы сохранения при выключении
	/// </summary>
	private void ClearShutDownSaves() 
	{
		InitSaveFiles();     // пердварительно по новой инициализируем имеющиеся в папке файлы

		foreach (string file in initFolderFiles)
		{
			if (shutdownSaveRegExp.IsMatch(file))
			{
				FileInfo delete = new FileInfo(file);
				if (delete.Exists) delete.Delete();
			}
		}
	}

	/// <summary>
	/// Вызов сериализации в файл перед выключением игры
	/// </summary>
	public void SerilizeOnShutDown()
	{
		SerializeData(TYPE.SHDN_FILE);                              // вызывает сериализацию в соответствующем режиме
	}

	/// <summary>
	/// Сохраняет записи по стейжу в структуру
	/// </summary>
	/// <param name="stage"></param>
	/// <exception cref="System.Exception"></exception>
	private void AddWorldSavePathData(string stage) 
	{
		if (fileStatus == STATUS.OPEN && saveData.TryGetValue(worldName, out WorldSavePathData worldData))
		{
			worldData.AddData(stage, filePath);
		}
		else 
		{
			throw new System.Exception("SERIALIZE_HANDLER::AddWorldSavePathData(string)::World data is not exist");
		}
	}

	/// <summary>
	/// Базовый, комплексный метод сериализации данных в различных режимах
	/// </summary>
	/// <param name="type"></param>
	/// <param name="stage"></param>
	/// <param name="exc_throw"></param>
	private SERIAL_RESULT SerializeData(TYPE type, string stage = "", bool exc_throw = true)
	{
		try
		{
			WriteNewFile(type, stage);                         // создаёт и открывает для записи новый файл сохранений
			SerializeWorldData();                              // записывает данные о состоянии игрового мира
			AddWorldSavePathData(stage);                       // сохраняет стадию и путь к файлу созранения в хеш
			CloseOpenedFile();                                 // закрывает файл записанный файл

			return SERIAL_RESULT.SAVE_STAGE_COMPLETE;
		}
		catch (System.Exception e)
		{
			if (exc_throw) {
				// в зависимости от флага или пробрасывает исключение дальше или возвращает результат обработки
				throw new System.Exception("SERIALIZE_HANDLER::SerializeData(TYPE, string, exc_throw == true)::Exception::" + e.Message);
			}
			else {
				Log.Message("SERIALIZE_HANDLER::SerializeData(TYPE, string, exc_throw == false)::Exception::Captured::\n" + e.Message);
				return SERIAL_RESULT.SAVE_ERROR_EXCEPTION;
			}
		}
		
	}

	/// <summary>
	/// Сохраняет настройки объектов игрового мира, выбрасывает исключение если файл сохранения уже существет и не установлен флаг перезаписи
	/// </summary>
	/// <param name="stage"></param>
	/// <param name="rewrite"></param>
	/// <exception cref="System.Exception"></exception>
	public void UnsafeSaveStage(string stage, bool rewrite = false)
	{
		// проверяем наличие сохранений по текущему игровому миру
		if (!saveData.TryGetValue(worldName, out WorldSavePathData worldData)) saveData.Add(worldName, worldData = new WorldSavePathData());
		// проверяем наличие сохранений по заданной стадии и флагу перезаписи
		if (worldData.CheckDataByStage(stage) && !rewrite)
		{
			throw new System.Exception("SERIALIZE_HANDLER::SaveStage(string)::Serialization error::Stage name already exist");
		}
		else 
		{
			try
			{
				SerializeData(TYPE.STAGE, stage);
			}
			catch (System.Exception e)
			{	
				throw new System.Exception("SERIALIZE_HANDLER::SaveStage(string)::Serialization error::" + e.Message); 
			}
		}
	}

	/// <summary>
	/// Делает попытку сохранить настройки объектов игрового мира, НЕ выбрасывает исключений и не останавливает работу модуля. Возвращает результат работы операции
	/// </summary>
	/// <param name="stage"></param>
	/// <param name="rewrite"></param>
	/// <exception cref="System.Exception"></exception>
	public SERIAL_RESULT TrySaveStage(string stage, bool rewrite = false) 
	{
		// Если данных изначально не было, то создаётся запись о сохранении
		if (!saveData.TryGetValue(worldName, out WorldSavePathData worldData)) saveData.Add(worldName, worldData = new WorldSavePathData());
		// Если данные уже содержат упоминания о сохраненной стадии и не выбран флаг перезаписи, то система возвращает код ошибки
		if (worldData.CheckDataByStage(stage) && !rewrite) return SERIAL_RESULT.SAVE_ERROR_SAVE_EXIST;

		// Возвращаем значение результат сериализации с отловом возможных исключений
		return SerializeData(TYPE.STAGE, stage, false);
	}


	/// <summary>
	/// Базовый, комплексный метод десериализации данных в различных режимах
	/// </summary>
	/// <param name="stage"></param>
	/// <param name="exc_throw"></param>
	private SERIAL_RESULT DeSerializeData(string stage, bool exc_throw = true) 
	{	
		if (!saveData.TryGetValue(worldName, out WorldSavePathData worldData)|| !worldData.CheckDataByStage(stage)) 
		{
			if (exc_throw) {
				// если данных по стадии нет в базе то кидаем соответствующее исключение
				throw new System.Exception("SERIALIZE_HANDLER::DeSerializeData(string)::Load error::Stage not exist"); 
			}
			else {
				Log.Message("SERIALIZE_HANDLER::DeSerializeData(string)::Load error::Stage not exist\n");
				return SERIAL_RESULT.LOAD_ERROR_NOT_EXIST;
			}
		}

		OpenRecentFile(worldData.GetPathByStage(stage), STREAM_MODE_READ_BINARY, TYPE.STAGE);

		try
		{
			LoadWorldData();
			CloseOpenedFile();

			return SERIAL_RESULT.LOAD_STAGE_COMPLETE;
		}
		catch (System.Exception e)
		{
			if (exc_throw) {
				throw new System.Exception("SERIALIZE_HANDLER::DeSerializeData(string)::Loading error::" + e.Message);
			}
			else {
				Log.Message("SERIALIZE_HANDLER::DeSerializeData(string)::Exception::Captured::\n" + e.Message);
				return SERIAL_RESULT.LOAD_ERROR_EXCEPTION;
			}
		}
	}

	/// <summary>
	/// Загружает параметры объектов игрового мира их сохраненного ранее файла стейжа, в случае отсуствия выбрасывает исключение
	/// </summary>
	/// <param name="stage"></param>
	/// <exception cref="System.Exception"></exception>
	public SERIAL_RESULT UnsafeLoadStage(string stage)
	{
		return DeSerializeData(stage);
	}

	/// <summary>
	/// Безопасная попытка загрузить данные, вернет результат работы, в случае ошибок или исключений не пробросит их дальше
	/// </summary>
	/// <param name="stage"></param>
	/// <returns></returns>
	public SERIAL_RESULT TryLoadStage(string stage)
	{
		return DeSerializeData(stage, false);
	}


	// ------------------------------------------- блок методов сохранения информации в файл --------------------------------------------

	/// <summary>
	/// Записывает в файл информаию о предоставленной ноде, также выставляет предварительную шапку по переданному Label
	/// </summary>
	/// <param name="node"></param>
	/// <param name="label"></param>
	private void SerializeNode(Node node, string label) 
	{
		fileSource.WriteString(label + ". Begin\n");
		fileSource.WriteInt(node.ID);                       // записываем ID текущей ноды
		fileSource.WriteInt(node.Parent.ID);                // записываем ID родителя
		fileSource.WriteString(node.Name);                  // записываем название текущей ноды
		fileSource.WriteMat4(node.WorldTransform);          // матрицу трансформации
		fileSource.WriteQuat(node.GetWorldRotation());      // кватернион поворота
		fileSource.WriteVec3(node.WorldPosition);           // мировую позицию
		fileSource.WriteVec3(node.WorldScale);              // мировой масштаб

		if (node.IsObject) {
			fileSource.WriteBool(true);                     // флаг объекта
			Unigine.Object nodeObject = node as Unigine.Object;

			fileSource.WriteInt(nodeObject.NumSurfaces);    // записываем количество поверхностей
			
			for (int i = 0; i != nodeObject.NumSurfaces; i++) {
				Material mat = nodeObject.GetMaterial(i);
				fileSource.WriteVec4(mat.GetParameterFloat4("auxiliary_color"));
			}
		}
		else 
		{
			fileSource.WriteBool(false);                    // флаг объекта
		}

		fileSource.WriteString("\n" + label + ". End\n");
	}

	/// <summary>
	/// Базовая рекурсивная функция сериализации узлов. Проходит по всем наследникам каждого из переданных узлов включая переданный.
	/// Если у узла нет наследников, он считается дочерним, если есть, то родительским.
	/// </summary>
	/// <param name="node"></param>
	private void SerializeNodeData(Node node)
	{
		int num = node.NumChildren;                         // получаем количество наследников

		if (num == 0) 
		{
			SerializeNode(node, "ChildNode");               // если наследников нет, записываем дочерний узел и выходим из рекурсии
		}
		else 
		{
			if (node.Parent != null) SerializeNode(node, "ParentNode");   // записываем родительский узел, если у него самого есть родитель
			for (int i = 0; i != num; i++)
			{
				SerializeNodeData(node.GetChild(i));        // записываем всех наследников родительского узла
			}
		}
	}

	/// <summary>
	/// Сериализация корневого узла
	/// </summary>
	/// <param name="node"></param>
	private void SerializeRootNode(Node node) 
	{
		fileSource.WriteString("Root Node. Begin\n");
		fileSource.WriteInt(node.ID);                       // записываем ID текущей ноды
		fileSource.WriteString(node.Name);                  // записываем название текущей ноды
		fileSource.WriteMat4(node.WorldTransform);          // матрицу трансформации
		fileSource.WriteQuat(node.GetWorldRotation());      // кватернион поворота
		fileSource.WriteVec3(node.WorldPosition);           // мировую позицию
		fileSource.WriteVec3(node.WorldScale);              // мировой масштаб

		if (node.IsObject) {
			fileSource.WriteBool(true);                     // флаг объекта
			Unigine.Object nodeObject = node as Unigine.Object;

			fileSource.WriteInt(nodeObject.NumSurfaces);    // записываем количество поверхностей
			
			for (int i = 0; i != nodeObject.NumSurfaces; i++) {
				Material mat = nodeObject.GetMaterial(i);
				fileSource.WriteVec4(mat.GetParameterFloat4("auxiliary_color"));
			}
		}
		else 
		{
			fileSource.WriteBool(false);                    // флаг объекта
		}

		fileSource.WriteString("\nRoot Node. End\n");
	}

	/// <summary>
	/// Вызов сериализации игровых данных в заранее открытый файл
	/// </summary>
	private void SerializeWorldData()
	{
		if (fileStatus == STATUS.OPEN) 
		{
			switch (placeSerialMode)
			{
				case MODE_PL.STATIC_ALL_ITEMS:
				case MODE_PL.DIRECT_WORLD_ITEMS:
				
					foreach (Node node in worldSerialNodes)
					{
						node.GetComponent<SerializeItem>().SerializeNodeData(fileSource);
					}

					break;
				

				case MODE_PL.STATIC_ALL_NODES:
				case MODE_PL.DIRECT_WORLD_NODES:

					foreach (Node node in worldRootNodes) 
					{	
						SerializeRootNode(node);
						if (node.NumChildren > 0) SerializeNodeData(node);
					}

					break;
			}
			
		}
		else 
		{
			throw new System.ArgumentException("SERIALIZE_HANDLER::SerializeWorldData()::fileStatus != STATUS.OPEN");
		}
	}


	// ------------------------------------------- блок методов загрузки информации из файла --------------------------------------------

	/// <summary>
	/// Загружает информаию о предоставленной ноде из файла
	/// </summary>
	/// <param name="node"></param>
	private void LoadNode(Node node) 
	{
		try 
		{
			string nodeHeader = fileSource.ReadString();        // получаем хеддер "... Node. Begin\n"
			int nodeId = fileSource.ReadInt();                  // получаем ID текущей ноды         
			int nodeParentId = fileSource.ReadInt();            // получаем ID родителя текущей ноды        
			string nodeName = fileSource.ReadString();          // получаем название текущей ноды      
			mat4 nodeWorldTransform = fileSource.ReadMat4();    // матрицу трансформации     
			quat nodeWorldRotation = fileSource.ReadQuat();     // кватернион поворота
			vec3 nodeWorldPosition = fileSource.ReadVec3();     // мировую позицию
			vec3 nodeWorldScale = fileSource.ReadVec3();        // мировой масштаб

			if (fileSource.ReadBool()) {
				// если нода является объектом, то записываем её параметры материалов
				Unigine.Object nodeObject = node as Unigine.Object;
				// количество поверхностей должно быть сериализировано, потому берем из потока
				int nodeNumSurfaces = fileSource.ReadInt();
				for (int i = 0; i != nodeNumSurfaces; i++) {
					Material mat = nodeObject.GetMaterial(i);
					// сразу же из потока берем цвет засветки
					mat.SetParameterFloat4("auxiliary_color", fileSource.ReadVec4());
				}
			}

			string nodeEnd = fileSource.ReadString();           // хеддер "... Node. End\n"

			if (node.ID == nodeId && node.Parent.ID == nodeParentId)    // в случае совпадения по ID записываем параметры в корневую ноду
			{
				node.WorldTransform = nodeWorldTransform;
				node.SetWorldRotation(nodeWorldRotation);
				node.WorldPosition = nodeWorldPosition;
				node.WorldScale = nodeWorldScale;
			}
		}
		catch
		{
			// TODO сделать выбрасывание исключения
		}
	}

	/// <summary>
	/// Базовая рекурсивная функция получения информации по узлам. Проходит по всем наследникам каждого из переданных узлов включая переданный.
	/// Если у узла нет наследников, он считается дочерним, если есть, то родительским.
	/// </summary>
	/// <param name="node"></param>
	private void LoadNodeData(Node node)
	{
		int num = node.NumChildren;                         // получаем количество наследников

		if (num == 0) 
		{
			LoadNode(node);               // если наследников нет, получаем дочерний узел и выходим из рекурсии
		}
		else 
		{
			if (node.Parent != null) LoadNode(node);   // получаем родительский узел, если у него самого есть родитель
			for (int i = 0; i != num; i++)
			{
				LoadNodeData(node.GetChild(i));        // получаем всех наследников родительского узла
			}
		}
	}

	/// <summary>
	/// Загружает параметры корневого узла
	/// </summary>
	/// <param name="node"></param>
	private void LoadRootNode(Node node) 
	{
		try 
		{
			string rootNodeHeader = fileSource.ReadString();        // получаем хеддер "Root Node. Begin\n"
			int rootNodeId = fileSource.ReadInt();                  // получаем ID текущей ноды         
			string rootNodeName = fileSource.ReadString();          // получаем название текущей ноды      
			mat4 rootNodeWorldTransform = fileSource.ReadMat4();    // матрицу трансформации     
			quat rootNodeWorldRotation = fileSource.ReadQuat();     // кватернион поворота
			vec3 rootNodeWorldPosition = fileSource.ReadVec3();     // мировую позицию
			vec3 rootNodeWorldScale = fileSource.ReadVec3();        // мировой масштаб

			if (fileSource.ReadBool()) {
				// если нода является объектом, то записываем её параметры материалов
				Unigine.Object rootNodeObject = node as Unigine.Object;
				// количество поверхностей должно быть сериализировано, потому берем из потока
				int rootNodeNumSurfaces = fileSource.ReadInt();
				for (int i = 0; i != rootNodeNumSurfaces; i++) {
					Material mat = rootNodeObject.GetMaterial(i);
					// сразу же из потока берем цвет засветки
					mat.SetParameterFloat4("auxiliary_color", fileSource.ReadVec4());
				}
			}

			string rootNodeEnd = fileSource.ReadString();           // хеддер "Root Node. End\n"

			if (node.ID == rootNodeId)    // в случае совпадения по ID записываем параметры в корневую ноду
			{
				node.WorldTransform = rootNodeWorldTransform;
				node.SetWorldRotation(rootNodeWorldRotation);
				node.WorldPosition = rootNodeWorldPosition;
				node.WorldScale = rootNodeWorldScale;
			}
		}
		catch
		{
			// TODO сделать выбрасывание исключения
		}
		
	}

	/// <summary>
	/// Загружает параметры из cериализованных данных
	/// </summary>
	private void LoadWorldData()
	{
		if (fileStatus == STATUS.OPEN) 
		{

			switch (placeSerialMode)
			{
				case MODE_PL.STATIC_ALL_ITEMS:
				case MODE_PL.DIRECT_WORLD_ITEMS:
				
					foreach (Node node in worldSerialNodes)
					{
						node.GetComponent<SerializeItem>().RestoreData(fileSource);
					}

					break;
				

				case MODE_PL.STATIC_ALL_NODES:
				case MODE_PL.DIRECT_WORLD_NODES:

					foreach (Node node in worldRootNodes) 
					{	
						LoadRootNode(node);
						if (node.NumChildren > 0) LoadNodeData(node);
					}

					break;
			}
		}
	}
}