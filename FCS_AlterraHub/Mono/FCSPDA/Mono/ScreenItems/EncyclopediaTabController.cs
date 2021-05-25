﻿using System.Collections.Generic;
using System.IO;
using FCS_AlterraHub.Configuration;
using FCS_AlterraHub.Registration;
using FCSCommon.Helpers;
using FCSCommon.Utilities;
using SMLHelper.V2.Handlers;
using SMLHelper.V2.Utility;
using UnityEngine;
using UnityEngine.UI;

namespace FCS_AlterraHub.Mono.FCSPDA.Mono.ScreenItems
{
    internal class EncyclopediaTabController : MonoBehaviour, uGUI_IListEntryManager
    {
        private CraftNode _pdaEncyclopediaTree => PDAEncyclopedia.tree;
        private Dictionary<string, PDAEncyclopedia.EntryData> _pdaEntrees => PDAEncyclopedia.mapping;

        internal static CraftNode Tree { get; set; }

        private uGUI_ListEntry selectedEntry
        {
            get
            {
                if (UISelection.selected != null && UISelection.selected.IsValid())
                {
                    return UISelection.selected as uGUI_ListEntry;
                }
                return null;
            }
        }
        private uGUI_ListEntry activeEntry;
        private Text title;
        private Text message;
        private RawImage image;
        public LayoutElement imageLayout;
        [AssertNotNull]
        public Texture2D defaultTexture;
        public float indentStep = 10f;
        private List<uGUI_ListEntry> pool = new List<uGUI_ListEntry>();
        public UISpriteData pathNodeSprites;
        public UISpriteData entryNodeSprites;
        public RectTransform listCanvas;
        public GameObject prefabEntry => Buildables.AlterraHub.EncyclopediaEntryPrefab;
        private object selectedItem;
        public ScrollRect listScrollRect;
        public Sprite iconCollapse;
        public Sprite iconExpand;
        private static readonly EntryComparer entryComparer = new EntryComparer();

        private void Awake()
        {
            title = GameObjectHelpers.FindGameObject(gameObject, "Title").GetComponent<Text>();
            message = GameObjectHelpers.FindGameObject(gameObject, "Description").GetComponent<Text>();
            var banner = GameObjectHelpers.FindGameObject(gameObject, "Banner");
            image = banner.GetComponent<RawImage>();
            imageLayout = banner.GetComponent<LayoutElement>();
            var encycList = GameObjectHelpers.FindGameObject(gameObject, "EncyclopediaList");
            listScrollRect = encycList.GetComponent<ScrollRect>();
            iconCollapse = Buildables.AlterraHub.UpArrow;
            iconExpand = Buildables.AlterraHub.DownArrow;
            listCanvas = encycList.FindChild("Viewport").FindChild("Content").GetComponent<RectTransform>();

            FCSAlterraHubService.PublicAPI.RegisterEncyclopediaEntries(QPatch.EncyclopediaConfig.EncyclopediaEntries);
        }

        internal void Refresh()
        {
            Expand(Tree);
        }

        public bool OnButtonDown(string key, GameInput.Button button)
        {
            CraftNode craftNode = Tree.FindNodeById(key, false) as CraftNode;
            if (craftNode == null)
            {
                return false;
            }
            GetNodeListEntry(craftNode);
            TreeAction action = craftNode.action;
            if (button != GameInput.Button.LeftHand)
            {
                switch (button)
                {
                    case GameInput.Button.UISubmit:
                        goto IL_4C;
                    case GameInput.Button.UILeft:
                        if (action == TreeAction.Expand)
                        {
                            if (GetNodeExpanded(craftNode))
                            {
                                ToggleExpand(craftNode);
                            }
                            else
                            {
                                CraftNode craftNode2 = craftNode.parent as CraftNode;
                                if (craftNode2 != null && craftNode2.action == TreeAction.Expand)
                                {
                                    SelectItem(GetNodeListEntry(craftNode2));
                                }
                            }
                        }
                        else if (action == TreeAction.Craft)
                        {
                            CraftNode craftNode3 = craftNode.parent as CraftNode;
                            if (craftNode3 != null && craftNode3.action == TreeAction.Expand)
                            {
                                SelectItem(GetNodeListEntry(craftNode3));
                            }
                        }
                        return true;
                    case GameInput.Button.UIRight:
                        if (action == TreeAction.Expand)
                        {
                            if (GetNodeExpanded(craftNode))
                            {
                                using (IEnumerator<CraftNode> enumerator = craftNode.GetEnumerator())
                                {
                                    if (enumerator.MoveNext())
                                    {
                                        CraftNode node = enumerator.Current;
                                        SelectItem(GetNodeListEntry(node));
                                    }
                                    return true;
                                }
                            }
                            ToggleExpand(craftNode);
                        }
                        else if (action == TreeAction.Craft)
                        {
                            Activate(craftNode);
                        }
                        return true;
                }
                return false;
            }
            IL_4C:
            if (action == TreeAction.Expand)
            {
                ToggleExpand(craftNode);
            }
            else if (action == TreeAction.Craft)
            {
                Activate(craftNode);
            }
            return true;
        }

        public void SelectItem(object item)
        {
            selectedItem = item;
            DeselectItem();
            uGUI_ListEntry uGUI_ListEntry = item as uGUI_ListEntry;
            if (uGUI_ListEntry != null)
            {
                UISelection.selected = uGUI_ListEntry;
                uGUI_ListEntry.OnPointerEnter(null);
                listScrollRect.ScrollTo(uGUI_ListEntry.rectTransform, true, false, new Vector4(10f, 10f, 10f, 10f));
                return;
            }
            if (selectedItem is Selectable)
            {
                (selectedItem as Selectable).Select();
                UISelection.selected = null;
            }
        }

        public void DeselectItem()
        {
            if (selectedEntry == null)
            {
                return;
            }
            selectedEntry.OnPointerExit(null);
            UISelection.selected = null;
        }

        private void ToggleExpand(CraftNode node)
        {
            if (GetNodeExpanded(node))
            {
                Collapse(node);
                return;
            }
            Expand(node);
            UpdatePositions();
        }

        private void UpdatePositions()
        {
            using (IEnumerator<CraftNode> enumerator = Tree.Traverse(false))
            {
                int num = 0;
                while (enumerator.MoveNext())
                {
                    CraftNode node = enumerator.Current;
                    uGUI_ListEntry nodeListEntry = GetNodeListEntry(node);
                    if (nodeListEntry != null)
                    {
                        nodeListEntry.rectTransform.SetSiblingIndex(num);
                    }
                    num++;
                }
            }
        }

        private void Expand(CraftNode node)
        {
            if (node == null)
            {
                QuickLogger.Error("node is null",true);
            }

            SetNodeExpanded(node, true);

            uGUI_ListEntry nodeListEntry = GetNodeListEntry(node);

            if (nodeListEntry != null)
            {
                nodeListEntry.SetIcon(iconCollapse);
            }

            CraftNode node2 = PDAEncyclopedia.GetNode(node.id);

            if (node2 == null)
            {
                return;
            }

            QuickLogger.Debug($"[Node] Id: {node?.id} | S0{node?.string0} | S1 {node?.string1} | bool {node?.bool0} | Mono {node?.monoBehaviour0}");
            QuickLogger.Debug($"[Node2] Id: {node2?.id} | S0{node2?.string0} | S1 {node2?.string1} | bool {node2?.bool0} | Mono {node2?.monoBehaviour0}");

            foreach (CraftNode craftNode in node2)
            {
                CraftNode craftNode2 = node[craftNode.id] as CraftNode;

                QuickLogger.Debug($"[CraftNode2] Id: {craftNode2?.id} | S0{craftNode2?.string0} | S1 {craftNode2?.string1} | bool {craftNode2?.bool0} | Mono {craftNode2?.monoBehaviour0}");
                
                if (craftNode2 == null)
                {
                    CreateNode(craftNode, node);
                }
                else
                {
                    GetNodeListEntry(craftNode2).gameObject.SetActive(true);

                    if (GetNodeExpanded(craftNode2))
                    {
                        Expand(craftNode2);
                    }
                }
            }

            node.Sort(entryComparer);
        }

        private CraftNode ExpandTo(string id)
        {
            if (string.IsNullOrEmpty(id) || Tree == null)
            {
                return null;
            }
            CraftNode result = null;
            List<TreeNode> list = new List<TreeNode>();
            if (Tree.FindNodeById(id, list, false))
            {
                CraftNode craftNode = Tree;
                for (int i = list.Count - 2; i >= 0; i--)
                {
                    CraftNode craftNode2 = list[i] as CraftNode;
                    string id2 = craftNode2.id;
                    CraftNode craftNode3 = craftNode[id2] as CraftNode;
                    if (craftNode3 == null)
                    {
                        craftNode3 = this.CreateNode(craftNode2, craftNode);
                        if (craftNode3 != null)
                        {
                            craftNode.Sort(entryComparer);
                        }
                    }
                    if (craftNode3.action == TreeAction.Expand && !GetNodeExpanded(craftNode3))
                    {
                        this.Expand(craftNode3);
                    }
                    if (i == 0)
                    {
                        result = craftNode3;
                    }
                    craftNode = craftNode3;
                }
                this.UpdatePositions();
            }
            return result;
        }

        public void OnAddEntry(CraftNode srcNode, bool verbose)
        {
            QuickLogger.Debug($"OnAddEntry {srcNode.string0} {srcNode.string1}");
            List<TreeNode> reversedPath = srcNode.GetReversedPath(true);
            CraftNode craftNode = Tree;
            bool flag = false;
            int num = reversedPath.Count - 1;
            while (num >= 0 && craftNode != null && GetNodeExpanded(craftNode))
            {
                srcNode = (reversedPath[num] as CraftNode);
                string id = srcNode.id;
                CraftNode craftNode2 = craftNode[id] as CraftNode;
                if (craftNode2 == null)
                {
                    craftNode2 = this.CreateNode(srcNode, craftNode);
                    if (craftNode2 != null)
                    {
                        craftNode.Sort(entryComparer);
                        flag = true;
                    }
                }
                craftNode = craftNode2;
                num--;
            }
            if (flag)
            {
                this.UpdatePositions();
            }
        }

        private static bool GetNodeExpanded(CraftNode node)
        {
            return node.bool0;
        }

        private void Activate(CraftNode node)
        {
            if (node == null || node.action != TreeAction.Craft)
            {
                return;
            }
            uGUI_ListEntry nodeListEntry = GetNodeListEntry(node);
            if (activeEntry != nodeListEntry)
            {
                if (activeEntry != null)
                {
                    activeEntry.SetSelected(false);
                }
                activeEntry = nodeListEntry;
                if (activeEntry != null)
                {
                    DisplayEntry(node.id);
                    activeEntry.SetSelected(true);
                }
            }
        }

        private static void SetNodeExpanded(CraftNode node, bool state)
        {
            node.bool0 = state;
        }

        private static uGUI_ListEntry GetNodeListEntry(CraftNode node)
        {
            return node.monoBehaviour0 as uGUI_ListEntry;
        }

        private void Collapse(CraftNode node)
        {
            SetNodeExpanded(node, false);
            uGUI_ListEntry nodeListEntry = GetNodeListEntry(node);
            if (nodeListEntry != null)
            {
                nodeListEntry.SetIcon(iconExpand);
            }
            using (IEnumerator<CraftNode> enumerator = node.Traverse(false))
            {
                while (enumerator.MoveNext())
                {
                    CraftNode node2 = enumerator.Current;
                    uGUI_ListEntry nodeListEntry2 = GetNodeListEntry(node2);
                    if (nodeListEntry2 != null)
                    {
                        nodeListEntry2.gameObject.SetActive(false);
                    }
                }
            }
        }

        private void DisplayEntry(string key)
        {
            if (key != null && PDAEncyclopedia.GetEntryData(key, out var entryData))
            {
                SetTitle(Language.main.Get("Ency_" + key));
                SetText(Language.main.Get("EncyDesc_" + key));
                SetImage(entryData.image);
                SetAudio(entryData.audio);
                return;
            }

            SetTitle(string.Empty);
            SetText(string.Empty);
            SetImage(null);
            SetAudio(null);
        }

        private void SetTitle(string value)
        {
            title.text = value;
        }

        private void SetText(string value)
        {
            message.text = value;
        }

        private void SetImage(Texture2D texture)
        {
            image.texture = texture;
            if (texture != null)
            {
                float num = (float)texture.height / (float)texture.width;
                float num2 = image.rectTransform.rect.width * num;
                imageLayout.minHeight = num2;
                imageLayout.preferredHeight = num2;
                image.gameObject.SetActive(true);
                return;
            }
            image.gameObject.SetActive(false);
        }

        private void SetAudio(FMODAsset asset)
        {
            //this.SetAudioState(false, 0f, 0f);
            //if (asset != null && !string.IsNullOrEmpty(asset.id))
            //{
            //    this.audio = asset.id;
            //    this.audioContainer.SetActive(true);
            //    return;
            //}
            //this.audio = null;
            //this.audioContainer.SetActive(false);
        }

        private CraftNode CreateNode(CraftNode srcNode, CraftNode parentNode)
        {
            if (srcNode == null || parentNode == null)
            {
                QuickLogger.Debug($"Create Node SourceNode {srcNode} | ParentNode {parentNode}",true);
                return null;
            }
            string id = srcNode.id;

            if (parentNode[id] != null)
            {
                QuickLogger.Debug($"Create Node SourceNode ParentNode {parentNode[id]} not null", true);
                return null;
            }

            TreeAction action = srcNode.action;
            int depth = parentNode.depth;
            
            UISpriteData spriteData;
            Sprite icon;
            float indent;
            if (action == TreeAction.Expand)
            {
                spriteData = pathNodeSprites;
                icon = iconExpand;
                indent = (float)(depth + 1) * indentStep;
            }
            else
            {
                if (action != TreeAction.Craft)
                {
                    return null;
                }
                spriteData = entryNodeSprites;
                icon = null;
                indent = (float)depth * indentStep;
            }
            string @string = srcNode.string0;
            string string2 = srcNode.string1;

            uGUI_ListEntry entry = GetEntry();
            entry.Initialize(this, id, spriteData);
            entry.SetIcon(icon);
            entry.SetIndent(indent);
            entry.SetText(string2);
            
            int num = 0;
            
            CraftNode craftNode = new CraftNode(id, action)
            {
                string0 = @string,
                string1 = string2,
                monoBehaviour0 = entry,
                bool0 = false,
                int0 = num
            };
            parentNode.AddNode(new CraftNode[]
            {
                craftNode
            });
            if (action == TreeAction.Expand)
            {
                using (IEnumerator<CraftNode> enumerator = srcNode.Traverse(false))
                {
                    while (enumerator.MoveNext())
                    {
                        CraftNode craftNode2 = enumerator.Current;
                        if (craftNode2.action == TreeAction.Craft && NotificationManager.main.Contains(NotificationManager.Group.Encyclopedia, craftNode2.id))
                        {
                            num++;
                        }
                    }
                }
            }
            else if (action == TreeAction.Craft)
            {
                NotificationManager.main.RegisterTarget(NotificationManager.Group.Encyclopedia, srcNode.id, entry);
            }
            return craftNode;
        }

        private uGUI_ListEntry GetEntry()
        {
            uGUI_ListEntry uGUI_ListEntry;
            if (pool.Count == 0)
            {
                for (int i = 0; i < 4; i++)
                {
                    uGUI_ListEntry = Instantiate<GameObject>(prefabEntry).GetComponent<uGUI_ListEntry>();
                    uGUI_ListEntry.rectTransform.SetParent(listCanvas, false);
                    uGUI_ListEntry.Uninitialize();
                    pool.Add(uGUI_ListEntry);
                }
            }

            int index = pool.Count - 1;
            uGUI_ListEntry = pool[index];
            pool.RemoveAt(index);
            return uGUI_ListEntry;
        }

        private class EntryComparer : IComparer<TreeNode>
        {

            public int Compare(TreeNode node1, TreeNode node2)
            {
                CraftNode craftNode = node1 as CraftNode;
                CraftNode craftNode2 = node2 as CraftNode;
                string strA = (craftNode != null) ? craftNode.string1 : null;
                string strB = (craftNode2 != null) ? craftNode2.string1 : null;
                return string.Compare(strA, strB);
            }
        }
    }
}
