using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace TinyGiantStudio.Ranks
{
    /// <summary>
    /// TODO Update body insides buttons if changed by code. bind to current rank change
    /// TODO All changes already call update everything. Remove single updates from buttons.
    /// TODO Tie saved rank value change call back to update all the sets to reflect changes by code.
    /// </summary>

    [CustomEditor(typeof(RanksAnimationController))]
    public class RanksAnimationControllerEditor : Editor
    {
        #region Variable declarations

        #region Referenced in the Inspector

        [SerializeField] private VisualTreeAsset visualTreeAsset;
        [SerializeField] private VisualTreeAsset rankSetAsset;

        #endregion Referenced in the Inspector

        private readonly string url_doc_howToModifyMaterialShortcuts = "https://ferdowsur.gitbook.io/3d-ranks/the-inspector/material-shortcuts";
        private readonly string url_doc_howToCreateBaseAnimations = "https://ferdowsur.gitbook.io/3d-ranks/base-animations#how-to-create-spawn-animations";
        private readonly string url_doc_howToCreateIconAnimations = "https://ferdowsur.gitbook.io/3d-ranks/rank/icon/create-new-icon";
        private readonly string url_assetStoreLink = "https://assetstore.unity.com/publishers/45848?aid=1011ljxWe";

        #region Main

        private VisualElement root;
        private RanksAnimationController animationController;

        #endregion Main

        // These require being set manually

        #region Settings

        private int iconsAmount = 10;

        #endregion Settings

        #region Tabs

        private Button customizeTabButton;

        /// <summary>
        /// This contains everything in the primary tab : Ranks
        /// </summary>
        private GroupBox ranksTabContent;

        /// <summary>
        /// This contains everything in the primary tab : Customize
        /// </summary>
        private GroupBox customTabContent;

        /// <summary>
        /// This contains everything in the primary tab : Settings
        /// </summary>
        private GroupBox settingsTabContent;

        private GroupBox referencesTabContent;

        private GroupBox debugTabContent;

        #endregion Tabs

        #region Customize Tab

        //For saving
        private TextField rankNameInput;

        private Button saveButton;

        private GroupBox iconsPrimaryMaterialsFoldout;
        private ObjectField iconsPrimaryMaterialsField;
        private GroupBox iconsSecondaryMaterialsFoldout;
        private ObjectField iconsSecondaryMaterialsField;

        private EnumField borderEntryAnimationType;
        private EnumField borderExitAnimationType;
        private GroupBox borderMaterialsFoldout;
        private ObjectField borderMaterialObjectField;

        private Foldout insidesFoldout;
        private EnumField insideEntryAnimationType;
        private EnumField insideExitAnimationType;
        private GroupBox insideMaterialsFoldout;
        private ObjectField insideMaterialObjectField;

        private GroupBox wingsMaterialFoldout;
        private ObjectField wingsMaterialObjectField;

        private List<Button> iconButtons = new();
        private List<Button> iconPrimaryMaterialButtons = new();
        private List<Button> iconSecondaryMaterialButtons = new();
        private List<Button> borderButtons = new();
        private List<Button> borderMaterialsButtons = new();
        private List<Button> insideButtons = new();
        private List<Button> insideMaterialsButtons = new();
        private List<Button> wingsButtons = new();
        private List<Button> wingsMaterialsButtons = new();
        //private List<Button> addonsButtons = new();

        #endregion Customize Tab

        private CustomFoldoutSetup customFoldoutSetup;

        #endregion Variable declarations

        #region Unity Stuff

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= () => UpdateEverything();
        }

        public override VisualElement CreateInspectorGUI()
        {
            animationController = target as RanksAnimationController;
            root = new VisualElement();

            if (visualTreeAsset == null)
                visualTreeAsset = AssetDatabase.LoadAssetAtPath("Assets/Tiny Giant Studio/3D Ranks/Scripts/RanksAnimationController.uxml", typeof(VisualTreeAsset)) as VisualTreeAsset;

            if (visualTreeAsset == null)
                return root;

            visualTreeAsset.CloneTree(root);

            if (!Application.isPlaying)
                root.Q<GroupBox>("PageContainer").Insert(0, new HelpBox("Enter Play Mode to test out the animations.", HelpBoxMessageType.Info));

            customFoldoutSetup = new CustomFoldoutSetup();

            IntegerField IconAnimationCounterField = root.Q<IntegerField>("IconAnimationCounterField");
            IconAnimationCounterField.schedule.Execute(() =>
            {
                iconsAmount = IconAnimationCounterField.value;
                UpdateIconChoices();

                IconAnimationCounterField.RegisterValueChangedCallback(evt =>
                {
                    iconsAmount = evt.newValue;
                    UpdateIconChoices();
                    UpdateEverything();
                });
            }).ExecuteLater(0);

            SetupTabs();

            SetupRanksTab();

            SetupCustomizeTab();

            SetupSettingsTab();

            SetupReferencesTab();

            SetupDocs();

            UpdateEverything();

            Undo.undoRedoPerformed += () => UpdateEverything();

            var currentRankField = root.Q<PropertyField>("CurrentRankField");
            currentRankField.schedule.Execute(() =>
            {
                currentRankField.RegisterValueChangeCallback(evt =>
                {
                    UpdateEverything();
                });
            }).ExecuteLater(1000);

            //ValidateData();// todo: disable

            return root;
        }

        #endregion Unity Stuff

        #region Update data

        private void UpdateEverything()
        {
            CreateRanksList();
            UpdateCustomTab();
        }

        #endregion Update data

        #region Tabs

        private void SetupTabs()
        {
            var tabsButtonContainer = root.Q<GroupBox>("TabsGroupbox");

            GroupBox tabContents = root.Q<GroupBox>("TabContents");
            ranksTabContent = root.Q<GroupBox>("RanksTabContent");
            customTabContent = root.Q<GroupBox>("CustomContent");
            settingsTabContent = tabContents.Q<GroupBox>("SettingsContent");
            referencesTabContent = tabContents.Q<GroupBox>("ReferenceTabContent");
            debugTabContent = tabContents.Q<GroupBox>("DebugContent");

            var setsTabButton = tabsButtonContainer.Q<Button>("SetsTabButton");
            customizeTabButton = tabsButtonContainer.Q<Button>("CustomTabButton");
            var settingTabButton = tabsButtonContainer.Q<Button>("SettingTabButton");
            var referencesTabButton = tabsButtonContainer.Q<Button>("ReferencesTabButton");
            var debugTabButton = tabsButtonContainer.Q<Button>("DebugTabButton");

            setsTabButton.clicked += () => { OpenStatesTab(); };
            customizeTabButton.clicked += () => { OpenCustomTab(); };
            settingTabButton.clicked += () => { OpenSettingsTab(); };
            referencesTabButton.clicked += () => { OpenReferencesTab(); };
            debugTabButton.clicked += () => { OpenDebugTab(); };

            Toggle showReferencesTab = settingsTabContent.Q<Toggle>("ShowReferencesTab");
            if (showReferencesTab.value)
                referencesTabButton.style.display = DisplayStyle.Flex;
            else
                referencesTabButton.style.display = DisplayStyle.None;
            showReferencesTab.RegisterValueChangedCallback(value =>
            {
                if (value.newValue)
                    referencesTabButton.style.display = DisplayStyle.Flex;
                else
                    referencesTabButton.style.display = DisplayStyle.None;
            });

            Toggle showDebugTab = settingsTabContent.Q<Toggle>("ShowDebugTab");
            if (showDebugTab.value)
                debugTabButton.style.display = DisplayStyle.Flex;
            else
                debugTabButton.style.display = DisplayStyle.None;
            showDebugTab.RegisterValueChangedCallback(value =>
            {
                if (value.newValue)
                    debugTabButton.style.display = DisplayStyle.Flex;
                else
                    debugTabButton.style.display = DisplayStyle.None;
            });

            //The default tab. temporary custom
            OpenStatesTab();

            void OpenStatesTab()
            {
                setsTabButton.AddToClassList("tab-selected");
                customizeTabButton.RemoveFromClassList("tab-selected");
                settingTabButton.RemoveFromClassList("tab-selected");
                referencesTabButton.RemoveFromClassList("tab-selected");
                debugTabButton.RemoveFromClassList("tab-selected");

                ranksTabContent.style.display = DisplayStyle.Flex;
                customTabContent.style.display = DisplayStyle.None;
                settingsTabContent.style.display = DisplayStyle.None;
                referencesTabContent.style.display = DisplayStyle.None;
                debugTabContent.style.display = DisplayStyle.None;
            }

            void OpenCustomTab()
            {
                UpdateEverything();

                setsTabButton.RemoveFromClassList("tab-selected");
                customizeTabButton.AddToClassList("tab-selected");
                settingTabButton.RemoveFromClassList("tab-selected");
                referencesTabButton.RemoveFromClassList("tab-selected");
                debugTabButton.RemoveFromClassList("tab-selected");

                ranksTabContent.style.display = DisplayStyle.None;
                customTabContent.style.display = DisplayStyle.Flex;
                settingsTabContent.style.display = DisplayStyle.None;
                referencesTabContent.style.display = DisplayStyle.None;
                debugTabContent.style.display = DisplayStyle.None;
            }

            void OpenSettingsTab()
            {
                setsTabButton.RemoveFromClassList("tab-selected");
                customizeTabButton.RemoveFromClassList("tab-selected");
                settingTabButton.AddToClassList("tab-selected");
                referencesTabButton.RemoveFromClassList("tab-selected");
                debugTabButton.RemoveFromClassList("tab-selected");

                ranksTabContent.style.display = DisplayStyle.None;
                customTabContent.style.display = DisplayStyle.None;
                settingsTabContent.style.display = DisplayStyle.Flex;
                referencesTabContent.style.display = DisplayStyle.None;
                debugTabContent.style.display = DisplayStyle.None;
            }

            void OpenReferencesTab()
            {
                setsTabButton.RemoveFromClassList("tab-selected");
                customizeTabButton.RemoveFromClassList("tab-selected");
                settingTabButton.RemoveFromClassList("tab-selected");
                referencesTabButton.AddToClassList("tab-selected");
                debugTabButton.RemoveFromClassList("tab-selected");

                ranksTabContent.style.display = DisplayStyle.None;
                customTabContent.style.display = DisplayStyle.None;
                settingsTabContent.style.display = DisplayStyle.None;
                referencesTabContent.style.display = DisplayStyle.Flex;
                debugTabContent.style.display = DisplayStyle.None;
            }

            void OpenDebugTab()
            {
                setsTabButton.RemoveFromClassList("tab-selected");
                customizeTabButton.RemoveFromClassList("tab-selected");
                settingTabButton.RemoveFromClassList("tab-selected");
                referencesTabButton.RemoveFromClassList("tab-selected");
                debugTabButton.AddToClassList("tab-selected");

                ranksTabContent.style.display = DisplayStyle.None;
                customTabContent.style.display = DisplayStyle.None;
                settingsTabContent.style.display = DisplayStyle.None;
                referencesTabContent.style.display = DisplayStyle.None;
                debugTabContent.style.display = DisplayStyle.Flex;
            }
        }

        #endregion Tabs

        #region Ranks Tab

        private void SetupRanksTab()
        {
            SetupSpawnAnimationButtons();

            //CreateRanksList(); //Called at start by UpdateEverything.

            ranksTabContent.schedule.Execute(() =>
            {
                ranksTabContent.Q<Button>("NewRankButton").clicked += () =>
                {
                    Undo.RecordObject(animationController, "New rank added");
                    Rank rank = new Rank(animationController.currentRank);
                    rank.name = Random.Range(0, 99999).ToString();
                    animationController.ranks.Add(rank);
                    EditorUtility.SetDirty(animationController);

                    UpdateCurrentRankNameRelatedLabels();
                    CreateRanksList();
                    UpdateEverything();
                };
            }).ExecuteLater(3000);
        }

        private VisualElement container;

        /// <summary>
        /// Creates UI for the states list saved.
        /// </summary>
        private void CreateRanksList()
        {
            GroupBox ranksListContainer = ranksTabContent.Q<GroupBox>("RanksListContainer");
            ranksListContainer.Clear();

            container = new VisualElement();
            ranksListContainer.Add(container);

            if (animationController.ranks.Count == 0)
                ranksListContainer.Add(new HelpBox("No ranks saved. Please use the customize tab to create ranks.", HelpBoxMessageType.Warning));

            for (int i = 0; i < animationController.ranks.Count; i++)
            {
                Rank rank = animationController.ranks[i];
                int index = i;
                if (index < 14)
                {
                    CreateRankUIItem(ranksListContainer, customFoldoutSetup, index, rank);
                }
                else
                {
                    container.schedule.Execute(() =>
                    {
                        CreateRankUIItem(ranksListContainer, customFoldoutSetup, index, rank);
                    }).ExecuteLater((index * 2) + 100);
                }
            }
        }

        private void CreateRankUIItem(GroupBox statesContainer, CustomFoldoutSetup customFoldoutSetup, int i, Rank rank)
        {
            //Create the UI and add it to container
            VisualElement rankVisualElement = new();
            rankSetAsset.CloneTree(rankVisualElement);
            statesContainer.Add(rankVisualElement);

            var rankUIRoot = rankVisualElement.Q<GroupBox>("RankMainGroupBox");

            //Make the foldouts work
            customFoldoutSetup.SetupFoldout(rankUIRoot);
            customFoldoutSetup.SetupFoldout(rankUIRoot.Q<GroupBox>("IconsFoldout"));
            customFoldoutSetup.SetupFoldout(rankUIRoot.Q<GroupBox>("BorderFoldout"));
            customFoldoutSetup.SetupFoldout(rankUIRoot.Q<GroupBox>("InsideFoldout"));
            customFoldoutSetup.SetupFoldout(rankUIRoot.Q<GroupBox>("WingsFoldout"));

            //Setup Index
            rankUIRoot.Q<Toggle>("FoldoutToggle").text = i + ".";

            //Setup Name
            var nameField = rankUIRoot.Q<TextField>("NameField");
            nameField.value = rank.name;
            nameField.RegisterValueChangedCallback(e =>
            {
                rank.name = nameField.value;
                UpdateCurrentRankNameRelatedLabels();
            });

            //Setup Apply Button
            rankUIRoot.Q<Button>("ApplyButton").clicked += () =>
            {
                animationController.ApplyRank(rank);
                rankNameInput.SetValueWithoutNotify(animationController.currentRank.name);
                //UpdateEverything();
                UpdateCustomTab();
                UpdateCurrentRankNameRelatedLabels();
            };

            //Setup remove Button
            rankUIRoot.Q<Button>("RemoveButton").clicked += () =>
            {
                if (EditorUtility.DisplayDialog("Remove State?", "Are you sure you want to remove the state?", "Remove State", "Cancel"))
                {
                    Undo.RecordObject(animationController, "Remove saved state : " + rank.name);
                    animationController.ranks.Remove(rank);
                    EditorUtility.SetDirty(animationController);
                    //stateNameInput.SetValueWithoutNotify(animationController.currentState.stateName);
                    UpdateEverything();
                    UpdateCurrentRankNameRelatedLabels();
                }
            };

            var moveUpButton = rankUIRoot.Q<Button>("MoveUpButton");
            var moveDownButton = rankUIRoot.Q<Button>("MoveDownButton");

            if (i == 0)
            {
                moveUpButton.style.display = DisplayStyle.None;
            }
            else
            {
                int targetUpIndex = i - 1;
                moveUpButton.clicked += () =>
                {
                    Undo.RecordObject(animationController, animationController.gameObject.name + " states rearranged.");
                    animationController.ranks.Remove(rank);
                    animationController.ranks.Insert(targetUpIndex, rank);
                    EditorUtility.SetDirty(animationController);
                    UpdateEverything();
                };
            }

            if (i == animationController.ranks.Count - 1)
                moveDownButton.style.display = DisplayStyle.None;
            else
            {
                int targetAboveIndex = i + 1;
                moveDownButton.clicked += () =>
                {
                    Undo.RecordObject(animationController, animationController.gameObject.name + " rank updated.");
                    animationController.ranks.Remove(rank);
                    animationController.ranks.Insert(targetAboveIndex, rank);
                    EditorUtility.SetDirty(animationController);
                    UpdateEverything();
                };
            }

            rankUIRoot.Q<Button>("DuplicateButton").clicked += () =>
            {
                Undo.RecordObject(animationController, "Rank duplicated");
                Rank duplicateRank = new Rank(rank);
                duplicateRank.name += " (Duplicate)";
                animationController.ranks.Insert(i + 1, duplicateRank);
                EditorUtility.SetDirty(animationController);

                UpdateCurrentRankNameRelatedLabels();
                CreateRanksList();
                UpdateEverything();
            };

            SetupIconForRankUI(rank, rankVisualElement);

            DropdownField insideDropdownField = rankUIRoot.Q<DropdownField>("InsideDropDownField");

            SetupBorderForRankUI(rank, rankVisualElement, insideDropdownField);

            SetupInsideForRankUI(rank, rankVisualElement, insideDropdownField);

            DropdownField wingsDropDownField = rankUIRoot.Q<DropdownField>("WingsDropDownField");
            List<string> wingsChoices = GetWingsChoices();
            wingsDropDownField.choices = wingsChoices;
            wingsDropDownField.index = rank.wings + 1;
            wingsDropDownField.schedule.Execute(() =>
            {
                wingsDropDownField.RegisterValueChangedCallback(e =>
                {
                    Undo.RecordObject(animationController, animationController.gameObject.name + " rank updated.");
                    rank.wings = wingsDropDownField.index - 1;
                    EditorUtility.SetDirty(animationController);

                    if (rank.wings >= 0)
                        rankUIRoot.Q<GroupBox>("WingsAnimationSettingsGroupBox").style.display = DisplayStyle.Flex;
                    else
                        rankUIRoot.Q<GroupBox>("WingsAnimationSettingsGroupBox").style.display = DisplayStyle.None;
                });
            }).ExecuteLater(1000);

            SetupStateWingsAnimationUI(rank, rankVisualElement);

            if (rank.wings >= 0)
                rankVisualElement.Q<GroupBox>("WingsAnimationSettingsGroupBox").style.display = DisplayStyle.Flex;
            else
                rankVisualElement.Q<GroupBox>("WingsAnimationSettingsGroupBox").style.display = DisplayStyle.None;
        }

        private void SetupInsideForRankUI(Rank rank, VisualElement rankVisualElement, DropdownField insideDropdownField)
        {
            ObjectField insideMaterialField = rankVisualElement.Q<ObjectField>("InsideMaterialField");
            insideMaterialField.SetValueWithoutNotify(rank.insideMat);
            insideMaterialField.schedule.Execute(() =>
            {
                insideMaterialField.RegisterValueChangedCallback(e =>
                {
                    Undo.RecordObject(animationController, animationController.gameObject.name + " rank updated.");
                    if (e.newValue != null)
                        rank.insideMat = e.newValue as Material;
                    else
                        rank.insideMat = null;
                    EditorUtility.SetDirty(animationController);
                });
            }).ExecuteLater(1000);

            ObjectField wingsMaterialField = rankVisualElement.Q<ObjectField>("WingsMaterialField");
            wingsMaterialField.SetValueWithoutNotify(rank.wingsMat);
            wingsMaterialField.schedule.Execute(() =>
            {
                wingsMaterialField.RegisterValueChangedCallback(e =>
                {
                    Undo.RecordObject(animationController, animationController.gameObject.name + " rank updated.");
                    if (e.newValue != null)
                        rank.wingsMat = e.newValue as Material;
                    else
                        rank.wingsMat = null;
                    EditorUtility.SetDirty(animationController);
                });
            }).ExecuteLater(1000);

            List<string> insideChoices = GetInsideChoices(rank.border);
            insideDropdownField.choices = insideChoices;
            if (rank.inside == null)
                insideDropdownField.value = "None";
            else if (rank.inside.mesh == null)
                insideDropdownField.value = "None";
            else
                insideDropdownField.value = GetInsideNameFromMesh(rank.inside.mesh);

            insideDropdownField.schedule.Execute(() =>
            {
                insideDropdownField.RegisterValueChangedCallback(e =>
                {
                    Undo.RecordObject(animationController, animationController.gameObject.name + " rank updated.");

                    int insideIndex = insideDropdownField.index - 1;

                    if (insideIndex < 0 || insideIndex >= animationController.bodies[rank.border].inside.Length)
                    {
                        rank.inside = null;
                        rankVisualElement.Q<GroupBox>("InsideAnimationSettingsGroupBox").style.display = DisplayStyle.None;
                    }
                    else
                    {
                        rank.inside = new();
                        rank.inside.mesh = animationController.bodies[rank.border].inside[insideIndex].mesh;
                        rank.inside.fractured = animationController.bodies[rank.border].inside[insideIndex].fractured;
                        rankVisualElement.Q<GroupBox>("InsideAnimationSettingsGroupBox").style.display = DisplayStyle.Flex;
                    }

                    EditorUtility.SetDirty(animationController);
                });
            }).ExecuteLater(1000);

            SetupStateInsideAnimationUI(rank, rankVisualElement);
            if (rank.inside.mesh != null && rank.inside.fractured != null)
            {
                rankVisualElement.Q<GroupBox>("InsideAnimationSettingsGroupBox").style.display = DisplayStyle.Flex;
            }
            else
            {
                rankVisualElement.Q<GroupBox>("InsideAnimationSettingsGroupBox").style.display = DisplayStyle.None;
            }

            FloatField oldInsideExitAnimationStartDelay = rankVisualElement.Q<FloatField>("OldInsideExitAnimationStartDelay");
            oldInsideExitAnimationStartDelay.SetValueWithoutNotify(rank.oldInsideExitDelay);
            oldInsideExitAnimationStartDelay.schedule.Execute(() =>
            {
                oldInsideExitAnimationStartDelay.RegisterValueChangedCallback(e =>
                {
                    Undo.RecordObject(animationController, animationController.gameObject.name + " rank updated.");
                    rank.oldInsideExitDelay = e.newValue;
                    EditorUtility.SetDirty(animationController);
                });
            }).ExecuteLater(500);

            FloatField newInsideEntryAnimationStartDelay = rankVisualElement.Q<FloatField>("NewInsideEntryAnimationStartDelay");
            newInsideEntryAnimationStartDelay.SetValueWithoutNotify(rank.newInsideEntryDelay);
            newInsideEntryAnimationStartDelay.schedule.Execute(() =>
            {
                newInsideEntryAnimationStartDelay.RegisterValueChangedCallback(e =>
                {
                    Undo.RecordObject(animationController, animationController.gameObject.name + " rank updated.");
                    rank.newInsideEntryDelay = e.newValue;
                    EditorUtility.SetDirty(animationController);
                });
            }).ExecuteLater(500);
        }

        private void SetupBorderForRankUI(Rank rank, VisualElement rankVisualElement, DropdownField insideDropdownField)
        {
            GroupBox borderAnimationSettingsGroupBox = rankVisualElement.Q<GroupBox>("BorderAnimationSettingsGroupBox");
            SetupRankBorderAnimationUI(rank, rankVisualElement);

            if (rank.border >= 0)
                borderAnimationSettingsGroupBox.style.display = DisplayStyle.Flex;
            else
                rankVisualElement.Q<GroupBox>("BorderAnimationSettingsGroupBox").style.display = DisplayStyle.None;

            DropdownField borderDropdownField = rankVisualElement.Q<DropdownField>("BorderDropDownField");
            List<string> borderChoices = GetBorderChoices();
            borderDropdownField.choices = borderChoices;
            borderDropdownField.index = rank.border + 1;
            borderDropdownField.schedule.Execute(() =>
            {
                borderDropdownField.RegisterValueChangedCallback(e =>
                {
                    Undo.RecordObject(animationController, animationController.gameObject.name + " rank updated.");
                    int border = borderChoices.IndexOf(e.newValue) - 1;
                    rank.border = border;
                    if (border < 0)
                        rank.inside = null;
                    EditorUtility.SetDirty(animationController);

                    if (rank.border >= 0)
                        borderAnimationSettingsGroupBox.style.display = DisplayStyle.Flex;
                    else
                        borderAnimationSettingsGroupBox.style.display = DisplayStyle.None;

                    List<string> insideChoices = GetInsideChoices(border);
                    insideDropdownField.choices = insideChoices;
                    if (rank.inside == null)
                        insideDropdownField.value = "None";
                    else if (rank.inside.mesh == null)
                        insideDropdownField.value = "None";
                    else
                        insideDropdownField.value = GetInsideNameFromMesh(rank.inside.mesh);
                });
            }).ExecuteLater(1000);

            ObjectField borderMaterialField = rankVisualElement.Q<ObjectField>("BorderMaterialField");
            borderMaterialField.SetValueWithoutNotify(rank.borderMat);
            borderMaterialField.schedule.Execute(() =>
            {
                borderMaterialField.RegisterValueChangedCallback(e =>
                {
                    Undo.RecordObject(animationController, animationController.gameObject.name + " rank updated.");
                    if (e.newValue != null)
                        rank.borderMat = e.newValue as Material;
                    else
                        rank.borderMat = null;
                    EditorUtility.SetDirty(animationController);
                });
            }).ExecuteLater(1000);

            FloatField oldBorderExitDelay = rankVisualElement.Q<FloatField>("OldBorderExitAnimationStartDelay");
            oldBorderExitDelay.SetValueWithoutNotify(rank.oldBorderExitDelay);
            oldBorderExitDelay.schedule.Execute(() =>
            {
                oldBorderExitDelay.RegisterValueChangedCallback(e =>
                {
                    Undo.RecordObject(animationController, animationController.gameObject.name + " rank updated.");
                    rank.oldBorderExitDelay = e.newValue;
                    EditorUtility.SetDirty(animationController);
                });
            }).ExecuteLater(500);

            FloatField newBorderEntryDelay = rankVisualElement.Q<FloatField>("NewBorderEntryAnimationStartDelay");
            newBorderEntryDelay.SetValueWithoutNotify(rank.newBorderEntryDelay);
            newBorderEntryDelay.schedule.Execute(() =>
            {
                newBorderEntryDelay.RegisterValueChangedCallback(e =>
                {
                    Undo.RecordObject(animationController, animationController.gameObject.name + " rank updated.");
                    rank.newBorderEntryDelay = e.newValue;
                    EditorUtility.SetDirty(animationController);
                });
            }).ExecuteLater(500);
        }

        private void SetupIconForRankUI(Rank rank, VisualElement rankVisualElement)
        {
            var iconDropDownField = rankVisualElement.Q<DropdownField>("IconDropDownField");
            iconDropDownField.schedule.Execute(() =>
            {
                iconDropDownField.choices = iconChoices.ToList();
                iconDropDownField.SetValueWithoutNotify(rank.icon.ToString());
                iconDropDownField.RegisterValueChangedCallback(e =>
                {
                    Undo.RecordObject(animationController, animationController.gameObject.name + " rank updated.");
                    int.TryParse(e.newValue, out int result);
                    rank.icon = result;
                    EditorUtility.SetDirty(animationController);
                });
            }).ExecuteLater(1000);

            ObjectField iconPrimaryMaterialField = rankVisualElement.Q<ObjectField>("IconPrimaryMaterialField");
            iconPrimaryMaterialField.SetValueWithoutNotify(rank.iconPrimaryMat);
            iconPrimaryMaterialField.schedule.Execute(() =>
            {
                iconPrimaryMaterialField.RegisterValueChangedCallback(e =>
                {
                    Undo.RecordObject(animationController, animationController.gameObject.name + " rank updated.");
                    if (e.newValue != null)
                        rank.iconPrimaryMat = e.newValue as Material;
                    else
                        rank.iconPrimaryMat = null;
                    EditorUtility.SetDirty(animationController);
                });
            });

            ObjectField iconSecondaryMaterialField = rankVisualElement.Q<ObjectField>("IconSecondaryMaterialField");
            iconSecondaryMaterialField.SetValueWithoutNotify(rank.iconSecondaryMat);
            iconSecondaryMaterialField.schedule.Execute(() =>
            {
                iconSecondaryMaterialField.RegisterValueChangedCallback(e =>
                {
                    Undo.RecordObject(animationController, animationController.gameObject.name + " rank updated.");
                    if (e.newValue != null)
                        rank.iconSecondaryMat = e.newValue as Material;
                    else
                        rank.iconSecondaryMat = null;
                    EditorUtility.SetDirty(animationController);
                });
            });
            FloatField optOutDelay = rankVisualElement.Q<FloatField>("IconOptOutDelay");
            optOutDelay.SetValueWithoutNotify(rank.oldIconExitDelay);
            optOutDelay.schedule.Execute(() =>
            {
                optOutDelay.RegisterValueChangedCallback(e =>
                {
                    Undo.RecordObject(animationController, animationController.gameObject.name + " rank updated.");
                    rank.oldIconExitDelay = e.newValue;
                    EditorUtility.SetDirty(animationController);
                });
            });
            FloatField optInDelay = rankVisualElement.Q<FloatField>("IconOptInDelay");
            optInDelay.SetValueWithoutNotify(rank.newIconEntryDelay);
            optInDelay.schedule.Execute(() =>
            {
                optInDelay.RegisterValueChangedCallback(e =>
                {
                    Undo.RecordObject(animationController, animationController.gameObject.name + " rank updated.");
                    rank.newIconEntryDelay = e.newValue;
                    EditorUtility.SetDirty(animationController);
                });
            });

            ObjectField newIconEntryAudioClipField = rankVisualElement.Q<ObjectField>("NewIconEntryAudioClip");
            newIconEntryAudioClipField.SetValueWithoutNotify(rank.newIconEntryAudioClip);
            newIconEntryAudioClipField.schedule.Execute(() =>
            {
                newIconEntryAudioClipField.RegisterValueChangedCallback(e =>
                {
                    Undo.RecordObject(animationController, animationController.gameObject.name + " rank updated.");
                    if (e.newValue != null)
                        rank.newIconEntryAudioClip = e.newValue as AudioClip;
                    else
                        rank.newIconEntryAudioClip = null;
                    EditorUtility.SetDirty(animationController);
                });
            });

            ObjectField oldIconExitAudioClipField = rankVisualElement.Q<ObjectField>("OldIconExitAudioClip");
            oldIconExitAudioClipField.SetValueWithoutNotify(rank.oldIconExitAudioClip);
            oldIconExitAudioClipField.schedule.Execute(() =>
            {
                oldIconExitAudioClipField.RegisterValueChangedCallback(e =>
                {
                    Undo.RecordObject(animationController, animationController.gameObject.name + " rank updated.");
                    if (e.newValue != null)
                        rank.oldIconExitAudioClip = e.newValue as AudioClip;
                    else
                        rank.oldIconExitAudioClip = null;
                    EditorUtility.SetDirty(animationController);
                });
            });
        }

        private string[] iconChoices;

        private void UpdateIconChoices()
        {
            iconChoices = Enumerable.Range(0, iconsAmount).Select(i => i.ToString()).ToArray();
            if (iconChoices.Length > 0)
                iconChoices[0] = "None";
        }

        private void SetupRankBorderAnimationUI(Rank state, VisualElement stateVisualElement)
        {
            EnumField borderEntryAnimationType = stateVisualElement.Q<EnumField>("BorderEntryAnimation");
            borderEntryAnimationType.value = state.newBorderEntryAnimationType;
            borderEntryAnimationType.RegisterValueChangedCallback(e =>
            {
                Undo.RecordObject(animationController, animationController.gameObject.name + " rank updated.");
                state.newBorderEntryAnimationType = (EntryAnimationType)e.newValue;
                EditorUtility.SetDirty(animationController);
            });

            SetupBorderExitAnimationControlUI(state, stateVisualElement);

            Toggle transitionToSameBorder = stateVisualElement.Q<Toggle>("TransitionToSameBorder");
            transitionToSameBorder.SetValueWithoutNotify(animationController.currentRank.transitionToSameBorder);
            transitionToSameBorder.RegisterValueChangedCallback(e =>
            {
                Undo.RecordObject(animationController, animationController.gameObject.name + " rank updated.");
                animationController.currentRank.transitionToSameBorder = e.newValue;
                EditorUtility.SetDirty(animationController);
            });
        }

        private void SetupBorderExitAnimationControlUI(Rank rank, VisualElement borderAnimationSettingsGroupBox)
        {
            EnumField borderExitAnimation = borderAnimationSettingsGroupBox.Q<EnumField>("BorderExitAnimation");
            GroupBox physicsGroupbox = borderAnimationSettingsGroupBox.Q<GroupBox>("BorderExitAnimationPhysicsGroupbox");
            GroupBox explosionGroupbox = borderAnimationSettingsGroupBox.Q<GroupBox>("ExplosionGroupBox");

            borderExitAnimation.value = rank.oldBorderExitAnimationType;
            UpdatePhysicsGroupBox();

            borderExitAnimation.RegisterValueChangedCallback(e =>
            {
                Undo.RecordObject(animationController, animationController.gameObject.name + " states updated.");
                rank.oldBorderExitAnimationType = (ExitAnimationType)e.newValue;
                EditorUtility.SetDirty(animationController);
                UpdatePhysicsGroupBox();
            });

            void UpdatePhysicsGroupBox()
            {
                if (rank.oldBorderExitAnimationType == ExitAnimationType.DropByPhysics || rank.oldBorderExitAnimationType == ExitAnimationType.ExplodeByPhysics)
                {
                    physicsGroupbox.style.display = DisplayStyle.Flex;

                    if (rank.oldBorderExitAnimationType == ExitAnimationType.ExplodeByPhysics)
                        explosionGroupbox.style.display = DisplayStyle.Flex;
                    else
                        explosionGroupbox.style.display = DisplayStyle.None;
                }
                else
                {
                    physicsGroupbox.style.display = DisplayStyle.None;
                }
            }

            ObjectField oldBorderExitAudioClip = borderAnimationSettingsGroupBox.Q<ObjectField>("OldBorderExitAudioClip");
            oldBorderExitAudioClip.SetValueWithoutNotify(rank.oldBorderExitAudioClip);
            oldBorderExitAudioClip.schedule.Execute(() =>
            {
                oldBorderExitAudioClip.RegisterValueChangedCallback(e =>
                {
                    Undo.RecordObject(animationController, animationController.gameObject.name + " rank updated.");
                    if (e.newValue != null)
                        rank.oldBorderExitAudioClip = e.newValue as AudioClip;
                    else
                        rank.oldBorderExitAudioClip = null;
                    EditorUtility.SetDirty(animationController);
                });
            });

            ObjectField newBorderEntryAudioClip = borderAnimationSettingsGroupBox.Q<ObjectField>("NewBorderEntryAudioClip");
            newBorderEntryAudioClip.SetValueWithoutNotify(rank.newBorderEntryAudioClip);
            newBorderEntryAudioClip.schedule.Execute(() =>
            {
                newBorderEntryAudioClip.RegisterValueChangedCallback(e =>
                {
                    Undo.RecordObject(animationController, animationController.gameObject.name + " rank updated.");
                    if (e.newValue != null)
                        rank.newBorderEntryAudioClip = e.newValue as AudioClip;
                    else
                        rank.newBorderEntryAudioClip = null;
                    EditorUtility.SetDirty(animationController);
                });
            });

            FloatField oldBorderExitAudioClipDelay = borderAnimationSettingsGroupBox.Q<FloatField>("OldBorderExitAudioClipDelay");
            oldBorderExitAudioClipDelay.SetValueWithoutNotify(rank.oldBorderExitAudioClipDelay);
            oldBorderExitAudioClipDelay.schedule.Execute(() =>
            {
                oldBorderExitAudioClipDelay.RegisterValueChangedCallback(e =>
                {
                    Undo.RecordObject(animationController, animationController.gameObject.name + " rank updated.");
                    rank.oldBorderExitAudioClipDelay = e.newValue;
                    EditorUtility.SetDirty(animationController);
                });
            });

            //For exit animation
            SetupBorderPhysicsSettings(rank, physicsGroupbox);
        }

        private void SetupBorderPhysicsSettings(Rank state, VisualElement physicsGroupbox)
        {
            GroupBox borderFracturedPiecesLifeTimeGroupbox = physicsGroupbox.Q<GroupBox>("FracturedPiecesLifeTimeGroupbox");
            MinMaxSlider fracturedPiecesLifeTime = borderFracturedPiecesLifeTimeGroupbox.Q<MinMaxSlider>("FracturedPiecesLifeTimeSlider");
            FloatField fracturedMinimumLifetime = borderFracturedPiecesLifeTimeGroupbox.Q<FloatField>("Minimum");
            FloatField fracturedMaximumLifetime = borderFracturedPiecesLifeTimeGroupbox.Q<FloatField>("Maximum");

            if (state.oldBorderExitAnimation_physics_fracturedPiecesLifeTime == new Vector2(0, 0))
            {
                state.oldBorderExitAnimation_physics_fracturedPiecesLifeTime = new Vector2(20, 30);
                EditorUtility.SetDirty(animationController);
            }
            fracturedPiecesLifeTime.value = state.oldBorderExitAnimation_physics_fracturedPiecesLifeTime;
            borderFracturedPiecesLifeTimeGroupbox.tooltip = "Fractured pieces will last between " + state.oldBorderExitAnimation_physics_fracturedPiecesLifeTime.x + " and " + state.oldBorderExitAnimation_physics_fracturedPiecesLifeTime.y + "seconds.";
            fracturedPiecesLifeTime.RegisterValueChangedCallback(e =>
            {
                Undo.RecordObject(animationController, animationController.gameObject.name + " state updated.");
                state.oldBorderExitAnimation_physics_fracturedPiecesLifeTime = e.newValue;
                EditorUtility.SetDirty(animationController);
                borderFracturedPiecesLifeTimeGroupbox.tooltip = "Fractured pieces will last between " + state.oldBorderExitAnimation_physics_fracturedPiecesLifeTime.x + " and " + state.oldBorderExitAnimation_physics_fracturedPiecesLifeTime.y + "seconds.";
                fracturedMinimumLifetime.SetValueWithoutNotify(state.oldBorderExitAnimation_physics_fracturedPiecesLifeTime.x);
                fracturedMaximumLifetime.SetValueWithoutNotify(state.oldBorderExitAnimation_physics_fracturedPiecesLifeTime.y);
            });

            fracturedMinimumLifetime.SetValueWithoutNotify(state.oldBorderExitAnimation_physics_fracturedPiecesLifeTime.x);
            fracturedMaximumLifetime.SetValueWithoutNotify(state.oldBorderExitAnimation_physics_fracturedPiecesLifeTime.y);

            fracturedMinimumLifetime.RegisterValueChangedCallback(e =>
            {
                Undo.RecordObject(animationController, animationController.gameObject.name + " state updated.");
                state.oldBorderExitAnimation_physics_fracturedPiecesLifeTime.x = e.newValue;
                EditorUtility.SetDirty(animationController);
                borderFracturedPiecesLifeTimeGroupbox.tooltip = "Fractured pieces will last between " + state.oldBorderExitAnimation_physics_fracturedPiecesLifeTime.x + " and " + state.oldBorderExitAnimation_physics_fracturedPiecesLifeTime.y + "seconds.";
                fracturedPiecesLifeTime.SetValueWithoutNotify(state.oldBorderExitAnimation_physics_fracturedPiecesLifeTime);
            });

            fracturedMaximumLifetime.RegisterValueChangedCallback(e =>
            {
                Undo.RecordObject(animationController, animationController.gameObject.name + " state updated.");
                state.oldBorderExitAnimation_physics_fracturedPiecesLifeTime.y = e.newValue;
                EditorUtility.SetDirty(animationController);
                borderFracturedPiecesLifeTimeGroupbox.tooltip = "Fractured pieces will last between " + state.oldBorderExitAnimation_physics_fracturedPiecesLifeTime.x + " and " + state.oldBorderExitAnimation_physics_fracturedPiecesLifeTime.y + "seconds.";
                fracturedPiecesLifeTime.SetValueWithoutNotify(state.oldBorderExitAnimation_physics_fracturedPiecesLifeTime);
            });

            FloatField explosionForce = physicsGroupbox.Q<FloatField>("ExplosionForce");
            if (state.oldBorderExitAnimation_physics_explosionForce < 0.01f)
            {
                state.oldBorderExitAnimation_physics_explosionForce = 10;
                EditorUtility.SetDirty(animationController);
            }
            explosionForce.value = state.oldBorderExitAnimation_physics_explosionForce;
            explosionForce.RegisterValueChangedCallback(e =>
            {
                Undo.RecordObject(animationController, animationController.gameObject.name + " state updated.");
                state.oldBorderExitAnimation_physics_explosionForce = e.newValue;
                EditorUtility.SetDirty(animationController);
            });

            FloatField explosionRadius = physicsGroupbox.Q<FloatField>("ExplosionRadius");
            if (state.oldBorderExitAnimation_physics_explosionRadius < 0.01f)
            {
                state.oldBorderExitAnimation_physics_explosionRadius = 3;
                EditorUtility.SetDirty(animationController);
            }
            explosionRadius.value = state.oldBorderExitAnimation_physics_explosionRadius;
            explosionRadius.RegisterValueChangedCallback(e =>
            {
                Undo.RecordObject(animationController, animationController.gameObject.name + " state updated.");
                state.oldBorderExitAnimation_physics_explosionRadius = e.newValue;
                EditorUtility.SetDirty(animationController);
            });

            FloatField explosionUpwardsForce = physicsGroupbox.Q<FloatField>("ExplosionUpwardsForce");
            //if (state.borderExitAnimation_physics_explosionUpwardsForce < 0.01f)
            //{
            //    state.borderExitAnimation_physics_explosionUpwardsForce = 0.3f;
            //    EditorUtility.SetDirty(animationController);
            //}
            explosionUpwardsForce.value = state.oldBorderExitAnimation_physics_explosionUpwardsForce;
            explosionUpwardsForce.RegisterValueChangedCallback(e =>
            {
                Undo.RecordObject(animationController, animationController.gameObject.name + " state updated.");
                state.oldBorderExitAnimation_physics_explosionUpwardsForce = e.newValue;
                EditorUtility.SetDirty(animationController);
            });
        }

        private void SetupStateInsideAnimationUI(Rank state, VisualElement setVisualElement)
        {
            EnumField insideEntryAnimationType = setVisualElement.Q<EnumField>("InsideEntryAnimation");
            insideEntryAnimationType.value = state.newInsideEntryAnimationType;
            insideEntryAnimationType.RegisterValueChangedCallback(e =>
            {
                Undo.RecordObject(animationController, animationController.gameObject.name + " states updated.");
                state.newInsideEntryAnimationType = (EntryAnimationType)e.newValue;
                EditorUtility.SetDirty(animationController);
            });

            SetupInsideExitAnimationControlUI(state, setVisualElement);
        }

        private void SetupInsideExitAnimationControlUI(Rank rank, VisualElement rankVisualElement)
        {
            EnumField insideExitAnimation = rankVisualElement.Q<EnumField>("InsideExitAnimation");
            GroupBox physicsGroupbox = rankVisualElement.Q<GroupBox>("InsideExitAnimationPhysicsGroupbox");
            GroupBox explosionGroupbox = physicsGroupbox.Q<GroupBox>("ExplosionGroupBox");

            UpdatePhysicsGroupBox();

            insideExitAnimation.value = rank.oldInsideExitAnimationType;
            insideExitAnimation.RegisterValueChangedCallback(e =>
            {
                Undo.RecordObject(animationController, animationController.gameObject.name + " states updated.");
                rank.oldInsideExitAnimationType = (ExitAnimationType)e.newValue;
                EditorUtility.SetDirty(animationController);
                UpdatePhysicsGroupBox();
            });

            Toggle transitionToSameInside = rankVisualElement.Q<Toggle>("TransitionToSameInside");
            transitionToSameInside.SetValueWithoutNotify(animationController.currentRank.transitionToSameInside);
            transitionToSameInside.RegisterValueChangedCallback(e =>
            {
                Undo.RecordObject(animationController, animationController.gameObject.name + " states updated.");
                animationController.currentRank.transitionToSameInside = e.newValue;
                EditorUtility.SetDirty(animationController);
            });

            void UpdatePhysicsGroupBox()
            {
                if (rank.oldInsideExitAnimationType == ExitAnimationType.DropByPhysics || rank.oldInsideExitAnimationType == ExitAnimationType.ExplodeByPhysics)
                {
                    physicsGroupbox.style.display = DisplayStyle.Flex;
                    if (rank.oldInsideExitAnimationType == ExitAnimationType.ExplodeByPhysics)
                        explosionGroupbox.style.display = DisplayStyle.Flex;
                    else
                        explosionGroupbox.style.display = DisplayStyle.None;
                }
                else
                    physicsGroupbox.style.display = DisplayStyle.None;
            }

            SetupInsidePhysicsSettings(rank, physicsGroupbox);

            ObjectField oldInsideExitAudioClip = rankVisualElement.Q<ObjectField>("OldInsideExitAudioClip");
            oldInsideExitAudioClip.SetValueWithoutNotify(rank.oldInsideExitAudioClip);
            oldInsideExitAudioClip.schedule.Execute(() =>
            {
                oldInsideExitAudioClip.RegisterValueChangedCallback(e =>
                {
                    Undo.RecordObject(animationController, animationController.gameObject.name + " rank updated.");
                    if (e.newValue != null)
                        rank.oldInsideExitAudioClip = e.newValue as AudioClip;
                    else
                        rank.oldInsideExitAudioClip = null;
                    EditorUtility.SetDirty(animationController);
                });
            });

            FloatField oldInsideExitAudioClipDelay = rankVisualElement.Q<FloatField>("OldInsideExitAudioClipDelay");
            oldInsideExitAudioClipDelay.SetValueWithoutNotify(rank.oldInsideExitAudioClipDelay);
            oldInsideExitAudioClipDelay.schedule.Execute(() =>
            {
                oldInsideExitAudioClipDelay.RegisterValueChangedCallback(e =>
                {
                    Undo.RecordObject(animationController, animationController.gameObject.name + " rank updated.");

                    rank.oldInsideExitAudioClipDelay = e.newValue;
                    EditorUtility.SetDirty(animationController);
                });
            });

            ObjectField newInsideEntryAudioClip = rankVisualElement.Q<ObjectField>("NewInsideEntryAudioClip");
            newInsideEntryAudioClip.SetValueWithoutNotify(rank.newInsideEntryAudioClip);
            newInsideEntryAudioClip.schedule.Execute(() =>
            {
                newInsideEntryAudioClip.RegisterValueChangedCallback(e =>
                {
                    Undo.RecordObject(animationController, animationController.gameObject.name + " rank updated.");
                    if (e.newValue != null)
                        rank.newInsideEntryAudioClip = e.newValue as AudioClip;
                    else
                        rank.newInsideEntryAudioClip = null;
                    EditorUtility.SetDirty(animationController);
                });
            });
        }

        private void SetupInsidePhysicsSettings(Rank state, VisualElement physicsGroupbox)
        {
            GroupBox fracturedPiecesLifeTimeGroupbox = physicsGroupbox.Q<GroupBox>("FracturedPiecesLifeTimeGroupbox");
            MinMaxSlider fracturedPiecesLifeTime = fracturedPiecesLifeTimeGroupbox.Q<MinMaxSlider>("FracturedPiecesLifeTimeSlider");
            FloatField fracturedMinimumLifetime = fracturedPiecesLifeTimeGroupbox.Q<FloatField>("Minimum");
            FloatField fracturedMaximumLifetime = fracturedPiecesLifeTimeGroupbox.Q<FloatField>("Maximum");

            if (state.oldInsideExitAnimation_physics_fracturedPiecesLifeTime == new Vector2(0, 0))
            {
                state.oldInsideExitAnimation_physics_fracturedPiecesLifeTime = new Vector2(20, 30);
                EditorUtility.SetDirty(animationController);
            }
            fracturedPiecesLifeTime.value = state.oldInsideExitAnimation_physics_fracturedPiecesLifeTime;
            fracturedPiecesLifeTimeGroupbox.tooltip = "Fractured pieces will last between " + state.oldInsideExitAnimation_physics_fracturedPiecesLifeTime.x + " and " + state.oldInsideExitAnimation_physics_fracturedPiecesLifeTime.y + "seconds.";
            fracturedPiecesLifeTime.RegisterValueChangedCallback(e =>
            {
                Undo.RecordObject(animationController, animationController.gameObject.name + " rank updated.");
                state.oldInsideExitAnimation_physics_fracturedPiecesLifeTime = e.newValue;
                EditorUtility.SetDirty(animationController);
                fracturedPiecesLifeTimeGroupbox.tooltip = "Fractured pieces will last between " + state.oldInsideExitAnimation_physics_fracturedPiecesLifeTime.x + " and " + state.oldInsideExitAnimation_physics_fracturedPiecesLifeTime.y + "seconds.";
                fracturedMinimumLifetime.SetValueWithoutNotify(state.oldInsideExitAnimation_physics_fracturedPiecesLifeTime.x);
                fracturedMaximumLifetime.SetValueWithoutNotify(state.oldInsideExitAnimation_physics_fracturedPiecesLifeTime.y);
            });

            fracturedMinimumLifetime.SetValueWithoutNotify(state.oldInsideExitAnimation_physics_fracturedPiecesLifeTime.x);
            fracturedMaximumLifetime.SetValueWithoutNotify(state.oldInsideExitAnimation_physics_fracturedPiecesLifeTime.y);

            fracturedMinimumLifetime.RegisterValueChangedCallback(e =>
            {
                Undo.RecordObject(animationController, animationController.gameObject.name + " rank updated.");
                state.oldInsideExitAnimation_physics_fracturedPiecesLifeTime.x = e.newValue;
                EditorUtility.SetDirty(animationController);
                fracturedPiecesLifeTimeGroupbox.tooltip = "Fractured pieces will last between " + state.oldInsideExitAnimation_physics_fracturedPiecesLifeTime.x + " and " + state.oldInsideExitAnimation_physics_fracturedPiecesLifeTime.y + "seconds.";
                fracturedPiecesLifeTime.SetValueWithoutNotify(state.oldInsideExitAnimation_physics_fracturedPiecesLifeTime);
            });

            fracturedMaximumLifetime.RegisterValueChangedCallback(e =>
            {
                Undo.RecordObject(animationController, animationController.gameObject.name + " rank updated.");
                state.oldInsideExitAnimation_physics_fracturedPiecesLifeTime.y = e.newValue;
                EditorUtility.SetDirty(animationController);
                fracturedPiecesLifeTimeGroupbox.tooltip = "Fractured pieces will last between " + state.oldInsideExitAnimation_physics_fracturedPiecesLifeTime.x + " and " + state.oldInsideExitAnimation_physics_fracturedPiecesLifeTime.y + "seconds.";
                fracturedPiecesLifeTime.SetValueWithoutNotify(state.oldInsideExitAnimation_physics_fracturedPiecesLifeTime);
            });

            FloatField explosionForce = physicsGroupbox.Q<FloatField>("ExplosionForce");
            if (state.oldInsideExitAnimation_physics_explosionForce < 0.01f)
            {
                state.oldInsideExitAnimation_physics_explosionForce = 10;
                EditorUtility.SetDirty(animationController);
            }
            explosionForce.value = state.oldInsideExitAnimation_physics_explosionForce;
            explosionForce.RegisterValueChangedCallback(e =>
            {
                Undo.RecordObject(animationController, animationController.gameObject.name + " rank updated.");
                state.oldInsideExitAnimation_physics_explosionForce = e.newValue;
                EditorUtility.SetDirty(animationController);
            });

            FloatField explosionRadius = physicsGroupbox.Q<FloatField>("ExplosionRadius");
            if (state.oldInsideExitAnimation_physics_explosionRadius < 0.01f)
            {
                state.oldInsideExitAnimation_physics_explosionRadius = 3;
                EditorUtility.SetDirty(animationController);
            }
            explosionRadius.value = state.oldInsideExitAnimation_physics_explosionRadius;
            explosionRadius.RegisterValueChangedCallback(e =>
            {
                Undo.RecordObject(animationController, animationController.gameObject.name + " rank updated.");
                state.oldInsideExitAnimation_physics_explosionRadius = e.newValue;
                EditorUtility.SetDirty(animationController);
            });

            FloatField explosionUpwardsForce = physicsGroupbox.Q<FloatField>("ExplosionUpwardsForce");
            if (state.oldInsideExitAnimation_physics_explosionUpwardsForce < 0.01f)
            {
                state.oldInsideExitAnimation_physics_explosionUpwardsForce = 0.3f;
                EditorUtility.SetDirty(animationController);
            }
            explosionUpwardsForce.value = state.oldInsideExitAnimation_physics_explosionUpwardsForce;
            explosionUpwardsForce.RegisterValueChangedCallback(e =>
            {
                Undo.RecordObject(animationController, animationController.gameObject.name + " rank updated.");
                state.oldInsideExitAnimation_physics_explosionUpwardsForce = e.newValue;
                EditorUtility.SetDirty(animationController);
            });
        }

        private void SetupStateWingsAnimationUI(Rank rank, VisualElement rankVisualElement)
        {
            FloatField wingsOutAnimationStartDelay = rankVisualElement.Q<FloatField>("WingsOutAnimationStartDelay");
            wingsOutAnimationStartDelay.value = rank.oldWingsExitAnimationStartDelay;
            wingsOutAnimationStartDelay.RegisterValueChangedCallback(e =>
            {
                Undo.RecordObject(animationController, animationController.gameObject.name + " wings movement in start delay updated.");
                rank.oldWingsExitAnimationStartDelay = e.newValue;
                EditorUtility.SetDirty(animationController);
            });

            CurveField wingsOutMovementCurveField = rankVisualElement.Q<CurveField>("WingsOutMovementCurveField");
            if (rank.oldWingsExitMovementCurve.keys.Length == 0)
            {
                rank.oldWingsExitMovementCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f); ;
                EditorUtility.SetDirty(animationController);
            }
            wingsOutMovementCurveField.value = rank.oldWingsExitMovementCurve;
            wingsOutMovementCurveField.RegisterValueChangedCallback(e =>
            {
                Undo.RecordObject(animationController, animationController.gameObject.name + " wings movement curve updated.");
                rank.oldWingsExitMovementCurve = e.newValue;
                EditorUtility.SetDirty(animationController);
            });

            FloatField wingsOutPositionAnimationDuration = rankVisualElement.Q<FloatField>("WingsOutPositionAnimationDuration");
            wingsOutPositionAnimationDuration.value = rank.oldWingsExitPositionAnimationDuration;
            wingsOutPositionAnimationDuration.RegisterValueChangedCallback(e =>
            {
                Undo.RecordObject(animationController, animationController.gameObject.name + " wings movement out duration updated.");
                rank.oldWingsExitPositionAnimationDuration = e.newValue;
                EditorUtility.SetDirty(animationController);
            });

            ObjectField oldWingsExitAudioClip = rankVisualElement.Q<ObjectField>("OldWingsExitAudioClip");
            oldWingsExitAudioClip.SetValueWithoutNotify(rank.oldWingsExitAudioClip);
            oldWingsExitAudioClip.schedule.Execute(() =>
            {
                oldWingsExitAudioClip.RegisterValueChangedCallback(e =>
                {
                    Undo.RecordObject(animationController, animationController.gameObject.name + " rank updated.");
                    if (e.newValue != null)
                        rank.oldWingsExitAudioClip = e.newValue as AudioClip;
                    else
                        rank.oldWingsExitAudioClip = null;
                    EditorUtility.SetDirty(animationController);
                });
            });

            ObjectField newWingsEntryAudioClip = rankVisualElement.Q<ObjectField>("NewWingsEntryAudioClip");
            newWingsEntryAudioClip.SetValueWithoutNotify(rank.newWingsEntryAudioClip);
            newWingsEntryAudioClip.schedule.Execute(() =>
            {
                newWingsEntryAudioClip.RegisterValueChangedCallback(e =>
                {
                    Undo.RecordObject(animationController, animationController.gameObject.name + " rank updated.");
                    if (e.newValue != null)
                        rank.newWingsEntryAudioClip = e.newValue as AudioClip;
                    else
                        rank.newWingsEntryAudioClip = null;
                    EditorUtility.SetDirty(animationController);
                });
            });

            FloatField newWingsInAnimationStartDelay = rankVisualElement.Q<FloatField>("WingsInAnimationStartDelay");
            newWingsInAnimationStartDelay.value = rank.newWingsEntryAnimationStartDelay;
            newWingsInAnimationStartDelay.RegisterValueChangedCallback(e =>
            {
                Undo.RecordObject(animationController, animationController.gameObject.name + " wings movement in start delay updated.");
                rank.newWingsEntryAnimationStartDelay = e.newValue;
                EditorUtility.SetDirty(animationController);
            });

            FloatField wingsInPositionAnimationDuration = rankVisualElement.Q<FloatField>("WingsInPositionAnimationDuration");
            wingsInPositionAnimationDuration.value = rank.newWingsStartPositionAnimationDuration;
            wingsInPositionAnimationDuration.RegisterValueChangedCallback(e =>
            {
                Undo.RecordObject(animationController, animationController.gameObject.name + " wings movement out duration updated.");
                rank.newWingsStartPositionAnimationDuration = e.newValue;
                EditorUtility.SetDirty(animationController);
            });

            CurveField wingsInMovementCurveField = rankVisualElement.Q<CurveField>("WingsInMovementCurveField");
            if (rank.newWingsInMovementCurve.keys.Length == 0)
            {
                rank.newWingsInMovementCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f); ;
                EditorUtility.SetDirty(animationController);
            }
            wingsInMovementCurveField.value = rank.newWingsInMovementCurve;
            wingsInMovementCurveField.RegisterValueChangedCallback(e =>
            {
                Undo.RecordObject(animationController, animationController.gameObject.name + " wings movement curve updated.");
                rank.newWingsInMovementCurve = e.newValue;
                EditorUtility.SetDirty(animationController);
            });
        }

        #endregion Ranks Tab

        #region Customize Tab

        private void SetupCustomizeTab()
        {
            customTabContent.Q<Button>("RemoveEverythingButton").clicked += () =>
            {
                if (EditorUtility.DisplayDialog("Remove Everything?", "This will set everything to null, creating an empty state.", "Confirm", "Cancel"))
                {
                    animationController.DisableEverything();

                    UpdateEverything();
                }
            };

            customTabContent.schedule.Execute(() =>
            {
                SetupRankIconButtons();
            }).ExecuteLater(1000);

            SetupRankIconsMaterialButtons();

            SetupBodyFoldout();

            SetupWings();

            SetupWingsMaterialButtons();

            //SetupAddons();

            SetupSaveState();
        }

        private void UpdateCustomTab()
        {
            UpdateRankIconButtonStyle();
            UpdateIconMaterialButtonStyle();

            borderEntryAnimationType.SetValueWithoutNotify(animationController.currentRank.newBorderEntryAnimationType);
            borderExitAnimationType.SetValueWithoutNotify(animationController.currentRank.oldBorderExitAnimationType);
            UpdateBorderButtonStyle();
            UpdateBorderMaterialButtonsStyle();

            if (insideEntryAnimationType != null)
                insideEntryAnimationType.SetValueWithoutNotify(animationController.currentRank.newInsideEntryAnimationType);
            if (insideExitAnimationType != null)
                insideExitAnimationType.SetValueWithoutNotify(animationController.currentRank.oldInsideExitAnimationType);
            UpdateInsideButtons();
            UpdateInsideButtonStyle();
            UpdateInsideMaterialButtonsStyle();

            UpdateWingsButtonStyle();
            UpdateWingsMaterialButtonsStyle();

            //UpdateAddonsButtonStyle();
        }

        private void SetupSaveState()
        {
            saveButton = customTabContent.Q<Button>("SaveStateButton");
            rankNameInput = customTabContent.Q<TextField>("StateSaveNameInputField");
            rankNameInput.value = animationController.currentRank.name;
            rankNameInput.schedule.Execute(() =>
            {
                rankNameInput.RegisterValueChangedCallback(evt =>
                {
                    animationController.currentRank.name = rankNameInput.value;
                    UpdateCurrentRankNameRelatedLabels();
                });

                UpdateCurrentRankNameRelatedLabels();
            }).ExecuteLater(1);

            saveButton.clicked += () =>
            {
                string rankName = rankNameInput.value;
                if (string.IsNullOrWhiteSpace(rankName))
                {
                    if (EditorUtility.DisplayDialog("Rank name can't be empty", "Please enter a valid name.", "OK"))
                    {
                        return;
                    }
                }
                //New name
                if (animationController.IsRankNameNew(rankName))
                {
                    if (EditorUtility.DisplayDialog("Save as new rank?", "Save current state as a new rank.", "Save new rank", "Cancel"))
                    {
                        Undo.RecordObject(animationController, rankName + " rank added");
                        Rank rank = new Rank(animationController.currentRank);
                        rank.name = rankNameInput.value;
                        animationController.ranks.Add(rank);
                        EditorUtility.SetDirty(animationController);

                        UpdateCurrentRankNameRelatedLabels();

                        UpdateEverything();
                    }
                }
                //Old name, update Rank
                else
                {
                    int index = animationController.IndexOfRank(rankName);
                    if (index >= 0)
                    {
                        ///This copies settings from customize tab and pastes them into Rank. Variables that aren't modifiable via customize tab don't need to be here.
                        if (EditorUtility.DisplayDialog("Update existing rank?", "This will update a rank that already exists.", "Update Rank", "Cancel"))
                        {
                            Undo.RecordObject(animationController, rankName + " rank updated");

                            animationController.ranks[index].icon = animationController.currentRank.icon;
                            animationController.ranks[index].iconPrimaryMat = animationController.currentRank.iconPrimaryMat;
                            animationController.ranks[index].iconSecondaryMat = animationController.currentRank.iconSecondaryMat;

                            animationController.ranks[index].border = animationController.currentRank.border;
                            animationController.ranks[index].borderMat = animationController.currentRank.borderMat;

                            animationController.ranks[index].inside = animationController.currentRank.inside;
                            animationController.ranks[index].insideMat = animationController.currentRank.insideMat;

                            animationController.ranks[index].wings = animationController.currentRank.wings;
                            animationController.ranks[index].wingsMat = animationController.currentRank.wingsMat;

                            //animationController.ranks[index].addons = animationController.currentRank.addons;

                            EditorUtility.SetDirty(animationController);

                            UpdateEverything();
                        }
                    }
                }
            };
        }

        private void UpdateCurrentRankNameRelatedLabels()
        {
            string rankName = rankNameInput.value;
            var rankNameNew = animationController.IsRankNameNew(rankName);

            if (rankNameNew)
            {
                saveButton.text = "Save";
                saveButton.tooltip = "This will create a new Rank";
                customizeTabButton.Q<Label>().text = "Customize New Rank";
            }
            else
            {
                saveButton.text = "Update";
                saveButton.tooltip = "A rank with this name already exists. Pressing this now will update that rank.";
                customizeTabButton.Q<Label>().text = "Customize " + rankName;
            }
        }

        private void SetupBodyFoldout()
        {
            var borderAnimationTestingGroupBox = customTabContent.Q<GroupBox>("BorderAnimationTestingGroupBox");
            if (!Application.isPlaying)
                borderAnimationTestingGroupBox.style.display = DisplayStyle.None;

            borderEntryAnimationType = customTabContent.Q<EnumField>("BorderEntryAnimationField");
            borderEntryAnimationType.value = animationController.currentRank.newBorderEntryAnimationType;
            borderEntryAnimationType.RegisterValueChangedCallback(e =>
            {
                animationController.currentRank.newBorderEntryAnimationType = (EntryAnimationType)e.newValue;
            });

            borderExitAnimationType = customTabContent.Q<EnumField>("BorderExitAnimationField");
            borderExitAnimationType.value = animationController.currentRank.oldBorderExitAnimationType;
            borderExitAnimationType.RegisterValueChangedCallback(e =>
            {
                animationController.currentRank.oldBorderExitAnimationType = (ExitAnimationType)e.newValue;
            });

            SetupBodyButtons();

            insidesFoldout = customTabContent.Q<Foldout>("InsidesFoldout");
            if (animationController.currentRank.border < 0)
                insidesFoldout.style.display = DisplayStyle.None;
            else
                insidesFoldout.style.display = DisplayStyle.Flex;

            insideMaterialsFoldout = customTabContent.Q<GroupBox>("InsideMaterialsFoldout");
            if (animationController.currentRank.inside == null || animationController.currentRank.inside.mesh == null)
                insideMaterialsFoldout.style.display = DisplayStyle.None;
            else
                insideMaterialsFoldout.style.display = DisplayStyle.Flex;

            SetupBorderMaterialButtons();
            SetupInsideMaterialButtons();
        }

        private void SetupSpawnAnimationButtons()
        {
            var spawnAnimationsContainer = root.Q<Foldout>("SpawnAnimationsContainer");
            var spawnAnimationButtons = root.Q<GroupBox>("SpawnAnimationButtons");
            IntegerField baseAnimationCounterField = root.Q<IntegerField>("BaseAnimationCounterField");
            var onEnableBaseAnimationSlider = root.Q<SliderInt>("OnEnableBaseAnimationSlider");

            if (!Application.isPlaying)
            {
                spawnAnimationButtons.parent.Insert(spawnAnimationButtons.parent.IndexOf(spawnAnimationButtons), new HelpBox("Enter Play Mode to test out the animations.", HelpBoxMessageType.Info));
                spawnAnimationButtons.style.display = DisplayStyle.None;
            }
            else
            {
                spawnAnimationButtons.style.display = DisplayStyle.Flex;
            }

            CreateBaseAnimationButtons(spawnAnimationButtons, baseAnimationCounterField, onEnableBaseAnimationSlider);
            spawnAnimationButtons.schedule.Execute(() => { CreateBaseAnimationButtons(spawnAnimationButtons, baseAnimationCounterField, onEnableBaseAnimationSlider); }).ExecuteLater(10);
            spawnAnimationButtons.schedule.Execute(() => { CreateBaseAnimationButtons(spawnAnimationButtons, baseAnimationCounterField, onEnableBaseAnimationSlider); }).ExecuteLater(5000);
        }

        private void CreateBaseAnimationButtons(GroupBox spawnAnimationButtons, IntegerField baseAnimationCounterField, SliderInt onEnableBaseAnimationSlider)
        {
            int count = baseAnimationCounterField.value;
            onEnableBaseAnimationSlider.highValue = count - 1;

            spawnAnimationButtons.Clear();

            if (!Application.isPlaying)
                return;

            for (int i = 0; i <= count; i++)
            {
                int index = i; //if this is not cached, the event will call the last value
                Button button = new()
                {
                    text = index.ToString()
                };

                button.clicked += delegate
                {
                    animationController.PlaySpawnAnimation(index);
                };
                spawnAnimationButtons.Add(button);
            }
        }

        private void SetupRankIconButtons()
        {
            var iconsContainer = customTabContent.Q<GroupBox>("IconButtonsContainer");
            iconsContainer.Clear();
            iconButtons.Clear();

            Button noneButton = new()
            {
                text = "None"
            };
            noneButton.clicked += delegate
            {
                animationController.SetIconDirectly(0);
                UpdateRankIconButtonStyle();
            };
            iconsContainer.Add(noneButton);
            iconButtons.Add(noneButton);

            for (int i = 1; i < iconsAmount; i++)
            {
                int index = i; //if this is not cached, the event will call the last value
                Button button = new()
                {
                    text = index.ToString()
                };

                button.clicked += delegate
                {
                    animationController.SetIconDirectly(index);
                    UpdateRankIconButtonStyle();
                };
                iconsContainer.Add(button);
                iconButtons.Add(button);
            }
        }

        private void SetupRankIconsMaterialButtons()
        {
            IconsPrimaryMaterial();
            IconsSecondaryMaterial();
        }

        private void IconsPrimaryMaterial()
        {
            iconsPrimaryMaterialsFoldout = customTabContent.Q<GroupBox>("IconsPrimaryMaterialsFoldout");
            customFoldoutSetup.SetupFoldout(iconsPrimaryMaterialsFoldout);
            var container = iconsPrimaryMaterialsFoldout.Q<GroupBox>("MaterialsContainer");
            container.Clear();
            iconPrimaryMaterialButtons.Clear();

            if (animationController.materials == null)
                animationController.materials = new Material[0];

            for (int i = 0; i < animationController.materials.Length; i++)
            {
                if (animationController.materials[i] == null)
                    continue;

                Button button = new()
                {
                    text = animationController.materials[i].name.ToString()
                };

                int index = i; //if this is not cached, the event will call the last value
                button.clicked += delegate
                {
                    animationController.SetIconPrimaryMaterial(index);
                    UpdateIconsPrimaryMaterialButtons();
                };
                container.Add(button);
                iconPrimaryMaterialButtons.Add(button);
            }

            iconsPrimaryMaterialsField = iconsPrimaryMaterialsFoldout.Q<ObjectField>("PrimaryIconsMaterialObjectField");
            iconsPrimaryMaterialsField.SetValueWithoutNotify(animationController.currentRank.iconPrimaryMat);
            iconsPrimaryMaterialsField.RegisterValueChangedCallback(e =>
            {
                if (e.newValue != null)
                    animationController.SetIconPrimaryMaterial(e.newValue as Material);
                else
                    animationController.SetIconPrimaryMaterial(null);
                UpdateIconsPrimaryMaterialButtons();
            });
        }

        private void IconsSecondaryMaterial()
        {
            iconsSecondaryMaterialsFoldout = customTabContent.Q<GroupBox>("IconsSecondaryMaterialsFoldout");
            customFoldoutSetup.SetupFoldout(iconsSecondaryMaterialsFoldout);
            var container = iconsSecondaryMaterialsFoldout.Q<GroupBox>("MaterialsContainer");
            container.Clear();
            iconSecondaryMaterialButtons.Clear();

            if (animationController.materials == null)
                animationController.materials = new Material[0];

            for (int i = 0; i < animationController.materials.Length; i++)
            {
                if (animationController.materials[i] == null)
                    continue;

                Button button = new()
                {
                    text = animationController.materials[i].name.ToString()
                };

                int index = i; //if this is not cached, the event will call the last value
                button.clicked += delegate
                {
                    animationController.SetIconSecondaryMaterial(index);
                    UpdateIconsSecondaryMaterialButtons();
                };
                container.Add(button);
                iconSecondaryMaterialButtons.Add(button);
            }

            iconsSecondaryMaterialsField = iconsSecondaryMaterialsFoldout.Q<ObjectField>("SecondaryIconsMaterialObjectField");
            iconsSecondaryMaterialsField.SetValueWithoutNotify(animationController.currentRank.iconSecondaryMat);
            iconsSecondaryMaterialsField.RegisterValueChangedCallback(e =>
            {
                if (e.newValue != null)
                    animationController.SetIconSecondaryMaterial(e.newValue as Material);
                else
                    animationController.SetIconSecondaryMaterial(null);
                UpdateIconsSecondaryMaterialButtons();
            });
        }

        private void UpdateRankIconButtonStyle()
        {
            if (iconButtons.Count < 2)
                return;

            for (int i = 0; i < iconButtons.Count; i++)
            {
                if (animationController.currentRank.icon == i)
                    iconButtons[i].AddToClassList("selected-button");
                else
                    iconButtons[i].RemoveFromClassList("selected-button");
            }
        }

        private void UpdateIconMaterialButtonStyle()
        {
            UpdateIconsPrimaryMaterialButtons();

            UpdateIconsSecondaryMaterialButtons();
        }

        private void UpdateIconsPrimaryMaterialButtons()
        {
            iconsPrimaryMaterialsField.SetValueWithoutNotify(animationController.currentRank.iconPrimaryMat);
            for (int i = 0; i < iconPrimaryMaterialButtons.Count; i++)
            {
                if (animationController.currentRank.iconPrimaryMat != null && animationController.currentRank.iconPrimaryMat.name.ToString() == iconPrimaryMaterialButtons[i].text)
                    iconPrimaryMaterialButtons[i].AddToClassList("selected-button");
                else
                    iconPrimaryMaterialButtons[i].RemoveFromClassList("selected-button");
            }
        }

        private void UpdateIconsSecondaryMaterialButtons()
        {
            iconsSecondaryMaterialsField.SetValueWithoutNotify(animationController.currentRank.iconSecondaryMat);
            for (int i = 0; i < iconSecondaryMaterialButtons.Count; i++)
            {
                if (animationController.currentRank.iconSecondaryMat != null && animationController.currentRank.iconSecondaryMat.name.ToString() == iconSecondaryMaterialButtons[i].text)
                    iconSecondaryMaterialButtons[i].AddToClassList("selected-button");
                else
                    iconSecondaryMaterialButtons[i].RemoveFromClassList("selected-button");
            }
        }

        private void SetupBodyButtons()
        {
            var borderContainer = customTabContent.Q<GroupBox>("BordersContainer");
            borderContainer.Clear();
            borderButtons.Clear();

            Button noneButton = new()
            {
                text = "None"
            };
            noneButton.clicked += delegate
            {
                animationController.currentRank.ResetWingsAnimationVariables();
                EditorUtility.SetDirty(animationController);

                animationController.currentRank.transitionToSameInside = true;
                EditorUtility.SetDirty(animationController);

                animationController.SetBorder(-1);

                borderMaterialsFoldout.style.display = DisplayStyle.None;
                insidesFoldout.style.display = DisplayStyle.None;
                UpdateBorderButtonStyle();
            };
            borderContainer.Add(noneButton);
            borderButtons.Add(noneButton);

            for (int i = 0; i < animationController.bodies.Length; i++)
            {
                Button button = new();
                if (animationController.bodies[i].border)
                {
                    string text = animationController.bodies[i].border.name;
                    if (text.Contains("Border "))
                        text = text.Replace("Border ", "");
                    button.text = text;
                }
                else
                {
                    button.text = "Invalid border at index: " + i;
                }
                int index = i; //if this is not cached, the event will call the last value

                button.clicked += delegate
                {
                    animationController.currentRank.ResetWingsAnimationVariables();
                    EditorUtility.SetDirty(animationController);

                    animationController.currentRank.transitionToSameInside = true;
                    EditorUtility.SetDirty(animationController);

                    animationController.SetBorder(index);
                    UpdateBorderButtonStyle();
                    UpdateInsideButtons();
                    UpdateInsideButtonStyle();

                    insidesFoldout.style.display = DisplayStyle.Flex;
                    borderMaterialsFoldout.style.display = DisplayStyle.Flex;
                };
                borderContainer.Add(button);
                borderButtons.Add(button);
            }
        }

        private List<string> GetBorderChoices()
        {
            List<string> borders = new();
            borders.Add("None");

            for (int i = 0; i < animationController.bodies.Length; i++)
            {
                string text;
                if (animationController.bodies[i].border)
                {
                    text = animationController.bodies[i].border.name;
                    if (text.Contains("Border "))
                        text = text.Replace("Border ", "");
                }
                else
                {
                    text = "Invalid border at index: " + i;
                }
                borders.Add(text);
            }

            return borders;
        }

        private void UpdateBorderButtonStyle()
        {
            if (borderButtons.Count < 2)
                return;

            if (animationController.currentRank.border < 0)
                borderButtons[0].AddToClassList("selected-button");
            else
                borderButtons[0].RemoveFromClassList("selected-button");

            for (int i = 1; i < borderButtons.Count; i++)
            {
                if (animationController.currentRank.border + 1 == i) //+1 because the none button is added
                    borderButtons[i].AddToClassList("selected-button");
                else
                    borderButtons[i].RemoveFromClassList("selected-button");
            }
        }

        private void UpdateInsideButtons()
        {
            var insideAnimationTestingGroupBox = insidesFoldout.Q<GroupBox>("InsideAnimationTestingGroupBox");
            if (!Application.isPlaying)
                insideAnimationTestingGroupBox.style.display = DisplayStyle.None;

            insideEntryAnimationType = insidesFoldout.Q<EnumField>("InsideEntryAnimationField");
            insideEntryAnimationType.value = animationController.currentRank.newInsideEntryAnimationType;
            insideEntryAnimationType.RegisterValueChangedCallback(e =>
            {
                animationController.currentRank.newInsideEntryAnimationType = (EntryAnimationType)e.newValue;
            });

            insideExitAnimationType = customTabContent.Q<EnumField>("InsideExitAnimationField");
            insideExitAnimationType.value = animationController.currentRank.oldInsideExitAnimationType;
            insideExitAnimationType.RegisterValueChangedCallback(e =>
            {
                animationController.currentRank.oldInsideExitAnimationType = (ExitAnimationType)e.newValue;
            });

            var insideContainer = insidesFoldout.Q<GroupBox>("InsidesContainer");
            insideContainer.Clear();
            insideButtons.Clear();

            Button noneButton = new()
            {
                text = "None"
            };
            noneButton.clicked += delegate
            {
                animationController.DisableInside();
                insideMaterialsFoldout.style.display = DisplayStyle.None;
                UpdateInsideButtonStyle();
            };
            insideContainer.Add(noneButton);
            insideButtons.Add(noneButton);

            if (animationController.currentRank.border < 0 || animationController.currentRank.border >= animationController.bodies.Length)
                return;

            for (int i = 0; i < animationController.bodies[animationController.currentRank.border].inside.Length; i++)
            {
                Button button = new();
                //If the mesh is properly referenced at the index
                if (animationController.bodies[animationController.currentRank.border].inside[i].mesh)
                {
                    string itemName = animationController.bodies[animationController.currentRank.border].inside[i].mesh.name;
                    itemName = itemName.Replace("Border", "");
                    itemName = itemName.Replace("Inside", "");
                    //itemName = itemName.Replace(" ", "");
                    button.text = itemName;
                }
                else
                    button.text = i.ToString();

                int index = i; //if this is not cached, the event will call the last value
                button.clicked += delegate
                {
                    insideMaterialsFoldout.style.display = DisplayStyle.Flex;

                    animationController.currentRank.transitionToSameInside = true;
                    EditorUtility.SetDirty(animationController);

                    animationController.SetInside(index);
                    UpdateInsideButtonStyle();
                };
                insideContainer.Add(button);
                insideButtons.Add(button);
            }
        }

        private List<string> GetInsideChoices(int border)
        {
            List<string> borders = new();
            borders.Add("None");

            if (border >= 0)
            {
                for (int i = 0; i < animationController.bodies[border].inside.Length; i++)
                {
                    if (animationController.bodies[border].inside[i].mesh)
                        borders.Add(GetInsideNameFromMesh(animationController.bodies[border].inside[i].mesh));
                    else
                        borders.Add("Invalid inside at index: " + i);
                }
            }

            return borders;
        }

        private string GetInsideNameFromMesh(Mesh mesh)
        {
            string text = mesh.name;
            text = text.Replace("Border", "");
            text = text.Replace("Inside", "");
            //text = text.Replace(" ", "");
            return text;
        }

        private void UpdateInsideButtonStyle()
        {
            if (insideButtons.Count < 1)
                return;

            if (animationController.currentRank.inside == null || animationController.currentRank.inside.mesh == null)
                insideButtons[0].AddToClassList("selected-button");
            else
                insideButtons[0].RemoveFromClassList("selected-button");

            for (int i = 1; i < insideButtons.Count; i++)
            {
                if (animationController.currentRank.inside != null && animationController.currentRank.inside.mesh == animationController.bodies[animationController.currentRank.border].inside[i - 1].mesh)
                    insideButtons[i].AddToClassList("selected-button");
                else
                    insideButtons[i].RemoveFromClassList("selected-button");
            }
        }

        private void SetupBorderMaterialButtons()
        {
            borderMaterialsFoldout = customTabContent.Q<GroupBox>("BorderMaterialsFoldout");
            customFoldoutSetup.SetupFoldout(borderMaterialsFoldout);
            borderMaterialsFoldout.style.display = animationController.currentRank.border < 0 ? DisplayStyle.None : DisplayStyle.Flex;

            var borderContainer = borderMaterialsFoldout.Q<GroupBox>("MaterialsContainer");
            borderContainer.Clear();
            borderMaterialsButtons.Clear();

            for (int i = 0; i < animationController.materials.Length; i++)
            {
                if (animationController.materials[i] == null)
                    continue;

                Button button = new()
                {
                    //text = (i /*+ 1*/).ToString(),
                    text = animationController.materials[i].name.ToString()
                };
                int index = i; //if this is not cached, the event will call the last value
                button.clicked += delegate
                {
                    animationController.SetBorderMaterials(index);
                    UpdateBorderMaterialButtonsStyle();
                };
                borderContainer.Add(button);
                borderMaterialsButtons.Add(button);
            }

            borderMaterialObjectField = borderMaterialsFoldout.Q<ObjectField>("BorderMaterialObjectField");
            borderMaterialObjectField.SetValueWithoutNotify(animationController.currentRank.borderMat);
            borderMaterialObjectField.RegisterValueChangedCallback(e =>
            {
                if (e.newValue != null)
                    animationController.SetBorderMaterials(e.newValue as Material);
                else
                    animationController.SetBorderMaterials(null);
                UpdateBorderMaterialButtonsStyle();
            });
        }

        private void UpdateBorderMaterialButtonsStyle()
        {
            borderMaterialObjectField.SetValueWithoutNotify(animationController.currentRank.borderMat);
            for (int i = 0; i < borderMaterialsButtons.Count; i++)
            {
                if (animationController.currentRank.borderMat != null && animationController.currentRank.borderMat.name.ToString() == borderMaterialsButtons[i].text)
                    borderMaterialsButtons[i].AddToClassList("selected-button");
                else
                    borderMaterialsButtons[i].RemoveFromClassList("selected-button");
            }
        }

        private void SetupInsideMaterialButtons()
        {
            customFoldoutSetup.SetupFoldout(insideMaterialsFoldout);
            GroupBox insideMaterialsContainer = insideMaterialsFoldout.Q<GroupBox>("MaterialsContainer");
            insideMaterialsContainer.Clear();
            insideMaterialsButtons.Clear();

            if (animationController.materials == null)
                animationController.materials = new Material[0];

            for (int i = 0; i < animationController.materials.Length; i++)
            {
                if (animationController.materials[i] == null)
                    continue;

                Button button = new()
                {
                    //text = (i /*+ 1*/).ToString(),
                    //tooltip = animationController.materials[i].name.ToString()
                    text = animationController.materials[i].name.ToString()
                };

                int index = i; //if this is not cached, the event will call the last value
                button.clicked += delegate
                {
                    animationController.SetInsideMaterial(index);
                    UpdateInsideMaterialButtonsStyle();
                };
                insideMaterialsContainer.Add(button);
                insideMaterialsButtons.Add(button);
            }

            insideMaterialObjectField = insideMaterialsFoldout.Q<ObjectField>("InsideMaterialObjectField");
            insideMaterialObjectField.SetValueWithoutNotify(animationController.currentRank.insideMat);
            insideMaterialObjectField.RegisterValueChangedCallback(e =>
            {
                if (e.newValue != null)
                    animationController.SetInsideMaterial(e.newValue as Material);
                else
                    animationController.SetInsideMaterial(null);
                UpdateInsideMaterialButtonsStyle();
            });
        }

        private void UpdateInsideMaterialButtonsStyle()
        {
            insideMaterialObjectField.SetValueWithoutNotify(animationController.currentRank.insideMat);
            for (int i = 0; i < insideMaterialsButtons.Count; i++)
            {
                if (animationController.currentRank.insideMat != null && animationController.currentRank.insideMat.name.ToString() == insideMaterialsButtons[i].text)
                    insideMaterialsButtons[i].AddToClassList("selected-button");
                else
                    insideMaterialsButtons[i].RemoveFromClassList("selected-button");
            }
        }

        private void SetupWings()
        {
            var wingsContainer = customTabContent.Q<GroupBox>("WingsContainer");
            wingsContainer.Clear();
            wingsButtons.Clear();

            Button noneButton = new()
            {
                text = "None"
            };
            noneButton.clicked += delegate
            {
                wingsMaterialFoldout.style.display = DisplayStyle.None;
                animationController.DisableWings();
                EditorUtility.SetDirty(animationController);
                UpdateWingsButtonStyle();
            }; wingsContainer.Add(noneButton);
            wingsButtons.Add(noneButton);

            for (int i = 0; i < animationController.wings.Length; i++)
            {
                if (animationController.wings[i].holder == null)
                    continue;

                Button button = new();

                if (animationController.wings[i].holder != null)
                {
                    button.text = animationController.wings[i].holder.name;
                }
                else
                {
                    button.text = "Reference Missing";
                    button.tooltip = "In settings/reference, please check wings at index " + i + " has holder";
                }

                int index = i; //if this is not cached, the event will call the last value
                button.clicked += delegate
                {
                    wingsMaterialFoldout.style.display = DisplayStyle.Flex;

                    animationController.currentRank.ResetWingsAnimationVariables();

                    animationController.SetWing(index);
                    animationController.SetWingsMaterial(animationController.currentRank.wingsMat);

                    EditorUtility.SetDirty(animationController);
                    UpdateWingsButtonStyle();
                };
                wingsContainer.Add(button);
                wingsButtons.Add(button);
            }
        }

        private List<string> GetWingsChoices()
        {
            List<string> wings = new();
            wings.Add("None");

            for (int i = 0; i < animationController.wings.Length; i++)
            {
                if (animationController.wings[i].holder)
                {
                    wings.Add(animationController.wings[i].holder.name);
                }
                else
                {
                    wings.Add("Reference Missing");
                }
            }

            return wings;
        }

        private void UpdateWingsButtonStyle()
        {
            if (wingsButtons.Count < 1)
                return;

            if (animationController.currentRank.wings < 0)
            {
                wingsButtons[0].AddToClassList("selected-button");
                wingsMaterialFoldout.style.display = DisplayStyle.None;
            }
            else
            {
                wingsButtons[0].RemoveFromClassList("selected-button");
                wingsMaterialFoldout.style.display = DisplayStyle.Flex;
            }

            for (int i = 1; i < wingsButtons.Count; i++)
            {
                if (animationController.currentRank.wings + 1 == i) //+1 because the none button is added
                    wingsButtons[i].AddToClassList("selected-button");
                else
                    wingsButtons[i].RemoveFromClassList("selected-button");
            }
        }

        private void SetupWingsMaterialButtons()
        {
            wingsMaterialFoldout = customTabContent.Q<GroupBox>("WingsMaterialFoldout");
            customFoldoutSetup.SetupFoldout(wingsMaterialFoldout);
            if (animationController.currentRank.wings < 0)
                wingsMaterialFoldout.style.display = DisplayStyle.None;
            else
                wingsMaterialFoldout.style.display = DisplayStyle.Flex;

            var container = wingsMaterialFoldout.Q<GroupBox>("MaterialsContainer");
            container.Clear();
            wingsMaterialsButtons.Clear();

            if (animationController.materials == null)
                animationController.materials = new Material[0];

            for (int i = 0; i < animationController.materials.Length; i++)
            {
                if (animationController.materials[i] == null)
                    continue;

                Button button = new()
                {
                    text = animationController.materials[i].name.ToString()
                };

                int index = i; //if this is not cached, the event will call the last value
                button.clicked += delegate
                {
                    animationController.SetWingsMaterial(index);
                    UpdateWingsMaterialButtonsStyle();
                };
                container.Add(button);
                wingsMaterialsButtons.Add(button);
            }

            wingsMaterialObjectField = wingsMaterialFoldout.Q<ObjectField>("WingsMaterialObjectField");
            wingsMaterialObjectField.SetValueWithoutNotify(animationController.currentRank.wingsMat);
            wingsMaterialObjectField.RegisterValueChangedCallback(e =>
            {
                if (e.newValue != null)
                    animationController.SetWingsMaterial(e.newValue as Material);
                else
                    animationController.SetWingsMaterial(null);
                UpdateWingsMaterialButtonsStyle();
            });
        }

        private void UpdateWingsMaterialButtonsStyle()
        {
            wingsMaterialObjectField.SetValueWithoutNotify(animationController.currentRank.wingsMat);
            for (int i = 0; i < wingsMaterialsButtons.Count; i++)
            {
                if (animationController.currentRank.wingsMat != null && animationController.currentRank.wingsMat.name.ToString() == wingsMaterialsButtons[i].text)
                    wingsMaterialsButtons[i].AddToClassList("selected-button");
                else
                    wingsMaterialsButtons[i].RemoveFromClassList("selected-button");
            }
        }

        //private void SetupAddons()
        //{
        //    var addonsContainer = customTabContent.Q<GroupBox>("AddonsContainer");
        //    addonsContainer.Clear();
        //    addonsButtons.Clear();

        //    Button noneButton = new Button
        //    {
        //        text = "None"
        //    };
        //    noneButton.clicked += delegate { animationController.DisableAllAddon(); UpdateAddonsButtonStyle(); };
        //    addonsContainer.Add(noneButton);
        //    addonsButtons.Add(noneButton);

        //    for (int i = 0; i < animationController.addons.Length; i++)
        //    {
        //        Button button = new()
        //        {
        //            text = animationController.addons[i].name
        //        };
        //        int index = i; //if this is not cached, the event will call the last value
        //        button.clicked += delegate
        //        {
        //            animationController.ToggleAddon(index);
        //            UpdateAddonsButtonStyle();
        //        };
        //        addonsContainer.Add(button);
        //        addonsButtons.Add(button);
        //    }
        //}

        //private void UpdateAddonsButtonStyle()
        //{
        //    if (addonsButtons.Count < 1)
        //        return;

        //    if (animationController.currentRank.addons.Count == 0)
        //        addonsButtons[0].AddToClassList("selected-button");
        //    else
        //        addonsButtons[0].RemoveFromClassList("selected-button");

        //    for (int i = 1; i < addonsButtons.Count; i++)
        //    {
        //        if (animationController.currentRank.addons.Contains(animationController.addons[i - 1]))
        //            addonsButtons[i].AddToClassList("selected-button");
        //        else
        //            addonsButtons[i].RemoveFromClassList("selected-button");
        //    }
        //}

        #endregion Customize Tab

        private void SetupSettingsTab()
        {
            //settingsContent.Q<Button>("OpenDocumentation_BaseAnimationSettings").clicked += () =>
            //{
            //    Application.OpenURL(url_doc_howToCreateBaseAnimations);
            //};
        }

        private void SetupReferencesTab()
        {
        }

        private void SetupDocs()
        {
            customTabContent.Q<Button>("CustomizeTab_OpenDocumentation_PrimaryMaterials").clicked += () =>
            {
                Application.OpenURL(url_doc_howToModifyMaterialShortcuts);
            };

            customTabContent.Q<Button>("CustomizeTab_OpenDocumentation_SecondaryMaterials").clicked += () =>
              {
                  Application.OpenURL(url_doc_howToModifyMaterialShortcuts);
              };
            customTabContent.Q<Button>("CustomizeTab_OpenDocumentation_BorderMaterials").clicked += () =>
              {
                  Application.OpenURL(url_doc_howToModifyMaterialShortcuts);
              };

            customTabContent.Q<Button>("CustomizeTab_OpenDocumentation_InsideMaterials").clicked += () =>
            {
                Application.OpenURL(url_doc_howToModifyMaterialShortcuts);
            };
            customTabContent.Q<Button>("CustomizeTab_OpenDocumentation_WingsMaterials").clicked += () =>
            {
                Application.OpenURL(url_doc_howToModifyMaterialShortcuts);
            };

            referencesTabContent.Q<Button>("OpenDocumentation_BaseAnimationSettings").clicked += () =>
            {
                Application.OpenURL(url_doc_howToCreateBaseAnimations);
            };
            referencesTabContent.Q<Button>("OpenDocumentation_CreateIconAnimation").clicked += () =>
            {
                Application.OpenURL(url_doc_howToCreateIconAnimations);
            };       
            root.Q<Button>("AssetStoreLink").clicked += () =>
            {
                Application.OpenURL(url_assetStoreLink);
            };
        }

        #region Tests

        //todo: Unique rank name check

        /// <summary>
        /// Tests all the variables that had missing reference during development because of asset change or something else.
        /// </summary>
        private void ValidateData()
        {
            for (int i = 0; i < animationController.bodies.Length; i++)
            {
                var body = animationController.bodies[i];
                for (int j = 0; j < body.inside.Length; j++)
                {
                    var inside = body.inside[j];
                    if (inside.mesh == null)
                        Debug.Log("Inside mesh at index : " + j + " in Body: " + i + " is missing");

                    if (inside.fractured == null)
                        Debug.Log("Inside fractured prefab at index : " + j + " in Body: " + i + " is missing");
                }
            }

            //for (int i = 0; i < animationController.ranks.Count; i++)
            //{
            //    var state = animationController.ranks[i];
            //    if (state.inside.mesh == null)
            //    {
            //        Debug.Log(state.stateName + " lacks a mesh for inside.");
            //    }
            //}
        }

        #endregion Tests
    }
}