using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Editor.CustomEditorTools.FilterAndSort;
using FilterMode = Editor.CustomEditorTools.FilterAndSort.FilterMode;

namespace Editor.CustomEditorTools
{
    public class GameObjectListWindow : EditorWindow
    {
        private readonly GameObjectFilter _filter = new();
        private SortType _sortType = SortType.AlphabeticalAz;
        private readonly GameObjectDataService _dataService = new();

        private List<GameObject> _allObjects = new();
        private List<GameObject> _selectedObjects = new();

        private Vector2 _scrollPosition;
        private bool _showFilters;
        private bool _selectAllToggle;

        [MenuItem("Tools/GameObject Manager")]
        private static void OpenWindow()
        {
            GetWindow<GameObjectListWindow>("GameObject Manager");
        }

        private void OnEnable()
        {
            RefreshList();
        }

        private void OnGUI()
        {
            DrawSearchBar();
            DrawFilterOptions();
            DrawSortingOptions();

            GUILayout.Space(5);
            DrawSelectionToggle();
            GUILayout.Space(5);

            // We could still keep a manual refresh if we like, but it's optional.
            //
            // if (GUILayout.Button("Refresh List", GUILayout.Height(25)))
            // {
            //     RefreshList();
            // }

            GUILayout.Space(5);
            DrawTableHeaders();

            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);
            foreach (var obj in _allObjects)
            {
                if (!obj) continue;
                DrawGameObjectRow(obj);
            }

            GUILayout.EndScrollView();

            GUILayout.Space(10);
            GUI.enabled = _selectedObjects.Count > 0;
            if (GUILayout.Button("Edit Selected", GUILayout.Height(30)))
            {
                GameObjectEditPopup.Open(_selectedObjects);
            }

            GUI.enabled = true;
        }

        private void DrawSearchBar()
        {
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Search:", EditorStyles.boldLabel, GUILayout.Width(50));

            string newSearch = EditorGUILayout.TextField(_filter.SearchQuery);
            if (newSearch != _filter.SearchQuery)
            {
                _filter.SearchQuery = newSearch;
                RefreshList();
            }

            GUILayout.EndHorizontal();
        }

        private void DrawFilterOptions()
        {
            _showFilters = EditorGUILayout.Foldout(_showFilters, "Filters", true);
            if (!_showFilters) return;

            // ---- Store old values ----
            FilterMode oldMeshFilter = _filter.MeshRendererFilter;
            FilterMode oldCollFilter = _filter.ColliderFilter;
            FilterMode oldRbFilter = _filter.RigidbodyFilter;
            bool oldShowInactive = _filter.ShowInactive;

            GUILayout.BeginVertical("box");
            GUILayout.BeginHorizontal();

            _filter.MeshRendererFilter = DrawFilterButton(_filter.MeshRendererFilter, "MeshRenderer");
            _filter.ColliderFilter = DrawFilterButton(_filter.ColliderFilter, "Collider");
            _filter.RigidbodyFilter = DrawFilterButton(_filter.RigidbodyFilter, "Rigidbody");

            bool newShowInactive = EditorGUILayout.ToggleLeft("Show Inactive", _filter.ShowInactive);
            _filter.ShowInactive = newShowInactive;

            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            if (
                _filter.MeshRendererFilter != oldMeshFilter ||
                _filter.ColliderFilter != oldCollFilter ||
                _filter.RigidbodyFilter != oldRbFilter ||
                _filter.ShowInactive != oldShowInactive
            )
            {
                RefreshList();
            }
        }

        private void DrawSortingOptions()
        {
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Sort By:", EditorStyles.boldLabel, GUILayout.Width(50));

            SortType newSort = (SortType)EditorGUILayout.EnumPopup(_sortType, GUILayout.Width(150));
            if (newSort != _sortType)
            {
                _sortType = newSort;
                RefreshList();
            }

            GUILayout.EndHorizontal();
        }

        private void DrawSelectionToggle()
        {
            bool newToggle = EditorGUILayout.ToggleLeft("Select All", _selectAllToggle);
            if (newToggle != _selectAllToggle)
            {
                _selectAllToggle = newToggle;
                _selectedObjects = _selectAllToggle ? new List<GameObject>(_allObjects) : new List<GameObject>();
            }
        }

        private void DrawTableHeaders()
        {
            GUILayout.BeginHorizontal("box", GUILayout.Height(20));
            GUILayout.Space(10);
            GUILayout.Label("Select", EditorStyles.boldLabel, GUILayout.Width(50));
            GUILayout.Space(85);
            GUILayout.Label("Name", EditorStyles.boldLabel, GUILayout.Width(75));
            GUILayout.Space(90);
            GUILayout.Label("Active", EditorStyles.boldLabel, GUILayout.Width(50));
            GUILayout.Space(30);
            GUILayout.Label("Tag", EditorStyles.boldLabel, GUILayout.Width(70));
            GUILayout.Space(30);
            GUILayout.Label("Layer", EditorStyles.boldLabel, GUILayout.Width(70));
            GUILayout.EndHorizontal();
        }

        private void DrawGameObjectRow(GameObject obj)
        {
            GUILayout.BeginHorizontal("box", GUILayout.Height(18));

            // -- Selection Toggle --
            GUILayout.Space(15);
            bool isSelected = _selectedObjects.Contains(obj);
            bool newSelect = EditorGUILayout.Toggle(isSelected, GUILayout.Width(50));
            if (newSelect != isSelected)
            {
                if (newSelect) _selectedObjects.Add(obj);
                else _selectedObjects.Remove(obj);
            }

            // -- Object --
            EditorGUILayout.ObjectField(obj, typeof(GameObject), true, GUILayout.Width(250));
            GUILayout.Space(20);


            // -- Active Toggle --
            bool newActive = EditorGUILayout.Toggle(obj.activeSelf, GUILayout.Width(30));
            if (newActive != obj.activeSelf)
            {
                Undo.RecordObject(obj, "Toggle Active State");
                obj.SetActive(newActive);
                EditorUtility.SetDirty(obj);
            }

            // -- Tag Dropdown --
            string newTag = EditorGUILayout.TagField(obj.tag, GUILayout.Width(110));
            if (!obj.CompareTag(newTag))
            {
                Undo.RecordObject(obj, "Change Tag");
                obj.tag = newTag;
                EditorUtility.SetDirty(obj);
            }

            // -- Layer Dropdown --
            int newLayer = EditorGUILayout.LayerField(obj.layer, GUILayout.Width(90));
            if (newLayer != obj.layer)
            {
                Undo.RecordObject(obj, "Change Layer");
                obj.layer = newLayer;
                EditorUtility.SetDirty(obj);
            }

            GUILayout.EndHorizontal();
        }

        private void RefreshList()
        {
            _allObjects = _dataService.GetFilteredAndSortedGameObjects(_filter, _sortType);
            _selectedObjects = new List<GameObject>(_selectedObjects.Intersect(_allObjects));
        }

        /// <summary>
        /// Draws a cycle button for the filter mode
        /// </summary>
        private FilterMode DrawFilterButton(FilterMode filterMode, string label)
        {
            GUIStyle style = new GUIStyle(GUI.skin.button);
            switch (filterMode)
            {
                case FilterMode.NoFilter:
                    style.normal.textColor = Color.gray;
                    break;
                case FilterMode.HasComponent:
                    style.normal.textColor = Color.green;
                    break;
                case FilterMode.DoesNotHaveComponent:
                    style.normal.textColor = Color.red;
                    break;
            }

            // Cycle through the 3 states on click
            if (GUILayout.Button(label, style))
            {
                filterMode = (FilterMode)(((int)filterMode + 1) % 3);
            }

            return filterMode;
        }
    }
}