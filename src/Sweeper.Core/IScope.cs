﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Sweeper.Core
{
    public interface IScope:IDisposable
    {
        //void close();
        ISpan span();
    }
}