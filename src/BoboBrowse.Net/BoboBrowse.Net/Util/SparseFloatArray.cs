﻿//* Bobo Browse Engine - High performance faceted/parametric search implementation 
//* that handles various types of semi-structured data.  Originally written in Java.
//*
//* Ported and adapted for C# by Shad Storhaug.
//*
//* Copyright (C) 2005-2015  Spackle
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
    using BoboBrowse.Net.Support;
    using System;

    public class SparseFloatArray
    {
        protected float[] _floats;
        protected BitSet _bits;

        // the number of bits set BEFORE the given reference point index*REFERENCE_POINT_EVERY.
        protected int[] _referencePoints;
        private int _capacity;
        protected const float ON_RATIO_CUTOFF = 0.75f;

        // 32 is 32 bits per 256 floats, which is the same as the 32 bits per 32 floats that are needed
        // in _bits.  
        protected const int REFERENCE_POINT_EVERY = 32;//256;

        /// <summary>
        /// Good for saving memory with sparse float arrays, when those arrays no longer need to be mutable.
        /// 
        /// requires: floats never changes after this method is called returns.
        /// in fact, you should lose all references to it, since this object 
        /// might save you a lot of memory.
        /// </summary>
        /// <param name="floats">The float array.</param>
        public SparseFloatArray(float[] floats)
        {
            _capacity = floats.Length;
            Condense(floats);
        }

        /// <summary>
        /// Short-cut to quickly create a sparse float array representing 
        /// <code>this(new float[capacity]);</code>, but without reading through said array.
        /// The advantage here is that the constructor is lightning-fast in the case that 
        /// all values in the float array are known to 
        /// <c>== 0f</c>.
        /// </summary>
        /// <param name="capacity">The capacity of the array.</param>
        public SparseFloatArray(int capacity)
        {
            _capacity = capacity;
            _floats = null;
            _bits = null;
            _referencePoints = null;
        }

        protected virtual void Condense(float[] floats)
        {
            if (floats.Length != _capacity)
            {
                throw new ArgumentException("bad input float array of length " + floats.Length + " for capacity: " + _capacity);
            }
            var bits = new BitSet(floats.Length);
            int on = 0;
            for (int i = 0; i < floats.Length; i++)
            {
                if (floats[i] != 0f)
                {
                    bits.Set(i);
                    on++;
                }
            }
            if (((float)on) / ((float)floats.Length) < ON_RATIO_CUTOFF)
            {
                // it's worth compressing
                if (0 == on)
                {
                    // it's worth super-compressing
                    _floats = null;
                    _bits = null;
                    _referencePoints = null;
                    // capacity is good.
                }
                else
                {
                    _bits = bits;
                    _floats = new float[_bits.Cardinality()];
                    _referencePoints = new int[floats.Length / REFERENCE_POINT_EVERY];
                    int i = 0;
                    int floatsIdx = 0;
                    int refIdx = 0;
                    while (i < floats.Length && (i = _bits.NextSetBit(i)) >= 0)
                    {
                        _floats[floatsIdx] = floats[i];
                        while (refIdx < i / REFERENCE_POINT_EVERY)
                        {
                            _referencePoints[refIdx++] = floatsIdx;
                        }
                        floatsIdx++;
                        i++;
                    }
                    while (refIdx < _referencePoints.Length)
                    {
                        _referencePoints[refIdx++] = floatsIdx;
                    }
                }
            }
            else
            {
                // it's not worth compressing
                _floats = floats;
                _bits = null;
            }
        }

        /// <summary>
        /// warning: DO NOT modify the return value at all.
        /// the assumption is that these arrays are QUITE LARGE and that we would not want 
        /// to unnecessarily copy them.  this method in many cases returns an array from its
        /// internal representation.  doing anything other than READING these values 
        /// results in UNDEFINED operations on this, from that point on.
        /// </summary>
        /// <returns></returns>
        public virtual float[] Expand()
        {
            if (null == _bits)
            {
                if (null == _floats)
                {
                    // super-compressed, all zeros
                    return new float[_capacity];
                }
                else
                {
                    return _floats;
                }
            }
            float[] all = new float[_capacity];
            int floatsidx = 0;
            for (int idx = _bits.NextSetBit(0); idx >= 0 && idx < _capacity; idx = _bits.NextSetBit(idx + 1))
            {
                all[idx] = _floats[floatsidx++];
            }
            return all;
        }

        public virtual float Get(int index)
        {
            if (null == _bits)
            {
                if (null == _floats)
                {
                    // super-compressed, all zeros
                    if (index < 0 || index >= _capacity)
                    {
                        throw new IndexOutOfRangeException("bad index: " + index + " for SparseFloatArray representing array of length " + _capacity);
                    }
                    return 0f;
                }
                else
                {
                    return _floats[index];
                }
            }
            else
            {
                if (_bits.Get(index))
                {
                    // count the number of bits that are on BEFORE this index
                    int count;
                    int @ref = index / REFERENCE_POINT_EVERY - 1;
                    if (@ref >= 0)
                    {
                        count = _referencePoints[@ref];
                    }
                    else
                    {
                        count = 0;
                    }
                    int i = index - index % REFERENCE_POINT_EVERY;
                    while ((i = _bits.NextSetBit(i)) >= 0 && i < index)
                    {
                        count++;
                        i++;
                    }
                    return _floats[count];
                }
                else
                {
                    return 0f;
                }
            }
        }
    }
}
