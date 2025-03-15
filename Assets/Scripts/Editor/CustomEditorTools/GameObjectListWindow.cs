using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Editor.CustomEditorTools
{
    public class GameObjectListWindow : EditorWindow
    {
        private List<GameObject> _allObjects = new();
        private List<GameObject> _selectedObjects = new();
        private Vector2 _scrollPosition;
        private string _searchQuery = "";
        private bool _showInactiveObjects = true;
        private bool _showFilters;
        private bool _selectAllToggle;

        private enum FilterMode { NoFilter, HasComponent, DoesNotHaveComponent }
        private FilterMode _filterMeshRenderer = FilterMode.NoFilter;
        private FilterMode _filterCollider = FilterMode.NoFilter;
        private FilterMode _filterRigidbody = FilterMode.NoFilter;

        private enum SortType { AlphabeticalAz, AlphabeticalZa, ActiveFirst, InactiveFirst, Tag, Layer }
        private SortType _currentSort = SortType.AlphabeticalAz;

        [MenuItem("Tools/GameObject Manager")]
        private static void OpenWindow()
        {
            GetWindow<GameObjectListWindow>("GameObject Manager");
        }

        private void OnEnable()
        {
            RefreshGameObjectList();
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
                RefreshGameObjectList();
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
            string newSearchQuery = EditorGUILayout.TextField(_searchQuery);
            if (newSearchQuery != _searchQuery)
            {
                _searchQuery = newSearchQuery;
                RefreshGameObjectList();
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

                DrawFilterToggle(ref _filterMeshRenderer, "Mesh Renderer");
                DrawFilterToggle(ref _filterCollider, "Collider");
                DrawFilterToggle(ref _filterRigidbody, "Rigidbody");

                bool newShowInactiveObjects = EditorGUILayout.ToggleLeft("Show Inactive", _showInactiveObjects);
                if (newShowInactiveObjects != _showInactiveObjects)
                {
                    _showInactiveObjects = newShowInactiveObjects;
                    RefreshGameObjectList();
                }

                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
            }
        }

        private void DrawFilterToggle(ref FilterMode filter, string label)
        {
            GUIStyle style = new GUIStyle(GUI.skin.button);
            switch (filter)
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
                filter = (FilterMode)(((int)filter + 1) % 3);
                RefreshGameObjectList();
            }
        }

        private void DrawSortingOptions()
        {
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Sort By:", EditorStyles.boldLabel, GUILayout.Width(50));
            SortType newSort = (SortType)EditorGUILayout.EnumPopup(_currentSort, GUILayout.Width(150));

            if (newSort != _currentSort)
            {
                _currentSort = newSort;
                SortGameObjects();
            }
            GUILayout.EndHorizontal();
        }

        private void DrawSelectionToggle()
        {
            bool newSelectAllToggle = EditorGUILayout.ToggleLeft("Select All", _selectAllToggle);
            if (newSelectAllToggle != _selectAllToggle)
            {
                _selectAllToggle = newSelectAllToggle;
                _selectedObjects = _selectAllToggle ? new List<GameObject>(_allObjects) : new List<GameObject>();
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
            if (!PassesFilter(obj)) return;

            GUILayout.BeginHorizontal("box", GUILayout.Height(18));

            // **Selection Toggle**
            bool isSelected = _selectedObjects.Contains(obj);
            bool newSelectionState = EditorGUILayout.Toggle(isSelected, GUILayout.Width(50));
            if (newSelectionState != isSelected)
            {
                if (newSelectionState)
                    _selectedObjects.Add(obj);
                else
                    _selectedObjects.Remove(obj);
            }

            // **GameObject Name**
            GUIContent nameContent = new GUIContent(obj.name, obj.name);
            GUILayout.Label(nameContent, GUILayout.Width(150));

            // **Active Toggle**
            bool newActiveState = EditorGUILayout.Toggle(obj.activeSelf, GUILayout.Width(50));
            if (newActiveState != obj.activeSelf)
            {
                Undo.RecordObject(obj, "Toggle Active State");
                obj.SetActive(newActiveState);
                EditorUtility.SetDirty(obj);
            }

            // **Tag Dropdown**
            string newTag = EditorGUILayout.TagField(obj.tag, GUILayout.Width(90));
            if (!obj.CompareTag(newTag))
            {
                Undo.RecordObject(obj, "Change Tag");
                obj.tag = newTag;
                EditorUtility.SetDirty(obj);
            }

            // **Layer Dropdown**
            int newLayer = EditorGUILayout.LayerField(obj.layer, GUILayout.Width(90));
            if (newLayer != obj.layer)
            {
                Undo.RecordObject(obj, "Change Layer");
                obj.layer = newLayer;
                EditorUtility.SetDirty(obj);
            }

            GUILayout.EndHorizontal();
        }


        private void RefreshGameObjectList()
        {
            _allObjects.Clear();
            GameObject[] allGameObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);

            foreach (GameObject obj in allGameObjects)
            {
                if (obj.transform.parent == null)
                    CollectGameObjectsRecursively(obj);
            }

            _selectedObjects = _selectedObjects.Where(obj => _allObjects.Contains(obj)).ToList();
            SortGameObjects();
        }

        private void CollectGameObjectsRecursively(GameObject obj)
        {
            if (PassesFilter(obj))
            {
                _allObjects.Add(obj);
            }

            foreach (Transform child in obj.transform)
            {
                CollectGameObjectsRecursively(child.gameObject);
            }
        }

        private bool PassesFilter(GameObject obj)
        {
            if (!_showInactiveObjects && !obj.activeInHierarchy) return false;
            if (!string.IsNullOrEmpty(_searchQuery) && !obj.name.ToLower().Contains(_searchQuery.ToLower())) return false;

            if (!MatchesFilter(obj.GetComponent<MeshRenderer>(), _filterMeshRenderer)) return false;
            if (!MatchesFilter(obj.GetComponent<Collider>(), _filterCollider)) return false;
            if (!MatchesFilter(obj.GetComponent<Rigidbody>(), _filterRigidbody)) return false;

            return true;
        }

        private bool MatchesFilter<T>(T component, FilterMode filter) where T : Component
        {
            return filter switch
            {
                FilterMode.HasComponent => component != null,
                FilterMode.DoesNotHaveComponent => component == null,
                _ => true
            };
        }

        private void SortGameObjects()
        {
            switch (_currentSort)
            {
                case SortType.AlphabeticalAz:
                    _allObjects = _allObjects.OrderBy(obj => obj.name).ToList();
                    break;
                case SortType.AlphabeticalZa:
                    _allObjects = _allObjects.OrderByDescending(obj => obj.name).ToList();
                    break;
                case SortType.ActiveFirst:
                    _allObjects = _allObjects.OrderByDescending(obj => obj.activeSelf).ToList();
                    break;
                case SortType.InactiveFirst:
                    _allObjects = _allObjects.OrderBy(obj => obj.activeSelf).ToList();
                    break;
                case SortType.Tag:
                    _allObjects = _allObjects.OrderBy(obj => obj.tag).ToList();
                    break;
                case SortType.Layer:
                    _allObjects = _allObjects.OrderBy(obj => obj.layer).ToList();
                    break;
            }
        }
    }
}
