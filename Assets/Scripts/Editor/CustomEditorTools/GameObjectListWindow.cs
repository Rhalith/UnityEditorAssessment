using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Editor.CustomEditorTools
{
    /// <summary>
    /// An Editor window that displays a list of all scene GameObjects (root + children).
    /// Allows filtering, sorting, selecting, and editing in bulk.
    /// </summary>
    public class GameObjectListWindow : EditorWindow
    {
        // --- Filter/Sorting/Service ---
        private GameObjectFilter _filter = new GameObjectFilter();
        private SortType _sortType = SortType.AlphabeticalAz;
        private GameObjectDataService _dataService = new GameObjectDataService();

        // --- State ---
        private List<GameObject> _allObjects = new();
        private List<GameObject> _selectedObjects = new();

        // --- UI ---
        private Vector2 _scrollPosition;
        private bool _showFilters = false;
        private bool _selectAllToggle = false;

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

            if (GUILayout.Button("Refresh List", GUILayout.Height(25)))
            {
                RefreshList();
            }

            GUILayout.Space(5);
            DrawTableHeaders();

            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);
            foreach (var obj in _allObjects)
            {
                if (obj == null) continue;
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
            if (_showFilters)
            {
                GUILayout.BeginVertical("box");
                GUILayout.BeginHorizontal();

                DrawFilterButton(ref _filter.MeshRendererFilter, "MeshRenderer");
                DrawFilterButton(ref _filter.ColliderFilter, "Collider");
                DrawFilterButton(ref _filter.RigidbodyFilter, "Rigidbody");

                bool newShowInactive = EditorGUILayout.ToggleLeft("Show Inactive", _filter.ShowInactive);
                if (newShowInactive != _filter.ShowInactive)
                {
                    _filter.ShowInactive = newShowInactive;
                    RefreshList();
                }

                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
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
                _selectedObjects = _selectAllToggle 
                    ? new List<GameObject>(_allObjects) 
                    : new List<GameObject>();
            }
        }

        private void DrawTableHeaders()
        {
            GUILayout.BeginHorizontal("box", GUILayout.Height(20));
            GUILayout.Label("Select", EditorStyles.boldLabel, GUILayout.Width(50));
            GUILayout.Label("Name", EditorStyles.boldLabel, GUILayout.Width(150));
            GUILayout.Label("Active", EditorStyles.boldLabel, GUILayout.Width(50));
            GUILayout.Label("Tag", EditorStyles.boldLabel, GUILayout.Width(90));
            GUILayout.Label("Layer", EditorStyles.boldLabel, GUILayout.Width(90));
            GUILayout.EndHorizontal();
        }

        private void DrawGameObjectRow(GameObject obj)
        {
            GUILayout.BeginHorizontal("box", GUILayout.Height(18));

            // -- Selection Toggle --
            bool isSelected = _selectedObjects.Contains(obj);
            bool newSelect  = EditorGUILayout.Toggle(isSelected, GUILayout.Width(50));
            if (newSelect != isSelected)
            {
                if (newSelect) _selectedObjects.Add(obj);
                else _selectedObjects.Remove(obj);
            }

            // -- Name --
            GUIContent nameContent = new GUIContent(obj.name, obj.name);
            GUILayout.Label(nameContent, GUILayout.Width(150));

            // -- Active Toggle --
            bool newActive = EditorGUILayout.Toggle(obj.activeSelf, GUILayout.Width(50));
            if (newActive != obj.activeSelf)
            {
                Undo.RecordObject(obj, "Toggle Active State");
                obj.SetActive(newActive);
                EditorUtility.SetDirty(obj);
            }

            // -- Tag Dropdown --
            string newTag = EditorGUILayout.TagField(obj.tag, GUILayout.Width(90));
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
            // Keep only those still valid after refresh
            _selectedObjects = new List<GameObject>(_selectedObjects.Intersect(_allObjects));
        }

        private void DrawFilterButton(ref FilterMode filterMode, string label)
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

            if (GUILayout.Button(label, style))
            {
                filterMode = (FilterMode)(((int)filterMode + 1) % 3);
                RefreshList();
            }
        }
    }
}
