﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RpcProviderCore.Models
{
    public class HelloDTO
    {
        public class HelloModel
        {
            public string Name { get; set; }
            public string Gender { get; set; }
            public string Head { get; set; }
        }
    }
}