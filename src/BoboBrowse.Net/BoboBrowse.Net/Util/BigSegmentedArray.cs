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
namespace BoboBrowse.Net.Util
{
    using Lucene.Net.Util;

    /// <summary>
    /// Breaks up a regular .NET array by splitting it into a 2 dimensional array with
    /// a predefined block size. Attempts to induce more efficient GC.
    /// </summary>
    public abstract class BigSegmentedArray
    {
        private readonly int _size;
        private readonly int _blockSize;
        private readonly int _shiftSize;

        protected internal int _numrows;

        protected BigSegmentedArray(int size)
        {
            _size = size;
            _blockSize = GetBlockSize();
            _shiftSize = GetShiftSize();
            _numrows = (size >> _shiftSize) + 1;
        }

        public virtual int Size()
        {
            return _size;
        }

        protected abstract int GetBlockSize();

        // TODO: maybe this should be automatically calculated
        protected abstract int GetShiftSize();

        public abstract int Get(int id);

        public virtual int Capacity()
        {
            return _numrows * _blockSize;
        }

        public abstract void Add(int id, int val);

        public abstract void Fill(int val);

        public abstract void EnsureCapacity(int size);

        public abstract int MaxValue { get; }

        public abstract int FindValue(int val, int id, int maxId);

        public abstract int FindValues(OpenBitSet bitset, int id, int maxId);

        public abstract int FindValues(BitVector bitset, int id, int maxId);

        public abstract int FindValueRange(int minVal, int maxVal, int id, int maxId);

        public abstract int FindBits(int bits, int id, int maxId);
    }
}
