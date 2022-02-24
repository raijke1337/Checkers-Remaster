using System;
using UnityEngine;
using UnityEngine.EventSystems;
using static Checkers.GameManager;

namespace Checkers
{
    public class ChipComponent : BaseClickComponent
    {
        public override void OnPointerEnter(PointerEventData eventData)
        {
            CallBackEvent((CellComponent)Pair, true);
        }

        public override void OnPointerExit(PointerEventData eventData)
        {
            CallBackEvent((CellComponent)Pair, false);
        }


    }
    

}
