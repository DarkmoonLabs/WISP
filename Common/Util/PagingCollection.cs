using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

namespace Shared
{
    public class PagingCollection<T> : IEnumerable<T>
    {
        #region non-public fields
        private static int _defaultPageSize = 10;
        private IEnumerable<T> _collection;
        private int _pageSize = _defaultPageSize;
        #endregion

        #region public fields
        /// <summary>
        /// Gets or sets page size
        /// </summary>
        public int PageSize
        {
            get
            {
                return _pageSize;
            }
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentException();
                }
                _pageSize = value;
            }
        }

        /// <summary>
        /// Gets pages count
        /// </summary>
        public int PagesCount
        {
            get
            {
                float num = _collection.Count();
                float pageSize = PageSize;

                return (int)Math.Ceiling(num / pageSize);
            }
        }
        #endregion

        #region ctor
        /// <summary>
        /// Creates paging collection and sets page size
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="pageSize"></param>
        public PagingCollection(IEnumerable<T> collection, int pageSize)
        {
            if (collection == null)
            {
                throw new ArgumentNullException("collection");
            }
            PageSize = pageSize;
            _collection = collection.ToArray();
        }

        /// <summary>
        /// Creates paging collection
        /// </summary>
        /// <param name="collection"></param>
        public PagingCollection(IEnumerable<T> collection)
            : this(collection, _defaultPageSize)
        { }
        #endregion

        #region public methods
        /// <summary>
        /// Returns data by page number
        /// </summary>
        /// <param name="pageNumber"></param>
        /// <returns></returns>
        public IEnumerable<T> GetData(int pageNumber)
        {
            if (pageNumber < 0 || pageNumber > PagesCount)
            {
                return new T[] { };
            }

            int offset = (pageNumber - 1) * PageSize;

            return _collection.Skip(offset).Take(PageSize);
        }

        /// <summary>
        /// Returns number of items on page by number
        /// </summary>
        /// <param name="pageNumber"></param>
        /// <returns></returns>
        public int GetCount(int pageNumber)
        {
            return GetData(pageNumber).Count();
        }
        #endregion

        #region static methods
        /// <summary>
        /// Returns data by page number and page size
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="pageNumber"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public static IEnumerable<T> GetPaging(IEnumerable<T> collection, int pageNumber, int pageSize)
        {
            return new PagingCollection<T>(collection, pageSize).GetData(pageNumber);
        }

        /// <summary>
        /// Returns data by page number
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="pageNumber"></param>
        /// <returns></returns>
        public static IEnumerable<T> GetPaging(IEnumerable<T> collection, int pageNumber)
        {
            return new PagingCollection<T>(collection, _defaultPageSize).GetData(pageNumber);
        }
        #endregion

        #region IEnumerable<T> Members
        /// <summary>
        /// Returns an enumerator that iterates through collection
        /// </summary>
        /// <returns></returns>
        public IEnumerator<T> GetEnumerator()
        {
            return _collection.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members
        /// <summary>
        /// Returns an enumerator that iterates through collection
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }
}
