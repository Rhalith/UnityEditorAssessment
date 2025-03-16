using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Editor.CustomEditorTools.FilterAndSort
{
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