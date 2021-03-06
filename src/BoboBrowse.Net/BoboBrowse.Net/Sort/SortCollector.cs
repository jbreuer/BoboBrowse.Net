﻿//* Bobo Browse Engine - High performance faceted/parametric search implementation 
//* that handles various types of semi-structured data.  Originally written in Java.
//*
//* Ported and adapted for C# by Shad Storhaug.
//*
//* Copyright (C) 2005-2015  John Wang
//*
//* Licensed under the Apache License, Version 2.0 (the "License");
//* you may not use this file except in compliance with the License.
//* You may obtain a copy of the License at
//*
//*   http://www.apache.org/licenses/LICENSE-2.0
//*
//* Unless required by applicable law or agreed to in writing, software
//* distributed under the License is distributed on an "AS IS" BASIS,
//* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//* See the License for the specific language governing permissions and
//* limitations under the License.

// Version compatibility level: 3.2.0
// EXCEPTION: MemoryCache
namespace BoboBrowse.Net.Sort
{
    using BoboBrowse.Net.Facets;
    using Common.Logging;
    using Lucene.Net.Search;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;

    public abstract class SortCollector : Collector, IDisposable
    {
        private static readonly ILog logger = LogManager.GetLogger(typeof(SortCollector));

        // NightOwl888: The _collectDocIdCache setting seems to put arrays into
        // memory, but then do nothing with the arrays. Seems wasteful and unnecessary.

        //public class CollectorContext
        //{
        //    public BoboIndexReader reader;
        //    public int @base;
        //    public DocComparator comparator;
        //    public int length;

        //    private IDictionary<string, IRuntimeFacetHandler> _runtimeFacetMap;
        //    private IDictionary<string, object> _runtimeFacetDataMap;

        //    public CollectorContext(BoboIndexReader reader, int @base, DocComparator comparator)
        //    {
        //        this.reader = reader;
        //        this.@base = @base;
        //        this.comparator = comparator;
        //        _runtimeFacetMap = reader.RuntimeFacetHandlerMap;
        //        _runtimeFacetDataMap = reader.RuntimeFacetDataMap;
        //    }

        //    public virtual void RestoreRuntimeFacets()
        //    {
        //        reader.RuntimeFacetHandlerMap = _runtimeFacetMap;
        //        reader.RuntimeFacetDataMap = _runtimeFacetDataMap;
        //    }

        //    public virtual void ClearRuntimeFacetData()
        //    {
        //        reader.ClearRuntimeFacetData();
        //        reader.ClearRuntimeFacetHandler();
        //        _runtimeFacetDataMap = null;
        //        _runtimeFacetMap = null;
        //    }
        //}

        public IFacetHandler groupBy = null; // Point to the first element of groupByMulti to avoid array lookups.
        public IFacetHandler[] groupByMulti = null;

        // NightOwl888: The _collectDocIdCache setting seems to put arrays into
        // memory, but then do nothing with the arrays. Seems wasteful and unnecessary.

        //public List<CollectorContext> contextList;
        //public List<int[]> docidarraylist;
        //public List<float[]> scorearraylist;

        //public static int BLOCK_SIZE = 4096;

        protected readonly SortField[] _sortFields;
        protected readonly bool _fetchStoredFields;
        protected bool _closed = false;

        protected SortCollector(SortField[] sortFields, bool fetchStoredFields)
        {
            _sortFields = sortFields;
            _fetchStoredFields = fetchStoredFields;
        }

        abstract public BrowseHit[] TopDocs { get; }

        abstract public int TotalHits { get; }
        abstract public int TotalGroups { get; }
        abstract public IFacetAccessible[] GroupAccessibles { get; }

        private static DocComparatorSource GetNonFacetComparatorSource(SortField sf)
        {
            string fieldname = sf.Field;
            CultureInfo locale = sf.Locale;
            if (locale != null)
            {
                return new DocComparatorSource.StringLocaleComparatorSource(fieldname, locale);
            }

            int type = sf.Type;

            switch (type)
            {
                case SortField.INT:
                    return new DocComparatorSource.IntDocComparatorSource(fieldname);

                case SortField.FLOAT:
                    return new DocComparatorSource.FloatDocComparatorSource(fieldname);

                case SortField.LONG:
                    return new DocComparatorSource.LongDocComparatorSource(fieldname);

                case SortField.DOUBLE:
                    return new DocComparatorSource.LongDocComparatorSource(fieldname);

                case SortField.BYTE:
                    return new DocComparatorSource.ByteDocComparatorSource(fieldname);

                case SortField.SHORT:
                    return new DocComparatorSource.ShortDocComparatorSource(fieldname);

                case SortField.CUSTOM:
                    throw new InvalidOperationException("lucene custom sort no longer supported: " + fieldname);

                case SortField.STRING:
                    return new DocComparatorSource.StringOrdComparatorSource(fieldname);

                case SortField.STRING_VAL:
                    return new DocComparatorSource.StringValComparatorSource(fieldname);

                default:
                    throw new InvalidOperationException("Illegal sort type: " + type + ", for field: " + fieldname);
            }
        }

        private static DocComparatorSource GetComparatorSource(IBrowsable browser, SortField sf)
        {
            DocComparatorSource compSource = null;
            if (SortField.FIELD_DOC.Equals(sf))
            {
                compSource = new DocComparatorSource.DocIdDocComparatorSource();
            }
            else if (SortField.FIELD_SCORE.Equals(sf) || sf.Type == SortField.SCORE)
            {
                // we want to do reverse sorting regardless for relevance
                compSource = new ReverseDocComparatorSource(new DocComparatorSource.RelevanceDocComparatorSource());
            }
            else if (sf is BoboCustomSortField)
            {
                BoboCustomSortField custField = (BoboCustomSortField)sf;
                DocComparatorSource src = custField.GetCustomComparatorSource();
                Debug.Assert(src != null);
                compSource = src;
            }
            else
            {
                IEnumerable<string> facetNames = browser.FacetNames;
                string sortName = sf.Field;
                if (facetNames.Contains(sortName))
                {
                    var handler = browser.GetFacetHandler(sortName);
                    Debug.Assert(handler != null);
                    compSource = handler.GetDocComparatorSource();
                }
                else
                {
                    // default lucene field
                    logger.Info("doing default lucene sort for: " + sf);
                    compSource = GetNonFacetComparatorSource(sf);
                }
            }
            bool reverse = sf.Reverse;
            if (reverse)
            {
                compSource = new ReverseDocComparatorSource(compSource);
            }
            compSource.IsReverse = reverse;
            return compSource;
        }

        private static SortField Convert(IBrowsable browser, SortField sort)
        {
            string field = sort.Field;
            var facetHandler = browser.GetFacetHandler(field);
            if (facetHandler != null)
            {
                //browser.GetFacetHandler(field); // BUG? this does nothing with the result.
                BoboCustomSortField sortField = new BoboCustomSortField(field, sort.Reverse, facetHandler.GetDocComparatorSource());
                return sortField;
            }
            else
            {
                return sort;
            }
        }

        public static SortCollector BuildSortCollector(IBrowsable browser, Query q, SortField[] sort,
            int offset, int count, bool forceScoring, bool fetchStoredFields, IEnumerable<string> termVectorsToFetch,
            string[] groupBy, int maxPerGroup, bool collectDocIdCache)
        {
            bool doScoring = forceScoring;
            if (sort == null || sort.Length == 0)
            {
                if (q != null && !(q is MatchAllDocsQuery))
                {
                    sort = new SortField[] { SortField.FIELD_SCORE };
                }
            }

            if (sort == null || sort.Length == 0)
            {
                sort = new SortField[] { SortField.FIELD_DOC };
            }

            IEnumerable<string> facetNames = browser.FacetNames;
            foreach (SortField sf in sort)
            {
                if (sf.Type == SortField.SCORE)
                {
                    doScoring = true;
                    break;
                }
            }

            DocComparatorSource compSource;
            if (sort.Length == 1)
            {
                SortField sf = Convert(browser, sort[0]);
                compSource = GetComparatorSource(browser, sf);
            }
            else
            {
                DocComparatorSource[] compSources = new DocComparatorSource[sort.Length];
                for (int i = 0; i < sort.Length; ++i)
                {
                    compSources[i] = GetComparatorSource(browser, Convert(browser, sort[i]));
                }
                compSource = new MultiDocIdComparatorSource(compSources);
            }
            return new SortCollectorImpl(compSource, sort, browser, offset, count, doScoring, fetchStoredFields, termVectorsToFetch, groupBy, maxPerGroup, collectDocIdCache);
        }

        public virtual Collector Collector { get; set; }

        public virtual void Dispose()
        {
            this.Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!_closed)
                {
                    _closed = true;

                    // NightOwl888: The _collectDocIdCache setting seems to put arrays into
                    // memory, but then do nothing with the arrays. Seems wasteful and unnecessary.
                    //if (contextList != null)
                    //{
                    //    foreach (CollectorContext context in contextList)
                    //    {
                    //        context.ClearRuntimeFacetData();
                    //    }
                    //}
                    //if (docidarraylist != null)
                    //{
                    //    while (!(docidarraylist.Count == 0))
                    //    {
                    //        intarraymgr.Release(docidarraylist.Poll());
                    //    }
                    //}
                    //if (scorearraylist != null)
                    //{
                    //    while (!(scorearraylist.Count == 0))
                    //    {
                    //        floatarraymgr.Release(scorearraylist.Poll());
                    //    }
                    //}
                }
            }
        }
    }
}
