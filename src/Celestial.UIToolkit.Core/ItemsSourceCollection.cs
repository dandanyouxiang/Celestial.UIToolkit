﻿using Celestial.UIToolkit.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Celestial.UIToolkit
{

    /// <summary>
    /// A collection which can retrieve its items from an items source, if specified.
    /// If not, it behaves like a normal collection to which any item can be added.
    /// </summary>
    /// <seealso cref="ItemsSource"/>
    public class ItemsSourceCollection 
        : IList, IList<object>, INotifyCollectionChanged, INotifyPropertyChanged
    {

        private const string IndexAccessorPropertyName = "Items[]";

        private object _itemsSource;
        private IEnumerable<object> _enumerableItemsSource;
        private ObservableCollection<object> _innerCollection;

        /// <summary>
        /// Occurs when either the internal collection or the <see cref="ItemsSource"/> changes.
        /// The <see cref="ItemsSource"/> must implement <see cref="INotifyCollectionChanged"/>
        /// for this event to work.
        /// </summary>
        public event NotifyCollectionChangedEventHandler CollectionChanged;
        
        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        
        /// <summary>
        /// Gets a value indicating whether the collection is currently
        /// based on an items source provided by the <see cref="ItemsSource"/> property.
        /// </summary>
        public bool IsUsingItemsSource => ItemsSource != null;

        /// <summary>
        /// Gets a value indicating whether the <see cref="ItemsSource"/> implements
        /// the <see cref="IEnumerable"/> interface.
        /// </summary>
        /// <remarks>
        /// Note that the non-generic <see cref="IEnumerable"/> interface is tested by this
        /// property.
        /// </remarks>
        protected bool HasEnumerableItemsSource => _enumerableItemsSource != null;

        /// <summary>
        /// Gets or sets an object from which this collection retrieves its items.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this property is set while the collection contains any items which do not
        /// come from another items source.
        /// </exception>
        public object ItemsSource
        {
            get { return _itemsSource; }
            set
            {
                object oldItemsSource = _itemsSource;
                object newItemsSource = value;
                ChangeItemsSource(oldItemsSource, newItemsSource);
            }
        }

        /// <summary>
        /// Gets the element at the specified index, or
        /// sets the item at the specified index within the current collection, if
        /// no <see cref="ItemsSource"/> is set.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>
        /// The element at the specified index in the collection, if <see cref="ItemsSource"/>
        /// is null.
        /// If <see cref="ItemsSource"/> implements <see cref="IEnumerable"/>, returns the element
        /// at the specified index from the <see cref="ItemsSource"/>.
        /// If <see cref="ItemsSource"/> is set, but does not implement <see cref="IEnumerable"/>,
        /// this will return the <see cref="ItemsSource"/>, as long as the <paramref name="index"/>
        /// is 0.
        /// </returns>
        /// <exception cref="IndexOutOfRangeException" />
        /// <exception cref="InvalidOperationException">
        /// Thrown if an attempt to set this property was made, while the collection had an active
        /// <see cref="ItemsSource"/>.
        /// </exception>
        public object this[int index]
        {
            get
            {
                return GetElementAt(index);
            }
            set
            {
                ThrowIfInnerCollectionIsNotWriteable();
                _innerCollection[index] = value;
            }
        }

        /// <summary>
        /// Gets the number of elements in this collection or an active <see cref="ItemsSource"/>
        /// which implements <see cref="IEnumerable"/>.
        /// If <see cref="ItemsSource"/> is set, but does not implement <see cref="IEnumerable"/>,
        /// this returns 1.
        /// </summary>
        public int Count
        {
            get
            {
                if (IsUsingItemsSource)
                {
                    if (HasEnumerableItemsSource)
                    {
                        return _enumerableItemsSource.Count();
                    }
                    else
                    {
                        return 1;
                    }
                }
                else
                {
                    return _innerCollection.Count;
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether the collection is currently read-only,
        /// meaning that any methods which modify it will throw an exception.
        /// </summary>
        public bool IsReadOnly => IsUsingItemsSource;

        /// <summary>
        /// Gets a value indicating whether the collection has a fixed size.
        /// </summary>
        public bool IsFixedSize => IsUsingItemsSource;

        /// <summary>
        /// Gets an object which can be used for cross-thread synchronization.
        /// Accessing this property while an <see cref="ItemsSource"/> is set will
        /// throw a <see cref="NotSupportedException"/>.
        /// </summary>
        /// <exception cref="NotSupportedException">
        /// Thrown when this property is accessed while the <see cref="ItemsSource"/> is set to a 
        /// valid value.
        /// </exception>
        public object SyncRoot
        {
            get
            {
                if (IsUsingItemsSource)
                    throw new NotSupportedException(
                        $"The {nameof(ItemsSourceCollection)} doesn't provide a synchronization " +
                        $"object if an {nameof(ItemsSource)} is used.");
                return ((ICollection)_innerCollection).SyncRoot;
            }
        }

        /// <summary>
        /// Gets a value which indicates whether the collection is synchronized.
        /// This is not the case. This returns false.
        /// </summary>
        public bool IsSynchronized => false;

        /// <summary>
        /// Initializes a new, empty <see cref="ItemsSourceCollection"/> without an
        /// <see cref="ItemsSource"/>.
        /// </summary>
        public ItemsSourceCollection()
        {
            _innerCollection = new ObservableCollection<object>();
            _innerCollection.CollectionChanged += Collection_Changed;
        }

        private void ChangeItemsSource(object oldItemsSource, object newItemsSource)
        {
            if (!IsUsingItemsSource && newItemsSource == null) return;
            ThrowIfItemsSourceIsNotChangeable();
            _itemsSource = newItemsSource;

            UnhookOldItemsSource(oldItemsSource);
            HookNewItemsSource(newItemsSource);
            RaiseItemsSourceChangedEvents(oldItemsSource, newItemsSource);
        }

        private void UnhookOldItemsSource(object oldItemsSource)
        {
            // Remove old event handlers to avoid memory leaks.
            if (oldItemsSource is INotifyCollectionChanged oldNotifyCollectionChanged)
            {
                oldNotifyCollectionChanged.CollectionChanged -= Collection_Changed;
            }
        }

        private void HookNewItemsSource(object newItemsSource)
        {
            // ItemsSource can be any object, but if we can convert it to an enumerable, 
            // we have a whole lot of additional methods that we can use.
            if (newItemsSource is IEnumerable enumerable)
            {
                _enumerableItemsSource = enumerable.Cast<object>();
            }

            // In any case, try to hook up a CollectionChanged event.
            if (newItemsSource is INotifyCollectionChanged newNotifyCollectionChanged)
            {
                newNotifyCollectionChanged.CollectionChanged += Collection_Changed;
            }
        }

        private void RaiseItemsSourceChangedEvents(object oldItemsSource, object newItemsSource)
        {
            // When an ItemsSource gets removed, we also need to notify the collection about that.
            if (oldItemsSource != null)
            {
                RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Reset));
                RaisePropertyChanged(nameof(Count));
                RaisePropertyChanged(IndexAccessorPropertyName);
            }

            // If we have a new ItemsSource, the collection also changed.
            // We do have to check if the ItemsSource is Enumerable though.
            if (newItemsSource != null)
            {
                // Iterate over the source/call the event for every single item,
                // because WPF doesn't support a "batch"-operations.
                // For instance, the ListView will throw NotSupportedEx for a
                // CollectionChanged-event which modifies multiple elements at once.
                int currentIndex = 0;
                foreach (var item in this)
                {
                    RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(
                        NotifyCollectionChangedAction.Add, item, currentIndex++));
                }
            }

            RaisePropertyChanged(nameof(ItemsSource));
            RaisePropertyChanged(nameof(IsUsingItemsSource));
            RaisePropertyChanged(nameof(IsReadOnly));
            RaisePropertyChanged(nameof(IsFixedSize));
            RaisePropertyChanged(nameof(HasEnumerableItemsSource));
            RaisePropertyChanged(nameof(SyncRoot));
        }

        private void Collection_Changed(object sender, NotifyCollectionChangedEventArgs e)
        {
            // We rely on the InnerCollection and the ItemsSource implementing
            // INotifyCollectionChanged themselves. Thanks to that, we can simply forward
            // the event.
            RaiseCollectionChanged(e);
        }

        /// <summary>
        /// Raises the <see cref="CollectionChanged"/> event and
        /// calls the <see cref="OnCollectionChanged"/> method afterwards.
        /// In addition, the <see cref="PropertyChanged"/> event is raised for the
        /// <see cref="Count"/> and index-accessor properties.
        /// </summary>
        /// <param name="e">Event data for the event.</param>
        protected void RaiseCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            OnCollectionChanged(e);
            CollectionChanged?.Invoke(this, e);

            // Whenever the collection is changed, these properties automatically change aswell.
            RaisePropertyChanged(nameof(Count));
            RaisePropertyChanged(IndexAccessorPropertyName);
        }

        /// <summary>
        /// Called before the <see cref="CollectionChanged"/> event occurs.
        /// </summary>
        /// <param name="e">Event data for the event.</param>
        protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e) { }

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event and
        /// calls the <see cref="OnPropertyChanged"/> method afterwards.
        /// </summary>
        /// <param name="propertyName">The name of the changed property.</param>
        protected void RaisePropertyChanged([CallerMemberName]string propertyName = "")
        {
            if (string.IsNullOrEmpty(propertyName))
                return;
            RaisePropertyChanged(new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event and
        /// calls the <see cref="OnPropertyChanged"/> method afterwards.
        /// </summary>
        /// <param name="e">Event data for the event.</param>
        protected void RaisePropertyChanged(PropertyChangedEventArgs e)
        {
            OnPropertyChanged(e);
            PropertyChanged?.Invoke(this, e);
        }

        /// <summary>
        /// Called before the <see cref="PropertyChanged"/> event occurs.
        /// </summary>
        /// <param name="e">Event data for the event.</param>
        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e) { }

        #region Collection Members

        /// <summary>
        /// Gets the element at the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>
        /// The element at the specified index in the collection, if <see cref="ItemsSource"/>
        /// is null.
        /// If <see cref="ItemsSource"/> implements <see cref="IEnumerable"/>, returns the element
        /// at the specified index from the <see cref="ItemsSource"/>.
        /// If <see cref="ItemsSource"/> is set, but does not implement <see cref="IEnumerable"/>,
        /// this will return the <see cref="ItemsSource"/>, as long as the <paramref name="index"/>
        /// is 0.
        /// </returns>
        /// <exception cref="IndexOutOfRangeException" />
        public object GetElementAt(int index)
        {
            if (IsUsingItemsSource)
            {
                if (HasEnumerableItemsSource)
                {
                    return _enumerableItemsSource.ElementAt(index);
                }
                else
                {
                    if (index != 0)
                    {
                        throw new IndexOutOfRangeException(
                            $"For a non-enumerable {nameof(ItemsSource)}, the only supported " +
                            $"index is 0.");
                    }
                    return ItemsSource;
                }
            }
            else
            {
                return _innerCollection[index];
            }
        }

        /// <summary>
        /// Returns the index of the specified <paramref name="value"/> within the collection.
        /// </summary>
        /// <param name="value">The value to be found.</param>
        /// <returns>
        /// The index of the <paramref name="value"/> within the collection or the 
        /// <see cref="ItemsSource"/>, if the latter implements <see cref="IEnumerable"/>.
        /// If <see cref="ItemsSource"/> is set, but does not implement <see cref="IEnumerable"/>,
        /// this method compares the <see cref="ItemsSource"/> object to <paramref name="value"/>
        /// and returns 0, if they are equal.
        /// 
        /// If nothing was found, returns -1.
        /// </returns>
        public int IndexOf(object value)
        {
            if (IsUsingItemsSource)
            {
                if (HasEnumerableItemsSource)
                {
                    return _enumerableItemsSource.IndexOf(value);
                }
                else
                {
                    return ItemsSource == value ? 0 : -1;
                }
            }
            else
            {
                return _innerCollection.IndexOf(value);
            }
        }

        /// <summary>
        /// Returns a value indicating whether the specified <paramref name="value"/> can be found
        /// inside this collection.
        /// </summary>
        /// <param name="value">
        /// The value to be checked.
        /// </param>
        /// <returns>
        /// true if either the collection, or an <see cref="ItemsSource"/> which implements
        /// <see cref="IEnumerable"/> contains the provided <paramref name="value"/>.
        /// If <see cref="ItemsSource"/> does not implement <see cref="IEnumerable"/>, 
        /// this returns a value indicating whether it equals the specified
        /// <paramref name="value"/>.
        /// </returns>
        public bool Contains(object value)
        {
            if (IsUsingItemsSource)
            {
                if (HasEnumerableItemsSource)
                {
                    return _enumerableItemsSource.Contains(value);
                }
                else
                {
                    return ItemsSource == value;
                }
            }
            else
            {
                return _innerCollection.Contains(value);
            }
        }

        /// <summary>
        /// Copies the elements of this collection to the specified <paramref name="array"/>.
        /// </summary>
        /// <param name="array">The destination array.</param>
        /// <param name="index">
        /// The zero-based index in <paramref name="array"/> from which copying starts.
        /// </param>
        public void CopyTo(Array array, int index)
        {
            if (IsUsingItemsSource)
            {
                if (HasEnumerableItemsSource)
                {
                    _enumerableItemsSource.ToArray().CopyTo(array, index);
                }
                else
                {
                    array.SetValue(ItemsSource, index);
                }
            }
            else
            {
                // For some reason, the ObservableCollection's CopyTo() throws during the tests.
                // It complains that an int[] can't be copied into an int[], which is obviously
                // wrong. (That's probably because it's an ObservableCollection<object>, which
                // can't easily be infered to IEnumerable<int>).
                // By using ToArray(), we get a functional CopyTo().
                _innerCollection.ToArray().CopyTo(array, index);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator which can be used to enumerate the collection.
        /// </summary>
        /// <returns>
        /// An <see cref="IEnumerator{T}"/> which can be used to enumerate over the
        /// collection.
        /// </returns>
        public IEnumerator<object> GetEnumerator()
        {
            if (IsUsingItemsSource)
            {
                if (HasEnumerableItemsSource)
                {
                    return _enumerableItemsSource.GetEnumerator();
                }
                else
                {
                    return new SingleItemEnumerator(ItemsSource);
                }
            }
            else
            {
                return _innerCollection.GetEnumerator();
            }
        }

        /// <summary>
        /// Adds the specified <paramref name="value"/> to the collection.
        /// </summary>
        /// <param name="value">The value to be added.</param>
        /// <returns>The index, at which the value was added.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the <see cref="ItemsSource"/> property is set.
        /// </exception>
        public int Add(object value)
        {
            int index = Count;
            Insert(index, value);
            return index;
        }

        /// <summary>
        /// Removes all elements from the collection.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the <see cref="ItemsSource"/> property is set.
        /// </exception>
        public void Clear()
        {
            ThrowIfInnerCollectionIsNotWriteable();
            _innerCollection.Clear();
        }

        /// <summary>
        /// Inserts an element into the collection at the specified <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The index at which the element should be inserted.</param>
        /// <param name="value">The element to be inserted.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the <see cref="ItemsSource"/> property is set.
        /// </exception>
        public void Insert(int index, object value)
        {
            ThrowIfInnerCollectionIsNotWriteable();
            _innerCollection.Insert(index, value);
        }

        void IList.Remove(object value)
        {
            Remove(value);
        }

        /// <summary>
        /// If found, removes the specified <paramref name="value"/> from the collection.
        /// </summary>
        /// <param name="value">The value to be removed from the collection.</param>
        /// <returns>
        /// true if removing the item succeeded; false if not.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the <see cref="ItemsSource"/> property is set.
        /// </exception>
        public bool Remove(object value)
        {
            ThrowIfInnerCollectionIsNotWriteable();
            int index = IndexOf(value);
            if (index >= 0)
            {
                RemoveAt(index);
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Removes the element at the specified <paramref name="index"/> from the collection.
        /// </summary>
        /// <param name="index">The element's index.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the <see cref="ItemsSource"/> property is set.
        /// </exception>
        public void RemoveAt(int index)
        {
            ThrowIfInnerCollectionIsNotWriteable();
            _innerCollection.RemoveAt(index);
        }

        private void ThrowIfItemsSourceIsNotChangeable()
        {
            // Only allow switching to an ItemsSource, if the underlying collection is empty.
            // Otherwise, items could get lost/we'd have to merge the two sources.
            if (!IsUsingItemsSource && _innerCollection.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Switching to an {nameof(ItemsSource)} failed, because the " +
                    $"collection is not empty.");
            }
        }
        
        private void ThrowIfInnerCollectionIsNotWriteable()
        {
            if (IsUsingItemsSource)
            {
                throw new InvalidOperationException(
                    $"Cannot modify the collection while an active " +
                    $"{nameof(ItemsSource)} is provided. " +
                    $"To manipulate the elements in this collection, use the value of the " +
                    $"{nameof(ItemsSource)} property.");
            }
        }

        #endregion

        #region IList<object> members

        int ICollection<object>.Count => Count;

        bool ICollection<object>.IsReadOnly => ((IList)this).IsReadOnly;

        object IList<object>.this[int index]
        {
            get => this[index];
            set => this[index] = value;
        }

        int IList<object>.IndexOf(object item) => IndexOf(item);

        void IList<object>.Insert(int index, object item) => Insert(index, item);

        void IList<object>.RemoveAt(int index) => RemoveAt(index);

        void ICollection<object>.Add(object item) => Add(item);

        void ICollection<object>.Clear() => Clear();

        bool ICollection<object>.Contains(object item) => Contains(item);

        void ICollection<object>.CopyTo(object[] array, int arrayIndex) 
            => CopyTo(array, arrayIndex);

        bool ICollection<object>.Remove(object item) => Remove(item);

        IEnumerator<object> IEnumerable<object>.GetEnumerator() => GetEnumerator();

        #endregion

        /// <summary>
        /// An enumerator which only enumerates over a single item.
        /// Used to provide enumeration for an ItemsSource which does not implement
        /// IEnumerable.
        /// </summary>
        private struct SingleItemEnumerator : IEnumerator<object>
        {

            private const int StartIndex = -1;
            private const int OnCurrentItemIndex = 0;
            private const int EndIndex = 1;

            // This enumerator can basically do nothing but provide the element with which it
            // has been initialized.
            private int _currentIndex;
            private object _item;

            public object Current
            {
                get
                {
                    if (_currentIndex == OnCurrentItemIndex)
                        return _item;
                    else
                        throw new InvalidOperationException(
                            $"The enumeration either hasn't started yet, or it has already been " +
                            $"finished.");
                }
            }

            public SingleItemEnumerator(object item)
            {
                _currentIndex = StartIndex;
                _item = item;
            }

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                if (_currentIndex == StartIndex)
                {
                    _currentIndex++;
                    return true;
                }
                else if (_currentIndex == OnCurrentItemIndex)
                {
                    _currentIndex++;
                    return false;
                }
                else if (_currentIndex == EndIndex)
                {
                    return false;
                }
                else
                {
                    throw new NotImplementedException(
                        $"Not implemented. The internal index of the " +
                        $"{nameof(SingleItemEnumerator)} has the wrong number. " +
                        $"If you see this, then you have definitly encountered a bug.");
                }
            }

            public void Reset()
            {
                _currentIndex = StartIndex;
            }

            public override string ToString()
            {
                return Current?.ToString() ?? "";
            }

        }

    }

}
