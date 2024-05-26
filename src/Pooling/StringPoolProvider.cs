﻿using System.Text;

namespace Oxide.Pooling
{
    internal sealed class StringPoolProvider : BasePoolProvider<StringBuilder>
    {
        protected override void OnTake(StringBuilder item) => OnReturn(item);

        protected override bool OnReturn(StringBuilder item)
        {
            item.Length = 0;
            return true;
        }

        protected override StringBuilder InstantiateItem() => new StringBuilder();
    }
}
