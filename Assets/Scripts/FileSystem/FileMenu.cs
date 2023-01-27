using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using JetBrains.Annotations;
using Tools;
using UnityEngine;
using UnityEngine.UI;

namespace FileSystem
{
    public class FileMenu : MonoBehaviour
    {
        [SerializeField] private Button houseClick;
        [SerializeField] private Button autoSaveClick;
        [SerializeField] private Button saveClick;
        [SerializeField] private Button newSaveClick;
        [SerializeField] private Button requestWorldClick;
        [SerializeField] private GameObject originalText;
        [SerializeField] private RectTransform menuTransform;
        [SerializeField] private GameObject inputMenu;
        [SerializeField] private Button inputMenuAccept;
        [SerializeField] private InputField inputMenuField;
        [SerializeField] private GameObject confirmationPopup;
        private Text confirmationText;


        public void CloseInputMenu()
        {
            inputMenuAccept.onClick.RemoveAllListeners();
            inputMenuField.onSubmit.RemoveAllListeners();
            inputMenuField.text = "";
            inputMenu.SetActive(false);
        }


        [CanBeNull] private Action lastAction;
        private void TriggerConfirmation(string text, Action action)
        {
            lastAction = action;
            confirmationText.text = text;
            confirmationPopup.SetActive(true);
        }

        public void OnConfirmConfirmation()
        {
            lastAction?.Invoke();
            CloseConfirmation();
        }

        public void CloseConfirmation()
        {
            lastAction = null;
            confirmationPopup.SetActive(false);
        }

        private List<GameObject> illustratedFiles = new List<GameObject>();
        private FileLocation currentLocation = FileLocation.SingleHouse;
        
        private void Start()
        {
            houseClick.onClick.AddListener(OnHouseClick);
            autoSaveClick.onClick.AddListener(OnAutoSaveClick);
            saveClick.onClick.AddListener(OnSaveClick);
            newSaveClick.onClick.AddListener(Save);
            requestWorldClick.onClick.AddListener(OnRequestWorld);
            originalText.SetActive(false);
            confirmationText = confirmationPopup.GetComponentInChildren<Text>();
            confirmationPopup.SetActive(false);
            FileSystem.SetupNecessaryFolders();

            var scrollMenu = menuTransform.parent.gameObject.GetComponent<RectTransform>();
            var height = Camera.main.pixelHeight;
            scrollMenu.anchoredPosition = new Vector3(0, -height);
            scrollMenu.sizeDelta = new Vector2(2000, height - 300);
        }
        
        private void Save()
        {
            if (currentLocation == FileLocation.SingleHouse)
            {
                ToolManager.GetManager().ChangeTool(new SaveHouseTool(HouseManager.Get(), ToolManager.GetManager(), OnHouseSelect));
                CloseMenu();
                return;
            }

            SaveWorld();
        }

        private void SaveWorld()
        {
            var data = HouseManager.Get().SerializeAllHouses();
            if (data == null) return;
            RequestName(OnWorldSave, data);
        }
        
        private void OnWorldSave(string name, object world)
        {
            FileSystem.WriteFile((string) world, name + ".txt", FileLocation.Save);
            ReplaceMenu(FileLocation.Save);
        }

        public static void PerformAutoSave()
        {
            var data = HouseManager.Get().SerializeAllHouses();
            if (data == null) return;
            var name = DateTime.Now.ToString("g", CultureInfo.GetCultureInfo("en-US")).Replace('/', '-');
            FileSystem.WriteFile(data, name + ".txt", FileLocation.Autosave);
        }

        private void OnHouseSelect(House house)
        {
            OpenMenu();
            Debug.Log("OnHouseSelect");
            Debug.Log(house.gameObject != null);
            RequestName(OnHouseSave, house);
        }

        private void OnHouseSave(string name, object house)
        {
            Debug.Log("OnHouseSave");
            Debug.Log(((House)house).gameObject != null);
            Debug.Log("About to serialize");
            var serializedHouse = ((House)house).Serialize();
            Debug.Log(serializedHouse);
            FileSystem.WriteFile(serializedHouse, name + ".txt", FileLocation.SingleHouse);
            ReplaceMenu(FileLocation.SingleHouse);
        }

        private void OnRequestWorld()
        {
            TriggerConfirmation("Are you sure you want to join the session and delete your current progress?", delegate
            {
                HouseManager.Get().RequestWorldLoad();
                CloseMenu();
            });
        }

        private void RequestName(Action<string, object> onNameFinished, object product)
        {
            inputMenu.SetActive(true);
            inputMenuField.Select();
            inputMenuAccept.onClick.AddListener(delegate() { onNameFinished(inputMenuField.text, product); CloseInputMenu(); });
            inputMenuField.onSubmit.AddListener(delegate(string objectName) { onNameFinished(objectName, product); CloseInputMenu(); });
        }

        private void OnEnable()
        {
            ReplaceMenu(FileLocation.SingleHouse);
            EraseAllColors();
            houseClick.GetComponentInParent<Text>().color = Color.cyan;
            currentLocation = FileLocation.SingleHouse;
            CloseInputMenu();
        }

        private void EraseAllColors()
        {
            houseClick.GetComponentInParent<Text>().color = Color.white;
            autoSaveClick.GetComponentInParent<Text>().color = Color.white;
            saveClick.GetComponentInParent<Text>().color = Color.white;
        }

        private void OnHouseClick()
        {
            ReplaceMenu(FileLocation.SingleHouse);
            EraseAllColors();
            houseClick.GetComponentInParent<Text>().color = Color.cyan;
            currentLocation = FileLocation.SingleHouse;
        }

        private void OnAutoSaveClick()
        {
            ReplaceMenu(FileLocation.Autosave);
            EraseAllColors();
            autoSaveClick.GetComponentInParent<Text>().color = Color.cyan;
            currentLocation = FileLocation.Autosave;
        }

        private void OnSaveClick()
        {
            ReplaceMenu(FileLocation.Save);
            EraseAllColors();
            saveClick.GetComponentInParent<Text>().color = Color.cyan;
            currentLocation = FileLocation.Save;
        }

        public void CloseMenu()
        {
            gameObject.transform.parent.gameObject.SetActive(false);
        }

        private void OpenMenu()
        {
            gameObject.transform.parent.gameObject.SetActive(true);
        }

        private void OnElementClick(FileInfo file, FileLocation location)
        {
            var content = FileSystem.ReadFile(file);
            var houseManager = HouseManager.Get();
            var toolManager = ToolManager.GetManager();
            switch (location)
            {
                case FileLocation.Autosave:
                case FileLocation.Save:
                    TriggerConfirmation(
                        "If you place a world, the current is deleted. Do you wish to delete your current world?",
                        delegate {
                            toolManager.ChangeTool(new LoadWorldTool(
                                houseManager, toolManager, content));
                            CloseMenu();
                        });
                    break;
                
                case FileLocation.SingleHouse:
                    toolManager.ChangeTool(new LoadHouseTool(houseManager, toolManager, content));
                    CloseMenu();
                    break;
            }
        }

        private void SetupMenu(FileLocation location)
        {
            var files = FileSystem.GetFileInformation(location);
            originalText.SetActive(true);
            originalText.GetComponent<RectTransform>().sizeDelta = new Vector2(Camera.main.pixelWidth-250,200);
            var number = files.Length - 1;
            foreach (var fileInfo in files)
            {
                var newTextObject = Instantiate(originalText, menuTransform);
                newTextObject.GetComponent<RectTransform>().localPosition = new Vector3(-900, -260 - number * 175);
                number--;

                newTextObject.GetComponent<Button>().onClick.AddListener(delegate {OnElementClick(fileInfo, location); });
                newTextObject.GetComponent<Text>().text = fileInfo.Name;
                illustratedFiles.Add(newTextObject);

                SetupSubButtons(newTextObject, fileInfo);
            }

            menuTransform.GetComponent<RectTransform>().sizeDelta = new Vector2(100, 300 + files.Length * 175);
            
            originalText.SetActive(false);
        }

        private void SetupSubButtons(GameObject entry, FileInfo file)
        {
            // First is delete button
            entry.transform.GetChild(0).GetComponent<Button>().onClick.AddListener(delegate
            {
                TriggerConfirmation("Are you sure you want to delete " + file.Name + "?", () =>
                {
                    file.Delete(); ReplaceMenu(currentLocation);
                });
            });

        }

        private void ReplaceMenu(FileLocation location)
        {
            EraseMenu();
            SetupMenu(location);
        }

        private void EraseMenu()
        {
            foreach (var textObject in illustratedFiles)
            {
                Destroy(textObject);
            }
            illustratedFiles.Clear();
        }
    }
}