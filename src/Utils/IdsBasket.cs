using DCFApixels.DragonECS.Relations.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DragonECS.DragonECS
{
    internal class IdsBasket
    {
        private IdsLinkedList _linketdList = new IdsLinkedList(4);
        private int[] _mapping;
        private int[] _counts;

        public void Clear()
        {
            _linketdList.Clear();
            ArrayUtility.Fill(_mapping, 0, 0, _mapping.Length);
            ArrayUtility.Fill(_counts, 0, 0, _counts.Length);
        }
        public void AddToHead(int headEntityID, int addedEntityID)
        {
            ref var nodeIndex = ref _mapping[headEntityID];
            if (nodeIndex <= 0)
            {
                nodeIndex = _linketdList.Add(addedEntityID);
            }
            else
            {
                _linketdList.InsertAfter(nodeIndex, addedEntityID);
            }
            _counts[headEntityID]++;
        }

        public void DelHead(int headEntityID)
        {

        }

        public IdsLinkedList.Span GetEntitiesFor(int entity)
        {
            ref var nodeIndex = ref _mapping[entity];
            if (nodeIndex <= 0)
                return _linketdList.EmptySpan();
            else
                return _linketdList.GetSpan(nodeIndex, _counts[entity]);
        }
    }
}
