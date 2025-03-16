using UnityEngine;

namespace Editor.CustomEditorTools.FilterAndSort
{
    /// <summary>
    /// Encapsulates all filtering criteria for a GameObject.
    /// </summary>
    public class GameObjectFilter
    {
        public string SearchQuery = "";
        public bool ShowInactive = true;

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
                FilterMode.HasComponent         => (component != null),
                FilterMode.DoesNotHaveComponent => (component == null),
                _ => true
            };
        }

    }
}