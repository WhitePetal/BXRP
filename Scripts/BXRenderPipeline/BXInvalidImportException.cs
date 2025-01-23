using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXRenderPipeline
{
    public class BXInvalidImportException : Exception
    {
        public BXInvalidImportException(string message)
                    : base(message)
        {
        }
    }
}
