﻿//* Bobo Browse Engine - High performance faceted/parametric search implementation 
//* that handles various types of semi-structured data.  Originally written in Java.
//*
//* Ported and adapted for C# by Shad Storhaug, Alexey Shcherbachev, and zhengchun.
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
namespace BoboBrowse.Net.Facets.Data
{
    using BoboBrowse.Net.Facets.Range;
    using BoboBrowse.Net.Sort;
    using BoboBrowse.Net.Support;
    using BoboBrowse.Net.Util;
    using Common.Logging;
    using Lucene.Net.Index;
    using Lucene.Net.Search;
    using Lucene.Net.Util;
    using System;
    using System.Collections.Generic;

    public class MultiValueFacetDataCache : FacetDataCache
    {
        //private static long serialVersionUID = 1L; // NOT USED
        private static ILog logger = LogManager.GetLogger(typeof(MultiValueFacetDataCache));

        protected readonly BigNestedIntArray _nestedArray;
        protected int _maxItems = BigNestedIntArray.MAX_ITEMS;
        protected bool _overflow = false;

        public MultiValueFacetDataCache()
        {
            _nestedArray = new BigNestedIntArray();
        }

        public BigNestedIntArray NestedArray
        {
            get { return _nestedArray; }
        }

        public virtual int MaxItems
        {
            set
            {
                _maxItems = Math.Min(value, BigNestedIntArray.MAX_ITEMS);
                _nestedArray.MaxItems = _maxItems;
            }
        }

        public override int GetNumItems(int docid)
        {
            return _nestedArray.GetNumItems(docid);
        } 

        public override void Load(string fieldName, IndexReader reader, TermListFactory listFactory)
        {
            this.Load(fieldName, reader, listFactory, new BoboIndexReader.WorkArea());
        }

        /// <summary>
        /// loads multi-value facet data. This method uses a workarea to prepare loading.
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="reader"></param>
        /// <param name="listFactory"></param>
        /// <param name="workArea"></param>
        public virtual void Load(string fieldName, IndexReader reader, TermListFactory listFactory, BoboIndexReader.WorkArea workArea)
        {
            long t0 = Environment.TickCount;
            int maxdoc = reader.MaxDoc;
            BigNestedIntArray.BufferedLoader loader = GetBufferedLoader(maxdoc, workArea);

            TermEnum tenum = null;
            TermDocs tdoc = null;
            ITermValueList list = (listFactory == null ? (ITermValueList)new TermStringList() : listFactory.CreateTermList());
            List<int> minIDList = new List<int>();
            List<int> maxIDList = new List<int>();
            List<int> freqList = new List<int>();
            OpenBitSet bitset = new OpenBitSet();
            int negativeValueCount = GetNegativeValueCount(reader, string.Intern(fieldName));
            int t = 0; // current term number
            list.Add(null);
            minIDList.Add(-1);
            maxIDList.Add(-1);
            freqList.Add(0);
            t++;

            _overflow = false;
            try
            {
                tdoc = reader.TermDocs();
                tenum = reader.Terms(new Term(fieldName, ""));
                if (tenum != null)
                {
                    do
                    {
                        Term term = tenum.Term;
                        if (term == null || !fieldName.Equals(term.Field))
                            break;

                        string val = term.Text;

                        if (val != null)
                        {
                            list.Add(val);

                            tdoc.Seek(tenum);
                            //freqList.add(tenum.docFreq()); // removed because the df doesn't take into account the num of deletedDocs
                            int df = 0;
                            int minID = -1;
                            int maxID = -1;
                            int valId = (t - 1 < negativeValueCount) ? (negativeValueCount - t + 1) : t;
                            if (tdoc.Next())
                            {
                                df++;
                                int docid = tdoc.Doc;

                                if (!loader.Add(docid, valId))
                                    LogOverflow(fieldName);
                                minID = docid;
                                bitset.Set(docid);
                                while (tdoc.Next())
                                {
                                    df++;
                                    docid = tdoc.Doc;

                                    if (!loader.Add(docid, valId))
                                        LogOverflow(fieldName);
                                    bitset.Set(docid);
                                }
                                maxID = docid;
                            }
                            freqList.Add(df);
                            minIDList.Add(minID);
                            maxIDList.Add(maxID);
                        }

                        t++;
                    }
                    while (tenum.Next());
                }
            }
            finally
            {
                try
                {
                    if (tdoc != null)
                    {
                        tdoc.Dispose();
                    }
                }
                finally
                {
                    if (tenum != null)
                    {
                        tenum.Dispose();
                    }
                }
            }

            list.Seal();

            try
            {
                _nestedArray.Load(maxdoc + 1, loader);
            }
            catch (System.IO.IOException e)
            {
                throw e;
            }
            catch (Exception e)
            {
                throw new RuntimeException("failed to load due to " + e.ToString(), e);
            }

            this.valArray = list;
            this.freqs = freqList.ToArray();
            this.minIDs = minIDList.ToArray();
            this.maxIDs = maxIDList.ToArray();

            int doc = 0;
            while (doc <= maxdoc && !_nestedArray.Contains(doc, 0, true))
            {
                ++doc;
            }
            if (doc <= maxdoc)
            {
                this.minIDs[0] = doc;
                doc = maxdoc;
                while (doc > 0 && !_nestedArray.Contains(doc, 0, true))
                {
                    --doc;
                }
                if (doc > 0)
                {
                    this.maxIDs[0] = doc;
                }
            }
            this.freqs[0] = maxdoc + 1 - (int)bitset.Cardinality();
        }

        /// <summary>
        /// loads multi-value facet data. This method uses the count payload to allocate storage before loading data.
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="reader"></param>
        /// <param name="listFactory"></param>
        /// <param name="sizeTerm"></param>
        public virtual void Load(string fieldName, IndexReader reader, TermListFactory listFactory, Term sizeTerm)
        {
            int maxdoc = reader.MaxDoc;
            BigNestedIntArray.Loader loader = new AllocOnlyLoader(_maxItems, sizeTerm, reader);
            int negativeValueCount = GetNegativeValueCount(reader, string.Intern(fieldName));
            try
            {
                _nestedArray.Load(maxdoc + 1, loader);
            }
            catch (System.IO.IOException e)
            {
                throw e;
            }
            catch (Exception e)
            {
                throw new RuntimeException("failed to load due to " + e.ToString(), e);
            }

            TermEnum tenum = null;
            TermDocs tdoc = null;
            ITermValueList list = (listFactory == null ? (ITermValueList)new TermStringList() : listFactory.CreateTermList());
            List<int> minIDList = new List<int>();
            List<int> maxIDList = new List<int>();
            List<int> freqList = new List<int>();
            OpenBitSet bitset = new OpenBitSet();

            int t = 0; // current term number
            list.Add(null);
            minIDList.Add(-1);
            maxIDList.Add(-1);
            freqList.Add(0);
            t++;

            _overflow = false;
            try
            {
                tdoc = reader.TermDocs();
                tenum = reader.Terms(new Term(fieldName, ""));
                if (tenum != null)
                {
                    do
                    {
                        Term term = tenum.Term;
                        if (term == null || !fieldName.Equals(term.Field))
                            break;

                        string val = term.Text;

                        if (val != null)
                        {
                            list.Add(val);

                            tdoc.Seek(tenum);
                            //freqList.add(tenum.docFreq()); // removed because the df doesn't take into account the num of deletedDocs
                            int df = 0;
                            int minID = -1;
                            int maxID = -1;
                            if (tdoc.Next())
                            {
                                df++;
                                int docid = tdoc.Doc;
                                if (!_nestedArray.AddData(docid, t))
                                    LogOverflow(fieldName);
                                minID = docid;
                                bitset.FastSet(docid);
                                int valId = (t - 1 < negativeValueCount) ? (negativeValueCount - t + 1) : t;
                                while (tdoc.Next())
                                {
                                    df++;
                                    docid = tdoc.Doc;
                                    if (!_nestedArray.AddData(docid, valId))
                                        LogOverflow(fieldName);
                                    bitset.FastSet(docid);
                                }
                                maxID = docid;
                            }
                            freqList.Add(df);
                            minIDList.Add(minID);
                            maxIDList.Add(maxID);
                        }

                        t++;
                    }
                    while (tenum.Next());
                }
            }
            finally
            {
                try
                {
                    if (tdoc != null)
                    {
                        tdoc.Dispose();
                    }
                }
                finally
                {
                    if (tenum != null)
                    {
                        tenum.Dispose();
                    }
                }
            }

            list.Seal();

            this.valArray = list;
            this.freqs = freqList.ToArray();
            this.minIDs = minIDList.ToArray();
            this.maxIDs = maxIDList.ToArray();

            int doc = 0;
            while (doc <= maxdoc && !_nestedArray.Contains(doc, 0, true))
            {
                ++doc;
            }
            if (doc <= maxdoc)
            {
                this.minIDs[0] = doc;
                doc = maxdoc;
                while (doc > 0 && !_nestedArray.Contains(doc, 0, true))
                {
                    --doc;
                }
                if (doc > 0)
                {
                    this.maxIDs[0] = doc;
                }
            }
            this.freqs[0] = maxdoc + 1 - (int)bitset.Cardinality();
        }

        protected virtual void LogOverflow(string fieldName)
        {
            if (!_overflow)
            {
                logger.Error("Maximum value per document: " + _maxItems + " exceeded, fieldName=" + fieldName);
                _overflow = true;
            }
        }

        protected virtual BigNestedIntArray.BufferedLoader GetBufferedLoader(int maxdoc, BoboIndexReader.WorkArea workArea)
        {
            if (workArea == null)
            {
                return new BigNestedIntArray.BufferedLoader(maxdoc, _maxItems, new BigIntBuffer());
            }
            else
            {
                BigIntBuffer buffer = workArea.Get<BigIntBuffer>();
                if (buffer == null)
                {
                    buffer = new BigIntBuffer();
                    workArea.Put(buffer);
                }
                else
                {
                    buffer.Reset();
                }

                BigNestedIntArray.BufferedLoader loader = workArea.Get<BigNestedIntArray.BufferedLoader>();
                if (loader == null || loader.Capacity < maxdoc)
                {
                    loader = new BigNestedIntArray.BufferedLoader(maxdoc, _maxItems, buffer);
                    workArea.Put(loader);
                }
                else
                {
                    loader.Reset(maxdoc, _maxItems, buffer);
                }
                return loader;
            }
        }

        /// <summary>
        /// A loader that allocate data storage without loading data to BigNestedIntArray.
        /// Note that this loader supports only non-negative integer data.
        /// </summary>
        public sealed class AllocOnlyLoader : BigNestedIntArray.Loader
        {
            private IndexReader _reader;
            private Term _sizeTerm;
            private int _maxItems;

            public AllocOnlyLoader(int maxItems, Term sizeTerm, IndexReader reader)
            {
                _maxItems = Math.Min(maxItems, BigNestedIntArray.MAX_ITEMS);
                _sizeTerm = sizeTerm;
                _reader = reader;
            }

            public override void Load()
            {
                TermPositions tp = null;
                byte[] payloadBuffer = new byte[4]; // four bytes for an int
                try
                {
                    tp = _reader.TermPositions(_sizeTerm);

                    if (tp == null)
                        return;

                    while (tp.Next())
                    {
                        if (tp.Freq > 0)
                        {
                            tp.NextPosition();
                            tp.GetPayload(payloadBuffer, 0);
                            int len = BytesToInt(payloadBuffer);
                            Allocate(tp.Doc, Math.Min(len, _maxItems), true);
                        }
                    }
                }
                finally
                {
                    if (tp != null)
                        tp.Dispose();
                }
            }

            private static int BytesToInt(byte[] bytes)
            {
                return ((bytes[3] & 0xFF) << 24) | ((bytes[2] & 0xFF) << 16) | ((bytes[1] & 0xFF) << 8) | (bytes[0] & 0xFF);
            }
        }
    }

    public sealed class MultiFacetDocComparatorSource : DocComparatorSource
    {
        private MultiDataCacheBuilder cacheBuilder;
        public MultiFacetDocComparatorSource(MultiDataCacheBuilder multiDataCacheBuilder)
        {
            cacheBuilder = multiDataCacheBuilder;
        }

        public override DocComparator GetComparator(IndexReader reader, int docbase)
        {
            if (!reader.GetType().Equals(typeof(BoboIndexReader)))
                throw new ArgumentException("reader must be instance of BoboIndexReader");
            BoboIndexReader boboReader = (BoboIndexReader)reader;
            MultiValueFacetDataCache dataCache = (MultiValueFacetDataCache)cacheBuilder.Build(boboReader);
            return new MultiFacetDocComparator(dataCache);
        }

        public sealed class MultiFacetDocComparator : DocComparator
        {
            private readonly MultiValueFacetDataCache _dataCache;

            public MultiFacetDocComparator(MultiValueFacetDataCache dataCache)
            {
                _dataCache = dataCache;
            }

            public override int Compare(ScoreDoc doc1, ScoreDoc doc2)
            {
                return _dataCache.NestedArray.Compare(doc1.Doc, doc2.Doc);
            }

            public override IComparable Value(ScoreDoc doc)
            {
                string[] vals = _dataCache.NestedArray.GetTranslatedData(doc.Doc, _dataCache.ValArray);
                return new StringArrayComparator(vals);
            }
        }
    }
}
