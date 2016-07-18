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

// Version compatibility level: 4.0.2
namespace BoboBrowse.Net.DocIdSet
{
    using System;
    using Common.Logging;
    using Lucene.Net.Search;

    public abstract class ImmutableDocSet : DocSet
    {
        private int size = -1;
        private static ILog log = LogManager.GetLogger(typeof(ImmutableDocSet));

        public override void AddDoc(int docid)
        {
            throw new NotSupportedException("Attempt to add document to an immutable data structure");
        }

        public override int Size
        {
            get { 
                // Do the size if we haven't done it so far.
                if (size < 0) {
                  DocIdSetIterator dcit = this.GetIterator();
                  size = 0;
                  try {
                    while (dcit.NextDoc() != DocIdSetIterator.NO_MORE_DOCS)
                      size++;
                  } catch (Exception e) {
                    log.Error("Error computing size..");
                    return -1;
                  }
                }
                return size;
            }
        }
    }
}
