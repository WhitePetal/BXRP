using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BXRenderPipeline
{
    /// <summary>
    /// 遍历SVCC时会直接进行Shader预热
    /// </summary>
    public class SVCC : ScriptableObject
    {
        [SerializeField]
        public ShaderVariantCollection[] collections;

        public int Count
        {
            get
            {
                return collections.Length;
            }
        }

        public struct Iterator
        {
            private readonly SVCC owner;

            private int index;
            
            public Iterator(SVCC setOwner)
            {
                this.owner = setOwner;
                index = 0;
            }

			public ref ShaderVariantCollection Current
            {
                get
                {
                    ref var svc = ref owner[index];
                    return ref svc;
                }
            }

            public bool MoveNext()
            {
                ++index;
                return index < owner.collections.Length;
            }

            public void Reset()
            {
                index = -1;
            }
        }

        public Iterator GetEnumerator()
        {
            return new Iterator(this);
        }

        /// <summary>
        /// 这里会直接执行Shader预热
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public ref ShaderVariantCollection this[int index]
        {
            get
            {
                var svc = collections[index];
                if (svc != null && !svc.isWarmedUp)
                    svc.WarmUp();
                return ref collections[index];
            }
        }
    }
}
