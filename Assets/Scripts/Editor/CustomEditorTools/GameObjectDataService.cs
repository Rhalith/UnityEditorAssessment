using UnityEngine;
using System.Collections.Generic;

namespace Editor.CustomEditorTools
{
    /// <summary>
    /// Gathers, filters, and sorts GameObjects from the current scene.
    /// </summary>
    public class GameObjectDataService
    {
        /// <summary>
        /// Collects all root GameObjects (and their children), then filters and sorts them
        /// based on the provided filter and sort type.
        /// </summary>
        public List<GameObject> GetFilteredAndSortedGameObjects(GameObjectFilter filter, SortType sortType)
        {
            List<GameObject> allObjects = new List<GameObject>();
            
            GameObject[] sceneObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);

            // Add only root objects, then recursively add children
            foreach (GameObject root in sceneObjects)
            {
                if (root.transform.parent == null)
                    CollectRecursively(root, filter, allObjects);
            }

            // Sort results
            GameObjectSorter.Sort(ref allObjects, sortType);
            return allObjects;
        }

        private void CollectRecursively(GameObject obj, GameObjectFilter filter, List<GameObject> results)
        {
            if (filter.Matches(obj))
                results.Add(obj);

            foreach (Transform child in obj.transform)
            {
                CollectRecursively(child.gameObject, filter, results);
            }
        }
    }
}