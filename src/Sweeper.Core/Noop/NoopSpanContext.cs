/*
 * Copyright 2016-2018 The OpenTracing Authors
 *
 * Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except
 * in compliance with the License. You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software distributed under the License
 * is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
 * or implied. See the License for the specific language governing permissions and limitations under
 * the License.
 */
using Sweeper.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Sweeper.Noop
{

    public sealed class NoopSpanContextImpl : ISpanContext
    {
        public static readonly NoopSpanContextImpl INSTANCE = new NoopSpanContextImpl();


        public IEnumerable<KeyValuePair<string, string>> baggageItems()
        {
            
            return new List<KeyValuePair<string,string>>();
        }

        public long TraceId { get; private set; }
        public int SpanId { get; private set; }


        public override string ToString() => typeof(NoopSpanContextImpl).Name; 
    }

}



