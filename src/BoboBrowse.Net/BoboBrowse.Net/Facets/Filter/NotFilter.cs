//* Bobo Browse Engine - High performance faceted/parametric search implementation 
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
namespace BoboBrowse.Net.Facets.Filter
{
    using Lucene.Net.Index;
    using Lucene.Net.Search;
    using LuceneExt.Impl;

    public class NotFilter : Filter
    {
        //private static long serialVersionUID = 1L; // NOT USED

        private readonly Filter _innerFilter;

        public NotFilter(Filter innerFilter)
        {
            _innerFilter = innerFilter;
        }

        public override DocIdSet GetDocIdSet(IndexReader reader)
        {
            return new NotDocIdSet(_innerFilter.GetDocIdSet(reader), reader.MaxDoc);
        }
    }
}