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
namespace BoboBrowse.Net.Util
{
    using BoboBrowse.Net.DocIdSet;
    using BoboBrowse.Net.Support;
    using Lucene.Net.Search;
    using LuceneExt.Impl;
    using NUnit.Framework;
    using System;

    [TestFixture]
    public class FilterTest
    {
        [Test]
        public void TestFilteredDocSetIterator()
        {
            var set1 = new IntArrayDocIdSet();
            for (int i = 0; i < 100; i++)
            {
                set1.AddDoc(2 * i); // 100 even numbers
            }

            var filteredIter = new MyFilteredDocSetIterator(set1.Iterator());

            var bs = new BitSet(200);
            for (int i = 0; i < 100; ++i)
            {
                int n = 10 * i;
                if (n < 200)
                {
                    bs.Set(n);
                }
            }

            try
            {
                int doc;
                while ((doc = filteredIter.NextDoc()) != DocIdSetIterator.NO_MORE_DOCS)
                {
                    if (!bs.Get(doc))
                    {
                        Assert.Fail("failed: " + doc + " not in expected set");
                        return;
                    }
                    else
                    {
                        bs.Clear(doc);
                    }
                }
                if (bs.Cardinality() > 0)
                {
                    Assert.Fail("failed: leftover cardinality: " + bs.Cardinality());
                }
            }
            catch (Exception e)
            {
                Assert.Fail(e.Message);
            }
        }

        private class MyFilteredDocSetIterator : FilteredDocSetIterator
        {
            public MyFilteredDocSetIterator(DocIdSetIterator iterator)
                : base(iterator)
            {
            }

            protected override bool Match(int doc)
            {
                return doc % 5 == 0;
            }
        }
    }
}
