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
    using BoboBrowse.Net.Support;
    using Lucene.Net.Documents;
    using Lucene.Net.Search;
    using System;
    using System.Collections.Generic;
    using System.Text;

    ///<summary>A hit from a browse.</summary>
    [Serializable]
    public class BrowseHit
    {
        //private static long serialVersionUID = 1L; // NOT USED

        [Serializable]
        public class TermFrequencyVector
        {
            //private static long serialVersionUID = 1L; // NOT USED
            public readonly string[] terms;
            public readonly int[] freqs;

            public TermFrequencyVector(string[] terms, int[] freqs)
            {
                this.terms = terms;
                this.freqs = freqs;
            }
        }

        [NonSerialized]
        private IComparable _comparable;

        /// <summary>
        /// Gets or sets the score.
        /// </summary>
        public virtual float Score { get; set; }

        /// <summary>
        /// Get the field values.
        /// </summary>
        /// <param name="field">field name</param>
        /// <returns>field value array</returns>
        /// <seealso cref="GetField(string)"/>
        public virtual string[] GetFields(string field)
        {
            return this.FieldValues != null ? this.FieldValues[field] : null;
        }

        /// <summary>
        /// Get the raw field values.
        /// </summary>
        /// <param name="field">field name</param>
        /// <returns>field value array</returns>
        public virtual object[] GetRawFields(string field)
        {
            return this.RawFieldValues != null ? this.RawFieldValues.Get(field) : null;
        }

        /// <summary>
        /// Gets the field value by field name.
        /// </summary>
        /// <param name="field">field name</param>
        /// <returns>field value</returns>
        /// <seealso cref="GetFields(string)"/>
        public virtual string GetField(string field)
        {
            string[] fields = this.GetFields(field);
            if (fields != null && fields.Length > 0)
            {
                return fields[0];
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Get the raw field value.
        /// </summary>
        /// <param name="field">field name</param>
        /// <returns>raw field value</returns>
        public virtual object GetRawField(string field)
        {
            object[] fields = this.GetRawFields(field);
            if (fields != null && fields.Length > 0)
            {
                return fields[0];
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Gets or sets a dictionary of field names to <see cref="T:TermFrequencyVector"/> instances. These are populated when specified in the <see cref="P:BrowseRequest.TermVectorsToFetch"/> property.
        /// A term vector is a list of the document's terms and their number of occurrences in that document.
        /// </summary>
        public virtual Dictionary<string, TermFrequencyVector> TermFreqMap { get; set; }

        /// <summary>
        /// Gets or sets the position of the <see cref="P:GroupField"/> inside groupBy request.
        /// NOTE: This does not appear to be in use by BoboBrowse.
        /// </summary>
        public virtual int GroupPosition { get; set; }

        /// <summary>
        /// Gets or sets the group field inside groupBy request.
        /// </summary>
        public virtual string GroupField { get; set; }

        /// <summary>
        /// Gets or sets the string value of the field that is currently the groupBy request.
        /// </summary>
        public virtual string GroupValue { get; set; }

        /// <summary>
        /// Gets or sets the primitive value of the field that is currently the groupBy request.
        /// </summary>
        public virtual object RawGroupValue { get; set; }

        /// <summary>
        /// Gets or sets the total FacetValueHitCount of the groupBy request.
        /// </summary>
        public virtual int GroupHitsCount { get; set; }

        /// <summary>
        /// Gets or sets the hits of the group.
        /// NOTE: This field does not appear to be in use by BoboBrowse.
        /// </summary>
        public virtual BrowseHit[] GroupHits { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="T:Lucene.Net.Search.Explanation"/>. This will be set if the <see cref="P:BrowseRequest.ShowExplanation"/> property is set to true.
        /// An <see cref="T:Lucene.Net.Search.Explanation"/> describes the score computation for document and query.
        /// </summary>
        public virtual Explanation Explanation { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="System.IComparable"/> value that is used to compare the current hit to other hits for sorting purposes.
        /// </summary>
        public virtual IComparable Comparable 
        {
            get { return _comparable; }
            set { _comparable = value; }
        }

        /// <summary>
        /// Gets or sets the internal document id.
        /// </summary>
        public virtual int DocId { get; set; }

        /// <summary>
        /// Gets or sets the field values.
        /// </summary>
        public virtual Dictionary<string, string[]> FieldValues { get; set; }

        /// <summary>
        /// Gets or sets the raw field value map.
        /// </summary>
        public virtual Dictionary<string, object[]> RawFieldValues { get; set; }

        /// <summary>
        /// Gets or sets the stored fields (a reference to the Lucene.Net Document).
        /// </summary>
        public virtual Document StoredFields { get; set; }


        public string ToString(IDictionary<string, string[]> map)
        {
            StringBuilder buffer = new StringBuilder();
            foreach (KeyValuePair<string, string[]> e in map)
            {
                buffer.Append(e.Key);
                buffer.Append(":");
                var vals = e.Value;
                buffer.Append(vals == null ? null : string.Join(", ", e.Value));
            }
            return buffer.ToString();
        }

        /// <summary>
        /// Gets a string representation of the current BrowseHit.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder buffer = new StringBuilder();
            buffer.Append("docid: ").Append(DocId);
            buffer.Append(" score: ").Append(Score).AppendLine();
            buffer.Append(" field values: ").Append(ToString(FieldValues)).AppendLine();
            return buffer.ToString();
        }
    }
}
