#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using R3;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Editor.R3
{
    public class ObservableTrackerViewItem : TreeViewItem
    {
        static Regex removeHref = new Regex("<a href.+>(.+)</a>", RegexOptions.Compiled);

        public string Type { get; set; }
        public string Elapsed { get; set; }

        string location;
        public string Location
        {
            get { return location; }
            set
            {
                location = value;
                LocationFirstLine = GetFirstLine(location);
            }
        }

        public string LocationFirstLine { get; private set; }

        static string GetFirstLine(string str)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < str.Length; i++)
            {
                if (str[i] == '\r' || str[i] == '\n')
                {
                    break;
                }
                sb.Append(str[i]);
            }

            return removeHref.Replace(sb.ToString(), "$1");
        }

        public ObservableTrackerViewItem(int id) : base(id)
        {

        }
    }

    public class ObservableTrackerTreeView : TreeView
    {
        const string sortedColumnIndexStateKey = "ObservableTrackerTreeView_sortedColumnIndex";

        public IReadOnlyList<TreeViewItem> CurrentBindingItems;

        public ObservableTrackerTreeView()
            : this(new TreeViewState(), new MultiColumnHeader(new MultiColumnHeaderState(new[]
            {
                new MultiColumnHeaderState.Column() { headerContent = new GUIContent("Type"), width = 20},
                new MultiColumnHeaderState.Column() { headerContent = new GUIContent("Elapsed"), width = 10},
                new MultiColumnHeaderState.Column() { headerContent = new GUIContent("Location")},
            })))
        {
        }

        ObservableTrackerTreeView(TreeViewState state, MultiColumnHeader header)
            : base(state, header)
        {
            rowHeight = 20;
            showAlternatingRowBackgrounds = true;
            showBorder = true;
            header.sortingChanged += Header_sortingChanged;

            header.ResizeToFit();
            Reload();

            header.sortedColumnIndex = SessionState.GetInt(sortedColumnIndexStateKey, 1);
        }

        public void ReloadAndSort()
        {
            List<int> currentSelected = this.state.selectedIDs;
            Reload();
            Header_sortingChanged(this.multiColumnHeader);
            this.state.selectedIDs = currentSelected;
        }

        private void Header_sortingChanged(MultiColumnHeader multiColumnHeader)
        {
            SessionState.SetInt(sortedColumnIndexStateKey, multiColumnHeader.sortedColumnIndex);
            int index = multiColumnHeader.sortedColumnIndex;
            bool ascending = multiColumnHeader.IsSortedAscending(multiColumnHeader.sortedColumnIndex);

            IEnumerable<ObservableTrackerViewItem> items = rootItem.children.Cast<ObservableTrackerViewItem>();

            IOrderedEnumerable<ObservableTrackerViewItem> orderedEnumerable;
            switch (index)
            {
                case 0:
                    orderedEnumerable = ascending ? items.OrderBy(item => item.Type) : items.OrderByDescending(item => item.Type);
                    break;
                case 1:
                    orderedEnumerable = ascending ? items.OrderBy(item => double.Parse(item.Elapsed)) : items.OrderByDescending(item => double.Parse(item.Elapsed));
                    break;
                case 2:
                    orderedEnumerable = ascending ? items.OrderBy(item => item.Location) : items.OrderByDescending(item => item.Location);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(index), index, null);
            }

            CurrentBindingItems = rootItem.children = orderedEnumerable.Cast<TreeViewItem>().ToList();
            BuildRows(rootItem);
        }

        protected override TreeViewItem BuildRoot()
        {
            TreeViewItem root = new TreeViewItem { depth = -1 };

            List<TreeViewItem> children = new List<TreeViewItem>();

            DateTime now = DateTime.Now; // tracking state is using local Now.
            ObservableTracker.ForEachActiveTask(state =>
            {
                children.Add(new ObservableTrackerViewItem(state.TrackingId) { Type = state.FormattedType, Elapsed = (now - state.AddTime).TotalSeconds.ToString("00.00"), Location = state.StackTrace });
            });

            CurrentBindingItems = children;
            root.children = CurrentBindingItems as List<TreeViewItem>;
            return root;
        }

        protected override bool CanMultiSelect(TreeViewItem item)
        {
            return false;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            ObservableTrackerViewItem item = args.item as ObservableTrackerViewItem;

            for (int visibleColumnIndex = 0; visibleColumnIndex < args.GetNumVisibleColumns(); visibleColumnIndex++)
            {
                Rect rect = args.GetCellRect(visibleColumnIndex);
                int columnIndex = args.GetColumn(visibleColumnIndex);

                GUIStyle labelStyle = args.selected ? EditorStyles.whiteLabel : EditorStyles.label;
                labelStyle.alignment = TextAnchor.MiddleLeft;
                switch (columnIndex)
                {
                    case 0:
                        EditorGUI.LabelField(rect, item.Type, labelStyle);
                        break;
                    case 1:
                        EditorGUI.LabelField(rect, item.Elapsed, labelStyle);
                        break;
                    case 2:
                        EditorGUI.LabelField(rect, item.LocationFirstLine, labelStyle);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(columnIndex), columnIndex, null);
                }
            }
        }
    }

}

