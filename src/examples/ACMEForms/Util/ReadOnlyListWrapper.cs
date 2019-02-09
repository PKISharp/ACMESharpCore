using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ACMEForms.Util
{
    /// <summary>
    /// Implements a read-only <see cref="IList"/> interface over wrapped collection with
    /// delegate accessors for items and count.
    /// </summary>
    /// <remarks>
    /// This wrapper class is useful as a <c>DataSource</c> for list-oriented controls
    /// around iterable data items without the need to construct actual list collections
    /// every time the data source is changed or its contents are updated.
    /// </remarks>
    public class ReadOnlyListWrapper<T> : IList<T>, IList
    {
        private Func<int> _countFunc;
        private Func<int, T> _getterFunc;
        private Func<T, int> _indexOfFunc;

        public ReadOnlyListWrapper(Func<List<T>> f)
        {
            _countFunc = () => f().Count;
            _getterFunc = i => f()[i];
            _indexOfFunc = i => f().IndexOf(i);
        }

        public ReadOnlyListWrapper(Func<T[]> f)
        {
            _countFunc = () => f().Length;
            _getterFunc = i => f()[i];
        }

        public ReadOnlyListWrapper(Func<IEnumerable<T>> f)
        {
            _countFunc = () => f().Count();
            _getterFunc = i => f().Skip(i).First();
        }

        public ReadOnlyListWrapper(Func<int> countFunc, Func<int, T> getterFunc, Func<T, int> indexOfFunc = null)
        {
            _countFunc = countFunc;
            _getterFunc = getterFunc;
            _indexOfFunc = indexOfFunc;
        }

        public T this[int index]
        {
            get => _getterFunc(index);
            set => throw new NotImplementedException();
        }

        public int Count => _countFunc();

        public bool IsReadOnly => true;

        bool IList.IsReadOnly => true;

        bool IList.IsFixedSize => false;

        int ICollection.Count => Count;

        object ICollection.SyncRoot => throw new NotImplementedException();

        bool ICollection.IsSynchronized => false;

        object IList.this[int index]
        {
            get => this[index];
            set => throw new NotImplementedException();
        }

        public int IndexOf(T item)
        {
            if (_indexOfFunc == null)
            {
                for (var i = 0; i < _countFunc(); ++i)
                    if (object.Equals(item, _getterFunc(i)))
                        return i;
                return -1;
            }
            else
            {
                return _indexOfFunc(item);
            }
        }

        public bool Contains(T item)
        {
            return IndexOf(item) >= 0;
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (var i = 0; i < _countFunc(); ++i)
                yield return _getterFunc(i);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            for (var i = 0; i < _countFunc(); ++i)
                yield return _getterFunc(i);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            for (var i = 0; i < _countFunc(); ++i)
                array[arrayIndex + i] = _getterFunc(i);
        }

        public void Add(T item)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public void Insert(int index, T item)
        {
            throw new NotImplementedException();
        }

        public bool Remove(T item)
        {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        int IList.IndexOf(object value) => IndexOf((T)value);

        bool IList.Contains(object value) => Contains((T)value);

        void ICollection.CopyTo(Array array, int index) => CopyTo((T[])array, index);

        int IList.Add(object value)
        {
            throw new NotImplementedException();
        }

        void IList.Clear()
        {
            throw new NotImplementedException();
        }

        void IList.Insert(int index, object value)
        {
            throw new NotImplementedException();
        }

        void IList.Remove(object value)
        {
            throw new NotImplementedException();
        }

        void IList.RemoveAt(int index)
        {
            throw new NotImplementedException();
        }
    }
}
