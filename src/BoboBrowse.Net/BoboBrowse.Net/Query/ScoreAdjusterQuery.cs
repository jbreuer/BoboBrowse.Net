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
namespace BoboBrowse.Net.Query
{
    using Lucene.Net.Index;
    using Lucene.Net.Search;
    using System.Collections.Generic;

    public class ScoreAdjusterQuery : Query
    {
        //private static long serialVersionUID = 1L; // NOT USED

        private class ScoreAdjusterWeight : Weight
        {
            //private static long serialVersionUID = 1L; // NOT USED

            Weight _innerWeight;
            private readonly ScoreAdjusterQuery _parent;

            public ScoreAdjusterWeight(ScoreAdjusterQuery parent, Weight innerWeight)
            {
                _parent = parent;
                _innerWeight = innerWeight;
            }

            public override string ToString()
            {
                return "weight(" + _parent.ToString() + ")";
            }

            public override Query Query
            {
                get { return _innerWeight.Query; }
            }

            public override float Value
            {
                get { return _innerWeight.Value; }
            }

            public override float GetSumOfSquaredWeights()
            {
                return _innerWeight.GetSumOfSquaredWeights();
            }

            public override void Normalize(float queryNorm)
            {
                _innerWeight.Normalize(queryNorm);
            }

            public override Scorer Scorer(IndexReader reader, bool scoreDocsInOrder, bool topScorer)
            {
                Scorer innerScorer = _innerWeight.Scorer(reader, scoreDocsInOrder, topScorer);
                return _parent._scorerBuilder.CreateScorer(innerScorer, reader, scoreDocsInOrder, topScorer);
            }

            public override Explanation Explain(IndexReader reader, int doc)
            {
                Explanation innerExplain = _innerWeight.Explain(reader, doc);
                return _parent._scorerBuilder.Explain(reader, doc, innerExplain);
            }
        }

        protected readonly Query _query;
        protected readonly IScorerBuilder _scorerBuilder;

        public ScoreAdjusterQuery(Query query, IScorerBuilder scorerBuilder)
        {
            _query = query;
            _scorerBuilder = scorerBuilder;
        }

        public override void ExtractTerms(ISet<Term> terms)
        {
            _query.ExtractTerms(terms);
        }

        public override Weight CreateWeight(Searcher searcher)
        {
            return new ScoreAdjusterWeight(this, _query.CreateWeight(searcher));
        }

        public override Query Rewrite(IndexReader reader)
        {
            _query.Rewrite(reader);
            return this;
        }

        public override string ToString(string field)
        {
            return _query.ToString(field);
        }
    }
}
