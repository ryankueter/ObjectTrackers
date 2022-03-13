/**
* Author: Ryan A. Kueter
* For the full copyright and license information, please view the LICENSE
* file that was distributed with this source code.
*/
using System.Linq;
using System.Text.Json;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace ObjectTrackers
{
    /// <summary>
    /// A class for tracking lists of items
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class TrackList<T>
    {
        /// <summary>
        /// A final list of any changes made to objects in the lists.
        /// </summary>
        public ConcurrentBag<ObjectTrackingChange> TrackedChanges { get; set; }

        /// <summary>
        /// A list of tracked changes.
        /// </summary>
        private readonly List<Track<T>> _trackedList;

        /// <summary>
        /// The original and current execution states, used
        /// for differencing items added or removed.
        /// </summary>
        private readonly List<T> _originalList;
        private List<T> _currentList;

        /// <summary>
        /// The list of property names you want to track.
        /// Leave this list empty if you want to track all properties.
        /// </summary>
        private readonly string[] Inclusions;

        /// <summary>
        /// Tracks a list of datatypes
        /// </summary>
        /// <param name="t">
        /// The list you want to track.
        /// </param>
        /// <param name="inclusions">
        /// A list of the property names you want to track.
        /// If this list is not included, it will make a best
        /// effort to track all property changes.
        /// </param>
        public TrackList(List<T> t, params string[] inclusions)
        {
            if (inclusions is not null)
                Inclusions = inclusions;

            _currentList = t;
            _trackedList = _currentList.Select(item => new Track<T>(item, inclusions)).ToList();
            _originalList = new List<T>(t);
        }

        /// <summary>
        /// Determines if any changes have occurred during execution.
        /// </summary>
        /// <returns>
        /// Returns true if changes were made, false if they were not.
        /// </returns>
        public bool HasChanges()
        {
            bool result = false;

            // Gets a list of any tracked changes among the objects.
            TrackedChanges = new ConcurrentBag<ObjectTrackingChange>();
            foreach (var item in _trackedList)
            {
                if (item.HasChanges())
                {
                    TrackedChanges.Add(new ObjectTrackingChange() { Item = item.Value, Before = item.Before, After = item.After });
                    result = true;
                }
            }

            // Determines if any rows were added or removed.
            if (ItemsAdded().Any() || ItemsRemoved().Any())
                result = true;

            return result;
        }

        /// <summary>
        /// Lists any rows removed.
        /// </summary>
        /// <returns>
        /// Returns a List<string> that stores json representations of the objects.
        /// </returns>
        public IEnumerable<string> ItemsRemovedJson()
        {
            var result = new List<string>();
            var removed = _originalList.Except(_currentList).ToList();
            if (removed != null)
            {
                foreach (var s in removed)
                    result.Add(JsonSerializer.Serialize(s));
            }
            return result;
        }

        /// <summary>
        /// Lists any rows added.
        /// </summary>
        /// <returns>
        /// Returns a List<string> that stores json representations of the objects.
        /// </returns>
        public IEnumerable<string> ItemsAddedJson()
        {
            var result = new List<string>();
            var added = _currentList.Except(_originalList).ToList();
            if (added != null)
            {
                foreach (var s in added)
                    result.Add(JsonSerializer.Serialize(s));
            }
            return result;
        }

        /// <summary>
        /// Lists any rows removed.
        /// </summary>
        /// <returns>
        /// Returns a List<T> that stores the objects removed.
        /// </returns>
        public IEnumerable<T> ItemsRemoved()
        {
            var result = new List<T>();
            var removed = _originalList.Except(_currentList).ToList();
            result.AddRange(removed);
            return result;
        }

        /// <summary>
        /// Lists any rows added.
        /// </summary>
        /// <returns>
        /// Returns a List<T> that stores the objects added.
        /// </returns>
        public IEnumerable<T> ItemsAdded()
        {
            var result = new List<T>();
            var added = _currentList.Except(_originalList).ToList();
            result.AddRange(added);
            return result;
        }

        /// <summary>
        /// Adds an item to the list and to tracking.
        /// </summary>
        /// <param name="p"></param>
        public void Add(T p)
        {
            _currentList.Add(p);
            _trackedList.Add(new Track<T>(p, Inclusions));
        }

        /// <summary>
        /// Removes an item from the list and from tracking.
        /// </summary>
        /// <param name="p"></param>
        public void Remove(T p)
        {
            _currentList.Remove(p);
            _trackedList.RemoveAll(x => x.Value is not null ? x.Value.Equals(p) : default);
        }

        /// <summary>
        /// A class created to store the original value being tracked
        /// along with the before and after changes.
        /// </summary>
        public class ObjectTrackingChange
        {
            public T? Item { get; set; }
            public string? Before { get; set; }
            public string? After { get; set; }
        }
    }
}
