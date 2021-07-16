using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dopamine.Core.Extensions
{
    public static class ObservableCollectionExtensions
    {
        /// <summary>
        /// Searches for an element that matches the conditions defined by the specified predicate,
        /// and returns the zero-based index of the first occurrence within the entire <see cref="ObservableCollection{T}"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection">The ObservableCollection.</param>
        /// <param name="predicate">The predicate.</param>
        /// <returns>
        /// The zero-based index of the first occurrence of an element that matches the conditions defined by <paramref name="predicate"/>, if found; otherwise it'll throw.
        /// </returns>
        public static int FindIndex<T>(this ObservableCollection<T> collection, Func<T, bool> predicate)
        {
            for (int i = 0, count = collection.Count; i < count; i++)
            {
                if (predicate(collection[i])) return i;
            }

            return -1;
        }
    }
}
