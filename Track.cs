/**
 * Author: Ryan A. Kueter
 * For the full copyright and license information, please view the LICENSE
 * file that was distributed with this source code.
 */
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using System.Reflection;
namespace ObjectTrackers
{
    /// <summary>
    /// A very simple execution state tracking solution 
    /// originally created to help with front end, or 
    /// client-side, auditing.
    /// </summary>
    /// <typeparam name="T">
    /// The datatype being tracked.
    /// </typeparam>
    public class Track<T>
    {
        /// <summary>
        /// The object being tracked
        /// </summary>
        public T Value;

        /// <summary>
        /// Stores a string representation of the properties and their values
        /// in a dictionary for comparison and differencing.
        /// </summary>
        private Dictionary<string, string> AllInitialValues = new Dictionary<string, string>();
        private Dictionary<string, string> AllFinalValues = new Dictionary<string, string>();

        /// <summary>
        /// Stores the before and after changes of the object being tracked.
        /// </summary>
        private Dictionary<string, string> PreviousChangedValues = new Dictionary<string, string>();
        private Dictionary<string, string> NewChangedValues = new Dictionary<string, string>();

        /// <summary>
        /// Stores a list of non-system and non-collection members
        /// </summary>
        private List<string> Properties = new List<string>();

        /// <summary>
        /// Stores a list of only the properties you want to track.
        /// </summary>
        private List<string> Inclusions = new List<string>();

        /// <summary>
        /// Enables the user to assign a custom id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The before and after values
        /// </summary>
        public string Before { get; set; } = String.Empty;
        public string After { get; set; } = String.Empty;

        /// <summary>
        /// Tracks the changes made to a datatype.
        /// </summary>
        /// <param name="t">
        /// The object you want to track.
        /// </param>
        /// <param name="inclusions">
        /// A list of the property names you want to track.
        /// If this list is excluded, it will make a best
        /// effort to track all property changes.
        /// </param>
        public Track(T t, params string[] inclusions)
        {
            Value = t;
            if (inclusions is not null)
                Inclusions.AddRange(inclusions);

            GetInitialValues();
        }

        /// <summary>
        /// Gets the initial values of the object and 
        /// stores them as strings.
        /// </summary>
        private void GetInitialValues()
        {
            GetProperties(AllInitialValues);
        }

        /// <summary>
        /// Determines if any changes have occurred during execution.
        /// </summary>
        /// <returns>
        /// Returns true if changes were made, false if they were not.
        /// </returns>
        public bool HasChanges()
        {
            ClearValues();
            GetProperties(AllFinalValues);

            bool hasChanges = false;
            foreach (KeyValuePair<string, string> init in AllInitialValues)
            {
                foreach (KeyValuePair<string, string> final in AllFinalValues)
                {
                    // Check tracked system datatypes
                    if (init.Key == final.Key && init.Value != final.Value)
                    {
                        PreviousChangedValues.Add(init.Key, init.Value);
                        NewChangedValues.Add(final.Key, final.Value);
                        hasChanges = true;
                    }

                    // Check any tracked custom or collection datatypes
                    if (init.Key == final.Key && Properties.Contains(init.Key))
                    {
                        // Compares the initial and final json strings for differences
                        // and includes them if they are different.
                        if (!init.Value.Equals(final.Value))
                        {
                            PreviousChangedValues[init.Key] = init.Value;
                            NewChangedValues[final.Key] = final.Value;
                            hasChanges = true;
                        }
                    }
                }
            }

            // If the execution state has changes
            // serialize the before and after results
            if (hasChanges)
            {
                var options = new JsonSerializerOptions()
                {
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
                Before = JsonSerializer.Serialize(PreviousChangedValues, options);
                After = JsonSerializer.Serialize(NewChangedValues, options);
            }
            return hasChanges;
        }

        /// <summary>
        /// Stores a string representation of the properties
        /// and their values for comparison and for creating
        /// the before and after values.
        /// </summary>
        /// <param name="a"></param>
        private void GetProperties(Dictionary<string, string> a)
        {
            Type type = typeof(T);
            foreach (PropertyInfo p in type.GetProperties())
            {
                // Check if the user only wants to track specific properties
                if (!Inclusions.Any() || Inclusions.Contains(p.Name))
                {
                    // Get the value and property information
                    var value = p.GetValue(Value) ?? String.Empty;
                    var fullName = p.PropertyType.FullName ?? String.Empty;

                    // Track system types
                    if (fullName.StartsWith("System") && !fullName.StartsWith("System.Collections"))
                    {
                        a.Add(p.Name, value.ToString());
                        continue;
                    }

                    // Add a custom and collection datatypes
                    Properties.Add(p.Name);
                    a.Add(p.Name, JsonSerializer.Serialize(value));
                }
            }
        }

        /// <summary>
        /// Clear the changes made in the execution state
        /// </summary>
        private void ClearValues()
        {
            AllFinalValues.Clear();
            PreviousChangedValues.Clear();
            NewChangedValues.Clear();
        }
    }
}