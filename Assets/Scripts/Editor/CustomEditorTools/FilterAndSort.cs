using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Editor.CustomEditorTools
{
    /// <summary>
    /// Possible filter modes for checking component presence.
    /// </summary>
    public enum FilterMode { NoFilter, HasComponent, DoesNotHaveComponent }

    /// <summary>
    /// Possible sorting criteria for GameObjects.
    /// </summary>
    public enum SortType { AlphabeticalAz, AlphabeticalZa, ActiveFirst, InactiveFirst, Tag, Layer }

    /// <summary>
    /// Encapsulates all filtering criteria for a GameObject.
    /// </summary>
    public class GameObjectFilter
    {
        public string SearchQuery { get; set; } = "";
        public bool ShowInactive { get; set; } = true;

        public FilterMode MeshRendererFilter = FilterMode.NoFilter;
        public FilterMode ColliderFilter = FilterMode.NoFilter;
        public FilterMode RigidbodyFilter = FilterMode.NoFilter;

        /// <summary>
        /// Determines if a given GameObject matches all of the set filter criteria.
        /// </summary>
        public bool Matches(GameObject obj)
        {
            // Check active state
            if (!ShowInactive && !obj.activeInHierarchy)
                return false;

            // Search by name
            if (!string.IsNullOrEmpty(SearchQuery))
            {
                if (!obj.name.ToLower().Contains(SearchQuery.ToLower()))
                    return false;
            }

            // Check components
            if (!MatchesFilter(obj.GetComponent<MeshRenderer>(), MeshRendererFilter)) return false;
            if (!MatchesFilter(obj.GetComponent<Collider>(), ColliderFilter)) return false;
            if (!MatchesFilter(obj.GetComponent<Rigidbody>(), RigidbodyFilter)) return false;

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
    }

    /// <summary>
    /// Encapsulates sorting options for a list of GameObjects.
    /// </summary>
    public static class GameObjectSorter
    {
        public static void Sort(ref List<GameObject> objects, SortType sortType)
        {
            switch (sortType)
            {
                case SortType.AlphabeticalAz:
                    objects = objects.OrderBy(obj => obj.name).ToList();
                    break;
                case SortType.AlphabeticalZa:
                    objects = objects.OrderByDescending(obj => obj.name).ToList();
                    break;
                case SortType.ActiveFirst:
                    objects = objects.OrderByDescending(obj => obj.activeSelf).ToList();
                    break;
                case SortType.InactiveFirst:
                    objects = objects.OrderBy(obj => obj.activeSelf).ToList();
                    break;
                case SortType.Tag:
                    objects = objects.OrderBy(obj => obj.tag).ToList();
                    break;
                case SortType.Layer:
                    objects = objects.OrderBy(obj => obj.layer).ToList();
                    break;
            }
        }
    }
}
