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
namespace BoboBrowse.Net
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// This class represents a facet
    /// </summary>
    [Serializable]
    public class BrowseFacet
    {
        //private static long serialVersionUID = 1L; // NOT USED

        private string _value;
        private int _hitcount;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:BrowseFacet"/> class.
        /// </summary>
        public BrowseFacet() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:BrowseFacet"/> class with the provided value and hit count.
        /// </summary>
        /// <param name="value">The facet value.</param>
        /// <param name="hitCount">The hit count.</param>
        public BrowseFacet(string value, int hitCount)
        {
            _value = value;
            _hitcount = hitCount;
        }

        /// <summary>
        /// Gets or sets the facet value.
        /// </summary>
        public virtual string Value 
        {
            get { return _value; }
            set { _value = value; }
        }

        /// <summary>
        /// Gets or sets the hit count.
        /// </summary>
        [Obsolete("Use FacetValueHitCount instead")]
        public virtual int HitCount
        {
            get { return _hitcount; }
            set { _hitcount = value; }
        }

        /// <summary>
        /// Gets or sets the hit count.
        /// </summary>
        public virtual int FacetValueHitCount
        {
            get { return _hitcount; }
            set { _hitcount = value; }
        }

        public override string ToString()
        {
            return string.Concat(Value, "(", _hitcount, ")");
        }

        public override bool Equals(object obj)
        {
            bool equals = false;

            if (obj is BrowseFacet)
            {
                BrowseFacet c2 = (BrowseFacet)obj;
                if (_hitcount == c2._hitcount && _value.Equals(c2._value))
                {
                    equals = true;
                }
            }
            return equals;
        }

        // Required by .NET because Equals() was overridden.
        // Source: http://stackoverflow.com/questions/70303/how-do-you-implement-gethashcode-for-structure-with-two-string#21604191
        public override int GetHashCode()
        {
            // Since any of the properties could change at any time, we need to
            // rely on the default implementation of GetHashCode for Contains.
            return base.GetHashCode();

            //unchecked
            //{
            //    int hashCode = 0;

            //    // String properties
            //    hashCode = (hashCode * 397) ^ (_value != null ? _value.GetHashCode() : string.Empty.GetHashCode());

            //    // int properties
            //    hashCode = (hashCode * 397) ^ _hitcount;

            //    return hashCode;
            //}
        }

        public virtual IEnumerable<BrowseFacet> Merge(IEnumerable<BrowseFacet> v, IComparer<BrowseFacet> comparator)
        {
            int i = 0;
            foreach (var facet in v)
            {
                int val = comparator.Compare(this, facet);
                if (val == 0)
                {
                    facet._hitcount += _hitcount;
                    return v;
                }
                i++;
            }
            var result = new List<BrowseFacet>(v);
            result.Add(this);
            return result;
        }
    }
}
