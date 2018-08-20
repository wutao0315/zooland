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
    public sealed class NoopSpanBuilderImpl : ISpanBuilder
    {

        public static readonly ISpanBuilder INSTANCE = new NoopSpanBuilderImpl();

        public ISpanBuilder addReference(string refType, ISpanContext referenced)
        {
            return this;
        }


        public ISpanBuilder asChildOf(ISpanContext parent)
        {
            return this;
        }


        public ISpanBuilder ignoreActiveSpan() { return this; }


        public ISpanBuilder asChildOf(ISpan parent)
        {
            return this;
        }


        public ISpanBuilder withTag(string key, String value)
        {
            return this;
        }


        public ISpanBuilder withTag(string key, bool value)
        {
            return this;
        }


        public ISpanBuilder withTag(string key, int value)
        {
            return this;
        }


        public ISpanBuilder withStartTimestamp(long microseconds)
        {
            return this;
        }


        public IScope startActive(bool finishOnClose)
        {
            return NoopScopeImpl.INSTANCE;
        }


        public ISpan start()
        {
            return startManual();
        }


        public ISpan startManual()
        {
            return NoopSpanImpl.INSTANCE;
        }

        public override string ToString() => typeof(NoopSpanBuilderImpl).Name;
    }
}


