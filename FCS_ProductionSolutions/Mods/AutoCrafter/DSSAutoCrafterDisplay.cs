﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FCS_AlterraHub.Buildables;
using FCS_AlterraHub.Helpers;
using FCS_AlterraHub.Model;
using FCS_AlterraHub.Mono;
using FCS_AlterraHub.Mono.Controllers;
using FCS_ProductionSolutions.Buildable;
using FCS_ProductionSolutions.Configuration;
using FCSCommon.Abstract;
using FCSCommon.Helpers;
using FCSCommon.Utilities;
using SMLHelper.V2.Crafting;
using SMLHelper.V2.Handlers;
using UnityEngine;
using UnityEngine.UI;
using WorldHelpers = FCS_AlterraHub.Helpers.WorldHelpers;

namespace FCS_ProductionSolutions.Mods.AutoCrafter
{
    internal class DSSAutoCrafterDisplay : AIDisplay
    {
        private DSSAutoCrafterController _mono;
        private GameObject _canvas;
        private uGUI_Icon _currentCraftingItemIcon;
        private uGUI_Icon _targetItemIcon;
        private GridHelperV2 _ingredientsGrid;
        private Text _reqItemsList;
        public Action OnCancelBtnClick;
        private Button _standbyBTN;
        private readonly StringBuilder _sb = new StringBuilder();
        private List<IngredientItem> _ingredientItems = new List<IngredientItem>();
        private ManualPageController _manualPageController;
        private HomePageController _homePageController;
        private Text _status;
        private Text _total;
        private FCSMessageBox _messageBox;
        private StandByPageController _standbyPageController;

        internal GameObject ManualPage { get; private set; }
        internal GameObject HomePage { get; private set; }
        internal GameObject AutomaticPage { get; private set; }
        public Action<string> OnStatusUpdate { get; set; }

        internal void Setup(DSSAutoCrafterController mono)
        {
            _mono = mono;

            if (FindAllComponents())
            {
                _manualPageController.Refresh();
            }
        }
        
        public override void OnButtonClick(string btnName, object tag)
        {

        }

        public override bool FindAllComponents()
        {
            try
            {
                ManualPage = GameObjectHelpers.FindGameObject(gameObject, "ManualPage");
                HomePage = GameObjectHelpers.FindGameObject(gameObject, "Main");
                AutomaticPage = GameObjectHelpers.FindGameObject(gameObject, "AutomaticPage");
                StandByPage = GameObjectHelpers.FindGameObject(gameObject, "StandBySettingsPage");

                _canvas = gameObject.GetComponentInChildren<Canvas>().gameObject;

                var ingredientsGrid = GameObjectHelpers.FindGameObject(gameObject, "Frame").FindChild("Grid");

                #region CurrentCraftingItemIcon

                _currentCraftingItemIcon = GameObjectHelpers.FindGameObject(gameObject, "CurrentCraftingItemIcon")
                    .FindChild("Icon").AddComponent<uGUI_Icon>();
                _currentCraftingItemIcon.sprite = SpriteManager.defaultSprite;

                #endregion

                #region CurrentItemIcon

                _targetItemIcon = GameObjectHelpers.FindGameObject(gameObject, "ItemIcon")
                    .FindChild("Icon").AddComponent<uGUI_Icon>();
                _targetItemIcon.sprite = SpriteManager.defaultSprite;
                #endregion

                _reqItemsList = GameObjectHelpers.FindGameObject(gameObject, "RequirementsNotMetInformation").GetComponent<Text>();

                foreach (Transform child in ingredientsGrid.transform)
                {
                    _ingredientItems.Add(child.gameObject.EnsureComponent<IngredientItem>());
                }

                #region LoadIngredients


                _ingredientsGrid = ingredientsGrid.EnsureComponent<GridHelperV2>();
                _ingredientsGrid.OnLoadDisplay += OnLoadIngredientsGrid;
                _ingredientsGrid.Setup(9, gameObject, Color.gray, Color.white, null);

                #endregion

                var cancelBTN = GameObjectHelpers.FindGameObject(gameObject, "CancelBTN").GetComponent<Button>();
                var cancelFBTN = cancelBTN.gameObject.AddComponent<FCSButton>();
                cancelFBTN.ShowMouseClick = true;
                cancelFBTN.TextLineOne = "Cancel";
                cancelFBTN.TextLineTwo = "Cancels crafting operation.";
                cancelBTN.onClick.AddListener(() =>
                {
                    OnCancelBtnClick?.Invoke();
                    Clear();
                    BaseManager.GlobalNotifyByID("DTC", "RefreshCraftingGrid");
                });


                var backBTN = GameObjectHelpers.FindGameObject(gameObject, "BackBTN").GetComponent<Button>();
                var backFBTN = backBTN.gameObject.AddComponent<FCSButton>();
                backFBTN.ShowMouseClick = true;
                backFBTN.TextLineOne = "Back";
                backBTN.onClick.AddListener(() =>
                {
                    GoToPage(AutoCrafterPages.Home);
                });

                var standbyToggle = GameObjectHelpers.FindGameObject(AutomaticPage, "StandByBTN");
                _standbyBTN = standbyToggle.GetComponent<Button>();
                var standByBtn = standbyToggle.AddComponent<FCSButton>();
                standByBtn.ShowMouseClick = true;
                standByBtn.TextLineOne = "StandBy";
                standByBtn.TextLineTwo = "Puts this crafter in a mode that allows it to help other crafters to craft missing required items.";
                _standbyBTN.onClick.AddListener(()=>
                {
                    GoToPage(AutoCrafterPages.StandBy);
                });

                _status = GameObjectHelpers.FindGameObject(AutomaticPage, "Status").GetComponent<Text>();
                _total = GameObjectHelpers.FindGameObject(AutomaticPage, "Total").GetComponent<Text>();

                _standbyPageController = StandByPage.AddComponent<StandByPageController>();
                _standbyPageController.Initialize(this);

                _homePageController = HomePage.AddComponent<HomePageController>();
                _homePageController.Initialize(this);

                _manualPageController = ManualPage.AddComponent<ManualPageController>();
                _manualPageController.Initialize(this);

                _messageBox = GameObjectHelpers.FindGameObject(gameObject, "MessageBox").AddComponent<FCSMessageBox>();

                OnMessageReceived += s =>
                {
                    _messageBox.Show(s,FCSMessageButton.OK,null);
                };

                OnStatusUpdate += status =>
                {
                    _status.text = status.ToUpper();
                };

                OnTotalUpdate += amount =>
                {
                    _total.text = $"{amount.x}/{amount.y}";
                };
            }
            catch (Exception e)
            {
                QuickLogger.Error(e.StackTrace);
                QuickLogger.Error(e.Message);
                return false;
            }

            return true;
        }

        public GameObject StandByPage { get; set; }

        public Action<Vector2> OnTotalUpdate { get; set; }
        public Action OnLoadComplete { get; set; }
        public Action<string> OnMessageReceived { get; set; }

        internal void GoToPage(AutoCrafterPages page)
        {
            switch (page)
            {
                case AutoCrafterPages.Home:
                    _manualPageController.Hide();
                    _homePageController.Show();
                    _standbyPageController.Hide();
                    AutomaticPage.SetActive(false);
                    break;
                case AutoCrafterPages.Automatic:
                    _manualPageController.Hide();
                    _homePageController.Hide();
                    _standbyPageController.Hide();
                    AutomaticPage.SetActive(true);
                    break;
                case AutoCrafterPages.Manual:
                    _manualPageController.Show();
                    _homePageController.Hide();
                    _standbyPageController.Hide();
                    AutomaticPage.SetActive(false);
                    break;
                case AutoCrafterPages.StandBy:
                    _manualPageController.Hide();
                    _homePageController.Hide();
                    _standbyPageController.Show();
                    AutomaticPage.SetActive(false);
                    break;
            }
        }
        
        internal void SetStandByState(bool state,bool notify = false)
        {
            _standbyPageController.SetStandByState(state, notify);
        }

        internal void Clear()
        {
            ClearMissingItem();
            _targetItemIcon.sprite = SpriteManager.defaultSprite;
            ResetIngredientItems();
            _ingredientsGrid.DrawPage();
        }
        
        private void OnLoadIngredientsGrid(DisplayData data)
        {
            try
            {
                if (_mono == null) return;

                var grouped = TechDataHelpers.GetIngredients(_mono.CraftManager.GetCraftingOperation().TechType);

                if(grouped==null)return;
                
                if (data.EndPosition > grouped.Count)
                {
                    data.EndPosition = grouped.Count;
                }

                ResetIngredientItems();

                for (int i = data.StartPosition; i < data.EndPosition; i++)
                {
                    _ingredientItems[i].Set(grouped[i], _mono.Manager);
                }

            }
            catch (Exception e)
            {
                QuickLogger.Error("Error Caught");
                QuickLogger.Error($"Error Message: {e.Message}");
                QuickLogger.Error($"Error StackTrace: {e.StackTrace}");
            }
        }

        private void ResetIngredientItems()
        {
            for (int i = 0; i < 9; i++)
            {
                _ingredientItems[i].Reset();
            }
        }

        internal void LoadCraft(CraftingOperation operation)
        {
            _targetItemIcon.sprite = SpriteManager.Get(operation.TechType);
            _ingredientsGrid.DrawPage();

        }

        public override void TurnOffDisplay()
        {
            _canvas.SetActive(false);
        }

        public override void TurnOnDisplay()
        {
            _canvas.SetActive(true);
        }

        public void ClearMissingItem()
        {
            _sb.Clear();
            _reqItemsList.text = string.Empty;
        }

        public void AddMissingItem(string item, int amount)
        {
            _sb.Append($"{item} x{amount}");
            _sb.Append(Environment.NewLine);
            _reqItemsList.text = _sb.ToString();
        }

        public override void GoToPage(int index)
        {
            _manualPageController.GoToPage(index);
        }

        public DSSAutoCrafterController GetController()
        {
            return _mono;
        }
    }

    internal class HomePageController : MonoBehaviour
    {
        private Text _status;
        private Text _info;
        
        internal void Initialize(DSSAutoCrafterDisplay display)
        {
            

            var manualBTN = GameObjectHelpers.FindGameObject(gameObject, "ManualBTN").GetComponent<Button>();
            var manualBtn = manualBTN.gameObject.AddComponent<FCSButton>();
            manualBtn.ShowMouseClick = true;
            manualBtn.TextLineOne = "Manual Operation Page.";
            manualBTN.onClick.AddListener(() =>
            {
                if (display.GetController().CraftManager.IsRunning() || display.GetController().CurrentCrafterMode == AutoCrafterMode.StandBy)
                {
                    display.GetController().ShowMessage("Cannot enter manual mode:\nPlease cancel any operations this crafter may be working on or turn off StandBy Mode.");
                    return;
                }
                display.GoToPage(AutoCrafterPages.Manual);
                display.GetController().SetManual();
            });

            var automaticBTN = GameObjectHelpers.FindGameObject(gameObject, "AutomatedBTN").GetComponent<Button>();
            var automaticBtn = automaticBTN.gameObject.AddComponent<FCSButton>();
            automaticBtn.ShowMouseClick = true;
            automaticBtn.TextLineOne = "Operations Page.";
            automaticBTN.onClick.AddListener((() =>
            {
                display.GoToPage(AutoCrafterPages.Automatic);
                if (display.GetController().CurrentCrafterMode != AutoCrafterMode.StandBy)
                {
                    display.GetController().SetAutomatic();
                }
            }));

            _info = GameObjectHelpers.FindGameObject(gameObject, "Info").GetComponent<Text>();

            _status = GameObjectHelpers.FindGameObject(gameObject, "Status").GetComponent<Text>();

            display.OnStatusUpdate += status => { _status.text = $"Status - {status}"; };

            display.OnLoadComplete += () => { _info.text = $"Auto Crafter UnitID - {display.GetController().UnitID}"; };

        }
        
        internal void Show()
        {
            gameObject.SetActive(true);
        }

        internal void Hide()
        {
            gameObject.SetActive(false);
        }
    }

    internal class ManualPageController : MonoBehaviour
    {
        private List<CraftableItem> _craftableToggles = new List<CraftableItem>();
        private GridHelperV2 _itemGrid;
        private TechType _selectedCraftable;
        private string _currentSearchString;
        private PaginatorController _paginatorController;
        private DSSAutoCrafterDisplay _mono;
        private int _amount = 1;
        private Text _craftingAmount;
        private const float _maxInteraction = 0.9f;

        internal void Initialize(DSSAutoCrafterDisplay mono)
        {
            _mono = mono;
            
            foreach (Transform craftableItem in GameObjectHelpers.FindGameObject(mono.ManualPage, "Grid").transform)
            {
                var craftableToggle = craftableItem.gameObject.EnsureComponent<CraftableItem>();
                craftableToggle.Initialize(mono.GetController());
                craftableToggle.OnButtonClick += OnToggleClick;
                _craftableToggles.Add(craftableToggle);
            }

            _itemGrid = mono.gameObject.EnsureComponent<GridHelperV2>();
            _itemGrid.OnLoadDisplay += OnLoadItemsGrid;
            _itemGrid.Setup(21, mono.ManualPage, Color.gray, Color.white, null);

            _paginatorController = GameObjectHelpers.FindGameObject(mono.ManualPage, "Paginator").AddComponent<PaginatorController>();
            _paginatorController.Initialize(mono);

            #region Search
            var inputField = InterfaceHelpers.FindGameObject(gameObject, "InputField");
            var text = InterfaceHelpers.FindGameObject(inputField, "Placeholder")?.GetComponent<Text>();
            text.text = AlterraHub.SearchForItemsMessage();

            var searchField = inputField.AddComponent<SearchField>();
            searchField.OnSearchValueChanged += UpdateSearch;
            #endregion

            _craftingAmount = InterfaceHelpers.FindGameObject(gameObject, "CraftAmount").GetComponent<Text>();

            var craftBTN = GameObjectHelpers.FindGameObject(mono.ManualPage, "CraftBTN").GetComponent<Button>();
            var craftFBTN = craftBTN.gameObject.AddComponent<FCSButton>();
            craftFBTN.MaxInteractionRange = _maxInteraction;
            craftBTN.onClick.AddListener((() =>
            {
                _mono.GetController().CraftItem(new CraftingOperation(_selectedCraftable, _amount, false));
                mono.GoToPage(AutoCrafterPages.Automatic);
            }));

            var addBTN = GameObjectHelpers.FindGameObject(gameObject, "AddBTN").GetComponent<Button>();
            var addFBTN = addBTN.gameObject.AddComponent<FCSButton>();
            addFBTN.MaxInteractionRange = _maxInteraction;
            addFBTN.ShowMouseClick = true;
            addFBTN.TextLineOne = "Add";
            addFBTN.TextLineTwo = "Adds to the amount to craft. Hold (Shift) to increment by 10.";
            addBTN.onClick.AddListener((() =>
            {
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                {
                    _amount+=10;
                }
                else
                {
                    _amount++;
                }

                if (_amount >= 100)
                {
                    _amount = 100;
                }

                _craftingAmount.text = _amount.ToString();
            }));

            var subtractBTN = GameObjectHelpers.FindGameObject(gameObject, "MinusBTN").GetComponent<Button>();
            var subtractFBTN = subtractBTN.gameObject.AddComponent<FCSButton>();
            subtractFBTN.MaxInteractionRange = _maxInteraction;
            subtractFBTN.ShowMouseClick = true;
            subtractFBTN.TextLineOne = "Subtract";
            subtractFBTN.TextLineTwo = "Removes from the amount to craft. Hold (Shift) to decrement by 10.";

            subtractBTN.onClick.AddListener((() =>
            {
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                {
                    _amount -= 10;
                }
                else
                {
                    _amount--;
                }

                if (_amount <= 1)
                {
                    _amount = 1;
                }
                _craftingAmount.text = _amount.ToString();
            }));

            var backBTN = GameObjectHelpers.FindGameObject(gameObject, "BackBTN").GetComponent<Button>();
            var backFBTN = backBTN.gameObject.AddComponent<FCSButton>();
            backFBTN.MaxInteractionRange = _maxInteraction;
            backFBTN.ShowMouseClick = true;
            backFBTN.TextLineOne = "Back";
            backBTN.onClick.AddListener((() =>
            {
                Reset();
                _mono.GoToPage(AutoCrafterPages.Home);
            }));
        }

        private void Reset()
        {
            _amount = 1;
            _craftingAmount.text = _amount.ToString();
            _selectedCraftable = TechType.None;
            foreach (CraftableItem craftableItem in _craftableToggles)
            {
                craftableItem.SetState(false);
            }
        }

        private void OnToggleClick(TechType techType, bool state)
        {

            _selectedCraftable = state ? techType : TechType.None;
        }

        private void OnLoadItemsGrid(DisplayData data)
        {
            try
            {
                _mono.GetController().GetCraftables();
                var grouped = Mod.Craftables;

                if (!string.IsNullOrEmpty(_currentSearchString?.Trim()))
                {
                    grouped = grouped.Where(p => Language.main.Get(p).ToLower().Contains(_currentSearchString.Trim().ToLower())).ToList();
                }

                if (data.EndPosition > grouped.Count)
                {
                    data.EndPosition = grouped.Count;
                }

                for (int i = 0; i < data.MaxPerPage; i++)
                {
                    _craftableToggles[i].Reset();
                }

                int w = 0;

                for (int i = data.StartPosition; i < data.EndPosition; i++)
                {
                    _craftableToggles[w++].Set(grouped[i], _selectedCraftable == grouped[i]);
                }

                _itemGrid.UpdaterPaginator(grouped.Count);
                _paginatorController.ResetCount(_itemGrid.GetMaxPages());
            }
            catch (Exception e)
            {
                QuickLogger.Error("Error Caught");
                QuickLogger.Error($"Error Message: {e.Message}");
                QuickLogger.Error($"Error StackTrace: {e.StackTrace}");
            }
        }

        private void UpdateSearch(string newSearch)
        {
            _currentSearchString = newSearch;
            _itemGrid.DrawPage();
        }

        public void Refresh()
        {
            _itemGrid?.DrawPage();
        }

        public void GoToPage(int index)
        {
            _itemGrid.DrawPage(index);
        }

        internal void Show()
        {
            gameObject.SetActive(true);
            _mono.GetController().SetManual();
        }

        internal void Hide()
        {
            gameObject.SetActive(false);
            Reset();
        }
    }

    internal class StandByPageController : MonoBehaviour
    {
        private List<AutoCrafterItem> _autocrafterToggles = new List<AutoCrafterItem>();
        private GridHelperV2 _itemGrid;
        private string _currentSearchString;
        private PaginatorController _paginatorController;
        private DSSAutoCrafterDisplay _mono;
        private List<string> _selectedCrafters = new List<string>();
        private Toggle _toggle;
        private const float _maxInteraction = 0.9f;

        internal void Initialize(DSSAutoCrafterDisplay mono)
        {
            _mono = mono;

            foreach (Transform craftableItem in GameObjectHelpers.FindGameObject(gameObject, "Grid").transform)
            {
                var autoCrafterItem = craftableItem.gameObject.EnsureComponent<AutoCrafterItem>();
                autoCrafterItem.Initialize(this);
                autoCrafterItem.OnButtonClick += OnToggleClick;
                _autocrafterToggles.Add(autoCrafterItem);
            }

            _itemGrid = mono.gameObject.EnsureComponent<GridHelperV2>();
            _itemGrid.OnLoadDisplay += OnLoadItemsGrid;
            _itemGrid.Setup(21, mono.ManualPage, Color.gray, Color.white, null);

            _paginatorController = GameObjectHelpers.FindGameObject(gameObject, "Paginator").AddComponent<PaginatorController>();
            _paginatorController.Initialize(mono);

            #region Search
            var inputField = InterfaceHelpers.FindGameObject(gameObject, "InputField");
            var text = InterfaceHelpers.FindGameObject(inputField, "Placeholder")?.GetComponent<Text>();
            text.text = AlterraHub.SearchForItemsMessage();

            var searchField = inputField.AddComponent<SearchField>();
            searchField.OnSearchValueChanged += UpdateSearch;
            #endregion

            _toggle = GameObjectHelpers.FindGameObject(gameObject, "Toggle").GetComponent<Toggle>();
            _toggle.onValueChanged.AddListener((state =>
            {
                if (_mono.GetController().CraftManager.IsRunning())
                {
                    _toggle.SetIsOnWithoutNotify(false);
                    _mono.GetController().ShowMessage("Autocrafter is currently crafting. Please wait until complete before trying to enable standby");
                    return;
                }

                if (state)
                {
                    _mono.GetController().SetStandBy();
                }
                else
                {
                    _mono.GetController().SetAutomatic();
                }
            }));

            var confirmBTN = GameObjectHelpers.FindGameObject(gameObject, "ConfirmBTN").GetComponent<Button>();
            var confirmFBTN = confirmBTN.gameObject.AddComponent<FCSButton>();
            confirmFBTN.MaxInteractionRange = _maxInteraction;
            confirmBTN.onClick.AddListener((() =>
            {
                foreach (var selectedCrafter in _selectedCrafters)
                {
                    var crafter = (DSSAutoCrafterController)_mono.GetController().Manager.FindDeviceById(selectedCrafter);
                    crafter.AddConnectedCrafter(_mono.GetController().UnitID);
                }

                _mono.GoToPage(AutoCrafterPages.Automatic);
                Reset();
            }));

            var backBTN = GameObjectHelpers.FindGameObject(gameObject, "BackBTN").GetComponent<Button>();
            var backFBTN = backBTN.gameObject.AddComponent<FCSButton>();
            backFBTN.MaxInteractionRange = _maxInteraction;
            backFBTN.ShowMouseClick = true;
            backFBTN.TextLineOne = "Back";
            backBTN.onClick.AddListener((() =>
            {
                Reset();
                _mono.GoToPage(AutoCrafterPages.Automatic);
            }));
        }
        
        private void Reset()
        {
            _selectedCrafters.Clear();
        }

        private void OnToggleClick(DSSAutoCrafterController crafter, bool state)
        {
            if(crafter == null)return;

            if (state)
            {
                _selectedCrafters.Add(crafter.UnitID);
            }
            else
            {
                crafter.RemoveAutoCrafter(_mono.GetController().UnitID);
                _selectedCrafters.Remove(crafter.UnitID);
            }
        }

        private void OnLoadItemsGrid(DisplayData data)
        {
            try
            {
                var grouped = _mono.GetController().Manager.GetDevices(Mod.DSSAutoCrafterTabID).ToList();

                if (!string.IsNullOrEmpty(_currentSearchString?.Trim()))
                {
                    grouped = grouped.Where(p => Language.main.Get(p.UnitID).ToLower().Contains(_currentSearchString.Trim().ToLower())).ToList();
                }

                if (data.EndPosition > grouped.Count)
                {
                    data.EndPosition = grouped.Count;
                }

                for (int i = 0; i < data.MaxPerPage; i++)
                {
                    _autocrafterToggles[i].Reset();
                }

                int w = 0;

                for (int i = data.StartPosition; i < data.EndPosition; i++)
                {
                    var crafterController = (DSSAutoCrafterController)grouped[i];
                    if(crafterController.UnitID == _mono.GetController().UnitID) continue;
                    _autocrafterToggles[w++].Set(crafterController, _mono.GetController().CheckIfConnected(crafterController.UnitID));
                }

                _itemGrid.UpdaterPaginator(grouped.Count);
                _paginatorController.ResetCount(_itemGrid.GetMaxPages());
            }
            catch (Exception e)
            {
                QuickLogger.Error("Error Caught");
                QuickLogger.Error($"Error Message: {e.Message}");
                QuickLogger.Error($"Error StackTrace: {e.StackTrace}");
            }
        }

        private void UpdateSearch(string newSearch)
        {
            _currentSearchString = newSearch;
            _itemGrid.DrawPage();
        }

        public void Refresh()
        {
            _itemGrid?.DrawPage();
        }

        public void GoToPage(int index)
        {
            _itemGrid.DrawPage(index);
        }

        internal void Show()
        {
            Refresh();
            gameObject.SetActive(true);
            _mono.GetController().SetManual();
        }

        internal void Hide()
        {
            gameObject.SetActive(false);
            Reset();
        }

        public void SetStandByState(bool state, bool notify)
        {
            if (notify)
            {
                _toggle.isOn = state;
            }
            else
            {
                _toggle.SetIsOnWithoutNotify(state);
            }
        }
    }

    internal class AutoCrafterItem : MonoBehaviour
    {
        private Text _unitID;
        private DSSAutoCrafterController _autoCrafterController;
        private StandByPageController _mono;
        private Toggle _button;
        private const float _maxInteraction = 0.9f;

        public Action<DSSAutoCrafterController, bool> OnButtonClick { get; set; }

        internal void Initialize(StandByPageController mono)
        {
            _mono = mono;

            _unitID = gameObject.GetComponentInChildren<Text>();
            _button = gameObject.GetComponentInChildren<Toggle>();
            _button.onValueChanged.AddListener((value => { OnButtonClick?.Invoke(_autoCrafterController, value); }));
            var fcsbutton = _button.gameObject.AddComponent<FCSButton>();
            fcsbutton.MaxInteractionRange = _maxInteraction;
        }

        internal void Set(DSSAutoCrafterController crafter, bool state)
        {
            _autoCrafterController = crafter;
            _unitID.text = crafter.UnitID;
            _button.SetIsOnWithoutNotify(state);
            gameObject.SetActive(true);
        }

        internal bool GetState()
        {
            return _button.isOn;
        }

        internal void SetState(bool state)
        {
            _button.isOn = state;
        }

        public void Reset()
        {
            _unitID.text = string.Empty;
            _autoCrafterController = null;
            _button.SetIsOnWithoutNotify(false);
            gameObject.SetActive(false);
        }
    }
    
    internal enum AutoCrafterPages
    {
        Home = 0,
        Automatic = 1,
        Manual = 2,
        StandBy = 3
    }

    internal class CraftableItem : MonoBehaviour
    {
        private uGUI_Icon _icon;
        private Toggle _button;
        private TechType _techType;
        private FCSToolTip _toolTip;
        private StringBuilder _sb = new StringBuilder();
        private StringBuilder _sb2 = new StringBuilder();
        private Dictionary<TechType,int> _ingredients = new Dictionary<TechType, int>();
        private DSSAutoCrafterController _mono;
        private const float _maxInteraction = 0.9f;
        
        public Action<TechType,bool> OnButtonClick { get; set; }

        internal void Initialize(DSSAutoCrafterController mono)
        {
            _mono = mono;
            _icon = GameObjectHelpers.FindGameObject(gameObject, "Icon").AddComponent<uGUI_Icon>();
            _button = gameObject.GetComponentInChildren<Toggle>();
            _button.onValueChanged.AddListener((value => {OnButtonClick?.Invoke(_techType,value);}));
            var fcsbutton = _button.gameObject.AddComponent<FCSButton>();
            fcsbutton.MaxInteractionRange = _maxInteraction;

            _toolTip = _button.gameObject.AddComponent<FCSToolTip>();
            _toolTip.RequestPermission += () => WorldHelpers.CheckIfInRange(gameObject, Player.main.gameObject, _maxInteraction);
            _toolTip.ToolTipStringDelegate += ToolTipStringDelegate;
        }

        private string ToolTipStringDelegate()
        {
            _sb.Clear();
            _sb.AppendFormat("\n<size=20><color=#FFA500FF>{0}</color></size>", $"{Language.main.Get(_techType)}");
            _sb.AppendFormat("\n<size=20><color=#ffffffff>{0}:</color> {1}</size>", "Ingredients", $"{BuildIngredients()}");
            return _sb.ToString();
        }

        private string BuildIngredients()
        {
            _sb2.Clear();
            foreach (KeyValuePair<TechType, int> ingredient in _ingredients)
            {
                var addSpace = _ingredients.Count > 1 ? "," : string.Empty;
                var hasIngredient = _mono.Manager.GetItemCount(ingredient.Key) >= ingredient.Value;
                var color = hasIngredient ? "00ff00ff" : "ff0000ff";
                _sb2.AppendFormat("\n<size=20><color=#{0}>{1} x{2}{3}</color></size>", color,Language.main.Get(ingredient.Key),ingredient.Value,addSpace);
            }

            return _sb2.ToString();
        }

        internal void Set(TechType techType, bool state)
        {
            _techType = techType;
            foreach (Ingredient ingredient in CraftDataHandler.GetTechData(techType).Ingredients)
            {
                _ingredients.Add(ingredient.techType, ingredient.amount);
            }
            //_toolTip.TechType = techType;
            _icon.sprite = SpriteManager.Get(techType);
            _button.SetIsOnWithoutNotify(state);
            gameObject.SetActive(true);
        }

        internal bool GetState()
        {
            return _button.isOn;
        }

        internal void SetState(bool state)
        {
            _button.isOn = state;
        }

        public void Reset()
        {
            _ingredients.Clear();
            _button.SetIsOnWithoutNotify(false);
            _icon.sprite = SpriteManager.Get(TechType.None);
            gameObject.SetActive(false);
        }


    }
}