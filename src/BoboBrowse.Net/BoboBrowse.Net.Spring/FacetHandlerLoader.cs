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

namespace BoboBrowse.Net.Spring
{
    using BoboBrowse.Net;
    using BoboBrowse.Net.Facets;
    using global::Spring.Context.Support;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    public class FacetHandlerLoader
    {
        public IEnumerable<IFacetHandler> LoadFacetHandlers(string springConfigFile, BoboIndexReader.WorkArea workArea)
        {
            if (File.Exists(springConfigFile))
            {
                XmlApplicationContext appCtx = new XmlApplicationContext(springConfigFile);
                return appCtx.GetObjectsOfType(typeof(IFacetHandler)).Values.OfType<IFacetHandler>().ToList();
            }
            else
            {
                return new List<IFacetHandler>();
            }
        }
    }
}
